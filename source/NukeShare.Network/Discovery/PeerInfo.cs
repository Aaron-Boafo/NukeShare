namespace NukeShare.Network.Discovery;

public enum PeerTrust
{
    Untrusted,
    Pending,
    Trusted
}

public record PeerInfo(
    string NodeId,
    string Username,
    string DeviceName,
    string IpAddress,
    int Port,
    DateTime LastSeen,
    PeerTrust Trust = PeerTrust.Untrusted
);
