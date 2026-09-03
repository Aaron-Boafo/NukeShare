using NukeShare.CLI.Infrastructure;
using Spectre.Console.Cli;

namespace NukeShare.CLI.Commands;

public class StopCommand : Command<StopCommand.Settings>
{
    public class Settings : CommandSettings { }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        DaemonProcessLauncher.StopDaemon();
        return 0;
    }
}
