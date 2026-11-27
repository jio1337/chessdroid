using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// Golden/snapshot corpus for MovesExplanation.GenerateMoveExplanation (CHESSDROID_AUDIT.md
/// §7.2/§7.3: "a corpus file of {fen, move, pvs, eval, expected} ... the net that makes
/// MovesExplanation/ThreatDetection refactorable"). This function had zero test coverage
/// despite being the single entry point ConsoleOutputFormatter uses for every "why this move"
/// explanation shown in the app, across ~3,500 lines of tactical/positional detection logic.
///
/// This is a SNAPSHOT test, not a correctness oracle: each expected string below is exactly
/// what the function produces TODAY (captured via a throwaway dump test, then transcribed
/// here), not independently verified as the "right" chess explanation — some read a little
/// oddly (e.g. an en passant capture described as "advances... pawn") and that's left as-is
/// rather than silently cleaned up, since the point is catching future behavior CHANGES, not
/// certifying today's wording. If a legitimate improvement changes one of these strings later,
/// update the expected value deliberately rather than treat a failure here as automatically
/// wrong.
///
/// Positions cover a deliberately diverse set of shapes (opening, capture, castling,
/// promotion, en passant, endgame, a mid-game non-mating move, and the forced-mate branch via
/// a real 4-move Scholar's Mate) rather than attempting to reconstruct the 17+ individual
/// false-positive fixes named in CHANGELOG.md, whose prose descriptions aren't reproducible
/// test vectors (no FEN/move/eval given) without independently re-deriving a matching position
/// for each — a given this corpus can still absorb as those are rediscovered.
/// </summary>
public class MovesExplanationGoldenTests
{
    public static IEnumerable<object[]> Cases()
    {
        yield return new object[] { "OpeningQuietMove",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "e2e4", new List<string> { "e2e4 e7e5 g1f3 b8c6" }, "+0.30",
            "advances e-pawn, strong opening principle (controls center)" };

        yield return new object[] { "PawnCapture",
            "rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 2",
            "e4d5", new List<string> { "e4d5 d8d5" }, "+0.50",
            "captures pawn" };

        yield return new object[] { "KiwipeteCastle",
            "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
            "e1g1", new List<string> { "e1g1 e8g8" }, "+0.20",
            "castles kingside, activates rook" };

        yield return new object[] { "PromotionToQueen",
            "k7/4P3/8/8/8/8/8/4K3 w - - 0 1",
            "e7e8q", new List<string> { "e7e8q" }, "+8.50",
            "promotes to Q, creates dangerous passed e-pawn" };

        yield return new object[] { "EnPassantCapture",
            "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1",
            "e5d6", new List<string> { "e5d6" }, "+1.00",
            "advances passed pawn, creates dangerous passed d-pawn" };

        yield return new object[] { "KingPawnEndgameStep",
            "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1",
            "e3f4", new List<string> { "e3f4" }, "+2.00",
            "slight edge (technical endgame)" };

        // Regression for a real false positive the user found live (Scandinavian: 1.e4 d5
        // 2.exd5 Qxd5 3.Nc3 Qa5): DetectXRayAttack used to report "x-ray attack on rook" here
        // because its "does the first piece have a reason to move" check was circular — it
        // just re-confirmed that the piece which moved (here, the queen onto a5) attacks the
        // a2 pawn directly in its own line of sight, which is true by construction for ANY
        // such line and proves nothing about whether the pawn will ever actually move. A pawn
        // blocking a straight file/rank can't clear it via a non-capturing move anyway (it
        // stays on the same file), so the rook was never actually under any real threat. Fixed
        // to require a genuinely independent attacker on the blocking piece.
        yield return new object[] { "NoFalseXrayThroughUnthreatenedPawn",
            "rnb1kbnr/ppp1pppp/8/3q4/8/2N5/PPPP1PPP/R1BQKBNR b KQkq - 1 3",
            "d5a5", new List<string> { "d5a5" }, "+0.76",
            "controls center" };

        // Move 3 of the Scholar's Mate sequence verified in PgnRoundTripTests — Qh5 developing
        // the queen with tempo-ish pressure toward the kingside.
        yield return new object[] { "EarlyQueenDevelopment",
            "r1bqkbnr/pppp1ppp/2n5/4p3/2B1P3/8/PPPP1PPP/RNBQK1NR w KQkq - 2 3",
            "d1h5", new List<string> { "d1h5" }, "+0.40",
            "x-ray attack on king" };

        // Move 4 of the same sequence — Qxf7# — exercises the dedicated "forced checkmate"
        // early-return branch (triggered by an evaluation starting with "Mate in ", which
        // itself needs to keep matching the Score struct's format, see Score.cs ToString()).
        yield return new object[] { "ScholarsMateFinish",
            "r1bqkb1r/pppp1ppp/2n2n2/4p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 4 4",
            "h5f7", new List<string> { "h5f7" }, "Mate in +1",
            "forced checkmate (mate in +1)" };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void GenerateMoveExplanation_MatchesGoldenSnapshot(
        string caseName, string fen, string bestMove, List<string> pvs, string evaluation, string expected)
    {
        string actual = MovesExplanation.GenerateMoveExplanation(bestMove, fen, pvs, evaluation);
        Assert.True(expected == actual, $"[{caseName}] expected \"{expected}\" but got \"{actual}\"");
    }
}
