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

                var raw = new List<OpeningEntry>();
                foreach (string file in new[] { "ecoA.json", "ecoB.json", "ecoC.json", "ecoD.json", "ecoE.json" })
                {
                    string path = Path.Combine(booksFolder, file);
                    if (!File.Exists(path)) continue;

                    try
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(path));
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            var val = prop.Value;
                            if (!val.TryGetProperty("eco", out var ecoProp)) continue;
                            if (!val.TryGetProperty("name", out var nameProp)) continue;
                            if (!val.TryGetProperty("moves", out var movesProp)) continue;

                            string eco = ecoProp.GetString() ?? "";
                            string name = nameProp.GetString() ?? "";
                            string moves = movesProp.GetString() ?? "";
                            if (string.IsNullOrEmpty(eco) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(moves))
                                continue;

                            evals.TryGetValue(prop.Name, out string? eval);
                            raw.Add(new OpeningEntry(eco, name, moves, eval ?? ""));
                        }
                    }
                    catch { /* skip malformed file */ }
                }

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
