namespace NukeShare.Network.Discovery;

public class PeerBeacon
{
    public string NodeId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public int TcpTransferPort { get; set; }
}
