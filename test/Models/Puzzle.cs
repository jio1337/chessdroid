namespace ChessDroid.Models
{
    /// <summary>
    /// Represents a single puzzle from the Lichess puzzle database.
    /// </summary>
    public class Puzzle
    {
        /// <summary>Lichess puzzle ID (e.g., "009Wb")</summary>
        public string PuzzleId { get; set; } = "";

        /// <summary>Starting FEN position (before opponent's setup move)</summary>
        public string FEN { get; set; } = "";

        /// <summary>All UCI moves: first is opponent's setup move, rest are the solution</summary>
        public string[] Moves { get; set; } = Array.Empty<string>();

        /// <summary>Glicko-2 rating (400-3300)</summary>
        public int Rating { get; set; }

        /// <summary>Theme tags (e.g., "fork", "middlegame", "short")</summary>
        public string[] Themes { get; set; } = Array.Empty<string>();

        // --- Derived properties ---

        /// <summary>The opponent's setup move (applied to FEN to create puzzle position)</summary>
        public string SetupMove => Moves.Length > 0 ? Moves[0] : "";

        /// <summary>The solution moves the user must find (odd indices: 1, 3, 5...)</summary>
        public string[] UserMoves => Moves.Where((_, i) => i > 0 && i % 2 == 1).ToArray();

        /// <summary>The opponent's response moves played automatically (even indices: 2, 4, 6...)</summary>
        public string[] OpponentResponses => Moves.Where((_, i) => i > 0 && i % 2 == 0).ToArray();

        /// <summary>Total number of moves the user must find</summary>
        public int SolutionLength => UserMoves.Length;

        /// <summary>Whether this is a mate puzzle</summary>
        public bool IsMate => Themes.Any(t => t.StartsWith("mateIn") || t == "mate");

        /// <summary>Rating category for display</summary>
        public string RatingCategory => Rating switch
        {
            < 800 => "Beginner",
            < 1200 => "Easy",
            < 1600 => "Intermediate",
            < 2000 => "Advanced",
            < 2400 => "Hard",
            _ => "Expert"
        };
    }

    /// <summary>
    /// Tracks user puzzle solving statistics, persisted to puzzle_stats.json.
    /// </summary>
    public class PuzzleStats
    {
        public int TotalAttempted { get; set; }
        public int TotalSolved { get; set; }
        public int TotalFailed { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public int HintsUsed { get; set; }
        public int EstimatedRating { get; set; } = 1200;

        public Dictionary<string, ThemeStats> ThemePerformance { get; set; } = new();
        public HashSet<string> AttemptedPuzzleIds { get; set; } = new();

        public double SuccessRate => TotalAttempted > 0
            ? (double)TotalSolved / TotalAttempted * 100 : 0;
    }

    /// <summary>
    /// Per-theme puzzle performance tracking.
    /// </summary>
    public class ThemeStats
    {
        public int Attempted { get; set; }
        public int Solved { get; set; }
        public double SuccessRate => Attempted > 0 ? (double)Solved / Attempted * 100 : 0;
    }
}
