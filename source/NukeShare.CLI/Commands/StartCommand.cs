using NukeShare.CLI.Infrastructure;
using NukeShare.Configuration.Models;
using NukeShare.Configuration.Service;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace NukeShare.CLI.Commands;

public class StartCommand(ConfigurationService _configService) : AsyncCommand<StartCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-p|--port <PORT>")]
        [Description("Port number to run nuked daemon (use default if omitted)")]
        public string? Args { get; set; }

        [CommandOption("-b|--background")]
        [Description("Run Daemon on background for persistency")]
        public bool Background { get; set; }

    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var option = new DaemonOption
        {
            ListeningPort = await GetDefaultPort()
        };


        if (!string.IsNullOrEmpty(settings.Args))
        {
            if (!ushort.TryParse(settings.Args, out var port) || port == 0)
            {
                AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
                AnsiConsole.MarkupLine($"[red]│[/] [white bold]{settings.Args.EscapeMarkup()}[/] is not a valid port number.");
                AnsiConsole.MarkupLine("[red]├─[/]");
                AnsiConsole.MarkupLine("[red]│[/] [grey]Port must be a number between [/][white bold]1[/][grey] and [/][white bold]65535[/][grey].[/]");
                AnsiConsole.MarkupLine("[red]└─[/]");
                return 1;
            }

            option.ListeningPort = settings.Args;
            AnsiConsole.MarkupLine("[green]┌─[bold] Port selected [/]─[/]");
            AnsiConsole.MarkupLine($"[green]│[/] Using provided port [bold cyan]{settings.Args.EscapeMarkup()}[/] for the daemon.");
            AnsiConsole.MarkupLine("[green]└─[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[grey]Using default port [white bold]{option.ListeningPort.EscapeMarkup()}[/].[/]");
        }

        if (settings.Background)
        {
            option.Background = true;
            AnsiConsole.MarkupLine("[yellow]┌─[bold] Background mode [/]─[/]");
            AnsiConsole.MarkupLine("[yellow]│[/] The daemon will run in the background for persistency.");
            AnsiConsole.MarkupLine("[yellow]└─[/]");
        }

        return AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("orange1"))
            .Start("Starting NukeShare daemon...", ctx =>
            {
                ctx.Status("Checking existing instances...");
                if (DaemonProcessLauncher.IsDaemonRunning())
                {
                    AnsiConsole.MarkupLine("[yellow]┌─[bold] Already running [/]─[/]");
                    AnsiConsole.MarkupLine("[yellow]│[/] A NukeShare daemon instance is already active.");
                    AnsiConsole.MarkupLine("[yellow]├─[/]");
                    AnsiConsole.MarkupLine("[yellow]│[/] [grey]Use [white bold]nuke stop[/] to terminate it first.[/]");
                    AnsiConsole.MarkupLine("[yellow]└─[/]");
                    return 0;
                }

                ctx.Status("Launching background process...");
                DaemonProcessLauncher.RunDaemon(option.Background, option.ListeningPort ?? "7654");

                AnsiConsole.MarkupLine("[green]┌─[bold] Daemon started [/]─[/]");
                AnsiConsole.MarkupLine($"[green]│[/] Listening on [bold cyan]http://127.0.0.1:{option.ListeningPort.EscapeMarkup()}[/].");
                AnsiConsole.MarkupLine("[green]├─[/]");
                AnsiConsole.MarkupLine("[green]│[/] [grey]Run [white bold]nuke stop[/] to terminate the daemon.[/]");
                AnsiConsole.MarkupLine("[green]└─[/]");
                return 0;
            });
    }

    public async Task<string> GetDefaultPort()
    {
        GlobalConfiguration config = await _configService.LoadGlobalConfig();
        return config.DefaultListenPort;
    }

    private class DaemonOption
    {
        public string? ListeningPort { get ; set; }
        public bool Background { get; set; } = false;
    }
}
