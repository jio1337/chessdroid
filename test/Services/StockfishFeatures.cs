using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Advanced chess analysis features inspired by Stockfish engine
    /// Implements threat detection, move quality classification, and singular move detection
    /// </summary>
    public class StockfishFeatures
    {
        // =============================
        // THREAT ARRAY DETECTION
        // Inspired by Stockfish's movepick.cpp threat array construction
        // =============================

        /// <summary>
        /// Build threat array - map of squares attacked by lower-value pieces
        /// This identifies tactical vulnerabilities
        /// PERFORMANCE: Optimized with BoardCache - O(n²) instead of O(n³)
        /// </summary>
        public static Dictionary<(int row, int col), List<(int attackerRow, int attackerCol, PieceType attackerType)>>
            BuildThreatArray(ChessBoard board, bool forWhite)
        {
            var threats = new Dictionary<(int, int), List<(int, int, PieceType)>>();

            // CRITICAL OPTIMIZATION: Use BoardCache to get pieces instead of scanning entire board
            // This reduces complexity from O(n³) to O(n²)
            var cache = new BoardCache(board);
            var pieces = cache.GetPieces(forWhite);

            // For each friendly piece, find all squares it attacks
            foreach (var (fromR, fromC, piece) in pieces)
            {
                PieceType attackerType = PieceHelper.GetPieceType(piece);

                // Find all squares this piece attacks
                for (int toR = 0; toR < 8; toR++)
                {
                    for (int toC = 0; toC < 8; toC++)
                    {
                        if (ChessUtilities.CanAttackSquare(board, fromR, fromC, piece, toR, toC))
                        {
                            var key = (toR, toC);
                            if (!threats.ContainsKey(key))
                                threats[key] = new List<(int, int, PieceType)>();

                            threats[key].Add((fromR, fromC, attackerType));
                        }
                    }
                }
            }

            return threats;
        }

        /// <summary>
        /// Detect if a piece is attacked by lower-value pieces (Stockfish threat detection)
        /// Example: Queen attacked by pawn = major threat
        /// </summary>
        public static string? DetectLowerValueThreat(ChessBoard board, int targetRow, int targetCol,
            char targetPiece, bool isWhite)
        {
            try
            {
                if (targetPiece == '.') return null;

                PieceType targetType = PieceHelper.GetPieceType(targetPiece);
                int targetValue = ChessUtilities.GetPieceValue(targetType);

                // Build threat array for opponent
                var threats = BuildThreatArray(board, !isWhite);
                var key = (targetRow, targetCol);

                if (!threats.ContainsKey(key)) return null;

                // Check if any attacker has lower value
                var lowerValueAttackers = threats[key]
                    .Where(attacker => ChessUtilities.GetPieceValue(attacker.attackerType) < targetValue)
                    .ToList();

                if (lowerValueAttackers.Count > 0)
                {
                    // Find the lowest value attacker
                    var lowestAttacker = lowerValueAttackers
                        .OrderBy(a => ChessUtilities.GetPieceValue(a.attackerType))
                        .First();

                    string attackerName = ChessUtilities.GetPieceName(lowestAttacker.attackerType);
                    string targetName = ChessUtilities.GetPieceName(targetType);

                    return $"{targetName} attacked by {attackerName}";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect if move creates threat on valuable piece with lower-value piece
        /// </summary>
        public static string? DetectThreatCreation(ChessBoard originalBoard, ChessBoard afterMoveBoard,
            int movedToRow, int movedToCol, char movedPiece, bool isWhite)
        {
            try
            {
                PieceType movedType = PieceHelper.GetPieceType(movedPiece);
                int movedValue = ChessUtilities.GetPieceValue(movedType);

                // Check what the moved piece now attacks
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char targetPiece = afterMoveBoard.GetPiece(r, c);
                        if (targetPiece == '.') continue;
                        if (char.IsUpper(targetPiece) == isWhite) continue; // Skip own pieces

                        PieceType targetType = PieceHelper.GetPieceType(targetPiece);
                        int targetValue = ChessUtilities.GetPieceValue(targetType);

                        // Only report if we're attacking something more valuable
                        if (targetValue > movedValue)
                        {
                            if (ChessUtilities.CanAttackSquare(afterMoveBoard, movedToRow, movedToCol, movedPiece, r, c))
                            {
                                // Check if this piece was NOT attacked before
                                bool wasAttackedBefore = ChessUtilities.IsSquareDefended(originalBoard, r, c, isWhite);

                                if (!wasAttackedBefore)
                                {
                                    return $"creates threat on {ChessUtilities.GetPieceName(targetType)}";
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // =============================
        // MOVE QUALITY CLASSIFICATION
        // Inspired by Stockfish's razoring and futility pruning
        // =============================

        public enum MoveQuality
        {
            Excellent,      // Clearly best move (singular, winning)
            Good,           // Solid move, maintains/improves position
            Marginal,       // Borderline move, small improvement
            Bad,            // Loses material or worsens position
            Terrible        // Blunder-level move
        }

        /// <summary>
        /// Classify move quality based on evaluation change and tactical features
        /// Inspired by Stockfish's move classification
        /// </summary>
        public static MoveQuality ClassifyMoveQuality(double? evalBefore, double? evalAfter,
            bool isWhite, bool hasTacticalFeature, bool isSingular = false)
        {
            // Singular moves (only good move) are excellent
            if (isSingular)
                return MoveQuality.Excellent;

            // Moves with strong tactical features are at least good
            if (hasTacticalFeature)
                return MoveQuality.Good;

            if (!evalBefore.HasValue || !evalAfter.HasValue)
                return MoveQuality.Good; // Default to good if no eval

            // Calculate evaluation change from perspective of side to move
            double evalChange = isWhite
                ? (evalAfter.Value - evalBefore.Value)  // White wants eval to increase
                : (evalBefore.Value - evalAfter.Value); // Black wants eval to decrease (more negative)

            // Classification thresholds (in pawns)
            if (evalChange >= 1.0)
                return MoveQuality.Excellent;  // Major improvement
            else if (evalChange >= 0.3)
                return MoveQuality.Good;       // Clear improvement
            else if (evalChange >= -0.3)
                return MoveQuality.Marginal;   // Neutral or slight improvement
            else if (evalChange >= -1.0)
                return MoveQuality.Bad;        // Loses advantage
            else
                return MoveQuality.Terrible;   // Blunder
        }

        /// <summary>
        /// Get human-readable quality description
        /// </summary>
        public static string GetQualityDescription(MoveQuality quality)
        {
            return quality switch
            {
                MoveQuality.Excellent => "excellent move",
                MoveQuality.Good => "good move",
                MoveQuality.Marginal => "marginal move",
                MoveQuality.Bad => "questionable move",
                MoveQuality.Terrible => "poor move",
                _ => "move"
            };
        }

        // =============================
        // SINGULAR MOVE DETECTION
        // Inspired by Stockfish's singular extension logic
        // =============================

        /// <summary>
        /// Detect if this is a singular move (only good move in position)
        /// Based on evaluation difference between best move and second-best move
        /// </summary>
        public static bool IsSingularMove(List<string> pvLines, string evaluation, string secondEvaluation)
        {
            try
            {
                // Need at least 2 PV lines to compare
                if (pvLines == null || pvLines.Count < 2)
                    return false;

                double? bestEval = MovesExplanation.ParseEvaluation(evaluation);
                double? secondEval = MovesExplanation.ParseEvaluation(secondEvaluation);

                if (!bestEval.HasValue || !secondEval.HasValue)
                    return false;

                // Singular if gap between best and second-best is large (>= 1.5 pawns)
                double gap = Math.Abs(bestEval.Value - secondEval.Value);

                // Stockfish uses depth-dependent margins, we'll use a simpler threshold
                return gap >= 1.5;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Detect if move is forced (in check with only one legal move)
        /// </summary>
        public static bool IsForcedMove(ChessBoard board, bool isWhite)
        {
            try
            {
                // Check if in check - find king using O(1) cached position
                var (kingRow, kingCol) = board.GetKingPosition(isWhite);
                if (kingRow < 0) return false;

                // Check if king is under attack
                bool inCheck = ChessUtilities.IsSquareDefended(board, kingRow, kingCol, !isWhite);

                // If in check, count legal moves (simplified - just count king moves)
                if (inCheck)
                {
                    int legalMoves = 0;

                    // Check king moves
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr == 0 && dc == 0) continue;

                            int newR = kingRow + dr;
                            int newC = kingCol + dc;

                            if (newR < 0 || newR >= 8 || newC < 0 || newC >= 8) continue;

                            char destPiece = board.GetPiece(newR, newC);
                            if (destPiece != '.' && char.IsUpper(destPiece) == isWhite) continue;

                            // Check if this square is safe
                            if (!ChessUtilities.IsSquareDefended(board, newR, newC, !isWhite))
                                legalMoves++;
                        }
                    }

                    return legalMoves == 1;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // =============================
        // FUTILITY CLASSIFICATION
        // Inspired by Stockfish's futility pruning margins
        // =============================

        /// <summary>
        /// Classify move based on futility - does it improve position enough?
        /// </summary>
        public static string? DetectFutility(double? evalChange, bool hasTacticalFeature)
        {
            // Skip tactical moves
            if (hasTacticalFeature)
                return null;

            if (!evalChange.HasValue)
                return null;

            // Futility thresholds (Stockfish uses complex margins, we simplify)
            if (Math.Abs(evalChange.Value) < 0.1)
                return "move doesn't improve position significantly";
            else if (evalChange.Value < -0.5)
                return "move worsens position";

            return null;
        }

        // =============================
        // HELPER METHODS
        // =============================

        // Helper methods moved to ChessUtilities for code reuse
    }
}