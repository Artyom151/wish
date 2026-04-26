namespace Wish.Shell;

public static class GitRepo
{
    public static bool IsInRepo(string startDirectory)
    {
        try
        {
            var dir = new DirectoryInfo(startDirectory);
            while (dir is not null)
            {
                var dotGit = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(dotGit) || File.Exists(dotGit))
                    return true;
                dir = dir.Parent;
            }
        }
        catch { }

        return false;
    }
}
