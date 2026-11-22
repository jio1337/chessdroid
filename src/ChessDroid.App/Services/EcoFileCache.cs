using System.Text.Json;

namespace ChessDroid.Services
{
    /// <summary>
    /// Parses ecoA-E.json once per folder and caches the raw rows. OpeningDatabase and
    /// EcoBookService both need the same underlying data but apply different, mutually
    /// incompatible dedup strategies on top of it (OpeningDatabase collapses by board
    /// position/piece-placement — fine, since transpositions should show one name;
    /// EcoBookService collapses by (eco, name) keeping the longest move sequence — doing
    /// that over an already position-deduped set silently drops entries whenever two
    /// different named openings transpose into the same board position). Sharing this one
    /// raw, non-deduped parse lets each consumer still apply its own correct dedup while
    /// reading the ~4.4MB of JSON from disk only once instead of twice.
    /// </summary>
    public static class EcoFileCache
    {
        public readonly record struct RawEntry(string FullFen, string Eco, string Name, string Moves);

        private static readonly object _lock = new();
        private static readonly Dictionary<string, List<RawEntry>> _cache = new();

        public static List<RawEntry> LoadRaw(string booksFolder)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(booksFolder, out var cached))
                    return cached;

                var raw = new List<RawEntry>();
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

                            raw.Add(new RawEntry(prop.Name, eco, name, moves));
                        }
                    }
                    catch { /* skip malformed file */ }
                }

                _cache[booksFolder] = raw;
                return raw;
            }
        }
    }
}
