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
                var (net, lost, gained, worst, promoPly) = outcome.Value;
                if (worst.piece == '.')
                {
                    // Nothing left after cancelling swaps: the loss is an opponent promotion
                    // (28.Rxc1? bxc1=Q — the rook "trade" hides a pawn becoming a queen).
                    if (promoPly < 0) return null;
                    string promo = SanAt(pv, fen, promoPly);
                    return promo != "" ? $"allows promotion ({promo})" : "allows promotion";
                }

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

        /// <summary>
        /// True/false when <paramref name="move"/>'s PV is available: does it net at least
        /// <see cref="MinLoss"/> of material down within the window? Null without a usable PV.
        /// </summary>
        public static bool? PvLosesMaterial(string move, string fen, List<string>? pvs)
        {
            try
            {
                string? pv = pvs?.FirstOrDefault(p => p.Split(' ')[0] == move);
                if (pv == null) return null;
                string[] parts = fen.Split(' ');
                var outcome = Simulate(pv, fen, parts.Length > 1 && parts[1] == "w");
                return outcome == null ? null : outcome.Value.net <= -MinLoss;
            }
            catch { return null; }
        }

        /// <summary>
        /// The moved piece can be won where it lands (undefended, or attacked by something
        /// cheaper) and — when the line's PV is known — the reply actually takes it. The PV
        /// check keeps a pinned or otherwise unusable attacker from counting.
        /// </summary>
        public static bool MovedPieceIsTaken(string move, string fen, List<string>? pvs)
        {
            try
            {
                if (string.IsNullOrEmpty(move) || move.Length < 4) return false;
                string[] parts = fen.Split(' ');
                bool moverWhite = parts.Length > 1 && parts[1] == "w";
                var board = ChessBoard.FromFEN(fen);
                string castling = parts.Length > 2 ? parts[2] : "-";
                string ep = parts.Length > 3 ? parts[3] : "-";
                ChessRulesService.ApplyUciMove(board, move, ref castling, ref ep);
                int row = '8' - move[3], col = move[2] - 'a';
                if (!ChessUtilities.IsPieceEnPrise(board, row, col, moverWhite)) return false;

                string? pv = pvs?.FirstOrDefault(p => p.Split(' ')[0] == move);
                var plies = pv?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (plies == null || plies.Length < 2) return true; // no PV: trust the static check
                return plies[1].Length >= 4 && plies[1].Substring(2, 2) == move.Substring(2, 2);
            }
            catch { return false; }
        }

        /// <summary>"sacrifices knight" for the piece <paramref name="move"/> moves, when the engine
        /// still rates the line ~equal or better for the mover; otherwise null.</summary>
        public static string? DescribeSacrifice(string move, string fen, string? lineEval)
        {
            try
            {
                string[] parts = fen.Split(' ');
                bool moverWhite = parts.Length > 1 && parts[1] == "w";
                double? line = MovesExplanation.ParseEvaluation(lineEval ?? "");
                bool sound = IsMate(lineEval) ? !lineEval!.Contains('-') == moverWhite
                    : line.HasValue && (moverWhite ? line.Value : -line.Value) >= CompensatedAbove;
                if (!sound) return null;
                var board = ChessBoard.FromFEN(fen);
                char piece = board.GetPiece('8' - move[1], move[0] - 'a');
                if (piece == '.') return null;
                // Taking something worth about as much (Bxc6 dxc6) is a trade, not a sacrifice.
                char captured = board.GetPiece('8' - move[3], move[2] - 'a');
                if (captured != '.' && Value(captured) >= Value(piece) - 1) return null;
                return $"sacrifices {Name(piece)}";
            }
            catch { return null; }
        }

        /// <summary>"gives up pawn (Bxc4)": the moved piece and the reply that takes it.</summary>
        public static string? DescribeGivenUp(string move, string fen, List<string>? pvs)
        {
            try
            {
                char piece = ChessBoard.FromFEN(fen).GetPiece('8' - move[1], move[0] - 'a');
                if (piece == '.') return null;
                string? pv = pvs?.FirstOrDefault(p => p.Split(' ')[0] == move);
                string san = pv != null ? SanAt(pv, fen, 1) : "";
                return san != "" ? $"gives up {Name(piece)} ({san})" : $"gives up {Name(piece)}";
            }
            catch { return null; }
        }

        /// <summary>MovesExplanation's evaluation wording for the position after <paramref name="move"/>.</summary>
        public static string? DescribeEvaluationAfter(string move, string fen, string evaluation)
        {
            try
            {
                string[] parts = fen.Split(' ');
                var board = ChessBoard.FromFEN(fen);
                string castling = parts.Length > 2 ? parts[2] : "-";
                string ep = parts.Length > 3 ? parts[3] : "-";
                ChessRulesService.ApplyUciMove(board, move, ref castling, ref ep);
                return MovesExplanation.DescribeEvaluation(board, evaluation, parts.Length > 1 && parts[1] == "w");
            }
            catch { return null; }
        }

        private static bool SameKind(char a, char b) => char.ToLowerInvariant(a) == char.ToLowerInvariant(b);

        /// <summary>
        /// Plays the PV window. Returns the mover's net material change, the pieces lost and
        /// gained after cancelling like-for-like swaps, and the most valuable remaining loss.
        /// </summary>
        private static (int net, List<(char piece, int ply)> lost, List<char> gained, (char piece, int ply) worst, int promoPly)?
            Simulate(string pv, string fen, bool moverWhite)
        {
            var all = pv.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (all.Length < 2) return null;

            string[] parts = fen.Split(' ');
            var board = ChessBoard.FromFEN(fen);
            string castling = parts.Length > 2 ? parts[2] : "-";
            string ep = parts.Length > 3 ? parts[3] : "-";
            int promoPly = -1;
            var lost = new List<(char, int)>();
            var gained = new List<char>();

            // Pass 1 — play the window, recording each ply. WindowPlies, extended while captures
            // continue (up to MaxPlies) so a trade isn't cut off between capture and recapture.
            var plies = new List<(string uci, char victim, int delta)>();
            bool lastWasCapture = false;
            for (int i = 0; i < all.Length && i < MaxPlies && (i < WindowPlies || lastWasCapture); i++)
            {
                string uci = all[i];
                if (uci.Length < 4) return null;
                char victim = board.GetPiece('8' - uci[3], uci[2] - 'a');
                lastWasCapture = victim != '.';
                if (i % 2 == 1 && uci.Length == 5 && promoPly < 0) promoPly = i; // opponent promotes
                int before = Balance(board, moverWhite);
                ChessRulesService.ApplyUciMove(board, uci, ref castling, ref ep);
                plies.Add((uci, victim, Balance(board, moverWhite) - before));
            }

            // Pass 2 — leave out the mover's own LATER decisions: a piece the mover moves on a
            // later turn that the opponent then takes (…Qc6 Qg4 Qd6 Rxg7 Bxg2 Kxg2: the bishop
            // is lost to Black's own 3rd-move choice deep in the engine's guess, not to Qc6).
            // Losses this move causes — something already there won (Rxd8), or the moved piece
            // itself taken (…Nb5 Bxb5) — are unaffected.
            // Only when that later pair is itself a loss for the mover (Bxg2 Kxg2: bishop for a
            // pawn) and NOT a recapture on the square the opponent just used — exchange chains
            // (fxe4 Bxe4 Bxe4 Qxe4) and combinations that win back material (Rxg4 Rxg4+ after
            // Qxf8+ Nxf8) are part of what this move sets in motion.
            // The line is read only up to that decision: everything after it (recaptures,
            // follow-ups) hangs on a choice this move didn't make.
            for (int i = 2; i + 1 < plies.Count; i += 2)
            {
                string dest = plies[i].uci.Substring(2, 2);
                bool takenNext = plies[i + 1].uci.Substring(2, 2) == dest;
                bool isRecapture = plies[i - 1].uci.Substring(2, 2) == dest;
                bool pairLoses = plies[i].delta + plies[i + 1].delta < 0;
                if (takenNext && !isRecapture && pairLoses)
                {
                    plies.RemoveRange(i, plies.Count - i);
                    break;
                }
            }

            int net = 0, running = 0;
            for (int i = 0; i < plies.Count; i++)
            {
                var p = plies[i];
                running += p.delta;
                if (i % 2 == 0) net = running; // judged after our own replies
                if (p.victim != '.')
                {
                    if (i % 2 == 0) gained.Add(p.victim);
                    else lost.Add((p.victim, i));
                }
            }
            // A PV ending on the opponent's capture: judge after it too (we may not get a reply in).
            if (plies.Count % 2 == 0) net = Math.Min(net, running);

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
            return (net, lost, gained, worst, promoPly);
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
