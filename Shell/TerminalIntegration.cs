namespace Wish.Shell;

public static class TerminalIntegration
{
    // OSC 7: set current working directory (supported by Windows Terminal)
    // https://iterm2.com/documentation-escape-codes.html (de-facto)
    public static void EmitOsc7Cwd()
    {
        try
        {
            var cwd = Environment.CurrentDirectory.Replace('\\', '/');
            // Use "file:///" for Windows paths.
            Ansi.Write($"\u001b]7;file:///{cwd}\u0007");
        }
        catch { }
    }

    // OSC 0: set window title
    public static void SetTitle(string title)
    {
        try
        {
            if (string.IsNullOrEmpty(title)) return;
            Ansi.Write($"\u001b]0;{title}\u0007");
        }
        catch { }
    }
}

