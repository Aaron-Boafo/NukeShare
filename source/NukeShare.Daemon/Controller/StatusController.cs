using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using NukeShare.Configuration.Models;
using NukeShare.Network.Discovery;

namespace NukeShare.Daemon.Controller;

[ApiController]
[Route("v1/status")]
public class StatusController : ControllerBase
{
    private static readonly DateTime StartTime = DateTime.UtcNow;
    private readonly PeerRegistry _peerRegistry;
    private readonly GlobalConfiguration _config;

    public StatusController(PeerRegistry peerRegistry, GlobalConfiguration config)
    {
        _peerRegistry = peerRegistry;
        _config = config;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var currentProcess = Process.GetCurrentProcess();

        return Ok(new
        {
            status = "online",
            processId = Environment.ProcessId,
            machine = Environment.MachineName,
            timestamp = DateTime.UtcNow,
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0",
            uptime = DateTime.UtcNow - StartTime,
            memoryUsageMb = Math.Round(currentProcess.WorkingSet64 / (1024.0 * 1024.0), 2)
        });
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        bool storageReady = Directory.Exists(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "NukeShare"));

        return Ok(new
        {
            healthy = storageReady,
            checks = new
            {
                storageDirectory = storageReady ? "Accessible" : "Missing",
                discoveryListener = "Listening",
                transferListener = "Listening"
            }
        });
    }

    [HttpGet("peers")]
    public IActionResult GetPeers()
    {
        var all = _peerRegistry.GetAll();
        var active = _peerRegistry.GetActivePeers();

        return Ok(new
        {
            totalDiscovered = all.Count,
            connected = active.Count,
            maxPeers = _config.MaxPeers,
            peers = active.Select(p => new
            {
                nodeId = p.NodeId,
                deviceName = p.DeviceName,
                ipAddress = p.IpAddress,
                port = p.Port,
                trust = p.Trust.ToString(),
                lastSeen = p.LastSeen
            }).ToArray()
        });
    }

    [HttpGet("transfers")]
    public IActionResult GetTransfers()
    {
        //TODO: Replace with your ActiveTransfers tracker
        return Ok(new
        {
            activeCount = 1,
            transfers = new[]
            {
                new
                {
                    transferId = Guid.NewGuid(),
                    fileName = "release.iso",
                    direction = "Receiving",
                    totalBytes = 1073741824L,
                    transferredBytes = 429496729L,
                    progressPercentage = 40.0,
                    transferSpeedBytesPerSec = 15728640,
                    activeChunks = 4
                }
            }
        });
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        return Ok(new
        {
            identity = new
            {
                username = _config.Username,
                deviceName = _config.DeviceName
            },
            network = new
            {
                listenPort = _config.DefaultListenPort,
                discoveryPort = _config.DefaultDiscoveryPort,
                maxPeers = _config.MaxPeers,
                peerTimeoutSeconds = _config.PeerTimeoutSeconds,
                discoveryIntervalSeconds = _config.DiscoveryIntervalSeconds,
                useMulticastDiscovery = _config.UseMulticastDiscovery,
                allowedSubnets = _config.AllowedSubnets
            },
            security = new
            {
                enableEncryption = _config.EnableEncryption,
                encryptionCipher = _config.EncryptionCipher,
                trustMode = _config.TrustMode,
                autoAcceptUntrustedPeers = _config.AutoAcceptUntrustedPeers
            },
            transfer = new
            {
                autoAcceptPeers = _config.AutoAcceptPeers,
                chunks = _config.Chunks,
                chunkSize = _config.ChunkSize,
                concurrentPeers = _config.ConcurrentPeers,
                bandwidthLimitBytesPerSecond = _config.BandwidthLimitBytesPerSecond,
                maxFileSizeBytes = _config.MaxFileSizeBytes
            }
        });
    }

    [HttpPost("shutdown")]
    public IActionResult Shutdown([FromServices] IHostApplicationLifetime lifetime)
    {
        Task.Run(async () =>
        {
            await Task.Delay(200);
            lifetime.StopApplication();
        });

        return Ok(new { message = "Daemon shutdown initiated." });
    }
}
