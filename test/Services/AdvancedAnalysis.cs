using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Advanced analysis features - Win Rate Model, Opening History
    /// Inspired by Stockfish's advanced evaluation and analysis features
    /// </summary>
    public class AdvancedAnalysis
    {
        // =============================
        // WIN RATE MODEL
        // Inspired by Stockfish's UCI_ShowWDL and win rate calculations
        // Converts centipawn evaluation to human-friendly win/draw/loss percentages
        // =============================

        /// <summary>
        /// Convert centipawn evaluation to winning percentage
        /// Based on Stockfish's win rate model with material-dependent parameters
        /// </summary>
        public static double EvalToWinningPercentage(double eval, int materialCount)
        {
            try
            {
                // Stockfish's win rate model uses logistic function
                // Formula: WinRate = 50 + 50 * (2 / (1 + exp(-eval * scale)) - 1)
                // Scale depends on material (more pieces = eval means less)

                // Material-dependent scaling (Stockfish uses complex formulas, we simplify)
                // More material = smaller eval changes mean less
                double scale = materialCount switch
                {
                    <= 10 => 0.0015,  // Endgame: small eval changes matter more
                    <= 20 => 0.0012,  // Transition: medium scaling
                    _ => 0.0010       // Opening/Middlegame: large eval changes needed
                };

                // Logistic function for win probability
                double winRate = 50.0 + 50.0 * (2.0 / (1.0 + Math.Exp(-eval * scale * 100)) - 1.0);

                // Clamp to [0, 100]
                return Math.Max(0.0, Math.Min(100.0, winRate));
            }
            catch
            {
                return 50.0; // Default to 50% if error
            }
        }

        /// <summary>
        /// Get Win/Draw/Loss percentages (Stockfish's WDL model)
        /// </summary>
        public static (double win, double draw, double loss) GetWDLPercentages(double eval, int materialCount)
        {
            try
            {
                double winRate = EvalToWinningPercentage(eval, materialCount);

                // Draw rate increases in endgames and balanced positions
                bool isEndgame = materialCount <= 10;
                double drawRate = 0.0;

                if (Math.Abs(eval) < 0.5)
                {
                    // Very balanced position
                    drawRate = isEndgame ? 40.0 : 30.0;
                }
                else if (Math.Abs(eval) < 1.5)
                {
                    // Slight advantage
                    drawRate = isEndgame ? 30.0 : 20.0;
                }
                else if (Math.Abs(eval) < 3.0)
                {
                    // Clear advantage
                    drawRate = isEndgame ? 15.0 : 10.0;
                }
                else
                {
                    // Decisive advantage
                    drawRate = isEndgame ? 5.0 : 3.0;
                }

                // Adjust win/loss based on draw rate
                double remainingProb = 100.0 - drawRate;
                double adjustedWin = (winRate / 100.0) * remainingProb;
                double adjustedLoss = remainingProb - adjustedWin;

                return (adjustedWin, drawRate, adjustedLoss);
            }
            catch
            {
                return (50.0, 0.0, 50.0);
            }
        }

        /// <summary>
        /// Format WDL as readable string
        /// </summary>
        public static string FormatWDL(double eval, int materialCount)
        {
            var (win, draw, loss) = GetWDLPercentages(eval, materialCount);

            if (eval > 0)
                return $"White: {win:F0}% win, {draw:F0}% draw, {loss:F0}% loss";
            else if (eval < 0)
                return $"Black: {loss:F0}% win, {draw:F0}% draw, {win:F0}% loss";
            else
                return $"Balanced: {draw:F0}% draw";
        }

        /// <summary>
        /// Get simple winning chance description
        /// </summary>
        public static string GetWinningChanceDescription(double eval, int materialCount)
        {
            double winRate = EvalToWinningPercentage(Math.Abs(eval), materialCount);

            if (winRate >= 95)
                return "winning position";
            else if (winRate >= 80)
                return "clearly better position";
            else if (winRate >= 65)
                return "advantage";
            else if (winRate >= 55)
                return "slight edge";
            else
                return "balanced position";
        }

        // =============================
        // OPENING MOVE PRINCIPLES
        // Basic opening principles for early moves
        // =============================

        /// <summary>
        /// Get opening move quality description based on common principles
        /// </summary>
        public static string? GetOpeningMoveDescription(string move, int ply)
        {
            if (ply > 20) return null;

            // Common opening principles (basic knowledge)
            if (ply <= 4)
            {
                // Central pawn moves
                if (move == "e2e4" || move == "d2d4" || move == "e7e5" || move == "d7d5")
                    return "strong opening principle (controls center)";

                // Knight development
                if (move == "g1f3" || move == "b1c3" || move == "g8f6" || move == "b8c6")
                    return "develops piece to good square";
            }

            return null;
        }

        // =============================
        // EVALUATION QUALITY INDICATORS
        // =============================

        /// <summary>
        /// Detect if evaluation is unstable (fluctuating)
        /// Indicates complex/unclear position
        /// </summary>
        public static bool IsEvaluationUnstable(List<double> recentEvaluations)
        {
            if (recentEvaluations == null || recentEvaluations.Count < 3)
                return false;

            // Check if recent evals fluctuate by more than 1.0 pawn
            double maxEval = recentEvaluations.Max();
            double minEval = recentEvaluations.Min();

            return (maxEval - minEval) > 1.0;
        }

        /// <summary>
        /// Detect sharp tactical position (large eval swings possible)
        /// </summary>
        public static bool IsSharpPosition(ChessBoard board, double currentEval)
        {
            try
            {
                // Sharp positions have:
                // 1. Many pieces still on board
                // 2. High material imbalance
                // 3. Exposed kings
                // 4. Unstable eval

                int totalPieces = EndgameAnalysis.CountTotalPieces(board);
                int materialBalance = Math.Abs(EndgameAnalysis.CalculateMaterialBalance(board));

                bool manyPieces = totalPieces >= 12;
                bool materialImbalance = materialBalance >= 3;
                bool unstableEval = Math.Abs(currentEval) > 2.0;

                return manyPieces && (materialImbalance || unstableEval);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get position complexity description
        /// </summary>
        public static string? GetComplexityDescription(ChessBoard board, double eval)
        {
            bool isSharp = IsSharpPosition(board, eval);
            bool isEndgame = EndgameAnalysis.IsEndgame(board);

            if (isSharp)
                return "sharp tactical position";
            else if (isEndgame)
                return "technical endgame";
            else
                return null;
        }
    }
}