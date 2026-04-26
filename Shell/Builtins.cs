using System.Linq;

namespace Wish.Shell;

public static class Builtins
{
    public const int ExitRequestedCode = -99999;

    public static bool TryHandle(string line, ShellConfig cfg, WishConfig wish, ref int lastExitCode)
    {
        line = WishConfig.ExpandAliasesAndFunctions(line, wish);
        var parts = CommandLine.Tokenize(line).Select(t => t.Text).ToList();
        if (parts.Count == 0) return false;

        var cmd = parts[0].ToLowerInvariant();
        switch (cmd)
        {
            case "exit":
                lastExitCode = ExitRequestedCode;
                return true;
            case "cd":
                {
                    var target = parts.Count >= 2 ? string.Join(' ', parts.Skip(1)) : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    try
                    {
                        Environment.SetEnvironmentVariable("WISH_PREV_CWD", Environment.CurrentDirectory);
                        Environment.CurrentDirectory = Path.GetFullPath(ExpandCdTarget(target));
                        lastExitCode = 0;
                    }
                    catch (Exception ex)
                    {
                        Ansi.WriteLine(ex.Message);
                        lastExitCode = 1;
                    }
                    return true;
                }
            case "pwd":
                Ansi.WriteLine(Environment.CurrentDirectory);
                lastExitCode = 0;
                return true;
            case "ls":
                {
                    var args = parts.Skip(1).ToArray();
                    lastExitCode = LsCommand.Run(cfg, args);
                    return true;
                }
            case "rm":
                {
                    var args = parts.Skip(1).ToArray();
                    lastExitCode = RmCommand.Run(cfg, args);
                    return true;
                }
            case "ps":
                {
                    var args = parts.Skip(1).ToArray();
                    lastExitCode = PsCommand.Run(args);
                    return true;
                }
            case "cat":
                {
                    var args = parts.Skip(1).ToArray();
                    lastExitCode = CatCommand.Run(args);
                    return true;
                }
            case "grep":
                {
                    var args = parts.Skip(1).ToArray();
                    lastExitCode = GrepCommand.Run(args);
                    return true;
                }
            case "which":
                {
                    var args = parts.Skip(1).ToArray();
                    lastExitCode = WhichCommand.Run(args);
                    return true;
                }
            case "tree":
                {
                    var args = parts.Skip(1).ToArray();
                    lastExitCode = TreeCommand.Run(cfg, args);
                    return true;
                }
            case "wish":
                {
                    var args = parts.Skip(1).ToArray();
                    lastExitCode = WishCliCommand.Run(cfg, args);
                    return true;
                }
            case "help":
                Ansi.WriteLine("Builtins: cd [path], pwd, ls [-a] [path], tree [-a] [-L n] [path], cat [-n] <files...>, grep [-i] [-n] [-F] <pattern> <files...>, ps [-a], which <cmd...>, rm [-r] [-f] <paths...>, exit, help, config");
                Ansi.WriteLine($"Wish config: {WishConfig.ConfigPath}");
                Ansi.WriteLine($"Config: {ShellConfig.ConfigPath}");
                Ansi.WriteLine("Plugin manager: wish install <zip>, wish plugin list/enable/disable/remove");
                lastExitCode = 0;
                return true;
            case "config":
                {
                    // Just print path; user can open it in their editor.
                    Ansi.WriteLine(ShellConfig.ConfigPath);
                    lastExitCode = 0;
                    return true;
                }
            default:
                return false;
        }
    }

    public static IEnumerable<string> ListNames()
        => new[] { "cd", "pwd", "ls", "tree", "cat", "grep", "ps", "which", "rm", "wish", "exit", "help", "config" };

    private static string ExpandCdTarget(string path)
    {
        path = path.Trim();
        if (path == "-")
        {
            var prev = Environment.GetEnvironmentVariable("WISH_PREV_CWD");
            if (!string.IsNullOrWhiteSpace(prev)) return prev!;
            return Environment.CurrentDirectory;
        }
        if (path == "~")
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (path.StartsWith("~\\"))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]);
        if (path == "/")
            return Path.GetPathRoot(Environment.CurrentDirectory) ?? Environment.CurrentDirectory;
        return path;
    }

    // NOTE: command parsing uses CommandLine.Tokenize
}
