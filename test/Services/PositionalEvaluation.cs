using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Detect connected pawns (adjacent pawns on same rank or one rank apart)
        /// Strength: Mutually supporting, strong pawn chain
        /// </summary>
        public static string? DetectConnectedPawns(ChessBoard board, int pawnRow, int pawnCol, bool isWhite)
        {
            try
            {
                char pawnChar = isWhite ? 'P' : 'p';

                // Check adjacent files within one rank
                for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2) // -1 or +1
                {
                    int adjacentFile = pawnCol + fileOffset;
                    if (adjacentFile < 0 || adjacentFile >= 8) continue;

                    for (int rankOffset = -1; rankOffset <= 1; rankOffset++)
                    {
                        int adjacentRow = pawnRow + rankOffset;
                        if (adjacentRow < 0 || adjacentRow >= 8) continue;

                        if (board.GetPiece(adjacentRow, adjacentFile) == pawnChar)
                        {
                            return "creates connected pawns";
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

        #endregion

        #region Piece Activity Analysis

        /// <summary>
        /// Detect outpost (piece on square enemy pawns can't attack)
        /// Inspired by Ethereal's outpost bonus
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

                // Check if this square can be attacked by enemy pawns
                char enemyPawn = isWhite ? 'p' : 'P';
                int pawnAttackDirection = isWhite ? 1 : -1; // Opposite of pawn move direction

                // Check two diagonal attack squares (where enemy pawns could attack from)
                for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
                {
                    int attackFromRow = pieceRow + pawnAttackDirection;
                    int attackFromCol = pieceCol + fileOffset;

                    if (attackFromRow >= 0 && attackFromRow < 8 && attackFromCol >= 0 && attackFromCol < 8)
                    {
                        // Check if enemy pawn exists on this square OR can advance to attack
                        if (board.GetPiece(attackFromRow, attackFromCol) == enemyPawn)
                            return null; // Can be attacked by pawn

                        // Also check if enemy pawn is behind and can advance to attack
                        int pawnBehindRow = attackFromRow + pawnAttackDirection;
                        if (pawnBehindRow >= 0 && pawnBehindRow < 8)
                        {
                            if (board.GetPiece(pawnBehindRow, attackFromCol) == enemyPawn)
                                return null; // Pawn can advance to attack
                        }
                    }
                }

                // CRITICAL: An outpost MUST be supported by our pawn
                // Definition: A secure square protected by a friendly pawn and immune to enemy pawn attacks
                bool supportedByPawn = IsPawnSupported(board, pieceRow, pieceCol, isWhite);

                // If not pawn-supported, it's NOT an outpost (even if enemy pawns can't attack)
                if (!supportedByPawn)
                    return null;

                // Check if in enemy territory (advanced outpost)
                // Outposts are typically on 4th, 5th, or 6th rank
                bool inEnemyTerritory = isWhite ? pieceRow <= 4 : pieceRow >= 3;

                if (inEnemyTerritory)
                {
                    return $"{ChessUtilities.GetPieceName(pieceType)} on strong outpost";
                }

                return null;
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

        #endregion

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

        #endregion

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

        #endregion

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

        // Moved to ChessUtilities: IsSquareDefended, CanAttackSquare, IsPathClear, GetPieceName

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

        #endregion
    }
}
