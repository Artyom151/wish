using System.Diagnostics;

namespace Wish.Shell;

public static class GitInfo
{
    public static bool TryGetBranch(string workingDirectory, out string branch)
    {
        branch = "";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --abbrev-ref HEAD",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            var output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(200);
            if (p.ExitCode != 0) return false;
            if (string.IsNullOrWhiteSpace(output) || output == "HEAD") return false;
            branch = output;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryGetStatus(string workingDirectory, out GitStatus status)
    {
        status = default;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "status --porcelain=1 -b",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(400);
            if (p.ExitCode != 0) return false;

            var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return false;

            string? branch = null;
            var ahead = 0;
            var behind = 0;
            var staged = 0;
            var changed = 0;
            var untracked = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("## "))
                {
                    // Examples:
                    // ## main
                    // ## main...origin/main [ahead 1]
                    // ## main...origin/main [ahead 1, behind 2]
                    var header = line[3..];
                    var first = header.Split(' ', 2)[0];
                    branch = first.Split("...", 2)[0];
                    if (header.Contains("[") && header.Contains("]"))
                    {
                        var bracket = header[(header.IndexOf('[') + 1)..header.IndexOf(']')];
                        foreach (var part in bracket.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (part.StartsWith("ahead "))
                                int.TryParse(part[6..], out ahead);
                            else if (part.StartsWith("behind "))
                                int.TryParse(part[7..], out behind);
                        }
                    }
                    continue;
                }

                if (line.StartsWith("?? "))
                {
                    untracked++;
                    continue;
                }

                if (line.Length >= 2)
                {
                    var x = line[0];
                    var y = line[1];
                    if (x != ' ' && x != '?') staged++;
                    if (y != ' ') changed++;
                }
            }

            if (string.IsNullOrWhiteSpace(branch)) return false;
            status = new GitStatus(branch!, ahead, behind, staged, changed, untracked);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public readonly record struct GitStatus(string Branch, int Ahead, int Behind, int Staged, int Changed, int Untracked);
