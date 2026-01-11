using System;
using System.IO;
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
        public bool ShowWinPercentage { get; set; } = true; // Show win percentage
        public bool ShowTablebaseInfo { get; set; } = true; // Show tablebase information
        public bool ShowMoveQualityColor { get; set; } = true; // Color-coded moves
        public bool ShowSEEValues { get; set; } = true; // Static Exchange Evaluation

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
