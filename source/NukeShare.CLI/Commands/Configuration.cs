using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

using NukeShare.CLI.UI;
using NukeShare.Configuration.Service;

namespace NukeShare.CLI.Commands;

public class ConfigCommand(ConfigurationService configService) : AsyncCommand<ConfigCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-i|--init")]
        [Description("Initialize the NukeShare configuration directory.")]
        public bool Init { get; init; }

        [CommandOption("-v|--version")]
        [Description("Display the version of the current build release.")]
        public bool Version { get; init; }

        [CommandArgument(0, "[KEY]")]
        [Description("Configuration key to read or update (e.g. username, port, concurrentpeers).")]
        public string? Key { get; init; }

        [CommandArgument(1, "[VALUE]")]
        [Description("Value to assign. If omitted, the current value for the key is printed.")]
        public string? Value { get; init; }

        public override ValidationResult Validate()
        {
            if ((Init || Version) && !string.IsNullOrWhiteSpace(Key))
                return ValidationResult.Error("Cannot specify a configuration key when using --init or --version.");

            return ValidationResult.Success();
        }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Init)
        {
            await configService.InitializeGlobalConfig();
            return 0;
        }

        if (settings.Version)
        {
            BannerBadge.RenderBadge();
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(settings.Key))
        {
            if (!string.IsNullOrEmpty(settings.Value))
            {
                await configService.UpdateGlobalConfig(settings.Key, settings.Value, cancellationToken);
                return 0;
            }

            return 1;
        }

        // Fallback error panel
        AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
        AnsiConsole.MarkupLine("[red]│[/] No valid command or option provided.");
        AnsiConsole.MarkupLine("[red]├─[/]");
        AnsiConsole.MarkupLine("[red]│[/] [grey]Check out [white bold]nuke config --help[/] for more information.[/]");
        AnsiConsole.MarkupLine("[red]└─[/]");

        return 1;
    }
}