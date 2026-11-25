namespace ChessDroid.Tests.App;

/// <summary>
/// OpeningExplorerDialog.ParseEvalValue turns the Eval column's display string back into a
/// comparable number for the click-to-sort header — it's the one piece of this feature with
/// real logic (everything else is grid/button wiring), so it's the one piece worth locking
/// down directly rather than only through manual testing.
/// </summary>
public class OpeningExplorerDialogTests
{
    [Theory]
    [InlineData("+0.74", 0.74)]
    [InlineData("-2.46", -2.46)]
    [InlineData("0.00", 0.0)]
    [InlineData("+10.50", 10.50)]
    public void ParseEvalValue_PlainCentipawnEval_ParsesToExactDouble(string eval, double expected)
    {
        double? result = OpeningExplorerDialog.ParseEvalValue(eval);
        Assert.NotNull(result);
        Assert.Equal(expected, result!.Value, 3);
    }

    [Fact]
    public void ParseEvalValue_MateInPositive_RanksAboveAnyCentipawnEval()
    {
        double? mate = OpeningExplorerDialog.ParseEvalValue("Mate in +2");
        double? bigEval = OpeningExplorerDialog.ParseEvalValue("+20.00");
        Assert.NotNull(mate);
        Assert.NotNull(bigEval);
        Assert.True(mate > bigEval);
    }

    [Fact]
    public void ParseEvalValue_MateInNegative_RanksBelowAnyCentipawnEval()
    {
        double? mate = OpeningExplorerDialog.ParseEvalValue("Mate in -2");
        double? bigEval = OpeningExplorerDialog.ParseEvalValue("-20.00");
        Assert.NotNull(mate);
        Assert.NotNull(bigEval);
        Assert.True(mate < bigEval);
    }

    [Fact]
    public void ParseEvalValue_FasterMateRanksMoreExtremeThanSlowerMate()
    {
        // Mate in +1 is more decisively winning than Mate in +5, so it should rank higher.
        double? mateIn1 = OpeningExplorerDialog.ParseEvalValue("Mate in +1");
        double? mateIn5 = OpeningExplorerDialog.ParseEvalValue("Mate in +5");
        Assert.True(mateIn1 > mateIn5);

        double? mateInNeg1 = OpeningExplorerDialog.ParseEvalValue("Mate in -1");
        double? mateInNeg5 = OpeningExplorerDialog.ParseEvalValue("Mate in -5");
        Assert.True(mateInNeg1 < mateInNeg5);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("garbage")]
    public void ParseEvalValue_BlankOrUnparseable_ReturnsNull(string? eval)
    {
        Assert.Null(OpeningExplorerDialog.ParseEvalValue(eval!));
    }
}
