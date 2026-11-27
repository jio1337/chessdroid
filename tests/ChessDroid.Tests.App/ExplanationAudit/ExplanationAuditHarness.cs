using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests.App.ExplanationAudit;

/// <summary>
/// Corpus-driven audit of the move-explanation pipeline (MovesExplanation + ThreatDetection +
/// DefenseDetection, composed the way ConsoleOutputFormatter does it). Two opt-in steps, both
/// no-ops in a normal test run:
///
///   CHESSDROID_AUDIT_RECORD=1  — play every game in games.pgn, run Stockfish (MultiPV 3) on each
///                                position, save the lines to corpus.json (needs the engine).
///   CHESSDROID_AUDIT=1         — replay corpus.json through the explainer and write a report
///                                flagging claims the engine's own PV contradicts (no engine).
///
/// The oracle is the PV: if a line is described with positive tactical claims but its own
/// continuation loses material, or "attacks X" while the reply simply takes the attacker, the
/// explanation is suspect. Flags are leads for manual review, not verdicts.
/// </summary>
public class ExplanationAuditHarness
{
    private const int Depth = 15;
    private const int MultiPv = 3;

    private static string Dir => Path.Combine(RepoRoot(), "tests", "ChessDroid.Tests.App", "ExplanationAudit");
    private static string CorpusPath => Path.Combine(Dir, "corpus.json");

    public record Line(string Pv, string Eval);
    public record Position(string Game, string Label, string Fen, string Played, List<Line> Lines);

    // ---------------------------------------------------------------- record

    [Fact]
    public void RecordCorpus()
    {
        if (Environment.GetEnvironmentVariable("CHESSDROID_AUDIT_RECORD") != "1") return;

        string engine = Path.Combine(RepoRoot(), "src", "ChessDroid.App", "Engines", "stockfish18.exe");
        using var sf = new Uci(engine);
        var corpus = new List<Position>();

        foreach (var (name, sans) in ParsePgn(File.ReadAllText(Path.Combine(Dir, "games.pgn"))))
        {
            string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            for (int i = 0; i < sans.Count; i++)
            {
                string? uci = ChessNotationService.ConvertSanToUci(sans[i], fen);
                Assert.True(uci != null, $"{name}: cannot convert {sans[i]} at ply {i + 1} ({fen})");
                string label = $"{i / 2 + 1}{(i % 2 == 0 ? "." : "...")}{sans[i]}";
                corpus.Add(new Position(name, label, fen, uci!, sf.Analyse(fen, Depth, MultiPv)));
                fen = ApplyUci(fen, uci!);
            }
        }

        File.WriteAllText(CorpusPath, JsonSerializer.Serialize(corpus, new JsonSerializerOptions { WriteIndented = true }));
    }

    // ---------------------------------------------------------------- audit

    [Fact]
    public void RunAudit()
    {
        if (Environment.GetEnvironmentVariable("CHESSDROID_AUDIT") != "1") return;

        var corpus = JsonSerializer.Deserialize<List<Position>>(File.ReadAllText(CorpusPath))!;
        var report = new StringBuilder();
        var flagged = new StringBuilder();
        int lines = 0, flags = 0;
        var flagCounts = new Dictionary<string, int>();

        foreach (var pos in corpus)
        {
            var pvs = pos.Lines.Select(l => l.Pv).ToList();
            var block = new StringBuilder();
            block.AppendLine($"=== {pos.Game} | before {pos.Label} | {pos.Fen}");
            bool anyFlag = false;

            foreach (var line in pos.Lines)
            {
                lines++;
                string move = line.Pv.Split(' ')[0];
                var (exp, def, thr) = Explain(move, pos.Fen, pvs, line.Eval, pos.Lines[0].Eval);
                string san = Pretty(line.Pv, pos.Fen, 8);
                block.AppendLine($"{line.Eval} {san}");
                block.AppendLine($"  → {exp}{(def != "" ? " | 🛡 " + def : "")}{(thr != "" ? " | ⚔ " + thr : "")}");

                foreach (var f in Oracle(pos.Fen, line.Pv, exp, def, thr))
                {
                    block.AppendLine($"  !! {f}");
                    string key = f.Split(':')[0];
                    flagCounts[key] = flagCounts.GetValueOrDefault(key) + 1;
                    flags++;
                    anyFlag = true;
                }
            }
            report.Append(block).AppendLine();
            if (anyFlag) flagged.Append(block).AppendLine();
        }

        string outDir = Environment.GetEnvironmentVariable("CHESSDROID_AUDIT_OUT") ?? Path.GetTempPath();
        var summary = $"{corpus.Count} positions, {lines} lines, {flags} flags\n" +
                      string.Join("\n", flagCounts.OrderByDescending(k => k.Value).Select(k => $"  {k.Key}: {k.Value}")) + "\n\n";
        File.WriteAllText(Path.Combine(outDir, "explanation-audit-full.txt"), summary + report);
        File.WriteAllText(Path.Combine(outDir, "explanation-audit-flags.txt"), summary + flagged);
    }

