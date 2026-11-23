using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Enumerates legal moves and detects check, operating purely on a ChessBoard plus
    /// explicit state parameters — no instance fields, no WinForms dependency.
    ///
    /// Before this, the one complete and correct legal-move implementation (castling incl.
    /// Chess960, en passant, check/pin filtering via full-board simulation) lived as private
    /// methods on ChessBoardControl, a WinForms Control, used only to highlight UI click
    /// targets. Core's own ChessRulesService.HasAnyLegalMove was a second, independent,
    /// incomplete implementation (no castling moves, broken en passant) that happened to
    /// stay roughly in sync by luck rather than by sharing code. This class is a faithful
    /// port of ChessBoardControl's logic — see CHESSDROID_AUDIT.md Phase 4 ("extract
    /// GameSession/single MoveGenerator out of ChessBoardControl, which currently owns
    /// rules+legality"). ChessBoardControl now delegates to this instead of maintaining its
    /// own copy; see PerftTests.cs (ported from ChessDroid.Tests.App to this project) for the
    /// regression coverage that verified the port is behavior-identical.
    /// </summary>
    public static class MoveGenerator
    {
        public static List<string> GetAllLegalMovesUci(
            ChessBoard board, bool whiteToMove, CastlingState castling, (int Row, int Col)? enPassant)
        {
            var moves = new List<string>();
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    char piece = board.GetPiece(row, col);
                    if (piece == '.' || char.IsUpper(piece) != whiteToMove) continue;

                    foreach (var (toRow, toCol) in GetLegalDestinations(board, row, col, whiteToMove, castling, enPassant))
                    {
                        bool isPromotion = char.ToLower(piece) == 'p' && (toRow == 0 || toRow == 7);
                        if (isPromotion)
                        {
                            foreach (char promo in new[] { 'q', 'r', 'b', 'n' })
                                moves.Add(ToUciMove(row, col, toRow, toCol, promo));
                        }
                        else
                        {
                            moves.Add(ToUciMove(row, col, toRow, toCol));
                        }
                    }
                }
            }
            return moves;
        }

        public static HashSet<(int row, int col)> GetLegalDestinations(
            ChessBoard board, int row, int col, bool whiteToMove, CastlingState castling, (int Row, int Col)? enPassant)
        {
            var moves = new HashSet<(int row, int col)>();
            char piece = board.GetPiece(row, col);
            if (piece == '.') return moves;

            for (int toRow = 0; toRow < 8; toRow++)
                for (int toCol = 0; toCol < 8; toCol++)
                    if (IsLegalMove(board, row, col, toRow, toCol, whiteToMove, castling, enPassant))
                        moves.Add((toRow, toCol));

            return moves;
        }

        public static bool IsLegalMove(
            ChessBoard board, int fromRow, int fromCol, int toRow, int toCol,
            bool whiteToMove, CastlingState castling, (int Row, int Col)? enPassant)
        {
            char targetPiece = board.GetPiece(toRow, toCol);
            char movingPiece = board.GetPiece(fromRow, fromCol);
            bool isSameColor = targetPiece != '.' &&
                ((char.IsUpper(movingPiece) && char.IsUpper(targetPiece)) ||
                 (char.IsLower(movingPiece) && char.IsLower(targetPiece)));

            // Chess960 exception: king moving to its own rook = castling (not a capture)
            bool isChess960Castle = castling.Chess960Mode && isSameColor &&
                char.ToLower(movingPiece) == 'k' && char.ToLower(targetPiece) == 'r' &&
                fromRow == toRow && (fromRow == 7 || fromRow == 0);

            if (isSameColor && !isChess960Castle) return false;

            if (isChess960Castle)
            {
                if (!IsValidCastling960(board, fromRow, fromCol, toCol, whiteToMove, castling)) return false;
            }
            else if (!castling.Chess960Mode && char.ToLower(movingPiece) == 'k' && Math.Abs(toCol - fromCol) == 2 && fromRow == toRow)
            {
                if (!IsValidCastling(board, fromRow, fromCol, toCol, whiteToMove, castling)) return false;
            }
            else if (char.ToLower(movingPiece) == 'p')
            {
                if (!IsValidPawnMove(board, fromRow, fromCol, toRow, toCol, movingPiece, targetPiece, enPassant))
                    return false;
            }
            else
            {
                if (!ChessRulesService.CanReachSquare(board, fromRow, fromCol, movingPiece, toRow, toCol))
                    return false;
            }

            // Check if move leaves king in check
            using var pooled = BoardPool.Rent(board);
            ChessBoard testBoard = pooled.Board;

            testBoard.SetPiece(toRow, toCol, movingPiece);
            testBoard.SetPiece(fromRow, fromCol, '.');

            if (char.ToLower(movingPiece) == 'k' && Math.Abs(fromCol - toCol) == 2)
            {
                if (toCol > fromCol)
                {
                    testBoard.SetPiece(fromRow, 5, testBoard.GetPiece(fromRow, 7));
                    testBoard.SetPiece(fromRow, 7, '.');
                }
                else
                {
                    testBoard.SetPiece(fromRow, 3, testBoard.GetPiece(fromRow, 0));
                    testBoard.SetPiece(fromRow, 0, '.');
                }
            }

            if (char.ToLower(movingPiece) == 'p' && fromCol != toCol && targetPiece == '.')
            {
                testBoard.SetPiece(fromRow, toCol, '.');
            }

            return !IsKingInCheck(testBoard, whiteToMove);
        }

        private static bool IsValidPawnMove(
            ChessBoard board, int fromRow, int fromCol, int toRow, int toCol, char pawn, char targetPiece,
            (int Row, int Col)? enPassant)
        {
            bool isWhite = char.IsUpper(pawn);
            int direction = isWhite ? -1 : 1; // White moves up (decreasing row), black moves down
            int startRow = isWhite ? 6 : 1;
            int dr = toRow - fromRow;
            int df = toCol - fromCol;

            if (df == 0)
            {
                if (dr == direction) return targetPiece == '.';
                if (dr == 2 * direction && fromRow == startRow)
                {
                    char middlePiece = board.GetPiece(fromRow + direction, fromCol);
                    return targetPiece == '.' && middlePiece == '.';
                }
                return false;
            }

            if (Math.Abs(df) == 1 && dr == direction)
            {
                if (targetPiece != '.')
                {
                    bool targetIsEnemy = isWhite ? char.IsLower(targetPiece) : char.IsUpper(targetPiece);
                    return targetIsEnemy;
                }

                if (enPassant.HasValue && toRow == enPassant.Value.Row && toCol == enPassant.Value.Col)
                    return true;

                return false;
            }

            return false;
        }

        private static bool IsValidCastling(
            ChessBoard board, int kingRow, int kingCol, int targetCol, bool whiteToMove, CastlingState castling)
        {
            bool isWhiteKing = whiteToMove;
            int expectedRow = isWhiteKing ? 7 : 0;

            if (kingRow != expectedRow || kingCol != 4) return false;

            bool isKingside = targetCol > kingCol;

            if (isWhiteKing)
            {
                if (isKingside && !castling.WhiteKingside) return false;
                if (!isKingside && !castling.WhiteQueenside) return false;
            }
            else
            {
                if (isKingside && !castling.BlackKingside) return false;
                if (!isKingside && !castling.BlackQueenside) return false;
            }

            int rookCol = isKingside ? 7 : 0;
            char expectedRook = isWhiteKing ? 'R' : 'r';
            if (board.GetPiece(kingRow, rookCol) != expectedRook) return false;

            int startCol = Math.Min(kingCol, rookCol) + 1;
            int endCol = Math.Max(kingCol, rookCol);
            for (int col = startCol; col < endCol; col++)
                if (board.GetPiece(kingRow, col) != '.') return false;

            if (IsKingInCheck(board, isWhiteKing)) return false;

            int passThroughCol = isKingside ? 5 : 3;
            using var pooled = BoardPool.Rent(board);
            ChessBoard testBoard = pooled.Board;
            char king = isWhiteKing ? 'K' : 'k';
            testBoard.SetPiece(kingRow, passThroughCol, king);
            testBoard.SetPiece(kingRow, kingCol, '.');
            if (IsKingInCheck(testBoard, isWhiteKing)) return false;

            return true;
        }

        // Chess960 castling: king moves to the rook's starting square. King ends on g-file
        // (kingside) or c-file (queenside); rook ends on f/d file.
        private static bool IsValidCastling960(
            ChessBoard board, int kingRow, int kingCol, int rookCol, bool whiteToMove, CastlingState castling)
        {
            bool isWhiteKing = whiteToMove;
            bool kingside = rookCol > kingCol;
            int kingDest = kingside ? 6 : 2;
            int rookDest = kingside ? 5 : 3;

            if (isWhiteKing)
            {
                if (kingside  && (!castling.WhiteKingside  || rookCol != castling.WhiteKingsideRookFile)) return false;
                if (!kingside && (!castling.WhiteQueenside || rookCol != castling.WhiteQueensideRookFile)) return false;
            }
            else
            {
                if (kingside  && (!castling.BlackKingside  || rookCol != castling.BlackKingsideRookFile)) return false;
                if (!kingside && (!castling.BlackQueenside || rookCol != castling.BlackQueensideRookFile)) return false;
            }

            char expectedRook = isWhiteKing ? 'R' : 'r';
            if (board.GetPiece(kingRow, rookCol) != expectedRook) return false;

            // All squares between king's start and king's destination, and between rook's
            // start and rook's destination, must be empty — excluding the king/rook start squares.
            int minF = Math.Min(Math.Min(kingCol, rookCol), Math.Min(kingDest, rookDest));
            int maxF = Math.Max(Math.Max(kingCol, rookCol), Math.Max(kingDest, rookDest));
            for (int f = minF; f <= maxF; f++)
            {
                if (f == kingCol || f == rookCol) continue;
                if (board.GetPiece(kingRow, f) != '.') return false;
            }

            if (IsKingInCheck(board, isWhiteKing)) return false;

            int step = kingside ? 1 : -1;
            for (int f = kingCol + step; f != kingDest + step; f += step)
            {
                using var pooled = BoardPool.Rent(board);
                ChessBoard testBoard = pooled.Board;
                char king = isWhiteKing ? 'K' : 'k';
                testBoard.SetPiece(kingRow, kingCol, '.');
                testBoard.SetPiece(kingRow, f, king);
                if (IsKingInCheck(testBoard, isWhiteKing)) return false;
            }

            return true;
        }

        public static bool IsKingInCheck(ChessBoard testBoard, bool whiteKing)
        {
            char king = whiteKing ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (testBoard.GetPiece(r, c) == king)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
                if (kingRow != -1) break;
            }

            if (kingRow == -1) return false; // No king found (shouldn't happen)

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = testBoard.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool isEnemyPiece = whiteKing ? char.IsLower(piece) : char.IsUpper(piece);
                    if (isEnemyPiece && CanAttackSquare(testBoard, r, c, piece, kingRow, kingCol))
                        return true;
                }
            }

            return false;
        }

        /// <summary>Checks if a piece can attack a square (geometry + path clear for sliding pieces).</summary>
        private static bool CanAttackSquare(ChessBoard testBoard, int fromRow, int fromCol, char piece, int toRow, int toCol)
        {
            if (!ChessRulesService.CanReachSquare(testBoard, fromRow, fromCol, piece, toRow, toCol))
                return false;

            char pieceLower = char.ToLower(piece);

            // Knights and kings don't need path checking (they jump or move 1 square)
            if (pieceLower == 'n' || pieceLower == 'k') return true;

            // Pawns attack diagonally, not forward — already validated by CanReachSquare
            if (pieceLower == 'p') return fromCol != toCol;

            // Sliding pieces (queen, rook, bishop): check the path is clear
            int dr = Math.Sign(toRow - fromRow);
            int dc = Math.Sign(toCol - fromCol);

            int currentRow = fromRow + dr;
            int currentCol = fromCol + dc;

            while (currentRow != toRow || currentCol != toCol)
            {
                if (testBoard.GetPiece(currentRow, currentCol) != '.') return false; // Path is blocked
                currentRow += dr;
                currentCol += dc;
            }

            return true;
        }

        private static string ToUciMove(int fromRow, int fromCol, int toRow, int toCol, char? promotion = null)
        {
            char fromFile = (char)('a' + fromCol);
            char fromRank = (char)('1' + (7 - fromRow));
            char toFile = (char)('a' + toCol);
            char toRank = (char)('1' + (7 - toRow));

            string move = $"{fromFile}{fromRank}{toFile}{toRank}";
            if (promotion.HasValue) move += char.ToLower(promotion.Value);
            return move;
        }
    }
}
