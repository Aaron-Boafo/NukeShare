using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NukeShare.Configuration.Models;

namespace NukeShare.Network.Discovery;

public class UdpDiscoveryService : BackgroundService
{
    private readonly int _discoveryPort;
    private readonly int _transferPort;
    private readonly int _discoveryIntervalSeconds;
    private readonly string _username;
    private readonly string _deviceName;
    private readonly string _trustMode;
    private readonly bool _autoAcceptPeers;
    private readonly string _nodeId = Guid.NewGuid().ToString("N")[..8];
    private readonly ILogger<UdpDiscoveryService> _logger;
    private readonly PeerRegistry _registry;

    public UdpDiscoveryService(
        ILogger<UdpDiscoveryService> logger,
        PeerRegistry registry,
        GlobalConfiguration config)
    {
        _logger = logger;
        _registry = registry;
        _discoveryPort = int.TryParse(config.DefaultDiscoveryPort, out var dp) ? dp : 7655;
        _transferPort = int.TryParse(config.DefaultListenPort, out var tp) ? tp : 7654;
        _discoveryIntervalSeconds = config.DiscoveryIntervalSeconds > 0 ? config.DiscoveryIntervalSeconds : 30;
        _username = string.IsNullOrWhiteSpace(config.Username) ? Environment.UserName : config.Username;
        _deviceName = string.IsNullOrWhiteSpace(config.DeviceName) ? Environment.MachineName : config.DeviceName;
        _trustMode = config.TrustMode;
        _autoAcceptPeers = config.AutoAcceptPeers;

        _registry.SetAllowedSubnets(config.AllowedSubnets);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Discovery starting: port={Port}, interval={Interval}s, trust={Trust}, autoAccept={AutoAccept}",
            _discoveryPort, _discoveryIntervalSeconds, _trustMode, _autoAcceptPeers);

        var listenerTask = StartListeningAsync(stoppingToken);
        var broadcasterTask = StartBroadcastingAsync(stoppingToken);

        await Task.WhenAll(listenerTask, broadcasterTask);
    }

    private async Task StartListeningAsync(CancellationToken ct)
    {
        using var receiver = new UdpClient();
        receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        receiver.Client.Bind(new IPEndPoint(IPAddress.Any, _discoveryPort));

        _logger.LogInformation("UDP Discovery Listener active on port {Port}", _discoveryPort);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await receiver.ReceiveAsync(ct);
                string json = Encoding.UTF8.GetString(result.Buffer);

                var beacon = JsonSerializer.Deserialize<PeerBeacon>(json);
                if (beacon == null) continue;
                if (beacon.NodeId == _nodeId) continue;

                string peerIp = result.RemoteEndPoint.Address.ToString();

                if (IsLocalAddress(peerIp)) continue;

                if (!_registry.IsSubnetAllowed(peerIp))
                {
                    _logger.LogDebug("Rejected peer {Ip} — not in allowed subnets", peerIp);
                    continue;
                }

                var trust = ResolveTrust(beacon.NodeId);

                var peerInfo = new PeerInfo(
                    NodeId: beacon.NodeId,
                    Username: beacon.Username,
                    DeviceName: beacon.DeviceName,
                    IpAddress: peerIp,
                    Port: beacon.TcpTransferPort,
                    LastSeen: DateTime.UtcNow,
                    Trust: trust
                );

                var accepted = _registry.RegisterPeer(peerInfo);

                if (!accepted)
                {
                    _logger.LogWarning(
                        "Peer {User} ({Device}) at {Ip} rejected — max peers ({Max}) reached",
                        beacon.Username, beacon.DeviceName, peerIp, _registry.Count);
                    continue;
                }

                _logger.LogInformation(
                    "Discovered peer: {User} ({Device}) at {Ip}:{Port} [Trust={Trust}]",
                    beacon.Username, beacon.DeviceName, peerIp, beacon.TcpTransferPort, trust);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error processing UDP packet: {Message}", ex.Message);
            }
        }
    }

    private async Task StartBroadcastingAsync(CancellationToken ct)
    {
        using var sender = new UdpClient();
        sender.EnableBroadcast = true;

        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, _discoveryPort);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var beacon = new PeerBeacon
                {
                    NodeId = _nodeId,
                    Username = _username,
                    DeviceName = _deviceName,
                    TcpTransferPort = _transferPort
                };

                byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(beacon));
                await sender.SendAsync(data, data.Length, broadcastEndpoint);

                await Task.Delay(TimeSpan.FromSeconds(_discoveryIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to broadcast UDP beacon: {Message}", ex.Message);
                await Task.Delay(5000, ct);
            }
        }
    }

    private static bool IsLocalAddress(string ip)
    {
        if (!IPAddress.TryParse(ip, out var address))
            return false;

        if (IPAddress.IsLoopback(address))
            return true;

        try
        {
            var localAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            return localAddresses.Any(a => a.Equals(address));
        }
        catch
        {
            return false;
        }
    }

    private PeerTrust ResolveTrust(string nodeId)
    {
        if (_autoAcceptPeers)
            return PeerTrust.Trusted;

        var existing = _registry.GetPeer(nodeId);
        if (existing != null)
            return existing.Trust;

        return _trustMode switch
        {
            "Auto" => PeerTrust.Trusted,
            "TrustOnFirstUse" => PeerTrust.Trusted,
            "Ask" => PeerTrust.Pending,
            _ => PeerTrust.Untrusted
        };
    }
}
