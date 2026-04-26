namespace Wish.Shell;

public static class PluginCommand
{
    public static int Run(ShellConfig cfg, string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            PrintHelp();
            return 0;
        }

        var sub = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();
        switch (sub)
        {
            case "list":
                return List();
            case "enable":
                if (rest.Length != 1) return Usage("plugin enable <id>");
                return DoEnable(rest[0], true);
            case "disable":
                if (rest.Length != 1) return Usage("plugin disable <id>");
                return DoEnable(rest[0], false);
            case "remove":
            case "rm":
                if (rest.Length != 1) return Usage("plugin remove <id>");
                return DoRemove(rest[0]);
            default:
                return Usage("plugin [list|enable|disable|remove]");
        }
    }

    private static int List()
    {
        foreach (var (installed, manifest) in PluginManager.List())
        {
            var name = manifest?.Name ?? installed.Id;
            var enabled = installed.Enabled ? $"{Ansi.Fg(Rgb.ParseHex("#86EFAC"))}enabled{Ansi.Reset}" : $"{Ansi.Fg(Rgb.ParseHex("#FCA5A5"))}disabled{Ansi.Reset}";
            Ansi.WriteLine($"{name} {Ansi.Dim}{installed.Version}{Ansi.Reset} {enabled}");
        }
        return 0;
    }

    private static int DoEnable(string id, bool enabled)
    {
        var code = PluginManager.EnableDisable(id, enabled, out var msg);
        Ansi.WriteLine(msg);
        return code;
    }

    private static int DoRemove(string id)
    {
        var code = PluginManager.Remove(id, out var msg);
        Ansi.WriteLine(msg);
        return code;
    }

    private static int Usage(string s)
    {
        Ansi.WriteLine("wish: " + s);
        return 1;
    }

    private static void PrintHelp()
    {
        Ansi.WriteLine("wish plugin list");
        Ansi.WriteLine("wish plugin enable <id>");
        Ansi.WriteLine("wish plugin disable <id>");
        Ansi.WriteLine("wish plugin remove <id>");
    }
}

