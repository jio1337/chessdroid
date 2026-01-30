using ChessDroid.Services;

namespace ChessDroid.Models
{
    /// <summary>
    /// Result of move classification analysis for a single move.
    /// </summary>
    public class MoveReviewResult
    {
        public MoveNode Node { get; set; } = null!;
        public string PlayedMove { get; set; } = "";
        public string BestMove { get; set; } = "";
        public double EvalBefore { get; set; }
        public double EvalAfter { get; set; }
        public double EvalBestMove { get; set; }
        public double CentipawnLoss { get; set; }
        public MoveQualityAnalyzer.MoveQuality Quality { get; set; }
        public string Symbol { get; set; } = "";
        public bool IsWhiteMove { get; set; }
    }

    /// <summary>
    /// Result of move classification with statistics.
    /// </summary>
    public class MoveClassificationResult
    {
        public string EngineName { get; set; } = "";
        public int EngineDepth { get; set; }
        public DateTime AnalysisDate { get; set; } = DateTime.Now;

        // Move-by-move results
        public List<MoveReviewResult> MoveResults { get; set; } = new();

        // Classification counts per side
        public Dictionary<MoveQualityAnalyzer.MoveQuality, int> WhiteCounts { get; set; } = new();
        public Dictionary<MoveQualityAnalyzer.MoveQuality, int> BlackCounts { get; set; } = new();

        // Move counts
        public int WhiteMoveCount { get; set; }
        public int BlackMoveCount { get; set; }

        /// <summary>
        /// Get total count for a specific classification across both sides.
        /// </summary>
        public int GetTotalCount(MoveQualityAnalyzer.MoveQuality quality)
        {
            int white = WhiteCounts.TryGetValue(quality, out var wc) ? wc : 0;
            int black = BlackCounts.TryGetValue(quality, out var bc) ? bc : 0;
            return white + black;
        }
    }
}
