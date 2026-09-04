using Scalar.AspNetCore;
using NukeShare.Configuration.Providers;
using NukeShare.Configuration.Service;
using NukeShare.Configuration.Models;
using NukeShare.Network.Discovery;

namespace NukeShare.Daemon;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();
        builder.Services.AddControllers();

        builder.Services.AddSingleton<GlobalProvider>();
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

        builder.Services.AddSingleton(sp =>
        {
            var configService = sp.GetRequiredService<IConfigurationService>();
            return configService.LoadGlobalConfig().GetAwaiter().GetResult();
        });

        builder.Services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<GlobalConfiguration>();
            var maxPeers = config.MaxPeers > 0 ? config.MaxPeers : 10;
            var peerTimeout = config.PeerTimeoutSeconds > 0 ? config.PeerTimeoutSeconds : 60;
            return new PeerRegistry(maxPeers, peerTimeout);
        });

        builder.Services.AddHostedService(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<UdpDiscoveryService>();
            var registry = sp.GetRequiredService<PeerRegistry>();
            var config = sp.GetRequiredService<GlobalConfiguration>();
            return new UdpDiscoveryService(logger, registry, config);
        });

        var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? $"http://127.0.0.1:7654";

        builder.WebHost.UseUrls(urls);

        if (args.Contains("--background"))
        {
            builder.Logging.ClearProviders();
        }

        var app = builder.Build();

        if (app.Environment.IsDevelopment() || args.Contains("--interactive"))
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapControllers();
        app.Run();
    }
}