    /// <summary>Exactly what the app shows for one line (ConsoleOutputFormatter.ComposeMoveLine).</summary>
    private static (string exp, string def, string thr) Explain(string move, string fen, List<string> pvs, string eval, string bestEval)
    {
        var (exp, thr, def) = ConsoleOutputFormatter.ComposeMoveLine(move, fen, pvs, eval, bestEval, includeThreats: true);
        return (exp, def, thr);
    }

    // ---------------------------------------------------------------- oracle

    private static readonly string[] PositiveClaims =
    {
        "defends", "saves", "attacks", "fork", "pin", "skewer", "x-ray", "wins", "threat",
        "clears", "removes defender", "overload", "discovered", "traps", "double attack",
        "captures", "decoy", "deflect", "intermezzo", "zwischenzug", "desperado",
    };
    private static readonly string[] Admissions = { "hangs", "loses ","sacrific", "gambit", "desperado", "exchange sac", "gives up" };

    private static IEnumerable<string> Oracle(string fen, string pv, string exp, string def, string thr)
    {
        string all = $"{exp} | {def} | {thr}".ToLowerInvariant();
        var moves = pv.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool moverWhite = fen.Split(' ')[1] == "w";
        if (all.Contains("checkmate") || all.Contains("mate in")) yield break;

        var nets = new List<int>(); // nets[k] = mover's material balance change after k+1 plies
        string f = fen;
        int start = Balance(f, moverWhite);
        foreach (var m in moves.Take(6))
        {
            f = ApplyUci(f, m);
            nets.Add(Balance(f, moverWhite) - start);
        }
        int stable = nets.Count >= 5 ? nets[4] : nets.Count >= 3 ? nets[2] : nets[0];

        var claims = PositiveClaims.Where(c => all.Contains(c)).ToList();
        bool admitted = Admissions.Any(a => all.Contains(a));

        if (claims.Count > 0 && !admitted && stable <= -2)
            yield return $"MATERIAL_LOSS: PV nets {stable:+0;-0} for the mover within {Math.Min(5, nets.Count)} plies, but claims [{string.Join(", ", claims)}] and admits nothing";

        // "attacks/fork/pin X" while the reply just captures the moved piece and the mover ends behind
        if (moves.Length >= 2 && moves[1].Substring(2, 2) == moves[0].Substring(2, 2) && nets.Count >= 3 && nets[2] < 0 &&
            claims.Any(c => c is "attacks" or "fork" or "pin" or "skewer" or "threat" or "double attack" or "x-ray"))
            yield return $"ATTACKER_TAKEN: reply {moves[1]} captures the moved piece (net {nets[2]:+0;-0} after 3 plies) but it claims [{string.Join(", ", claims)}]";

        // Explanation only: "⚔ wins X" in the threat list means "threatens to win X".
        if (Regex.IsMatch(exp.ToLowerInvariant(), @"\bwins (pawn|knight|bishop|rook|queen|material|exchange)") && stable < 1)
            yield return $"NO_WIN: claims to win material but PV nets {stable:+0;-0}";
    }

    private static int Balance(string fen, bool forWhite)
    {
        int sum = 0;
        foreach (char c in fen.Split(' ')[0])
        {
            int v = char.ToLowerInvariant(c) switch { 'p' => 1, 'n' => 3, 'b' => 3, 'r' => 5, 'q' => 9, _ => 0 };
            sum += char.IsUpper(c) == forWhite ? v : -v;
        }
        return sum;
    }

