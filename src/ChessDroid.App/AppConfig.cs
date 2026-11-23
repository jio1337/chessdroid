using System.Text.Json;

namespace ChessDroid
{
    public class EngineProfile
    {
        public string DisplayName { get; set; } = "";
        public int Elo { get; set; }              // CCRL reference rating, manually set
        public int ChessdroidElo { get; set; }    // Dynamic rating from in-app matches; 0 = not yet seeded
        public int GamesPlayed { get; set; }      // Games played inside Chessdroid
    }

    public class TrainingPersonalBest
    {
        public int BestCorrect { get; set; }
        public int BestQuestions { get; set; }
        public double BestTime { get; set; } = double.MaxValue;
        public double BestTimePerQuestion { get; set; } = double.MaxValue; // BestTime/BestCorrect — comparable across round sizes
        public string LastSet { get; set; } = ""; // ISO date "yyyy-MM-dd" when this PB was last beaten
    }

    public class OpeningStats
    {
        public int TotalRuns { get; set; }
        public int PerfectRuns { get; set; }
        public int TotalHintsUsed { get; set; }
        public int TotalMistakes { get; set; }       // cumulative wrong-move attempts across all runs
        public int BestRunMistakes { get; set; } = int.MaxValue; // min wrong attempts in a single run; 0 = clean run achieved
        public string LastAttempted { get; set; } = "";   // ISO date "yyyy-MM-dd"
    }

    public class AppConfig
    {
        private static readonly JsonSerializerOptions SaveJsonOptions = new() { WriteIndented = true };

        public string TemplatesFolder { get; set; } = "Templates";
        public string EnginesFolder { get; set; } = "Engines";
        public int MoveTimeoutMs { get; set; } = 30000;
        public int EngineResponseTimeoutMs { get; set; } = 10000;
        public int MaxEngineRetries { get; set; } = 3;
        public int EngineDepth { get; set; } = 15; // Default depth for engine analysis (1-20)
        public string Theme { get; set; } = "Dark"; // "Dark" or "Light"

        // Display settings
        public string SelectedEngine { get; set; } = ""; // Selected engine name

        public string SelectedSite { get; set; } = "Lichess"; // "Lichess" or "Chess.com"
        public bool ShowBestLine { get; set; } = true; // Show best line
        public bool ShowSecondLine { get; set; } // Show 2nd best line
        public bool ShowThirdLine { get; set; } // Show 3rd best line
        public bool ShowEngineArrows { get; set; } = true; // Kept for JSON backward compatibility
        public int EngineArrowCount { get; set; } = 1;    // 0=none, 1=best only, 2=best+2nd, 3=all 3
        public bool ShowEvalBar { get; set; } = true; // Show evaluation bar

        // Board square colors (hex strings e.g. "#F0D9B5")
        public string LightSquareColor { get; set; } = "#F0D9B5";
        public string DarkSquareColor { get; set; } = "#B58863";
        public bool MonochromeBoard { get; set; }
        public bool ShowSquareLabels { get; set; }
        public bool ShowCoordinates { get; set; } = true;

        // Visual aids
        public bool ShowThreatArrows { get; set; }
        public bool ShowBookArrows { get; set; } = true;
        public bool ShowEvalGraph { get; set; } = true;
        public bool ShowAnimations { get; set; } = true;
        public int AnimationDurationMs { get; set; } = 150;
        public bool ShowLastMoveHighlight { get; set; } = true;
        public bool ShowMaterialStrips { get; set; } = true;
        public bool ShowLegalMoves { get; set; } = true;

        // Piece movement input mode: "Both", "Drag", "Click"
        public string MovementMode { get; set; } = "Both";

        // Board visual effects
        public bool GradientBoard { get; set; }
        public bool BoardVignette { get; set; }
        public int  VignetteAlpha { get; set; } = 110;
        public bool PieceGlow { get; set; }
        public bool BoardFrame { get; set; }
        public int BoardFrameWidth { get; set; } = 26;
        public string BoardFrameColor { get; set; } = "#503219";

        // Sound effects
        public bool SoundEffectsEnabled { get; set; } = true;

        // Training records
        public int PuzzleRushBest { get; set; } // kept for backward compat; prefer PuzzleRushBestByMinutes
        public Dictionary<int, int> PuzzleRushBestByMinutes { get; set; } = new(); // keyed by duration in minutes (1–5)
        public int PuzzleTrainingBestStreak { get; set; }
        public string PuzzleTrainingBestStreakDate { get; set; } = "";
        public int PuzzleTrainingTotalAttempted { get; set; }
        public int PuzzleTrainingTotalClean { get; set; }
        public int GauntletBestStreak { get; set; }
        public string GauntletBestStreakDate { get; set; } = "";
        public List<int> GauntletRecentScores { get; set; } = new(); // last 5 session scores
        public int VisionBestStreak { get; set; }
        public bool PuzzleAutoNext { get; set; } = true;
        public string DailyPuzzleLastSolvedDate { get; set; } = "";
        public int    DailyPuzzleStreak         { get; set; }
        public int    DailyPuzzleBestStreak     { get; set; }
        public int    DailyPuzzleCleanSolves    { get; set; }
        public bool VisionAutoNext { get; set; } = true;

