using Microsoft.AspNetCore.Mvc;
using NukeShare.Configuration.Models;
using NukeShare.Daemon.Service;
using NukeShare.Network.Discovery;

namespace NukeShare.Daemon.Controller;

[ApiController]
[Route("v1/transfer")]
public class TransferController(
    TransferTracker _tracker, 
    PeerRegistry _peerRegistry, 
    GlobalConfiguration _config ) : ControllerBase
{
  
    [HttpGet]
    public IActionResult Index()
    {
        return Ok(new
        {
            activeCount = _tracker.GetActive().Count,
            transfers = _tracker.GetActive().Select(TransferTracker.ToDto).ToArray()
        });
    }

    [HttpGet("recent")]
    public IActionResult GetRecent()
    {
        return Ok(new
        {
            transfers = _tracker.GetRecent().Select(TransferTracker.ToDto).ToArray()
        });
    }

    [HttpGet("{transferId:guid}")]
    public IActionResult Get(Guid transferId)
    {
        var entry = _tracker.Get(transferId);
        if (entry == null)
            return NotFound(new { error = $"Transfer {transferId} not found." });

        return Ok(TransferTracker.ToDto(entry));
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] TransferSendRequest request, CancellationToken ct)
    {
        var peer = _peerRegistry.GetPeer(request.NodeId);
        if (peer == null)
            return NotFound(new { error = $"Peer {request.NodeId} not found or not discovered yet." });

        if (!System.IO.File.Exists(request.FilePath))
            return BadRequest(new { error = $"File not found: {request.FilePath}" });

        var fileInfo = new FileInfo(request.FilePath);
        if (_config.MaxFileSizeBytes > 0 && fileInfo.Length > _config.MaxFileSizeBytes)
            return BadRequest(new
            {
                error = $"File size {fileInfo.Length:N0} bytes exceeds the max of {_config.MaxFileSizeBytes:N0} bytes."
            });

        var entry = await _tracker.StartAsync(peer, request.FilePath, ct);
        return Accepted(new { transfer = TransferTracker.ToDto(entry) });
    }

    [HttpPost("{transferId:guid}/cancel")]
    public IActionResult Cancel(Guid transferId)
    {
        if (!_tracker.Cancel(transferId))
            return NotFound(new { error = $"Transfer {transferId} not found or already completed." });

        return Ok(new { transferId, message = "Transfer cancellation requested." });
    }
}

public record TransferSendRequest(string NodeId, string FilePath);