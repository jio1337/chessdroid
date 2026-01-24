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
        /// Uses negamax-style SEE algorithm with proper alternating sides
        /// ENHANCED: Now check-aware and pin-aware for more accurate evaluation
        /// </summary>
        public static int StaticExchangeEvaluation(ChessBoard board, int targetRow, int targetCol, char attackingPiece, bool isWhite, int srcRow = -1, int srcCol = -1)
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

                // Build gain array for each capture depth
                // gain[d] = material balance after d captures
                List<int> gains = new List<int>(16);
                gains.Add(victimValue); // Initial capture gains the victim

                // Create temporary board to simulate the initial capture
                using var pooled = BoardPool.Rent(board);
                ChessBoard tempBoard = pooled.Board;

                // Simulate the initial capture - remove attacker from source, put on target
                if (srcRow >= 0 && srcCol >= 0)
                {
                    tempBoard.SetPiece(srcRow, srcCol, '.'); // Remove from source
                }
                tempBoard.SetPiece(targetRow, targetCol, attackingPiece);

                // CHECK-AWARE SEE: If the initial capture gives check, opponent may not be able to recapture
                bool captureGivesCheck = ChessUtilities.IsGivingCheck(tempBoard, targetRow, targetCol, attackingPiece, isWhite);

                // Find all attackers to this square AFTER the initial capture
                List<(int row, int col, char piece, int value)> attackers = GetAllAttackers(tempBoard, targetRow, targetCol);

                // PIN-AWARE SEE: Filter out pinned pieces that can't legally recapture
                attackers = FilterPinnedAttackers(tempBoard, attackers, targetRow, targetCol);

                // If capture gives check, filter out attackers that can't deal with check while recapturing
                if (captureGivesCheck)
                {
                    attackers = FilterAttackersInCheck(tempBoard, attackers, targetRow, targetCol, !isWhite);
                }

                // Start with opponent's turn (they can recapture)
                bool currentSideIsWhite = !isWhite;
                int pieceOnTarget = attackerValue; // Our piece is now on the target
                char pieceCharOnTarget = attackingPiece;

                int depth = 0;
                while (attackers.Any(a => char.IsUpper(a.piece) == currentSideIsWhite))
                {
                    depth++;

                    // Find least valuable attacker of current side that can legally capture
                    var nextAttacker = attackers
                        .Where(a => char.IsUpper(a.piece) == currentSideIsWhite)
                        .OrderBy(a => a.value)
                        .FirstOrDefault();

                    if (nextAttacker.piece == default) break;

                    // Calculate gain at this depth: capture the piece on target, minus accumulated gain
                    gains.Add(pieceOnTarget - gains[depth - 1]);

                    // Simulate this capture on the temp board
                    tempBoard.SetPiece(nextAttacker.row, nextAttacker.col, '.');
                    tempBoard.SetPiece(targetRow, targetCol, nextAttacker.piece);

                    // The capturing piece is now on the target
                    pieceOnTarget = nextAttacker.value;
                    pieceCharOnTarget = nextAttacker.piece;

                    // Remove this attacker from the list
                    attackers.Remove(nextAttacker);

                    // CHECK-AWARE: Does this recapture give check?
                    bool thisRecaptureGivesCheck = ChessUtilities.IsGivingCheck(tempBoard, targetRow, targetCol, pieceCharOnTarget, currentSideIsWhite);

                    // Switch sides
                    currentSideIsWhite = !currentSideIsWhite;

                    // If this recapture gives check, filter remaining attackers
                    if (thisRecaptureGivesCheck)
                    {
                        attackers = FilterAttackersInCheck(tempBoard, attackers, targetRow, targetCol, currentSideIsWhite);
                    }

                    // Re-filter for pins after board state changed
                    attackers = FilterPinnedAttackers(tempBoard, attackers, targetRow, targetCol);

                    // Alpha-beta style pruning: if the side to move is already ahead,
                    // they might not want to continue the exchange
                    if (Math.Max(-gains[depth], gains[depth - 1]) < 0)
                        break;
                }

                // Propagate back: each side chooses the best outcome
                // gains[d-1] = -max(-gains[d], gains[d-1])
                // Simplified: gains[d-1] = min(-gains[d], gains[d-1])
                for (int d = depth; d >= 1; d--)
                {
                    gains[d - 1] = -Math.Max(-gains[d], gains[d - 1]);
                }

                return gains[0];
            }
            catch
            {
                return 0; // If error, assume neutral
            }
        }

        /// <summary>
        /// Filter out attackers that are pinned to their king and can't legally capture
        /// </summary>
        private static List<(int row, int col, char piece, int value)> FilterPinnedAttackers(
            ChessBoard board,
            List<(int row, int col, char piece, int value)> attackers,
            int targetRow, int targetCol)
        {
            var legalAttackers = new List<(int row, int col, char piece, int value)>();

            foreach (var attacker in attackers)
            {
                // Check if this piece is pinned
                if (!IsPiecePinnedToKing(board, attacker.row, attacker.col, attacker.piece, targetRow, targetCol))
                {
                    legalAttackers.Add(attacker);
                }
            }

            return legalAttackers;
        }

        /// <summary>
        /// Filter out attackers whose side is in check and can't recapture while dealing with check
        /// </summary>
        private static List<(int row, int col, char piece, int value)> FilterAttackersInCheck(
            ChessBoard board,
            List<(int row, int col, char piece, int value)> attackers,
            int targetRow, int targetCol,
            bool sideInCheckIsWhite)
        {
            // Find the king position for the side in check
            char king = sideInCheckIsWhite ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8 && kingRow < 0; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(r, c) == king)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
            }

            if (kingRow < 0) return attackers; // King not found, return all

            var legalAttackers = new List<(int row, int col, char piece, int value)>();

            foreach (var attacker in attackers)
            {
                // Only filter attackers of the side in check
                bool attackerIsWhite = char.IsUpper(attacker.piece);
                if (attackerIsWhite != sideInCheckIsWhite)
                {
                    legalAttackers.Add(attacker);
                    continue;
                }

                // Simulate the recapture and check if king is still in check
                using var pooled = BoardPool.Rent(board);
                ChessBoard testBoard = pooled.Board;
                testBoard.SetPiece(attacker.row, attacker.col, '.');
                testBoard.SetPiece(targetRow, targetCol, attacker.piece);

                // Is the king still attacked after this recapture?
                if (!ChessUtilities.IsSquareAttackedBy(testBoard, kingRow, kingCol, !sideInCheckIsWhite))
                {
                    // This recapture also deals with the check - it's legal
                    legalAttackers.Add(attacker);
                }
            }

            return legalAttackers;
        }

        /// <summary>
        /// Check if a piece is pinned to its king and can't move to the target square
        /// </summary>
        private static bool IsPiecePinnedToKing(ChessBoard board, int pieceRow, int pieceCol, char piece, int targetRow, int targetCol)
        {
            bool isWhite = char.IsUpper(piece);
            char king = isWhite ? 'K' : 'k';

            // Find our king
            int kingRow = -1, kingCol = -1;
            for (int r = 0; r < 8 && kingRow < 0; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(r, c) == king)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
            }

            if (kingRow < 0) return false; // King not found

            // Simulate moving this piece to the target
            using var pooled = BoardPool.Rent(board);
            ChessBoard testBoard = pooled.Board;
            testBoard.SetPiece(pieceRow, pieceCol, '.');
            testBoard.SetPiece(targetRow, targetCol, piece);

            // Check if our king is now attacked (would be illegal)
            return ChessUtilities.IsSquareAttackedBy(testBoard, kingRow, kingCol, !isWhite);
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

                // Check if destination square is defended by enemy (on post-capture board)
                bool squareDefended = ChessUtilities.IsSquareDefended(tempBoard, destRow, destCol, !isWhite);

                // Evaluate full exchange sequence on ORIGINAL board
                // (SEE needs to see the target piece on the square to calculate its value)
                // Pass source position so SEE can remove the attacker from its source
                int seeValue = StaticExchangeEvaluation(board, destRow, destCol, movingPiece, isWhite, srcRow, srcCol);

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
                    // Don't say "loses exchange" - it's misleading for best moves
                    // SEE doesn't account for checks, tactics, or strategic compensation
                    // The engine evaluation already tells the full story
                    return $"captures {ChessUtilities.GetPieceName(capturedType)}";
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

                    // SEE adjustment - use original board so SEE can see the target piece
                    int seeValue = StaticExchangeEvaluation(board, destRow, destCol, piece, isWhite, srcRow, srcCol);
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