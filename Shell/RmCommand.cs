namespace Wish.Shell;

public static class RmCommand
{
    public static int Run(ShellConfig cfg, string[] args)
    {
        var recursive = false;
        var force = false;
        var targets = new List<string>();

        foreach (var a in args)
        {
            if (a is "-r" or "-R" or "--recursive") recursive = true;
            else if (a is "-f" or "--force") force = true;
            else targets.Add(a);
        }

        if (targets.Count == 0)
        {
            Ansi.WriteLine("rm: missing operand");
            return 1;
        }

        var errors = 0;
        foreach (var t in targets)
        {
            var path = Expand(t);
            try
            {
                if (Directory.Exists(path))
                {
                    if (!recursive)
                    {
                        Ansi.WriteLine($"rm: cannot remove '{t}': is a directory (use -r)");
                        errors++;
                        continue;
                    }
                    Directory.Delete(path, recursive: true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else
                {
                    if (!force)
                    {
                        Ansi.WriteLine($"rm: cannot remove '{t}': no such file or directory");
                        errors++;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!force)
                    Ansi.WriteLine($"rm: {t}: {ex.Message}");
                errors++;
            }
        }

        return errors == 0 ? 0 : 1;
    }

    private static string Expand(string path)
    {
        if (path == "~")
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (path.StartsWith("~\\"))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]);
        return Path.GetFullPath(path);
    }
}

