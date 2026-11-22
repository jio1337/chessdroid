// Minimal fake UCI engine used by ChessDroid.Tests.App to verify ChessEngineService /
// EngineMatchService behavior against a real child process, without depending on a real
// chess engine binary being present in CI. Behavior is selected by the exe's own file name
// (ProcessStartInfo in ChessEngineService doesn't pass arguments), so FakeEngineFixture
// copies this build to differently-named files:
//   fakeuciengine.exe         - normal: responds to uci/isready/go depth/go infinite/stop
//   fakeuciengine-hang.exe    - never responds to "go" (simulates a hung engine)
//   fakeuciengine-crash.exe   - exits the process shortly after starting (simulates a crash)
//   fakeuciengine-nullmove.exe - always answers "go" with bestmove 0000 (no legal move)

string exeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "").ToLowerInvariant();
bool hang = exeName.Contains("hang");
bool crash = exeName.Contains("crash");
bool nullMove = exeName.Contains("nullmove");

if (crash)
{
    // Look alive just long enough for the caller to see uciok/readyok, then die.
    Console.Out.WriteLine("id name FakeEngine");
    Console.Out.WriteLine("uciok");
    Console.Out.Flush();
    await Task.Delay(150);
    Environment.Exit(1);
    return;
}

var stdout = Console.Out;
void Send(string s) { stdout.WriteLine(s); stdout.Flush(); }

int multiPvSetting = 1;
bool stopRequested = false;

async Task RunGoDepth(int depth)
{
    stopRequested = false;
    if (hang)
    {
        while (!stopRequested) await Task.Delay(50);
        Send("bestmove 0000");
        return;
    }
    if (nullMove)
    {
        Send("info depth 1 seldepth 1 multipv 1 score cp 0 pv 0000");
        Send("bestmove 0000");
        return;
    }
    for (int d = 1; d <= depth; d++)
    {
        for (int mpv = 1; mpv <= multiPvSetting; mpv++)
        {
            int cp = 25 + d * 3 - mpv; // PV1 always best for white
            string pv = mpv == 1 ? "e2e4 e7e5" : "d2d4 d7d5";
            Send($"info depth {d} seldepth {d + 2} multipv {mpv} score cp {cp} wdl {500 + d} {300 - d} {200} pv {pv}");
        }
        await Task.Delay(1);
        if (stopRequested) break;
    }
    Send("bestmove e2e4 ponder e7e5");
}

async Task RunGoInfinite()
{
    stopRequested = false;
    int d = 0;
    while (!stopRequested)
    {
        d++;
        for (int mpv = 1; mpv <= multiPvSetting; mpv++)
        {
            int cp = 10 + d * 2 - mpv;
            string pv = mpv == 1 ? "g1f3 g8f6" : "c2c4 c7c5";
            Send($"info depth {d} seldepth {d + 1} multipv {mpv} score cp {cp} wdl {480 + d} {310} {210 - d} pv {pv}");
        }
        await Task.Delay(5);
        if (d > 500) break; // safety valve
    }
    Send("bestmove g1f3 ponder g8f6");
}

string? line;
while ((line = Console.In.ReadLine()) != null)
{
    line = line.Trim();
    if (line == "uci")
    {
        Send("id name FakeEngine");
        Send("id author harness");
        Send("option name UCI_ShowWDL type check default false");
        Send("uciok");
    }
    else if (line == "isready")
    {
        Send("readyok");
    }
    else if (line.StartsWith("setoption name MultiPV value"))
    {
        var parts = line.Split(' ');
        if (int.TryParse(parts[^1], out int mpv)) multiPvSetting = mpv;
    }
    else if (line.StartsWith("setoption")) { /* ignore other options */ }
    else if (line.StartsWith("position")) { /* stateless fake */ }
    else if (line.StartsWith("ucinewgame")) { /* ignore */ }
    else if (line.StartsWith("go depth"))
    {
        var parts = line.Split(' ');
        int depth = int.TryParse(parts[^1], out int dd) ? dd : 5;
        _ = Task.Run(() => RunGoDepth(depth));
    }
    else if (line.StartsWith("go infinite"))
    {
        _ = Task.Run(RunGoInfinite);
    }
    else if (line.StartsWith("go movetime"))
    {
        var parts = line.Split(' ');
        int ms = int.TryParse(parts[^1], out int m) ? m : 100;
        _ = Task.Run(async () =>
        {
            stopRequested = false;
            if (hang) { while (!stopRequested) await Task.Delay(50); Send("bestmove 0000"); return; }
            await Task.Delay(Math.Min(ms, 50));
            Send("info depth 3 seldepth 3 multipv 1 score cp 15 pv e2e4 e7e5");
            Send(nullMove ? "bestmove 0000" : "bestmove e2e4");
        });
    }
    else if (line == "stop")
    {
        stopRequested = true;
    }
    else if (line == "quit")
    {
        break;
    }
}
