using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Advanced analysis features - Win Rate Model, Opening History, Tablebase Integration
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
        // LOW-PLY HISTORY (Opening-specific patterns)
        // Inspired by Stockfish's low-ply history tracking
        // Tracks move success in early game (first 20 moves)
        // =============================

        private static Dictionary<string, int> openingMoveHistory = new Dictionary<string, int>();
        private static int totalOpeningMoves = 0;

        /// <summary>
        /// Track opening move (first 20 plies)
        /// </summary>
        public static void RecordOpeningMove(string move, int ply, bool wasGood)
        {
            if (ply > 20) return; // Only track opening moves

            string key = $"{move}_ply{ply}";

            if (!openingMoveHistory.ContainsKey(key))
                openingMoveHistory[key] = 0;

            if (wasGood)
                openingMoveHistory[key]++;

            totalOpeningMoves++;
        }

        /// <summary>
        /// Check if move is historically good in opening
        /// </summary>
        public static bool IsGoodOpeningMove(string move, int ply)
        {
            if (ply > 20) return false;

            string key = $"{move}_ply{ply}";

            if (openingMoveHistory.TryGetValue(key, out int successCount))
            {
                // If seen at least 5 times and success rate > 60%
                if (successCount >= 5 && totalOpeningMoves > 0)
                {
                    double successRate = (double)successCount / totalOpeningMoves;
                    return successRate > 0.6;
                }
            }

            return false;
        }

        /// <summary>
        /// Get opening move quality description
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

            // Check historical data
            if (IsGoodOpeningMove(move, ply))
                return "historically strong opening move";

            return null;
        }

        // =============================
        // ADVANCED TABLEBASE INTEGRATION
        // Inspired by Stockfish's Syzygy tablebase probing with DTZ ranking
        // =============================

        /// <summary>
        /// Simulate tablebase probe result
        /// (In real implementation, this would query actual Syzygy tablebases)
        /// </summary>
        public class TablebaseResult
        {
            public bool IsTablebasePosition { get; set; }
            public string Outcome { get; set; } = "Unknown";  // "Win", "Draw", "Loss"
            public int DTZ { get; set; }  // Distance to zeroing move (capture/pawn move)
            public int DTM { get; set; }  // Distance to mate (if winning)
            public string BestMove { get; set; } = "";
        }

        /// <summary>
        /// Check if position is in tablebase range
        /// </summary>
        public static bool IsTablebasePosition(ChessBoard board)
        {
            int totalPieces = EndgameAnalysis.CountTotalPieces(board);
            // Syzygy tablebases typically cover up to 7 pieces
            return totalPieces <= 7;
        }

        /// <summary>
        /// Simulate tablebase probe (real implementation would use Fathom library)
        /// </summary>
        public static TablebaseResult? ProbeTablebase(ChessBoard board, bool isWhite)
        {
            try
            {
                if (!IsTablebasePosition(board))
                    return null;

                // In real implementation, would call Syzygy tablebase library
                // For now, detect simple theoretical endgames

                int totalPieces = EndgameAnalysis.CountTotalPieces(board);

                // King vs King = draw
                if (totalPieces == 0)
                {
                    return new TablebaseResult
                    {
                        IsTablebasePosition = true,
                        Outcome = "Draw",
                        DTZ = 0,
                        DTM = 0
                    };
                }

                // King and Pawn vs King
                string? kpvk = EndgameAnalysis.DetectKPvK(board, isWhite);
                if (kpvk != null)
                {
                    return new TablebaseResult
                    {
                        IsTablebasePosition = true,
                        Outcome = "Win",
                        DTZ = 15,  // Approximate
                        DTM = 20   // Approximate
                    };
                }

                // Opposite-colored bishops
                string? oppBishops = EndgameAnalysis.DetectOppositeBishops(board);
                if (oppBishops != null)
                {
                    return new TablebaseResult
                    {
                        IsTablebasePosition = true,
                        Outcome = "Draw",
                        DTZ = 0,
                        DTM = 0
                    };
                }

                // Bare king
                string? bareKing = EndgameAnalysis.DetectBareKing(board, isWhite);
                if (bareKing != null)
                {
                    return new TablebaseResult
                    {
                        IsTablebasePosition = true,
                        Outcome = "Win",
                        DTZ = 10,
                        DTM = 10
                    };
                }

                // Default: unknown tablebase position
                return new TablebaseResult
                {
                    IsTablebasePosition = true,
                    Outcome = "Unknown",
                    DTZ = -1,
                    DTM = -1
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get tablebase explanation
        /// </summary>
        public static string? GetTablebaseExplanation(ChessBoard board, bool isWhite)
        {
            var result = ProbeTablebase(board, isWhite);
            if (result == null || !result.IsTablebasePosition)
                return null;

            if (result.Outcome == "Win")
            {
                if (result.DTM > 0)
                    return $"tablebase win in ~{result.DTM} moves";
                else
                    return "tablebase winning position";
            }
            else if (result.Outcome == "Draw")
            {
                return "tablebase draw";
            }
            else if (result.Outcome == "Loss")
            {
                return "tablebase losing position";
            }

            return null;
        }

        /// <summary>
        /// Rank tablebase moves by DTZ (Stockfish-style)
        /// Lower DTZ = better (faster to zeroing move)
        /// </summary>
        public static string? GetTablebaseMoveRanking(int dtz, string outcome)
        {
            if (outcome == "Draw")
                return "optimal drawing move";

            if (outcome == "Win")
            {
                if (dtz <= 5)
                    return "fastest winning move";
                else if (dtz <= 15)
                    return "optimal winning path";
                else
                    return "wins but not optimal";
            }

            if (outcome == "Loss")
            {
                return "delays loss longest";
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