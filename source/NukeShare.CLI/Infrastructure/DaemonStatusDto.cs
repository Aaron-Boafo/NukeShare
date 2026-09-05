namespace NukeShare.CLI.Infrastructure;

public record StatusDTO
(
    string Status,
    int ProcessId,
    string Machine,
    DateTime Timestamp,
    string Version,
    TimeSpan Uptime,
    double MemoryUsageMb
);

public record HealthDTO
(
    bool Healthy,
    HealthChecksDTO Checks
);

public record HealthChecksDTO
(
    string StorageDirectory,
    string DiscoveryListener,
    string TransferListener
);

public record PeerDTO
(
    string NodeId,
    string DeviceName,
    string IpAddress,
    int Port,
    string Trust,
    DateTime LastSeen
);

public record PeersDTO
(
    int TotalDiscovered,
    int Connected,
    int MaxPeers,
    PeerDTO[] Peers
);

public record TransferDTO
(
    Guid TransferId,
    string FileName,
    string Direction,
    long TotalBytes,
    long TransferredBytes,
    double ProgressPercentage,
    long TransferSpeedBytesPerSec,
    int ActiveChunks
);

public record TransfersDTO
(
    int ActiveCount,
    TransferDTO[] Transfers
);

public record ShutdownDTO
(
    string Message
);

public record ConfigDTO
(
    ConfigIdentityDTO Identity,
    ConfigNetworkDTO Network,
    ConfigSecurityDTO Security,
    ConfigTransferDTO Transfer
);

public record ConfigIdentityDTO(string Username, string DeviceName);

public record ConfigNetworkDTO(
    string ListenPort,
    string DiscoveryPort,
    int MaxPeers,
    int PeerTimeoutSeconds,
    int DiscoveryIntervalSeconds,
    bool UseMulticastDiscovery,
    string[] AllowedSubnets
);

public record ConfigSecurityDTO(
    bool EnableEncryption,
    string EncryptionCipher,
    string TrustMode,
    bool AutoAcceptUntrustedPeers
);

public record ConfigTransferDTO(
    bool AutoAcceptPeers,
    int Chunks,
    int ChunkSize,
    int ConcurrentPeers,
    long BandwidthLimitBytesPerSecond,
    long MaxFileSizeBytes
);

public record TrustRequestDTO(string Trust);

public record TrustResponseDTO(string NodeId, string Trust);

public record RemovePeerResponseDTO(string NodeId, bool Removed);
