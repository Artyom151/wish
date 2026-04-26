using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wish.Shell;

public sealed class ShellConfig
{
    public bool ShowBanner { get; set; } = true;
    public Md3Palette Md3 { get; set; } = Md3Palette.Default();
    public PromptTheme Prompt { get; set; } = PromptTheme.Default();
    public PromptOptions Options { get; set; } = PromptOptions.Default();

    public static string ConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wish", "config.json");

    public static ShellConfig LoadOrCreateDefault()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<ShellConfig>(json, JsonOptions()) ?? new ShellConfig();
                cfg.Md3 ??= Md3Palette.Default();
                cfg.Prompt ??= PromptTheme.Default();
                cfg.Options ??= PromptOptions.Default();
                return cfg;
            }
        }
        catch
        {
            // ignore, fall back to default
        }

        var created = new ShellConfig();
        created.Save();
        return created;
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOptions()));
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new RgbJsonConverter() },
    };
}

public sealed class Md3Palette
{
    public Rgb Primary { get; set; }
    public Rgb OnPrimary { get; set; }
    public Rgb Surface { get; set; }
    public Rgb OnSurface { get; set; }
    public Rgb SurfaceVariant { get; set; }
    public Rgb OnSurfaceVariant { get; set; }
    public Rgb Outline { get; set; }
    public Rgb Error { get; set; }

    public static Md3Palette Default() => new()
    {
        // A pleasant MD3-ish dark palette (customizable in config.json)
        Primary = Rgb.ParseHex("#8AB4F8"),
        OnPrimary = Rgb.ParseHex("#0B1B33"),
        Surface = Rgb.ParseHex("#0F1216"),
        OnSurface = Rgb.ParseHex("#E6E1E5"),
        SurfaceVariant = Rgb.ParseHex("#1B222C"),
        OnSurfaceVariant = Rgb.ParseHex("#C4C7C5"),
        Outline = Rgb.ParseHex("#3A4452"),
        Error = Rgb.ParseHex("#F2B8B5"),
    };
}

public sealed class PromptTheme
{
    public Rgb UserBg { get; set; }
    public Rgb UserFg { get; set; }
    public Rgb ShellBg { get; set; }
    public Rgb ShellFg { get; set; }
    public Rgb PathBg { get; set; }
    public Rgb PathFg { get; set; }
    public Rgb ToolBg { get; set; }
    public Rgb ToolFg { get; set; }
    public Rgb GitBg { get; set; }
    public Rgb GitFg { get; set; }
    public Rgb GitBranchBg { get; set; }
    public Rgb GitBranchFg { get; set; }
    public Rgb ErrorBg { get; set; }
    public Rgb ErrorFg { get; set; }

    public static PromptTheme Default() => new()
    {
        // Light/pastel rainbow defaults (user can tweak in config.json)
        UserBg = Rgb.ParseHex("#C4B5FD"), // light violet
        UserFg = Rgb.ParseHex("#111827"),
        ShellBg = Rgb.ParseHex("#F0ABFC"), // light purple/pink
        ShellFg = Rgb.ParseHex("#111827"),
        PathBg = Rgb.ParseHex("#93C5FD"), // light blue
        PathFg = Rgb.ParseHex("#0B1B33"),
        ToolBg = Rgb.ParseHex("#86EFAC"), // light green
        ToolFg = Rgb.ParseHex("#05210E"),
        GitBg = Rgb.ParseHex("#FDE68A"), // light yellow (stats)
        GitFg = Rgb.ParseHex("#111827"),
        GitBranchBg = Rgb.ParseHex("#A7F3D0"), // light mint (branch)
        GitBranchFg = Rgb.ParseHex("#05210E"),
        ErrorBg = Rgb.ParseHex("#FCA5A5"), // light red
        ErrorFg = Rgb.ParseHex("#111827"),
    };
}

public sealed class PromptOptions
{
    public bool UseNerdFontIcons { get; set; } = true;
    public bool UsePowerlineSeparators { get; set; } = true;
    public bool AnimatePrompt { get; set; } = true;
    public int PromptTrailMs { get; set; } = 90;

    public static PromptOptions Default() => new()
    {
        // User preference: keep Nerd Font icons enabled by default.
        UseNerdFontIcons = true,
        UsePowerlineSeparators = true,
        AnimatePrompt = true,
        PromptTrailMs = 90,
    };
}

public sealed class RgbJsonConverter : JsonConverter<Rgb>
{
    public override Rgb Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Rgb.ParseHex(reader.GetString() ?? "#000000");

    public override void Write(Utf8JsonWriter writer, Rgb value, JsonSerializerOptions options)
        => writer.WriteStringValue($"#{value.R:X2}{value.G:X2}{value.B:X2}");
}
