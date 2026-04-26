namespace Wish.Shell;

using System.Linq;

public static class SyntaxHighlighter
{
    public static string Highlight(string line, ShellConfig cfg, WishConfig wish)
    {
        var tokens = CommandLine.Tokenize(line);
        if (tokens.Count == 0) return line;

        var head = tokens[0].Text;
        var isKnown = IsKnownCommand(head, wish);

        var outLine = "";
        var last = 0;
        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Start > last)
                outLine += line.Substring(last, t.Start - last); // preserve whitespace exactly

            var raw = line.Substring(t.Start, t.Length);
            if (i == 0)
            {
                var color = isKnown ? cfg.Md3.Primary : cfg.Md3.Error;
                outLine += $"{Ansi.Fg(color)}{raw}{Ansi.Reset}";
            }
            else if (raw.StartsWith('-'))
            {
                outLine += $"{Ansi.Fg(cfg.Md3.OnSurfaceVariant)}{raw}{Ansi.Reset}";
            }
            else if (LooksLikePath(raw))
            {
                outLine += $"{Ansi.Fg(Rgb.ParseHex("#93C5FD"))}{raw}{Ansi.Reset}";
            }
            else if (t.WasQuoted || IsQuoted(raw))
            {
                outLine += $"{Ansi.Fg(Rgb.ParseHex("#FDE68A"))}{raw}{Ansi.Reset}";
            }
            else
            {
                outLine += raw;
            }

            last = t.Start + t.Length;
        }

        if (last < line.Length)
            outLine += line[last..];

        return outLine;
    }

    public static bool IsKnownCommand(string head, WishConfig wish)
    {
        if (string.IsNullOrWhiteSpace(head)) return false;
        if (Builtins.ListNames().Any(b => b.Equals(head, StringComparison.OrdinalIgnoreCase))) return true;
        if (wish.Aliases.ContainsKey(head)) return true;
        if (wish.Functions.ContainsKey(head)) return true;
        return CommandExistsOnPath(head);
    }

    private static bool CommandExistsOnPath(string name)
    {
        try
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            var exts = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD;.COM")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var dir in path.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var ext in exts)
                {
                    var full = Path.Combine(dir, name + ext.ToLowerInvariant());
                    if (File.Exists(full)) return true;
                    full = Path.Combine(dir, name + ext.ToUpperInvariant());
                    if (File.Exists(full)) return true;
                }
            }
        }
        catch { }
        return false;
    }

    private static bool LooksLikePath(string s)
        => s.Contains('\\') || s.Contains('/') || s.StartsWith(".\\") || s.StartsWith("..\\") || s.StartsWith("~/") || s.StartsWith("~\\");

    private static bool IsQuoted(string s)
        => s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\''));

    // Tokenization uses CommandLine.Tokenize
}
