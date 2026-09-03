using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NukeShare.Configuration.Models
{
    public class GlobalConfiguration
    {
        // Identity
        [Description("Peer identification name shown to others on the network.")]
        public string Username { set; get; } = Environment.UserName;

        [Description("Machine name broadcasted during local network discovery.")]
        public string DeviceName { set; get; } = Environment.MachineName;

        [Description("Operating system platform this device is running on.")]
        public string OperatingSystem { set; get; } = Environment.OSVersion.Platform.ToString();

        // Storage / Files
        [Description("Root directory where NukeShare stores its data.")]
        public string DefaultStoragePath { set; get; } = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "NukeShare"
            );

        [Description("Directory where received files are saved.")]
        public string IncomingPath { set; get; } = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "NukeShare", "Incoming"
            );

        [Description("Temporary directory used while transfers are in progress.")]
        public string TempPath { set; get; } = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "NukeShare", "Temp"
            );

        [Description("Directories published to peers for sharing (comma-separated).")]
        public string[] SharedDirectories { set; get; } = new string[] { };

        [Description("Maximum file size in bytes accepted for transfer.")]
        public long MaxFileSizeBytes { set; get; } = 10L * 1024 * 1024 * 1024;

        [Description("Overwrite files that already exist in the destination (true/false).")]
        public bool OverwriteExisting { set; get; } = false;

        // Network / Discovery
        [Description("TCP port used for direct chunk streaming and transfers.")]
        public string DefaultListenPort { set; get; } = "7654";

        [Description("UDP port used for local network discovery.")]
        public string DefaultDiscoveryPort { set; get; } = "7655";

        [Description("Hard limit on simultaneous peer connections.")]
        public int MaxPeers { set; get; } = 10;

        [Description("Seconds without activity before a peer is considered offline.")]
        public int PeerTimeoutSeconds { set; get; } = 60;

        [Description("Seconds between discovery broadcasts.")]
        public int DiscoveryIntervalSeconds { set; get; } = 30;

        [Description("Use multicast/broadcast discovery instead of manual peer entry (true/false).")]
        public bool UseMulticastDiscovery { set; get; } = false;

        [Description("Allowed subnets for discovery (comma-separated CIDR ranges).")]
        public string[] AllowedSubnets { set; get; } = new string[] { };

        // Security / Encryption
        [Description("Encrypt all transfers end-to-end (true/false).")]
        public bool EnableEncryption { set; get; } = true;

        [Description("Cipher used for encryption (e.g. AES-256-GCM).")]
        public string EncryptionCipher { set; get; } = "AES-256-GCM";

        [Description("Peer trust mode: Auto, Ask, or TrustOnFirstUse.")]
        public string TrustMode { set; get; } = "Ask";

        [Description("Auto-accept transfers from untrusted peers (true/false).")]
        public bool AutoAcceptUntrustedPeers { set; get; } = false;

        // Transfer / Throttling
        [Description("Automatically accept incoming transfers without prompt (true/false).")]
        public bool AutoAcceptPeers { set; get; } = false;

        [Description("Number of concurrent chunks processed per file.")]
        public int Chunks { set; get; } = 4;

        [Description("Size in bytes for each transmission chunk.")]
        public int ChunkSize { set; get; } = 2 * 1024 * 1024;

        [Description("Maximum peers used per transfer.")]
        public int ConcurrentPeers { set; get; } = 3;

        [Description("Global bandwidth limit in bytes per second (0 = unlimited).")]
        public long BandwidthLimitBytesPerSecond { set; get; } = 0;

        [Description("Number of retries for a failed chunk.")]
        public int ChunkRetryCount { set; get; } = 3;

        [Description("Timeout in milliseconds for a chunk request.")]
        public int ChunkRequestTimeoutMs { set; get; } = 30_000;

        [Description("Resume interrupted transfers from where they stopped (true/false).")]
        public bool ResumeInterruptedTransfers { set; get; } = true;

        // Daemon / API
        [Description("Expose a local HTTP API for the daemon (true/false).")]
        public bool EnableApi { set; get; } = false;

        [Description("Port used by the local HTTP API.")]
        public int ApiPort { set; get; } = 7656;
    }
}
