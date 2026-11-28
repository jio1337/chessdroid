using System.Globalization;
using System.Text;
using System.Text.Json;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests.App.ExplanationAudit;

/// <summary>
/// Offline audit of Game Review (MoveClassificationService.ClassifyMove + ComputeAccuracy) over
/// the explanation corpus. Every corpus position is the position before a game move, with the
/// engine's MultiPV evals; the next position's top eval is the eval after the move — the same
/// pair the app's review searches for. No engine needed. Opt-in, a no-op in a normal run:
///
///   CHESSDROID_REVIEW_AUDIT=1 with CHESSDROID_AUDIT_OUT=&lt;dir&gt; — writes review-audit.txt:
///   one line per move (game | move | symbol quality | before → after | loss), then each game's
///   accuracy. Diff two runs line by line to see exactly what a change re-classified.
/// </summary>
public class ReviewAuditHarness
{
    public record Move(string Game, string Label, string Played, bool White,
        string Before, string After, MoveQualityAnalyzer.MoveQuality Quality, string Symbol, double Loss);

    /// <summary>Replays the review over the corpus; the last move of each game has no "after" and is skipped.</summary>
    public static List<Move> Replay()
    {
        var corpus = Directory.GetFiles(Dir, "corpus*.json").OrderBy(f => f)
            .SelectMany(f => JsonSerializer.Deserialize<List<ExplanationAuditHarness.Position>>(File.ReadAllText(f))!)
            .ToList();
        var moves = new List<Move>();
        double? prevAfter = null;
        for (int i = 0; i + 1 < corpus.Count; i++)
        {
            var pos = corpus[i];
            var next = corpus[i + 1];
            if (next.Game != pos.Game) { prevAfter = null; continue; }
            if (pos.Lines.Count == 0 || next.Lines.Count == 0) continue;

            bool white = pos.Fen.Split(' ')[1] == "w";
            string san = pos.Label[(pos.Label.LastIndexOf('.') + 1)..];
            string before = pos.Lines[0].Eval, after = next.Lines[0].Eval;
            var (q, sym, loss) = MoveClassificationService.ClassifyMove(pos.Fen, pos.Played, san, white,
                pos.Lines[0].Pv.Split(' ')[0], before, pos.Lines.Select(l => l.Eval).ToList(), after, prevAfter);
            moves.Add(new Move(pos.Game, pos.Label, pos.Played, white, before, after, q, sym, loss));
            prevAfter = EvaluationParser.ParseNullable(after);
        }
        return moves;
    }

    public static (double white, double black) Accuracy(IEnumerable<Move> game)
    {
        var results = game.Select(m => new MoveReviewResult
        {
            EvalBefore = EvaluationParser.Parse(m.Before), EvalAfter = EvaluationParser.Parse(m.After),
            IsWhiteMove = m.White, Quality = m.Quality, Symbol = m.Symbol,
        }).ToList();
        return (MoveClassificationService.ComputeAccuracy(results, true), MoveClassificationService.ComputeAccuracy(results, false));
    }

    [Fact]
    public void RunReviewAudit()
    {
        if (Environment.GetEnvironmentVariable("CHESSDROID_REVIEW_AUDIT") != "1") return;
        string outDir = Environment.GetEnvironmentVariable("CHESSDROID_AUDIT_OUT") ?? Dir;

        var moves = Replay();
        var sb = new StringBuilder();
        foreach (var m in moves)
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"{m.Game} | {m.Label} | {m.Symbol,-2} {m.Quality,-10} | {m.Before} -> {m.After} | loss {m.Loss:F2}");
        sb.AppendLine();
        foreach (var g in moves.GroupBy(m => m.Game))
        {
            var (w, b) = Accuracy(g);
            sb.AppendLine(CultureInfo.InvariantCulture, $"ACCURACY {g.Key} | White {w:F1} | Black {b:F1}");
        }
        sb.AppendLine();
        foreach (var q in moves.GroupBy(m => m.Quality).OrderBy(g => g.Key))
            sb.AppendLine(CultureInfo.InvariantCulture, $"COUNT {q.Key} {q.Count()}");
        File.WriteAllText(Path.Combine(outDir, "review-audit.txt"), sb.ToString());
    }

    private static string Dir
    {
        get
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !Directory.Exists(Path.Combine(d.FullName, "tests", "ChessDroid.Tests.App"))) d = d.Parent;
            return Path.Combine(d!.FullName, "tests", "ChessDroid.Tests.App", "ExplanationAudit");
        }
    }
}
