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
            TrappedPiece        // Piece with no escape squares
        }

        /// <summary>
        /// Analyzes NEW threats WE create after making the best move.
        /// Only returns threats that didn't exist before the move.
        /// </summary>
        public static List<Threat> AnalyzeThreatsAfterMove(ChessBoard board, string move, bool movingPlayerIsWhite)
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

                // Create board after the move
                ChessBoard afterMove = new ChessBoard(board.GetArray());
                char movingPiece = afterMove.GetPiece(srcRank, srcFile);
                afterMove.SetPiece(destRank, destFile, movingPiece);
                afterMove.SetPiece(srcRank, srcFile, '.');

                // Handle promotion
                if (move.Length > 4)
                {
                    char promotionPiece = movingPlayerIsWhite ? char.ToUpper(move[4]) : char.ToLower(move[4]);
                    afterMove.SetPiece(destRank, destFile, promotionPiece);
                }

                // Detect various threats AFTER our move
                DetectCheckThreats(afterMove, destRank, destFile, movingPlayerIsWhite, threatsAfter);
                DetectRealHangingPieces(afterMove, movingPlayerIsWhite, threatsAfter);
                DetectForks(afterMove, destRank, destFile, movingPlayerIsWhite, threatsAfter);
                DetectPins(afterMove, movingPlayerIsWhite, threatsAfter);
                DetectPromotionThreats(afterMove, movingPlayerIsWhite, threatsAfter);
                DetectTrappedPieces(afterMove, movingPlayerIsWhite, threatsAfter);

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
        public static List<Threat> AnalyzeOpponentThreats(ChessBoard board, bool weAreWhite)
        {
            var threats = new List<Threat>();

            try
            {
                bool opponentIsWhite = !weAreWhite;

                // Check if our king is in check
                DetectCheckThreatsAgainstUs(board, weAreWhite, threats);

                // Check for opponent pieces that can capture our valuable pieces on their next move
                DetectOpponentCaptureThreats(board, opponentIsWhite, threats);

                // Check for opponent forks (their piece attacking 2+ of our valuable pieces)
                DetectOpponentForks(board, opponentIsWhite, threats);

                // Check for opponent promotion threats
                DetectOpponentPromotionThreats(board, opponentIsWhite, threats);

                // Check for pins against us
                DetectPinsAgainstUs(board, weAreWhite, threats);
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

        /// <summary>
        /// Detect if the move gives check or threatens mate
        /// </summary>
        private static void DetectCheckThreats(ChessBoard board, int pieceRow, int pieceCol,
            bool movingPlayerIsWhite, List<Threat> threats)
        {
            char piece = board.GetPiece(pieceRow, pieceCol);

            // Find opponent's king
            char opponentKing = movingPlayerIsWhite ? 'k' : 'K';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(r, c) == opponentKing)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
                if (kingRow >= 0) break;
            }

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

                        if (!canEscape)
                        {
                            // Truly hanging - can't escape!
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
        private static void DetectPins(ChessBoard board, bool attackerIsWhite, List<Threat> threats)
        {
            // Find all our sliding pieces (bishops, rooks, queens)
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

                    // Check for pins in each direction
                    var pin = DetectPinFromPiece(board, r, c, piece, attackerIsWhite);
                    if (pin != null)
                    {
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

            int[][] directions = pieceType == PieceType.Bishop
                ? new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } }
                : pieceType == PieceType.Rook
                    ? new[] { new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } }
                    : new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 },
                              new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } };

            foreach (var dir in directions)
            {
                int dR = dir[0], dF = dir[1];
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
                                    bool isAbsolutePin = secondType == PieceType.King;
                                    bool wouldWinMaterial = false;

                                    if (!isAbsolutePin)
                                    {
                                        // Check if we could actually capture the piece behind profitably
                                        // Create a temp board where the pinned piece is removed
                                        ChessBoard tempBoard = new ChessBoard(board.GetArray());
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

                    if (!canEscape && !isDefended)
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

        #endregion Our Threat Detection Methods

        #region Opponent Threat Detection Methods

        /// <summary>
        /// Detect if our king is in check
        /// </summary>
        private static void DetectCheckThreatsAgainstUs(ChessBoard board, bool weAreWhite, List<Threat> threats)
        {
            // Find our king
            char ourKing = weAreWhite ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(r, c) == ourKing)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
                if (kingRow >= 0) break;
            }

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
                            // Opponent can trade favorably
                            bool canEscape = CanPieceEscape(board, r, c, piece, !opponentIsWhite);
                            if (!canEscape)
                            {
                                threats.Add(new Threat
                                {
                                    Description = $"{pieceName} on {square} under attack",
                                    Type = ThreatType.MaterialWin,
                                    Severity = Math.Min(pieceValue - lowestAttackerValue, 4),
                                    Square = square
                                });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Detect opponent forks against our pieces
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

                    var attackedPieces = new List<(char piece, int value, string square)>();

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
                                int value = ChessUtilities.GetPieceValue(targetType);
                                attackedPieces.Add((target, value, GetSquareName(tr, tc)));
                            }
                        }
                    }

                    var valuableAttacked = attackedPieces.Where(p => p.value >= 3 || char.ToUpper(p.piece) == 'K').ToList();

                    if (valuableAttacked.Count >= 2)
                    {
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
        private static void DetectPinsAgainstUs(ChessBoard board, bool weAreWhite, List<Threat> threats)
        {
            bool opponentIsWhite = !weAreWhite;

            // Find opponent's sliding pieces
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

                    // Check for pins in each direction (opponent's perspective)
                    var pin = DetectPinAgainstUs(board, r, c, piece, opponentIsWhite);
                    if (pin != null)
                    {
                        threats.Add(pin);
                    }
                }
            }
        }

        private static Threat? DetectPinAgainstUs(ChessBoard board, int pieceRow, int pieceCol,
            char piece, bool attackerIsWhite)
        {
            PieceType pieceType = PieceHelper.GetPieceType(piece);

            int[][] directions = pieceType == PieceType.Bishop
                ? new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } }
                : pieceType == PieceType.Rook
                    ? new[] { new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } }
                    : new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 },
                              new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } };

            foreach (var dir in directions)
            {
                int dR = dir[0], dF = dir[1];
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

        #endregion Opponent Threat Detection Methods

        #region Helper Methods

        private static string GetSquareName(int row, int col)
        {
            char file = (char)('a' + col);
            int rank = 8 - row;
            return $"{file}{rank}";
        }

        private static bool IsSquareAttackedBy(ChessBoard board, int row, int col, bool byWhite)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != byWhite) continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, row, col))
                        return true;
                }
            }
            return false;
        }

        private static int GetLowestAttackerValue(ChessBoard board, int row, int col, bool attackerIsWhite)
        {
            int lowestValue = int.MaxValue;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != attackerIsWhite) continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, row, col))
                    {
                        int value = ChessUtilities.GetPieceValue(PieceHelper.GetPieceType(piece));
                        if (value < lowestValue)
                            lowestValue = value;
                    }
                }
            }

            return lowestValue == int.MaxValue ? 0 : lowestValue;
        }

        /// <summary>
        /// Check if a piece can escape to at least one safe square
        /// </summary>
        private static bool CanPieceEscape(ChessBoard board, int pieceRow, int pieceCol, char piece, bool pieceIsWhite)
        {
            var possibleMoves = GetPossibleMoves(board, pieceRow, pieceCol, piece, pieceIsWhite);

            foreach (var (newRow, newCol) in possibleMoves)
            {
                // Check if this square would be safe
                ChessBoard tempBoard = new ChessBoard(board.GetArray());
                tempBoard.SetPiece(pieceRow, pieceCol, '.');
                tempBoard.SetPiece(newRow, newCol, piece);

                if (!IsSquareAttackedBy(tempBoard, newRow, newCol, !pieceIsWhite))
                {
                    return true; // Found at least one safe square
                }
            }

            return false;
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
                    ChessBoard tempBoard = new ChessBoard(board.GetArray());
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

        private static List<(int row, int col)> GetPossibleMoves(ChessBoard board, int row, int col,
            char piece, bool isWhite)
        {
            var moves = new List<(int, int)>();
            PieceType pieceType = PieceHelper.GetPieceType(piece);

            switch (pieceType)
            {
                case PieceType.Pawn:
                    // Pawns can move forward or capture diagonally
                    int direction = isWhite ? -1 : 1;
                    int newRow = row + direction;
                    if (newRow >= 0 && newRow < 8)
                    {
                        // Forward move
                        if (board.GetPiece(newRow, col) == '.')
                            moves.Add((newRow, col));

                        // Captures
                        for (int dc = -1; dc <= 1; dc += 2)
                        {
                            int nc = col + dc;
                            if (nc >= 0 && nc < 8)
                            {
                                char target = board.GetPiece(newRow, nc);
                                if (target != '.' && char.IsUpper(target) != isWhite)
                                    moves.Add((newRow, nc));
                            }
                        }
                    }
                    break;

                case PieceType.Knight:
                    int[][] knightMoves = { new[] { -2, -1 }, new[] { -2, 1 }, new[] { -1, -2 }, new[] { -1, 2 },
                                            new[] { 1, -2 }, new[] { 1, 2 }, new[] { 2, -1 }, new[] { 2, 1 } };
                    foreach (var km in knightMoves)
                    {
                        int nr = row + km[0], nc = col + km[1];
                        if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                        {
                            char target = board.GetPiece(nr, nc);
                            if (target == '.' || char.IsUpper(target) != isWhite)
                                moves.Add((nr, nc));
                        }
                    }
                    break;

                case PieceType.King:
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr == 0 && dc == 0) continue;
                            int nr = row + dr, nc = col + dc;
                            if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                            {
                                char target = board.GetPiece(nr, nc);
                                if (target == '.' || char.IsUpper(target) != isWhite)
                                    moves.Add((nr, nc));
                            }
                        }
                    }
                    break;

                case PieceType.Bishop:
                case PieceType.Rook:
                case PieceType.Queen:
                    int[][] dirs = pieceType == PieceType.Bishop
                        ? new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } }
                        : pieceType == PieceType.Rook
                            ? new[] { new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } }
                            : new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 },
                                      new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } };

                    foreach (var d in dirs)
                    {
                        int nr = row + d[0], nc = col + d[1];
                        while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                        {
                            char target = board.GetPiece(nr, nc);
                            if (target == '.')
                            {
                                moves.Add((nr, nc));
                            }
                            else
                            {
                                if (char.IsUpper(target) != isWhite)
                                    moves.Add((nr, nc));
                                break;
                            }
                            nr += d[0];
                            nc += d[1];
                        }
                    }
                    break;
            }

            return moves;
        }

        #endregion Helper Methods
    }
}