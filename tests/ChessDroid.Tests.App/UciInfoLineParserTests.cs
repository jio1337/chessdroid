using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// CHESSDROID_AUDIT.md §5.1: GetBestMoveAsync and RunContinuousAnalysisAsync each had their
/// own inline copy of the "info ... score ... pv ..." tokenizing loop. Consolidated into
/// UciInfoLineParser.ParsePvLine, used by both — these tests cover the parser directly,
/// including the cases where the two original copies diverged (depth presence, WDL token).
/// </summary>
public class UciInfoLineParserTests
{
    [Fact]
    public void ParsePvLine_CentipawnScore_WhiteToMove_KeepsSign()
    {
        var result = UciInfoLineParser.ParsePvLine(
            "info depth 15 multipv 1 score cp 35 pv e2e4 e7e5", whiteToMove: true);

        Assert.Equal(15, result.Depth);
        Assert.Equal(1, result.MultiPv);
        Assert.Equal("+0.35", result.Eval);
        Assert.Equal(new List<string> { "e2e4", "e7e5" }, result.PvMoves);
        Assert.Null(result.Wdl);
    }

    [Fact]
    public void ParsePvLine_CentipawnScore_BlackToMove_FlipsSign()
    {
        var result = UciInfoLineParser.ParsePvLine(
            "info depth 15 multipv 1 score cp 35 pv e7e5", whiteToMove: false);

        Assert.Equal("-0.35", result.Eval);
    }

    [Fact]
    public void ParsePvLine_MateScore_FormatsAsMate()
    {
        var result = UciInfoLineParser.ParsePvLine(
            "info depth 3 multipv 1 score mate 2 pv d1h5 g6h5 d8h4", whiteToMove: true);

        Assert.Equal("Mate in +2", result.Eval);
    }

    [Fact]
    public void ParsePvLine_NoDepthToken_DepthIsNull()
    {
        var result = UciInfoLineParser.ParsePvLine(
            "info multipv 1 score cp 10 pv e2e4", whiteToMove: true);

        Assert.Null(result.Depth);
    }

    [Fact]
    public void ParsePvLine_NoMultipvToken_DefaultsToOne()
    {
        var result = UciInfoLineParser.ParsePvLine(
            "info depth 10 score cp 10 pv e2e4", whiteToMove: true);

        Assert.Equal(1, result.MultiPv);
    }

    [Fact]
    public void ParsePvLine_WdlToken_OnlyCapturedForMultipvOne()
    {
        var mpv1 = UciInfoLineParser.ParsePvLine(
            "info depth 10 multipv 1 score cp 10 wdl 500 300 200 pv e2e4", whiteToMove: true);
        var mpv2 = UciInfoLineParser.ParsePvLine(
            "info depth 10 multipv 2 score cp 5 wdl 480 320 200 pv d2d4", whiteToMove: true);

        Assert.NotNull(mpv1.Wdl);
        Assert.Equal(500, mpv1.Wdl!.Win);
        Assert.Equal(300, mpv1.Wdl.Draw);
        Assert.Equal(200, mpv1.Wdl.Loss);
        Assert.Null(mpv2.Wdl); // WDL only recorded for the best line, matches pre-extraction behavior
    }

    [Fact]
    public void ParsePvLine_NoWdlToken_WdlIsNull()
    {
        var result = UciInfoLineParser.ParsePvLine(
            "info depth 10 multipv 1 score cp 10 pv e2e4", whiteToMove: true);

        Assert.Null(result.Wdl);
    }

    [Fact]
    public void ParsePvLine_EmptyPv_ReturnsEmptyMoveList()
    {
        var result = UciInfoLineParser.ParsePvLine(
            "info depth 10 multipv 1 score cp 10 pv", whiteToMove: true);

        Assert.Empty(result.PvMoves);
    }
}
