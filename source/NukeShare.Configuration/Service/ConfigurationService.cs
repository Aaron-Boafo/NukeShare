using NukeShare.Configuration.Models;
using NukeShare.Configuration.Providers;
using NukeShare.Core.Logger;

using System.Reflection;
using Spectre.Console;

namespace NukeShare.Configuration.Service;

public class ConfigurationService(
    GlobalProvider _globalProvider,
    Logger _logger
)
{
    private readonly string basePath = ConfigurationPathResolver.GetConfigurationDirectory();

    public async Task InitializeGlobalConfig()
    {
        string filePath = Path.Combine(basePath, "config.json");
        bool existed = File.Exists(filePath);

        var config = await _globalProvider.LoadAsync(basePath);

        EnsureDirectory(config.DefaultStoragePath);
        EnsureDirectory(config.IncomingPath);
        EnsureDirectory(config.TempPath);
        EnsureDirectory(ConfigurationPathResolver.GetLogPath());

        await _globalProvider.SaveAsync(basePath, config);

        if (existed)
        {
            AnsiConsole.MarkupLine("[yellow]┌─[bold] Already initialized [/]─[/]");
            AnsiConsole.MarkupLine($"[yellow]│[/] A global configuration already exists at: [cyan]{filePath}[/]");
            AnsiConsole.MarkupLine("[yellow]│[/] [grey]Directories were verified and the configuration was refreshed with the latest settings.[/]");
            AnsiConsole.MarkupLine("[yellow]└─[/]");
            return;
        }

        AnsiConsole.MarkupLine("[green]┌─[bold] Config created [/]─[/]");
        AnsiConsole.MarkupLine($"[green]│[/] NukeShare is ready to use. Your global configuration has been created at: [cyan]{filePath}[/]");
        AnsiConsole.MarkupLine("[green]├─[/]");
        AnsiConsole.MarkupLine("[green]│[/] [grey]Storage, incoming, temp, and log directories were set up.[/]");
        AnsiConsole.MarkupLine("[green]│[/] [grey]Tip:[/] [white bold]nuke config --list[/] to review your settings.");
        AnsiConsole.MarkupLine("[green]└─[/]");
    }

    private static void EnsureDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Directory.CreateDirectory(path);
    }

    public async Task<GlobalConfiguration> LoadGlobalConfig()
    {
        return await _globalProvider.LoadAsync(basePath);
    }

    public async Task UpdateGlobalConfig(
        string key, string value,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger?.LogWarning("Configuration update failed: no key provided.");
            AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
            AnsiConsole.MarkupLine("[red]│[/] A configuration key is required.");
            AnsiConsole.MarkupLine("[red]├─[/]");
            AnsiConsole.MarkupLine("[red]│[/] [grey]Usage:[/] [white bold]nuke config <KEY> <VALUE>[/]");
            AnsiConsole.MarkupLine("[red]└─[/]");
            return;
        }

        if (string.IsNullOrEmpty(value))
        {
            _logger?.LogWarning($"Configuration update failed: no value provided for '{key}'.");
            AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
            AnsiConsole.MarkupLine($"[red]│[/] A value is required to set [white bold]{key.EscapeMarkup()}[/].");
            AnsiConsole.MarkupLine("[red]├─[/]");
            AnsiConsole.MarkupLine($"[red]│[/] [grey]Usage:[/] [white bold]nuke config {key.EscapeMarkup()} <VALUE>[/]");
            AnsiConsole.MarkupLine("[red]└─[/]");
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var update = await _globalProvider.LoadAsync(basePath);
            var property = update.GetType()
                .GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

            if (property == null || !property.CanWrite)
            {
                _logger?.LogWarning($"Cannot update '{key}': it does not exist or is read-only.");
                AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
                AnsiConsole.MarkupLine($"[red]│[/] [white bold]{key.EscapeMarkup()}[/] is not a valid configuration setting.");
                AnsiConsole.MarkupLine("[red]├─[/]");
                AnsiConsole.MarkupLine("[red]│[/] [grey]Run [white bold]nuke config --help[/] to list valid settings.[/]");
                AnsiConsole.MarkupLine("[red]└─[/]");
                return;
            }

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var convertedValue = Convert.ChangeType(value, targetType);

            property.SetValue(update, convertedValue);

            await _globalProvider.SaveAsync(basePath, update);
            AnsiConsole.MarkupLine("[green]┌─[bold] Updated [/]─[/]");
            AnsiConsole.MarkupLine($"[green]│[/] Set [white bold]{key.EscapeMarkup()}[/] to [bold cyan]{value.EscapeMarkup()}[/].");
            AnsiConsole.MarkupLine("[green]└─[/]");
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Configuration update cancelled.");
            AnsiConsole.MarkupLine("[yellow]┌─[bold] Cancelled [/]─[/]");
            AnsiConsole.MarkupLine("[yellow]│[/] The configuration update was cancelled.");
            AnsiConsole.MarkupLine("[yellow]└─[/]");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to update global configuration: {ex.Message}");
            AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
            AnsiConsole.MarkupLine($"[red]│[/] Could not update [white bold]{key.EscapeMarkup()}[/]: {ex.Message.EscapeMarkup()}");
            AnsiConsole.MarkupLine("[red]└─[/]");
        }
    }
}
