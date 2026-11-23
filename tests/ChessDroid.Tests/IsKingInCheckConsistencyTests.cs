using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests;

/// <summary>
/// Characterization tests written BEFORE consolidating IsKingInCheck (CHESSDROID_AUDIT.md
/// §6.3 — 4 independent copies: ChessUtilities, ChessRulesService, EngineMatchService,
/// MoveGenerator). None of the 4 had any existing test coverage except transitively through
/// MoveGenerator's perft suite, so this corpus locks in that the 3 statically-callable
/// implementations already agree before any of them get redirected onto MoveGenerator's
/// (the most-tested one). EngineMatchService's copy is a private instance method and can't
/// be driven directly from a test — it was verified by reading instead (same geometric
/// checks, no Chess960/en-passant involvement since check detection never needs either), and
/// by the existing EngineMatchServiceTests.cs suite, which exercises live-match checkmate/
/// stalemate detection end to end.
/// </summary>
public class IsKingInCheckConsistencyTests
{
    public static IEnumerable<object[]> Positions()
    {
        yield return new object[] { "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", true };   // start position
        yield return new object[] { "rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3", true }; // Fool's mate — queen checks along the long diagonal
        yield return new object[] { "rnbqkbnr/ppp2ppp/8/3pp3/8/5N2/PPPPPPPP/RNBQKB1R b KQkq - 0 1", false }; // ordinary developing position
        yield return new object[] { "4k3/8/8/8/8/8/4R3/4K3 b - - 0 1", false };  // rook checks along the e-file
        yield return new object[] { "4k3/8/8/8/8/8/8/R3K3 b - - 0 1", false };  // rook on a1 is not aligned with the black king
        yield return new object[] { "4k3/8/8/8/8/2B5/8/4K3 b - - 0 1", false }; // bishop on c3
        yield return new object[] { "4k3/8/8/3N4/8/8/8/4K3 b - - 0 1", false }; // knight on d5
        yield return new object[] { "8/8/8/8/8/8/3p4/4K3 w - - 0 1", true };    // pawn checks diagonally
        yield return new object[] { "8/8/8/8/8/8/8/4K2k w - - 0 1", true };     // kings adjacent (illegal in real play, exercised anyway — pure geometry)
        yield return new object[] { "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", true };  // Kiwipete
        yield return new object[] { "r3k2r/p1ppqpb1/bn2pNp1/3P4/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq - 0 1", false };  // Kiwipete after Ne5xf7
    }

    [Theory]
    [MemberData(nameof(Positions))]
    public void AllThreeImplementations_AgreeOnCheckStatus(string fen, bool kingIsWhite)
    {
        var board = ChessBoard.FromFEN(fen);

        bool coreUtilities = ChessUtilities.IsKingInCheck(board, kingIsWhite);
        bool rulesService = ChessRulesService.IsKingInCheck(board, kingIsWhite);
        bool moveGenerator = MoveGenerator.IsKingInCheck(board, kingIsWhite);

        Assert.True(coreUtilities == rulesService && rulesService == moveGenerator,
            $"Disagreement on {fen} (kingIsWhite={kingIsWhite}): " +
            $"ChessUtilities={coreUtilities}, ChessRulesService={rulesService}, MoveGenerator={moveGenerator}");
    }

    [Theory]
    [InlineData("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3", true, true)]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", true, false)]
    [InlineData("4k3/8/8/8/8/8/4R3/4K3 b - - 0 1", false, true)]
    [InlineData("8/8/8/8/8/8/3p4/4K3 w - - 0 1", true, true)]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", true, false)]
    public void KnownPositions_MatchExpectedCheckStatus(string fen, bool kingIsWhite, bool expectedInCheck)
    {
        var board = ChessBoard.FromFEN(fen);
        Assert.Equal(expectedInCheck, MoveGenerator.IsKingInCheck(board, kingIsWhite));
    }
}
