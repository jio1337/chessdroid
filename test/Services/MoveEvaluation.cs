using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Advanced move evaluation inspired by Ethereal's movepicker.c and search.c
    /// Implements Static Exchange Evaluation (SEE) and move interestingness scoring
    /// </summary>
    public class MoveEvaluation
    {
        // =============================
        // STATIC EXCHANGE EVALUATION (SEE)
        // Inspired by Ethereal's movepicker.c
        // =============================
        //
        // SEE evaluates a capture by simulating all possible recaptures
        // Example: If we capture a rook (5) with a knight (3), but the knight gets captured:
        //   Initial: +5 (we capture rook)
        //   After recapture: +5 - 3 = +2 (they capture our knight)
        //   Result: SEE = +2 (we win the exchange)

        /// <summary>
        /// Evaluate static exchange on a square
        /// Returns the material gain/loss after all captures are resolved
        /// </summary>
        public static int StaticExchangeEvaluation(ChessBoard board, int targetRow, int targetCol, char attackingPiece, bool isWhite)
        {
            try
            {
                char targetPiece = board.GetPiece(targetRow, targetCol);

                // If capturing nothing, SEE is 0
                if (targetPiece == '.') return 0;

                PieceType attackerType = PieceHelper.GetPieceType(attackingPiece);
                PieceType victimType = PieceHelper.GetPieceType(targetPiece);

                int attackerValue = ChessUtilities.GetPieceValue(attackerType);
                int victimValue = ChessUtilities.GetPieceValue(victimType);

                // Initial capture value
                int gain = victimValue;

                // Create temporary board to simulate exchanges (using pooling)
                using var pooled = BoardPool.Rent(board);
                ChessBoard tempBoard = pooled.Board;

                // Find all attackers to this square (both sides)
                List<(int row, int col, char piece, int value)> attackers = GetAllAttackers(tempBoard, targetRow, targetCol);

                // Simulate captures alternating between sides
                bool currentSideIsWhite = isWhite;
                int currentAttackerValue = attackerValue;

                while (attackers.Any(a => char.IsUpper(a.piece) == currentSideIsWhite))
                {
                    // Find least valuable attacker of current side
                    var nextAttacker = attackers
                        .Where(a => char.IsUpper(a.piece) == currentSideIsWhite)
                        .OrderBy(a => a.value)
                        .FirstOrDefault();

                    if (nextAttacker.piece == default) break;

                    // Simulate capture
                    if (currentSideIsWhite)
                        gain += currentAttackerValue; // We lose our piece
                    else
                        gain -= currentAttackerValue; // Opponent loses their piece

                    // Remove this attacker
                    attackers.Remove(nextAttacker);

                    // Next attacker value becomes the current one
                    currentAttackerValue = nextAttacker.value;

                    // Switch sides
                    currentSideIsWhite = !currentSideIsWhite;

                    // If gain is already decided (side to move won't capture), stop
                    if (currentSideIsWhite && gain < 0) break;  // We're losing, stop
                    if (!currentSideIsWhite && gain > 0) break; // Opponent is losing, they stop
                }

                return gain;
            }
            catch
            {
                return 0; // If error, assume neutral
            }
        }

        /// <summary>
        /// Get all pieces attacking a square (for SEE calculation)
        /// </summary>
        private static List<(int row, int col, char piece, int value)> GetAllAttackers(
            ChessBoard board, int targetRow, int targetCol)
        {
            var attackers = new List<(int, int, char, int)>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, targetRow, targetCol))
                    {
                        PieceType type = PieceHelper.GetPieceType(piece);
                        int value = ChessUtilities.GetPieceValue(type);
                        attackers.Add((r, c, piece, value));
                    }
                }
            }

            return attackers;
        }

        /// <summary>
        /// Detect if a capture wins material using SEE
        /// Inspired by Ethereal's move picker filtering
        /// </summary>
        public static string? DetectWinningCapture(ChessBoard board, int srcRow, int srcCol,
            int destRow, int destCol, char movingPiece, char capturedPiece, bool isWhite)
        {
            try
            {
                if (capturedPiece == '.') return null;

                PieceType movingType = PieceHelper.GetPieceType(movingPiece);
                PieceType capturedType = PieceHelper.GetPieceType(capturedPiece);
                int movingValue = ChessUtilities.GetPieceValue(movingType);
                int capturedValue = ChessUtilities.GetPieceValue(capturedType);

                // Create board after capture (using pooling)
                using var pooledCapture = BoardPool.Rent(board);
                ChessBoard tempBoard = pooledCapture.Board;
                tempBoard.SetPiece(destRow, destCol, movingPiece);
                tempBoard.SetPiece(srcRow, srcCol, '.');

                // Check if destination square is defended by enemy
                bool squareDefended = ChessUtilities.IsSquareDefended(tempBoard, destRow, destCol, !isWhite);

                // Evaluate full exchange sequence
                int seeValue = StaticExchangeEvaluation(tempBoard, destRow, destCol, movingPiece, isWhite);

                // CRITICAL FIX: Distinguish between trades and wins
                // Without move history, we can't know if this is a recapture
                // But we can be smarter about what we report

                // Check if SEE values should be displayed
                bool showSEE = ExplanationFormatter.Features.ShowSEEValues;

                if (seeValue > 0)
                {
                    // We win material after full exchange
                    // But check if it's just a fair trade (equal pieces)
                    if (squareDefended && movingValue == capturedValue)
                    {
                        // Equal pieces trading - likely a fair trade, not a "win"
                        // Report as simple trade, not "wins"
                        return $"trades {ChessUtilities.GetPieceName(capturedType)}";
                    }
                    else
                    {
                        // Actually winning material
                        string seeInfo = showSEE ? $" (SEE +{seeValue})" : "";
                        return $"wins {ChessUtilities.GetPieceName(capturedType)}{seeInfo}";
                    }
                }
                else if (seeValue == 0)
                {
                    // Equal exchange
                    if (squareDefended)
                    {
                        // Defended piece, equal trade
                        return $"trades {ChessUtilities.GetPieceName(capturedType)}";
                    }
                    else
                    {
                        // Undefended but SEE = 0 (shouldn't happen often)
                        return $"captures {ChessUtilities.GetPieceName(capturedType)}";
                    }
                }
                else // seeValue < 0
                {
                    string seeInfo = showSEE ? " (loses exchange)" : "";
                    return $"captures {ChessUtilities.GetPieceName(capturedType)}{seeInfo}";
                }
            }
            catch
            {
                return null;
            }
        }

        // =============================
        // MOVE INTERESTINGNESS SCORING
        // Inspired by Ethereal's move ordering priorities
        // =============================
        //
        // Move Priority (highest to lowest):
        // 1. Hash table moves (from previous search)
        // 2. Good captures (passing SEE > 0)
        // 3. Killer moves (caused cutoffs in siblings)
        // 4. Quiet moves with good history
        // 5. Bad captures (failing SEE < 0)

        /// <summary>
        /// Score move "interestingness" based on Ethereal's move picker priorities
        /// Higher score = more interesting/forcing move
        /// </summary>
        public static int ScoreMoveInterestingness(ChessBoard board, int srcRow, int srcCol,
            int destRow, int destCol, char piece, bool isWhite)
        {
            try
            {
                int score = 0;
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                char targetPiece = board.GetPiece(destRow, destCol);
                bool isCapture = targetPiece != '.';

                // CHECKS (very forcing) - Priority 1
                using var pooledCheck = BoardPool.Rent(board);
                ChessBoard tempBoard = pooledCheck.Board;
                tempBoard.SetPiece(destRow, destCol, piece);
                tempBoard.SetPiece(srcRow, srcCol, '.');

                if (IsGivingCheck(tempBoard, destRow, destCol, piece, isWhite))
                {
                    score += 10000; // Checks are highly forcing
                }

                // CAPTURES - Priority 2
                if (isCapture)
                {
                    PieceType victimType = PieceHelper.GetPieceType(targetPiece);
                    int victimValue = ChessUtilities.GetPieceValue(victimType);
                    int attackerValue = ChessUtilities.GetPieceValue(pieceType);

                    // MVV-LVA (Most Valuable Victim - Least Valuable Attacker)
                    // Prefer capturing valuable pieces with cheap pieces
                    score += (victimValue * 10) - attackerValue;

                    // SEE adjustment
                    int seeValue = StaticExchangeEvaluation(tempBoard, destRow, destCol, piece, isWhite);
                    if (seeValue > 0)
                        score += 1000; // Good capture
                    else if (seeValue < 0)
                        score -= 5000; // Bad capture (loses material)
                }

                // PROMOTIONS - Priority 3
                if (pieceType == PieceType.Pawn)
                {
                    int promotionRank = isWhite ? 0 : 7;
                    if (destRow == promotionRank)
                        score += 8000; // Promotion to queen
                }

                // CENTRAL CONTROL - Priority 4
                if (destRow >= 3 && destRow <= 4 && destCol >= 3 && destCol <= 4)
                    score += 50; // Center squares

                // DEVELOPMENT - Priority 5
                if ((srcRow == 0 || srcRow == 7) && (pieceType == PieceType.Knight || pieceType == PieceType.Bishop))
                    score += 30;

                // CASTLING - Priority 6
                if (pieceType == PieceType.King && Math.Abs(destCol - srcCol) == 2)
                    score += 100;

                return score;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Classify move as forcing or quiet
        /// Inspired by Ethereal's noisy vs quiet move distinction
        /// </summary>
        public static string GetMoveCategory(ChessBoard board, int srcRow, int srcCol,
            int destRow, int destCol, char piece, bool isWhite)
        {
            try
            {
                char targetPiece = board.GetPiece(destRow, destCol);
                bool isCapture = targetPiece != '.';

                PieceType pieceType = PieceHelper.GetPieceType(piece);
                bool isPromotion = (pieceType == PieceType.Pawn && (destRow == 0 || destRow == 7));

                using var pooledNoisy = BoardPool.Rent(board);
                ChessBoard tempBoard = pooledNoisy.Board;
                tempBoard.SetPiece(destRow, destCol, piece);
                tempBoard.SetPiece(srcRow, srcCol, '.');
                bool isCheck = IsGivingCheck(tempBoard, destRow, destCol, piece, isWhite);

                // "Noisy" moves (forcing, tactical)
                if (isCapture || isPromotion || isCheck)
                    return "forcing";

                // "Quiet" moves (positional, strategic)
                return "quiet";
            }
            catch
            {
                return "unknown";
            }
        }

        // =============================
        // IMPROVING POSITION DETECTION
        // Inspired by Ethereal's search.c momentum tracking
        // =============================

        /// <summary>
        /// Detect if position is improving (gaining advantage)
        /// This requires comparing evaluation before and after move
        /// </summary>
        public static bool IsPositionImproving(double? currentEval, double? previousEval, bool isWhite)
        {
            if (!currentEval.HasValue || !previousEval.HasValue)
                return false;

            double change = currentEval.Value - previousEval.Value;

            // Position improving if eval change favors the side to move
            if (isWhite)
                return change > 0.3; // White's eval increased by 0.3+
            else
                return change < -0.3; // Black's eval improved (more negative)
        }

        /// <summary>
        /// Detect if position is worsening (losing advantage)
        /// </summary>
        public static bool IsPositionWorsening(double? currentEval, double? previousEval, bool isWhite)
        {
            if (!currentEval.HasValue || !previousEval.HasValue)
                return false;

            double change = currentEval.Value - previousEval.Value;

            // Position worsening if eval change goes against the side to move
            if (isWhite)
                return change < -0.3; // White's eval decreased by 0.3+
            else
                return change > 0.3; // Black's eval worsened (more positive)
        }

        // =============================
        // HELPER METHODS
        // =============================

        // Delegate to ChessUtilities
        private static bool IsGivingCheck(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
            => ChessUtilities.IsGivingCheck(board, pieceRow, pieceCol, piece, isWhite);
    }
}