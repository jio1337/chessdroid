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

        // Knight move offsets
        private static readonly int[] _kdr = { -2, -2, -1, -1,  1,  1,  2,  2 };
        private static readonly int[] _kdc = { -1,  1, -2,  2, -2,  2, -1,  1 };

        // Queen/rook/bishop ray directions (N S W E and diagonals)
        private static readonly int[][] _dirs =
        {
            new[] { -1,  0 }, new[] {  1,  0 }, new[] {  0, -1 }, new[] {  0,  1 },
            new[] { -1, -1 }, new[] { -1,  1 }, new[] {  1, -1 }, new[] {  1,  1 }
        };

        // ── Public entry points ──────────────────────────────────────────

        public static GeneratedPuzzle? TryGenerateKnightFork(int max = 500)
        {
            for (int i = 0; i < max; i++) { var p = BuildKnightFork();     if (p != null) return p; }
            return null;
        }

        public static GeneratedPuzzle? TryGenerateQueenFork(int max = 500)
        {
            for (int i = 0; i < max; i++) { var p = BuildQueenFork();      if (p != null) return p; }
            return null;
        }

        public static GeneratedPuzzle? TryGenerateBackRankMate(int max = 200)
        {
            for (int i = 0; i < max; i++) { var p = BuildBackRankMate();   if (p != null) return p; }
            return null;
        }

        public static GeneratedPuzzle? TryGenerateSmotheredMate(int max = 300)
        {
            for (int i = 0; i < max; i++) { var p = BuildSmotheredMate();  if (p != null) return p; }
            return null;
        }

        public static GeneratedPuzzle? TryGenerateSkewer(int max = 500)
        {
            for (int i = 0; i < max; i++) { var p = BuildSkewer();         if (p != null) return p; }
            return null;
        }

        public static GeneratedPuzzle? TryGenerateRandom(int max = 500) =>
            _rng.Next(5) switch
            {
                0 => TryGenerateKnightFork(max),
                1 => TryGenerateQueenFork(max),
                2 => TryGenerateBackRankMate(max),
                3 => TryGenerateSmotheredMate(max),
                _ => TryGenerateSkewer(max),
            };

        // ── Knight Fork ──────────────────────────────────────────────────

        private static GeneratedPuzzle? BuildKnightFork()
        {
            // Knight destination: inner 6×6 grid so it always has enough attack squares
            int dstR = _rng.Next(1, 7);
            int dstC = _rng.Next(1, 7);

            var attacked = KnightMoves(dstR, dstC);
            if (attacked.Count < 2) return null;

            var (kR, kC) = attacked[_rng.Next(attacked.Count)];

            var others = attacked.Where(t => t != (kR, kC)).ToList();
            if (others.Count == 0) return null;
            var (qR, qC) = others[_rng.Next(others.Count)];

            var sources = KnightMoves(dstR, dstC)
                .Where(s => s != (kR, kC) && s != (qR, qC))
                .ToList();
            if (sources.Count == 0) return null;
            var (srcR, srcC) = sources[_rng.Next(sources.Count)];

            var board = new ChessBoard();
            board.SetPiece(srcR, srcC, 'N');
            board.SetPiece(kR,   kC,   'k');
            board.SetPiece(qR,   qC,   'q');

            var wk = FindSafeKingSquare(board, true,
                new[] { (kR, kC), (dstR, dstC), (qR, qC) }, 2);
            if (wk == null) return null;
            board.SetPiece(wk.Value.r, wk.Value.c, 'K');

            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: true))  return null;
            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: false)) return null;

            using var pooled = BoardPool.Rent(board);
            var tmp = pooled.Board;
            tmp.SetPiece(dstR, dstC, 'N');
            tmp.SetPiece(srcR, srcC, '.');

            if (!ChessUtilities.IsKingInCheck(tmp, kingIsWhite: false)) return null;
            if ( ChessUtilities.IsKingInCheck(tmp, kingIsWhite: true))  return null;

            string fen  = $"{board.ToFEN()} w - - 0 1";
            string uci  = $"{(char)('a' + srcC)}{8 - srcR}{(char)('a' + dstC)}{8 - dstR}";
            string kSq  = ChessUtilities.GetSquareName(kR, kC);
            string qSq  = ChessUtilities.GetSquareName(qR, qC);
            return new GeneratedPuzzle(fen, uci, "Knight Fork",
                $"Fork — check on {kSq}, queen on {qSq}");
        }

        // ── Queen Fork ───────────────────────────────────────────────────

        private static GeneratedPuzzle? BuildQueenFork()
        {
            // Queen destination: inner board for good ray coverage
            int dstR = _rng.Next(1, 7), dstC = _rng.Next(1, 7);

            // Two distinct ray directions: king on ray d0, target on ray d1
            int d0 = _rng.Next(8);
            int d1 = (d0 + 1 + _rng.Next(7)) % 8;

            var ray0 = GetRaySquares(dstR, dstC, _dirs[d0]);
            var ray1 = GetRaySquares(dstR, dstC, _dirs[d1]);
            if (ray0.Count == 0 || ray1.Count == 0) return null;

            var (kR, kC) = ray0[_rng.Next(ray0.Count)];
            var (tR, tC) = ray1[_rng.Next(ray1.Count)];

            // King must not be able to capture the queen at dst
            if (Math.Abs(dstR - kR) <= 1 && Math.Abs(dstC - kC) <= 1) return null;

            // Queen source: on one of the remaining rays (won't block the fork attacks)
            var srcCandidates = new List<(int r, int c)>();
            for (int di = 0; di < 8; di++)
            {
                if (di == d0 || di == d1) continue;
                srcCandidates.AddRange(GetRaySquares(dstR, dstC, _dirs[di])
                    .Where(s => s != (kR, kC) && s != (tR, tC)));
            }
            if (srcCandidates.Count == 0) return null;
            var (srcR, srcC) = srcCandidates[_rng.Next(srcCandidates.Count)];

            var board = new ChessBoard();
            board.SetPiece(srcR, srcC, 'Q');
            board.SetPiece(kR,   kC,   'k');
            board.SetPiece(tR,   tC,   'q');

            var wk = FindSafeKingSquare(board, true,
                new[] { (kR, kC), (dstR, dstC), (tR, tC) }, 2);
            if (wk == null) return null;
            board.SetPiece(wk.Value.r, wk.Value.c, 'K');

            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: true))  return null;
            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: false)) return null;

            // Queen must have a clear path to the fork square
            if (!ChessUtilities.CanAttackSquare(board, srcR, srcC, 'Q', dstR, dstC)) return null;

            using var pooled = BoardPool.Rent(board);
            var tmp = pooled.Board;
            tmp.SetPiece(dstR, dstC, 'Q');
            tmp.SetPiece(srcR, srcC, '.');

            if (!ChessUtilities.IsKingInCheck(tmp, kingIsWhite: false)) return null;
            if ( ChessUtilities.IsKingInCheck(tmp, kingIsWhite: true))  return null;
            // Queen must still see the target after the fork move
            if (!ChessUtilities.CanAttackSquare(tmp, dstR, dstC, 'Q', tR, tC)) return null;

            string fen = $"{board.ToFEN()} w - - 0 1";
            string uci = $"{(char)('a' + srcC)}{8 - srcR}{(char)('a' + dstC)}{8 - dstR}";
            string kSq = ChessUtilities.GetSquareName(kR, kC);
            string tSq = ChessUtilities.GetSquareName(tR, tC);
            return new GeneratedPuzzle(fen, uci, "Queen Fork",
                $"Fork — check on {kSq}, queen on {tSq}");
        }

        // ── Back-Rank Mate ───────────────────────────────────────────────

        private static GeneratedPuzzle? BuildBackRankMate()
        {
            // Two corner geometries: a8 (left) or h8 (right)
            // Black king boxed in by own rook + pawns; white rook slides along rank 8 to deliver #.
            // White king is fixed at the one square that defends the mating square from row 1.
            bool left  = _rng.Next(2) == 0;
            int  kC    = left ? 0 : 7;   // black king corner
            int  mateC = left ? 1 : 6;   // rook captures here → checkmate
            int  wkC   = left ? 2 : 5;   // white king covers mating square

            // White rook: random column with clear path to mateC along rank 8
            int rC = left ? _rng.Next(2, 8) : _rng.Next(0, 6);

            var board = new ChessBoard();
            board.SetPiece(0, kC,    'k');  // black king in corner
            board.SetPiece(0, mateC, 'r');  // black rook: blocks check + blocks escape square
            board.SetPiece(1, kC,    'p');  // black pawn: blocks forward escape
            board.SetPiece(1, mateC, 'p');  // black pawn: blocks diagonal escape
            board.SetPiece(0, rC,    'R');  // white rook (will slide to mateC)
            board.SetPiece(1, wkC,   'K');  // white king defends mating square

            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: true))  return null;
            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: false)) return null;

            using var pooled = BoardPool.Rent(board);
            var tmp = pooled.Board;
            tmp.SetPiece(0, mateC, 'R');  // rook captures and mates
            tmp.SetPiece(0, rC,    '.');

            if (!ChessUtilities.IsKingMated(tmp, kingIsWhite: false)) return null;
            if ( ChessUtilities.IsKingInCheck(tmp, kingIsWhite: true))  return null;

            string fen = $"{board.ToFEN()} w - - 0 1";
            string uci = $"{(char)('a' + rC)}8{(char)('a' + mateC)}8";
            string sq  = ChessUtilities.GetSquareName(0, mateC);
            return new GeneratedPuzzle(fen, uci, "Back-Rank Mate",
                $"Back-rank checkmate — Rook to {sq}#");
        }

        // ── Smothered Mate ───────────────────────────────────────────────

        private static GeneratedPuzzle? BuildSmotheredMate()
        {
            // Classic smothered mate geometry: knight jumps to c7 or f7, checking king
            // trapped in the corner by its own rook (b8/g8) and two pawns (a7+b7 or g7+h7).
            // Neither the rook nor the pawns can capture the knight.
            bool left  = _rng.Next(2) == 0;
            int  kC    = left ? 0 : 7;   // black king
            int  mateC = left ? 2 : 5;   // knight destination: c7 or f7
            int  rookC = left ? 1 : 6;   // smothering rook: b8 or g8
            int  p1C   = left ? 0 : 7;   // smothering pawn 1: a7 or h7
            int  p2C   = left ? 1 : 6;   // smothering pawn 2: b7 or g7

            // Knight source: any square that can reach (1, mateC), excluding occupied squares
            var sources = KnightMoves(1, mateC)
                .Where(s => s != (0, kC) && s != (0, rookC) && s != (1, p1C) && s != (1, p2C))
                .ToList();
            if (sources.Count == 0) return null;
            var (srcR, srcC) = sources[_rng.Next(sources.Count)];

            var board = new ChessBoard();
            board.SetPiece(0, kC,    'k');
            board.SetPiece(0, rookC, 'r');
            board.SetPiece(1, p1C,   'p');
            board.SetPiece(1, p2C,   'p');
            board.SetPiece(srcR, srcC, 'N');

            var wk = FindSafeKingSquare(board, true,
                new[] { (0, kC), (1, mateC), (0, rookC) }, 2);
            if (wk == null) return null;
            board.SetPiece(wk.Value.r, wk.Value.c, 'K');

            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: true))  return null;
            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: false)) return null;

            using var pooled = BoardPool.Rent(board);
            var tmp = pooled.Board;
            tmp.SetPiece(1, mateC, 'N');
            tmp.SetPiece(srcR, srcC, '.');

            if (!ChessUtilities.IsKingMated(tmp, kingIsWhite: false)) return null;
            if ( ChessUtilities.IsKingInCheck(tmp, kingIsWhite: true))  return null;

            string fen = $"{board.ToFEN()} w - - 0 1";
            string uci = $"{(char)('a' + srcC)}{8 - srcR}{(char)('a' + mateC)}{8 - 1}";
            string sq  = ChessUtilities.GetSquareName(1, mateC);
            return new GeneratedPuzzle(fen, uci, "Smothered Mate",
                $"Smothered checkmate — Knight to {sq}#");
        }

        // ── Skewer ───────────────────────────────────────────────────────

        private static GeneratedPuzzle? BuildSkewer()
        {
            // Queen slides to a square that checks the black king; a valuable black piece
            // sits behind the king on the same ray and will be won after the king flees.
            var dir = _dirs[_rng.Next(_dirs.Length)];
            int dr = dir[0], dc = dir[1];

            // Black king: inner board, needs room in both directions on the chosen ray
            int kR = _rng.Next(2, 6), kC = _rng.Next(2, 6);

            // Queen destination: 2 steps before king on ray (king can't capture)
            int dstR = kR - dr * 2, dstC = kC - dc * 2;
            if ((uint)dstR >= 8 || (uint)dstC >= 8) return null;

            // Target piece: 1–2 steps beyond king on the same ray
            int tDist = _rng.Next(1, 3);
            int tR = kR + dr * tDist, tC = kC + dc * tDist;
            if ((uint)tR >= 8 || (uint)tC >= 8) return null;

            // Queen source: on the perpendicular direction from dst
            int[] perp = { -dc, dr };
            int srcDist = _rng.Next(2, 5);
            int srcR = dstR + perp[0] * srcDist, srcC = dstC + perp[1] * srcDist;
            if ((uint)srcR >= 8 || (uint)srcC >= 8) return null;
            if (srcR == kR   && srcC == kC)   return null;
            if (srcR == tR   && srcC == tC)   return null;
            if (srcR == dstR && srcC == dstC) return null;

            var board = new ChessBoard();
            board.SetPiece(kR,   kC,   'k');
            board.SetPiece(tR,   tC,   'q');   // black queen behind king
            board.SetPiece(srcR, srcC, 'Q');   // white queen

            var wk = FindSafeKingSquare(board, true,
                new[] { (kR, kC), (dstR, dstC), (tR, tC) }, 2);
            if (wk == null) return null;
            board.SetPiece(wk.Value.r, wk.Value.c, 'K');

            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: true))  return null;
            if (ChessUtilities.IsKingInCheck(board, kingIsWhite: false)) return null;

            // White queen must have a clear path to the skewer square
            if (!ChessUtilities.CanAttackSquare(board, srcR, srcC, 'Q', dstR, dstC)) return null;

            using var pooled = BoardPool.Rent(board);
            var tmp = pooled.Board;
            tmp.SetPiece(dstR, dstC, 'Q');
            tmp.SetPiece(srcR, srcC, '.');

            if (!ChessUtilities.IsKingInCheck(tmp, kingIsWhite: false)) return null;
            if ( ChessUtilities.IsKingInCheck(tmp, kingIsWhite: true))  return null;

            string fen = $"{board.ToFEN()} w - - 0 1";
            string uci = $"{(char)('a' + srcC)}{8 - srcR}{(char)('a' + dstC)}{8 - dstR}";
            string kSq = ChessUtilities.GetSquareName(kR, kC);
            string tSq = ChessUtilities.GetSquareName(tR, tC);
            return new GeneratedPuzzle(fen, uci, "Skewer",
                $"Skewer — force king off {kSq}, win queen on {tSq}");
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

        private static List<(int r, int c)> GetRaySquares(int r, int c, int[] dir)
        {
            var list = new List<(int, int)>();
            int nr = r + dir[0], nc = c + dir[1];
            while ((uint)nr < 8 && (uint)nc < 8)
            {
                list.Add((nr, nc));
                nr += dir[0]; nc += dir[1];
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
