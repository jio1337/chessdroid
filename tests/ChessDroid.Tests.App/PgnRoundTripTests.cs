using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// SAN/PGN move round-trip coverage (CHESSDROID_AUDIT.md §7.2: "PGN — round-trip — import,
/// export, re-import produces the same tree"). Before this, PgnImportService.ConvertSanToUci —
/// the actual function AnalysisBoardForm.ImportPgn calls for every move in a real PGN file —
/// had zero test coverage anywhere in the suite, despite ChessNotationService.ConvertUCIToSAN
/// (the reverse direction) already being tested in NotationTests.cs.
///
/// AnalysisBoardForm.ImportPgn/GeneratePgn themselves are private instance methods deeply
/// coupled to the form's own move-tree/UI state (variation stack, classification cache, move
/// list) — not something to construct a full AnalysisBoardForm for. Instead these tests drive
/// the same underlying production primitives ImportPgn itself calls, in the same sequence, to
/// get real coverage of the actual parsing/round-trip logic without that risk.
///
/// ConvertUCIToSAN never adds +/# suffixes, so round-tripped SAN is compared with suffixes
/// stripped from both sides; checkmate itself is verified separately where relevant via
/// ChessUtilities.IsKingMated.
/// </summary>
public class PgnRoundTripTests
{
    private static string StripSuffix(string san) => san.TrimEnd('+', '#');

    private static string ConvertUciToSan(string uciMove, string fen) =>
        ChessNotationService.ConvertUCIToSAN(uciMove, fen, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);

    [Fact]
    public void ScholarsMate_RoundTripsSequentially_AndEndsInVerifiedCheckmate()
    {
        // 1.e4 e5 2.Bc4 Nc6 3.Qh5 Nf6?? 4.Qxf7# — a real short game chaining multiple moves
        // through the same position, which isolated single-move SAN tests can't catch: each
        // move's correct SAN depends on the FEN reached by every move before it.
        string[] sanMoves = { "e4", "e5", "Bc4", "Nc6", "Qh5", "Nf6", "Qxf7#" };

        var board = new ChessBoardControl();
        string fen = board.GetFEN();
        var reconstructedSan = new List<string>();

        foreach (string san in sanMoves)
        {
            string? uci = PgnImportService.ConvertSanToUci(san, fen);
            Assert.NotNull(uci);

            reconstructedSan.Add(StripSuffix(ConvertUciToSan(uci!, fen)));

            Assert.True(board.MakeMove(uci!), $"MakeMove rejected '{uci}' (from SAN '{san}') at {fen}");
            fen = board.GetFEN();
        }

        Assert.Equal(sanMoves.Select(StripSuffix), reconstructedSan);

        var finalBoard = ChessBoard.FromFEN(fen);
        Assert.True(ChessUtilities.IsKingMated(finalBoard, kingIsWhite: false));
    }

    // Castling, en passant, and promotion moves below are drawn directly from
    // MoveGenerator.GetAllLegalMovesUci (perft-verified) rather than hand-typed, so there's no
    // transcription-error risk in asserting they're legal — only the round-trip itself is new.

    [Theory]
    [InlineData("e1g1", "O-O")]
    [InlineData("e1c1", "O-O-O")]
    public void Castling_RoundTripsThroughSanUci(string uciMove, string expectedSan)
    {
        string fen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1"; // Kiwipete
        var castling = new CastlingState(true, true, true, true);
        Assert.Contains(uciMove, MoveGenerator.GetAllLegalMovesUci(ChessBoard.FromFEN(fen), true, castling, null));

        string san = ConvertUciToSan(uciMove, fen);
        Assert.Equal(expectedSan, san);

        Assert.Equal(uciMove, PgnImportService.ConvertSanToUci(san, fen));
    }

    [Fact]
    public void EnPassantCapture_RoundTripsThroughSanUci()
    {
        const string uciMove = "e5d6";
        string fen = "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1";
        var castling = new CastlingState(false, false, false, false);
        var enPassant = (Row: 2, Col: 3); // d6
        Assert.Contains(uciMove, MoveGenerator.GetAllLegalMovesUci(ChessBoard.FromFEN(fen), true, castling, enPassant));

        string san = ConvertUciToSan(uciMove, fen);
        Assert.Equal("exd6", san);

        Assert.Equal(uciMove, PgnImportService.ConvertSanToUci(san, fen));
    }

    [Theory]
    [InlineData("e7e8q", "e8=Q")]
    [InlineData("e7e8n", "e8=N")]
    public void Promotion_RoundTripsThroughSanUci(string uciMove, string expectedSan)
    {
        string fen = "k7/4P3/8/8/8/8/8/4K3 w - - 0 1";
        var castling = new CastlingState(false, false, false, false);
        Assert.Contains(uciMove, MoveGenerator.GetAllLegalMovesUci(ChessBoard.FromFEN(fen), true, castling, null));

        string san = ConvertUciToSan(uciMove, fen);
        Assert.Equal(expectedSan, san);

        Assert.Equal(uciMove, PgnImportService.ConvertSanToUci(san, fen));
    }
}
