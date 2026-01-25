using System.Text.Json;

namespace ChessDroid
{
    public class AppConfig
    {
        public string TemplatesFolder { get; set; } = "Templates";
        public string EnginesFolder { get; set; } = "Engines";
        public double MatchThreshold { get; set; } = 0.55;
        public int CannyThresholdLow { get; set; } = 50;
        public int CannyThresholdHigh { get; set; } = 150;
        public int MinBoardArea { get; set; } = 5000;
        public int MoveTimeoutMs { get; set; } = 30000;
        public int EngineResponseTimeoutMs { get; set; } = 10000;
        public int MaxEngineRetries { get; set; } = 3;
        public int EngineDepth { get; set; } = 15; // Default depth for engine analysis (1-20)
        public int MinAnalysisTimeMs { get; set; } = 500; // Minimum analysis time in milliseconds (0 = no minimum)
        public string Theme { get; set; } = "Dark"; // "Dark" or "Light"
        public bool ShowDebugCells { get; set; } = false; // Show cell recognition popup

        // Display settings
        public string SelectedEngine { get; set; } = ""; // Selected engine name

        public string SelectedSite { get; set; } = "Lichess"; // "Lichess" or "Chess.com"
        public bool ShowBestLine { get; set; } = true; // Show best line
        public bool ShowSecondLine { get; set; } = false; // Show 2nd best line
        public bool ShowThirdLine { get; set; } = false; // Show 3rd best line

        // Explanation settings
        public string ExplanationComplexity { get; set; } = "Intermediate"; // Beginner, Intermediate, Advanced, Master

        public bool ShowTacticalAnalysis { get; set; } = true; // Show tactical patterns
        public bool ShowPositionalAnalysis { get; set; } = true; // Show positional evaluation
        public bool ShowEndgameAnalysis { get; set; } = true; // Show endgame patterns
        public bool ShowOpeningPrinciples { get; set; } = true; // Show opening principles
        public bool ShowSEEValues { get; set; } = true; // Static Exchange Evaluation
        public bool ShowThreats { get; set; } = true; // Show threats analysis
        public bool ShowWDL { get; set; } = true; // Show Win/Draw/Loss probabilities (Lc0-inspired)

        // Auto-monitoring settings
        public bool AutoMonitorBoard { get; set; } = false; // Enable continuous board monitoring

        // Lc0-inspired features
        public int Aggressiveness { get; set; } = 50; // 0=Solid (avoid risk), 50=Balanced, 100=Aggressive (seek complications)
        public bool ShowOpeningName { get; set; } = true; // Show opening name when in known theory
        public bool ShowMoveQuality { get; set; } = true; // Show move quality indicators (brilliant, best, good, etc.)

        // Opening book settings (Polyglot .bin format)
        public string OpeningBooksFolder { get; set; } = "Books"; // Folder containing Polyglot .bin files (loads all)
        public bool UseOpeningBook { get; set; } = false; // Enable opening book move suggestions
        public bool ShowBookMoves { get; set; } = true; // Show book move suggestions in console

        private static readonly string ConfigFilePath = Path.Combine(
            Application.StartupPath, "config.json");

        public static AppConfig Load()
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
            config.Save();
            return config;
        }

        public void Save()
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

        /// <summary>
        /// Copies all property values from another AppConfig instance.
        /// Used to update config in-place so all references stay valid.
        /// </summary>
        public void CopyFrom(AppConfig other)
        {
            TemplatesFolder = other.TemplatesFolder;
            EnginesFolder = other.EnginesFolder;
            MatchThreshold = other.MatchThreshold;
            CannyThresholdLow = other.CannyThresholdLow;
            CannyThresholdHigh = other.CannyThresholdHigh;
            MinBoardArea = other.MinBoardArea;
            MoveTimeoutMs = other.MoveTimeoutMs;
            EngineResponseTimeoutMs = other.EngineResponseTimeoutMs;
            MaxEngineRetries = other.MaxEngineRetries;
            EngineDepth = other.EngineDepth;
            MinAnalysisTimeMs = other.MinAnalysisTimeMs;
            Theme = other.Theme;
            ShowDebugCells = other.ShowDebugCells;
            SelectedEngine = other.SelectedEngine;
            SelectedSite = other.SelectedSite;
            ShowBestLine = other.ShowBestLine;
            ShowSecondLine = other.ShowSecondLine;
            ShowThirdLine = other.ShowThirdLine;
            ExplanationComplexity = other.ExplanationComplexity;
            ShowTacticalAnalysis = other.ShowTacticalAnalysis;
            ShowPositionalAnalysis = other.ShowPositionalAnalysis;
            ShowEndgameAnalysis = other.ShowEndgameAnalysis;
            ShowOpeningPrinciples = other.ShowOpeningPrinciples;
            ShowSEEValues = other.ShowSEEValues;
            ShowThreats = other.ShowThreats;
            ShowWDL = other.ShowWDL;
            AutoMonitorBoard = other.AutoMonitorBoard;
            Aggressiveness = other.Aggressiveness;
            ShowOpeningName = other.ShowOpeningName;
            ShowMoveQuality = other.ShowMoveQuality;
            OpeningBooksFolder = other.OpeningBooksFolder;
            UseOpeningBook = other.UseOpeningBook;
            ShowBookMoves = other.ShowBookMoves;
        }

        public string GetTemplatesPath()
        {
            // Try multiple locations
            string[] possiblePaths = new[]
            {
                Path.Combine(Application.StartupPath, TemplatesFolder),
                Path.Combine(AppContext.BaseDirectory, TemplatesFolder),
                Path.Combine(Directory.GetCurrentDirectory(), TemplatesFolder),
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", TemplatesFolder)
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

        public string GetEnginesPath()
        {
            // Try multiple locations
            string[] possiblePaths = new[]
            {
                Path.Combine(Application.StartupPath, EnginesFolder),
                Path.Combine(AppContext.BaseDirectory, EnginesFolder),
                Path.Combine(Directory.GetCurrentDirectory(), EnginesFolder),
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", EnginesFolder)
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