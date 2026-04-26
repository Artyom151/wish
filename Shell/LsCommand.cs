namespace Wish.Shell;

public static class LsCommand
{
    public static int Run(ShellConfig cfg, string[] args)
    {
        var showAll = false;
        string? path = null;

        foreach (var a in args)
        {
            if (a is "-a" or "--all") showAll = true;
            else if (path is null) path = a;
            else
            {
                Ansi.WriteLine("ls: too many arguments");
                return 1;
            }
        }

        path ??= Environment.CurrentDirectory;
        path = Expand(path);

        try
        {
            var dir = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            if (dir is null || !Directory.Exists(dir))
            {
                Ansi.WriteLine($"ls: cannot access '{path}'");
                return 2;
            }

            var entries = Directory.EnumerateFileSystemEntries(dir)
                .Select(p => new Entry(p))
                .Where(e => showAll || !e.IsHidden)
                .OrderByDescending(e => e.IsDir)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            RenderGrid(cfg, entries);
            return 0;
        }
        catch (Exception ex)
        {
            Ansi.WriteLine($"ls: {ex.Message}");
            return 1;
        }
    }

    private static void RenderGrid(ShellConfig cfg, List<Entry> entries)
    {
        if (entries.Count == 0) return;

        var termWidth = Math.Max(40, Console.BufferWidth);
        var cells = entries.Select(e => FormatCell(cfg, e)).ToList();
        var widths = cells.Select(c => Ansi.PrintableWidth(c)).ToList();
        var colWidth = Math.Min(widths.Max(), 50);
        var colPad = 2;
        var cols = Math.Max(1, (termWidth + colPad) / (colWidth + colPad));
        cols = Math.Min(cols, entries.Count);
        var rows = (int)Math.Ceiling(entries.Count / (double)cols);

        for (var r = 0; r < rows; r++)
        {
            var line = "";
            for (var c = 0; c < cols; c++)
            {
                var i = c * rows + r; // column-major like `ls`
                if (i >= cells.Count) continue;
                var cell = cells[i];
                var w = widths[i];
                var pad = (colWidth - w) + colPad;
                line += cell + new string(' ', Math.Max(0, pad));
            }
            Ansi.WriteLine(line.TrimEnd());
        }
    }

    private static string FormatCell(ShellConfig cfg, Entry e)
    {
        var icon = e.IsDir ? "󰉋" :
            e.IsExe ? "󰆍" :
            e.IsSymlink ? "󰌷" :
            "󰈔";

        var name = e.Name + (e.IsDir ? Path.DirectorySeparatorChar : "");

        var fg = e.IsDir ? cfg.Md3.Primary :
            e.IsExe ? Rgb.ParseHex("#86EFAC") :
            e.IsSymlink ? cfg.Md3.OnSurfaceVariant :
            cfg.Md3.OnSurface;

        return $"{Ansi.Fg(fg)}{icon} {name}{Ansi.Reset}";
    }

    private static string Expand(string path)
    {
        if (path == "~")
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (path.StartsWith("~\\"))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]);
        return Path.GetFullPath(path);
    }

    private sealed record Entry(string FullPath)
    {
        public string Name => Path.GetFileName(FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        public bool IsDir => Directory.Exists(FullPath);

        public bool IsSymlink
        {
            get
            {
                try
                {
                    var attrs = File.GetAttributes(FullPath);
                    return attrs.HasFlag(FileAttributes.ReparsePoint);
                }
                catch { return false; }
            }
        }

        public bool IsHidden
        {
            get
            {
                try
                {
                    var attrs = File.GetAttributes(FullPath);
                    return attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System);
                }
                catch { return false; }
            }
        }

        public bool IsExe
        {
            get
            {
                if (IsDir) return false;
                var ext = Path.GetExtension(FullPath).ToLowerInvariant();
                return ext is ".exe" or ".bat" or ".cmd" or ".ps1" or ".com";
            }
        }
    }
}

