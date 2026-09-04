using System.ComponentModel;
using NukeShare.CLI.Infrastructure;
using NukeShare.Configuration.Service;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NukeShare.CLI.Commands;

public class StatusCommand(ConfigurationService _configService) : AsyncCommand<StatusCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-h|--health")]
        [Description("Show daemon health checks")]
        public bool Health { get; init; }

        [CommandOption("-p|--peers")]
        [Description("List discovered and connected peers")]
        public bool Peers { get; init; }

        [CommandOption("-t|--transfers")]
        [Description("Show active file transfers")]
        public bool Transfers { get; init; }

        [CommandOption("-s|--shutdown")]
        [Description("Gracefully shut down the daemon")]
        public bool Shutdown { get; init; }

        [CommandOption("-c|--config")]
        [Description("Show daemon configuration")]
        public bool Config { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var api = CreateApi();

        if (settings.Shutdown)
            return await ExecuteShutdown(api, cancellationToken);

        if (settings.Config)
            return await ExecuteConfig(api, cancellationToken);

        if (settings.Health)
            return await ExecuteHealth(api, cancellationToken);

        if (settings.Peers)
            return await ExecutePeers(api, cancellationToken);

        if (settings.Transfers)
            return await ExecuteTransfers(api, cancellationToken);

        return await ExecuteStatus(api, cancellationToken);
    }

    private async Task<int> ExecuteStatus(StatusRestApi api, CancellationToken ct)
    {
        var status = await api.GetStatusAsync(ct);
        if (status is null)
        {
            RenderOffline();
            return 1;
        }

        var health = await api.GetHealthAsync(ct);
        var uptime = status.Uptime;
        var uptimeFormatted = $"{(int)uptime.TotalHours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[cyan]NukeShare Daemon Status[/]")
            .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned().NoWrap().Width(22))
            .AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        var statusColor = status.Status == "online" ? "green" : "red";
        table.AddRow("[grey]Status[/]", $"[bold {statusColor}]{status.Status.EscapeMarkup()}[/]");
        table.AddRow("[grey]Process ID[/]", $"[white]{status.ProcessId}[/]");
        table.AddRow("[grey]Machine[/]", $"[white]{status.Machine.EscapeMarkup()}[/]");
        table.AddRow("[grey]Version[/]", $"[white]{status.Version.EscapeMarkup()}[/]");
        table.AddRow("[grey]Uptime[/]", $"[white]{uptimeFormatted}[/]");
        table.AddRow("[grey]Memory Usage[/]", $"[white]{status.MemoryUsageMb} MB[/]");
        table.AddRow("[grey]Timestamp[/]", $"[white]{status.Timestamp:yyyy-MM-dd HH:mm:ss} UTC[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);

        if (health is not null)
        {
            AnsiConsole.WriteLine();
            var hc = health.Healthy ? "green" : "red";
            var hl = health.Healthy ? "Healthy" : "Unhealthy";
            AnsiConsole.MarkupLine($"Health: [bold {hc}]{hl}[/]");
            AnsiConsole.MarkupLine($"  Storage:    [grey]{health.Checks.StorageDirectory.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  Discovery:  [grey]{health.Checks.DiscoveryListener.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"  Transfers:  [grey]{health.Checks.TransferListener.EscapeMarkup()}[/]");
        }

        AnsiConsole.WriteLine();
        return 0;
    }

    private async Task<int> ExecuteHealth(StatusRestApi api, CancellationToken ct)
    {
        var health = await api.GetHealthAsync(ct);
        if (health is null)
        {
            RenderOffline();
            return 1;
        }

        var hc = health.Healthy ? "green" : "red";
        var hl = health.Healthy ? "Healthy" : "Unhealthy";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[cyan]Health Check[/] — [bold {hc}]{hl}[/]")
            .AddColumn(new TableColumn("[bold]Component[/]").LeftAligned().NoWrap().Width(22))
            .AddColumn(new TableColumn("[bold]Status[/]").LeftAligned());

        table.AddRow("[grey]Storage Directory[/]", $"[white]{health.Checks.StorageDirectory.EscapeMarkup()}[/]");
        table.AddRow("[grey]Discovery Listener[/]", $"[white]{health.Checks.DiscoveryListener.EscapeMarkup()}[/]");
        table.AddRow("[grey]Transfer Listener[/]", $"[white]{health.Checks.TransferListener.EscapeMarkup()}[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        return 0;
    }

    private async Task<int> ExecutePeers(StatusRestApi api, CancellationToken ct)
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

    private async Task<int> ExecuteTransfers(StatusRestApi api, CancellationToken ct)
    {
        var transfers = await api.GetTransfersAsync(ct);
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

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[cyan]Transfers[/]")
            .AddColumn(new TableColumn("[bold]File[/]").LeftAligned().NoWrap().Width(24))
            .AddColumn(new TableColumn("[bold]Direction[/]").LeftAligned().NoWrap().Width(12))
            .AddColumn(new TableColumn("[bold]Progress[/]").LeftAligned().NoWrap().Width(12))
            .AddColumn(new TableColumn("[bold]Speed[/]").LeftAligned().NoWrap().Width(14))
            .AddColumn(new TableColumn("[bold]Chunks[/]").LeftAligned().NoWrap().Width(8));

        foreach (var t in transfers.Transfers)
        {
            var dirColor = t.Direction == "Receiving" ? "cyan" : "green";
            var speed = FormatBytesPerSec(t.TransferSpeedBytesPerSec);

            table.AddRow(
                $"[white]{t.FileName.EscapeMarkup()}[/]",
                $"[{dirColor}]{t.Direction.EscapeMarkup()}[/]",
                $"[white]{t.ProgressPercentage:F1}%[/]",
                $"[white]{speed}[/]",
                $"[white]{t.ActiveChunks}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        return 0;
    }

    private async Task<int> ExecuteShutdown(StatusRestApi api, CancellationToken ct)
    {
        var confirmed = AnsiConsole.Confirm("Are you sure you want to shut down the daemon?");
        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[grey]Shutdown cancelled.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[grey]Sending shutdown signal...[/]");
        var result = await api.ShutdownAsync(ct);

        if (result is null)
        {
            RenderOffline();
            return 1;
        }

        AnsiConsole.MarkupLine("[green]┌─[bold] Shutdown [/]─[/]");
        AnsiConsole.MarkupLine($"[green]│[/] {result.Message.EscapeMarkup()}");
        AnsiConsole.MarkupLine("[green]└─[/]");
        return 0;
    }

    private async Task<int> ExecuteConfig(StatusRestApi api, CancellationToken ct)
    {
        var config = await api.GetConfigAsync(ct);
        if (config is null)
        {
            RenderOffline();
            return 1;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[cyan]Daemon Configuration[/]")
            .AddColumn(new TableColumn("[bold]Section[/]").LeftAligned().NoWrap().Width(16))
            .AddColumn(new TableColumn("[bold]Key[/]").LeftAligned().NoWrap().Width(28))
            .AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        void AddRow(string section, string key, string value)
        {
            table.AddRow(
                $"[grey]{section.EscapeMarkup()}[/]",
                $"[white]{key.EscapeMarkup()}[/]",
                $"[green]{value.EscapeMarkup()}[/]"
            );
        }

        AddRow("Identity", "Username", config.Identity.Username);
        AddRow("Identity", "DeviceName", config.Identity.DeviceName);

        AddRow("Network", "Listen Port", config.Network.ListenPort);
        AddRow("Network", "Discovery Port", config.Network.DiscoveryPort);
        AddRow("Network", "Max Peers", config.Network.MaxPeers.ToString());
        AddRow("Network", "Peer Timeout", $"{config.Network.PeerTimeoutSeconds}s");
        AddRow("Network", "Broadcast Interval", $"{config.Network.DiscoveryIntervalSeconds}s");
        AddRow("Network", "Multicast", config.Network.UseMulticastDiscovery ? "Enabled" : "Disabled");
        AddRow("Network", "Allowed Subnets", config.Network.AllowedSubnets.Length == 0 ? "All" : string.Join(", ", config.Network.AllowedSubnets));

        AddRow("Security", "Encryption", config.Security.EnableEncryption ? $"Enabled ({config.Security.EncryptionCipher})" : "Disabled");
        AddRow("Security", "Trust Mode", config.Security.TrustMode);
        AddRow("Security", "Auto-Accept Untrusted", config.Security.AutoAcceptUntrustedPeers ? "Yes" : "No");

        AddRow("Transfer", "Auto-Accept Peers", config.Transfer.AutoAcceptPeers ? "Yes" : "No");
        AddRow("Transfer", "Chunks", config.Transfer.Chunks.ToString());
        AddRow("Transfer", "Chunk Size", $"{config.Transfer.ChunkSize / (1024.0 * 1024.0):F1} MB");
        AddRow("Transfer", "Concurrent Peers", config.Transfer.ConcurrentPeers.ToString());
        AddRow("Transfer", "Bandwidth Limit", config.Transfer.BandwidthLimitBytesPerSecond == 0 ? "Unlimited" : $"{config.Transfer.BandwidthLimitBytesPerSecond / (1024.0 * 1024.0):F1} MB/s");
        AddRow("Transfer", "Max File Size", $"{config.Transfer.MaxFileSizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        return 0;
    }

    private static string FormatBytesPerSec(long bytesPerSec)
    {
        if (bytesPerSec >= 1024 * 1024)
            return $"{bytesPerSec / (1024.0 * 1024.0):F1} MB/s";
        if (bytesPerSec >= 1024)
            return $"{bytesPerSec / 1024.0:F1} KB/s";
        return $"{bytesPerSec} B/s";
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
