using System.Text.Json;

namespace Wish.Shell;

public sealed class HistoryStore
{
    private readonly List<string> _items = new();

    public IReadOnlyList<string> Items => _items;

    public static string HistoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wish", "history.json");

    public static HistoryStore Load()
    {
        var store = new HistoryStore();
        try
        {
            if (!File.Exists(HistoryPath)) return store;
            var json = File.ReadAllText(HistoryPath);
            var items = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    store._items.Add(item);
            }
        }
        catch
        {
            // ignore
        }
        return store;
    }

    public void Add(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        if (_items.Count > 0 && _items[^1] == line) return;
        _items.Add(line);
        if (_items.Count > 5000) _items.RemoveRange(0, _items.Count - 5000);
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(HistoryPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(HistoryPath, JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // ignore
        }
    }
}
