using System.Diagnostics;
using System.Linq;

namespace Wish.Shell;

public static class PsCommand
{
    public static int Run(string[] args)
    {
        var showAll = args.Any(a => a is "-a" or "--all");
        var list = Process.GetProcesses()
            .OrderBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Ansi.WriteLine($"{Pad("PID", 8)} {Pad("Name", 28)} {Pad("WS(MB)", 8)}  Title");
        foreach (var p in list)
        {
            try
            {
                var title = "";
                try { title = p.MainWindowTitle ?? ""; } catch { }
                if (!showAll && string.IsNullOrWhiteSpace(title)) title = "";

                var wsMb = (p.WorkingSet64 / (1024.0 * 1024.0));
                var line = $"{Pad(p.Id.ToString(), 8)} {Pad(p.ProcessName, 28)} {Pad(wsMb.ToString("0"), 8)}  {title}";
                Ansi.WriteLine(line.TrimEnd());
            }
            catch { }
        }

        return 0;
    }

    private static string Pad(string s, int w)
        => s.Length >= w ? s[..w] : s + new string(' ', w - s.Length);
}
