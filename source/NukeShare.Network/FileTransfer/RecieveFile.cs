using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NukeShare.Configuration.Models;

namespace NukeShare.Network.FileTransfer;

public class TcpFileReceiverService : BackgroundService
{
    private const int BufferSize = 64 * 1024; // 64 KB chunks
    private const int MaxFileNameLength = 1024;
    private static readonly TimeSpan ReadIdleTimeout = TimeSpan.FromSeconds(60);

    private readonly ILogger<TcpFileReceiverService> _logger;
    private readonly int _transferPort;
    private readonly string _incomingPath;
    private readonly long _maxFileSizeBytes;

    public TcpFileReceiverService(ILogger<TcpFileReceiverService> logger, GlobalConfiguration config)
    {
        _logger = logger;
        _transferPort = int.TryParse(config.DefaultTransferPort, out var port) ? port : 7656;
        _incomingPath = config.IncomingPath;
        _maxFileSizeBytes = config.MaxFileSizeBytes;

        Directory.CreateDirectory(_incomingPath);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _transferPort);
        listener.Start();
        _logger.LogInformation("TCP File Transfer listener bound to 0.0.0.0:{Port}", _transferPort);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = ProcessIncomingClientAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task ProcessIncomingClientAsync(TcpClient client, CancellationToken ct)
    {
        string? destPath = null;

        using (client)
        await using (var netStream = client.GetStream())
        {
            try
            {
                // 1. Read File Name Length (4 bytes)
                byte[] lenBuf = await ReadExactlyAsync(netStream, 4, ct);
                int nameLen = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);
                if (nameLen <= 0 || nameLen > MaxFileNameLength)
                    throw new InvalidDataException($"Invalid file name length received: {nameLen}.");

                // 2. Read File Name (N bytes)
                byte[] nameBuf = await ReadExactlyAsync(netStream, nameLen, ct);
                string fileName = Path.GetFileName(Encoding.UTF8.GetString(nameBuf)); // Sanitize against path traversal
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new InvalidDataException("Received an invalid file name.");

                // 3. Read File Size (8 bytes)
                byte[] sizeBuf = await ReadExactlyAsync(netStream, 8, ct);
                long fileSize = BinaryPrimitives.ReadInt64LittleEndian(sizeBuf);
                if (fileSize <= 0)
                    throw new InvalidDataException($"Invalid file size received: {fileSize}.");
                if (fileSize > _maxFileSizeBytes)
                {
                    _logger.LogError(
                        "Rejected {FileName}: size {Size:N0} bytes exceeds the max of {Max:N0} bytes.",
                        fileName, fileSize, _maxFileSizeBytes);
                    return;
                }

                // 4. Read Expected SHA-256 Hash (32 bytes)
                byte[] expectedHash = await ReadExactlyAsync(netStream, 32, ct);

                destPath = Path.Combine(_incomingPath, fileName);
                _logger.LogInformation("Receiving {FileName} ({Size:N0} bytes)", fileName, fileSize);

                // 5. Stream payload to disk and compute incremental hash
                byte[] actualHash = await ReceivePayloadAsync(netStream, destPath, fileSize, ct);

                if (!actualHash.SequenceEqual(expectedHash))
                {
                    _logger.LogError("Hash mismatch on {FileName}! Transfer corrupted; deleting file.", fileName);
                    TryDelete(destPath);
                    return;
                }

                _logger.LogInformation("File saved and verified: {Path}", destPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer failed for {Path}.", destPath ?? "(header)");
                TryDelete(destPath);
            }
        }
    }

    private async Task<byte[]> ReceivePayloadAsync(NetworkStream netStream, string destPath, long fileSize, CancellationToken ct)
    {
        await using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        byte[] poolBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        long totalRead = 0;

        try
        {
            while (totalRead < fileSize)
            {
                int toRead = (int)Math.Min(BufferSize, fileSize - totalRead);
                int read = await netStream.ReadAsync(poolBuffer.AsMemory(0, toRead), ct)
                    .AsTask()
                    .WaitAsync(ReadIdleTimeout, ct);

                if (read == 0)
                    throw new EndOfStreamException("Connection aborted before file transfer completed.");

                await fileStream.WriteAsync(poolBuffer.AsMemory(0, read), ct);
                sha256.AppendData(poolBuffer, 0, read);
                totalRead += read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(poolBuffer);
        }

        return sha256.GetHashAndReset();
    }

    private static async Task<byte[]> ReadExactlyAsync(NetworkStream netStream, int count, CancellationToken ct)
    {
        byte[] buffer = new byte[count];
        await netStream.ReadExactlyAsync(buffer, ct)
            .AsTask()
            .WaitAsync(ReadIdleTimeout, ct);
        return buffer;
    }

    private void TryDelete(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete partial file {Path}.", path);
        }
    }
}