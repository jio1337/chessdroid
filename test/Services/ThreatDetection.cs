using ChessDroid.Models;
using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Detects chess threats for both sides in a position.
    ///
    /// Types of threats detected:
    /// 1. Direct Attacks (Material): Attacking unprotected/higher-value pieces that can't escape
    /// 2. Tactical Threats: Forks, skewers, pins, discovered attacks
    /// 3. Checkmate Threats: Moves that set up mate-in-one or forced mate
    /// 4. Positional Threats: Passed pawns, promotion threats
    /// </summary>
    public static class ThreatDetection
    {
        /// <summary>
        /// Represents a detected threat
        /// </summary>
        public class Threat
        {
            public string Description { get; set; } = "";
            public ThreatType Type { get; set; }
            public int Severity { get; set; } // 1-5, higher = more severe
            public string Square { get; set; } = ""; // e.g., "e4"
        }

        public enum ThreatType
        {
            HangingPiece,       // Undefended piece under attack that can't escape
            MaterialWin,        // Attacking higher value piece
            Fork,               // Attacking multiple pieces
            Pin,                // Pinning a piece
            Skewer,             // Attacking through a piece
            DiscoveredAttack,   // Moving piece reveals attack
            CheckmateThreat,    // Threatening mate
            Check,              // Giving check
            PassedPawn,         // Creating/advancing passed pawn
            Promotion,          // Pawn about to promote
            TrappedPiece,       // Piece with no escape squares
            EnPassant           // En passant capture available
        }

        /// <summary>
        /// Analyzes NEW threats WE create after making the best move.
        /// Only returns threats that didn't exist before the move.
        /// </summary>
        /// <param name="board">The current board position</param>
        /// <param name="move">The UCI move to analyze</param>
        /// <param name="movingPlayerIsWhite">Whether white is making the move</param>
        /// <param name="enPassantSquare">The en passant target square from FEN (e.g., "e3") or "-" if none</param>
        public static List<Threat> AnalyzeThreatsAfterMove(ChessBoard board, string move, bool movingPlayerIsWhite, string enPassantSquare = "-")
        {
            var threatsAfter = new List<Threat>();

            try
            {
                // Parse the move
                if (move.Length < 4) return threatsAfter;

                int srcFile = move[0] - 'a';
                int srcRank = 8 - (move[1] - '0');
                int destFile = move[2] - 'a';
                int destRank = 8 - (move[3] - '0');

                // First, get threats that exist BEFORE the move (current position)
                var threatsBefore = new List<Threat>();
                DetectRealHangingPieces(board, movingPlayerIsWhite, threatsBefore);
                DetectPins(board, movingPlayerIsWhite, threatsBefore);
                DetectPromotionThreats(board, movingPlayerIsWhite, threatsBefore);
                // Note: Don't include check/forks before - we want to show those created by the move

                // Create board after the move (using pooling)
                using var pooled = BoardPool.Rent(board);
                ChessBoard afterMove = pooled.Board;
                char movingPiece = afterMove.GetPiece(srcRank, srcFile);
                afterMove.SetPiece(destRank, destFile, movingPiece);
                afterMove.SetPiece(srcRank, srcFile, '.');

                // Handle promotion
                if (move.Length > 4)
                {
                    char promotionPiece = movingPlayerIsWhite ? char.ToUpper(move[4]) : char.ToLower(move[4]);
                    afterMove.SetPiece(destRank, destFile, promotionPiece);
                }

                // Check if the moving piece will be immediately recaptured
                // This covers: 1) gives check and must be captured, 2) captured on defended square
                // If so, most threats are "phantom" - they won't materialize
                bool pieceWillBeRecaptured = IsPieceImmediatelyRecapturable(afterMove, destRank, destFile, movingPiece, movingPlayerIsWhite, board);

                // Create BoardCache for afterMove board - O(n) once instead of O(64) per detection method
                var afterMoveCache = new BoardCache(afterMove);

                // Detect various threats AFTER our move
                // Pass pieceWillBeRecaptured to skip phantom threats
                DetectCheckThreats(afterMove, destRank, destFile, movingPlayerIsWhite, threatsAfter, pieceWillBeRecaptured);

                // Skip phantom threats if piece gives check and will be recaptured
                // These threats won't materialize because opponent MUST recapture to escape check
                if (!pieceWillBeRecaptured)
                {
                    DetectRealHangingPieces(afterMove, movingPlayerIsWhite, threatsAfter);
                    DetectForks(afterMove, destRank, destFile, movingPlayerIsWhite, threatsAfter);
                }

                DetectPins(afterMove, movingPlayerIsWhite, threatsAfter, afterMoveCache);
                DetectPromotionThreats(afterMove, movingPlayerIsWhite, threatsAfter);

                // Skip trapped piece detection if our piece will be recaptured
                if (!pieceWillBeRecaptured)
                {
                    DetectTrappedPieces(afterMove, movingPlayerIsWhite, threatsAfter);
                }

                // Detect if we have an en passant capture available (using the EP square from the FEN)
                DetectEnPassantThreat(board, enPassantSquare, movingPlayerIsWhite, threatsAfter);

                // Filter out threats that already existed before the move
                // A threat is "new" if its description didn't exist in threatsBefore
                var beforeDescriptions = new HashSet<string>(threatsBefore.Select(t => t.Description));
                threatsAfter = threatsAfter.Where(t => !beforeDescriptions.Contains(t.Description)).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ThreatDetection error: {ex.Message}");
            }

            // Sort by severity (highest first) and take top threats, deduplicate
            return threatsAfter
                .GroupBy(t => t.Description)
                .Select(g => g.First())
                .OrderByDescending(t => t.Severity)
                .Take(3)
                .ToList();
        }

        /// <summary>
        /// Analyzes threats the OPPONENT currently has against us (before we move).
        /// This checks what the opponent could do on their NEXT move if we don't address it.
        /// </summary>
        /// <param name="board">The current board position</param>
        /// <param name="weAreWhite">Whether we are playing white</param>
        /// <param name="enPassantSquare">The en passant target square from FEN (e.g., "e3") or "-" if none</param>
        public static List<Threat> AnalyzeOpponentThreats(ChessBoard board, bool weAreWhite, string enPassantSquare = "-")
        {
            var threats = new List<Threat>();

            try
            {
                bool opponentIsWhite = !weAreWhite;

                // Create BoardCache once - O(n) instead of multiple O(64) scans
                var cache = new BoardCache(board);

                // Check if our king is in check
                DetectCheckThreatsAgainstUs(board, weAreWhite, threats);

                // Check for opponent pieces that can capture our valuable pieces on their next move
                DetectOpponentCaptureThreats(board, opponentIsWhite, threats);

                // Check for opponent forks (their piece attacking 2+ of our valuable pieces)
                DetectOpponentForks(board, opponentIsWhite, threats);

                // Check for opponent promotion threats
                DetectOpponentPromotionThreats(board, opponentIsWhite, threats);

                // Check for pins against us - use cache for O(n) sliding piece lookup
                DetectPinsAgainstUs(board, weAreWhite, threats, cache);

                // Check if opponent can capture our pawn en passant
                DetectOpponentEnPassantThreat(board, enPassantSquare, weAreWhite, threats);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ThreatDetection opponent error: {ex.Message}");
            }

            return threats
                .GroupBy(t => t.Description)
                .Select(g => g.First())
                .OrderByDescending(t => t.Severity)
                .Take(3)
                .ToList();
        }

        #region Our Threat Detection Methods

        // Delegate to ChessUtilities
        private static bool IsPieceImmediatelyRecapturable(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite, ChessBoard? originalBoard = null)
            => ChessUtilities.IsPieceImmediatelyRecapturable(board, pieceRow, pieceCol, piece, isWhite, originalBoard);

        /// <summary>
        /// Detect if the move gives check or threatens mate
        /// </summary>
        private static void DetectCheckThreats(ChessBoard board, int pieceRow, int pieceCol,
            bool movingPlayerIsWhite, List<Threat> threats, bool pieceWillBeRecaptured = false)
        {
            char piece = board.GetPiece(pieceRow, pieceCol);

            // Find opponent's king - O(1) using cached position
            var (kingRow, kingCol) = board.GetKingPosition(!movingPlayerIsWhite);
            if (kingRow < 0) return;

            // Check if we're giving check
            if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, kingRow, kingCol))
            {
                threats.Add(new Threat
                {
                    Description = "gives check",
                    Type = ThreatType.Check,
                    Severity = 4,
                    Square = GetSquareName(kingRow, kingCol)
                });

                // Check if it could be checkmate threat (king has few escape squares)
                // Skip if piece will be recaptured - no real mate threat
                if (!pieceWillBeRecaptured)
                {
                    int escapeSquares = CountKingEscapeSquares(board, kingRow, kingCol, !movingPlayerIsWhite);
                    if (escapeSquares <= 1)
                    {
                        threats.Add(new Threat
                        {
                            Description = "threatens checkmate",
                            Type = ThreatType.CheckmateThreat,
                            Severity = 5,
                            Square = GetSquareName(kingRow, kingCol)
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Detect REAL hanging pieces - pieces that are attacked, undefended, AND cannot escape.
        /// A queen that can simply move away is NOT a real threat.
        /// </summary>
        private static void DetectRealHangingPieces(ChessBoard board, bool attackerIsWhite, List<Threat> threats)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);

                    // Look for enemy pieces (that we attack)
                    if (pieceIsWhite == attackerIsWhite) continue;

                    // Check if this piece is attacked by us
                    bool isAttacked = IsSquareAttackedBy(board, r, c, attackerIsWhite);
                    if (!isAttacked) continue;

                    // Check if it's defended
                    bool isDefended = IsSquareAttackedBy(board, r, c, !attackerIsWhite);

                    PieceType pieceType = PieceHelper.GetPieceType(piece);
                    int pieceValue = ChessUtilities.GetPieceValue(pieceType);
                    string pieceName = ChessUtilities.GetPieceName(pieceType);
                    string square = GetSquareName(r, c);

                    if (pieceType == PieceType.King) continue; // Kings handled separately

                    if (!isDefended)
                    {
                        // Check if the piece can escape to a safe square
                        bool canEscape = CanPieceEscape(board, r, c, piece, !attackerIsWhite);

                        // Also check if the attack can be blocked (for sliding piece attacks)
                        bool canBlock = ChessUtilities.CanBlockSlidingAttack(board, r, c, !attackerIsWhite);

                        if (!canEscape && !canBlock)
                        {
                            // Truly hanging - can't escape and can't be blocked!
                            threats.Add(new Threat
                            {
                                Description = $"wins {pieceName} on {square}",
                                Type = ThreatType.HangingPiece,
                                Severity = Math.Min(pieceValue, 5),
                                Square = square
                            });
                        }
                        else if (pieceValue >= 3)
                        {
                            // Can escape but we're creating a threat - they must respond
                            threats.Add(new Threat
                            {
                                Description = $"attacks {pieceName} on {square}",
                                Type = ThreatType.MaterialWin,
                                Severity = Math.Min(pieceValue - 1, 3),
                                Square = square
                            });
                        }
                    }
                    else if (pieceValue >= 3)
                    {
                        // Piece is defended - check if we can win material via lower-value attacker
                        int lowestAttackerValue = GetLowestAttackerValue(board, r, c, attackerIsWhite);
                        if (lowestAttackerValue > 0 && lowestAttackerValue < pieceValue)
                        {
                            // We can trade favorably, but only if piece can't escape
                            bool canEscape = CanPieceEscape(board, r, c, piece, !attackerIsWhite);
                            if (!canEscape)
                            {
                                threats.Add(new Threat
                                {
                                    Description = $"threatens {pieceName} on {square}",
                                    Type = ThreatType.MaterialWin,
                                    Severity = Math.Min(pieceValue - lowestAttackerValue + 1, 4),
                                    Square = square
                                });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Detect fork threats (one piece attacking multiple valuable pieces)
        /// </summary>
        private static void DetectForks(ChessBoard board, int pieceRow, int pieceCol,
            bool movingPlayerIsWhite, List<Threat> threats)
        {
            char piece = board.GetPiece(pieceRow, pieceCol);
            var attackedPieces = new List<(int row, int col, char piece, int value)>();

            // Find all enemy pieces this piece attacks
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char target = board.GetPiece(r, c);
                    if (target == '.') continue;

                    bool targetIsWhite = char.IsUpper(target);
                    if (targetIsWhite == movingPlayerIsWhite) continue; // Skip our own pieces

                    if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                    {
                        PieceType targetType = PieceHelper.GetPieceType(target);
                        int value = ChessUtilities.GetPieceValue(targetType);
                        attackedPieces.Add((r, c, target, value));
                    }
                }
            }

            // Fork = attacking 2+ pieces worth 3+ points each (or King)
            var valuableAttacked = attackedPieces.Where(p => p.value >= 3 || char.ToUpper(p.piece) == 'K').ToList();

            if (valuableAttacked.Count >= 2)
            {
                var pieceNames = valuableAttacked
                    .Select(p => ChessUtilities.GetPieceName(PieceHelper.GetPieceType(p.piece)))
                    .Take(2);

                threats.Add(new Threat
                {
                    Description = $"forks {string.Join(" and ", pieceNames)}",
                    Type = ThreatType.Fork,
                    Severity = 5,
                    Square = GetSquareName(pieceRow, pieceCol)
                });
            }
        }

        /// <summary>
        /// Detect pins (piece pinned to a more valuable piece or king)
        /// </summary>
        private static void DetectPins(ChessBoard board, bool attackerIsWhite, List<Threat> threats, BoardCache? cache = null)
        {
            // Use BoardCache for O(n) sliding piece lookup instead of O(64) board scan
            if (cache != null)
            {
                foreach (var (r, c, piece) in cache.GetSlidingPieces(attackerIsWhite))
                {
                    var pin = DetectPinFromPiece(board, r, c, piece, attackerIsWhite);
                    if (pin != null)
                        threats.Add(pin);
                }
            }
            else
            {
                // Fallback: O(64) board scan when no cache provided
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        bool pieceIsWhite = char.IsUpper(piece);
                        if (pieceIsWhite != attackerIsWhite) continue;

                        PieceType pieceType = PieceHelper.GetPieceType(piece);
                        if (pieceType != PieceType.Bishop && pieceType != PieceType.Rook && pieceType != PieceType.Queen)
                            continue;

                        var pin = DetectPinFromPiece(board, r, c, piece, attackerIsWhite);
                        if (pin != null)
                            threats.Add(pin);
                    }
                }
            }
        }

        private static Threat? DetectPinFromPiece(ChessBoard board, int pieceRow, int pieceCol,
            char piece, bool attackerIsWhite)
        {
            PieceType pieceType = PieceHelper.GetPieceType(piece);
            int attackerValue = ChessUtilities.GetPieceValue(pieceType);

            var directions = ChessUtilities.GetDirectionsForPiece(pieceType);

            foreach (var (dR, dF) in directions)
            {
                int r = pieceRow + dR, c = pieceCol + dF;
                char? firstPiece = null;
                int firstRow = -1, firstCol = -1;

                while (r >= 0 && r < 8 && c >= 0 && c < 8)
                {
                    char target = board.GetPiece(r, c);
                    if (target != '.')
                    {
                        bool targetIsWhite = char.IsUpper(target);

                        if (firstPiece == null)
                        {
                            // First piece must be enemy
                            if (targetIsWhite != attackerIsWhite)
                            {
                                firstPiece = target;
                                firstRow = r;
                                firstCol = c;
                            }
                            else break;
                        }
                        else
                        {
                            // Second piece must also be enemy and more valuable (or King)
                            if (targetIsWhite != attackerIsWhite)
                            {
                                PieceType firstType = PieceHelper.GetPieceType(firstPiece.Value);
                                PieceType secondType = PieceHelper.GetPieceType(target);

                                int firstValue = ChessUtilities.GetPieceValue(firstType);
                                int secondValue = secondType == PieceType.King ? 100 : ChessUtilities.GetPieceValue(secondType);

                                // Only report as a pin if:
                                // 1. The piece behind is the KING (absolute pin - illegal to move)
                                // 2. OR we would actually WIN the piece behind if the front piece moved
                                //    (meaning the piece behind is undefended or we'd win the exchange)
                                if (secondValue > firstValue)
                                {
                                    // PAWN SPECIAL CASE: Pawns can only move forward or capture diagonally.
                                    // A pawn is only meaningfully "pinned" if:
                                    // 1. It's pinned along a diagonal AND
                                    // 2. Moving diagonally (capturing) would expose the piece behind
                                    // Pawns CANNOT be pinned along files/ranks because they don't move that way anyway
                                    // (they can only move forward, so a rook "pinning" a pawn to a king on the same file is meaningless)
                                    if (firstType == PieceType.Pawn)
                                    {
                                        // Check if this is a diagonal pin (bishop/queen on diagonal)
                                        bool isDiagonalPin = (dR != 0 && dF != 0);

                                        if (!isDiagonalPin)
                                        {
                                            // Pawn pinned along a file or rank - not a real pin since pawns don't move that way
                                            break;
                                        }

                                        // Even on a diagonal, pawn can only capture in the forward diagonal direction
                                        // Check if the pawn could actually capture in this pin direction
                                        bool pawnIsWhite = char.IsUpper(firstPiece.Value);
                                        int pawnForwardDir = pawnIsWhite ? -1 : 1; // White moves up (negative row), black moves down

                                        // The pin direction from attacker to pawn
                                        // If attacker is below-left of pawn and pawn is white, pawn could capture toward attacker (moving down-left)
                                        // But white pawns capture UP (forward), so this wouldn't expose the king
                                        // The pawn can only "unpin" by capturing in the direction AWAY from the attacker (toward the king)
                                        // which means capturing in the direction of (dR, dF) from the pawn's perspective

                                        // For a pin to matter: the pawn must be able to capture in the direction toward the piece behind
                                        // Pawn captures diagonally forward: (pawnForwardDir, ±1)
                                        // Pin direction from pawn to king: (dR, dF) [same as attacker's ray direction]

                                        // If the pin is along the pawn's forward-diagonal capture direction, it's a real pin
                                        // Otherwise, the pawn can't move along that diagonal anyway
                                        bool pawnCanMoveAlongPin = (dR == pawnForwardDir);

                                        if (!pawnCanMoveAlongPin)
                                        {
                                            // Pawn is "pinned" along a backward diagonal - but pawns can't move backward!
                                            break;
                                        }

                                        // Even if the pawn CAN move in this diagonal direction, it can only do so by CAPTURING
                                        // Check if there's actually an enemy piece to capture
                                        int captureRow = firstRow + pawnForwardDir;
                                        int captureCol1 = firstCol - 1;
                                        int captureCol2 = firstCol + 1;

                                        bool hasCapture = false;
                                        if (captureRow >= 0 && captureRow < 8)
                                        {
                                            // Check if there's an enemy piece to capture that would break the pin
                                            if (captureCol1 >= 0 && captureCol1 < 8)
                                            {
                                                char target1 = board.GetPiece(captureRow, captureCol1);
                                                if (target1 != '.' && char.IsUpper(target1) == attackerIsWhite)
                                                {
                                                    // There's an enemy piece the pawn could capture - check if it's along the pin line
                                                    // The pin line direction is (dR, dF)
                                                    if (dF == -1 || dF == 1) // diagonal pin
                                                    {
                                                        // Check if this capture square is along the pin line toward the king
                                                        int captureDirFromPawn = captureCol1 - firstCol; // -1 or would be checked above
                                                        if (captureDirFromPawn == dF)
                                                            hasCapture = true;
                                                    }
                                                }
                                            }
                                            if (!hasCapture && captureCol2 >= 0 && captureCol2 < 8)
                                            {
                                                char target2 = board.GetPiece(captureRow, captureCol2);
                                                if (target2 != '.' && char.IsUpper(target2) == attackerIsWhite)
                                                {
                                                    int captureDirFromPawn = captureCol2 - firstCol; // +1
                                                    if (captureDirFromPawn == dF)
                                                        hasCapture = true;
                                                }
                                            }
                                        }

                                        if (!hasCapture)
                                        {
                                            // Pawn has no capture that would break the pin - not a meaningful pin
                                            break;
                                        }
                                    }

                                    bool isAbsolutePin = secondType == PieceType.King;
                                    bool wouldWinMaterial = false;

                                    if (!isAbsolutePin)
                                    {
                                        // Check if we could actually capture the piece behind profitably
                                        // Create a temp board where the pinned piece is removed
                                        using var pooledTemp = BoardPool.Rent(board);
                                        ChessBoard tempBoard = pooledTemp.Board;
                                        tempBoard.SetPiece(firstRow, firstCol, '.'); // Remove pinned piece

                                        // Check if the piece behind would be defended after we capture
                                        bool pieceDefended = IsSquareAttackedBy(tempBoard, r, c, !attackerIsWhite);

                                        if (!pieceDefended)
                                        {
                                            // Piece is undefended - we'd win it
                                            wouldWinMaterial = true;
                                        }
                                        else if (secondValue > attackerValue)
                                        {
                                            // Even if defended, we'd trade up (e.g., our bishop for their queen)
                                            wouldWinMaterial = true;
                                        }
                                        // If defended by equal or lesser value piece, not a real pin
                                    }

                                    if (isAbsolutePin || wouldWinMaterial)
                                    {
                                        string pinnedName = ChessUtilities.GetPieceName(firstType);
                                        string behindName = secondType == PieceType.King ? "king" : ChessUtilities.GetPieceName(secondType);
                                        string pinnedSquare = GetSquareName(firstRow, firstCol);

                                        return new Threat
                                        {
                                            Description = $"pins {pinnedName} on {pinnedSquare} to {behindName}",
                                            Type = ThreatType.Pin,
                                            Severity = secondType == PieceType.King ? 4 : 3,
                                            Square = pinnedSquare
                                        };
                                    }
                                }
                            }
                            break;
                        }
                    }
                    r += dR;
                    c += dF;
                }
            }

            return null;
        }

        /// <summary>
        /// Detect promotion threats (pawns about to promote)
        /// </summary>
        private static void DetectPromotionThreats(ChessBoard board, bool movingPlayerIsWhite, List<Threat> threats)
        {
            char ourPawn = movingPlayerIsWhite ? 'P' : 'p';
            int promotionRank = movingPlayerIsWhite ? 1 : 6; // One rank before promotion

            for (int c = 0; c < 8; c++)
            {
                if (board.GetPiece(promotionRank, c) == ourPawn)
                {
                    // Check if the promotion square is available
                    int promoRank = movingPlayerIsWhite ? 0 : 7;
                    char promoSquare = board.GetPiece(promoRank, c);

                    if (promoSquare == '.' || char.IsUpper(promoSquare) != movingPlayerIsWhite)
                    {
                        char file = (char)('a' + c);
                        threats.Add(new Threat
                        {
                            Description = $"{file}-pawn threatens promotion",
                            Type = ThreatType.Promotion,
                            Severity = 5,
                            Square = GetSquareName(promotionRank, c)
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Detect trapped pieces (pieces with no safe escape squares that are under attack)
        /// </summary>
        private static void DetectTrappedPieces(ChessBoard board, bool attackerIsWhite, List<Threat> threats)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite == attackerIsWhite) continue; // Skip our pieces

                    PieceType pieceType = PieceHelper.GetPieceType(piece);
                    if (pieceType == PieceType.Pawn || pieceType == PieceType.King) continue;

                    int pieceValue = ChessUtilities.GetPieceValue(pieceType);
                    if (pieceValue < 3) continue; // Only check valuable pieces

                    // Check if piece is attacked
                    if (!IsSquareAttackedBy(board, r, c, attackerIsWhite)) continue;

                    // Check if it's defended by a lower-value piece
                    bool isDefended = IsSquareAttackedBy(board, r, c, !attackerIsWhite);

                    // Count safe squares for this piece
                    bool canEscape = CanPieceEscape(board, r, c, piece, !attackerIsWhite);

                    // Also check if the attack can be blocked (for sliding piece attacks)
                    bool canBlock = ChessUtilities.CanBlockSlidingAttack(board, r, c, !attackerIsWhite);

                    if (!canEscape && !isDefended && !canBlock)
                    {
                        string pieceName = ChessUtilities.GetPieceName(pieceType);
                        string square = GetSquareName(r, c);
                        threats.Add(new Threat
                        {
                            Description = $"{pieceName} on {square} is trapped",
                            Type = ThreatType.TrappedPiece,
                            Severity = Math.Min(pieceValue, 5),
                            Square = square
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Detect en passant capture opportunities.
        /// En passant is a special pawn capture that can only happen immediately after
        /// an enemy pawn moves two squares forward from its starting position.
        /// </summary>
        private static void DetectEnPassantThreat(ChessBoard board, string enPassantSquare, bool movingPlayerIsWhite, List<Threat> threats)
        {
            // If no en passant square available, nothing to detect
            if (string.IsNullOrEmpty(enPassantSquare) || enPassantSquare == "-")
                return;

            // Parse the en passant target square
            if (enPassantSquare.Length < 2)
                return;

            int epFile = enPassantSquare[0] - 'a';
            int epRank = 8 - (enPassantSquare[1] - '0');

            // Validate
            if (epFile < 0 || epFile > 7 || epRank < 0 || epRank > 7)
                return;

            // Determine which rank our pawns should be on to capture en passant
            // White captures en passant on rank 6 (row 2 in array), black on rank 3 (row 5)
            // The EP square is where the capturing pawn lands (behind the enemy pawn)
            char ourPawn = movingPlayerIsWhite ? 'P' : 'p';
            int expectedPawnRank = movingPlayerIsWhite ? 3 : 4; // Rank 5 for white (row 3), rank 4 for black (row 4)

            // Check if we have a pawn that can capture en passant
            bool canCaptureEP = false;
            string capturingPawnFile = "";

            // Check left of EP square
            if (epFile > 0)
            {
                char leftPiece = board.GetPiece(expectedPawnRank, epFile - 1);
                if (leftPiece == ourPawn)
                {
                    canCaptureEP = true;
                    capturingPawnFile = ((char)('a' + epFile - 1)).ToString();
                }
            }

            // Check right of EP square
            if (epFile < 7)
            {
                char rightPiece = board.GetPiece(expectedPawnRank, epFile + 1);
                if (rightPiece == ourPawn)
                {
                    canCaptureEP = true;
                    if (!string.IsNullOrEmpty(capturingPawnFile))
                        capturingPawnFile += " or " + ((char)('a' + epFile + 1)).ToString();
                    else
                        capturingPawnFile = ((char)('a' + epFile + 1)).ToString();
                }
            }

            if (canCaptureEP)
            {
                // The enemy pawn is on the same file as the EP square, but one rank behind
                int enemyPawnRank = movingPlayerIsWhite ? epRank + 1 : epRank - 1;
                string enemyPawnSquare = GetSquareName(enemyPawnRank, epFile);

                threats.Add(new Threat
                {
                    Description = $"en passant capture available on {enPassantSquare}",
                    Type = ThreatType.EnPassant,
                    Severity = 2, // Moderate severity - it's a pawn capture
                    Square = enPassantSquare
                });
            }
        }

        #endregion Our Threat Detection Methods

        #region Opponent Threat Detection Methods

        /// <summary>
        /// Detect if our king is in check
        /// </summary>
        private static void DetectCheckThreatsAgainstUs(ChessBoard board, bool weAreWhite, List<Threat> threats)
        {
            // Find our king - O(1) using cached position
            var (kingRow, kingCol) = board.GetKingPosition(weAreWhite);
            if (kingRow < 0) return;

            // Check if our king is in check
            if (IsSquareAttackedBy(board, kingRow, kingCol, !weAreWhite))
            {
                threats.Add(new Threat
                {
                    Description = "king is in check!",
                    Type = ThreatType.Check,
                    Severity = 5,
                    Square = GetSquareName(kingRow, kingCol)
                });
            }
        }

        /// <summary>
        /// Detect opponent pieces that threaten to capture our pieces on their next move.
        /// Only shows threats where we would lose material.
        /// </summary>
        private static void DetectOpponentCaptureThreats(ChessBoard board, bool opponentIsWhite, List<Threat> threats)
        {
            // For each of our pieces, check if opponent can capture it profitably
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);

                    // Look for OUR pieces (that opponent attacks)
                    if (pieceIsWhite == opponentIsWhite) continue;

                    PieceType pieceType = PieceHelper.GetPieceType(piece);
                    if (pieceType == PieceType.King) continue; // King handled by check detection

                    int pieceValue = ChessUtilities.GetPieceValue(pieceType);
                    if (pieceValue < 1) continue;

                    // Is this piece attacked by opponent?
                    bool isAttacked = IsSquareAttackedBy(board, r, c, opponentIsWhite);
                    if (!isAttacked) continue;

                    // Is it defended?
                    bool isDefended = IsSquareAttackedBy(board, r, c, !opponentIsWhite);

                    string pieceName = ChessUtilities.GetPieceName(pieceType);
                    string square = GetSquareName(r, c);

                    if (!isDefended)
                    {
                        // Undefended piece - can opponent capture it?
                        bool canEscape = CanPieceEscape(board, r, c, piece, !opponentIsWhite);

                        if (!canEscape)
                        {
                            // Hanging and can't escape!
                            threats.Add(new Threat
                            {
                                Description = $"{pieceName} on {square} is hanging",
                                Type = ThreatType.HangingPiece,
                                Severity = Math.Min(pieceValue, 5),
                                Square = square
                            });
                        }
                        else if (pieceValue >= 3)
                        {
                            // Can escape but need to move
                            threats.Add(new Threat
                            {
                                Description = $"{pieceName} on {square} is attacked",
                                Type = ThreatType.MaterialWin,
                                Severity = 2,
                                Square = square
                            });
                        }
                    }
                    else if (pieceValue >= 3)
                    {
                        // Defended but check if opponent wins the trade
                        int lowestAttackerValue = GetLowestAttackerValue(board, r, c, opponentIsWhite);
                        if (lowestAttackerValue > 0 && lowestAttackerValue < pieceValue)
                        {
                            // Opponent attacks with lower-value piece - this is a threat we must address
                            // Even if our piece can escape, user should know about the threat
                            bool canEscape = CanPieceEscape(board, r, c, piece, !opponentIsWhite);

                            // Higher severity if piece can't escape (trapped), lower if it can move
                            int severity = canEscape ? 2 : Math.Min(pieceValue - lowestAttackerValue, 4);

                            threats.Add(new Threat
                            {
                                Description = $"{pieceName} on {square} is attacked",
                                Type = ThreatType.MaterialWin,
                                Severity = severity,
                                Square = square
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Detect opponent forks against our pieces
        /// A fork is only a real threat if we would LOSE material (can't save both pieces)
        /// </summary>
        private static void DetectOpponentForks(ChessBoard board, bool opponentIsWhite, List<Threat> threats)
        {
            // Look for opponent pieces that attack multiple of our valuable pieces
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != opponentIsWhite) continue;

                    PieceType attackerType = PieceHelper.GetPieceType(piece);
                    int attackerValue = ChessUtilities.GetPieceValue(attackerType);

                    var attackedPieces = new List<(char piece, int value, string square, int row, int col, bool isDefended)>();

                    // Find all our pieces this opponent piece attacks
                    for (int tr = 0; tr < 8; tr++)
                    {
                        for (int tc = 0; tc < 8; tc++)
                        {
                            char target = board.GetPiece(tr, tc);
                            if (target == '.') continue;

                            bool targetIsWhite = char.IsUpper(target);
                            if (targetIsWhite == opponentIsWhite) continue; // Skip opponent's own pieces

                            if (ChessUtilities.CanAttackSquare(board, r, c, piece, tr, tc))
                            {
                                PieceType targetType = PieceHelper.GetPieceType(target);
                                int value = targetType == PieceType.King ? 100 : ChessUtilities.GetPieceValue(targetType);
                                // Check if our piece is defended by us (the non-opponent)
                                bool isDefended = IsSquareAttackedBy(board, tr, tc, !opponentIsWhite);
                                attackedPieces.Add((target, value, GetSquareName(tr, tc), tr, tc, isDefended));
                            }
                        }
                    }

                    var valuableAttacked = attackedPieces.Where(p => p.value >= 3 || char.ToUpper(p.piece) == 'K').ToList();

                    if (valuableAttacked.Count >= 2)
                    {
                        // For a fork to be a REAL threat, we need to check if opponent actually wins material
                        // Fork works if: at least one piece is undefended, OR attacker value < lowest target value (can trade up)
                        bool hasUndefendedPiece = valuableAttacked.Any(p => !p.isDefended);
                        int lowestTargetValue = valuableAttacked.Min(p => p.value);

                        // If all pieces are defended AND attacker is worth same or more, fork doesn't win material
                        // We can just take the attacker and only lose one trade
                        if (!hasUndefendedPiece && attackerValue >= lowestTargetValue)
                        {
                            // Not a real fork - we can just let them take one piece and recapture
                            // No material loss beyond a single trade
                            continue;
                        }

                        var pieceNames = valuableAttacked
                            .Select(p => ChessUtilities.GetPieceName(PieceHelper.GetPieceType(p.piece)))
                            .Take(2);

                        threats.Add(new Threat
                        {
                            Description = $"opponent forks {string.Join(" and ", pieceNames)}",
                            Type = ThreatType.Fork,
                            Severity = 5,
                            Square = GetSquareName(r, c)
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Detect opponent pawns about to promote
        /// </summary>
        private static void DetectOpponentPromotionThreats(ChessBoard board, bool opponentIsWhite, List<Threat> threats)
        {
            char oppPawn = opponentIsWhite ? 'P' : 'p';
            int promotionRank = opponentIsWhite ? 1 : 6;

            for (int c = 0; c < 8; c++)
            {
                if (board.GetPiece(promotionRank, c) == oppPawn)
                {
                    char file = (char)('a' + c);
                    threats.Add(new Threat
                    {
                        Description = $"opponent's {file}-pawn threatens promotion",
                        Type = ThreatType.Promotion,
                        Severity = 5,
                        Square = GetSquareName(promotionRank, c)
                    });
                }
            }
        }

        /// <summary>
        /// Detect pins against our pieces by opponent sliding pieces
        /// </summary>
        private static void DetectPinsAgainstUs(ChessBoard board, bool weAreWhite, List<Threat> threats, BoardCache? cache = null)
        {
            bool opponentIsWhite = !weAreWhite;

            // Use BoardCache for O(n) sliding piece lookup instead of O(64) board scan
            if (cache != null)
            {
                foreach (var (r, c, piece) in cache.GetSlidingPieces(opponentIsWhite))
                {
                    var pin = DetectPinAgainstUs(board, r, c, piece, opponentIsWhite);
                    if (pin != null)
                        threats.Add(pin);
                }
            }
            else
            {
                // Fallback: O(64) board scan when no cache provided
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        bool pieceIsWhite = char.IsUpper(piece);
                        if (pieceIsWhite != opponentIsWhite) continue;

                        PieceType pieceType = PieceHelper.GetPieceType(piece);
                        if (pieceType != PieceType.Bishop && pieceType != PieceType.Rook && pieceType != PieceType.Queen)
                            continue;

                        var pin = DetectPinAgainstUs(board, r, c, piece, opponentIsWhite);
                        if (pin != null)
                            threats.Add(pin);
                    }
                }
            }
        }

        private static Threat? DetectPinAgainstUs(ChessBoard board, int pieceRow, int pieceCol,
            char piece, bool attackerIsWhite)
        {
            PieceType pieceType = PieceHelper.GetPieceType(piece);

            var directions = ChessUtilities.GetDirectionsForPiece(pieceType);

            foreach (var (dR, dF) in directions)
            {
                int r = pieceRow + dR, c = pieceCol + dF;
                char? firstPiece = null;
                int firstRow = -1, firstCol = -1;

                while (r >= 0 && r < 8 && c >= 0 && c < 8)
                {
                    char target = board.GetPiece(r, c);
                    if (target != '.')
                    {
                        bool targetIsWhite = char.IsUpper(target);

                        if (firstPiece == null)
                        {
                            // First piece must be enemy (our piece)
                            if (targetIsWhite != attackerIsWhite)
                            {
                                firstPiece = target;
                                firstRow = r;
                                firstCol = c;
                            }
                            else break;
                        }
                        else
                        {
                            // Second piece must also be enemy (our piece) and more valuable (or King)
                            if (targetIsWhite != attackerIsWhite)
                            {
                                PieceType firstType = PieceHelper.GetPieceType(firstPiece.Value);
                                PieceType secondType = PieceHelper.GetPieceType(target);

                                int firstValue = ChessUtilities.GetPieceValue(firstType);
                                int secondValue = secondType == PieceType.King ? 100 : ChessUtilities.GetPieceValue(secondType);

                                if (secondValue > firstValue && secondType == PieceType.King)
                                {
                                    // PAWN SPECIAL CASE: Pawns can only move forward, not along pin lines
                                    // A pawn "pinned" along a diagonal or file often can't move anyway
                                    // Only report pawn pins if the pawn could actually move along that line
                                    if (firstType == PieceType.Pawn)
                                    {
                                        // Pawns can only move forward (white: up, black: down)
                                        // They can only be meaningfully pinned if the pin is along their capture diagonal
                                        // AND they have something to capture there
                                        // For simplicity, skip pawn pins - they're rarely meaningful threats
                                        // The pawn usually can't move anyway (blocked or no captures)
                                        break;
                                    }

                                    // Our piece is pinned to our king
                                    string pinnedName = ChessUtilities.GetPieceName(firstType);
                                    string pinnedSquare = GetSquareName(firstRow, firstCol);

                                    return new Threat
                                    {
                                        Description = $"our {pinnedName} on {pinnedSquare} is pinned",
                                        Type = ThreatType.Pin,
                                        Severity = 3,
                                        Square = pinnedSquare
                                    };
                                }
                            }
                            break;
                        }
                    }
                    r += dR;
                    c += dF;
                }
            }

            return null;
        }

        /// <summary>
        /// Detect if the opponent can capture our pawn via en passant.
        /// This is a threat against us - our pawn is vulnerable.
        ///
        /// IMPORTANT: The en passant square in FEN tells us which pawn is vulnerable:
        /// - EP on rank 3 (e.g., "c3") means a WHITE pawn on rank 4 just moved 2 squares
        ///   and could be captured by a BLACK pawn on b4 or d4
        /// - EP on rank 6 (e.g., "c6") means a BLACK pawn on rank 5 just moved 2 squares
        ///   and could be captured by a WHITE pawn on b5 or d5
        ///
        /// This is only a threat to US if OUR pawn is the vulnerable one.
        /// </summary>
        private static void DetectOpponentEnPassantThreat(ChessBoard board, string enPassantSquare, bool weAreWhite, List<Threat> threats)
        {
            // If no en passant square available, nothing to detect
            if (string.IsNullOrEmpty(enPassantSquare) || enPassantSquare == "-")
                return;

            // Parse the en passant target square
            if (enPassantSquare.Length < 2)
                return;

            int epFile = enPassantSquare[0] - 'a';

            // Validate file
            if (epFile < 0 || epFile > 7)
                return;

            // Determine whose pawn is vulnerable based on the EP square's rank
            // EP on rank 3 = White pawn vulnerable (White just pushed to rank 4)
            // EP on rank 6 = Black pawn vulnerable (Black just pushed to rank 5)
            bool whitePawnVulnerable = (enPassantSquare[1] == '3');
            bool blackPawnVulnerable = (enPassantSquare[1] == '6');

            // This is only a threat to US if OUR pawn is the vulnerable one
            bool ourPawnIsVulnerable = (weAreWhite && whitePawnVulnerable) || (!weAreWhite && blackPawnVulnerable);

            if (!ourPawnIsVulnerable)
            {
                // The opponent's pawn is vulnerable, not ours - not a threat to us
                return;
            }

            // Our pawn is vulnerable - check if opponent actually has a pawn that can capture it
            // The vulnerable pawn is on rank 4 (row 4) for White, rank 5 (row 3) for Black
            // Enemy capturing pawns must be adjacent on the same rank
            char enemyPawn = weAreWhite ? 'p' : 'P';
            int vulnerablePawnRank = weAreWhite ? 4 : 3; // row index: White pawn on rank 4 (row 4), Black on rank 5 (row 3)

            bool opponentCanCaptureEP = false;

            // Check left of vulnerable pawn for enemy pawn
            if (epFile > 0)
            {
                char leftPiece = board.GetPiece(vulnerablePawnRank, epFile - 1);
                if (leftPiece == enemyPawn)
                {
                    opponentCanCaptureEP = true;
                }
            }

            // Check right of vulnerable pawn for enemy pawn
            if (epFile < 7)
            {
                char rightPiece = board.GetPiece(vulnerablePawnRank, epFile + 1);
                if (rightPiece == enemyPawn)
                {
                    opponentCanCaptureEP = true;
                }
            }

            if (opponentCanCaptureEP)
            {
                // Our pawn is vulnerable and opponent has a pawn that can capture it
                string ourPawnSquare = GetSquareName(vulnerablePawnRank, epFile);
                char pawnFile = (char)('a' + epFile);

                threats.Add(new Threat
                {
                    Description = $"opponent can capture {pawnFile}-pawn en passant",
                    Type = ThreatType.EnPassant,
                    Severity = 2,
                    Square = ourPawnSquare
                });
            }
        }

        #endregion Opponent Threat Detection Methods

        #region Helper Methods

        private static string GetSquareName(int row, int col) => ChessUtilities.GetSquareName(row, col);

        private static bool IsSquareAttackedBy(ChessBoard board, int row, int col, bool byWhite)
            => ChessUtilities.IsSquareAttackedBy(board, row, col, byWhite);

        private static int GetLowestAttackerValue(ChessBoard board, int row, int col, bool attackerIsWhite)
            => ChessUtilities.GetLowestAttackerValue(board, row, col, attackerIsWhite);

        /// <summary>
        /// Check if a piece can escape to at least one safe square
        /// </summary>
        private static bool CanPieceEscape(ChessBoard board, int pieceRow, int pieceCol, char piece, bool pieceIsWhite)
        {
            // Delegate to consolidated implementation in ChessUtilities
            return ChessUtilities.CanPieceEscape(board, pieceRow, pieceCol, piece, pieceIsWhite);
        }

        private static int CountKingEscapeSquares(ChessBoard board, int kingRow, int kingCol, bool kingIsWhite)
        {
            int escapeSquares = 0;
            char king = kingIsWhite ? 'K' : 'k';

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;

                    int newRow = kingRow + dr;
                    int newCol = kingCol + dc;

                    if (newRow < 0 || newRow >= 8 || newCol < 0 || newCol >= 8) continue;

                    char target = board.GetPiece(newRow, newCol);

                    // Can't move to square occupied by own piece
                    if (target != '.' && char.IsUpper(target) == kingIsWhite) continue;

                    // Check if square is attacked by opponent
                    // Temporarily remove king to check attacks
                    using var pooledKing = BoardPool.Rent(board);
                    ChessBoard tempBoard = pooledKing.Board;
                    tempBoard.SetPiece(kingRow, kingCol, '.');
                    tempBoard.SetPiece(newRow, newCol, king);

                    if (!IsSquareAttackedBy(tempBoard, newRow, newCol, !kingIsWhite))
                    {
                        escapeSquares++;
                    }
                }
            }

            return escapeSquares;
        }


        #endregion Helper Methods
    }
}