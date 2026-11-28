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
            m.Before.StartsWith("Mate in ", StringComparison.Ordinal) && m.After.StartsWith("Mate in ", StringComparison.Ordinal) &&
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

    // ---- key moments: the review summary's list of brilliant moves, mistakes and blunders

    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const string AfterG4 = "rnbqkbnr/pppppppp/8/8/6P1/8/PPPPPP1P/RNBQKBNR b KQkq g3 0 1";

    [Fact]
    public async Task KeyMoment_ShowsTheMoveTheSwingAndTheBetterMove()
    {
        var cache = new Dictionary<string, CachedAnalysis>
        {
            [Key(StartFen)] = new() { BestMove = "e2e4", Evaluation = "+0.30", Evaluations = new() { "+0.30", "+0.25" }, Depth = 99 },
            [Key(AfterG4)] = new() { BestMove = "d7d5", Evaluation = "-1.50", Depth = 99 },
        };
        var node = new MoveNode { ParentFEN = StartFen, FEN = AfterG4, UciMove = "g2g4", SanMove = "g4", HalfMove = 0 };
        using var engine = new ChessEngineService(new AppConfig());
        var result = await new MoveClassificationService(engine).ClassifyAsync(
            new List<MoveNode> { node }, engineDepth: 5, cache, null, null, default);

        var moment = Assert.Single(ConsoleOutputFormatter.KeyMoments(result!.MoveResults));
        Assert.Equal(("1. g4?", "+0.30 → -1.50", "e4"), ConsoleOutputFormatter.KeyMomentParts(moment));
    }

    [Fact]
    public void KeyMoments_AreTheTurningPointsInGameOrder()
    {
        MoveReviewResult R(int ply, Q q) => new() { Node = new MoveNode { HalfMove = ply, SanMove = "x" }, Quality = q, IsWhiteMove = ply % 2 == 0 };
        var results = new[] { R(9, Q.Blunder), R(2, Q.Best), R(4, Q.Inaccuracy), R(1, Q.Brilliant), R(6, Q.Mistake), R(7, Q.Precise) };

        Assert.Equal(new[] { 1, 6, 9 }, ConsoleOutputFormatter.KeyMoments(results).Select(r => r.Node.HalfMove));
    }

    [Fact]
    public void KeyMoment_BlackMoveWithMates_AndNoBetterMoveForABrilliancy()
    {
        var blunder = new MoveReviewResult
        {
            Node = new MoveNode { HalfMove = 24, SanMove = "Nxh4" }, IsWhiteMove = true, Quality = Q.Blunder, Symbol = "??",
            EvalBeforeText = "+1.13", EvalAfterText = "Mate in -1", BestMove = "Qa6+",
        };
        Assert.Equal(("13. Nxh4??", "+1.13 → #-1", "Qa6+"), ConsoleOutputFormatter.KeyMomentParts(blunder));

        var brilliant = new MoveReviewResult
        {
            Node = new MoveNode { HalfMove = 21, SanMove = "Qxd4+" }, IsWhiteMove = false, Quality = Q.Brilliant, Symbol = "!!",
            EvalBeforeText = "Mate in -5", EvalAfterText = "Mate in -4", BestMove = "Qxd4+",
        };
        Assert.Equal(("11... Qxd4+!!", "#-5 → #-4", ""), ConsoleOutputFormatter.KeyMomentParts(brilliant));
    }

    [Fact]
    public void KeyMoment_FromPgnAnnotations_ShowsJustTheMove()
    {
        // A review rebuilt from an annotated PGN has no evals or better move; the SAN may carry the symbol.
        var r = new MoveReviewResult { Node = new MoveNode { HalfMove = 5, SanMove = "Bc5?" }, IsWhiteMove = false, Quality = Q.Mistake, Symbol = "?" };
        Assert.Equal(("3... Bc5?", "", ""), ConsoleOutputFormatter.KeyMomentParts(r));
    }

    [Theory]
    [InlineData("Mate in -1", "#-1")]
    [InlineData("Mate in +6", "#6")]
    [InlineData("Mate in 0", "#")]
    [InlineData("+1.13", "+1.13")]
    public void ShortEval(string eval, string expected) => Assert.Equal(expected, ConsoleOutputFormatter.ShortEval(eval));

    // ---- step links: prev/next through the reviewed moves of the same kind by the same side

    private static MoveReviewResult Rev(int ply, Q q) => new()
    {
        Node = new MoveNode { HalfMove = ply, SanMove = $"m{ply}" }, Quality = q, IsWhiteMove = ply % 2 == 0,
    };

    [Fact]
    public void ReviewSteps_WalkTheSameKindBySameSide_InGameOrder()
    {
        // White blunders on plies 4, 10, 16; a Black blunder (7) and a White mistake (12) in between.
        var w4 = Rev(4, Q.Blunder); var b7 = Rev(7, Q.Blunder); var w10 = Rev(10, Q.Blunder);
        var w12 = Rev(12, Q.Mistake); var w16 = Rev(16, Q.Blunder);
        var results = new[] { w16, b7, w4, w12, w10 }; // any order in the review

        var first = ConsoleOutputFormatter.ReviewSteps(results, w4.Node);
        Assert.Equal((null, w10, 1, 3, "White blunder"), (first.prev, first.next, first.index, first.count, first.label));

        var middle = ConsoleOutputFormatter.ReviewSteps(results, w10.Node);
        Assert.Equal((w4, w16, 2, 3), (middle.prev, middle.next, middle.index, middle.count));

        var last = ConsoleOutputFormatter.ReviewSteps(results, w16.Node);
        Assert.Equal((w10, null, 3, 3), (last.prev, last.next, last.index, last.count));

        var only = ConsoleOutputFormatter.ReviewSteps(results, b7.Node);
        Assert.Equal((null, null, 1, 1, "Black blunder"), (only.prev, only.next, only.index, only.count, only.label));
    }

    [Fact]
    public void ReviewSteps_NoneForAMoveOutsideTheReview()
    {
        var results = new[] { Rev(4, Q.Blunder) };
        Assert.Equal(0, ConsoleOutputFormatter.ReviewSteps(results, new MoveNode { HalfMove = 4 }).count); // e.g. a variation move
        Assert.Equal(0, ConsoleOutputFormatter.ReviewSteps(results, null).count);                         // the start position
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
