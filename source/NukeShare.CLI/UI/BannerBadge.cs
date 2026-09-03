using Spectre.Console;

namespace NukeShare.CLI.UI
{
    internal static class BannerBadge
    {
        private static readonly FigletText Badge = new FigletText("NukeShare")
        {
            Color = Color.OrangeRed1,
            Justification = Justify.Left,
        };

        private static Markup subtitle = new Markup(
            "[bold white]Peer-to-Peer Encrypted File Transfer Engine[/]\n" +
            $"[bold cyan]Build version[/] [grey]{"v1.0.0"}[/]"
        );

        private static Markup hints = new Markup(
            "[grey62]Run [/][yellow]nuke --help[/][grey62] for commands or [/][yellow]nuke peers[/][grey62] to discover nodes.[/]"
        );

        private static Grid grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddRow(Badge)
            .AddRow(subtitle)
            .AddRow(new Rule().RuleStyle(Color.Grey100))
            .AddRow(hints);

        public static void RenderBadge()
        {
            var panel = new Panel(grid)
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(Color.DarkOrange))
            .Padding(2, 1);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

    }
}
