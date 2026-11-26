using ChessDroid.Models;

namespace ChessDroid.Tests.App;

/// <summary>
/// PositionEditorForm.ValidatePosition/ParseMoveCounters had zero test coverage despite
/// being the only thing standing between an arbitrary user-built board and the rules
/// engine. A prior session's trace found two real gaps here: a position with two kings
/// for one side passed validation (only "at least one king" was checked, not "exactly
/// one"), and the halfmove clock/fullmove number were silently reset to "0 1" on every
/// Apply, even a pure no-op round trip. These tests lock in the fix for both.
/// </summary>
public class PositionEditorFormTests
{
    private static ChessBoard EmptyBoardWithKings(bool extraWhiteKing = false, bool extraBlackKing = false)
    {
        var board = new ChessBoard();
        board.SetPiece(0, 4, 'k');
        board.SetPiece(7, 4, 'K');
        if (extraWhiteKing) board.SetPiece(7, 0, 'K');
        if (extraBlackKing) board.SetPiece(0, 0, 'k');
        return board;
    }

    [Fact]
    public void ValidatePosition_OneKingEachSide_IsValid()
    {
        var board = EmptyBoardWithKings();
        Assert.Null(PositionEditorForm.ValidatePosition(board));
    }

    [Fact]
    public void ValidatePosition_TwoWhiteKings_IsRejected()
    {
        var board = EmptyBoardWithKings(extraWhiteKing: true);
        string? error = PositionEditorForm.ValidatePosition(board);
        Assert.NotNull(error);
        Assert.Contains("exactly one king", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePosition_TwoBlackKings_IsRejected()
    {
        var board = EmptyBoardWithKings(extraBlackKing: true);
        string? error = PositionEditorForm.ValidatePosition(board);
        Assert.NotNull(error);
        Assert.Contains("exactly one king", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePosition_MissingBlackKing_IsRejected()
    {
        var board = new ChessBoard();
        board.SetPiece(7, 4, 'K');
        string? error = PositionEditorForm.ValidatePosition(board);
        Assert.NotNull(error);
        Assert.Contains("exactly one king", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)] // rank 8 (row 0)
    [InlineData(7)] // rank 1 (row 7)
    public void ValidatePosition_PawnOnBackRank_IsRejected(int row)
    {
        var board = EmptyBoardWithKings();
        board.SetPiece(row, 3, 'P');
        string? error = PositionEditorForm.ValidatePosition(board);
        Assert.NotNull(error);
        Assert.Contains("1st or 8th rank", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePosition_PawnOnMiddleRanks_IsValid()
    {
        var board = EmptyBoardWithKings();
        board.SetPiece(4, 3, 'P');
        Assert.Null(PositionEditorForm.ValidatePosition(board));
    }

    [Fact]
    public void ValidatePosition_TooManyWhitePawns_IsRejected()
    {
        var board = EmptyBoardWithKings();
        for (int c = 0; c < 8; c++) board.SetPiece(1, c, 'P');
        board.SetPiece(2, 0, 'P'); // 9th white pawn
        string? error = PositionEditorForm.ValidatePosition(board);
        Assert.NotNull(error);
        Assert.Contains("Too many pieces", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(new[] { "8/8/8/8/8/8/8/8", "w", "-" }, 0, 1)]
    [InlineData(new[] { "8/8/8/8/8/8/8/8", "w", "-", "-", "12", "34" }, 12, 34)]
    [InlineData(new[] { "8/8/8/8/8/8/8/8", "w", "-", "e3", "0", "1" }, 0, 1)]
    public void ParseMoveCounters_WellFormedFen_RoundTripsExactValues(string[] fenParts, int expectedHalfmove, int expectedFullmove)
    {
        var (halfmove, fullmove) = PositionEditorForm.ParseMoveCounters(fenParts);
        Assert.Equal(expectedHalfmove, halfmove);
        Assert.Equal(expectedFullmove, fullmove);
    }

    [Theory]
    [InlineData(new object[] { new[] { "8/8/8/8/8/8/8/8", "w", "-", "-", "notanumber", "1" } })]
    [InlineData(new object[] { new[] { "8/8/8/8/8/8/8/8", "w", "-", "-", "-5", "1" } })] // negative halfmove is nonsensical
    [InlineData(new object[] { new[] { "8/8/8/8/8/8/8/8", "w", "-", "-", "0", "0" } })] // fullmove must be >= 1
    public void ParseMoveCounters_MalformedTail_FallsBackToZeroOne(string[] fenParts)
    {
        var (halfmove, fullmove) = PositionEditorForm.ParseMoveCounters(fenParts);
        Assert.Equal(0, halfmove);
        Assert.Equal(1, fullmove);
    }
}
