using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Tracks board changes and evaluation history for blunder detection
    /// Extracted from MainForm to centralize blunder tracking logic
    /// </summary>
    public class BlunderTracker
    {
        private string lastAnalyzedFEN = "";
        private double? previousEvaluation = null;

        /// <summary>
        /// Updates board change tracking and evaluation history
        /// Returns the previous evaluation for blunder detection
        /// </summary>
        public double? UpdateBoardChangeTracking(string currentFEN, string currentEvaluation)
        {
            try
            {
                // Extract just the position part of FEN (ignore move counters)
                string currentPosition = ChessNotationService.GetPositionFromFEN(currentFEN);
                string lastPosition = ChessNotationService.GetPositionFromFEN(lastAnalyzedFEN);

                // Check if board actually changed
                if (currentPosition != lastPosition)
                {
                    // Board changed! This is a new move
                    Debug.WriteLine($"Board changed detected: {lastPosition} -> {currentPosition}");

                    // Parse current evaluation for next comparison
                    double? currentEval = MovesExplanation.ParseEvaluation(currentEvaluation);

                    // Only update previousEvaluation if we had a previous analysis
                    // This ensures we're tracking consecutive moves properly
                    if (!string.IsNullOrEmpty(lastAnalyzedFEN) && currentEval.HasValue)
                    {
                        previousEvaluation = currentEval;
                    }
                    else if (currentEval.HasValue)
                    {
                        // First move - just store it, don't compare yet
                        previousEvaluation = currentEval;
                    }

                    // Update last analyzed position
                    lastAnalyzedFEN = currentFEN;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateBoardChangeTracking: {ex.Message}");
            }

            return previousEvaluation;
        }

        /// <summary>
        /// Gets the previous evaluation for blunder detection
        /// </summary>
        public double? GetPreviousEvaluation()
        {
            return previousEvaluation;
        }

        /// <summary>
        /// Sets the previous evaluation (used after blunder detection)
        /// </summary>
        public void SetPreviousEvaluation(double? evaluation)
        {
            previousEvaluation = evaluation;
        }

        /// <summary>
        /// Resets the tracker (used when resetting the application)
        /// </summary>
        public void Reset()
        {
            lastAnalyzedFEN = "";
            previousEvaluation = null;
        }
    }
}