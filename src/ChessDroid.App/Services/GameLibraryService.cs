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

        public void Save(SavedGame game)
        {
            File.WriteAllText(
                Path.Combine(GamesFolder, $"{game.Id}.json"),
                JsonSerializer.Serialize(game, _jsonOptions));
        }

        public SavedGame? Load(string id)
        {
            string path = Path.Combine(GamesFolder, $"{id}.json");
            if (!File.Exists(path)) return null;
            try { return JsonSerializer.Deserialize<SavedGame>(File.ReadAllText(path), _jsonOptions); }
            catch { return null; }
        }

        public void Delete(string id)
        {
            string path = Path.Combine(GamesFolder, $"{id}.json");
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