        // Explanation settings
        public bool ShowTacticalAnalysis { get; set; } = true; // Show tactical patterns
        public bool ShowPositionalAnalysis { get; set; } = true; // Show positional evaluation
        public bool ShowEndgameAnalysis { get; set; } = true; // Show endgame patterns
        public bool ShowOpeningPrinciples { get; set; } = true; // Show opening principles
        public bool ShowSEEValues { get; set; } // SEE runs internally, no longer displayed
        public bool ShowThreats { get; set; } = true; // Show threats analysis
        public bool ShowWDL { get; set; } = true; // Show Win/Draw/Loss probabilities (Lc0-inspired)

        // Lc0-inspired features
        public bool PlayStyleEnabled { get; set; } = true; // Enable play style recommendations
        public int Aggressiveness { get; set; } = 50; // 0=Solid (avoid risk), 50=Balanced, 100=Aggressive (seek complications)
        public bool ShowOpeningName { get; set; } = true; // Show opening name when in known theory
        public bool ShowMoveQuality { get; set; } = true; // Show move quality indicators (brilliant, best, good, etc.)


        // Analysis mode
        public bool ContinuousAnalysis { get; set; } // true = go infinite live updates, false = fixed depth
        public bool ShowExplanations { get; set; } = true;    // false = raw depth header + clickable PVs only
        public int AutoPlayInterval { get; set; } = 600; // ms between moves during auto-play (200-2000)

        // Console font settings
        public string ConsoleFontFamily { get; set; } = "Consolas";
        public float ConsoleFontSize { get; set; } = 10.0f;

        // Syzygy endgame tablebases (offline — Stockfish probes these files directly)
        public string SyzygyPath { get; set; } = ""; // Path to folder containing .rtbw/.rtbz files

        // Opening book settings (Polyglot .bin format)
        public string OpeningBooksFolder { get; set; } = "Books"; // Folder containing Polyglot .bin files (loads all)
        public bool UseOpeningBook { get; set; } = true; // Enable opening book move suggestions
        public bool ShowBookMoves { get; set; } = true; // Show book move suggestions in console

        // Engine profiles: keyed by engine filename (e.g. "stockfish18.exe")
        public Dictionary<string, EngineProfile> EngineProfiles { get; set; } = new();

        // Square training personal bests: keyed by mode string e.g. "Challenge-Black"
        public Dictionary<string, TrainingPersonalBest> TrainingPersonalBests { get; set; } = new();

        // Opening training lifetime stats: keyed by "{eco}|{name}"
        public Dictionary<string, OpeningStats> OpeningTrainingStats { get; set; } = new();

        // UI layout
        public int BoardSplitterDistance { get; set; } = -1; // -1 = compute from board height
        public int SplitterDistance { get; set; } = -1; // -1 = use default (130px)
        public string PanelLayout { get; set; } = "BMA"; // slot order: B=Board M=Moves A=Analysis

        private static readonly string ConfigFilePath = Path.Combine(
            Application.StartupPath, "config.json");
        private static readonly string BackupFilePath = ConfigFilePath + ".bak";
        private static readonly string TempFilePath = ConfigFilePath + ".tmp";

        // Thread synchronization for file I/O operations
        private static readonly object _fileLock = new object();

        public static AppConfig Load()
        {
            lock (_fileLock)
            {
                if (File.Exists(ConfigFilePath))
                {
                    try
                    {
                        string json = File.ReadAllText(ConfigFilePath);
                        var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                        if (loaded != null)
                        {
                            // Good load — refresh the backup so it reflects the latest known-good state.
                            TryCopy(ConfigFilePath, BackupFilePath);
                            return loaded;
                        }
                    }
                    catch (Exception ex)
                    {
                        Services.CrashLogger.LogException(ex, "Error loading config.json");
                    }

                    // The file exists but failed to load. Quarantine it instead of overwriting it —
                    // a corrupt/truncated file is still the user's data and must never be destroyed.
                    QuarantineCorruptConfig();
                }

                // Nothing valid at ConfigFilePath. Try the last known-good backup before giving up.
                if (File.Exists(BackupFilePath))
                {
                    try
                    {
                        string backupJson = File.ReadAllText(BackupFilePath);
                        var recovered = JsonSerializer.Deserialize<AppConfig>(backupJson);
                        if (recovered != null)
                        {
                            recovered.SaveInternal();
                            return recovered;
                        }
                    }
                    catch (Exception ex)
                    {
                        Services.CrashLogger.LogException(ex, "Error loading config.json.bak during recovery");
                    }
                }

                // No usable config anywhere — fall back to defaults.
                var config = new AppConfig();
                config.SaveInternal(); // Use internal method since we already hold the lock
                return config;
            }
        }

