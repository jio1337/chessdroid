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
                catch { }
            }
            result.Sort((a, b) => b.SavedAt.CompareTo(a.SavedAt));
            return result;
        }

        public void Save(SavedGame game)
        {
            File.WriteAllText(
                Path.Combine(GamesFolder, $"{game.Id}.json"),
                JsonSerializer.Serialize(game, _jsonOptions));
        }

        public void Delete(string id)
        {
            string path = Path.Combine(GamesFolder, $"{id}.json");
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
