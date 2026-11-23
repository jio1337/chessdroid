using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests;

/// <summary>
/// Perft tests for Core's MoveGenerator — the same technique and same four standard
/// chess-programming positions as tests/ChessDroid.Tests.App/PerftTests.cs (which drove
/// ChessBoardControl, the WinForms-coupled original). This is the regression proof that
/// porting ChessBoardControl's private legal-move logic into MoveGenerator (Phase 4, "extract
/// GameSession/single MoveGenerator out of ChessBoardControl") was behavior-identical: same
/// positions, same depths, same expected node counts, now running against pure Core with no
/// WinForms dependency at all (faster, and runnable from ChessDroid.Tests).
/// </summary>
public class MoveGeneratorPerftTests
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const string KiwipeteFen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
    private const string Position3Fen = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
    private const string Position4Fen = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";

    private static CastlingState ParseCastling(string rights) => new(
        WhiteKingside: rights.Contains('K'),
        WhiteQueenside: rights.Contains('Q'),
        BlackKingside: rights.Contains('k'),
        BlackQueenside: rights.Contains('q'));

    private static string CastlingToString(CastlingState c)
    {
        string s = (c.WhiteKingside ? "K" : "") + (c.WhiteQueenside ? "Q" : "")
                 + (c.BlackKingside ? "k" : "") + (c.BlackQueenside ? "q" : "");
        return s.Length == 0 ? "-" : s;
    }

    private static (int Row, int Col)? ParseEnPassant(string ep) =>
        ep == "-" || ep.Length != 2 ? null : (8 - (ep[1] - '0'), ep[0] - 'a');

    private static long Perft(string fen, int depth)
    {
        var parts = fen.Split(' ');
        var board = ChessBoard.FromFEN(fen);
        bool whiteToMove = parts[1] == "w";
        var castling = ParseCastling(parts[2]);
        var enPassant = ParseEnPassant(parts[3]);
        return PerftRecursive(board, whiteToMove, castling, enPassant, depth);
    }

    private static long PerftRecursive(
        ChessBoard board, bool whiteToMove, CastlingState castling, (int Row, int Col)? enPassant, int depth)
    {
        if (depth == 0) return 1;

        long nodes = 0;
        foreach (var move in MoveGenerator.GetAllLegalMovesUci(board, whiteToMove, castling, enPassant))
        {
            var nextBoard = new ChessBoard(board.GetArray());
            string castlingStr = CastlingToString(castling);
            string epStr = enPassant.HasValue ? $"{(char)('a' + enPassant.Value.Col)}{8 - enPassant.Value.Row}" : "-";

            ChessRulesService.ApplyUciMove(nextBoard, move, ref castlingStr, ref epStr);

            nodes += PerftRecursive(nextBoard, !whiteToMove, ParseCastling(castlingStr), ParseEnPassant(epStr), depth - 1);
        }
        return nodes;
    }

    [Theory]
    [InlineData(1, 20)]
    [InlineData(2, 400)]
    [InlineData(3, 8902)]
    [InlineData(4, 197281)]
    public void StartingPosition_MatchesPublishedPerftCounts(int depth, long expectedNodes)
        => Assert.Equal(expectedNodes, Perft(StartFen, depth));

    [Theory]
    [InlineData(1, 48)]
    [InlineData(2, 2039)]
    [InlineData(3, 97862)]
    public void Kiwipete_CastlingAndCapturesHeavy_MatchesPublishedPerftCounts(int depth, long expectedNodes)
        => Assert.Equal(expectedNodes, Perft(KiwipeteFen, depth));

    [Theory]
    [InlineData(1, 14)]
    [InlineData(2, 191)]
    [InlineData(3, 2812)]
    [InlineData(4, 43238)]
    public void Position3_EnPassantHeavy_MatchesPublishedPerftCounts(int depth, long expectedNodes)
        => Assert.Equal(expectedNodes, Perft(Position3Fen, depth));

    [Theory]
    [InlineData(1, 6)]
    [InlineData(2, 264)]
    [InlineData(3, 9467)]
    public void Position4_PromotionHeavy_MatchesPublishedPerftCounts(int depth, long expectedNodes)
        => Assert.Equal(expectedNodes, Perft(Position4Fen, depth));

    // Chess960 coverage: rooks on their standard a/h files under Chess960 rules is operationally
    // identical to standard castling, so these must match the exact same published counts — driven
    // through the Chess960 "king captures own rook" notation end to end. Caught a real bug this
    // way: IsLegalMove's generic post-move check used toCol (the rook's square in this notation)
    // as if it were the king's own destination, which could spuriously detect check on the wrong
    // square and silently drop legal castling moves. Fixed in MoveGenerator.IsLegalMove by
    // returning directly from the already-self-contained IsValidCastling960 result instead of
    // falling through to that generic recheck.
    // Both Chess960 test positions keep the king on the e-file, so a rook's Shredder-FEN
    // file letter maps to kingside/queenside purely by whether it's above or below file e.
    // Also accepts the literal K/Q/k/q letters CastlingToString emits between recursion
    // steps, so this one parser covers both the initial FEN and every subsequent ply.
    private static CastlingState ParseShredderCastling(string rights)
    {
        bool wKs = false, wQs = false, bKs = false, bQs = false;
        foreach (char c in rights)
        {
            if (c == 'K') wKs = true;
            else if (c == 'Q') wQs = true;
            else if (c == 'k') bKs = true;
            else if (c == 'q') bQs = true;
            else if (c >= 'A' && c <= 'H') { if (c - 'A' > 4) wKs = true; else wQs = true; }
            else if (c >= 'a' && c <= 'h') { if (c - 'a' > 4) bKs = true; else bQs = true; }
        }
        return new CastlingState(wKs, wQs, bKs, bQs, Chess960Mode: true,
            WhiteKingsideRookFile: 7, WhiteQueensideRookFile: 0,
            BlackKingsideRookFile: 7, BlackQueensideRookFile: 0);
    }

    private static long Perft960(string fen, int depth)
    {
        var parts = fen.Split(' ');
        var board = ChessBoard.FromFEN(fen);
        bool whiteToMove = parts[1] == "w";
        var castling = ParseShredderCastling(parts[2]);
        var enPassant = ParseEnPassant(parts[3]);
        return PerftRecursive960(board, whiteToMove, castling, enPassant, depth);
    }

    private static long PerftRecursive960(
        ChessBoard board, bool whiteToMove, CastlingState castling, (int Row, int Col)? enPassant, int depth)
    {
        if (depth == 0) return 1;

        long nodes = 0;
        foreach (var move in MoveGenerator.GetAllLegalMovesUci(board, whiteToMove, castling, enPassant))
        {
            var nextBoard = new ChessBoard(board.GetArray());
            string castlingStr = CastlingToString(castling);
            string epStr = enPassant.HasValue ? $"{(char)('a' + enPassant.Value.Col)}{8 - enPassant.Value.Row}" : "-";

            ChessRulesService.ApplyUciMove(nextBoard, move, ref castlingStr, ref epStr);

            nodes += PerftRecursive960(nextBoard, !whiteToMove, ParseShredderCastling(castlingStr), ParseEnPassant(epStr), depth - 1);
        }
        return nodes;
    }

    [Theory]
    [InlineData(1, 20)]
    [InlineData(2, 400)]
    [InlineData(3, 8902)]
    public void Chess960_StartingPositionWithStandardRookFiles_MatchesPublishedPerftCounts(int depth, long expectedNodes)
        => Assert.Equal(expectedNodes, Perft960("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w HAha - 0 1", depth));

    [Theory]
    [InlineData(1, 48)]
    [InlineData(2, 2039)]
    public void Chess960_KiwipeteWithStandardRookFiles_MatchesPublishedPerftCounts(int depth, long expectedNodes)
        => Assert.Equal(expectedNodes, Perft960("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w HAha - 0 1", depth));
}
