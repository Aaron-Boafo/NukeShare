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
        var textPath = new TextPath(filePath);

        if (File.Exists(filePath))
        {
            AnsiConsole.MarkupLine($"[bold white]config already exist at[/] [green]{filePath}[/]");
            AnsiConsole.Write(textPath);
            return ;
        }

        await _globalProvider.SaveAsync(basePath);
        AnsiConsole.MarkupLine($"[bold white]successfully created NukeShare config[/] [green]{filePath} [/]");
        AnsiConsole.Write(textPath);
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
            _logger?.LogWarning("No options provided to update.");
            return;
        }

        if (string.IsNullOrEmpty(value))
        {
            _logger?.LogWarning("No options provided to update.");
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
                AnsiConsole.MarkupLine($"Property does not exist or can't be written");
                return;
            }

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var convertedValue = Convert.ChangeType(value, targetType);

            property.SetValue(update, convertedValue);
          
            await _globalProvider.SaveAsync(basePath, update);
            AnsiConsole.MarkupLine($"[green]✓[/] sucessfully updated [bold green]{key}[/] to [bold cyan]{value}[/] properties");

        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Update was cancelled");
            AnsiConsole.MarkupLine("[yellow]⚠[/] Operation cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to update global configuration: {ex.Message}");
            AnsiConsole.MarkupLine($"[red]✗[/] [bold]Error:[/] {ex.Message.EscapeMarkup()}");
        }
    }
}
