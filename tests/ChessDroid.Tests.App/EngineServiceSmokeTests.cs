using ChessDroid;
using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>Pipeline smoke test — confirms the App ProjectReference + FakeUciEngine wiring works end to end.</summary>
public class EngineServiceSmokeTests
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Fact]
    public async Task GetBestMoveAsync_AgainstFakeEngine_ReturnsExpectedMove()
    {
        var config = new AppConfig { EngineResponseTimeoutMs = 5000, MaxEngineRetries = 2 };
        var svc = new ChessEngineService(config);
        try
        {
            await svc.InitializeAsync(FakeEngineFixture.NormalPath);
            Assert.Equal(EngineState.Ready, svc.State);

            var (bestMove, eval, pvs, _, _) = await svc.GetBestMoveAsync(StartFen, depth: 5, multiPV: 2);

            Assert.Equal("e2e4", bestMove);
            Assert.Equal("+0.39", eval);
            Assert.Equal(2, pvs.Count);
        }
        finally
        {
            svc.Dispose();
        }
    }
}
