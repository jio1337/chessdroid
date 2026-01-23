using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Endgame analysis and special position detection
    /// Inspired by Stockfish, Ethereal, and Lc0 endgame evaluation techniques
    /// Implements heuristic-based evaluation without requiring tablebases
    /// </summary>
    public class EndgameAnalysis
    {
        // King centralization table (center = highest value)
        // Used for mop-up evaluation and king activity scoring
        private static readonly int[,] KingCentralizationTable = new int[8, 8]
        {
            { 0, 1, 2, 3, 3, 2, 1, 0 },
            { 1, 2, 3, 4, 4, 3, 2, 1 },
            { 2, 3, 4, 5, 5, 4, 3, 2 },
            { 3, 4, 5, 6, 6, 5, 4, 3 },
            { 3, 4, 5, 6, 6, 5, 4, 3 },
            { 2, 3, 4, 5, 5, 4, 3, 2 },
            { 1, 2, 3, 4, 4, 3, 2, 1 },
            { 0, 1, 2, 3, 3, 2, 1, 0 }
        };

        // Corner proximity table (corners = highest value)
        // Used for mop-up evaluation when pushing king to corner
        private static readonly int[,] CornerProximityTable = new int[8, 8]
        {
            { 6, 5, 4, 3, 3, 4, 5, 6 },
            { 5, 4, 3, 2, 2, 3, 4, 5 },
            { 4, 3, 2, 1, 1, 2, 3, 4 },
            { 3, 2, 1, 0, 0, 1, 2, 3 },
            { 3, 2, 1, 0, 0, 1, 2, 3 },
            { 4, 3, 2, 1, 1, 2, 3, 4 },
            { 5, 4, 3, 2, 2, 3, 4, 5 },
            { 6, 5, 4, 3, 3, 4, 5, 6 }
        };

        // =============================
        // ENDGAME DETECTION
        // Inspired by Ethereal's tablebase integration
        // =============================

        /// <summary>
        /// Count total pieces on the board (excluding kings)
        /// </summary>
        public static int CountTotalPieces(ChessBoard board)
        {
            int count = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece != '.' && char.ToUpper(piece) != 'K')
                        count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Detect if position is in endgame phase
        /// Endgame: 6 or fewer pieces (excluding kings)
        /// </summary>
        public static bool IsEndgame(ChessBoard board)
        {
            return CountTotalPieces(board) <= 6;
        }

        /// <summary>
        /// Detect if position is in late endgame (tablebase territory)
        /// Late endgame: 5 or fewer pieces (excluding kings)
        /// </summary>
        public static bool IsLateEndgame(ChessBoard board)
        {
            return CountTotalPieces(board) <= 5;
        }

        /// <summary>
        /// Detect if position is in middlegame
        /// </summary>
        public static bool IsMiddlegame(ChessBoard board)
        {
            return CountTotalPieces(board) >= 12;
        }

        /// <summary>
        /// Get game phase description
        /// </summary>
        public static string GetGamePhase(ChessBoard board)
        {
            int pieces = CountTotalPieces(board);

            if (pieces <= 5)
                return "late endgame";
            else if (pieces <= 6)
                return "endgame";
            else if (pieces <= 11)
                return "transition";
            else
                return "middlegame";
        }

        // =============================
        // SPECIFIC ENDGAME PATTERNS
        // =============================

        /// <summary>
        /// Detect King and Pawn vs King endgame
        /// Critical endgame: requires precise technique
        /// </summary>
        public static string? DetectKPvK(ChessBoard board, bool isWhite)
        {
            try
            {
                int whitePawns = 0, blackPawns = 0;
                int whiteOthers = 0, blackOthers = 0;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        if (char.IsUpper(piece))
                        {
                            if (piece == 'P') whitePawns++;
                            else if (piece != 'K') whiteOthers++;
                        }
                        else
                        {
                            if (piece == 'p') blackPawns++;
                            else if (piece != 'k') blackOthers++;
                        }
                    }
                }

                // King and Pawn vs King
                if (isWhite && whitePawns == 1 && whiteOthers == 0 && blackPawns == 0 && blackOthers == 0)
                    return "King and pawn endgame (technical win)";

                if (!isWhite && blackPawns == 1 && blackOthers == 0 && whitePawns == 0 && whiteOthers == 0)
                    return "King and pawn endgame (technical win)";

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect opposite-colored bishops endgame
        /// Often drawn even with material advantage
        /// </summary>
        public static string? DetectOppositeBishops(ChessBoard board)
        {
            try
            {
                int whiteBishopSquareColor = -1; // -1 = none, 0 = dark, 1 = light
                int blackBishopSquareColor = -1;
                int whiteBishopCount = 0, blackBishopCount = 0;
                int otherPieces = 0;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        if (piece == 'B')
                        {
                            whiteBishopCount++;
                            whiteBishopSquareColor = (r + c) % 2; // 0 = dark, 1 = light
                        }
                        else if (piece == 'b')
                        {
                            blackBishopCount++;
                            blackBishopSquareColor = (r + c) % 2;
                        }
                        else if (piece != 'K' && piece != 'k' && piece != 'P' && piece != 'p')
                        {
                            otherPieces++; // Non-pawn, non-bishop piece
                        }
                    }
                }

                // Exactly one bishop per side, opposite colors, no other pieces
                if (whiteBishopCount == 1 && blackBishopCount == 1 && otherPieces == 0 &&
                    whiteBishopSquareColor != blackBishopSquareColor && whiteBishopSquareColor != -1)
                {
                    return "opposite-colored bishops (drawish)";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect rook endgame
        /// One of the most common and complex endgames
        /// </summary>
        public static string? DetectRookEndgame(ChessBoard board)
        {
            try
            {
                int whiteRooks = 0, blackRooks = 0;
                int otherWhitePieces = 0, otherBlackPieces = 0;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        if (char.IsUpper(piece))
                        {
                            if (piece == 'R') whiteRooks++;
                            else if (piece != 'K' && piece != 'P') otherWhitePieces++;
                        }
                        else
                        {
                            if (piece == 'r') blackRooks++;
                            else if (piece != 'k' && piece != 'p') otherBlackPieces++;
                        }
                    }
                }

                // Both sides have rooks, no other pieces (except pawns and kings)
                if (whiteRooks >= 1 && blackRooks >= 1 && otherWhitePieces == 0 && otherBlackPieces == 0)
                {
                    if (whiteRooks == 1 && blackRooks == 1)
                        return "rook endgame (technical)";
                    else
                        return "rook endgame";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect queen endgame
        /// Highly tactical, often decided by checks
        /// </summary>
        public static string? DetectQueenEndgame(ChessBoard board)
        {
            try
            {
                int whiteQueens = 0, blackQueens = 0;
                int otherWhitePieces = 0, otherBlackPieces = 0;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        if (char.IsUpper(piece))
                        {
                            if (piece == 'Q') whiteQueens++;
                            else if (piece != 'K' && piece != 'P') otherWhitePieces++;
                        }
                        else
                        {
                            if (piece == 'q') blackQueens++;
                            else if (piece != 'k' && piece != 'p') otherBlackPieces++;
                        }
                    }
                }

                // Both sides have queens, no other pieces (except pawns)
                if (whiteQueens >= 1 && blackQueens >= 1 && otherWhitePieces == 0 && otherBlackPieces == 0)
                    return "queen endgame (tactical)";

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect bare king (checkmate is inevitable)
        /// </summary>
        public static string? DetectBareKing(ChessBoard board, bool checkForWhite)
        {
            try
            {
                int whiteNonKing = 0, blackNonKing = 0;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        if (char.IsUpper(piece) && piece != 'K')
                            whiteNonKing++;
                        else if (char.IsLower(piece) && piece != 'k')
                            blackNonKing++;
                    }
                }

                if (checkForWhite && blackNonKing == 0 && whiteNonKing > 0)
                    return "bare king (forced checkmate)";

                if (!checkForWhite && whiteNonKing == 0 && blackNonKing > 0)
                    return "bare king (forced checkmate)";

                return null;
            }
            catch
            {
                return null;
            }
        }

        // =============================
        // ZUGZWANG DETECTION
        // Position where the side to move is at a disadvantage
        // because they MUST move (passing would be better)
        // =============================

        /// <summary>
        /// Detect potential zugzwang characteristics and return description
        /// Zugzwang: the obligation to move is a disadvantage
        /// </summary>
        public static string? DetectZugzwangPotential(ChessBoard board, bool whiteToMove)
        {
            try
            {
                // Zugzwang is only relevant in endgames
                if (!IsEndgame(board))
                    return null;

                int totalPieces = CountTotalPieces(board);
                int blockedPawns = CountBlockedPawns(board);
                int totalPawns = CountPawns(board);

                // Pure pawn endgames with blocked structures are zugzwang-prone
                bool isPureKingPawnEndgame = true;
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece != '.' && piece != 'K' && piece != 'k' && piece != 'P' && piece != 'p')
                        {
                            isPureKingPawnEndgame = false;
                            break;
                        }
                    }
                    if (!isPureKingPawnEndgame) break;
                }

                // King + Pawn endgames with blocked pawns are classic zugzwang territory
                if (isPureKingPawnEndgame && totalPawns > 0 && blockedPawns >= totalPawns / 2)
                {
                    string sideToMove = whiteToMove ? "White" : "Black";
                    return $"critical position - {sideToMove} to move (move carefully, tempo matters)";
                }

                // Very few pieces (2-3) = likely zugzwang territory
                if (totalPieces <= 3 && isPureKingPawnEndgame)
                {
                    string sideToMove = whiteToMove ? "White" : "Black";
                    return $"zugzwang-prone position - {sideToMove} must find the only good move";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Legacy method for compatibility - returns bool
        /// </summary>
        public static bool IsPotentialZugzwang(ChessBoard board, bool isWhite)
        {
            return DetectZugzwangPotential(board, isWhite) != null;
        }

        private static int CountBlockedPawns(ChessBoard board)
        {
            int blocked = 0;

            for (int r = 1; r < 7; r++) // Skip first and last ranks
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == 'P') // White pawn
                    {
                        char ahead = board.GetPiece(r - 1, c);
                        if (ahead != '.')
                            blocked++;
                    }
                    else if (piece == 'p') // Black pawn
                    {
                        char ahead = board.GetPiece(r + 1, c);
                        if (ahead != '.')
                            blocked++;
                    }
                }
            }

            return blocked;
        }

        private static int CountPawns(ChessBoard board)
        {
            int count = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == 'P' || piece == 'p')
                        count++;
                }
            }
            return count;
        }

        // =============================
        // MATERIAL IMBALANCE DETECTION
        // =============================

        /// <summary>
        /// Calculate material balance (positive = White ahead, negative = Black ahead)
        /// </summary>
        public static int CalculateMaterialBalance(ChessBoard board)
        {
            int balance = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    int value = GetPieceValue(PieceHelper.GetPieceType(piece));

                    if (char.IsUpper(piece))
                        balance += value;
                    else
                        balance -= value;
                }
            }

            return balance;
        }

        /// <summary>
        /// Detect significant material imbalance
        /// </summary>
        public static string? DetectMaterialImbalance(ChessBoard board)
        {
            int balance = CalculateMaterialBalance(board);

            if (Math.Abs(balance) >= 3)
            {
                if (balance > 0)
                    return $"White is up {balance} points of material";
                else
                    return $"Black is up {Math.Abs(balance)} points of material";
            }

            return null;
        }

        /// <summary>
        /// Detect quality vs quantity imbalances (e.g., rook and pawns vs bishop and knight)
        /// </summary>
        public static string? DetectQualityImbalance(ChessBoard board)
        {
            try
            {
                int whiteMinor = 0, blackMinor = 0; // Knights and bishops
                int whiteRooks = 0, blackRooks = 0;
                int whiteQueens = 0, blackQueens = 0;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        PieceType type = PieceHelper.GetPieceType(piece);

                        if (char.IsUpper(piece))
                        {
                            if (type == PieceType.Knight || type == PieceType.Bishop) whiteMinor++;
                            if (type == PieceType.Rook) whiteRooks++;
                            if (type == PieceType.Queen) whiteQueens++;
                        }
                        else
                        {
                            if (type == PieceType.Knight || type == PieceType.Bishop) blackMinor++;
                            if (type == PieceType.Rook) blackRooks++;
                            if (type == PieceType.Queen) blackQueens++;
                        }
                    }
                }

                // Rook vs two minors (roughly equal, but depends on position)
                if (whiteRooks > blackRooks && blackMinor >= whiteMinor + 2)
                    return "material imbalance: rooks vs minor pieces";

                if (blackRooks > whiteRooks && whiteMinor >= blackMinor + 2)
                    return "material imbalance: rooks vs minor pieces";

                // Queen vs rook and minor
                if (whiteQueens > blackQueens && (blackRooks + blackMinor) >= 2)
                    return "material imbalance: queen vs rook and minor";

                if (blackQueens > whiteQueens && (whiteRooks + whiteMinor) >= 2)
                    return "material imbalance: queen vs rook and minor";

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static int GetPieceValue(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => 1,
                PieceType.Knight => 3,
                PieceType.Bishop => 3,
                PieceType.Rook => 5,
                PieceType.Queen => 9,
                PieceType.King => 0, // Don't count king in material
                _ => 0
            };
        }

        // =============================
        // RULE OF THE SQUARE
        // Inspired by Stockfish's unstoppable pawn evaluation
        // =============================

        /// <summary>
        /// Determines if a passed pawn can queen without the enemy king catching it
        /// Rule of the Square: If the defending king is outside the square formed
        /// by the pawn and the promotion square, the pawn cannot be stopped
        /// </summary>
        public static bool CanPawnPromoteUnstoppable(ChessBoard board, int pawnRow, int pawnCol, bool pawnIsWhite, bool whiteToMove)
        {
            try
            {
                // Find the defending king
                int defenderKingRow = -1, defenderKingCol = -1;
                char defenderKing = pawnIsWhite ? 'k' : 'K';

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (board.GetPiece(r, c) == defenderKing)
                        {
                            defenderKingRow = r;
                            defenderKingCol = c;
                            break;
                        }
                    }
                    if (defenderKingRow >= 0) break;
                }

                if (defenderKingRow < 0) return true; // No defending king found

                // Calculate promotion rank and distance
                int promotionRow = pawnIsWhite ? 0 : 7;
                int distanceToPromotion = Math.Abs(pawnRow - promotionRow);

                // Adjust for tempo (if it's defender's turn, they get one extra step)
                bool defenderToMove = (pawnIsWhite && !whiteToMove) || (!pawnIsWhite && whiteToMove);
                int defenderBonus = defenderToMove ? 1 : 0;

                // Calculate the square
                int squareSize = distanceToPromotion;

                // King distance to the promotion square (using Chebyshev distance)
                int kingDistToPromoSquare = Math.Max(
                    Math.Abs(defenderKingRow - promotionRow),
                    Math.Abs(defenderKingCol - pawnCol)
                );

                // If king can't reach the square in time, pawn promotes
                return kingDistToPromoSquare > squareSize + defenderBonus;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Detect unstoppable passed pawn for either side
        /// Returns description if found
        /// </summary>
        public static string? DetectUnstoppablePawn(ChessBoard board, bool whiteToMove)
        {
            try
            {
                var passedPawns = FindPassedPawns(board);

                foreach (var (row, col, isWhite) in passedPawns)
                {
                    if (CanPawnPromoteUnstoppable(board, row, col, isWhite, whiteToMove))
                    {
                        string side = isWhite ? "White" : "Black";
                        string square = $"{(char)('a' + col)}{8 - row}";
                        return $"{side} has unstoppable passed pawn on {square}";
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
        // KING CENTRALIZATION
        // Inspired by Stockfish and Ethereal king activity evaluation
        // =============================

        /// <summary>
        /// Calculate king centralization score (0-6, higher = more central)
        /// In endgames, central king is usually stronger
        /// </summary>
        public static int GetKingCentralization(int kingRow, int kingCol)
        {
            if (kingRow < 0 || kingRow >= 8 || kingCol < 0 || kingCol >= 8)
                return 0;
            return KingCentralizationTable[kingRow, kingCol];
        }

        /// <summary>
        /// Evaluate king activity advantage
        /// Returns positive for White advantage, negative for Black
        /// </summary>
        public static string? EvaluateKingActivity(ChessBoard board)
        {
            try
            {
                if (!IsEndgame(board)) return null;

                int whiteKingRow = -1, whiteKingCol = -1;
                int blackKingRow = -1, blackKingCol = -1;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == 'K') { whiteKingRow = r; whiteKingCol = c; }
                        if (piece == 'k') { blackKingRow = r; blackKingCol = c; }
                    }
                }

                if (whiteKingRow < 0 || blackKingRow < 0) return null;

                int whiteCentralization = GetKingCentralization(whiteKingRow, whiteKingCol);
                int blackCentralization = GetKingCentralization(blackKingRow, blackKingCol);

                int diff = whiteCentralization - blackCentralization;

                if (diff >= 3)
                    return "White has much more active king";
                else if (diff >= 2)
                    return "White has more active king";
                else if (diff <= -3)
                    return "Black has much more active king";
                else if (diff <= -2)
                    return "Black has more active king";

                return null;
            }
            catch
            {
                return null;
            }
        }

        // =============================
        // OPPOSITION DETECTION
        // Critical concept in pawn endgames
        // =============================

        /// <summary>
        /// Detect if kings are in opposition (same file or rank, 1 square apart)
        /// The side NOT to move has the opposition (advantage)
        /// </summary>
        public static string? DetectOpposition(ChessBoard board, bool whiteToMove)
        {
            try
            {
                int whiteKingRow = -1, whiteKingCol = -1;
                int blackKingRow = -1, blackKingCol = -1;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == 'K') { whiteKingRow = r; whiteKingCol = c; }
                        if (piece == 'k') { blackKingRow = r; blackKingCol = c; }
                    }
                }

                if (whiteKingRow < 0 || blackKingRow < 0) return null;

                // Direct opposition: same file or rank, exactly 2 squares apart
                bool directOpposition = false;

                // Vertical opposition (same column)
                if (whiteKingCol == blackKingCol && Math.Abs(whiteKingRow - blackKingRow) == 2)
                    directOpposition = true;

                // Horizontal opposition (same row)
                if (whiteKingRow == blackKingRow && Math.Abs(whiteKingCol - blackKingCol) == 2)
                    directOpposition = true;

                // Diagonal opposition (2 squares diagonally)
                if (Math.Abs(whiteKingRow - blackKingRow) == 2 && Math.Abs(whiteKingCol - blackKingCol) == 2)
                    directOpposition = true;

                if (directOpposition)
                {
                    // The side NOT to move has the opposition
                    string holder = whiteToMove ? "Black" : "White";
                    return $"{holder} has the opposition";
                }

                // Distant opposition (same file/rank, even number of squares apart)
                if (whiteKingCol == blackKingCol || whiteKingRow == blackKingRow)
                {
                    int distance = whiteKingCol == blackKingCol
                        ? Math.Abs(whiteKingRow - blackKingRow)
                        : Math.Abs(whiteKingCol - blackKingCol);

                    if (distance > 2 && distance % 2 == 0)
                    {
                        string holder = whiteToMove ? "Black" : "White";
                        return $"{holder} has distant opposition";
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
        // WRONG COLOR BISHOP
        // Rook pawn + bishop that can't control promotion square = draw
        // =============================

        /// <summary>
        /// Detect wrong color bishop scenario with rook pawn
        /// Bishop + rook pawn where bishop can't control promotion square = drawn
        /// </summary>
        public static string? DetectWrongColorBishop(ChessBoard board)
        {
            try
            {
                int whiteBishopRow = -1, whiteBishopCol = -1;
                int blackBishopRow = -1, blackBishopCol = -1;
                List<(int row, int col, bool isWhite)> rookPawns = new();
                int otherPieces = 0;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        if (piece == 'B') { whiteBishopRow = r; whiteBishopCol = c; }
                        else if (piece == 'b') { blackBishopRow = r; blackBishopCol = c; }
                        else if (piece == 'P' && (c == 0 || c == 7))
                            rookPawns.Add((r, c, true));
                        else if (piece == 'p' && (c == 0 || c == 7))
                            rookPawns.Add((r, c, false));
                        else if (piece != 'K' && piece != 'k' && piece != 'P' && piece != 'p')
                            otherPieces++;
                    }
                }

                // Need exactly one bishop and rook pawns, no other pieces
                if (otherPieces > 0) return null;

                // Check white bishop + white rook pawn
                if (whiteBishopRow >= 0 && blackBishopRow < 0)
                {
                    int bishopSquareColor = (whiteBishopRow + whiteBishopCol) % 2;
                    foreach (var (pawnRow, pawnCol, isWhite) in rookPawns)
                    {
                        if (!isWhite) continue;
                        // Promotion square color (row 0)
                        int promotionSquareColor = (0 + pawnCol) % 2;
                        if (bishopSquareColor != promotionSquareColor)
                            return "wrong color bishop (rook pawn cannot promote)";
                    }
                }

                // Check black bishop + black rook pawn
                if (blackBishopRow >= 0 && whiteBishopRow < 0)
                {
                    int bishopSquareColor = (blackBishopRow + blackBishopCol) % 2;
                    foreach (var (pawnRow, pawnCol, isWhite) in rookPawns)
                    {
                        if (isWhite) continue;
                        // Promotion square color (row 7)
                        int promotionSquareColor = (7 + pawnCol) % 2;
                        if (bishopSquareColor != promotionSquareColor)
                            return "wrong color bishop (rook pawn cannot promote)";
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
        // INSUFFICIENT MATERIAL
        // Inspired by FIDE rules and engine implementation
        // =============================

        /// <summary>
        /// Detect insufficient mating material (automatic draw)
        /// </summary>
        public static string? DetectInsufficientMaterial(ChessBoard board)
        {
            try
            {
                int whiteKnights = 0, whiteBishops = 0;
                int blackKnights = 0, blackBishops = 0;
                int whitePawns = 0, blackPawns = 0;
                int whiteRooks = 0, blackRooks = 0;
                int whiteQueens = 0, blackQueens = 0;
                int whiteBishopColor = -1, blackBishopColor = -1;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        switch (piece)
                        {
                            case 'P': whitePawns++; break;
                            case 'p': blackPawns++; break;
                            case 'N': whiteKnights++; break;
                            case 'n': blackKnights++; break;
                            case 'B':
                                whiteBishops++;
                                whiteBishopColor = (r + c) % 2;
                                break;
                            case 'b':
                                blackBishops++;
                                blackBishopColor = (r + c) % 2;
                                break;
                            case 'R': whiteRooks++; break;
                            case 'r': blackRooks++; break;
                            case 'Q': whiteQueens++; break;
                            case 'q': blackQueens++; break;
                        }
                    }
                }

                // If any pawns, rooks, or queens exist, material is sufficient
                if (whitePawns + blackPawns + whiteRooks + blackRooks + whiteQueens + blackQueens > 0)
                    return null;

                // King vs King
                if (whiteKnights + whiteBishops + blackKnights + blackBishops == 0)
                    return "insufficient material (King vs King)";

                // King + minor vs King
                int whiteMaterial = whiteKnights + whiteBishops;
                int blackMaterial = blackKnights + blackBishops;

                if (whiteMaterial <= 1 && blackMaterial == 0)
                    return "insufficient material (cannot force checkmate)";

                if (blackMaterial <= 1 && whiteMaterial == 0)
                    return "insufficient material (cannot force checkmate)";

                // King + Knight vs King + Knight (very drawish)
                if (whiteKnights == 1 && blackKnights == 1 && whiteBishops == 0 && blackBishops == 0)
                    return "insufficient material (Knight vs Knight)";

                // King + Bishop vs King + Bishop (same color)
                if (whiteBishops == 1 && blackBishops == 1 && whiteKnights == 0 && blackKnights == 0)
                {
                    if (whiteBishopColor == blackBishopColor)
                        return "insufficient material (same-colored bishops)";
                }

                // Two knights cannot force mate
                if (whiteKnights == 2 && whiteBishops == 0 && blackMaterial == 0)
                    return "insufficient material (two knights cannot force mate)";

                if (blackKnights == 2 && blackBishops == 0 && whiteMaterial == 0)
                    return "insufficient material (two knights cannot force mate)";

                return null;
            }
            catch
            {
                return null;
            }
        }

        // =============================
        // MOP-UP EVALUATION
        // Inspired by Stockfish's mop-up for KBN vs K, KQ vs K, etc.
        // =============================

        /// <summary>
        /// Calculate mop-up score for endgames where we have mating material
        /// Goal: Push enemy king to corner/edge
        /// Returns positive for White advantage, negative for Black
        /// </summary>
        public static int CalculateMopUpScore(ChessBoard board)
        {
            try
            {
                int whiteKingRow = -1, whiteKingCol = -1;
                int blackKingRow = -1, blackKingCol = -1;
                int whiteMaterial = 0, blackMaterial = 0;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        if (piece == 'K') { whiteKingRow = r; whiteKingCol = c; }
                        else if (piece == 'k') { blackKingRow = r; blackKingCol = c; }
                        else if (char.IsUpper(piece))
                            whiteMaterial += GetPieceValue(PieceHelper.GetPieceType(piece));
                        else
                            blackMaterial += GetPieceValue(PieceHelper.GetPieceType(piece));
                    }
                }

                if (whiteKingRow < 0 || blackKingRow < 0) return 0;

                int score = 0;

                // White has mating advantage - push black king to corner
                if (whiteMaterial >= 3 && blackMaterial == 0)
                {
                    // Bonus for enemy king near corner
                    score += CornerProximityTable[blackKingRow, blackKingCol] * 10;

                    // Bonus for own king close to enemy king (to restrict movement)
                    int kingDistance = Math.Max(
                        Math.Abs(whiteKingRow - blackKingRow),
                        Math.Abs(whiteKingCol - blackKingCol)
                    );
                    score += (8 - kingDistance) * 5;
                }

                // Black has mating advantage - push white king to corner
                if (blackMaterial >= 3 && whiteMaterial == 0)
                {
                    // Bonus for enemy king near corner
                    score -= CornerProximityTable[whiteKingRow, whiteKingCol] * 10;

                    // Bonus for own king close to enemy king
                    int kingDistance = Math.Max(
                        Math.Abs(whiteKingRow - blackKingRow),
                        Math.Abs(whiteKingCol - blackKingCol)
                    );
                    score -= (8 - kingDistance) * 5;
                }

                return score;
            }
            catch
            {
                return 0;
            }
        }

        // =============================
        // PASSED PAWN EVALUATION
        // Inspired by Stockfish's passed pawn bonuses
        // =============================

        /// <summary>
        /// Find all passed pawns on the board
        /// </summary>
        public static List<(int row, int col, bool isWhite)> FindPassedPawns(ChessBoard board)
        {
            var passedPawns = new List<(int row, int col, bool isWhite)>();

            try
            {
                for (int r = 1; r < 7; r++) // Pawns can't be on first or last rank
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);

                        if (piece == 'P') // White pawn
                        {
                            bool isPassed = true;
                            // Check for blocking or adjacent black pawns ahead
                            for (int checkRow = r - 1; checkRow >= 0; checkRow--)
                            {
                                for (int checkCol = Math.Max(0, c - 1); checkCol <= Math.Min(7, c + 1); checkCol++)
                                {
                                    if (board.GetPiece(checkRow, checkCol) == 'p')
                                    {
                                        isPassed = false;
                                        break;
                                    }
                                }
                                if (!isPassed) break;
                            }
                            if (isPassed) passedPawns.Add((r, c, true));
                        }
                        else if (piece == 'p') // Black pawn
                        {
                            bool isPassed = true;
                            // Check for blocking or adjacent white pawns ahead
                            for (int checkRow = r + 1; checkRow <= 7; checkRow++)
                            {
                                for (int checkCol = Math.Max(0, c - 1); checkCol <= Math.Min(7, c + 1); checkCol++)
                                {
                                    if (board.GetPiece(checkRow, checkCol) == 'P')
                                    {
                                        isPassed = false;
                                        break;
                                    }
                                }
                                if (!isPassed) break;
                            }
                            if (isPassed) passedPawns.Add((r, c, false));
                        }
                    }
                }
            }
            catch { }

            return passedPawns;
        }

        /// <summary>
        /// Calculate passed pawn advancement bonus
        /// More advanced = exponentially more dangerous
        /// </summary>
        public static int GetPassedPawnBonus(int row, bool isWhite)
        {
            // Distance to promotion (rank 0 for white, rank 7 for black)
            int distanceToPromotion = isWhite ? row : (7 - row);

            // Exponential bonus: 6th rank pawn is very dangerous
            return distanceToPromotion switch
            {
                1 => 100, // One step from promotion
                2 => 60,  // Two steps
                3 => 30,  // Three steps
                4 => 15,  // Four steps
                5 => 8,   // Five steps
                _ => 4    // Six steps
            };
        }

        /// <summary>
        /// Evaluate passed pawn advantage
        /// </summary>
        public static string? EvaluatePassedPawns(ChessBoard board)
        {
            try
            {
                var passedPawns = FindPassedPawns(board);
                if (passedPawns.Count == 0) return null;

                int whiteBonus = 0, blackBonus = 0;
                string? mostAdvanced = null;
                int mostAdvancedBonus = 0;

                foreach (var (row, col, isWhite) in passedPawns)
                {
                    int bonus = GetPassedPawnBonus(row, isWhite);
                    if (isWhite)
                        whiteBonus += bonus;
                    else
                        blackBonus += bonus;

                    if (bonus > mostAdvancedBonus)
                    {
                        mostAdvancedBonus = bonus;
                        string square = $"{(char)('a' + col)}{8 - row}";
                        int ranksFromPromotion = isWhite ? row : (7 - row);
                        string side = isWhite ? "White" : "Black";
                        mostAdvanced = $"{side}'s passed pawn on {square} ({ranksFromPromotion} ranks from promotion)";
                    }
                }

                return mostAdvanced;
            }
            catch
            {
                return null;
            }
        }

        // =============================
        // KING TROPISM TO PASSED PAWNS
        // Inspired by Stockfish's king proximity to passed pawns
        // =============================

        /// <summary>
        /// Evaluate king proximity to passed pawns
        /// Supporting king near passed pawn = good, defending king far = bad
        /// </summary>
        public static string? EvaluateKingTropism(ChessBoard board)
        {
            try
            {
                var passedPawns = FindPassedPawns(board);
                if (passedPawns.Count == 0) return null;

                int whiteKingRow = -1, whiteKingCol = -1;
                int blackKingRow = -1, blackKingCol = -1;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == 'K') { whiteKingRow = r; whiteKingCol = c; }
                        if (piece == 'k') { blackKingRow = r; blackKingCol = c; }
                    }
                }

                if (whiteKingRow < 0 || blackKingRow < 0) return null;

                foreach (var (pawnRow, pawnCol, isWhite) in passedPawns)
                {
                    int promotionRow = isWhite ? 0 : 7;

                    // Distance of each king to the promotion square
                    int supporterDist = isWhite
                        ? Math.Max(Math.Abs(whiteKingRow - promotionRow), Math.Abs(whiteKingCol - pawnCol))
                        : Math.Max(Math.Abs(blackKingRow - promotionRow), Math.Abs(blackKingCol - pawnCol));

                    int defenderDist = isWhite
                        ? Math.Max(Math.Abs(blackKingRow - promotionRow), Math.Abs(blackKingCol - pawnCol))
                        : Math.Max(Math.Abs(whiteKingRow - promotionRow), Math.Abs(whiteKingCol - pawnCol));

                    // If supporting king is close and defender is far, that's good
                    if (supporterDist <= 2 && defenderDist >= 4)
                    {
                        string side = isWhite ? "White" : "Black";
                        return $"{side} king well-placed to support passed pawn";
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
        // FORTRESS DETECTION
        // Recognize drawable fortress positions
        // =============================

        /// <summary>
        /// Detect potential fortress positions (defensive setup that can't be broken)
        /// </summary>
        public static string? DetectFortress(ChessBoard board)
        {
            try
            {
                // Common fortress: Rook vs Rook + Pawn (rook behind pawn)
                // We'll check for basic fortress indicators

                int whiteRooks = 0, blackRooks = 0;
                int whitePawns = 0, blackPawns = 0;
                int otherWhite = 0, otherBlack = 0;

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        switch (piece)
                        {
                            case 'R': whiteRooks++; break;
                            case 'r': blackRooks++; break;
                            case 'P': whitePawns++; break;
                            case 'p': blackPawns++; break;
                            case 'K': case 'k': break;
                            default:
                                if (char.IsUpper(piece)) otherWhite++;
                                else otherBlack++;
                                break;
                        }
                    }
                }

                // Rook endgame with single pawn - often drawable
                if (whiteRooks == 1 && blackRooks == 1 && otherWhite == 0 && otherBlack == 0)
                {
                    if (whitePawns == 1 && blackPawns == 0)
                        return "potential fortress (R+P vs R is often drawable)";
                    if (blackPawns == 1 && whitePawns == 0)
                        return "potential fortress (R+P vs R is often drawable)";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // =============================
        // COMPREHENSIVE ENDGAME ASSESSMENT
        // =============================

        /// <summary>
        /// Get comprehensive endgame analysis with all applicable insights
        /// </summary>
        public static List<string> GetEndgameInsights(ChessBoard board, bool whiteToMove)
        {
            var insights = new List<string>();

            try
            {
                if (!IsEndgame(board)) return insights;

                // Check for insufficient material (auto-draw)
                var insufficientMaterial = DetectInsufficientMaterial(board);
                if (insufficientMaterial != null)
                {
                    insights.Add(insufficientMaterial);
                    return insights; // No other analysis needed
                }

                // Check for wrong color bishop
                var wrongBishop = DetectWrongColorBishop(board);
                if (wrongBishop != null) insights.Add(wrongBishop);

                // Check for unstoppable pawn
                var unstoppable = DetectUnstoppablePawn(board, whiteToMove);
                if (unstoppable != null) insights.Add(unstoppable);

                // Check for opposition
                var opposition = DetectOpposition(board, whiteToMove);
                if (opposition != null) insights.Add(opposition);

                // Check king activity
                var kingActivity = EvaluateKingActivity(board);
                if (kingActivity != null) insights.Add(kingActivity);

                // Check passed pawns
                var passedPawns = EvaluatePassedPawns(board);
                if (passedPawns != null) insights.Add(passedPawns);

                // Check king tropism to passed pawns
                var tropism = EvaluateKingTropism(board);
                if (tropism != null) insights.Add(tropism);

                // Check for fortress
                var fortress = DetectFortress(board);
                if (fortress != null) insights.Add(fortress);

                // Check for opposite colored bishops
                var oppBishops = DetectOppositeBishops(board);
                if (oppBishops != null) insights.Add(oppBishops);

                // Check for zugzwang potential (with meaningful description)
                var zugzwang = DetectZugzwangPotential(board, whiteToMove);
                if (zugzwang != null) insights.Add(zugzwang);

                // Add specific endgame type
                var kpk = DetectKPvK(board, whiteToMove);
                if (kpk != null) insights.Add(kpk);

                var rookEndgame = DetectRookEndgame(board);
                if (rookEndgame != null) insights.Add(rookEndgame);

                var queenEndgame = DetectQueenEndgame(board);
                if (queenEndgame != null) insights.Add(queenEndgame);
            }
            catch { }

            return insights;
        }
    }
}