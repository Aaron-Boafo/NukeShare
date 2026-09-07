using NukeShare.CLI.UI;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace NukeShare.CLI.Commands;

public class RootCommand : Command<RootCommand.Settings>
{
    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        BannerBadge.RenderBadge();
        return 0;
    }

    public class Settings: CommandSettings
    {
        [CommandOption("-v|--version")]
        [Description("View application release version")]
        public bool RenderBanner { get; set; } 
    }

   
}
