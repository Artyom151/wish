namespace Wish.Shell;

public static class TreeCommand
{
    public static int Run(ShellConfig cfg, string[] args)
    {
        var depth = 3;
        var showAll = false;
        string? root = null;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "-a" or "--all") showAll = true;
            else if (a is "-L" or "--depth")
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out var d))
                {
                    depth = Math.Clamp(d, 1, 20);
                    i++;
                }
                else
                {
                    Ansi.WriteLine("tree: expected number after -L/--depth");
                    return 1;
                }
            }
            else if (root is null) root = a;
            else
            {
                Ansi.WriteLine("tree: too many arguments");
                return 1;
            }
        }

        root ??= Environment.CurrentDirectory;
        root = Path.GetFullPath(root);
        if (!Directory.Exists(root))
        {
            Ansi.WriteLine($"tree: {root}: not a directory");
            return 2;
        }

        Ansi.WriteLine($"{Ansi.Fg(cfg.Md3.Primary)} {root}{Ansi.Reset}");
        RenderDir(cfg, new DirectoryInfo(root), "", depth, showAll);
        return 0;
    }

    private static void RenderDir(ShellConfig cfg, DirectoryInfo dir, string prefix, int depth, bool showAll)
    {
        if (depth <= 0) return;

        FileSystemInfo[] entries;
        try
        {
            entries = dir.EnumerateFileSystemInfos()
                .Where(e => showAll || !IsHidden(e))
                .OrderByDescending(e => e is DirectoryInfo)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .Take(500)
                .ToArray();
        }
        catch
        {
            return;
        }

        for (var i = 0; i < entries.Length; i++)
        {
            var last = i == entries.Length - 1;
            var branch = last ? "└── " : "├── ";
            var nextPrefix = prefix + (last ? "    " : "│   ");

            var e = entries[i];
            var icon = e is DirectoryInfo ? "󰉋" : (IsExe(e.FullName) ? "󰆍" : "󰈔");
            var color = e is DirectoryInfo ? cfg.Md3.Primary : (IsExe(e.FullName) ? Rgb.ParseHex("#86EFAC") : cfg.Md3.OnSurface);
            Ansi.WriteLine(prefix + branch + $"{Ansi.Fg(color)}{icon} {e.Name}{Ansi.Reset}");

            if (e is DirectoryInfo d)
                RenderDir(cfg, d, nextPrefix, depth - 1, showAll);
        }
    }

    private static bool IsHidden(FileSystemInfo e)
    {
        try
        {
            return e.Attributes.HasFlag(FileAttributes.Hidden) || e.Attributes.HasFlag(FileAttributes.System);
        }
        catch { return false; }
    }

    private static bool IsExe(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".exe" or ".bat" or ".cmd" or ".ps1" or ".com";
    }
}

