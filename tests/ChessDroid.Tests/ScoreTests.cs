using ChessDroid.Models;

namespace ChessDroid.Tests;

/// <summary>
/// Locks in Score's ToString() format before it replaces the 3 copy-pasted format blocks
/// in ChessEngineService (CHESSDROID_AUDIT.md §5.1) — each produced byte-identical output to
/// these cases (verified by reading all 3 before writing this struct), so any future change
/// to Score's formatting that drifts from these strings is a visible regression, not just a
/// refactor. Deliberately scoped to the producers only: Phase 5 found the 5 separate string
/// PARSERS downstream disagree on what a mate score means NUMERICALLY (flat ±100 vs.
/// distance-scaled vs. sort-dominant ±100000 vs. exact-preserved) — consolidating those was
/// out of scope for this slice, so Score has no numeric conversion beyond Cp/Mate themselves.
/// </summary>
public class ScoreTests
{
    [Theory]
    [InlineData(35, "+0.35")]
    [InlineData(-120, "-1.20")]
    [InlineData(0, "+0.00")]
    [InlineData(933, "+9.33")]
    public void FromCentipawns_FormatsLikeTheOriginalThreeCopies(int cp, string expected)
        => Assert.Equal(expected, Score.FromCentipawns(cp).ToString());

    [Theory]
    [InlineData(3, "Mate in +3")]
    [InlineData(-3, "Mate in -3")]
    [InlineData(0, "Mate in 0")]
    public void FromMate_FormatsLikeTheOriginalThreeCopies(int mateIn, string expected)
        => Assert.Equal(expected, Score.FromMate(mateIn).ToString());
}
