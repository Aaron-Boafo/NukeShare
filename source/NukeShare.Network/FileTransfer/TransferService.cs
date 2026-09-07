using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace NukeShare.Network.FileTransfer;

public class FileTransferSender
{
    private const int BufferSize = 64 * 1024;

    public async Task SendAsync(string peerIp, int peerPort, string filePath, IProgress<double>? progress, CancellationToken ct)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);

        // Precompute file hash
        byte[] hash = await ComputeSha256Async(filePath, ct);

        using var client = new TcpClient();
        client.NoDelay = true; // Disable Nagle's algorithm for low packet latency
        client.SendTimeout = 30_000;
        await client.ConnectAsync(peerIp, peerPort, ct);

        await using NetworkStream netStream = client.GetStream();

        // 1. Send Name Length + Name
        byte[] nameBytes = Encoding.UTF8.GetBytes(fileInfo.Name);
        byte[] nameLenBuf = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(nameLenBuf, nameBytes.Length);
        await netStream.WriteAsync(nameLenBuf, ct);
        await netStream.WriteAsync(nameBytes, ct);

        // 2. Send Size
        byte[] sizeBuf = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(sizeBuf, fileInfo.Length);
        await netStream.WriteAsync(sizeBuf, ct);

        // 3. Send Hash
        await netStream.WriteAsync(hash, ct);
        await netStream.FlushAsync(ct);

        // 4. Stream file body
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        long bytesSent = 0;

        try
        {
            int read;
            while ((read = await fileStream.ReadAsync(buffer.AsMemory(0, BufferSize), ct)) > 0)
            {
                await netStream.WriteAsync(buffer.AsMemory(0, read), ct);
                bytesSent += read;

                progress?.Report((double)bytesSent / fileInfo.Length * 100);
            }

            await netStream.FlushAsync(ct);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<byte[]> ComputeSha256Async(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return await SHA256.HashDataAsync(stream, ct);
    }
}