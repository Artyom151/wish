using System.Reflection;

namespace Wish.Shell;

public static class AppVersion
{
    public static string Get()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info)) return info!;

        var v = asm.GetName().Version;
        return v?.ToString() ?? "0.0.0";
    }
}

