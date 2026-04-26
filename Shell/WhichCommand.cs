namespace Wish.Shell;

public static class WhichCommand
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Ansi.WriteLine("which: missing operand");
            return 1;
        }

        var errors = 0;
        foreach (var name in args)
        {
            var resolved = Resolve(name);
            if (resolved.Count == 0)
            {
                errors++;
                continue;
            }

            foreach (var p in resolved)
                Ansi.WriteLine(p);
        }

        return errors == 0 ? 0 : 1;
    }

    public static List<string> Resolve(string name)
    {
        var results = new List<string>();

        try
        {
            if (Path.IsPathRooted(name) || name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar))
            {
                var full = Path.GetFullPath(name);
                if (File.Exists(full)) results.Add(full);
                return results;
            }

            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            var exts = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD;.COM")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var dir in path.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var ext in exts)
                {
                    var full = Path.Combine(dir, name + ext);
                    if (File.Exists(full))
                        results.Add(full);
                }
            }
        }
        catch { }

        return results;
    }
}

