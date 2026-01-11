using System.Diagnostics;

namespace ChessDroid.Services
{
    public class ChessEngineService
    {
        private Process? engineProcess;
        private StreamWriter? engineInput;
        private StreamReader? engineOutput;
        private readonly AppConfig config;

        // UCI Protocol Commands
        private const string UCI_CMD_UCI = "uci";

        private const string UCI_CMD_UCINEWGAME = "ucinewgame";
        private const string UCI_CMD_POSITION = "position fen";
        private const string UCI_CMD_GO_DEPTH = "go depth";
        private const string UCI_CMD_SETOPTION = "setoption name";
        private const string UCI_RESPONSE_INFO = "info";
        private const string UCI_RESPONSE_BESTMOVE = "bestmove";
        private const string UCI_TOKEN_MULTIPV = "multipv";
        private const string UCI_TOKEN_SCORE = "score";
        private const string UCI_TOKEN_CP = "cp";
        private const string UCI_TOKEN_MATE = "mate";
        private const string UCI_TOKEN_PV = "pv";

        public ChessEngineService(AppConfig config)
        {
            this.config = config;
        }

        public async Task InitializeAsync(string enginePath)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = enginePath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                engineProcess = Process.Start(psi);
                if (engineProcess == null)
                    throw new Exception("Failed to start engine process");

                engineInput = engineProcess.StandardInput;
                engineOutput = engineProcess.StandardOutput;

                await engineInput.WriteLineAsync(UCI_CMD_UCI);
                await engineInput.FlushAsync();
                await Task.Delay(10);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting engine: {ex.Message}");
                throw;
            }
        }

        public async Task<(string bestMove, string evaluation, List<string> pvs, List<string> evaluations)> GetBestMoveAsync(
            string fen, int depth, int multiPV)
        {
            string bestMove = "";
            string evaluation = "";
            var pvs = new List<string>();
            var evaluations = new List<string>();
            int retryCount = 0;

            if (engineInput == null || engineOutput == null)
                return (bestMove, evaluation, pvs, evaluations);

            // Configure engine to output N PV lines
            await engineInput.WriteLineAsync($"{UCI_CMD_SETOPTION} MultiPV value {multiPV}");
            await engineInput.FlushAsync();

            while (retryCount < config.MaxEngineRetries)
            {
                try
                {
                    await engineInput.WriteLineAsync(UCI_CMD_UCINEWGAME);
                    await engineInput.FlushAsync();

                    await engineInput.WriteLineAsync($"{UCI_CMD_POSITION} {fen}");
                    await engineInput.FlushAsync();

                    await engineInput.WriteLineAsync($"{UCI_CMD_GO_DEPTH} {depth}");
                    await engineInput.FlushAsync();

                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(config.EngineResponseTimeoutMs);

                    while (!engineOutput.EndOfStream)
                    {
                        string? line = await engineOutput.ReadLineAsync().WaitAsync(cts.Token);

                        if (line == null) continue;

                        // Parse info lines containing PV
                        if (line.StartsWith(UCI_RESPONSE_INFO) && line.Contains(UCI_TOKEN_PV))
                        {
                            var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            int mpvIndex = 1;
                            string evalStr = "";
                            var pvList = new List<string>();

                            for (int i = 0; i < tokens.Length; i++)
                            {
                                if (tokens[i] == UCI_TOKEN_MULTIPV && i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out int mpv))
                                    mpvIndex = mpv;

                                if (tokens[i] == UCI_TOKEN_SCORE && i + 2 < tokens.Length)
                                {
                                    if (tokens[i + 1] == UCI_TOKEN_CP && double.TryParse(tokens[i + 2], out double cp))
                                        evalStr = (cp / 100.0 >= 0 ? "+" : "") + (cp / 100.0).ToString("F2");
                                    else if (tokens[i + 1] == UCI_TOKEN_MATE)
                                        evalStr = "Mate in " + tokens[i + 2];
                                }

                                if (tokens[i] == UCI_TOKEN_PV)
                                {
                                    for (int j = i + 1; j < tokens.Length; j++)
                                        pvList.Add(tokens[j]);
                                    break;
                                }
                            }

                            string pvLine = string.Join(" ", pvList);

                            // Ensure the lists have the correct index
                            while (pvs.Count < mpvIndex)
                            {
                                pvs.Add("");
                                evaluations.Add("");
                            }

                            pvs[mpvIndex - 1] = pvLine;
                            evaluations[mpvIndex - 1] = evalStr;

                            // The eval for PV1 (best line)
                            if (mpvIndex == 1)
                                evaluation = evalStr;
                        }
                        else if (line.StartsWith(UCI_RESPONSE_BESTMOVE))
                        {
                            var parts = line.Split(' ');
                            if (parts.Length >= 2)
                            {
                                bestMove = parts[1];
                            }
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(bestMove))
                    {
                        break; // Success - exit retry loop
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"Engine response timeout (attempt {retryCount + 1}/{config.MaxEngineRetries})");
                    // Don't restart on first timeout - just retry
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Engine communication error: {ex.Message}");
                }

                retryCount++;

                // Only restart engine on last retry or if process is dead
                if (retryCount < config.MaxEngineRetries)
                {
                    // Check if engine process is still alive
                    bool engineDead = engineProcess == null || engineProcess.HasExited;

                    if (engineDead || retryCount == config.MaxEngineRetries - 1)
                    {
                        Debug.WriteLine($"Restarting engine (attempt {retryCount}/{config.MaxEngineRetries})...");
                        await RestartAsync();
                        await Task.Delay(500); // Give engine time to initialize
                    }
                    else
                    {
                        // Just wait a bit before retry without full restart
                        Debug.WriteLine($"Retrying without restart (attempt {retryCount}/{config.MaxEngineRetries})...");
                        await Task.Delay(100);
                    }
                }
            }

            // Trim to requested count
            if (pvs.Count > multiPV)
            {
                pvs = pvs.Take(multiPV).ToList();
                evaluations = evaluations.Take(multiPV).ToList();
            }

            return (bestMove, evaluation, pvs, evaluations);
        }

        public async Task RestartAsync()
        {
            string? enginePath = null;
            try
            {
                if (engineProcess != null && !engineProcess.HasExited)
                {
                    enginePath = engineProcess.StartInfo?.FileName;
                    engineProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restarting engine: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(enginePath))
            {
                await InitializeAsync(enginePath);
            }
        }

        public void Dispose()
        {
            try
            {
                if (engineProcess != null && !engineProcess.HasExited)
                {
                    engineProcess.Kill();
                    engineProcess.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing engine: {ex.Message}");
            }
        }
    }
}