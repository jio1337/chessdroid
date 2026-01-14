using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Tracks board changes and evaluation history for blunder detection
    /// Extracted from MainForm to centralize blunder tracking logic
    ///
    /// USAGE:
    /// - Call StartTracking() when user wants to track a game/session
    /// - Call StopTracking() when switching to puzzle solving or new position
    /// - Automatically detects invalid position jumps using smart heuristics
    /// </summary>
    public class BlunderTracker
    {
        private string lastAnalyzedFEN = "";
        private double? previousEvaluation = null;
        private bool isTrackingEnabled = false;

        /// <summary>
        /// Starts tracking blunders for a game/session
        /// Call this when user begins analyzing a continuous game
        /// </summary>
        public void StartTracking()
        {
            isTrackingEnabled = true;
            Reset(); // Clear any previous state
            Debug.WriteLine("BlunderTracker: Tracking ENABLED");
        }

        /// <summary>
        /// Stops tracking blunders
        /// Call this when switching to puzzle solving or manual position entry
        /// </summary>
        public void StopTracking()
        {
            isTrackingEnabled = false;
            Reset(); // Clear state to prevent false positives
            Debug.WriteLine("BlunderTracker: Tracking DISABLED");
        }

        /// <summary>
        /// Returns whether blunder tracking is currently enabled
        /// </summary>
        public bool IsTrackingEnabled()
        {
            return isTrackingEnabled;
        }

        /// <summary>
        /// Updates board change tracking and evaluation history
        /// Returns the previous evaluation for blunder detection
        /// Only tracks if enabled AND position change is valid (natural move)
        /// </summary>
        public double? UpdateBoardChangeTracking(string currentFEN, string currentEvaluation)
        {
            // Don't track if disabled
            if (!isTrackingEnabled)
            {
                return null;
            }

            try
            {
                // Extract just the position part of FEN (ignore move counters)
                string currentPosition = ChessNotationService.GetPositionFromFEN(currentFEN);
                string lastPosition = ChessNotationService.GetPositionFromFEN(lastAnalyzedFEN);

                // Check if board actually changed
                if (currentPosition != lastPosition)
                {
                    // Count differences between positions
                    int differences = CountPositionDifferences(currentPosition, lastPosition);

                    // Smart heuristic: Normal moves change 2-6 squares
                    // - Simple move: 2 squares (from + to)
                    // - Capture: 2 squares (from + to, piece removed)
                    // - Castling: 4 squares (king + rook move)
                    // - En passant: 3 squares
                    // - Promotion: 2 squares
                    // Anything > 6 is likely a position jump (puzzle/manual edit)
                    bool isNaturalMove = differences >= 2 && differences <= 6;

                    if (isNaturalMove)
                    {
                        // This looks like a valid game continuation
                        Debug.WriteLine($"BlunderTracker: Natural move detected ({differences} changes)");

                        // Parse current evaluation for next comparison
                        double? currentEval = MovesExplanation.ParseEvaluation(currentEvaluation);

                        // Only update previousEvaluation if we had a previous analysis
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
                    else
                    {
                        // Too many changes - likely new puzzle or manual position edit
                        Debug.WriteLine($"BlunderTracker: Position jump detected ({differences} changes) - resetting");
                        Reset(); // Reset to avoid false positives
                        lastAnalyzedFEN = currentFEN; // Still store current FEN for next comparison
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateBoardChangeTracking: {ex.Message}");
            }

            return previousEvaluation;
        }

        /// <summary>
        /// Counts the number of position differences between two FEN position strings
        /// Used to detect if position change is a natural move or a position jump
        /// </summary>
        private int CountPositionDifferences(string position1, string position2)
        {
            if (string.IsNullOrEmpty(position1) || string.IsNullOrEmpty(position2))
            {
                return int.MaxValue; // Treat as completely different
            }

            int differences = 0;

            // Compare character by character
            int minLength = Math.Min(position1.Length, position2.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (position1[i] != position2[i])
                {
                    differences++;
                }
            }

            // Add length difference (handles promotions, captures)
            differences += Math.Abs(position1.Length - position2.Length);

            return differences;
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