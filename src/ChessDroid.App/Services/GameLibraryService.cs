using System.Text.Json;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    public class GameLibraryService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public string GamesFolder { get; }

        public GameLibraryService(string appFolder)
        {
            GamesFolder = Path.Combine(appFolder, "Games");
            Directory.CreateDirectory(GamesFolder);
        }

        public List<SavedGame> LoadAll()
        {
            var result = new List<SavedGame>();
            foreach (var file in Directory.GetFiles(GamesFolder, "*.json"))
            {
                try
                {
                    var game = JsonSerializer.Deserialize<SavedGame>(File.ReadAllText(file), _jsonOptions);
                    if (game != null) result.Add(game);
                }
                catch (Exception ex)
                {
                    CrashLogger.LogException(ex, $"Error loading saved game '{file}'");
                    QuarantineCorruptFile(file);
                }
            }
            result.Sort((a, b) => b.SavedAt.CompareTo(a.SavedAt));
            return result;
        }

        // The file exists but failed to load — quarantine it instead of silently skipping it,
        // so a corrupt save never just vanishes from the library with no trace (same reasoning
        // as AppConfig.QuarantineCorruptConfig for config.json).
        private static void QuarantineCorruptFile(string path)
        {
            try
            {
                string quarantinePath = Path.Combine(
                    Path.GetDirectoryName(path) ?? ".",
                    $"{Path.GetFileNameWithoutExtension(path)}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.Move(path, quarantinePath, overwrite: false);
            }
            catch (Exception ex)
            {
                CrashLogger.LogException(ex, $"Failed to quarantine corrupt saved game '{path}'");
            }
        }

        // Write-then-replace instead of a direct WriteAllText, same reasoning as
        // AppConfig.SaveInternal — a crash/power-loss mid-write must never leave a
        // truncated .json in place of a previously-good saved game.
        public void Save(SavedGame game)
        {
            string path = Path.Combine(GamesFolder, $"{game.Id}.json");
            string tempPath = path + ".tmp";
            string json = JsonSerializer.Serialize(game, _jsonOptions);
            try
            {
                File.WriteAllText(tempPath, json);
                if (File.Exists(path))
                    File.Replace(tempPath, path, null);
                else
                    File.Move(tempPath, path);
            }
            catch (Exception ex)
            {
                CrashLogger.LogException(ex, $"Error saving game '{path}'");
            }
        }

        public SavedGame? Load(string id)
        {
            string path = Path.Combine(GamesFolder, $"{id}.json");
            if (!File.Exists(path)) return null;
            try
            {
                return JsonSerializer.Deserialize<SavedGame>(File.ReadAllText(path), _jsonOptions);
            }
            catch (Exception ex)
            {
                CrashLogger.LogException(ex, $"Error loading saved game '{path}'");
                QuarantineCorruptFile(path);
                return null;
            }
        }

        public void Delete(string id)
        {
            string path = Path.Combine(GamesFolder, $"{id}.json");
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>
        /// Saves raw PGN games (e.g. picked from a multi-game PGN file) as library entries,
        /// skipping any whose players and moves already match a library game, including
        /// duplicates within <paramref name="pgns"/> itself. Games keep their input order in the
        /// newest-first library list.
        /// </summary>
        public (int saved, int skipped) ImportPgns(IReadOnlyList<string> pgns)
        {
            var known = new HashSet<string>(LoadAll().Select(g => DuplicateKey(g.White, g.Black, g.Pgn)));
            DateTime baseTime = DateTime.Now;
            int saved = 0, skipped = 0;

            for (int i = 0; i < pgns.Count; i++)
            {
                string pgn = pgns[i].Trim();
                var headers = PgnGamePickerDialog.ParsePgnHeaders(pgn);
                string white = headers.GetValueOrDefault("White", "?");
                string black = headers.GetValueOrDefault("Black", "?");
                if (!known.Add(DuplicateKey(white, black, pgn))) { skipped++; continue; }

                Save(new SavedGame
                {
                    White = white,
                    Black = black,
                    Event = headers.GetValueOrDefault("Event", "Chess Analysis"),
                    Result = headers.GetValueOrDefault("Result", "*"),
                    Opening = DetectOpening(pgn),
                    Pgn = pgn,
                    // Newest-first list: step back a millisecond per game to keep file order.
                    SavedAt = baseTime.AddMilliseconds(-i),
                });
                saved++;
            }
            return (saved, skipped);
        }

        /// <summary>Players + bare SAN moves: formatting, comments and annotations don't make a game new.</summary>
        internal static string DuplicateKey(string white, string black, string pgn)
            => $"{white}\n{black}\n{string.Join(' ', SanMoves(pgn))}";

        /// <summary>Main-line SAN tokens of a PGN: no headers, comments, variations, NAGs, numbers or result.</summary>
        internal static List<string> SanMoves(string pgn)
        {
            string body = string.Join('\n', pgn.Split('\n').Where(l => !l.TrimStart().StartsWith('[')
                                                                    || !l.Contains('"')));
            body = System.Text.RegularExpressions.Regex.Replace(body, @"\{[^}]*\}|;[^\n]*", " ");
            // Strip variations, innermost first.
            string prev;
            do { prev = body; body = System.Text.RegularExpressions.Regex.Replace(body, @"\([^()]*\)", " "); } while (body != prev);
            return body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => System.Text.RegularExpressions.Regex.Replace(t, @"^\d+\.+", ""))
                .Where(t => t != "" && !t.StartsWith('$') && !t.StartsWith('[')
                            && t is not ("1-0" or "0-1" or "1/2-1/2" or "*"))
                .ToList();
        }

        /// <summary>Deepest named opening reached in the first 30 plies, or "".</summary>
        private static string DetectOpening(string pgn)
        {
            try
            {
                var sans = SanMoves(pgn);
                string opening = "";
                for (int k = 1; k <= Math.Min(30, sans.Count); k++)
                {
                    string o = OpeningCatalog.GetOpeningDisplay(ChessNotationService.GetFenAfterSanMoves(string.Join(' ', sans.Take(k))));
                    if (!string.IsNullOrEmpty(o)) opening = o;
                }
                return opening;
            }
            catch { return ""; }
        }
    }
}
