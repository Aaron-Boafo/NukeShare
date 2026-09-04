using System.ComponentModel;
using NukeShare.CLI.Infrastructure;
using NukeShare.Configuration.Service;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NukeShare.CLI.Commands;

public class PeersCommand(ConfigurationService _configService) : AsyncCommand<PeersCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--approve")]
        [Description("Approve pending peers (interactive checklist)")]
        public bool Approve { get; init; }

        [CommandOption("--reject")]
        [Description("Reject pending peers (interactive checklist)")]
        public bool Reject { get; init; }

        [CommandOption("--node")]
        [Description("Target a specific NodeId (use with --approve or --reject)")]
        public string? NodeId { get; init; }

        [CommandOption("--remove")]
        [Description("Remove a peer from the registry by NodeId")]
        public string? Remove { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var api = CreateApi();

        if (!string.IsNullOrEmpty(settings.Remove))
            return await ExecuteRemove(api, settings.Remove, cancellationToken);

        if (settings.Approve)
            return await ExecuteTrustBulk(api, "Trusted", settings.NodeId ?? "", cancellationToken);

        if (settings.Reject)
            return await ExecuteTrustBulk(api, "Untrusted", settings.NodeId ?? "", cancellationToken);

        return await ExecuteList(api, cancellationToken);
    }

    private async Task<int> ExecuteList(StatusRestApi api, CancellationToken ct)
    {
        var peers = await api.GetPeersAsync(ct);
        if (peers is null)
        {
            RenderOffline();
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[cyan]Discovered:[/] [white]{peers.TotalDiscovered}[/]  [cyan]Connected:[/] [bold green]{peers.Connected}[/]  [cyan]Max:[/] [white]{peers.MaxPeers}[/]");
        AnsiConsole.WriteLine();

        if (peers.Peers.Length == 0)
        {
            AnsiConsole.MarkupLine("[grey]No peers found.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[cyan]Peers[/]")
            .AddColumn(new TableColumn("[bold]Node ID[/]").LeftAligned().NoWrap().Width(16))
            .AddColumn(new TableColumn("[bold]Device[/]").LeftAligned().NoWrap().Width(22))
            .AddColumn(new TableColumn("[bold]Address[/]").LeftAligned().NoWrap().Width(22))
            .AddColumn(new TableColumn("[bold]Port[/]").LeftAligned().NoWrap().Width(8))
            .AddColumn(new TableColumn("[bold]Trust[/]").LeftAligned().Width(12));

        foreach (var peer in peers.Peers)
        {
            var trustColor = peer.Trust switch
            {
                "Trusted" => "green",
                "Pending" => "yellow",
                _ => "red"
            };
            table.AddRow(
                $"[grey]{peer.NodeId.EscapeMarkup()}[/]",
                $"[white]{peer.DeviceName.EscapeMarkup()}[/]",
                $"[white]{peer.IpAddress.EscapeMarkup()}[/]",
                $"[white]{peer.Port}[/]",
                $"[bold {trustColor}]{peer.Trust.EscapeMarkup()}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        return 0;
    }

    private async Task<int> ExecuteTrustBulk(StatusRestApi api, string trust, string nodeIdArg, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(nodeIdArg))
            return await ExecuteTrustSingle(api, trust, nodeIdArg, ct);

        var peers = await api.GetPeersAsync(ct);
        if (peers is null)
        {
            RenderOffline();
            return 1;
        }

        var pending = peers.Peers.Where(p => p.Trust == "Pending").ToArray();
        if (pending.Length == 0)
        {
            AnsiConsole.MarkupLine("[grey]No pending peers to " + trust.ToLowerInvariant() + ".[/]");
            return 0;
        }

        var options = pending.Select(p =>
            $"{p.NodeId}  [grey]{p.DeviceName.EscapeMarkup()}  {p.IpAddress.EscapeMarkup()}[/]").ToList();

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title($"[cyan]Select peers to {trust.ToLowerInvariant()}:[/]")
                .PageSize(10)
                .Required(true)
                .AddChoices(options));

        int success = 0;
        foreach (var choice in selected)
        {
            var peerNodeId = choice.Split("  ")[0].Trim();
            var result = await api.SetPeerTrustAsync(peerNodeId, trust, ct);
            if (result is not null)
            {
                var color = trust == "Trusted" ? "green" : "red";
                AnsiConsole.MarkupLine($"[bold {color}]{result.NodeId}[/] is now [{color}]{trust}[/].");
                success++;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to set {peerNodeId} to {trust}.[/]");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Updated {success} peer(s) to {trust}.[/]");
        AnsiConsole.WriteLine();
        return 0;
    }

    private async Task<int> ExecuteTrustSingle(StatusRestApi api, string trust, string nodeId, CancellationToken ct)
    {
        var result = await api.SetPeerTrustAsync(nodeId, trust, ct);
        if (result is null)
        {
            RenderOffline();
            return 1;
        }

        var color = trust == "Trusted" ? "green" : "red";
        AnsiConsole.MarkupLine($"[bold {color}]{result.NodeId}[/] is now [{color}]{trust}[/].");
        return 0;
    }

    private async Task<int> ExecuteRemove(StatusRestApi api, string nodeId, CancellationToken ct)
    {
        var result = await api.RemovePeerAsync(nodeId, ct);
        if (result is null)
        {
            RenderOffline();
            return 1;
        }

        AnsiConsole.MarkupLine($"[green]Peer [bold]{result.NodeId}[/] has been removed.[/]");
        return 0;
    }

    private StatusRestApi CreateApi()
    {
        var port = _configService.LoadGlobalConfig().GetAwaiter().GetResult().DefaultListenPort;
        var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromSeconds(5) };
        return new StatusRestApi(http);
    }

    private static void RenderOffline()
    {
        AnsiConsole.MarkupLine("[yellow]┌─[bold] Daemon offline [/]─[/]");
        AnsiConsole.MarkupLine("[yellow]│[/] Could not connect to the NukeShare daemon.");
        AnsiConsole.MarkupLine("[yellow]├─[/]");
        AnsiConsole.MarkupLine("[yellow]│[/] [grey]Ensure the daemon is running with [white bold]nuke start[/].[/]");
        AnsiConsole.MarkupLine("[yellow]└─[/]");
    }
}
