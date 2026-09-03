using System.ComponentModel;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

using NukeShare.CLI.UI;
using NukeShare.Configuration.Models;
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

        [CommandOption("-l|--list")]
        [Description("Display all available configuration keys")]
        public bool ListConfig { get; init; }

        [CommandArgument(0, "[KEY]")]
        [Description("Configuration key to read or update (e.g. username, port, concurrentpeers).")]
        public string? Key { get; init; }

        [CommandArgument(1, "[VALUE]")]
        [Description("Value to assign. If omitted, the current value for the key is printed.")]
        public string? Value { get; init; }

        public override ValidationResult Validate()
        {
            if ((Init || Version || ListConfig) && !string.IsNullOrWhiteSpace(Key))
                return ValidationResult.Error("Cannot specify a configuration key when using --init, --version, or --list.");

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

        if (settings.ListConfig)
        {
            var currentConfig = await configService.LoadGlobalConfig();

            var properties = typeof(GlobalConfiguration)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite)
                .OrderBy(p => p.Name);

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[cyan]Available Configuration Keys[/]")
                .AddColumn(new TableColumn("[bold]Key[/]").LeftAligned().NoWrap().Width(29))
                .AddColumn(new TableColumn("[bold]Type[/]").LeftAligned().Width(10))
                .AddColumn(new TableColumn("[bold]Description[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Current[/]").LeftAligned().Width(24));

            foreach (var property in properties)
            {
                var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
                var value = property.GetValue(currentConfig);
                var displayValue = FormatValue(value);

                table.AddRow(
                    $"[white bold]{property.Name.EscapeMarkup()}[/]",
                    $"[grey]{CSharpTypeName(property.PropertyType).EscapeMarkup()}[/]",
                    $"[grey]{description.EscapeMarkup()}[/]",
                    $"[green]{displayValue.EscapeMarkup()}[/]"
                );
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("[grey]Manage these settings using:[/] [white bold]nuke config <KEY> <VALUE>[/]");
            AnsiConsole.WriteLine();

            return 0;
        }

        if (!string.IsNullOrWhiteSpace(settings.Key))
        {
            if (!string.IsNullOrEmpty(settings.Value))
            {
                await configService.UpdateGlobalConfig(settings.Key, settings.Value, cancellationToken);
                return 0;
            }

            AnsiConsole.MarkupLine("[yellow]┌─[bold] Value required [/]─[/]");
            AnsiConsole.MarkupLine($"[yellow]│[/] You provided a key but no value for [white bold]{settings.Key.EscapeMarkup()}[/].");
            AnsiConsole.MarkupLine("[yellow]├─[/]");
            AnsiConsole.MarkupLine($"[yellow]│[/] [grey]To set it, run:[/] [white bold]nuke config {settings.Key.EscapeMarkup()} <VALUE>[/]");
            AnsiConsole.MarkupLine("[yellow]└─[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
        AnsiConsole.MarkupLine("[red]│[/] No valid command or option provided.");
        AnsiConsole.MarkupLine("[red]├─[/]");
        AnsiConsole.MarkupLine("[red]│[/] [grey]Run [white bold]nuke config --help[/] to see all options.[/]");
        AnsiConsole.MarkupLine("[red]└─[/]");

        return 1;
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "(null)";
        }

        if (value is string[] array)
        {
            return array.Length == 0 ? "(empty)" : string.Join(", ", array);
        }

        return value.ToString() ?? "(null)";
    }

    private static string CSharpTypeName(Type type)
    {
        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(long))
        {
            return "long";
        }

        if (type == typeof(string[]))
        {
            return "string[]";
        }

        return type.Name;
    }
}