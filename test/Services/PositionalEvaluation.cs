using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Positional evaluation inspired by Ethereal chess engine
    /// Analyzes pawn structure, piece activity, king safety, and space control
    /// </summary>
    public class PositionalEvaluation
    {
        // =============================
        // POSITIONAL EVALUATION FEATURES
        // Inspired by Ethereal's evaluate.c
        // =============================

        #region Pawn Structure Analysis

        /// <summary>
        /// Detect isolated pawns (no friendly pawns on adjacent files)
        /// Weakness: Can't be defended by other pawns
        /// </summary>
        public static string? DetectIsolatedPawn(ChessBoard board, int pawnRow, int pawnCol, bool isWhite)
        {
            try
            {
                // Check adjacent files for friendly pawns
                bool hasLeftSupport = false;
                bool hasRightSupport = false;

                if (pawnCol > 0) // Check left file
                {
                    for (int r = 0; r < 8; r++)
                    {
                        char piece = board.GetPiece(r, pawnCol - 1);
                        if (piece == (isWhite ? 'P' : 'p'))
                        {
                            hasLeftSupport = true;
                            break;
                        }
                    }
                }

                if (pawnCol < 7) // Check right file
                {
                    for (int r = 0; r < 8; r++)
                    {
                        char piece = board.GetPiece(r, pawnCol + 1);
                        if (piece == (isWhite ? 'P' : 'p'))
                        {
                            hasRightSupport = true;
                            break;
                        }
                    }
                }

                if (!hasLeftSupport && !hasRightSupport)
                {
                    // File names for explanation
                    string file = ((char)('a' + pawnCol)).ToString();
                    return $"creates isolated {file}-pawn";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect doubled pawns (two or more pawns on same file)
        /// Weakness: Less mobile, can't defend each other
        /// </summary>
        public static string? DetectDoubledPawns(ChessBoard board, int pawnRow, int pawnCol, bool isWhite)
        {
            try
            {
                char pawnChar = isWhite ? 'P' : 'p';
                int pawnCount = 0;

                for (int r = 0; r < 8; r++)
                {
                    if (board.GetPiece(r, pawnCol) == pawnChar)
                        pawnCount++;
                }

                if (pawnCount >= 2)
                {
                    string file = ((char)('a' + pawnCol)).ToString();
                    return $"creates doubled {file}-pawns";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect backward pawns (can't advance safely, no friendly pawns behind on adjacent files)
        /// Weakness: Stuck, can become a target
        /// </summary>
        public static string? DetectBackwardPawn(ChessBoard board, int pawnRow, int pawnCol, bool isWhite)
        {
            try
            {
                int direction = isWhite ? -1 : 1; // White moves up (decreasing row), Black moves down
                int nextRow = pawnRow + direction;

                // Can't advance if blocked
                if (nextRow >= 0 && nextRow < 8 && board.GetPiece(nextRow, pawnCol) != '.')
                    return null;

                // Check if this pawn is behind all adjacent pawns (backward)
                bool isBehind = false;

                // Check left and right files
                for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
                {
                    int adjacentFile = pawnCol + fileOffset;
                    if (adjacentFile < 0 || adjacentFile >= 8) continue;

                    for (int r = 0; r < 8; r++)
                    {
                        char piece = board.GetPiece(r, adjacentFile);
                        if (piece == (isWhite ? 'P' : 'p'))
                        {
                            // Check if this adjacent pawn is ahead of our pawn
                            if (isWhite && r < pawnRow) // Adjacent pawn more advanced (lower row)
                                isBehind = true;
                            else if (!isWhite && r > pawnRow) // Adjacent pawn more advanced (higher row)
                                isBehind = true;
                        }
                    }
                }

                if (isBehind)
                {
                    // Check if the square in front is attacked by enemy pawns
                    if (nextRow >= 0 && nextRow < 8)
                    {
                        bool underAttack = IsPawnAttacked(board, nextRow, pawnCol, !isWhite);
                        if (underAttack)
                            return "creates backward pawn";
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect connected pawns (pawns on adjacent files that DEFEND each other)
        /// Definition: "Connected pawns are two or more pawns of the same color on adjacent files
        /// that defend each other, forming a strong defensive unit or pawn chain."
        /// Strength: Mutually supporting, strong pawn chain
        /// </summary>
        public static string? DetectConnectedPawns(ChessBoard board, int pawnRow, int pawnCol, bool isWhite)
        {
            try
            {
                char pawnChar = isWhite ? 'P' : 'p';

                // Connected pawns MUST defend each other
                // A pawn defends a square diagonally in front of it
                // For white: defends diagonally upward (lower row numbers)
                // For black: defends diagonally downward (higher row numbers)

                // Check if THIS pawn is defended by another pawn (pawn behind and diagonal)
                int supportDirection = isWhite ? 1 : -1; // Direction our pawns come FROM
                for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
                {
                    int supportRow = pawnRow + supportDirection;
                    int supportCol = pawnCol + fileOffset;

                    if (supportRow >= 0 && supportRow < 8 && supportCol >= 0 && supportCol < 8)
                    {
                        if (board.GetPiece(supportRow, supportCol) == pawnChar)
                        {
                            // Found a pawn that DEFENDS this pawn - truly connected
                            return "creates connected pawns";
                        }
                    }
                }

                // Also check if THIS pawn defends another pawn (pawn ahead and diagonal)
                int defendDirection = isWhite ? -1 : 1; // Direction we attack
                for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
                {
                    int defendRow = pawnRow + defendDirection;
                    int defendCol = pawnCol + fileOffset;

                    if (defendRow >= 0 && defendRow < 8 && defendCol >= 0 && defendCol < 8)
                    {
                        if (board.GetPiece(defendRow, defendCol) == pawnChar)
                        {
                            // This pawn defends another pawn - truly connected
                            return "creates connected pawns";
                        }
                    }
                }

                // Pawns on same rank but adjacent files are NOT connected unless they can defend each other
                // (which they can't since pawns only capture diagonally forward)
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect passed pawns (no enemy pawns ahead on same file or adjacent files)
        /// Strength: Can advance freely toward promotion
        /// </summary>
        public static string? DetectPassedPawn(ChessBoard board, int pawnRow, int pawnCol, bool isWhite)
        {
            try
            {
                char enemyPawn = isWhite ? 'p' : 'P';
                int direction = isWhite ? -1 : 1; // Direction toward promotion

                // Check this file and adjacent files ahead
                for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
                {
                    int checkFile = pawnCol + fileOffset;
                    if (checkFile < 0 || checkFile >= 8) continue;

                    // Scan from pawn position toward promotion rank
                    int row = pawnRow + direction;
                    while (row >= 0 && row < 8)
                    {
                        if (board.GetPiece(row, checkFile) == enemyPawn)
                            return null; // Enemy pawn blocking, not passed

                        row += direction;
                    }
                }

                // No enemy pawns ahead - it's a passed pawn
                int distanceToPromotion = isWhite ? pawnRow : (7 - pawnRow);

                if (distanceToPromotion <= 2)
                    return "creates dangerous passed pawn";
                else
                    return "creates passed pawn";
            }
            catch
            {
                return null;
            }
        }

        #endregion Pawn Structure Analysis

        #region Piece Activity Analysis

        /// <summary>
        /// Detect outpost (piece on square enemy pawns can't attack)
        /// Definition: "An outpost is a strategically secure square, typically on the 4th, 5th, or 6th rank,
        /// that is defended by a friendly pawn and CANNOT BE ATTACKED BY ANY of the opponent's pawns."
        /// Key: No enemy pawn on adjacent files can ever advance to attack this square
        /// Strength: Stable, strong square especially for knights
        /// </summary>
        public static string? DetectOutpost(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);

                // Outposts are most valuable for knights and bishops
                if (pieceType != PieceType.Knight && pieceType != PieceType.Bishop)
                    return null;

                // First check: Must be on 4th, 5th, or 6th rank (row 2, 3, or 4 for white; row 3, 4, or 5 for black)
                // For white: ranks 4-6 are rows 4, 3, 2 (0-indexed from white's perspective)
                // For black: ranks 4-6 are rows 3, 4, 5
                bool onCorrectRank = isWhite ? (pieceRow >= 2 && pieceRow <= 4) : (pieceRow >= 3 && pieceRow <= 5);
                if (!onCorrectRank)
                    return null;

                // Second check: MUST be supported by our pawn
                bool supportedByPawn = IsPawnSupported(board, pieceRow, pieceCol, isWhite);
                if (!supportedByPawn)
                    return null;

                // Third check (CRITICAL): No enemy pawn on adjacent files can EVER attack this square
                // This means checking the ENTIRE adjacent file from the enemy's starting rank up to our square
                char enemyPawn = isWhite ? 'p' : 'P';

                // Check both adjacent files (left and right)
                for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
                {
                    int adjacentFile = pieceCol + fileOffset;
                    if (adjacentFile < 0 || adjacentFile >= 8) continue;

                    // For white outpost: check if any black pawn on the adjacent file
                    // is positioned such that it could advance to attack our square
                    // Black pawns move DOWN (increasing row numbers), attack diagonally
                    // So a black pawn at row R attacks squares at row R+1, files +/-1
                    // Our piece is at (pieceRow, pieceCol). For a pawn to attack it from adjacentFile,
                    // the pawn must be at row (pieceRow - 1) for black (attacking down-diagonal)
                    // BUT any black pawn ABOVE that row (lower row number) could advance to attack

                    if (isWhite)
                    {
                        // Black pawns start at row 1, move toward row 7
                        // A black pawn at row R can attack our square if R < pieceRow (and on adjacent file)
                        // because it can advance to row (pieceRow - 1) and then attack diagonally
                        for (int r = 1; r < pieceRow; r++) // Check from black's second rank down to just above our piece
                        {
                            if (board.GetPiece(r, adjacentFile) == enemyPawn)
                            {
                                // This pawn could potentially advance to attack our square
                                // Check if the path is clear for it to reach the attacking position
                                bool pathClear = true;
                                for (int checkRow = r + 1; checkRow < pieceRow; checkRow++)
                                {
                                    if (board.GetPiece(checkRow, adjacentFile) != '.')
                                    {
                                        pathClear = false;
                                        break;
                                    }
                                }
                                if (pathClear)
                                    return null; // Enemy pawn can advance to attack - NOT an outpost
                            }
                        }
                    }
                    else
                    {
                        // White pawns start at row 6, move toward row 0
                        // A white pawn at row R can attack our square if R > pieceRow
                        for (int r = 6; r > pieceRow; r--) // Check from white's second rank up to just below our piece
                        {
                            if (board.GetPiece(r, adjacentFile) == enemyPawn)
                            {
                                // This pawn could potentially advance to attack our square
                                bool pathClear = true;
                                for (int checkRow = r - 1; checkRow > pieceRow; checkRow--)
                                {
                                    if (board.GetPiece(checkRow, adjacentFile) != '.')
                                    {
                                        pathClear = false;
                                        break;
                                    }
                                }
                                if (pathClear)
                                    return null; // Enemy pawn can advance to attack - NOT an outpost
                            }
                        }
                    }
                }

                // All checks passed - this is a true outpost
                return $"{ChessUtilities.GetPieceName(pieceType)} on strong outpost";
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Calculate piece mobility (number of legal moves)
        /// Higher mobility = more active piece
        /// </summary>
        public static string? DetectHighMobility(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);

                // Only evaluate mobility for knights, bishops, rooks, queens
                if (pieceType == PieceType.Pawn || pieceType == PieceType.King)
                    return null;

                int moveCount = CountPossibleMoves(board, pieceRow, pieceCol, piece, isWhite);

                // Mobility thresholds (inspired by Ethereal)
                int highMobilityThreshold = pieceType switch
                {
                    PieceType.Knight => 6,  // Knight has max 8 moves
                    PieceType.Bishop => 10, // Bishop diagonal moves
                    PieceType.Rook => 12,   // Rook file/rank moves
                    PieceType.Queen => 20,  // Queen combines both
                    _ => 0
                };

                if (moveCount >= highMobilityThreshold)
                    return "maximizes piece activity";

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect long diagonal control for bishops (Ethereal bonus)
        /// Bishops controlling both central squares on long diagonals
        /// </summary>
        public static string? DetectLongDiagonalControl(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                if (pieceType != PieceType.Bishop) return null;

                // Check if bishop is on a long diagonal (a1-h8 or a8-h1)
                bool onLongDiagonal = (pieceRow == pieceCol) || (pieceRow + pieceCol == 7);

                if (!onLongDiagonal) return null;

                // Check if bishop controls both central squares (d4, e4, d5, e5)
                List<(int, int)> centralSquares = new List<(int, int)>
                {
                    (4, 3), // d4
                    (4, 4), // e4
                    (3, 3), // d5
                    (3, 4)  // e5
                };

                int controlledCentralSquares = 0;
                foreach (var (r, c) in centralSquares)
                {
                    if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                        controlledCentralSquares++;
                }

                if (controlledCentralSquares >= 2)
                    return "bishop controls long diagonal";

                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion Piece Activity Analysis

        #region King Safety Analysis

        /// <summary>
        /// Evaluate king shelter (friendly pawn shield)
        /// Inspired by Ethereal's king safety evaluation
        /// </summary>
        public static string? DetectKingShelter(ChessBoard board, int kingRow, int kingCol, bool isWhite)
        {
            try
            {
                char friendlyPawn = isWhite ? 'P' : 'p';
                int pawnShieldRow = isWhite ? kingRow + 1 : kingRow - 1;

                if (pawnShieldRow < 0 || pawnShieldRow >= 8) return null;

                int pawnShield = 0;

                // Check three files in front of king
                for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
                {
                    int checkFile = kingCol + fileOffset;
                    if (checkFile < 0 || checkFile >= 8) continue;

                    if (board.GetPiece(pawnShieldRow, checkFile) == friendlyPawn)
                        pawnShield++;
                }

                if (pawnShield == 0)
                    return "exposes king (no pawn shield)";
                else if (pawnShield == 1)
                    return "weakens king safety";

                // Good pawn shield (2-3 pawns)
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detect weak squares near king
        /// Squares attacked by opponent but only defended by queen/king
        /// </summary>
        public static string? DetectWeakKingSquares(ChessBoard board, int kingRow, int kingCol, bool isWhite)
        {
            try
            {
                int weakSquareCount = 0;

                // Check squares around king
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;

                        int r = kingRow + dr;
                        int c = kingCol + dc;

                        if (r < 0 || r >= 8 || c < 0 || c >= 8) continue;

                        // Is this square attacked by enemy?
                        bool attackedByEnemy = ChessUtilities.IsSquareDefended(board, r, c, !isWhite);

                        // Is it only defended by our king or queen?
                        bool poorlyDefended = !IsSquareWellDefended(board, r, c, isWhite);

                        if (attackedByEnemy && poorlyDefended)
                            weakSquareCount++;
                    }
                }

                if (weakSquareCount >= 3)
                    return "creates weak squares near king";

                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion King Safety Analysis

        #region Space Control

        /// <summary>
        /// Detect central control (controlling key center squares)
        /// Inspired by Ethereal's space evaluation
        /// </summary>
        public static string? DetectCentralControl(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                // Central squares: d4, e4, d5, e5
                List<(int row, int col)> centralSquares = new List<(int, int)>
                {
                    (4, 3), // d4
                    (4, 4), // e4
                    (3, 3), // d5
                    (3, 4)  // e5
                };

                int controlledSquares = 0;
                foreach (var (r, c) in centralSquares)
                {
                    if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                        controlledSquares++;
                }

                if (controlledSquares >= 2)
                    return "controls center";

                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion Space Control

        #region Helper Methods

        private static bool IsPawnAttacked(ChessBoard board, int row, int col, bool byWhite)
        {
            char pawn = byWhite ? 'P' : 'p';
            int attackDirection = byWhite ? 1 : -1; // Direction pawns attack FROM

            // Check two diagonal attack positions
            for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
            {
                int attackRow = row + attackDirection;
                int attackCol = col + fileOffset;

                if (attackRow >= 0 && attackRow < 8 && attackCol >= 0 && attackCol < 8)
                {
                    if (board.GetPiece(attackRow, attackCol) == pawn)
                        return true;
                }
            }

            return false;
        }

        private static bool IsPawnSupported(ChessBoard board, int row, int col, bool isWhite)
        {
            char pawn = isWhite ? 'P' : 'p';
            int supportDirection = isWhite ? 1 : -1; // Direction our pawns come FROM

            // Check two diagonal positions behind the piece
            for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
            {
                int supportRow = row + supportDirection;
                int supportCol = col + fileOffset;

                if (supportRow >= 0 && supportRow < 8 && supportCol >= 0 && supportCol < 8)
                {
                    if (board.GetPiece(supportRow, supportCol) == pawn)
                        return true;
                }
            }

            return false;
        }

        private static int CountPossibleMoves(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            int moveCount = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (r == pieceRow && c == pieceCol) continue;

                    if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                    {
                        char targetPiece = board.GetPiece(r, c);
                        // Can move to empty squares or enemy pieces
                        if (targetPiece == '.' || char.IsUpper(targetPiece) != isWhite)
                            moveCount++;
                    }
                }
            }

            return moveCount;
        }

        private static bool IsSquareWellDefended(ChessBoard board, int row, int col, bool byWhite)
        {
            int defenderCount = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != byWhite) continue;

                    PieceType type = PieceHelper.GetPieceType(piece);

                    // Don't count king or queen as "good" defenders
                    if (type == PieceType.King || type == PieceType.Queen)
                        continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, row, col))
                        defenderCount++;
                }
            }

            return defenderCount >= 1;
        }

        #endregion Helper Methods
    }
}