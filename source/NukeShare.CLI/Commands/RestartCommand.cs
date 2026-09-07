using NukeShare.CLI.Infrastructure;
using NukeShare.Configuration.Service;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace NukeShare.CLI.Commands;

[Description("Restart Nuke Daemon")]
public class RestartCommand(ConfigurationService _configService) : AsyncCommand<RestartCommand.Settings>
{
    public class Settings : CommandSettings 
    {
        [CommandOption("-b|--background")]
        [Description("Run Nuke Daemon in background task for persistency")]
        public bool Background { get; set; }

        [CommandOption("-p|--port")]
        [Description("Port for the Nuke Daemon to listen on")]
        public string? ListeningPort { get; set; }

    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("Preparing to RESTART Daemon...\n");
        var stopResults = DaemonProcessLauncher.StopDaemon();
        if (stopResults != 0)
            return 1;

        await Task.Delay(500, cancellationToken);

        await StartCommand.ProceedStart(
            settings.Background,
            string.IsNullOrWhiteSpace(settings.ListeningPort)?
                await DaemonProcessLauncher.GetDefaultPort(_configService)
                : settings.ListeningPort
         );

        return 0;
    }

}
