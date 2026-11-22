using System.Text.Json;

namespace ChessDroid.Services
{
    public record OpeningEntry(string Eco, string Name, string Moves, string Eval = "");

    /// <summary>
    /// Loads and caches the ECO opening database from the ecoA-ecoE JSON files.
    /// Each JSON is keyed by FEN; entries have eco, name, and moves (SAN sequence).
    /// </summary>
    public static class EcoBookService
    {
        private static List<OpeningEntry>? _cache;
        private static readonly object _lock = new();

        public static List<OpeningEntry> LoadAll(string booksFolder)
        {
            lock (_lock)
            {
                if (_cache != null) return _cache;

                // Load pre-baked SF18 evals (opening_evals.json sits one level above Books/)
                var evals = new Dictionary<string, string>(StringComparer.Ordinal);
                string evalsPath = Path.Combine(Path.GetDirectoryName(booksFolder) ?? booksFolder, "opening_evals.json");
                if (File.Exists(evalsPath))
                {
                    try
                    {
                        using var evDoc = JsonDocument.Parse(File.ReadAllText(evalsPath));
                        foreach (var ep in evDoc.RootElement.EnumerateObject())
                            evals[ep.Name] = ep.Value.GetString() ?? "";
                    }
                    catch { /* evals are cosmetic — skip on error */ }
                }

                // EcoFileCache parses the ecoA-E.json files once and shares the raw rows
                // with OpeningDatabase (see EcoFileCache for why neither consumer can
                // safely dedup on the other's behalf).
                var raw = EcoFileCache.LoadRaw(booksFolder)
                    .Select(e =>
                    {
                        evals.TryGetValue(e.FullFen, out string? eval);
                        return new OpeningEntry(e.Eco, e.Name, e.Moves, eval ?? "");
                    });

                // Deduplicate: for each (eco, name) pair keep the longest move sequence
                _cache = raw
                    .GroupBy(e => (e.Eco, e.Name))
                    .Select(g => g.OrderByDescending(e => e.Moves.Length).First())
                    .OrderBy(e => e.Eco)
                    .ThenBy(e => e.Name)
                    .ToList();

                return _cache;
            }
        }
    }
}
