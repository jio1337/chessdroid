using ChessDroid.Services;

namespace ChessDroid.Tests;

/// <summary>
/// EloCalculator feeds Tournament mode's rating updates and had zero test coverage before this.
/// Pins down the standard FIDE logistic expected-score formula and both K-factor variants against
/// known reference values, plus the equal-strength (0.5 expected) and format-sign edge cases.
/// </summary>
public class EloCalculatorTests
{
    [Theory]
    [InlineData(1500, 1500, 0.5)]
    [InlineData(1600, 1400, 0.7597)]
    [InlineData(1400, 1600, 0.2403)]
    [InlineData(2000, 2000, 0.5)]
    public void ExpectedScore_MatchesStandardLogisticFormula(int myElo, int opponentElo, double expected)
        => Assert.Equal(expected, EloCalculator.ExpectedScore(myElo, opponentElo), 4);

    [Theory]
    [InlineData(1500, 1500, 1.0, 5)]   // win as equal: K=10 * (1 - 0.5) = 5
    [InlineData(1500, 1500, 0.0, -5)]  // loss as equal: K=10 * (0 - 0.5) = -5
    [InlineData(1500, 1500, 0.5, 0)]   // draw as equal: no change
    [InlineData(1600, 1400, 1.0, 2)]   // expected winner wins: small gain
    [InlineData(1400, 1600, 1.0, 8)]   // underdog wins: bigger gain
    public void EloChange_UsesFideKFactorOfTen(int myElo, int opponentElo, double score, int expectedDelta)
        => Assert.Equal(expectedDelta, EloCalculator.EloChange(myElo, opponentElo, score));

    [Theory]
    [InlineData(1500, 1500, 1.0, 16)]   // win as equal: K=32 * (1 - 0.5) = 16
    [InlineData(1500, 1500, 0.0, -16)]  // loss as equal: K=32 * (0 - 0.5) = -16
    [InlineData(1500, 1500, 0.5, 0)]    // draw as equal: no change
    public void EloChangeChessdroid_UsesLargerKFactorOfThirtyTwo(int myElo, int opponentElo, double score, int expectedDelta)
        => Assert.Equal(expectedDelta, EloCalculator.EloChangeChessdroid(myElo, opponentElo, score));

    [Fact]
    public void EloChangeChessdroid_MovesFasterThanFideVariant_ForSameInputs()
    {
        int fide = EloCalculator.EloChange(1400, 1600, 1.0);
        int chessdroid = EloCalculator.EloChangeChessdroid(1400, 1600, 1.0);
        Assert.True(chessdroid > fide);
    }

    [Theory]
    [InlineData(5, "+5")]
    [InlineData(0, "+0")]
    [InlineData(-5, "-5")]
    public void FormatDelta_PrefixesNonNegativeWithPlus(int delta, string expected)
        => Assert.Equal(expected, EloCalculator.FormatDelta(delta));
}
