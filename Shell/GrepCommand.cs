using System.Text;
using System.Text.RegularExpressions;

namespace Wish.Shell;

public static class GrepCommand
{
    public static int Run(string[] args)
    {
        var ignoreCase = args.Any(a => a is "-i" or "--ignore-case");
        var number = args.Any(a => a is "-n" or "--line-number");
        var fixedString = args.Any(a => a is "-F" or "--fixed-string");

        var positionals = args.Where(a => !a.StartsWith('-')).ToList();
        if (positionals.Count < 2)
        {
            Ansi.WriteLine("grep: usage: grep [-i] [-n] [-F] <pattern> <files...>");
            return 1;
        }

        var pattern = positionals[0];
        var files = positionals.Skip(1).ToList();

        var opts = RegexOptions.Compiled;
        if (ignoreCase) opts |= RegexOptions.IgnoreCase;
        Regex? regex = null;
        if (!fixedString)
        {
            try { regex = new Regex(pattern, opts); }
            catch (Exception ex)
            {
                Ansi.WriteLine($"grep: invalid regex: {ex.Message}");
                return 2;
            }
        }

        var matchedAny = false;
        foreach (var f in files)
        {
            var path = Path.GetFullPath(f);
            if (!File.Exists(path))
            {
                Ansi.WriteLine($"grep: {f}: no such file");
                continue;
            }

            try
            {
                var lines = File.ReadAllLines(path, Encoding.UTF8);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var isMatch = fixedString
                        ? line.Contains(pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                        : (regex?.IsMatch(line) ?? false);
                    if (!isMatch) continue;

                    matchedAny = true;
                    var prefix = files.Count > 1 ? $"{f}:" : "";
                    if (number) prefix += $"{i + 1}:";
                    Ansi.WriteLine(prefix + HighlightMatch(line, pattern, regex, ignoreCase, fixedString));
                }
            }
            catch (Exception ex)
            {
                Ansi.WriteLine($"grep: {f}: {ex.Message}");
            }
        }

        return matchedAny ? 0 : 1;
    }

    private static string HighlightMatch(string line, string pattern, Regex? regex, bool ignoreCase, bool fixedString)
    {
        var hi = Ansi.Bg(Rgb.ParseHex("#FDE68A")) + Ansi.Fg(Rgb.ParseHex("#111827"));
        var reset = Ansi.Reset;

        if (fixedString)
        {
            var idx = line.IndexOf(pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (idx < 0) return line;
            return line[..idx] + hi + line.Substring(idx, pattern.Length) + reset + line[(idx + pattern.Length)..];
        }

        if (regex is null) return line;
        var m = regex.Match(line);
        if (!m.Success) return line;
        return line[..m.Index] + hi + line.Substring(m.Index, m.Length) + reset + line[(m.Index + m.Length)..];
    }
}