    // ---------------------------------------------------------------- plumbing

    private static string ApplyUci(string fen, string uci)
    {
        var p = fen.Split(' ');
        var board = ChessBoard.FromFEN(fen);
        string castling = p.Length > 2 ? p[2] : "-";
        string ep = p.Length > 3 ? p[3] : "-";
        ChessRulesService.ApplyUciMove(board, uci, ref castling, ref ep);
        bool wtm = p[1] == "w";
        int full = p.Length > 5 && int.TryParse(p[5], out var n) ? n : 1;
        if (!wtm) full++;
        return $"{board.ToFEN()} {(wtm ? "b" : "w")} {(string.IsNullOrEmpty(castling) ? "-" : castling)} {(string.IsNullOrEmpty(ep) ? "-" : ep)} 0 {full}";
    }

    private static string Pretty(string pv, string fen, int maxPlies)
    {
        string trimmed = string.Join(' ', pv.Split(' ').Take(maxPlies));
        try
        {
            return ChessNotationService.ConvertFullPvToSan(trimmed, fen, ChessRulesService.ApplyUciMove,
                ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
        }
        catch { return trimmed; }
    }

    private static List<(string name, List<string> sans)> ParsePgn(string text)
    {
        var games = new List<(string, List<string>)>();
        foreach (var chunk in Regex.Split(text, @"\r?\n(?=\[White )"))
        {
            if (string.IsNullOrWhiteSpace(chunk)) continue;
            string white = Regex.Match(chunk, @"\[White ""([^""]*)""\]").Groups[1].Value;
            string black = Regex.Match(chunk, @"\[Black ""([^""]*)""\]").Groups[1].Value;
            string body = Regex.Replace(chunk, @"\[[^\]]*\]", " ");
            var sans = Regex.Split(body, @"\s+")
                .Select(t => Regex.Replace(t, @"^\d+\.+", ""))
                .Where(t => t != "" && !Regex.IsMatch(t, @"^(1-0|0-1|1/2-1/2|\*)$"))
                .ToList();
            games.Add(($"{white.Split(',')[0]}-{black.Split(',')[0]}", sans));
        }
        return games;
    }

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "tests", "ChessDroid.Tests.App"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repo root not found");
    }

    /// <summary>Minimal synchronous UCI client — just enough for MultiPV analysis.</summary>
    private sealed class Uci : IDisposable
    {
        private readonly Process _p;

        public Uci(string exe)
        {
            _p = Process.Start(new ProcessStartInfo(exe)
            {
                RedirectStandardInput = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true,
            })!;
            Send("uci"); WaitFor("uciok");
            Send("setoption name Threads value 4");
            Send("setoption name Hash value 128");
            Send($"setoption name MultiPV value {MultiPv}");
            Send("isready"); WaitFor("readyok");
        }

        public List<Line> Analyse(string fen, int depth, int multiPv)
        {
            bool wtm = fen.Split(' ')[1] == "w";
            Send($"position fen {fen}");
            Send($"go depth {depth}");
            var best = new SortedDictionary<int, Line>();
            string? l;
            while ((l = _p.StandardOutput.ReadLine()) != null)
            {
                if (l.StartsWith("bestmove")) break;
                var m = Regex.Match(l, @"\bdepth (\d+) .*?\bmultipv (\d+) .*?\bscore (cp|mate) (-?\d+).*? pv (.+)$");
                if (!m.Success || int.Parse(m.Groups[1].Value) != depth) continue;
                int raw = int.Parse(m.Groups[4].Value);
                int white = wtm ? raw : -raw;
                string eval = m.Groups[3].Value == "mate"
                    ? Score.FromMate(white).ToString()
                    : Score.FromCentipawns(white).ToString();
                best[int.Parse(m.Groups[2].Value)] = new Line(m.Groups[5].Value.Trim(), eval);
            }
            return best.Values.Take(multiPv).ToList();
        }

        private void Send(string s) { _p.StandardInput.WriteLine(s); _p.StandardInput.Flush(); }
        private void WaitFor(string token) { string? l; while ((l = _p.StandardOutput.ReadLine()) != null && !l.StartsWith(token)) { } }
        public void Dispose() { try { Send("quit"); _p.WaitForExit(2000); } catch { } _p.Dispose(); }
    }
}
