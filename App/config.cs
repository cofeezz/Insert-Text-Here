using System.Text.Json;

static class Config
{
    static readonly string Dir  = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProcessSuspender");

    public static readonly string WatchlistPath = Path.Combine(Dir, "watchlist.json");
    public static readonly string LogPath       = Path.Combine(Dir, "monitor.log");

    static Config() => Directory.CreateDirectory(Dir);

    public static List<string> Load()
    {
        try
        {
            if (!File.Exists(WatchlistPath)) return [];
            var json = File.ReadAllText(WatchlistPath);
            var doc  = JsonDocument.Parse(json);
            return doc.RootElement
                      .GetProperty("watchlist")
                      .EnumerateArray()
                      .Select(e => e.GetString() ?? "")
                      .Where(s => s.Length > 0)
                      .ToList();
        }
        catch { return []; }
    }

    public static void Save(IEnumerable<string> items)
    {
        var obj  = new { watchlist = items };
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(WatchlistPath, json);
    }
}