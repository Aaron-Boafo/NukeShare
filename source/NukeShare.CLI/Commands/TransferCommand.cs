using System.ComponentModel;
using NukeShare.CLI.Infrastructure;
using NukeShare.Configuration.Service;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NukeShare.CLI.Commands;

public class TransferCommand(ConfigurationService _configService) : AsyncCommand<TransferCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[filePath]")]
        [Description("Path to the file to send to a peer")]
        public string? FilePath { get; init; }

        [CommandOption("--node")]
        [Description("Target peer NodeId to send to")]
        public string? NodeId { get; init; }

        [CommandOption("--cancel")]
        [Description("Cancel an in-flight transfer by TransferId")]
        public string? CancelId { get; init; }

        [CommandOption("--recent")]
        [Description("Show recently completed, failed, or cancelled transfers")]
        public bool Recent { get; init; }

        [CommandOption("--watch")]
        [Description("Send a file and display live progress until it completes")]
        public bool Watch { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var api = CreateApis();

        if (!string.IsNullOrEmpty(settings.CancelId))
            return await ExecuteCancel(api, settings.CancelId, cancellationToken);

        if (settings.Recent)
            return await ExecuteRecent(api, cancellationToken);

        if (!string.IsNullOrEmpty(settings.FilePath))
            return await ExecuteSend(api, settings, cancellationToken);

        return await ExecuteList(api, cancellationToken);
    }

    private async Task<int> ExecuteSend(ApiClients api, Settings settings, CancellationToken ct)
    {
        var filePath = Path.GetFullPath(settings.FilePath!);
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            AnsiConsole.MarkupLine($"[red]File not found:[/] [white]{filePath.EscapeMarkup()}[/]");
            return 1;
        }

        string? nodeId = string.IsNullOrWhiteSpace(settings.NodeId)
            ? await SelectPeerAsync(api, ct)
            : settings.NodeId;

        if (string.IsNullOrWhiteSpace(nodeId))
            return 0;

        AnsiConsole.MarkupLine($"[grey]Starting transfer of[/] [white]{fileInfo.Name.EscapeMarkup()}[/] [grey]to peer[/] [cyan]{nodeId.EscapeMarkup()}[/]...");
        AnsiConsole.WriteLine();

        var result = await api.Transfer.SendAsync(nodeId, filePath, ct);

        if (result.Error is not null)
        {
            AnsiConsole.MarkupLine("[red]┌─[bold] Transfer failed [/]─[/]");
            AnsiConsole.MarkupLine($"[red]│[/] {result.Error.EscapeMarkup()}");
            AnsiConsole.MarkupLine("[red]└─[/]");
            return 1;
        }

        if (result.Transfer is null)
        {
            RenderOffline();
            return 1;
        }

        RenderAccepted(result.Transfer);

        if (settings.Watch)
            return await WatchAsync(api, result.Transfer, ct);

        return 0;
    }

    private async Task<string?> SelectPeerAsync(ApiClients api, CancellationToken ct)
    {
        var peers = await api.Status.GetPeersAsync(ct);
        if (peers is null)
        {
            RenderOffline();
            return null;
        }

        if (peers.Peers.Length == 0)
        {
            AnsiConsole.MarkupLine("[grey]No peers discovered yet. Ensure the daemon is running and run [white bold]nuke status --peers[/] to check.[/]");
            return null;
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<PeerDTO>()
                .Title("[cyan]Select a peer to send to:[/]")
                .PageSize(10)
                .UseConverter(p => $"{p.NodeId}  [grey]{p.DeviceName.EscapeMarkup()}  {p.IpAddress.EscapeMarkup()}[/]  [{TrustColor(p.Trust)}]{p.Trust.EscapeMarkup()}[/]")
                .AddChoices(peers.Peers))
            .NodeId;
    }

    private async Task<int> WatchAsync(ApiClients api, TransferDetailDTO transfer, CancellationToken ct)
    {
        AnsiConsole.WriteLine();
        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[cyan]{transfer.FileName.EscapeMarkup()}[/]");
                task.MaxValue = 100;

                while (true)
                {
                    var dto = await api.Transfer.GetAsync(transfer.TransferId, ct);
                    if (dto is null)
                    {
                        task.Description = "[red]Daemon unreachable while watching transfer[/]";
                        break;
                    }

                    task.Value = dto.ProgressPercentage;
                    task.Description = $"[cyan]{dto.FileName.EscapeMarkup()}[/] [grey]{FormatBytesPerSec(dto.TransferSpeedBytesPerSec)}[/]";

                    if (dto.State is not ("Queued" or "Transferring"))
                    {
                        FinishProgress(task, dto);
                        break;
                    }

                    await Task.Delay(500, ct);
                }
            });

        AnsiConsole.WriteLine();
        return 0;
    }

    private static void FinishProgress(ProgressTask task, TransferDetailDTO dto)
    {
        switch (dto.State)
        {
            case "Completed":
                task.Description = $"[green]┌─[bold] {dto.FileName.EscapeMarkup()} completed [/]─[/]";
                task.Value = 100;
                break;
            case "Cancelled":
                task.Description = $"[yellow]┌─[bold] {dto.FileName.EscapeMarkup()} cancelled [/]─[/]";
                break;
            case "Failed":
                task.Description = $"[red]┌─[bold] {dto.FileName.EscapeMarkup()} failed [/]─[/]";
                break;
            default:
                task.Description = $"[grey]{dto.State.EscapeMarkup()}[/]";
                break;
        }
    }

    private void RenderAccepted(TransferDetailDTO transfer)
    {
        AnsiConsole.MarkupLine("[green]┌─[bold] Transfer accepted [/]─[/]");
        AnsiConsole.MarkupLine($"[green]│[/] [grey]Transfer ID:[/] [white]{transfer.TransferId}[/]");
        AnsiConsole.MarkupLine($"[green]│[/] [grey]File:[/] [white]{transfer.FileName.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"[green]│[/] [grey]Direction:[/] [cyan]{transfer.Direction.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"[green]│[/] [grey]Size:[/] [white]{FormatBytes(transfer.TotalBytes)}[/]");
        AnsiConsole.MarkupLine($"[green]│[/] [grey]State:[/] [{StateColor(transfer.State)}]{transfer.State.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[green]└─[/]");
        AnsiConsole.MarkupLine("[grey]Run [/][white bold]nuke transfer --watch[/][grey] to track progress, or [/][white bold]nuke transfer --list[/][grey] to view all active transfers.[/]");
        AnsiConsole.WriteLine();
    }

    private async Task<int> ExecuteCancel(ApiClients api, string cancelId, CancellationToken ct)
    {
        if (!Guid.TryParse(cancelId, out var transferId))
        {
            AnsiConsole.MarkupLine($"[red]Invalid transfer id:[/] [white]{cancelId.EscapeMarkup()}[/]");
            return 1;
        }

        var result = await api.Transfer.CancelAsync(transferId, ct);
        if (result is null)
        {
            RenderOffline();
            return 1;
        }

        if (result.Error is not null)
        {
            AnsiConsole.MarkupLine("[yellow]┌─[bold] Cancel failed [/]─[/]");
            AnsiConsole.MarkupLine($"[yellow]│[/] {result.Error.EscapeMarkup()}");
            AnsiConsole.MarkupLine("[yellow]└─[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("[green]┌─[bold] Cancelled [/]─[/]");
        AnsiConsole.MarkupLine($"[green]│[/] {result.Message?.EscapeMarkup()}");
        AnsiConsole.MarkupLine("[green]└─[/]");
        return 0;
    }

    private async Task<int> ExecuteList(ApiClients api, CancellationToken ct)
    {
        var transfers = await api.Transfer.GetActiveAsync(ct);
        if (transfers is null)
        {
            RenderOffline();
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[cyan]Active Transfers:[/] [bold white]{transfers.ActiveCount}[/]");
        AnsiConsole.WriteLine();

        if (transfers.Transfers.Length == 0)
        {
            AnsiConsole.MarkupLine("[grey]No active transfers.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        RenderTransferTable(transfers.Transfers);
        return 0;
    }

    private async Task<int> ExecuteRecent(ApiClients api, CancellationToken ct)
    {
        var history = await api.Transfer.GetRecentAsync(ct);
        if (history is null)
        {
            RenderOffline();
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[cyan]Recent Transfers:[/]");
        AnsiConsole.WriteLine();

        if (history.Transfers.Length == 0)
        {
            AnsiConsole.MarkupLine("[grey]No transfers recorded yet.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        RenderTransferTable(history.Transfers);
        return 0;
    }

    private static void RenderTransferTable(TransferDetailDTO[] transfers)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[cyan]Transfers[/]")
            .AddColumn(new TableColumn("[bold]File[/]").LeftAligned().NoWrap().Width(20))
            .AddColumn(new TableColumn("[bold]Direction[/]").LeftAligned().NoWrap().Width(12))
            .AddColumn(new TableColumn("[bold]Progress[/]").LeftAligned().NoWrap().Width(12))
            .AddColumn(new TableColumn("[bold]Speed[/]").LeftAligned().NoWrap().Width(14))
            .AddColumn(new TableColumn("[bold]State[/]").LeftAligned().NoWrap().Width(12));

        foreach (var t in transfers)
        {
            var dirColor = t.Direction == "Receiving" ? "cyan" : "green";
            table.AddRow(
                $"[white]{t.FileName.EscapeMarkup()}[/]",
                $"[{dirColor}]{t.Direction.EscapeMarkup()}[/]",
                $"[white]{t.ProgressPercentage:F1}%[/]",
                $"[white]{FormatBytesPerSec(t.TransferSpeedBytesPerSec)}[/]",
                $"[{StateColor(t.State)}]{t.State.EscapeMarkup()}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static string StateColor(string state) => state switch
    {
        "Completed" or "Transferring" => "green",
        "Queued" => "yellow",
        "Cancelled" => "yellow",
        "Failed" => "red",
        _ => "white"
    };

    private static string TrustColor(string trust) => trust switch
    {
        "Trusted" => "green",
        "Pending" => "yellow",
        _ => "red"
    };

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }

    private static string FormatBytesPerSec(long bytesPerSec)
    {
        if (bytesPerSec >= 1024 * 1024)
            return $"{bytesPerSec / (1024.0 * 1024.0):F1} MB/s";
        if (bytesPerSec >= 1024)
            return $"{bytesPerSec / 1024.0:F1} KB/s";
        return $"{bytesPerSec} B/s";
    }

    private ApiClients CreateApis()
    {
        var port = _configService.LoadGlobalConfig().GetAwaiter().GetResult().DefaultListenPort;
        var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromSeconds(5) };
        return new ApiClients(new StatusRestApi(http), new TransferRestApi(http));
    }

    private record ApiClients(StatusRestApi Status, TransferRestApi Transfer);

    private static void RenderOffline()
    {
        AnsiConsole.MarkupLine("[yellow]┌─[bold] Daemon offline [/]─[/]");
        AnsiConsole.MarkupLine("[yellow]│[/] Could not connect to the NukeShare daemon.");
        AnsiConsole.MarkupLine("[yellow]├─[/]");
        AnsiConsole.MarkupLine("[yellow]│[/] [grey]Ensure the daemon is running with [white bold]nuke start[/].[/]");
        AnsiConsole.MarkupLine("[yellow]└─[/]");
    }
}