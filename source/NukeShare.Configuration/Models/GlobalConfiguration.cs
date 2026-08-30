using System.Runtime.InteropServices;

namespace NukeShare.Configuration.Models
{
    public class GlobalConfiguration
    {
        public string Username { set; get;  } = Environment.UserName;
        public string DeviceName { set; get; } = Environment.MachineName;
        public string OperatingSystem { set; get; } = Environment.OSVersion.Platform.ToString();
        public string DefaultStoragePath { set; get; } = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads/NukeShare"
            );
        public bool AutoAcceptPeers { set; get; } = false;
        public string DefaultListenPort { set; get; } = "7654";
        public string DefaultDiscoveryPort { set; get; } = "7655";
        public int Chunks {  set; get; } = 4;
        public int ChunkSize { set; get; } = 2 * 1024 * 1024;
        public int ConcurrentPeers { set; get; } = 3;

    }

    
}
