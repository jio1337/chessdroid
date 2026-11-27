using System.Text.Json;
using ChessDroid.Services;

namespace ChessDroid.Tests.App.ExplanationAudit;

/// <summary>
/// Regressions from the corpus audit (see ExplanationAuditHarness), replayed through exactly
/// what the app shows for a line (ConsoleOutputFormatter.ComposeMoveLine) using the recorded
/// Stockfish lines in corpus.json — no engine needed. Each case names the game, the position
/// (the move played from it), and the candidate line being explained.
/// </summary>
public class ExplanationAuditRegressionTests
{
    private static readonly Lazy<List<ExplanationAuditHarness.Position>> Corpus = new(() =>
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "tests", "ChessDroid.Tests.App"))) d = d.Parent;
        string path = Path.Combine(d!.FullName, "tests", "ChessDroid.Tests.App", "ExplanationAudit", "corpus.json");
        return JsonSerializer.Deserialize<List<ExplanationAuditHarness.Position>>(File.ReadAllText(path))!;
    });

    private static string Explain(string game, string label, string move)
    {
        var pos = Corpus.Value.Single(p => p.Game == game && p.Label == label);
        var line = pos.Lines.Single(l => l.Pv.Split(' ')[0] == move);
        var (exp, _, _) = ConsoleOutputFormatter.ComposeMoveLine(
            move, pos.Fen, pos.Lines.Select(l => l.Pv).ToList(), line.Eval, pos.Lines[0].Eval, includeThreats: true);
        return exp;
    }

    [Theory]
    // The originally reported position (after 18.Ne5): every Black move drops material.
    [InlineData("Mackenzie-Paulsen", "18...Nd5", "d7e8", "still loses queen for rook (Rxd8)")]
    [InlineData("Mackenzie-Paulsen", "18...Nd5", "e7f5", "still loses bishop (Rxd7)")]
    // Two-move tactic: …Bxe6 "wins pawn | saves bishop", but Bh7+ uncovers Rd1 on the queen.
    [InlineData("Mackenzie-Paulsen", "16...fxe6", "d7e6", "still loses queen for rook and pawn (Rxd8)")]
    // Moved piece simply taken: …Nb5 "attacks bishop" — the bishop takes it.
    [InlineData("Paulsen-Mackenzie", "23...Kh8", "c7b5", "loses knight (Bxb5)")]
    // Losing capture: …Qxf1+ "pins bishop, captures rook" — Qxf1 and Black is a queen for a rook down.
    [InlineData("Mackenzie-Blackburne", "22...Qh6", "h3f1", "loses queen for rook (Qxf1)")]
    // Check-fork one move later: …Rd7 "attacks queen | defends h7" — Qa8+ and Qxe8.
    [InlineData("Chigorin-Mackenzie", "30...e5", "d8d7", "loses rook (Qxe8)")]
    // Like-for-like swaps cancelled: not "bishop for bishop" — the real loss is axb4.
    [InlineData("MacDonnell-Mackenzie", "25...Bb6", "c5b4", "loses bishop for pawn (axb4)")]
    public void LineThatLosesMaterialSaysSo(string game, string label, string move, string expected)
        => Assert.Equal(expected, Explain(game, label, move));

    [Theory]
    // 36.Qg5 gives up a rook but forces perpetual check (0.00): compensated, not "loses".
    [InlineData("MacDonnell-Mackenzie", "36.Qh3", "g3g5")]
    // 22.Rxg7+ drops a knight for a pawn but White is still +7.59: not worth calling a loss.
    [InlineData("Paulsen-Mackenzie", "22.Qh6", "g1g7")]
    // Mackenzie's 14.Nxf7 piece sacrifice, the engine's top choice.
    [InlineData("Mackenzie-Blackburne", "14.Nxf7", "d6f7")]
    public void SoundSacrificeIsNotCalledALoss(string game, string label, string move)
        => Assert.DoesNotContain("loses", Explain(game, label, move));
}
