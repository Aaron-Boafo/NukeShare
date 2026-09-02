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

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.AddCommand<ConfigCommand>("config");
        });

        app.Run(args);
    }
}

    