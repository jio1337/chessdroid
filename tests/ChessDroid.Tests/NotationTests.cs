using ChessDroid.Services;

namespace ChessDroid.Tests
{
    public class NotationTests
    {
        private static string ConvertUciToSan(string uciMove, string fen) =>
            ChessNotationService.ConvertUCIToSAN(
                uciMove, fen,
                ChessRulesService.CanReachSquare,
                ChessRulesService.FindAllPiecesOfSameType);

        [Theory]
        [InlineData("e2e4", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", "e4")]
        [InlineData("g1f3", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", "Nf3")]
        public void ConvertUCIToSAN_Simple_Moves(string uci, string fen, string expectedSan)
        {
            Assert.Equal(expectedSan, ConvertUciToSan(uci, fen));
        }

        [Fact]
        public void ConvertUCIToSAN_Capture_Includes_X()
        {
            // White knight on e5 takes black pawn on f7 is illegal as a test setup;
            // use a simple pawn capture instead: white pawn e4 takes black pawn d5.
            string fen = "rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 2";
            Assert.Equal("exd5", ConvertUciToSan("e4d5", fen));
        }

        [Fact]
        public void ConvertUCIToSAN_Disambiguates_Two_Knights_On_Same_File()
        {
            // White knights on d2 and d4, both can reach b3 — must disambiguate by rank.
            string fen = "8/8/8/8/3N4/8/3N1K2/7k w - - 0 1";
            Assert.Equal("N2b3", ConvertUciToSan("d2b3", fen));
            Assert.Equal("N4b3", ConvertUciToSan("d4b3", fen));
        }

        [Theory]
        [InlineData("e1g1", "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "O-O")]
        [InlineData("e1c1", "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "O-O-O")]
        [InlineData("e8g8", "r3k2r/8/8/8/8/8/8/R3K2R b KQkq - 0 1", "O-O")]
        public void ConvertUCIToSAN_Castling(string uci, string fen, string expectedSan)
        {
            Assert.Equal(expectedSan, ConvertUciToSan(uci, fen));
        }

        [Fact]
        public void ConvertUCIToSAN_Promotion_Appends_Equals_Piece()
        {
            string fen = "8/P6k/8/8/8/8/7K/8 w - - 0 1";
            Assert.Equal("a8=Q", ConvertUciToSan("a7a8q", fen));
        }

        [Theory]
        [InlineData("a2a4")]
        [InlineData("b2b3")]
        [InlineData("a7a8q")]
        public void IsValidUCIMove_Accepts_Board_Coordinates(string move)
        {
            Assert.True(ChessNotationService.IsValidUCIMove(move));
        }

        [Theory]
        [InlineData("")]
        [InlineData("e9e4")]
        [InlineData("z1z2")]
        [InlineData(null)]
        public void IsValidUCIMove_Rejects_Malformed_Input(string? move)
        {
            Assert.False(ChessNotationService.IsValidUCIMove(move));
        }
    }
}
