using NukeShare.Configuration.Models;
using System.Text.Json;

namespace NukeShare.Configuration.Providers
{
    public class GlobalProvider : IProviderConfig<GlobalConfiguration>
    {
        public async Task<GlobalConfiguration> SaveAsync(string basePath, GlobalConfiguration? configModel = null)
        {
            string configFilePath = Path.Combine(basePath, "config.json");
            GlobalConfiguration configuration = configModel ?? new GlobalConfiguration();

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            try
            {
                await using FileStream stream = File.Create(configFilePath);
                await JsonSerializer.SerializeAsync(stream, configuration, JsonOptions.Default);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return configuration;
        }

        public async Task<GlobalConfiguration> LoadAsync(string basePath)
        {
            string configFilePath = Path.Combine(basePath, "config.json");
            if (!File.Exists(configFilePath))
            {
                return await SaveAsync(basePath);
            }

            await using FileStream stream = File.OpenRead(configFilePath);
            return await JsonSerializer.DeserializeAsync<GlobalConfiguration>(stream, JsonOptions.Default) ?? new GlobalConfiguration();
        }
    }
}
