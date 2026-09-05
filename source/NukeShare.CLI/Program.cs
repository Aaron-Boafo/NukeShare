using Spectre.Console.Cli;
using Microsoft.Extensions.DependencyInjection;

using NukeShare.CLI.Commands;
using NukeShare.Configuration.Providers;
using NukeShare.Configuration.Service;
using NukeShare.Core.Logger;
using NukeShare.CLI.Infrastructure;

namespace NukeShare.CLI;

public class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddSingleton<GlobalProvider>();
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<ILogger, Logger>();
        services.AddSingleton<Logger>();

        services.AddSingleton<ConfigCommand>();
        services.AddSingleton<StartCommand>();
        services.AddSingleton<StopCommand>();

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("nuke");
            config.SetApplicationVersion("1.0.0");

            config.AddCommand<ConfigCommand>("config")
            .WithDescription("Manage global and local NukeShare settings")
            .WithExample(["config", "--init"])
            .WithExample(["config", "username", "\"Nuke User\""])
            .WithExample(["config", "-g", "DefaultListenPort", "7654"]);

            config.AddCommand<StartCommand>("start")
            .WithDescription("Start the P2P transfer daemon or listen for incoming peers")
            .WithAlias("run")
            .WithExample(["start"])
            .WithExample(["start", "--port", "7654"])
            .WithExample(["start", "--background"]);

            config.AddCommand<StopCommand>("stop")
            .WithDescription("Stops the P2P transfer daemon")
            .WithAlias("kill")
            .WithExample(["stop"]);

        });

        app.Run(args);
    }
}

    