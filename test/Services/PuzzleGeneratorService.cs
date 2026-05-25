using ChessDroid.Models;

namespace ChessDroid.Services
{
    public record GeneratedPuzzle(
        string Fen,
        string SolutionUci,
        string TacticType,
        string Description
    );

    /// <summary>
    /// Generates chess puzzle positions from scratch using piece placement + tactical verification.
    /// No external databases — positions are constructed algorithmically and verified for soundness.
    /// </summary>
    public static class PuzzleGeneratorService
    {
        private static readonly Random _rng = new();
        private static readonly int[] _kdr = { -2, -2, -1, -1,  1, 1,  2, 2 };
        private static readonly int[] _kdc = { -1,  1, -2,  2, -2, 2, -1, 1 };

        // ── Public entry points ──────────────────────────────────────────

        public static GeneratedPuzzle? TryGenerateKnightFork(int maxAttempts = 500)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                var p = BuildKnightFork();
                if (p != null) return p;
            }
            return null;
        }

        // ── Knight Fork ──────────────────────────────────────────────────

        private static GeneratedPuzzle? BuildKnightFork()
        {
            // Knight destination: inner 6×6 grid so it always has enough attack squares
            int dstR = _rng.Next(1, 7);
            int dstC = _rng.Next(1, 7);

            var attacked = KnightMoves(dstR, dstC);
            if (attacked.Count < 2) return null;

            // Black king on one of the attacked squares
            var (kR, kC) = attacked[_rng.Next(attacked.Count)];

            // Black queen on a different attacked square
            var others = attacked.Where(t => t != (kR, kC)).ToList();
            if (others.Count == 0) return null;
            var (qR, qC) = others[_rng.Next(others.Count)];

            // Knight source: any square that can jump to dstR/dstC, not on target squares
            var sources = KnightMoves(dstR, dstC)
                .Where(s => s != (kR, kC) && s != (qR, qC))
                .ToList();
            if (sources.Count == 0) return null;
            var (srcR, srcC) = sources[_rng.Next(sources.Count)];

            var board = new ChessBoard();
            board.SetPiece(srcR, srcC, 'N');  // White knight at source
            board.SetPiece(kR, kC, 'k');       // Black king
            board.SetPiece(qR, qC, 'q');       // Black queen

            // White king: safe, not adjacent to the fork zone
            var wk = FindSafeKingSquare(board, isWhite: true,
                avoid: new[] { (kR, kC), (dstR, dstC), (qR, qC) }, minDist: 2);
            if (wk == null) return null;
            board.SetPiece(wk.Value.r, wk.Value.c, 'K');

            // Position must be legal before the move
            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: true))  return null;
            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: false)) return null;

            // Simulate the knight jump and verify it gives check (forking check)
            using var pooled = BoardPool.Rent(board);
            var tmp = pooled.Board;
            tmp.SetPiece(dstR, dstC, 'N');
            tmp.SetPiece(srcR, srcC, '.');

            if (!ChessUtilities.IsKingInCheck(tmp, kingIsWhite: false)) return null;
            if ( ChessUtilities.IsKingInCheck(tmp, kingIsWhite: true))  return null; // knight was pinned

            // Build FEN (white to move, no castling, no en passant)
            string fen = $"{board.ToFEN()} w - - 0 1";
            string uci = $"{(char)('a' + srcC)}{8 - srcR}{(char)('a' + dstC)}{8 - dstR}";
            string kSq = ChessUtilities.GetSquareName(kR, kC);
            string qSq = ChessUtilities.GetSquareName(qR, qC);

            return new GeneratedPuzzle(fen, uci, "Knight Fork",
                $"Fork — check on {kSq}, queen on {qSq}");
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static List<(int row, int col)> KnightMoves(int r, int c)
        {
            var list = new List<(int, int)>();
            for (int i = 0; i < 8; i++)
            {
                int nr = r + _kdr[i], nc = c + _kdc[i];
                if ((uint)nr < 8 && (uint)nc < 8) list.Add((nr, nc));
            }
            return list;
        }

        private static (int r, int c)? FindSafeKingSquare(
            ChessBoard board, bool isWhite,
            (int r, int c)[] avoid, int minDist)
        {
            char king = isWhite ? 'K' : 'k';
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int r = _rng.Next(8), c = _rng.Next(8);
                if (board.GetPiece(r, c) != '.') continue;
                // Keep king away from the fork zone to avoid illegal adjacency
                if (avoid.Any(a => Math.Abs(r - a.r) < minDist && Math.Abs(c - a.c) < minDist))
                    continue;
                board.SetPiece(r, c, king);
                bool safe = !ChessUtilities.IsKingInCheck(board, isWhite);
                board.SetPiece(r, c, '.');
                if (safe) return (r, c);
            }
            return null;
        }
    }
}
