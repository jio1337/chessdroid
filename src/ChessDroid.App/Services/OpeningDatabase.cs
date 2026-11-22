using System.Diagnostics;
using System.Text.Json;

namespace ChessDroid.Services
{
    /// <summary>
    /// Enhanced opening database that loads from external JSON/TSV files.
    /// Supplements the built-in OpeningBook.cs with 3000+ additional positions.
    ///
    /// Data sources:
    /// - eco.json from https://github.com/hayatbiralem/eco.json (12,000+ positions)
    /// - Lichess chess-openings from https://github.com/lichess-org/chess-openings
    /// </summary>
    public class OpeningDatabase
    {
        private static readonly Lazy<OpeningDatabase> _instance = new(() => new OpeningDatabase());
        public static OpeningDatabase Instance => _instance.Value;

        // Dictionary mapping FEN piece placement -> Opening info
        private readonly Dictionary<string, OpeningInfo> _openings = new();
        private bool _isLoaded = false;

        public class OpeningInfo
        {
            public string ECO { get; set; } = "";
            public string Name { get; set; } = "";
            public string Moves { get; set; } = "";
        }

        public bool IsLoaded => _isLoaded;
        public int Count => _openings.Count;

        private OpeningDatabase()
        {
            // Auto-load on construction
            LoadFromAppDirectory();
        }

        /// <summary>
        /// Loads opening data from the application directory.
        /// Looks for: openings.json, ecoA.json-ecoE.json in app dir or Books folder
        /// </summary>
        public void LoadFromAppDirectory()
        {
            string appPath = Application.StartupPath;
            string booksPath = Path.Combine(appPath, "Books");

            // Try loading from combined openings.json first
            string combinedJson = Path.Combine(appPath, "openings.json");
            if (File.Exists(combinedJson))
            {
                LoadFromJson(combinedJson);
                return;
            }

            // Try loading from eco*.json files in app directory or Books folder
            string[] ecoFiles = { "ecoA.json", "ecoB.json", "ecoC.json", "ecoD.json", "ecoE.json" };
            bool loadedAny = false;

            // Check app directory first
            foreach (var file in ecoFiles)
            {
                string path = Path.Combine(appPath, file);
                if (File.Exists(path))
                {
                    LoadFromJson(path);
                    loadedAny = true;
                }
            }

            // Also check Books folder
            if (Directory.Exists(booksPath))
            {
                foreach (var file in ecoFiles)
                {
                    string path = Path.Combine(booksPath, file);
                    if (File.Exists(path))
                    {
                        LoadFromJson(path);
                        loadedAny = true;
                    }
                }
            }

            if (loadedAny) return;

            // Try loading from TSV file
            string tsvPath = Path.Combine(appPath, "openings.tsv");
            if (File.Exists(tsvPath))
            {
                LoadFromTsv(tsvPath);
                return;
            }

            // Fall back to Books folder for TSV
            string booksTsvPath = Path.Combine(booksPath, "openings.tsv");
            if (File.Exists(booksTsvPath))
            {
                LoadFromTsv(booksTsvPath);
            }
        }

        /// <summary>
        /// Loads openings from a JSON file in eco.json format.
        /// Format: { "FEN": { "eco": "B00", "name": "...", "moves": "..." }, ... }
        /// </summary>
        public void LoadFromJson(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (data == null) return;

                foreach (var kvp in data)
                {
                    string fullFen = kvp.Key;
                    var value = kvp.Value;

                    // Extract piece placement (first part of FEN)
                    string piecePlacement = ExtractPiecePlacement(fullFen);
                    if (string.IsNullOrEmpty(piecePlacement)) continue;

                    // Parse the opening info
                    string eco = "";
                    string name = "";
                    string moves = "";

                    if (value.TryGetProperty("eco", out var ecoProp))
                        eco = ecoProp.GetString() ?? "";
                    if (value.TryGetProperty("name", out var nameProp))
                        name = nameProp.GetString() ?? "";
                    if (value.TryGetProperty("moves", out var movesProp))
                        moves = movesProp.GetString() ?? "";

                    // Only add if we have a name
                    if (!string.IsNullOrEmpty(name))
                    {
                        // Don't overwrite existing entries (first one wins)
                        if (!_openings.ContainsKey(piecePlacement))
                        {
                            _openings[piecePlacement] = new OpeningInfo
                            {
                                ECO = eco,
                                Name = name,
                                Moves = moves
                            };
                        }
                    }
                }

                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading opening database from {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads openings from a TSV file.
        /// Format: FEN\tECO\tName\tMoves (tab-separated, one per line)
        /// </summary>
        public void LoadFromTsv(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("#")) continue; // Skip comments
                    if (line.StartsWith("fen\t", StringComparison.OrdinalIgnoreCase)) continue; // Skip header

                    var parts = line.Split('\t');
                    if (parts.Length < 3) continue;

                    string piecePlacement = ExtractPiecePlacement(parts[0]);
                    if (string.IsNullOrEmpty(piecePlacement)) continue;

                    string eco = parts.Length > 1 ? parts[1] : "";
                    string name = parts.Length > 2 ? parts[2] : "";
                    string moves = parts.Length > 3 ? parts[3] : "";

                    if (!string.IsNullOrEmpty(name) && !_openings.ContainsKey(piecePlacement))
                    {
                        _openings[piecePlacement] = new OpeningInfo
                        {
                            ECO = eco,
                            Name = name,
                            Moves = moves
                        };
                    }
                }

                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading opening database from {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the opening info for a position.
        /// </summary>
        /// <param name="fen">Full FEN or just piece placement</param>
        /// <returns>Opening info or null if not found</returns>
        public OpeningInfo? GetOpening(string fen)
        {
            string piecePlacement = ExtractPiecePlacement(fen);
            if (string.IsNullOrEmpty(piecePlacement)) return null;

            _openings.TryGetValue(piecePlacement, out var opening);
            return opening;
        }

        /// <summary>
        /// Gets just the opening name for a position.
        /// </summary>
        public string? GetOpeningName(string fen)
        {
            var opening = GetOpening(fen);
            if (opening == null) return null;

            // Format: "ECO: Name" or just "Name" if no ECO
            if (!string.IsNullOrEmpty(opening.ECO))
                return $"{opening.ECO}: {opening.Name}";
            return opening.Name;
        }

        /// <summary>
        /// Extracts the piece placement portion from a FEN string.
        /// </summary>
        private static string ExtractPiecePlacement(string fen)
        {
            if (string.IsNullOrEmpty(fen)) return "";

            // FEN format: "piece_placement active_color castling en_passant halfmove fullmove"
            // We only want the first part (piece placement)
            int spaceIndex = fen.IndexOf(' ');
            return spaceIndex > 0 ? fen.Substring(0, spaceIndex) : fen;
        }

        /// <summary>
        /// Clears all loaded openings.
        /// </summary>
        public void Clear()
        {
            _openings.Clear();
            _isLoaded = false;
        }
    }
}
