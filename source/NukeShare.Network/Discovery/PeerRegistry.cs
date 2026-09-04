using System.Collections.Concurrent;
using System.Net;

namespace NukeShare.Network.Discovery;

public class PeerRegistry
{
    private readonly ConcurrentDictionary<string, PeerInfo> _peers = new();
    private readonly int _maxPeers;
    private readonly int _peerTimeoutSeconds;

    public PeerRegistry(int maxPeers = 10, int peerTimeoutSeconds = 60)
    {
        _maxPeers = maxPeers;
        _peerTimeoutSeconds = peerTimeoutSeconds;
    }

    public bool RegisterPeer(PeerInfo peer)
    {
        if (_peers.ContainsKey(peer.NodeId))
        {
            var existing = _peers[peer.NodeId];
            var updated = existing with
            {
                LastSeen = peer.LastSeen,
                IpAddress = peer.IpAddress,
                Port = peer.Port
            };
            _peers[peer.NodeId] = updated;
            return true;
        }

        if (_peers.Count >= _maxPeers)
            return false;

        _peers[peer.NodeId] = peer;
        return true;
    }

    public void SetTrust(string nodeId, PeerTrust trust)
    {
        if (!_peers.ContainsKey(nodeId)) return;
        var existing = _peers[nodeId];
        _peers[nodeId] = existing with { Trust = trust };
    }

    public PeerInfo? GetPeer(string nodeId)
    {
        return _peers.TryGetValue(nodeId, out var peer) ? peer : null;
    }

    public void RemovePeer(string nodeId)
    {
        _peers.TryRemove(nodeId, out _);
    }

    public List<PeerInfo> GetActivePeers()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromSeconds(_peerTimeoutSeconds);
        return _peers.Values
            .Where(p => p.LastSeen >= cutoff)
            .OrderByDescending(p => p.LastSeen)
            .ToList();
    }

    public List<PeerInfo> GetAll()
    {
        return _peers.Values.OrderByDescending(p => p.LastSeen).ToList();
    }

    public bool IsSubnetAllowed(string ipAddress)
    {
        if (_allowedSubnets.Length == 0)
            return true;

        if (!IPAddress.TryParse(ipAddress, out var address))
            return false;

        return _allowedSubnets.Any(cidr => IsInSubnet(address, cidr));
    }

    private string[] _allowedSubnets = [];

    public void SetAllowedSubnets(string[] subnets)
    {
        _allowedSubnets = subnets;
    }

    public int Count => _peers.Count;

    private static bool IsInSubnet(IPAddress address, string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2) return false;
        if (!IPAddress.TryParse(parts[0], out var network)) return false;
        if (!int.TryParse(parts[1], out var prefixLength)) return false;

        var addrBytes = address.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();

        if (addrBytes.Length != networkBytes.Length) return false;

        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;

        for (int i = 0; i < fullBytes; i++)
        {
            if (addrBytes[i] != networkBytes[i]) return false;
        }

        if (remainingBits > 0 && fullBytes < addrBytes.Length)
        {
            byte mask = (byte)(0xFF << (8 - remainingBits));
            if ((addrBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
                return false;
        }

        return true;
    }
}
