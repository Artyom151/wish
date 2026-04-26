using System.Diagnostics;
using System.Linq;

namespace Wish.Shell;

public static class ToolInfo
{
    public static bool TryGetPythonVersion(out string version)
        => TryGetVersion("python", "--version", s => s.StartsWith("Python ") ? s["Python ".Length..].Trim() : s.Trim(), out version);

    public static bool TryGetDotnetVersion(out string version)
        => TryGetVersion("dotnet", "--version", s => s.Trim(), out version);

    public static bool TryGetCargoVersion(out string version)
        => TryGetVersion("cargo", "-V", s => ExtractSecondToken(s, "cargo"), out version);

    public static bool TryGetRustcVersion(out string version)
        => TryGetVersion("rustc", "-V", s => ExtractSecondToken(s, "rustc"), out version);

    public static bool TryGetGoVersion(out string version)
        => TryGetVersion("go", "version", s => ExtractGoVersion(s), out version);

    public static bool TryGetPhpVersion(out string version)
        => TryGetVersion("php", "-v", s => ExtractSecondToken(s, "PHP"), out version);

    public static bool TryGetRubyVersion(out string version)
        => TryGetVersion("ruby", "-v", s => ExtractRubyVersion(s), out version);

    public static bool TryGetJavaVersion(out string version)
        => TryGetVersion("java", "-version", s => ExtractJavaVersion(s), out version);
    public static bool TryGetNodeVersion(out string version)
    {
        if (!TryGetVersion("node", "-v", s => s.Trim().TrimStart('v'), out version))
            return false;
        return true;
    }

    private static bool TryGetVersion(string fileName, string arguments, Func<string, string> normalize, out string version)
    {
        version = "";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;

            // Some tools print version to stderr (e.g. python --version sometimes).
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(300);
            if (p.ExitCode != 0) return false;

            var raw = (stdout + "\n" + stderr).Trim();
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var firstLine = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? raw;
            version = normalize(firstLine);
            return !string.IsNullOrWhiteSpace(version);
        }
        catch
        {
            return false;
        }
    }

    private static string ExtractSecondToken(string line, string expectedFirst)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2 && parts[0].Equals(expectedFirst, StringComparison.OrdinalIgnoreCase))
            return parts[1];
        return line.Trim();
    }

    private static string ExtractGoVersion(string line)
    {
        // go version go1.22.0 windows/amd64
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3 && parts[0] == "go" && parts[1] == "version" && parts[2].StartsWith("go"))
            return parts[2].TrimStart('g', 'o');
        return line.Trim();
    }

    private static string ExtractRubyVersion(string line)
    {
        // ruby 3.2.2p53 (...)  -> 3.2.2p53
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2 && parts[0].Equals("ruby", StringComparison.OrdinalIgnoreCase))
            return parts[1];
        return line.Trim();
    }

    private static string ExtractJavaVersion(string line)
    {
        // java version "17.0.10"  / openjdk version "17.0.10" ...
        var q1 = line.IndexOf('"');
        if (q1 >= 0)
        {
            var q2 = line.IndexOf('"', q1 + 1);
            if (q2 > q1)
                return line.Substring(q1 + 1, q2 - q1 - 1);
        }
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3 && parts[1].Equals("version", StringComparison.OrdinalIgnoreCase))
            return parts[2].Trim('"');
        return line.Trim();
    }
}
