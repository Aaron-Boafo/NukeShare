using System.Runtime.InteropServices;
using System.Text.Json;

namespace NukeShare.Configuration.Providers
{
    public interface IProviderConfig<T> where T : class
    {
        Task<T> LoadAsync(string filePath);
        Task<T> SaveAsync(string filePath, T? configModel = null);
    }

    public static class JsonOptions
    {
        public static JsonSerializerOptions Default = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public static class ConfigurationPathResolver
    {
        public const string AppName = "NukeShare";

        public static string GetConfigurationDirectory()
        {
            string basePath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        AppName
                    );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Application Support",
                    AppName
                );
            }
            else
            {
                string? xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                basePath = !string.IsNullOrWhiteSpace(xdgConfig)
                    ? Path.Combine(xdgConfig, AppName.ToLowerInvariant())
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", AppName.ToLowerInvariant());
            }

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            return basePath;
        }

        public static string GetLogPath()
        {
            string logDirectory = Path.Combine(GetConfigurationDirectory(), "tmp");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            return logDirectory;
        }
    }
}
