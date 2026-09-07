using System.Collections.Concurrent;
using NukeShare.Network.Discovery;
using NukeShare.Network.FileTransfer;

namespace NukeShare.Daemon.Service;

public enum TransferState
{
    Queued,
    Transferring,
    Completed,
    Failed,
    Cancelled
}

public record TransferEntry(
    Guid TransferId,
    string NodeId,
    string FileName,
    string Direction,
    long TotalBytes,
    DateTime StartedAt,
    long TransferredBytes,
    long TransferSpeedBytesPerSec,
    TransferState State,
    DateTime? CompletedAt,
    string? Error)
{
    public double ProgressPercentage => TotalBytes > 0
        ? Math.Min(100.0, TransferredBytes / (double)TotalBytes * 100.0)
        : 100.0;
}

public record TransferDto(
    Guid TransferId,
    string FileName,
    string Direction,
    long TotalBytes,
    long TransferredBytes,
    double ProgressPercentage,
    long TransferSpeedBytesPerSec,
    int ActiveChunks,
    string State);

public class TransferTracker
{
    private static readonly TimeSpan HistoryRetention = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<Guid, TransferEntry> _transfers = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellations = new();

    public async Task<TransferEntry> StartAsync(PeerInfo peer, string filePath, CancellationToken ct = default)
    {
        var fileInfo = new FileInfo(filePath);
        var entry = new TransferEntry(
            TransferId: Guid.NewGuid(),
            NodeId: peer.NodeId,
            FileName: fileInfo.Name,
            Direction: "Sending",
            TotalBytes: fileInfo.Length,
            StartedAt: DateTime.UtcNow,
            TransferredBytes: 0,
            TransferSpeedBytesPerSec: 0,
            State: TransferState.Queued,
            CompletedAt: null,
            Error: null);

        _transfers[entry.TransferId] = entry;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _cancellations[entry.TransferId] = cts;

        _ = RunTransferAsync(peer, filePath, entry, cts.Token);

        return entry;
    }

    private async Task RunTransferAsync(PeerInfo peer, string filePath, TransferEntry entry, CancellationToken ct)
    {
        var progress = new Progress<double>(percent => UpdateProgress(entry, percent));

        try
        {
            await new FileTransferSender().SendAsync(peer.IpAddress, peer.Port, filePath, progress, ct);
            _transfers[entry.TransferId] = entry with
            {
                TransferredBytes = entry.TotalBytes,
                TransferSpeedBytesPerSec = 0,
                State = TransferState.Completed,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            _transfers[entry.TransferId] = entry with
            {
                State = TransferState.Cancelled,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _transfers[entry.TransferId] = entry with
            {
                State = TransferState.Failed,
                CompletedAt = DateTime.UtcNow,
                Error = ex.Message
            };
        }
        finally
        {
            if (_cancellations.TryRemove(entry.TransferId, out var cts))
                cts.Dispose();
        }
    }

    private void UpdateProgress(TransferEntry entry, double percent)
    {
        if (entry.TotalBytes <= 0)
            return;

        var now = DateTime.UtcNow;
        long transferred = Math.Min((long)(percent / 100.0 * entry.TotalBytes), entry.TotalBytes);
        var elapsed = now - entry.StartedAt;
        long speed = elapsed.TotalSeconds > 0 ? (long)(transferred / elapsed.TotalSeconds) : 0;

        _transfers[entry.TransferId] = entry with
        {
            TransferredBytes = transferred,
            TransferSpeedBytesPerSec = speed,
            State = TransferState.Transferring
        };
    }

    public bool Cancel(Guid transferId)
    {
        if (!_cancellations.TryGetValue(transferId, out var cts))
            return false;

        cts.Cancel();
        return true;
    }

    public TransferEntry? Get(Guid transferId)
    {
        return _transfers.TryGetValue(transferId, out var entry) ? entry : null;
    }

    public List<TransferEntry> GetActive()
    {
        Prune();
        return _transfers.Values
            .Where(t => t.State is TransferState.Queued or TransferState.Transferring)
            .OrderBy(t => t.StartedAt)
            .ToList();
    }

    public List<TransferEntry> GetRecent()
    {
        Prune();
        return _transfers.Values
            .OrderByDescending(t => t.StartedAt)
            .ToList();
    }

    public static TransferDto ToDto(TransferEntry entry)
    {
        return new TransferDto(
            entry.TransferId,
            entry.FileName,
            entry.Direction,
            entry.TotalBytes,
            entry.TransferredBytes,
            entry.ProgressPercentage,
            entry.TransferSpeedBytesPerSec,
            ActiveChunks: 1,
            entry.State.ToString());
    }

    private void Prune()
    {
        var cutoff = DateTime.UtcNow - HistoryRetention;

        foreach (var kv in _transfers)
        {
            if (kv.Value.State is not (TransferState.Completed or TransferState.Failed or TransferState.Cancelled))
                continue;

            if (kv.Value.CompletedAt is { } completedAt && completedAt < cutoff)
            {
                _transfers.TryRemove(kv.Key, out _);

                if (_cancellations.TryRemove(kv.Key, out var cts))
                    cts.Dispose();
            }
        }
    }
}