using System.Text.Json;

namespace ChessDroid
{
    public class AppConfig
    {
        public string TemplatesFolder { get; set; } = "Templates";
        public string EnginesFolder { get; set; } = "Engines";
        public int MoveTimeoutMs { get; set; } = 30000;
        public int EngineResponseTimeoutMs { get; set; } = 10000;
        public int MaxEngineRetries { get; set; } = 3;
        public int EngineDepth { get; set; } = 15; // Default depth for engine analysis (1-20)
        public int MinAnalysisTimeMs { get; set; } = 500; // Minimum analysis time in milliseconds (0 = no minimum)
        public string Theme { get; set; } = "Dark"; // "Dark" or "Light"

        // Display settings
        public string SelectedEngine { get; set; } = ""; // Selected engine name

        public string SelectedSite { get; set; } = "Lichess"; // "Lichess" or "Chess.com"
        public bool ShowBestLine { get; set; } = true; // Show best line
        public bool ShowSecondLine { get; set; } = false; // Show 2nd best line
        public bool ShowThirdLine { get; set; } = false; // Show 3rd best line
        public bool ShowEngineArrows { get; set; } = true; // Show engine move arrows on board

        // Explanation settings
        public string ExplanationComplexity { get; set; } = "Intermediate"; // Beginner, Intermediate, Advanced, Master

        public bool ShowTacticalAnalysis { get; set; } = true; // Show tactical patterns
        public bool ShowPositionalAnalysis { get; set; } = true; // Show positional evaluation
        public bool ShowEndgameAnalysis { get; set; } = true; // Show endgame patterns
        public bool ShowOpeningPrinciples { get; set; } = true; // Show opening principles
        public bool ShowSEEValues { get; set; } = false; // SEE runs internally, no longer displayed
        public bool ShowThreats { get; set; } = true; // Show threats analysis
        public bool ShowWDL { get; set; } = true; // Show Win/Draw/Loss probabilities (Lc0-inspired)

        // Lc0-inspired features
        public bool PlayStyleEnabled { get; set; } = true; // Enable play style recommendations
        public int Aggressiveness { get; set; } = 50; // 0=Solid (avoid risk), 50=Balanced, 100=Aggressive (seek complications)
        public bool ShowOpeningName { get; set; } = true; // Show opening name when in known theory
        public bool ShowMoveQuality { get; set; } = true; // Show move quality indicators (brilliant, best, good, etc.)

        // Puzzle settings
        public int PuzzleMinRating { get; set; } = 800;
        public int PuzzleMaxRating { get; set; } = 2000;
        public string PuzzleThemeFilter { get; set; } = "All Themes";

        // Opening book settings (Polyglot .bin format)
        public string OpeningBooksFolder { get; set; } = "Books"; // Folder containing Polyglot .bin files (loads all)
        public bool UseOpeningBook { get; set; } = true; // Enable opening book move suggestions
        public bool ShowBookMoves { get; set; } = true; // Show book move suggestions in console

        private static readonly string ConfigFilePath = Path.Combine(
            Application.StartupPath, "config.json");

        // Thread synchronization for file I/O operations
        private static readonly object _fileLock = new object();

        public static AppConfig Load()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(ConfigFilePath))
                    {
                        string json = File.ReadAllText(ConfigFilePath);
                        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
                }

                // Create default config if doesn't exist
                var config = new AppConfig();
                config.SaveInternal(); // Use internal method since we already hold the lock
                return config;
            }
        }

        public void Save()
        {
            lock (_fileLock)
            {
                SaveInternal();
            }
        }

        // Internal save method that doesn't acquire lock (for use within already-locked context)
        private void SaveInternal()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        /// <summary>
        /// Reloads configuration from disk and updates this instance in-place.
        /// This ensures all services holding a reference to this config get updated values.
        /// </summary>
        public void Reload()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(ConfigFilePath))
                    {
                        string json = File.ReadAllText(ConfigFilePath);
                        var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                        if (loaded != null)
                        {
                            CopyFrom(loaded);
                            System.Diagnostics.Debug.WriteLine($"[Config] Reloaded - Aggressiveness: {Aggressiveness}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reloading config: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Copies all property values from another AppConfig instance.
        /// Used to update config in-place so all references stay valid.
        /// </summary>
        public void CopyFrom(AppConfig other)
        {
            TemplatesFolder = other.TemplatesFolder;
            EnginesFolder = other.EnginesFolder;
            MoveTimeoutMs = other.MoveTimeoutMs;
            EngineResponseTimeoutMs = other.EngineResponseTimeoutMs;
            MaxEngineRetries = other.MaxEngineRetries;
            EngineDepth = other.EngineDepth;
            MinAnalysisTimeMs = other.MinAnalysisTimeMs;
            Theme = other.Theme;
            SelectedEngine = other.SelectedEngine;
            SelectedSite = other.SelectedSite;
            ShowBestLine = other.ShowBestLine;
            ShowSecondLine = other.ShowSecondLine;
            ShowThirdLine = other.ShowThirdLine;
            ShowEngineArrows = other.ShowEngineArrows;
            ExplanationComplexity = other.ExplanationComplexity;
            ShowTacticalAnalysis = other.ShowTacticalAnalysis;
            ShowPositionalAnalysis = other.ShowPositionalAnalysis;
            ShowEndgameAnalysis = other.ShowEndgameAnalysis;
            ShowOpeningPrinciples = other.ShowOpeningPrinciples;
            ShowSEEValues = other.ShowSEEValues;
            ShowThreats = other.ShowThreats;
            ShowWDL = other.ShowWDL;
            PlayStyleEnabled = other.PlayStyleEnabled;
            Aggressiveness = other.Aggressiveness;
            ShowOpeningName = other.ShowOpeningName;
            ShowMoveQuality = other.ShowMoveQuality;
            PuzzleMinRating = other.PuzzleMinRating;
            PuzzleMaxRating = other.PuzzleMaxRating;
            PuzzleThemeFilter = other.PuzzleThemeFilter;
            OpeningBooksFolder = other.OpeningBooksFolder;
            UseOpeningBook = other.UseOpeningBook;
            ShowBookMoves = other.ShowBookMoves;
        }

        public string GetTemplatesPath() => ResolveFolderPath(TemplatesFolder);

        public string GetEnginesPath() => ResolveFolderPath(EnginesFolder);

        /// <summary>
        /// Resolves a folder path by trying multiple base locations.
        /// Returns the first existing path, or the default location if none exist.
        /// </summary>
        private static string ResolveFolderPath(string folderName)
        {
            string[] possiblePaths = new[]
            {
                Path.Combine(Application.StartupPath, folderName),
                Path.Combine(AppContext.BaseDirectory, folderName),
                Path.Combine(Directory.GetCurrentDirectory(), folderName),
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", folderName)
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            // If none found, return the first option (will fail later with better error message)
            return possiblePaths[0];
        }
    }
}