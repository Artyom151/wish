using System.Text;
using System.Linq;

namespace Wish.Shell;

public sealed class ShellApp
{
    private readonly ShellConfig _config = ShellConfig.LoadOrCreateDefault();
    private readonly HistoryStore _history = HistoryStore.Load();
    private readonly CommandRunner _runner = new();
    private readonly WishConfig _wish = WishConfig.Load();
    private int _lastExitCode;
    private volatile bool _inCommand;

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        Console.OutputEncoding = Encoding.UTF8;
        WindowsTerminal.EnableVirtualTerminalProcessing();

        ConsoleCancelEventHandler? cancelHandler = (_, e) =>
        {
            // Never let Ctrl+C terminate the shell process.
            e.Cancel = true;
            if (!_inCommand)
            {
                Ansi.WriteLine("");
                _lastExitCode = 130;
            }
        };
        Console.CancelKeyPress += cancelHandler;

        if (args.Any(a => a is "--version" or "-v"))
        {
            PrintPrettyVersion();
            Console.CancelKeyPress -= cancelHandler;
            return;
        }

        PrintBanner();

        var editor = new LineEditor(_history);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TerminalIntegration.EmitOsc7Cwd();
                TerminalIntegration.SetTitle($"wish — {Environment.CurrentDirectory}");

                var prompt = PromptRenderer.Render(_config, _lastExitCode);
                var line = editor.ReadLine(prompt, _config, _wish);
                if (line is null) break; // Ctrl+Z / input closed

                if (string.IsNullOrWhiteSpace(line)) continue;

                _history.Add(line);
                _history.Save();

                if (Builtins.TryHandle(line, _config, _wish, ref _lastExitCode))
                {
                    if (_lastExitCode == Builtins.ExitRequestedCode) break;
                    continue;
                }

                line = WishConfig.ExpandAliasesAndFunctions(line, _wish);
                _inCommand = true;
                try
                {
                    _lastExitCode = await _runner.RunViaWindowsPowerShellAsync(line);
                }
                finally
                {
                    _inCommand = false;
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private void PrintBanner()
    {
        if (!_config.ShowBanner) return;

        Ansi.WriteLine(
            $"{Ansi.Fg(_config.Md3.Primary)}wish{Ansi.Reset} " +
            $"{Ansi.Fg(_config.Md3.OnSurfaceVariant)}(prototype){Ansi.Reset} — " +
            $"fish-like line editing + MD3 prompt on Windows");
        Ansi.WriteLine($"Type {Ansi.Bold}help{Ansi.Reset} for builtins. Ctrl+C to cancel a running command.");
        if (_config.Options.UseNerdFontIcons)
        {
            Ansi.WriteLine("If icons render as ◇? set Windows Terminal font to a Nerd Font (e.g. JetBrainsMono Nerd Font).");
        }
        Ansi.WriteLine("");
    }

    private void PrintPrettyVersion()
    {
        var v = AppVersion.Get();
        var md3 = _config.Md3;
        var theme = _config.Prompt;

        // Badge-style output, similar vibe to the prompt segments.
        var left = $"{Ansi.Bg(theme.ShellBg)}{Ansi.Fg(theme.ShellFg)}  wish {Ansi.Reset}";
        var right = $"{Ansi.Bg(md3.SurfaceVariant)}{Ansi.Fg(md3.OnSurfaceVariant)} v{v} {Ansi.Reset}";
        Ansi.WriteLine(left + " " + right);
    }
}
