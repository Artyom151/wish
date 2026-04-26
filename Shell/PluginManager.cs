using System.IO.Compression;
using System.Text.Json;

namespace Wish.Shell;

public static class PluginManager
{
    public static string PluginsRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wish", "plugins");

    public static string StatePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wish", "plugins.json");

    public static PluginState LoadState()
    {
        try
        {
            if (!File.Exists(StatePath)) return new PluginState();
            var json = File.ReadAllText(StatePath);
            return JsonSerializer.Deserialize<PluginState>(json) ?? new PluginState();
        }
        catch
        {
            return new PluginState();
        }
    }

    public static void SaveState(PluginState state)
    {
        var dir = Path.GetDirectoryName(StatePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static int InstallFromZip(string zipPath, out string message)
    {
        message = "";
        try
        {
            zipPath = Path.GetFullPath(zipPath);
            if (!File.Exists(zipPath))
            {
                message = $"wish: install: not found: {zipPath}";
                return 2;
            }

            using var zip = ZipFile.OpenRead(zipPath);
            var manifestEntry = zip.Entries.FirstOrDefault(e => e.FullName.Replace('\\', '/').EndsWith("plugin.json", StringComparison.OrdinalIgnoreCase));
            if (manifestEntry is null)
            {
                message = "wish: install: plugin.json not found in zip";
                return 3;
            }

            PluginManifest manifest;
            using (var s = manifestEntry.Open())
                manifest = JsonSerializer.Deserialize<PluginManifest>(s) ?? throw new Exception("Invalid plugin.json");

            if (string.IsNullOrWhiteSpace(manifest.Id))
            {
                message = "wish: install: plugin.json missing id";
                return 3;
            }

            var dest = Path.Combine(PluginsRoot, manifest.Id);
            if (Directory.Exists(dest))
                Directory.Delete(dest, recursive: true);
            Directory.CreateDirectory(dest);

            ZipFile.ExtractToDirectory(zipPath, dest, overwriteFiles: true);

            var state = LoadState();
            state.Plugins.RemoveAll(p => p.Id.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase));
            state.Plugins.Add(new PluginInstalled
            {
                Id = manifest.Id,
                Version = manifest.Version ?? "0.0.0",
                Enabled = true,
                InstalledAtUtc = DateTime.UtcNow,
            });
            SaveState(state);

            message = $"Installed {manifest.Id} {manifest.Version}";
            return 0;
        }
        catch (Exception ex)
        {
            message = $"wish: install: {ex.Message}";
            return 1;
        }
    }

    public static int EnableDisable(string id, bool enabled, out string message)
    {
        message = "";
        var state = LoadState();
        var p = state.Plugins.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (p is null)
        {
            message = $"wish: plugin: not installed: {id}";
            return 2;
        }
        p.Enabled = enabled;
        SaveState(state);
        message = enabled ? $"Enabled {id}" : $"Disabled {id}";
        return 0;
    }

    public static int Remove(string id, out string message)
    {
        message = "";
        try
        {
            var state = LoadState();
            var removed = state.Plugins.RemoveAll(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) > 0;
            SaveState(state);
            var dir = Path.Combine(PluginsRoot, id);
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
            message = removed ? $"Removed {id}" : $"Not installed: {id}";
            return removed ? 0 : 2;
        }
        catch (Exception ex)
        {
            message = $"wish: plugin: remove: {ex.Message}";
            return 1;
        }
    }

    public static IEnumerable<(PluginInstalled Installed, PluginManifest? Manifest)> List()
    {
        var state = LoadState();
        foreach (var p in state.Plugins.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
        {
            PluginManifest? manifest = null;
            try
            {
                var manifestPath = Path.Combine(PluginsRoot, p.Id, "plugin.json");
                if (File.Exists(manifestPath))
                    manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath));
            }
            catch { }
            yield return (p, manifest);
        }
    }
}

public sealed class PluginState
{
    public List<PluginInstalled> Plugins { get; set; } = new();
}

public sealed class PluginInstalled
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "0.0.0";
    public bool Enabled { get; set; } = true;
    public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PluginManifest
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Entry { get; set; }
}