        private static void QuarantineCorruptConfig()
        {
            try
            {
                string quarantinePath = Path.Combine(
                    Path.GetDirectoryName(ConfigFilePath) ?? ".",
                    $"config.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.Move(ConfigFilePath, quarantinePath, overwrite: false);
            }
            catch (Exception ex)
            {
                Services.CrashLogger.LogException(ex, "Failed to quarantine corrupt config.json");
            }
        }

        private static void TryCopy(string source, string destination)
        {
            try
            {
                File.Copy(source, destination, overwrite: true);
            }
            catch (Exception ex)
            {
                Services.CrashLogger.LogException(ex, "Failed to refresh config.json.bak");
            }
        }

        public void Save()
        {
            lock (_fileLock)
            {
                SaveInternal();
            }
        }

        // Internal save method that doesn't acquire lock (for use within already-locked context).
        // Writes to a temp file first and atomically replaces config.json, so a crash or power
        // loss mid-write can never leave a truncated/corrupt file in its place.
        private void SaveInternal()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, SaveJsonOptions);
                File.WriteAllText(TempFilePath, json);

                if (File.Exists(ConfigFilePath))
                {
                    File.Replace(TempFilePath, ConfigFilePath, null);
                }
                else
                {
                    File.Move(TempFilePath, ConfigFilePath);
                }
            }
            catch (Exception ex)
            {
                Services.CrashLogger.LogException(ex, "Error saving config.json");
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
            Theme = other.Theme;
            SelectedEngine = other.SelectedEngine;
            SelectedSite = other.SelectedSite;
            ShowBestLine = other.ShowBestLine;
            ShowSecondLine = other.ShowSecondLine;
            ShowThirdLine = other.ShowThirdLine;
            ShowEngineArrows = other.ShowEngineArrows;
            EngineArrowCount = other.EngineArrowCount;
            ShowEvalBar = other.ShowEvalBar;
            LightSquareColor = other.LightSquareColor;
            DarkSquareColor = other.DarkSquareColor;
            MonochromeBoard = other.MonochromeBoard;
            ShowSquareLabels = other.ShowSquareLabels;
            ShowCoordinates = other.ShowCoordinates;
            ShowThreatArrows = other.ShowThreatArrows;
            ShowBookArrows = other.ShowBookArrows;
            ShowEvalGraph = other.ShowEvalGraph;
            ShowAnimations = other.ShowAnimations;
            AnimationDurationMs = other.AnimationDurationMs;
            ShowLastMoveHighlight = other.ShowLastMoveHighlight;
            ShowMaterialStrips = other.ShowMaterialStrips;
            ShowLegalMoves = other.ShowLegalMoves;
            MovementMode = other.MovementMode;
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
            ContinuousAnalysis = other.ContinuousAnalysis;
            ShowExplanations = other.ShowExplanations;
            AutoPlayInterval = other.AutoPlayInterval;
            ConsoleFontFamily = other.ConsoleFontFamily;
            ConsoleFontSize = other.ConsoleFontSize;

            GradientBoard = other.GradientBoard;
            BoardVignette = other.BoardVignette;
            VignetteAlpha = other.VignetteAlpha;
            PieceGlow = other.PieceGlow;
            BoardFrame = other.BoardFrame;
            BoardFrameWidth = other.BoardFrameWidth;
            BoardFrameColor = other.BoardFrameColor;
            SoundEffectsEnabled = other.SoundEffectsEnabled;
            PuzzleRushBest                 = other.PuzzleRushBest;
            PuzzleRushBestByMinutes        = new Dictionary<int, int>(other.PuzzleRushBestByMinutes);
            PuzzleTrainingBestStreak       = other.PuzzleTrainingBestStreak;
            PuzzleTrainingBestStreakDate   = other.PuzzleTrainingBestStreakDate;
            PuzzleTrainingTotalAttempted   = other.PuzzleTrainingTotalAttempted;
            PuzzleTrainingTotalClean       = other.PuzzleTrainingTotalClean;
            GauntletBestStreak             = other.GauntletBestStreak;
            GauntletBestStreakDate         = other.GauntletBestStreakDate;
            GauntletRecentScores           = new List<int>(other.GauntletRecentScores);
            VisionBestStreak               = other.VisionBestStreak;
            PuzzleAutoNext                 = other.PuzzleAutoNext;
            VisionAutoNext                 = other.VisionAutoNext;
            DailyPuzzleLastSolvedDate      = other.DailyPuzzleLastSolvedDate;
            DailyPuzzleStreak              = other.DailyPuzzleStreak;
            DailyPuzzleBestStreak          = other.DailyPuzzleBestStreak;
            DailyPuzzleCleanSolves         = other.DailyPuzzleCleanSolves;

            SyzygyPath = other.SyzygyPath;
            OpeningBooksFolder = other.OpeningBooksFolder;
            UseOpeningBook = other.UseOpeningBook;
            ShowBookMoves = other.ShowBookMoves;
            EngineProfiles = new Dictionary<string, EngineProfile>(other.EngineProfiles);
            TrainingPersonalBests = new Dictionary<string, TrainingPersonalBest>(other.TrainingPersonalBests);
            OpeningTrainingStats  = new Dictionary<string, OpeningStats>(other.OpeningTrainingStats);
            BoardSplitterDistance = other.BoardSplitterDistance;
            SplitterDistance = other.SplitterDistance;
            PanelLayout = other.PanelLayout;
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