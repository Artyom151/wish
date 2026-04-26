namespace Wish.Shell;

public sealed class WishConfig
{
    public Dictionary<string, string> Aliases { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Functions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static string ConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wish", "config.wish");

    public static string LegacyFishPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wish", "config.fish");

    public static WishConfig Load()
    {
        var cfg = new WishConfig();
        try
        {
            var path = File.Exists(ConfigPath) ? ConfigPath : (File.Exists(LegacyFishPath) ? LegacyFishPath : null);
            if (path is null) return cfg;

            var lines = File.ReadAllLines(path);

            for (var i = 0; i < lines.Length; i++)
            {
                var raw = StripComment(lines[i]).Trim();
                if (raw.Length == 0) continue;

                if (raw.StartsWith("alias ", StringComparison.OrdinalIgnoreCase))
                {
                    // Accept:
                    // alias ll "ls -la"
                    // alias ll='ls -la'
                    // alias ll=ls -la
                    var rest = raw[6..].Trim();
                    var eq = rest.IndexOf('=');
                    if (eq > 0)
                    {
                        var name = rest[..eq].Trim();
                        var value = Unquote(rest[(eq + 1)..].Trim());
                        if (name.Length > 0 && value.Length > 0)
                            cfg.Aliases[name] = value;
                        continue;
                    }

                    var parts = SplitOnce(rest);
                    if (parts is null) continue;
                    cfg.Aliases[parts.Value.Left] = Unquote(parts.Value.Right);
                    continue;
                }

                if (raw.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
                {
                    // Very small subset:
                    // set -gx VAR value...
                    var tokens = CommandLine.Tokenize(raw).Select(t => t.Text).ToList();
                    if (tokens.Count >= 4 && tokens[0] == "set" && (tokens[1] == "-gx" || tokens[1] == "-xg" || tokens[1] == "-x"))
                    {
                        var varName = tokens[2];
                        var value = string.Join(' ', tokens.Skip(3));
                        if (!string.IsNullOrWhiteSpace(varName))
                            Environment.SetEnvironmentVariable(varName, value);
                    }
                    continue;
                }

                if (raw.StartsWith("function ", StringComparison.OrdinalIgnoreCase))
                {
                    // Minimal function:
                    // function foo
                    //   echo hi
                    // end
                    var tokens = CommandLine.Tokenize(raw).Select(t => t.Text).ToList();
                    if (tokens.Count < 2) continue;
                    var name = tokens[1];
                    var body = new List<string>();
                    for (i = i + 1; i < lines.Length; i++)
                    {
                        var line = StripComment(lines[i]).Trim();
                        if (line.Equals("end", StringComparison.OrdinalIgnoreCase))
                            break;
                        if (line.Length > 0)
                            body.Add(line.TrimEnd(';'));
                    }

                    if (name.Length > 0 && body.Count > 0)
                        cfg.Functions[name] = string.Join("; ", body);
                    continue;
                }
            }
        }
        catch
        {
            // ignore
        }

        return cfg;
    }

    public static string ExpandAliasesAndFunctions(string line, WishConfig cfg)
    {
        var parts = CommandLine.Tokenize(line).Select(t => t.Text).ToList();
        if (parts.Count == 0) return line;
        var head = parts[0];
        var rest = line.Substring(IndexAfterFirstToken(line, head)).TrimStart();

        if (cfg.Aliases.TryGetValue(head, out var alias))
            return alias + (rest.Length > 0 ? " " + rest : "");

        if (cfg.Functions.TryGetValue(head, out var fn))
            return fn + (rest.Length > 0 ? " " + rest : "");

        return line;
    }

    private static int IndexAfterFirstToken(string line, string token)
    {
        var idx = line.IndexOf(token, StringComparison.Ordinal);
        return idx < 0 ? token.Length : idx + token.Length;
    }

    private static (string Left, string Right)? SplitOnce(string s)
    {
        var i = 0;
        while (i < s.Length && !char.IsWhiteSpace(s[i])) i++;
        if (i == 0 || i >= s.Length) return null;
        var left = s[..i].Trim();
        var right = s[i..].Trim();
        if (left.Length == 0 || right.Length == 0) return null;
        return (left, right);
    }

    // NOTE: command tokenization uses CommandLine.Tokenize

    private static string Unquote(string s)
    {
        if (s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
            return s[1..^1];
        return s;
    }

    private static string StripComment(string line)
    {
        // Strip # comments when not inside quotes.
        char? quote = null;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (quote is null && (ch == '"' || ch == '\''))
            {
                quote = ch;
                continue;
            }
            if (quote is not null && ch == quote.Value)
            {
                quote = null;
                continue;
            }
            if (quote is null && ch == '#')
                return line[..i];
        }
        return line;
    }
}
