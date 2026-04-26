namespace Wish.Shell;

public static class CompletionEngine
{
    public static IReadOnlyList<string> GetCandidates(string line, int cursor, WishConfig wish)
    {
        var tok = CommandLine.GetTokenAtCursor(line, cursor);
        var token = tok?.RawToken ?? "";
        var tokenStart = tok?.TokenStart ?? 0;
        var tokensBefore = CommandLine.Tokenize(line[..Math.Min(cursor, line.Length)]).Select(t => t.Text).ToList();
        var isFirstToken = tokensBefore.Count <= 1 && tokenStart == 0;

        // Subcommand completions for "wish ..."
        if (tokensBefore.Count >= 1 && tokensBefore[0].Equals("wish", StringComparison.OrdinalIgnoreCase) && tokenStart > 0)
        {
            if (tokensBefore.Count == 1)
            {
                var subs = new[] { "install", "plugin" }
                    .Where(s => s.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                return subs;
            }

            if (tokensBefore.Count == 2 && tokensBefore[1].Equals("plugin", StringComparison.OrdinalIgnoreCase))
            {
                var subs = new[] { "list", "enable", "disable", "remove" }
                    .Where(s => s.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                return subs;
            }
        }

        if (isFirstToken)
        {
            var builtins = Builtins.ListNames();
            var aliases = wish.Aliases.Keys;
            var functions = wish.Functions.Keys;
            var pathCmds = PathCommands.ListExecutablesOnPath(token);
            var all = builtins
                .Concat(aliases)
                .Concat(functions)
                .Concat(pathCmds)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(c => c.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .Take(200)
                .ToList();
            return all;
        }

        return PathCompletion.ListPathCandidates(token, Environment.CurrentDirectory)
            .Where(c => c.StartsWith(token, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToList();
    }

    public static string ApplySingleCompletion(string line, int cursor, string completion, out int newCursor)
    {
        var tok = CommandLine.GetTokenAtCursor(line, cursor);
        if (tok is null)
        {
            newCursor = line.Length;
            return completion;
        }

        return CommandLine.ReplaceToken(line, tok.Value.TokenStart, tok.Value.TokenLength, completion, out newCursor);
    }
}

internal static class PathCommands
{
    public static IEnumerable<string> ListExecutablesOnPath(string prefix)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var exts = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD;.COM")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in path.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.EnumerateFiles(dir, prefix + "*", SearchOption.TopDirectoryOnly).Take(400))
                {
                    var ext = Path.GetExtension(file);
                    if (exts.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                        results.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch { }
        }

        return results;
    }
}

internal static class PathCompletion
{
    public static IEnumerable<string> ListPathCandidates(string token, string cwd)
    {
        var expanded = ExpandToken(token, cwd, out var baseDir, out var prefix);
        if (baseDir is null || !Directory.Exists(baseDir)) yield break;

        IEnumerable<string> entries;
        try { entries = Directory.EnumerateFileSystemEntries(baseDir, prefix + "*"); }
        catch { yield break; }

        foreach (var e in entries.Take(200))
        {
            var name = Path.GetFileName(e);
            var candidate = expanded.Leading + name;
            if (Directory.Exists(e)) candidate += Path.DirectorySeparatorChar;
            yield return candidate;
        }
    }

    private static (string Leading, string? BaseDir, string Prefix) ExpandToken(string token, string cwd, out string? baseDir, out string prefix)
    {
        // Preserve original leading path portion so completion matches user-typed style.
        var leading = "";
        var t = token;
        if (t.StartsWith("~\\") || t == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            t = t == "~" ? home : Path.Combine(home, t[2..]);
            leading = token.StartsWith("~") ? "~" + Path.DirectorySeparatorChar : "";
        }

        var dirPart = Path.GetDirectoryName(t);
        prefix = Path.GetFileName(t);
        var resolvedDir = string.IsNullOrEmpty(dirPart) ? cwd : (Path.IsPathRooted(dirPart) ? dirPart : Path.GetFullPath(Path.Combine(cwd, dirPart)));

        // For non-rooted, keep the user's original dirPart as leading.
        if (!string.IsNullOrEmpty(dirPart) && !Path.IsPathRooted(dirPart) && leading.Length == 0)
            leading = dirPart.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        baseDir = resolvedDir;
        return (leading, resolvedDir, prefix);
    }
}
