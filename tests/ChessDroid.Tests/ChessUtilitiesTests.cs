using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests;

/// <summary>
/// Regression coverage for the Phase 5 GetPieceValue/GetMaterialValue split
/// (CHESSDROID_AUDIT.md §6.3). These two methods look like duplicates but deliberately
/// disagree on King's value: GetPieceValue (exchange evaluation) uses 100 so SEE-style
/// math never treats the king as a free capture; GetMaterialValue (material counting,
/// what EndgameAnalysis and MoveSharpnessAnalyzer both need) uses 0, since the king isn't
/// part of either side's material total. Locking both in so a future "just merge these"
/// pass doesn't silently flip one call site's king value.
/// </summary>
public class ChessUtilitiesTests
{
    [Theory]
    [InlineData(PieceType.Pawn, 1)]
    [InlineData(PieceType.Knight, 3)]
    [InlineData(PieceType.Bishop, 3)]
    [InlineData(PieceType.Rook, 5)]
    [InlineData(PieceType.Queen, 9)]
    [InlineData(PieceType.King, 100)]
    [InlineData(PieceType.None, 0)]
    public void GetPieceValue_ExchangeEvaluation_KingIsLargeFiniteValue(PieceType piece, int expected)
        => Assert.Equal(expected, ChessUtilities.GetPieceValue(piece));

    [Theory]
    [InlineData(PieceType.Pawn, 1)]
    [InlineData(PieceType.Knight, 3)]
    [InlineData(PieceType.Bishop, 3)]
    [InlineData(PieceType.Rook, 5)]
    [InlineData(PieceType.Queen, 9)]
    [InlineData(PieceType.King, 0)]
    [InlineData(PieceType.None, 0)]
    public void GetMaterialValue_MaterialCounting_KingIsExcluded(PieceType piece, int expected)
        => Assert.Equal(expected, ChessUtilities.GetMaterialValue(piece));
}
