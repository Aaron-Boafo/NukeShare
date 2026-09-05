using Spectre.Console;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NukeShare.CLI.Infrastructure;

public static class DaemonProcessLauncher
{
    private const string AppName = "nuked";
    private const string DefaultPort = "7654";

    public static bool IsDaemonRunning()
    {
        return Process.GetProcessesByName(AppName).Length > 0;
    }

    public static string ResolveDaemonPath()
    {
        string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "nuked.exe" : "nuked";

        string cliBaseDirectory = AppContext.BaseDirectory;
        string candidatePath = Path.Combine(cliBaseDirectory, executableName);
        if (File.Exists(candidatePath))
        {
            return candidatePath;
        }

        string currentDir = Path.GetFullPath(cliBaseDirectory);
        while (true)
        {
            string daemonDir = Path.Combine(currentDir, "NukeShare.Daemon");
            if (Directory.Exists(daemonDir))
            {
                string? daemonPath = FindDaemonExecutable(daemonDir, executableName);
                if (daemonPath != null)
                {
                    return daemonPath;
                }
            }

            string? parentDir = Path.GetDirectoryName(currentDir);
            if (string.IsNullOrEmpty(parentDir) || string.Equals(parentDir, currentDir, StringComparison.Ordinal))
            {
                break;
            }
            currentDir = parentDir;
        }

        return candidatePath;
    }

    private static string? FindDaemonExecutable(string daemonDirectory, string executableName)
    {
        string directPath = Path.Combine(daemonDirectory, executableName);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        foreach (string subDir in Directory.GetDirectories(daemonDirectory))
        {
            string candidate = Path.Combine(subDir, executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static void RunDaemon(bool runInBackground = false, string listeningPort = DefaultPort)
    {
        string daemonPath = ResolveDaemonPath();

        if (IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[yellow]┌─[bold] Already running [/]─[/]");
            AnsiConsole.MarkupLine("[yellow]│[/] The NukeShare daemon is already running.");
            AnsiConsole.MarkupLine("[yellow]└─[/]");
            return;
        }

        if (string.IsNullOrEmpty(daemonPath) || !File.Exists(daemonPath))
        {
            AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
            AnsiConsole.MarkupLine("[red]│[/] Could not locate the [white bold]nuked[/] daemon executable.");
            AnsiConsole.MarkupLine("[red]├─[/]");
            AnsiConsole.MarkupLine("[red]│[/] [grey]Ensure NukeShare.Daemon has been built alongside the CLI.[/]");
            AnsiConsole.MarkupLine("[red]└─[/]");
            return;
        }

        ProcessStartInfo daemonStartInfo;

        if (runInBackground)
        {
            daemonStartInfo = new ProcessStartInfo
            {
                FileName = daemonPath,
                Arguments = "--background",
                WorkingDirectory = Path.GetDirectoryName(daemonPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                daemonStartInfo = new ProcessStartInfo
                {
                    FileName = daemonPath,
                    Arguments = "--interactive",
                    WorkingDirectory = Path.GetDirectoryName(daemonPath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
            }
            else
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    daemonStartInfo = new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"-a Terminal \"{daemonPath}\"",
                        UseShellExecute = false
                    };
                }
                else
                {
                    daemonStartInfo = new ProcessStartInfo
                    {
                        FileName = "x-terminal-emulator",
                        Arguments = $"-e \"{daemonPath}\"",
                        UseShellExecute = false
                    };
                }
            }
        }

        daemonStartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        daemonStartInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{listeningPort}";

        try
        {
            var process = Process.Start(daemonStartInfo);

            if (process == null)
            {
                AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
                AnsiConsole.MarkupLine("[red]│[/] The daemon process could not be started.");
                AnsiConsole.MarkupLine("[red]└─[/]");
                return;
            }

            if (!runInBackground && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.OutputDataReceived += (_, _) => { };
                process.ErrorDataReceived += (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            AnsiConsole.MarkupLine("[green]┌─[bold] Process launched [/]─[/]");
            AnsiConsole.MarkupLine($"[green]│[/] Daemon PID [bold cyan]{process.Id}[/] is now running.");
            AnsiConsole.MarkupLine("[green]└─[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
            AnsiConsole.MarkupLine($"[red]│[/] An error occurred while starting the daemon: [white bold]{ex.Message.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine("[red]├─[/]");
            AnsiConsole.MarkupLine("[red]│[/] [grey]Check that the [white bold]nuked[/] executable is present.[/]");
            AnsiConsole.MarkupLine("[red]└─[/]");
        }
    }

    public static void StopDaemon()
    {
        var processes = Process.GetProcessesByName(AppName);

        if (processes.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]┌─[bold] No daemon [/]─[/]");
            AnsiConsole.MarkupLine("[yellow]│[/] No active NukeShare daemon instance was found.");
            AnsiConsole.MarkupLine("[yellow]└─[/]");
            return;
        }

        foreach (var process in processes)
        {
            try
            {
                process.Kill();
                process.WaitForExit(3000);
                AnsiConsole.MarkupLine("[green]┌─[bold] Stopped [/]─[/]");
                AnsiConsole.MarkupLine($"[green]│[/] Terminated daemon instance [bold cyan](PID: {process.Id})[/].");
                AnsiConsole.MarkupLine("[green]└─[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]┌─[bold] Error [/]─[/]");
                AnsiConsole.MarkupLine($"[red]│[/] Failed to stop [white bold]PID {process.Id}[/]: {ex.Message.EscapeMarkup()}");
                AnsiConsole.MarkupLine("[red]└─[/]");
            }
        }
    }
}
