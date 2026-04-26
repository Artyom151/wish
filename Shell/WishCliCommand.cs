namespace Wish.Shell;

public static class WishCliCommand
{
    public static int Run(ShellConfig cfg, string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            PrintHelp();
            return 0;
        }

        var cmd = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        switch (cmd)
        {
            case "install":
                if (rest.Length != 1)
                {
                    Ansi.WriteLine("wish: install <plugin.zip>");
                    return 1;
                }
                var code = PluginManager.InstallFromZip(rest[0], out var msg);
                Ansi.WriteLine(msg);
                return code;
            case "plugin":
            case "plugins":
                return PluginCommand.Run(cfg, rest);
            default:
                PrintHelp();
                return 1;
        }
    }

    private static void PrintHelp()
    {
        Ansi.WriteLine("wish install <plugin.zip>");
        Ansi.WriteLine("wish plugin list");
        Ansi.WriteLine("wish plugin enable <id>");
        Ansi.WriteLine("wish plugin disable <id>");
        Ansi.WriteLine("wish plugin remove <id>");
        Ansi.WriteLine($"Plugins folder: {PluginManager.PluginsRoot}");
    }
}

