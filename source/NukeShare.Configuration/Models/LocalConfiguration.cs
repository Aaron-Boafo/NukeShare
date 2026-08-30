namespace NukeShare.Configuration.Models
{
    public class LocalConfiguration
    {
        public int? ConcurrentPeers { get; set; } = null;
        public string? DefaultStoragePath { set; get; } = null;
        public bool? AutoAcceptPeers { set; get; } = null;
        public int? Chunks { set; get; } = null;
        public int? ChunkSize { set; get; } = null;

        public static LocalConfiguration CreateDefaultTemplate(string DirectoryPath)
        {
            return new LocalConfiguration
            {
                ConcurrentPeers = 3,
                DefaultStoragePath = Path.Combine(DirectoryPath, "NukeShare/downloads"),
                AutoAcceptPeers = false,
                Chunks = 4,
                ChunkSize = 2 * 1024 * 1024   
            };
        }
    }
}
