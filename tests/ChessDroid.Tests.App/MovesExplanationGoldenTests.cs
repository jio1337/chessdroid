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

        // DetectRemovalOfDefender used to read the "captured piece" from the post-move board,
        // where the destination already holds the mover's own piece, so it never fired at all.
        // Nxc6 removes the knight guarding d8, and Rd1 then wins the rook.
        yield return new object[] { "RemovalOfDefenderNowReachable",
            "3r3k/1p6/2n5/N7/8/8/8/3RK3 w - - 0 1",
            "a5c6", new List<string> { "a5c6" }, "+1.00",
            "removes defender of rook, captures knight" };

        // Same capture without Rd1: b7xc6 recaptures and nothing else attacks d8, so this is
        // just a knight trade — must NOT be reported as removing a defender.
        yield return new object[] { "TradeIsNotRemovalOfDefender",
            "3r3k/1p6/2n5/N7/8/8/8/4K3 w - - 0 1",
            "a5c6", new List<string> { "a5c6" }, "+0.00",
            "captures knight, creates isolated b-pawn for opponent" };

        // Positions from Mackenzie–Paulsen, London 1862 (Evans Gambit), each a false
        // positive reported from live play.
        // 4...Bxb4: bishop lines up with e1 through the d2 pawn — nothing else attacks d2, so
        // it's not an x-ray on the king.
        yield return new object[] { "EvansGambitBxb4NoXray",
            "r1bqk1nr/pppp1ppp/2n5/2b1p3/1PB1P3/5N2/P1PP1PPP/RNBQK2R b KQkq b3 0 4",
            "c5b4", new List<string> { "c5b4" }, "-0.17",
            "wins pawn, creates isolated a-pawn for opponent" };

        // 18...Nc4 "attacks" Ba3 and Ne5, but Ne5 just takes the knight — not a fork.
        yield return new object[] { "KnightForkRefutedByKnightRecapture",
            "r2q1r1k/pp1bn1pB/1b2p2p/n1p1N3/8/B1N5/P1Q2PPP/3R1RK1 b - - 1 18",
            "a5c4", new List<string> { "a5c4" }, "+2.00",
            "maximizes piece activity, attacks knight" };

        // 21...Bxc2 / 21...Bxh7: the f5 bishop itself sat between Qc2 and Bh7, so neither
        // defended the other — no "removes defender".
        yield return new object[] { "CaptureThatUnblocksLineIsNotRemovalOfDefender",
            "r2R1r1k/pp4pB/1b5p/n1p1Nb2/8/B7/P1Q2PPP/5RK1 b - - 0 21",
            "f5c2", new List<string> { "f5c2" }, "+4.50",
            "captures queen" };

        yield return new object[] { "CaptureThatUnblocksLineIsNotRemovalOfDefender2",
            "r2R1r1k/pp4pB/1b5p/n1p1Nb2/8/B7/P1Q2PPP/5RK1 b - - 0 21",
            "f5h7", new List<string> { "f5h7" }, "+5.31",
            "captures bishop" };

        // 23.Bxc2: just winning a free bishop, not a "deflection".
        yield return new object[] { "FreePieceCaptureIsNotDeflection",
            "5r1k/pp4pB/1b5p/n1p1N3/8/B7/P1b2PPP/5RK1 w - - 0 23",
            "h7c2", new List<string> { "h7c2" }, "+4.55",
            "wins bishop" };

        // Regression: DetectSmotheredMate required >= 6 own pieces surrounding the king, but
        // a king on the back rank (where every real smothered mate happens) has at most 5
        // valid on-board neighbor squares, so the label could never appear for the canonical
        // pattern. Fixed to compare against the actual count of valid neighbor squares.
        yield return new object[] { "SmotheredMateOnBackRankNowReachable",
            "5rkb/5ppp/2N5/8/8/8/8/4K3 w - - 0 1",
            "c6e7", new List<string> { "c6e7" }, "+5.00",
            "smothered mate, maximizes piece activity" };

        // Regression: CountPawnShield's "rank in front of the king" formula was inverted
        // (kingRow + 1 for White instead of - 1, confirmed wrong by contrast with DetectLuft's
        // correct formula for the same concept elsewhere in this file), so for a king still on
        // its own back rank — the normal case — the shelter count always computed an
        // out-of-bounds row and returned 0, making "improves king safety" unreachable. No
        // castling rights here so the king is forced to walk rather than castle, which has its
        // own dedicated message and would otherwise mask this specific comparison.
        yield return new object[] { "PawnShieldDirectionFixed",
            "4k3/8/8/8/8/8/5PPP/4K3 w - - 0 1",
            "e1f1", new List<string> { "e1f1" }, "+0.10",
            "improves king safety" };

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
