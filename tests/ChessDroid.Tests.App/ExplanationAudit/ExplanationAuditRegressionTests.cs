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
        string dir = Path.Combine(d!.FullName, "tests", "ChessDroid.Tests.App", "ExplanationAudit");
        return Directory.GetFiles(dir, "corpus*.json").OrderBy(f => f)
            .SelectMany(f => JsonSerializer.Deserialize<List<ExplanationAuditHarness.Position>>(File.ReadAllText(f))!)
            .ToList();
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

    // ---- second corpus (games2.pgn): attack claims from a piece that's simply taken (A),
    // a refuted "hangs" (B), promotions (C), fake skewers (D), and the sacrifice wording.

    [Theory]
    // A: 13...b5 "creates threat on bishop" — Bxb5 just takes the pawn.
    [InlineData("Haughton-Mackenzie (USA-02.Congress 1871 r2)", "13...d6", "b7b5", "advances b-pawn")]
    // A: 19.Nc7+ "traps rook | wins rook on a8" — Qxc7; White is still winning, so it's a sacrifice.
    [InlineData("Mackenzie-Haughton (USA-02.Congress 1871 r1)", "19.Nc3", "d5c7", "sacrifices knight")]
    // C: 28.Rxc1? bxc1=Q — the rook swaps cancel out; the promotion is the loss.
    [InlineData("Mackenzie-Zukertort (London 1883)", "28.Qxd1", "b1c1", "allows promotion (bxc1=Q)")]
    // Mackenzie's 17.Qh6+!! against Mason (Paris 1878): the queen draws the king to h6 (Kxh6 Nf5+).
    [InlineData("Mackenzie-Mason", "17.Qh6+", "h5h6", "decoy sacrifice (queen), forced checkmate (mate in +6)")]
    public void SecondCorpusExplanation(string game, string label, string move, string expected)
        => Assert.Equal(expected, Explain(game, label, move));

    [Theory]
    // B: 23.c5! opens the a6-bishop onto the queen, but the engine line loses nothing.
    [InlineData("Mackenzie-Bird (Paris playoff-4pl 1878 r2)", "23.c5", "c4c5", "hangs")]
    // D: 20...Bxe3+ "skewers king (to the g1 rook)" — the king just takes it.
    [InlineData("Kennicott-Mackenzie (USA-03.Congress+ 1874 r1)", "20...Qe7", "b6e3", "skewer")]
    // 6...Bg4+ "skewers king, wins material" (Ke2 to Qd1) — Nf3 just blocks.
    [InlineData("Bock-Mackenzie (USA-03.Congress 1874 r1)", "6...Bg4+", "c8g4", "skewer")]
    // A trade (Bxc6 dxc6) is not a sacrifice.
    [InlineData("Mackenzie-Smith (USA-02.Congress 1871 r1)", "15.Bxc6", "a8c6", "sacrific")]
    public void SecondCorpusRefutedClaimIsGone(string game, string label, string move, string absent)
        => Assert.DoesNotContain(absent, Explain(game, label, move));

    // Reported live (Mackenzie–Bird, Paris 1878, after 21.Rf7), with the app's own engine lines:
    // 21...Qc6 read "loses bishop (Kxg2)", but that bishop is lost to Black's own later choice
    // deep in the PV (Qc6 Qg4 Qd6 Rxg7 Bxg2 Kxg2), not to Qc6 — the line is only 0.20 worse.
    [Fact]
    public void LossFromMoversOwnLaterChoiceIsNotBlamedOnTheMove()
    {
        string fen = ChessNotationService.GetFenAfterSanMoves(
            "e4 e6 d4 b6 Bd3 Bb7 Nh3 c5 c3 cxd4 cxd4 Nc6 Be3 Bb4+ Nc3 Nf6 O-O Bxc3 bxc3 Na5 " +
            "e5 Nd5 Qg4 Qc7 Rac1 O-O-O c4 f5 Qe2 Nxe3 fxe3 d6 Nf4 Rhe8 Nxe6 Rxe6 Bxf5 dxe5 " +
            "Bxe6+ Kb8 Rf7");
        var pvs = new List<string>
        {
            "c7d6 e6d5 b7a6 d4e5 d6a3 e2e1 a5c4 d5c4 a6c4 c1c4 a3a2",
            "c7c6 e2g4 c6d6 f7g7 b7g2 g1g2",
            "c7f7 e6f7 a7a6 c1d1 b7e4 e2h5 a5c6",
        };
        string Line(int i, string eval) =>
            ConsoleOutputFormatter.ComposeMoveLine(pvs[i].Split(' ')[0], fen, pvs, eval, "+4.49", includeThreats: true).explanation;

        Assert.DoesNotContain("loses", Line(1, "+4.69"));                  // …Qc6: no longer "loses bishop"
        Assert.Equal("loses queen for rook (Bxf7)", Line(2, "+6.32"));     // …Qxf7: a real loss, unchanged
    }

    // Reported live, same game after 22.d5: 22...Qc5 claimed "x-ray attack on rook" (c1) through
    // the c4 pawn — a pawn can't leave a file, and Nxc4 only swaps one blocker for another.
    // 22...Ba6 against Qe2 through the same pawn is a diagonal: c4-c5 does open it, so it stays.
    [Fact]
    public void NoXrayThroughPawnOnAFile()
    {
        string fen = ChessNotationService.GetFenAfterSanMoves(
            "e4 e6 d4 b6 Bd3 Bb7 Nh3 c5 c3 cxd4 cxd4 Nc6 Be3 Bb4+ Nc3 Nf6 O-O Bxc3 bxc3 Na5 " +
            "e5 Nd5 Qg4 Qc7 Rac1 O-O-O c4 f5 Qe2 Nxe3 fxe3 d6 Nf4 Rhe8 Nxe6 Rxe6 Bxf5 dxe5 " +
            "Bxe6+ Kb8 Rf7 Qd6 d5");
        var pvs = new List<string> { "b7a6 e2e1 a5c4 e1c3 d6c5 f7g7 c4e3 c3c5 b6c5 h2h4", "d6c5 h2h3 b7a6 g1h2 e5e4 e2f2 c5d6 f2f4 d6f4 e3f4" };

        Assert.DoesNotContain("x-ray", MovesExplanation.GenerateMoveExplanation("d6c5", fen, pvs, "+4.41"));
        Assert.Contains("x-ray attack on queen", MovesExplanation.GenerateMoveExplanation("b7a6", fen, pvs, "+4.28"));
    }

    // Reported live (Bock–Mackenzie, USA-03.Congress 1874, after 8...Bc5), with the app's own
    // engine lines: 9.Qe1 read "decoy sacrifice (traps piece)". It only offers a queen trade —
    // …Qxe1 Kxe1 — and the "trapped" queen on e1 is just recaptured.
    [Fact]
    public void QueenTradeOfferIsNotADecoy()
    {
        string fen = ChessNotationService.GetFenAfterSanMoves(
            "e4 e5 Nc3 Nc6 f4 exf4 d4 Qh4+ Ke2 d5 exd5 Bg4+ Nf3 O-O-O dxc6 Bc5");
        var pvs = new List<string>
        {
            "d1e1 h4h5 e2d1 g4f3 g2f3 h5f3 f1e2 d8d4 c1d2 d4d2",
            "c3b5 g4f3 g2f3 b7c6 c2c3 c6b5 d1d3 g8f6 e2d1 h4f2",
            "c6b7 c8b8 c3b5 g4f3 g2f3 a7a6 c2c3 a6b5 a2a4 b5b4",
        };
        string exp = ConsoleOutputFormatter.ComposeMoveLine("d1e1", fen, pvs, "+0.12", "+0.12", includeThreats: true).explanation;
        Assert.DoesNotContain("decoy", exp);
    }

    [Theory]
    // A piece given up to draw the king in, which is checked at once.
    [InlineData("Thompson-Mackenzie (GBR-ch 1868)", "10...Rb8", "f2d4")]               // …Qxd4+ Kxd4 Bg7+
    [InlineData("Metger-Mackenzie (DSB-05.Kongress 1887 r15)", "19...Qh4", "g3h2")]   // …Bh2+ Kxh2 Qh4+
    [InlineData("Mackenzie-Judd (Toledo m 1887)", "25.f4+", "h2h4")]                  // h4+ Kxh4 Nf5+
    public void KingDrawnInAndCheckedIsADecoy(string game, string label, string move)
        => Assert.Contains("decoy sacrifice", Explain(game, label, move));

    // Reported live (Thompson–Mackenzie, GBR-ch 1868, after 11.Rf1), with the app's own line:
    // 11...Qxd4+!! read "sacrifices queen for decisive advantage" — the Brilliant wording replaced
    // the mate, and the decoy went unmentioned because the engine's defence declines it (Kd2).
    // Taking (12.Kxd4, as played) walks into Bg7+.
    private const string ThompsonAfterRf1 =
        "e4 e5 f4 exf4 Nf3 g5 d4 g4 Ne5 Qh4+ Kd2 Qf2+ Kc3 Nc6 a3 d6 Nxc6 bxc6 Bd3 Rb8 Rf1";
    private static readonly List<string> ThompsonPvs = new() { "f2d4 c3d2 d4e3 d2c3 f8g7 e4e5 g7e5 c3c4 e3d4" };

    [Fact]
    public void ForcedMateDecoyIsNamed_EvenWhenTheLineDeclinesIt()
    {
        string fen = ChessNotationService.GetFenAfterSanMoves(ThompsonAfterRf1);
        Assert.Equal("decoy sacrifice (queen), forced checkmate (mate in -5)",
            MovesExplanation.GenerateMoveExplanation("f2d4", fen, ThompsonPvs, "Mate in -5"));
    }

    [Fact]
    public void BrilliantMateSacrificeKeepsTheMateAndTheDecoy()
    {
        string fen = ChessNotationService.GetFenAfterSanMoves(ThompsonAfterRf1);
        var (exp, threats, _) = ConsoleOutputFormatter.ComposeMoveLine("f2d4", fen, ThompsonPvs, "Mate in -5", "Mate in -5",
            includeThreats: true, overrideExplanation: "sacrifices queen for decisive advantage");
        Assert.Equal("decoy sacrifice (queen), forced checkmate (mate in -5)", exp);
        Assert.Equal("", threats); // a forced mate needs no "⚔ gives check" — for Black too
    }

    // A forced mate where the king can't take the piece is not a decoy (18.Nhf5+ Bxf5).
    [Fact]
    public void ForcedMateWithoutAKingCaptureIsNotADecoy()
        => Assert.Equal("forced checkmate (mate in +5)", Explain("Mackenzie-Mason", "18.Nhf5+", "h4f5"));

    // The side to move is getting mated: not "forced checkmate", and no "🛡 stops checkmate threat".
    [Fact]
    public void SideBeingMatedIsNotSaidToMate()
    {
        var pos = Corpus.Value.Single(p => p.Game == "Mackenzie-Mason" && p.Label == "17...Kxh6");
        var line = pos.Lines.Single(l => l.Pv.Split(' ')[0] == "g7h6");
        var (exp, threats, defenses) = ConsoleOutputFormatter.ComposeMoveLine(
            "g7h6", pos.Fen, pos.Lines.Select(l => l.Pv).ToList(), line.Eval, pos.Lines[0].Eval, includeThreats: true);
        Assert.Equal("can't avoid checkmate (mate in +5)", exp);
        Assert.Equal(("", ""), (threats, defenses));
    }

    [Theory]
    // Queen trade offers / recaptures the old static check called "traps piece".
    [InlineData("Bock-Mackenzie (USA-03.Congress 1874 r1)", "9.Qd3", "d1e1")]
    [InlineData("Smith-Mackenzie (USA-02.Congress 1871 r2)", "17...Qxg6+", "e8g6")]
    [InlineData("Mackenzie-Judd (Toledo m 1887) #2", "5.Nd5", "d1e1")]
    // A check the king answers by stepping away, not by taking the bait.
    [InlineData("Mackenzie-Judd (Toledo m 1887)", "25.f4+", "f2f4")]
    // Trades the king recaptures, then a check: nothing was given away.
    [InlineData("Mackenzie-Smith (USA-02.Congress 1871 r1)", "20.Rxf8+", "f1f8")]     // Rxf8+ Kxf8 Rf1+
    [InlineData("Englisch-Mackenzie (Vienna 1882 r9)", "19.Rfd1", "d5d7")]           // Qxd7+ Kxd7 Rfd1+
    [InlineData("Kennicott-Mackenzie (USA-03.Congress+ 1874 r1)", "20...Qe7", "b6e3")] // …Bxe3+ Kxe3
    public void NoDecoyWithoutTheBaitBeingTakenAndPunished(string game, string label, string move)
        => Assert.DoesNotContain("decoy", Explain(game, label, move));

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
