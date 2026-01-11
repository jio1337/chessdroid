using System;
using System.Collections.Generic;
using System.Linq;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Endgame analysis and special position detection
    /// Inspired by Ethereal's syzygy.c and endgame evaluation
    /// </summary>
    public class EndgameAnalysis
    {
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
                        if (piece == '.')  continue;

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
        // Position where any move worsens the position
        // =============================

        /// <summary>
        /// Detect potential zugzwang characteristics
        /// Common in pawn endgames and blocked positions
        /// </summary>
        public static bool IsPotentialZugzwang(ChessBoard board, bool isWhite)
        {
            try
            {
                // Zugzwang is common in endgames with few pieces
                if (!IsEndgame(board))
                    return false;

                // Count pieces and mobility
                int totalPieces = CountTotalPieces(board);

                // Very few pieces = higher zugzwang likelihood
                if (totalPieces <= 4)
                    return true;

                // Check for blocked pawn structures (common zugzwang indicator)
                int blockedPawns = CountBlockedPawns(board);
                int totalPawns = CountPawns(board);

                // If most pawns are blocked, zugzwang is more likely
                if (totalPawns > 0 && blockedPawns >= totalPawns / 2)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
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
    }
}
