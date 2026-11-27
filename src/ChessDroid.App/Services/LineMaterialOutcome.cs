using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Reads a candidate line's own engine continuation to see whether the move simply loses
    /// material. The tactical detectors each look at one position and keep missing losses the
    /// engine already shows a ply or two later — a piece that's just taken (…Nb5 Bxb5), a
    /// capture that costs more than it wins (…Qxf1+ Qxf1), or a discovered tactic one move on
    /// (…Bxe6 Bh7+ Kh8 Rxd8). Found by the corpus audit in
    /// tests/ChessDroid.Tests.App/ExplanationAudit.
    /// </summary>
    public static class LineMaterialOutcome
    {
        private const int WindowPlies = 5;          // mover, reply, mover, reply, mover
        private const int MaxPlies = 8;             // hard cap when extending through a capture sequence
        private const int MinLoss = 2;              // net material, judged after the mover's own replies
        private const double CompensatedAbove = -0.5; // engine still has the mover ~equal or better: a sound sacrifice

        /// <summary>
        /// Returns e.g. "loses queen for rook and pawn (Rxd8)" when <paramref name="move"/>'s PV
        /// loses at least <see cref="MinLoss"/> of material within <see cref="WindowPlies"/>
        /// plies and the engine doesn't consider it compensated; "still loses …" when the best
        /// line loses as much (the loss is unavoidable, not this move's fault); otherwise null.
        /// </summary>
        public static string? Describe(string move, string fen, List<string>? pvs, string? lineEval, string? bestEval)
        {
            try
            {
                if (string.IsNullOrEmpty(move) || pvs == null || pvs.Count == 0) return null;
                string? pv = pvs.FirstOrDefault(p => p.Split(' ')[0] == move);
                if (pv == null) return null;
                if (IsMate(lineEval) || IsMate(bestEval)) return null; // mating lines are explained elsewhere

                string[] parts = fen.Split(' ');
                bool moverWhite = parts.Length > 1 && parts[1] == "w";
                double? line = MovesExplanation.ParseEvaluation(lineEval ?? "");
                if (!line.HasValue) return null;
                double lineForMover = moverWhite ? line.Value : -line.Value;
                if (lineForMover >= CompensatedAbove) return null;

                var outcome = Simulate(pv, fen, moverWhite);
                if (outcome == null || outcome.Value.net > -MinLoss) return null;
                var (net, lost, gained, worst) = outcome.Value;
                if (worst.piece == '.') return null;

                int sameKind = lost.Count(l => SameKind(l.piece, worst.piece));
                string desc = sameKind >= 2 ? $"loses two {Name(worst.piece)}s" : $"loses {Name(worst.piece)}";
                var forWhat = gained.OrderByDescending(Value).Take(2).Select(Name).ToList();
                if (forWhat.Count > 0) desc += " for " + string.Join(" and ", forWhat);

                // Unavoidable? If the top line loses as much (or the same piece), this move isn't
                // what drops it — say "still loses" rather than pin the loss on it.
                string bestPv = pvs[0];
                bool isBest = bestPv.Split(' ')[0] == move;
                if (isBest || (Simulate(bestPv, fen, moverWhite) is { } b &&
                               (b.net <= net || (b.worst.piece != '.' && SameKind(b.worst.piece, worst.piece)))))
                    desc = "still " + desc;

                string san = SanAt(pv, fen, worst.ply);
                return san != "" ? $"{desc} ({san})" : desc;
            }
            catch
            {
                return null;
            }
        }

        private static bool SameKind(char a, char b) => char.ToLowerInvariant(a) == char.ToLowerInvariant(b);

        /// <summary>
        /// Plays the PV window. Returns the mover's net material change, the pieces lost and
        /// gained after cancelling like-for-like swaps, and the most valuable remaining loss.
        /// </summary>
        private static (int net, List<(char piece, int ply)> lost, List<char> gained, (char piece, int ply) worst)?
            Simulate(string pv, string fen, bool moverWhite)
        {
            var all = pv.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (all.Length < 2) return null;

            string[] parts = fen.Split(' ');
            var board = ChessBoard.FromFEN(fen);
            string castling = parts.Length > 2 ? parts[2] : "-";
            string ep = parts.Length > 3 ? parts[3] : "-";
            int start = Balance(board, moverWhite), net = 0, lossPly = -1;
            var lost = new List<(char, int)>();
            var gained = new List<char>();

            // Window: WindowPlies, extended while captures continue (up to MaxPlies) so a trade
            // isn't cut off between the capture and its recapture (…Rxd1+ | Nxd1).
            int played = 0;
            bool lastWasCapture = false;
            for (int i = 0; i < all.Length && i < MaxPlies && (i < WindowPlies || lastWasCapture); i++)
            {
                string uci = all[i];
                if (uci.Length < 4) return null;
                played++;
                char victim = board.GetPiece('8' - uci[3], uci[2] - 'a');
                lastWasCapture = victim != '.';
                bool moverPly = i % 2 == 0;
                if (victim != '.')
                {
                    if (moverPly) gained.Add(victim);
                    else { lost.Add((victim, i)); if (lossPly < 0) lossPly = i; }
                }
                ChessRulesService.ApplyUciMove(board, uci, ref castling, ref ep);
                if (moverPly) net = Balance(board, moverWhite) - start;
            }
            // A PV ending on the opponent's capture: judge after it too (we may not get a reply in).
            if (played % 2 == 0) net = Math.Min(net, Balance(board, moverWhite) - start);

            // Cancel like-for-like swaps so "rook for rook" doesn't hide a knight-for-pawn loss.
            // Cancel the LATEST matching loss: a later trade (Bxd5 cxd5) answers its own capture,
            // so the earlier one (axb4) is the real loss to cite.
            foreach (char g in gained.ToList())
            {
                int i = lost.FindLastIndex(l => SameKind(l.Item1, g));
                if (i >= 0) { lost.RemoveAt(i); gained.Remove(g); }
            }
            var worst = lost.Count == 0 ? ('.', -1)
                : lost.OrderByDescending(l => Value(l.Item1)).ThenBy(l => l.Item2).First();
            return (net, lost, gained, worst);
        }

        private static bool IsMate(string? eval) => eval != null && eval.StartsWith("Mate", StringComparison.Ordinal);

        private static string Name(char piece) => ChessUtilities.GetPieceName(PieceHelper.GetPieceType(piece));

        private static int Value(char piece) => char.ToLowerInvariant(piece) switch
        {
            'p' => 1, 'n' => 3, 'b' => 3, 'r' => 5, 'q' => 9, _ => 0,
        };

        private static int Balance(ChessBoard board, bool forWhite)
        {
            int sum = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    char p = board.GetPiece(r, c);
                    if (p == '.') continue;
                    sum += char.IsUpper(p) == forWhite ? Value(p) : -Value(p);
                }
            return sum;
        }

        private static string SanAt(string pv, string fen, int ply)
        {
            if (ply < 0) return "";
            string prefix = string.Join(' ', pv.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(ply + 1));
            string san = ChessNotationService.ConvertFullPvToSan(prefix, fen, ChessRulesService.ApplyUciMove,
                ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
            var tokens = san.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return tokens.Length > ply ? tokens[ply] : "";
        }
    }
}
