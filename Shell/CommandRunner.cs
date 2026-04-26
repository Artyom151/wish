using System.Diagnostics;

namespace Wish.Shell;

public sealed class CommandRunner
{
    public async Task<int> RunViaWindowsPowerShellAsync(string command)
    {
        // Run as a child process so we get Windows command semantics, pipelines, etc.
        // Note: for a real "fish clone" you'd parse/execute yourself; this is a pragmatic prototype.
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoLogo -NoProfile -Command {EscapeForPowershellCommand(command)}",
            UseShellExecute = false,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            WorkingDirectory = Environment.CurrentDirectory,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);

        ConsoleCancelEventHandler? handler = (_, e) =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch { }
            e.Cancel = true;
        };

        Console.CancelKeyPress += handler;
        try
        {
            process.Start();
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    private static string EscapeForPowershellCommand(string command)
    {
        // We pass a single -Command string. Wrap it in & { ... } and escape quotes.
        var escaped = command.Replace("\"", "`\"");
        return $"\"& {{ {escaped} }}\"";
    }
}
