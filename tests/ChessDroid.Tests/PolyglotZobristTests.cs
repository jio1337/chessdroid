using ChessDroid.Services;

namespace ChessDroid.Tests
{
    // Reference keys from the Polyglot book-format spec
    // (http://hgm.nubati.net/book_format.html), computed by every
    // Polyglot-compatible program. These catch any regression in the
    // random-number table or the hashing order instantly.
    public class PolyglotZobristTests
    {
        [Fact]
        public void StartingPosition_Matches_PolyglotReferenceKey()
        {
            string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            ulong key = PolyglotZobrist.ComputeKey(fen);
            Assert.Equal(0x463b96181691fc9cUL, key);
        }

        [Fact]
        public void AfterE4_Matches_PolyglotReferenceKey()
        {
            string fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";
            ulong key = PolyglotZobrist.ComputeKey(fen);
            Assert.Equal(0x823c9b50fd114196UL, key);
        }

        [Fact]
        public void AfterE4D5_Matches_PolyglotReferenceKey()
        {
            string fen = "rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 2";
            ulong key = PolyglotZobrist.ComputeKey(fen);
            Assert.Equal(0x0756b94461c50fb0UL, key);
        }

        [Fact]
        public void AfterE4D5E5_Matches_PolyglotReferenceKey()
        {
            string fen = "rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 2";
            ulong key = PolyglotZobrist.ComputeKey(fen);
            Assert.Equal(0x662fafb965db29d4UL, key);
        }

        [Fact]
        public void AfterE4D5E5F5_Matches_PolyglotReferenceKey()
        {
            string fen = "rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3";
            ulong key = PolyglotZobrist.ComputeKey(fen);
            Assert.Equal(0x22a48b5a8e47ff78UL, key);
        }

        [Fact]
        public void AfterE4D5E5F5Ke2_Matches_PolyglotReferenceKey()
        {
            string fen = "rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPPKPPP/RNBQ1BNR b kq - 0 3";
            ulong key = PolyglotZobrist.ComputeKey(fen);
            Assert.Equal(0x652a607ca3f242c1UL, key);
        }

        [Fact]
        public void TrickyPosition_Matches_PolyglotReferenceKey()
        {
            string fen = "rnbq1bnr/ppp1pkpp/8/3pPp2/8/8/PPPPKPPP/RNBQ1BNR w - - 0 4";
            ulong key = PolyglotZobrist.ComputeKey(fen);
            Assert.Equal(0x00fdd303c946bdd9UL, key);
        }

        // After 1.a4 b5 2.h4 b4 3.c4 — three different white pawns have left rank 2
        // (a4, h4, c4), and the c4 pawn is en-passant-capturable by the b4 black pawn.
        [Fact]
        public void TrickyPosition2_Matches_PolyglotReferenceKey()
        {
            string fen = "rnbqkbnr/p1pppppp/8/8/PpP4P/8/1P1PPPP1/RNBQKBNR b KQkq c3 0 3";
            ulong key = PolyglotZobrist.ComputeKey(fen);
            Assert.Equal(0x3c8123ea7b067637UL, key);
        }

        // Continuing with 3...bxc3 (en passant) 4.Rxa3(Ra1-a3) — the captured c4 pawn
        // is gone, the black pawn lands on c3, and white's queenside rook move drops
        // the Q castling right while keeping K.
        [Fact]
        public void TrickyPosition3_Matches_PolyglotReferenceKey()
        {
            string fen = "rnbqkbnr/p1pppppp/8/8/P6P/R1p5/1P1PPPP1/1NBQKBNR b Kkq - 0 4";
            ulong key = PolyglotZobrist.ComputeKey(fen);
            Assert.Equal(0x5c3f9b829b279560UL, key);
        }
    }
}
