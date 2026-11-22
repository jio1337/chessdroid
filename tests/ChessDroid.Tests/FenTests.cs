using ChessDroid.Models;

namespace ChessDroid.Tests
{
    public class FenTests
    {
        private const string StartingPosition =
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

        [Theory]
        [InlineData(StartingPosition)]
        [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R")] // Kiwipete
        [InlineData("8/8/8/8/8/8/8/8")] // empty board
        [InlineData("4k3/8/8/8/8/8/8/4K3")] // kings only
        public void ToFEN_RoundTrips_Through_FromFEN(string placement)
        {
            var board = ChessBoard.FromFEN(placement);
            Assert.Equal(placement, board.ToFEN());
        }

        [Fact]
        public void FromFEN_Ignores_Fields_After_Placement()
        {
            string full = StartingPosition + " w KQkq - 0 1";
            var board = ChessBoard.FromFEN(full);
            Assert.Equal(StartingPosition, board.ToFEN());
        }

        [Theory]
        [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                    "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        // Unknown piece letters (e.g. a user-typed 'D') collapse into empty-square counts,
        // and adjacent empty counts merge rather than appearing as two separate runs.
        [InlineData("rnbqkbnr/pppppppp/8/8/8/3D4/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                    "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        public void SanitizeFenForEngine_Produces_Expected_Output(string input, string expected)
        {
            Assert.Equal(expected, ChessBoard.SanitizeFenForEngine(input));
        }

        [Fact]
        public void SanitizeFenForEngine_Preserves_Fields_After_Placement()
        {
            string input = StartingPosition + " b kq e3 5 12";
            string result = ChessBoard.SanitizeFenForEngine(input);
            Assert.Equal(StartingPosition + " b kq e3 5 12", result);
        }

        [Fact]
        public void GetKingPosition_Finds_Both_Kings_After_FromFEN()
        {
            var board = ChessBoard.FromFEN(StartingPosition);
            Assert.Equal((7, 4), board.GetKingPosition(isWhite: true));
            Assert.Equal((0, 4), board.GetKingPosition(isWhite: false));
        }
    }
}
