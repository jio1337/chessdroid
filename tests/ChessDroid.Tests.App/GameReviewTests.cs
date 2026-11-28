using ChessDroid.Models;
using ChessDroid.Services;
using ChessDroid.Tests.App.ExplanationAudit;
using Q = ChessDroid.Services.MoveQualityAnalyzer.MoveQuality;

namespace ChessDroid.Tests.App;

/// <summary>
/// Game Review audit (2026-11-28). Most cases replay real games from the explanation corpus
/// through MoveClassificationService.ClassifyMove (see ReviewAuditHarness). What was fixed:
///  - errors were judged by raw pawns, so +10.12 → +6.23 (still completely winning) was a
///    "Blunder"; now by win-probability drop, the rule the analysis panel already used;
///  - that shared curve is now Lichess's (the accuracy formula's own), so a 0.4-pawn slip near
///    equality isn't an inaccuracy;
///  - mate rules could never fire (mates parse as 10–15 pawns, the checks wanted > 9000):
///    losing or allowing a forced mate now follows Lichess's rules;
///  - a move that delivers mate reads "Mate in 0" (unsigned) after it — a Black mate scored as
///    a Black blunder and dragged Black's accuracy down;
///  - an annotator's "!!" no longer overrides an engine-detected error;
///  - review symbols in the move list survive the list being rebuilt (starting a variation).
/// </summary>
public class GameReviewTests
{
    private static readonly Lazy<List<ReviewAuditHarness.Move>> Corpus = new(ReviewAuditHarness.Replay);

    private static ReviewAuditHarness.Move Played(string game, string label)
        => Corpus.Value.Single(m => m.Game == game && m.Label == label);

    [Theory]
    // Still completely winning after: not a blunder.
    [InlineData("Paulsen-Mackenzie", "23.Ba4", Q.Inaccuracy)]                                  // +10.12 → +6.23
    [InlineData("Smith-Mackenzie (USA-02.Congress 1871 r2)", "17...Qxg6+", Q.Good)]            // -11.84 → -7.49
    // Already lost before: a further slide is not a mistake.
    [InlineData("Paulsen-Mackenzie", "22...Qf8", Q.Good)]                                      // +8.76 → +10.12
    // Near equality a half-pawn slip stays "Good" — about the noise between two searches.
    [InlineData("Mackenzie-Paulsen", "14.Ba3", Q.Good)]                                        // +0.68 → +0.15
    // Throwing away a clear advantage is still a blunder.
    [InlineData("Mackenzie-Haughton (USA-02.Congress 1871 r1)", "8.O-O", Q.Blunder)]           // +2.22 → -0.21
    public void ErrorsAreJudgedByWinProbability(string game, string label, Q expected)
        => Assert.Equal(expected, Played(game, label).Quality);

    [Theory]
    // Losing a forced mate: by how much the mover still wins (Lichess: >10 inaccuracy, >7 mistake).
    [InlineData("Mackenzie-MacDonnell (London m3 1862 r10)", "35.Qxd8+", Q.Blunder)]           // mate in 9 → +6.67
    // Allowing a forced mate: by how lost the mover already was.
    [InlineData("MacDonnell-Mackenzie (London m3 1862 r7)", "27.Kf2", Q.Mistake)]              // -7.91 → mated in 3
    [InlineData("Mackenzie-Smith (USA-02.Congress 1871 r1)", "21...Ke8", Q.Inaccuracy)]         // -10.70 → mated in 6
    [InlineData("Bock-Mackenzie (USA-03.Congress 1874 r1)", "13.Nxh4", Q.Blunder)]              // +1.13 → mated in 1 (…Rxd4#)
    public void ForcedMateSwingsFollowLichess(string game, string label, Q expected)
        => Assert.Equal(expected, Played(game, label).Quality);

    [Fact]
    public void KeepingAForcedMateIsNeverAnError()
        => Assert.DoesNotContain(Corpus.Value, m =>
            m.Before.StartsWith("Mate in ") && m.After.StartsWith("Mate in ") &&
            (m.Before.Contains('-') == m.After.Contains('-')) &&
            m.Quality is Q.Inaccuracy or Q.Mistake or Q.Blunder);

    // ---- delivering checkmate: the engine reports the mated position as unsigned "Mate in 0"

    private const string BeforeQh4 = "rnbqkbnr/pppp1ppp/8/4p3/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq g3 0 2";
    private const string AfterQh4 = "rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3";

    [Fact]
    public void BlackDeliveringMate_IsNotABlunder()
    {
        var (q, sym, _) = MoveClassificationService.ClassifyMove(BeforeQh4, "d8h4", "Qh4#", isWhiteMove: false,
            bestMove: "a7a6", "Mate in -1", new[] { "Mate in -1", "-5.00" }, "Mate in 0", prevEvalAfter: null);
        Assert.Equal((Q.Best, ""), (q, sym));
    }

    [Fact]
    public async Task BlackDeliveringMate_DoesNotCostBlackAccuracy()
    {
        // Through ClassifyAsync with cached evals (no engine call), as the app runs it.
        var cache = new Dictionary<string, CachedAnalysis>
        {
            [Key(BeforeQh4)] = new() { BestMove = "d8h4", Evaluation = "Mate in -1", Evaluations = new() { "Mate in -1" }, Depth = 99 },
            [Key(AfterQh4)] = new() { BestMove = "", Evaluation = "Mate in 0", Depth = 99 },
        };
        var node = new MoveNode { ParentFEN = BeforeQh4, FEN = AfterQh4, UciMove = "d8h4", SanMove = "Qh4#", HalfMove = 3 };
        using var engine = new ChessEngineService(new AppConfig()); // never started: every eval is cached
        var result = await new MoveClassificationService(engine).ClassifyAsync(
            new List<MoveNode> { node }, engineDepth: 5, cache, null, null, default);

        var move = Assert.Single(result!.MoveResults);
        Assert.True(move.EvalAfter < 0, "the mate counts for Black");
        Assert.Equal(100.0, result.BlackAccuracy, precision: 1);
    }

    private static string Key(string fen) => string.Join(' ', fen.Split(' ').Take(4));

    // ---- annotations

    [Fact]
    public void AnnotatorsBrilliantDoesNotOverrideAnEngineError()
    {
        const string start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var (q, _, _) = MoveClassificationService.ClassifyMove(start, "g2g4", "g4!!", isWhiteMove: true,
            bestMove: "e2e4", "+0.30", new[] { "+0.30", "+0.25" }, "-2.50", prevEvalAfter: null);
        Assert.Equal(Q.Blunder, q);
    }

    // ---- move list: symbols come from the review at draw time, not from rows built once

    [Fact]
    public void ReviewSymbolIsLookedUpForAnyNodeInTheReview()
    {
        var reviewed = new MoveNode { SanMove = "Kc3" };
        var lookup = new Dictionary<MoveNode, MoveReviewResult> { [reviewed] = new() { Symbol = "?!" } };

        Assert.Equal("?!", AnalysisBoardForm.ReviewSymbolFor(lookup, reviewed));
        Assert.Null(AnalysisBoardForm.ReviewSymbolFor(lookup, new MoveNode { SanMove = "Kd2" })); // a new variation move
        Assert.Null(AnalysisBoardForm.ReviewSymbolFor(null, reviewed));                            // no review
    }
}
