using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Service for chess rules logic (castling, en passant, move validation, etc.)
    /// </summary>
    public class ChessRulesService
    {
        /// <summary>
        /// Removes castling rights when king moves
        /// </summary>
        public static void RemoveKingCastlingRights(ref string castlingRights, char kingPiece)
        {
            if (char.IsUpper(kingPiece))
                castlingRights = castlingRights.Replace("K", "").Replace("Q", "");
            else
                castlingRights = castlingRights.Replace("k", "").Replace("q", "");
        }

        /// <summary>
        /// Removes castling rights when rook moves or is captured
        /// </summary>
        public static void RemoveRookCastlingRights(ref string castlingRights, int rank, int file)
        {
            if (rank == 7 && file == 0) castlingRights = castlingRights.Replace("Q", "");
            if (rank == 7 && file == 7) castlingRights = castlingRights.Replace("K", "");
            if (rank == 0 && file == 0) castlingRights = castlingRights.Replace("q", "");
            if (rank == 0 && file == 7) castlingRights = castlingRights.Replace("k", "");
        }

        /// <summary>
        /// Detects what move was made by comparing two board states and updates castling rights
        /// </summary>
        public static (string uciMove, string updatedCastling, string newEnPassant) DetectMoveAndUpdateCastling(
            ChessBoard oldBoard, ChessBoard newBoard, string currentCastling)
        {
            if (oldBoard == null || newBoard == null)
                return ("", currentCastling, "-");

            int srcRow = -1, srcCol = -1, dstRow = -1, dstCol = -1;
            char movedPiece = '.';
            char capturedPiece = '.';

            // Find the source and destination squares
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char oldPiece = oldBoard[r, c];
                    char newPiece = newBoard[r, c];

                    if (oldPiece != '.' && newPiece == '.')
                    {
                        srcRow = r;
                        srcCol = c;
                        movedPiece = oldPiece;
                    }
                    else if (oldPiece == '.' && newPiece != '.')
                    {
                        dstRow = r;
                        dstCol = c;
                    }
                    else if (oldPiece != newPiece && oldPiece != '.')
                    {
                        srcRow = r;
                        srcCol = c;
                        movedPiece = oldPiece;
                        dstRow = r;
                        dstCol = c;
                        capturedPiece = oldPiece;
                    }
                }
            }

            if (srcRow == -1 || dstRow == -1)
                return ("", currentCastling, "-");

            char srcFile = (char)('a' + srcCol);
            char srcRank = (char)('8' - srcRow);
            char dstFile = (char)('a' + dstCol);
            char dstRank = (char)('8' - dstRow);
            string uciMove = $"{srcFile}{srcRank}{dstFile}{dstRank}";

            string updatedCastling = currentCastling;

            // Update castling rights if king moved
            if (PieceHelper.GetPieceType(movedPiece) == PieceType.King)
            {
                RemoveKingCastlingRights(ref updatedCastling, movedPiece);
            }

            // Update castling rights if rook moved
            if (PieceHelper.GetPieceType(movedPiece) == PieceType.Rook)
            {
                RemoveRookCastlingRights(ref updatedCastling, srcRow, srcCol);
            }

            // Update castling rights if rook was captured
            if (PieceHelper.GetPieceType(capturedPiece) == PieceType.Rook)
            {
                RemoveRookCastlingRights(ref updatedCastling, dstRow, dstCol);
            }

            string newEnPassant = "-";
            if (PieceHelper.GetPieceType(movedPiece) == PieceType.Pawn)
            {
                int delta = dstRow - srcRow;
                if (Math.Abs(delta) == 2)
                {
                    int epRank = (srcRow + dstRow) / 2;
                    char epFile = (char)('a' + srcCol);
                    char epRankChar = (char)('8' - epRank);
                    newEnPassant = $"{epFile}{epRankChar}";
                }
            }

            return (uciMove, updatedCastling, newEnPassant);
        }

        /// <summary>
        /// Applies a UCI move to a board, handling captures, castling, en passant, promotions
        /// </summary>
        public static void ApplyUciMove(ChessBoard board, string uciMove, ref string castlingRights, ref string enPassant)
        {
            if (string.IsNullOrEmpty(uciMove) || board == null) return;
            if (uciMove.Length < 4) return;

            string src = uciMove.Substring(0, 2);
            string dst = uciMove.Substring(2, 2);

            // Validate input
            if (src.Length != 2 || dst.Length != 2) return;
            if (src[0] < 'a' || src[0] > 'h' || src[1] < '1' || src[1] > '8') return;
            if (dst[0] < 'a' || dst[0] > 'h' || dst[1] < '1' || dst[1] > '8') return;

            int srcFile = src[0] - 'a';
            int srcRank = 8 - (src[1] - '0');
            int dstFile = dst[0] - 'a';
            int dstRank = 8 - (dst[1] - '0');

            char moving = board[srcRank, srcFile];
            char target = board[dstRank, dstFile];

            // Handle en-passant capture
            if (PieceHelper.GetPieceType(moving) == PieceType.Pawn && target == '.' && srcFile != dstFile)
            {
                // If pawn moved diagonally to empty square, it may be en-passant
                if (!string.IsNullOrEmpty(enPassant) && enPassant.Length >= 2)
                {
                    // Validate en passant square
                    if (enPassant[0] >= 'a' && enPassant[0] <= 'h' && enPassant[1] >= '1' && enPassant[1] <= '8')
                    {
                        int epFile = enPassant[0] - 'a';
                        int epRank = 8 - (enPassant[1] - '0');
                        if (dstFile == epFile && dstRank == epRank)
                        {
                            // capture the pawn behind
                            int capturedPawnRank = (char.IsUpper(moving)) ? dstRank + 1 : dstRank - 1;
                            board[capturedPawnRank, dstFile] = '.';
                        }
                    }
                }
            }

            // Move piece
            board[dstRank, dstFile] = moving;
            board[srcRank, srcFile] = '.';

            // Handle promotions
            if (uciMove.Length > 4)
            {
                char prom = uciMove[4];
                char promPiece = char.IsUpper(moving) ? char.ToUpper(prom) : char.ToLower(prom);
                board[dstRank, dstFile] = promPiece;
            }

            // Handle castling rook moves
            if (PieceHelper.GetPieceType(moving) == PieceType.King)
            {
                // white king
                if (srcFile == 4 && srcRank == 7)
                {
                    if (dstFile == 6 && dstRank == 7)
                    {
                        board[7, 5] = board[7, 7];
                        board[7, 7] = '.';
                    }
                    if (dstFile == 2 && dstRank == 7)
                    {
                        board[7, 3] = board[7, 0];
                        board[7, 0] = '.';
                    }
                }
                // black king
                if (srcFile == 4 && srcRank == 0)
                {
                    if (dstFile == 6 && dstRank == 0)
                    {
                        board[0, 5] = board[0, 7];
                        board[0, 7] = '.';
                    }
                    if (dstFile == 2 && dstRank == 0)
                    {
                        board[0, 3] = board[0, 0];
                        board[0, 0] = '.';
                    }
                }

                // Remove castling rights for that color
                RemoveKingCastlingRights(ref castlingRights, moving);
            }

            // Update castling rights if rook moved
            if (PieceHelper.GetPieceType(moving) == PieceType.Rook)
            {
                RemoveRookCastlingRights(ref castlingRights, srcRank, srcFile);
            }

            // Update castling rights if rook was captured
            if (PieceHelper.GetPieceType(target) == PieceType.Rook)
            {
                RemoveRookCastlingRights(ref castlingRights, dstRank, dstFile);
            }

            // Update en-passant: if a pawn moved two squares, set target
            enPassant = "-";
            if (PieceHelper.GetPieceType(moving) == PieceType.Pawn)
            {
                if (Math.Abs(dstRank - srcRank) == 2)
                {
                    int epRank = (srcRank + dstRank) / 2;
                    char epFile = (char)('a' + srcFile);
                    char epRankChar = (char)('8' - epRank);
                    enPassant = $"{epFile}{epRankChar}";
                }
            }

            if (string.IsNullOrEmpty(castlingRights)) castlingRights = "-";
        }

        /// <summary>
        /// Determines if a piece can legally reach a target square (checks move geometry and path clearance).
        /// Overload without en passant parameter for backward compatibility with delegates.
        /// </summary>
        public static bool CanReachSquare(ChessBoard board, int fromRow, int fromCol, char piece, int toRow, int toCol)
        {
            return CanReachSquareWithEnPassant(board, fromRow, fromCol, piece, toRow, toCol, enPassantSquare: null);
        }

        /// <summary>
        /// Determines if a piece can legally reach a target square (checks move geometry and path clearance for sliding pieces).
        /// </summary>
        public static bool CanReachSquareWithEnPassant(ChessBoard board, int fromRow, int fromCol, char piece, int toRow, int toCol, string? enPassantSquare)
        {
            PieceType pt = PieceHelper.GetPieceType(piece);
            int dr = toRow - fromRow;
            int df = toCol - fromCol;

            switch (pt)
            {
                case PieceType.Knight:
                    return (Math.Abs(dr) == 2 && Math.Abs(df) == 1) || (Math.Abs(dr) == 1 && Math.Abs(df) == 2);

                case PieceType.King:
                    return Math.Abs(dr) <= 1 && Math.Abs(df) <= 1 && (dr != 0 || df != 0);

                case PieceType.Pawn:
                    bool isWhite = char.IsUpper(piece);
                    int direction = isWhite ? -1 : 1;
                    char destPiece = board[toRow, toCol];
                    if (df == 0)
                    {
                        // Straight move - destination must be empty
                        if (destPiece != '.') return false;
                        return dr == direction || (dr == 2 * direction && ((isWhite && fromRow == 6) || (!isWhite && fromRow == 1)));
                    }
                    // Diagonal move - must be a capture (enemy piece on destination) OR en passant
                    if (dr == direction && Math.Abs(df) == 1)
                    {
                        // Check for en passant: destination is empty but matches en passant square
                        if (destPiece == '.')
                        {
                            if (!string.IsNullOrEmpty(enPassantSquare) && enPassantSquare != "-" && enPassantSquare.Length >= 2)
                            {
                                int epCol = enPassantSquare[0] - 'a';
                                int epRow = 7 - (enPassantSquare[1] - '1');
                                if (toRow == epRow && toCol == epCol)
                                    return true; // Valid en passant capture
                            }
                            return false; // Can't capture empty square (not en passant)
                        }
                        bool destIsEnemy = char.IsUpper(destPiece) != isWhite;
                        return destIsEnemy;
                    }
                    return false;

                case PieceType.Bishop:
                    if (Math.Abs(dr) != Math.Abs(df) || dr == 0) return false;
                    return IsPathClear(board, fromRow, fromCol, toRow, toCol);

                case PieceType.Rook:
                    if (!((dr == 0 && df != 0) || (df == 0 && dr != 0))) return false;
                    return IsPathClear(board, fromRow, fromCol, toRow, toCol);

                case PieceType.Queen:
                    if (!((dr == 0 && df != 0) || (df == 0 && dr != 0) || (Math.Abs(dr) == Math.Abs(df) && dr != 0))) return false;
                    return IsPathClear(board, fromRow, fromCol, toRow, toCol);
            }
            return false;
        }

        /// <summary>
        /// Checks if the path between two squares is clear (no pieces blocking).
        /// Used for sliding pieces (rook, bishop, queen).
        /// </summary>
        private static bool IsPathClear(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol)
        {
            int dr = Math.Sign(toRow - fromRow);
            int df = Math.Sign(toCol - fromCol);

            int currentRow = fromRow + dr;
            int currentCol = fromCol + df;

            // Check all squares between source and destination (exclusive)
            while (currentRow != toRow || currentCol != toCol)
            {
                if (board[currentRow, currentCol] != '.')
                    return false; // Path is blocked

                currentRow += dr;
                currentCol += df;
            }

            return true; // Path is clear
        }

        /// <summary>
        /// Finds all pieces of the same type that can reach a destination square.
        /// Overload without en passant parameter for backward compatibility with delegates.
        /// </summary>
        public static List<(int row, int col)> FindAllPiecesOfSameType(ChessBoard board, char pieceLower, bool isWhite, int destRank, int destFile)
        {
            return FindAllPiecesOfSameTypeWithEnPassant(board, pieceLower, isWhite, destRank, destFile, enPassantSquare: null);
        }

        /// <summary>
        /// Finds all pieces of the same type that can reach a destination square
        /// </summary>
        public static List<(int row, int col)> FindAllPiecesOfSameTypeWithEnPassant(ChessBoard board, char pieceLower, bool isWhite, int destRank, int destFile, string? enPassantSquare)
        {
            var result = new List<(int, int)>();
            for (int r = 0; r < 8; r++)
            {
                for (int f = 0; f < 8; f++)
                {
                    char p = board[r, f];
                    if (p == '.') continue;
                    if (char.IsUpper(p) != isWhite) continue;
                    if (char.ToLower(p) != pieceLower) continue;

                    if (CanReachSquareWithEnPassant(board, r, f, p, destRank, destFile, enPassantSquare))
                    {
                        result.Add((r, f));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Counts differences between two board states
        /// </summary>
        public static int CountBoardDifferences(ChessBoard board1, ChessBoard board2)
        {
            int differences = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board1[r, c] != board2[r, c])
                        differences++;
                }
            }
            return differences;
        }

        /// <summary>
        /// Infers castling rights from the current board position
        /// </summary>
        public static string InferCastlingRights(ChessBoard board)
        {
            string rights = "";

            // Check White kingside castling (K on e1, R on h1)
            if (board.GetPiece(7, 4) == 'K' && board.GetPiece(7, 7) == 'R')
                rights += "K";

            // Check White queenside castling (K on e1, R on a1)
            if (board.GetPiece(7, 4) == 'K' && board.GetPiece(7, 0) == 'R')
                rights += "Q";

            // Check Black kingside castling (k on e8, r on h8)
            if (board.GetPiece(0, 4) == 'k' && board.GetPiece(0, 7) == 'r')
                rights += "k";

            // Check Black queenside castling (k on e8, r on a8)
            if (board.GetPiece(0, 4) == 'k' && board.GetPiece(0, 0) == 'r')
                rights += "q";

            return string.IsNullOrEmpty(rights) ? "-" : rights;
        }
    }
}