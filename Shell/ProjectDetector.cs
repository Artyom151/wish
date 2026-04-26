namespace Wish.Shell;

public static class ProjectDetector
{
    public static ProjectKinds Detect(string directory)
    {
        // Check current dir and a couple parents (common monorepo layouts).
        var current = new DirectoryInfo(directory);
        for (var depth = 0; depth < 3 && current is not null; depth++, current = current.Parent)
        {
            var kind = DetectHere(current.FullName);
            if (kind != ProjectKinds.None) return kind;
        }
        return ProjectKinds.None;
    }

    private static ProjectKinds DetectHere(string dir)
    {
        try
        {
            var kinds = ProjectKinds.None;

            if (File.Exists(Path.Combine(dir, "package.json")) ||
                File.Exists(Path.Combine(dir, "pnpm-lock.yaml")) ||
                File.Exists(Path.Combine(dir, "yarn.lock")))
                kinds |= ProjectKinds.Node;

            if (File.Exists(Path.Combine(dir, "pyproject.toml")) ||
                File.Exists(Path.Combine(dir, "requirements.txt")) ||
                File.Exists(Path.Combine(dir, "Pipfile")))
                kinds |= ProjectKinds.Python;

            if (File.Exists(Path.Combine(dir, "composer.json")) ||
                Directory.EnumerateFiles(dir, "*.php").Any())
                kinds |= ProjectKinds.Php;

            if (File.Exists(Path.Combine(dir, "Gemfile")) ||
                Directory.EnumerateFiles(dir, "*.rb").Any())
                kinds |= ProjectKinds.Ruby;

            if (Directory.EnumerateFiles(dir, "*.csproj").Any() ||
                Directory.EnumerateFiles(dir, "*.fsproj").Any() ||
                File.Exists(Path.Combine(dir, "global.json")))
                kinds |= ProjectKinds.DotNet;

            if (File.Exists(Path.Combine(dir, "Cargo.toml")))
                kinds |= ProjectKinds.Rust;

            if (File.Exists(Path.Combine(dir, "go.mod")))
                kinds |= ProjectKinds.Go;

            if (File.Exists(Path.Combine(dir, "pom.xml")) ||
                File.Exists(Path.Combine(dir, "build.gradle")) ||
                File.Exists(Path.Combine(dir, "build.gradle.kts")) ||
                File.Exists(Path.Combine(dir, "settings.gradle")) ||
                File.Exists(Path.Combine(dir, "settings.gradle.kts")) ||
                Directory.EnumerateFiles(dir, "*.java").Any() ||
                Directory.EnumerateFiles(dir, "*.kt").Any())
                kinds |= ProjectKinds.Java;

            if (Directory.EnumerateFiles(dir, "*.asm").Any() ||
                Directory.EnumerateFiles(dir, "*.s").Any() ||
                Directory.EnumerateFiles(dir, "*.S").Any())
                kinds |= ProjectKinds.Asm;

            if (File.Exists(Path.Combine(dir, "CMakeLists.txt")) ||
                Directory.EnumerateFiles(dir, "*.cpp").Any() ||
                Directory.EnumerateFiles(dir, "*.c").Any() ||
                Directory.EnumerateFiles(dir, "*.h").Any() ||
                Directory.EnumerateFiles(dir, "*.hpp").Any())
                kinds |= ProjectKinds.Cpp;

            return kinds;
        }
        catch
        {
            return ProjectKinds.None;
        }
    }
}

[Flags]
public enum ProjectKinds
{
    None = 0,
    Node = 1 << 0,
    Python = 1 << 1,
    Php = 1 << 2,
    Ruby = 1 << 3,
    DotNet = 1 << 4,
    Rust = 1 << 5,
    Go = 1 << 6,
    Cpp = 1 << 7,
    Asm = 1 << 8,
    Java = 1 << 9,
}
