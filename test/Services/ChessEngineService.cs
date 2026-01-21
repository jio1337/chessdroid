using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Engine state machine states (SCID-inspired)
    /// </summary>
    public enum EngineState
    {
        Uninitialized,  // Engine not started
        Starting,       // Engine process started, waiting for uciok
        Ready,          // Engine ready, can accept commands
        Analyzing,      // Engine analyzing position
        Error           // Engine in error state, needs restart
    }

    public class ChessEngineService
    {
        private Process? engineProcess;
        private StreamWriter? engineInput;
        private StreamReader? engineOutput;
        private readonly AppConfig config;
        private string? lastEnginePath;
        private EngineState state = EngineState.Uninitialized;
        private readonly object stateLock = new object();

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
        private const string UCI_TOKEN_WDL = "wdl";

        public ChessEngineService(AppConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Get current engine state
        /// </summary>
        public EngineState State
        {
            get { lock (stateLock) return state; }
            private set { lock (stateLock) state = value; }
        }

        /// <summary>
        /// Check if engine process is alive and ready
        /// </summary>
        public bool IsEngineAlive()
        {
            return engineProcess != null && !engineProcess.HasExited && engineInput != null && engineOutput != null;
        }

        /// <summary>
        /// Synchronizes with the engine using isready/readyok protocol.
        /// This ensures all previous commands have been processed before continuing.
        /// SCID-inspired pattern for reliable engine communication.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds (default 3000ms)</param>
        /// <returns>True if engine responded with readyok, false otherwise</returns>
        private async Task<bool> SyncWithEngineAsync(int timeoutMs = 3000)
        {
            try
            {
                if (!IsEngineAlive())
                {
                    Debug.WriteLine("SyncWithEngine: Engine not alive");
                    State = EngineState.Error;
                    return false;
                }

                if (!await SafeWriteLineAsync("isready"))
                {
                    Debug.WriteLine("SyncWithEngine: Failed to send isready");
                    State = EngineState.Error;
                    return false;
                }

                using var cts = new CancellationTokenSource(timeoutMs);
                while (IsEngineAlive())
                {
                    try
                    {
                        string? line = await engineOutput!.ReadLineAsync().WaitAsync(cts.Token);
                        if (line != null && line.StartsWith("readyok"))
                        {
                            Debug.WriteLine("SyncWithEngine: Received readyok - engine synchronized");
                            State = EngineState.Ready;
                            return true;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine($"SyncWithEngine: Timeout after {timeoutMs}ms waiting for readyok");
                        State = EngineState.Error;
                        return false;
                    }
                }

                Debug.WriteLine("SyncWithEngine: Engine died while waiting for readyok");
                State = EngineState.Error;
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SyncWithEngine: Error - {ex.Message}");
                State = EngineState.Error;
                return false;
            }
        }

        /// <summary>
        /// Safely write to engine, checking if process is alive first
        /// </summary>
        private async Task<bool> SafeWriteLineAsync(string command)
        {
            try
            {
                if (!IsEngineAlive())
                {
                    Debug.WriteLine($"Engine not alive, cannot send: {command}");
                    return false;
                }

                await engineInput!.WriteLineAsync(command);
                await engineInput.FlushAsync();
                return true;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"IOException writing to engine: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to engine: {ex.Message}");
                return false;
            }
        }

        public async Task InitializeAsync(string enginePath)
        {
            try
            {
                // Store path for restart capability
                lastEnginePath = enginePath;
                State = EngineState.Starting;

                // Clean up any existing process
                CleanupProcess();

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = enginePath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // Also capture stderr
                    CreateNoWindow = true
                };

                engineProcess = Process.Start(psi);
                if (engineProcess == null)
                    throw new Exception("Failed to start engine process");

                engineInput = engineProcess.StandardInput;
                engineOutput = engineProcess.StandardOutput;

                // Give the process a moment to fully start
                await Task.Delay(100);

                if (!IsEngineAlive())
                {
                    throw new Exception("Engine process died immediately after start");
                }

                // Step 1: Send 'uci' and wait for 'uciok' response
                if (!await SafeWriteLineAsync(UCI_CMD_UCI))
                {
                    throw new Exception("Failed to send UCI command");
                }

                // Wait for 'uciok' with timeout
                bool receivedUciOk = false;
                using (var cts = new CancellationTokenSource(5000))
                {
                    while (!receivedUciOk && IsEngineAlive())
                    {
                        try
                        {
                            string? line = await engineOutput.ReadLineAsync().WaitAsync(cts.Token);
                            if (line != null && line.StartsWith("uciok"))
                            {
                                receivedUciOk = true;
                                Debug.WriteLine("Engine: received uciok");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine("Warning: Timeout waiting for uciok");
                            break;
                        }
                    }
                }

                if (!IsEngineAlive())
                {
                    throw new Exception("Engine died during UCI initialization");
                }

                // Step 1.5: Enable WDL output (for Lc0-inspired WDL display)
                await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} UCI_ShowWDL value true");
                Debug.WriteLine("Engine: enabled UCI_ShowWDL for WDL output");

                // Step 2: Send 'isready' and wait for 'readyok'
                if (!await SafeWriteLineAsync("isready"))
                {
                    throw new Exception("Failed to send isready command");
                }

                bool receivedReadyOk = false;
                using (var cts2 = new CancellationTokenSource(5000))
                {
                    while (!receivedReadyOk && IsEngineAlive())
                    {
                        try
                        {
                            string? line = await engineOutput.ReadLineAsync().WaitAsync(cts2.Token);
                            if (line != null && line.StartsWith("readyok"))
                            {
                                receivedReadyOk = true;
                                Debug.WriteLine("Engine: received readyok - fully initialized");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine("Warning: Timeout waiting for readyok");
                            break;
                        }
                    }
                }

                // Set state based on initialization result
                if (receivedUciOk && receivedReadyOk && IsEngineAlive())
                {
                    State = EngineState.Ready;
                }
                else
                {
                    State = EngineState.Error;
                }

                Debug.WriteLine($"Engine initialized: uciok={receivedUciOk}, readyok={receivedReadyOk}, alive={IsEngineAlive()}, state={State}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting engine: {ex.Message}");
                State = EngineState.Error;
                CleanupProcess();
                throw;
            }
        }

        public async Task<(string bestMove, string evaluation, List<string> pvs, List<string> evaluations, WDLInfo? wdl)> GetBestMoveAsync(
            string fen, int depth, int multiPV)
        {
            string bestMove = "";
            string evaluation = "";
            var pvs = new List<string>();
            var evaluations = new List<string>();
            WDLInfo? wdlInfo = null;
            int retryCount = 0;

            // Determine whose turn it is from FEN (for evaluation perspective)
            // UCI evaluations are always from White's perspective
            string[] fenParts = fen.Split(' ');
            bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";

            while (retryCount < config.MaxEngineRetries)
            {
                // Check engine health before each attempt
                if (!IsEngineAlive())
                {
                    Debug.WriteLine($"Engine not alive at start of attempt {retryCount + 1}, restarting...");
                    await RestartAsync();
                    if (!IsEngineAlive())
                    {
                        Debug.WriteLine("Failed to restart engine");
                        retryCount++;
                        continue;
                    }
                }

                try
                {
                    // Sync with engine before sending commands (SCID-inspired)
                    // This ensures the engine is ready and previous commands are processed
                    if (!await SyncWithEngineAsync())
                    {
                        Debug.WriteLine("Engine sync failed before analysis");
                        throw new IOException("Engine sync failed");
                    }

                    // Configure MultiPV
                    if (!await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} MultiPV value {multiPV}"))
                    {
                        throw new IOException("Failed to set MultiPV option");
                    }

                    // Send ucinewgame (clears hash tables)
                    if (!await SafeWriteLineAsync(UCI_CMD_UCINEWGAME))
                    {
                        throw new IOException("Failed to send ucinewgame");
                    }

                    // Set position
                    if (!await SafeWriteLineAsync($"{UCI_CMD_POSITION} {fen}"))
                    {
                        throw new IOException("Failed to set position");
                    }

                    // Update state to analyzing
                    State = EngineState.Analyzing;

                    // Start analysis
                    // If MinAnalysisTimeMs > 0: use time-based search (reaches deeper on complex positions)
                    // If MinAnalysisTimeMs = 0: use depth-based search (fast, consistent depth)
                    string goCommand;
                    int effectiveTimeout;

                    if (config.MinAnalysisTimeMs > 0)
                    {
                        // Time-based search: engine searches for X milliseconds, reaching whatever depth it can
                        // This gives better results on complex positions where depth 15 isn't enough
                        goCommand = $"go movetime {config.MinAnalysisTimeMs}";
                        effectiveTimeout = config.MinAnalysisTimeMs + 2000; // Add buffer for response
                        Debug.WriteLine($"Engine: Time-based search for {config.MinAnalysisTimeMs}ms");
                    }
                    else
                    {
                        // Depth-based search: fast, reaches exact depth
                        goCommand = $"{UCI_CMD_GO_DEPTH} {depth}";
                        effectiveTimeout = config.EngineResponseTimeoutMs;
                        Debug.WriteLine($"Engine: Depth-based search to depth {depth}");
                    }

                    if (!await SafeWriteLineAsync(goCommand))
                    {
                        throw new IOException("Failed to start analysis");
                    }

                    // Read analysis results
                    using (var cts = new CancellationTokenSource(effectiveTimeout))
                    {
                        while (IsEngineAlive())
                        {
                            string? line = await engineOutput!.ReadLineAsync().WaitAsync(cts.Token);

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
                                        {
                                            // UCI scores are always from White's perspective
                                            // If Black to move, negate the score for correct display from Black's perspective
                                            double displayCp = whiteToMove ? cp : -cp;
                                            evalStr = (displayCp / 100.0 >= 0 ? "+" : "") + (displayCp / 100.0).ToString("F2");
                                        }
                                        else if (tokens[i + 1] == UCI_TOKEN_MATE)
                                        {
                                            int mateIn = int.Parse(tokens[i + 2]);
                                            // Negate mate distance for Black too
                                            int displayMate = whiteToMove ? mateIn : -mateIn;
                                            evalStr = "Mate in " + displayMate;
                                        }
                                    }

                                    // Parse WDL (Win/Draw/Loss) data - format: "wdl W D L" (per mille values)
                                    if (tokens[i] == UCI_TOKEN_WDL && i + 3 < tokens.Length)
                                    {
                                        if (int.TryParse(tokens[i + 1], out int w) &&
                                            int.TryParse(tokens[i + 2], out int d) &&
                                            int.TryParse(tokens[i + 3], out int l))
                                        {
                                            // Only store WDL for best line (multipv 1)
                                            if (mpvIndex == 1)
                                            {
                                                wdlInfo = new WDLInfo(w, d, l);
                                            }
                                        }
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
                    }

                    if (!string.IsNullOrEmpty(bestMove))
                    {
                        State = EngineState.Ready; // Analysis complete, back to ready
                        break; // Success - exit retry loop
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"Engine response timeout (attempt {retryCount + 1}/{config.MaxEngineRetries})");
                    State = EngineState.Error;
                }
                catch (IOException ex)
                {
                    Debug.WriteLine($"Engine IO error: {ex.Message}");
                    State = EngineState.Error;
                    // IO error likely means pipe is broken - force restart
                    CleanupProcess();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Engine communication error: {ex.Message}");
                    State = EngineState.Error;
                }

                retryCount++;

                if (retryCount < config.MaxEngineRetries)
                {
                    Debug.WriteLine($"Restarting engine (attempt {retryCount}/{config.MaxEngineRetries})...");
                    await RestartAsync();
                    await Task.Delay(200); // Give engine time to initialize
                }
            }

            // Trim to requested count
            if (pvs.Count > multiPV)
            {
                pvs = pvs.Take(multiPV).ToList();
                evaluations = evaluations.Take(multiPV).ToList();
            }

            // Sort PV lines by evaluation (best to worst)
            // This ensures consistent ordering even if engine sends updates out of order
            if (pvs.Count > 1 && evaluations.Count == pvs.Count)
            {
                var combined = pvs.Zip(evaluations, (pv, eval) => new { Pv = pv, Eval = eval }).ToList();

                // Sort by evaluation (descending = best first)
                combined.Sort((a, b) =>
                {
                    double evalA = ParseEvaluationForSorting(a.Eval);
                    double evalB = ParseEvaluationForSorting(b.Eval);
                    return evalB.CompareTo(evalA); // Descending order (best first)
                });

                pvs = combined.Select(x => x.Pv).ToList();
                evaluations = combined.Select(x => x.Eval).ToList();

                // Update main evaluation to be the best one after sorting
                if (evaluations.Count > 0)
                    evaluation = evaluations[0];
            }

            // If engine didn't provide WDL, estimate from centipawns
            if (wdlInfo == null && !string.IsNullOrEmpty(evaluation) && !evaluation.StartsWith("Mate"))
            {
                // Parse centipawns from evaluation string (e.g., "+1.50" or "-0.75")
                if (double.TryParse(evaluation.Replace("+", ""), out double evalPawns))
                {
                    double centipawns = evalPawns * 100;
                    wdlInfo = WDLUtilities.EstimateWDLFromCentipawns(centipawns);
                    Debug.WriteLine($"WDL estimated from eval {evaluation}: {wdlInfo}");
                }
            }

            return (bestMove, evaluation, pvs, evaluations, wdlInfo);
        }

        /// <summary>
        /// Parse evaluation string for sorting purposes
        /// Returns a numeric value where higher = better
        /// </summary>
        private static double ParseEvaluationForSorting(string evalStr)
        {
            if (string.IsNullOrEmpty(evalStr))
                return double.MinValue;

            // Handle mate scores - mate is always best
            if (evalStr.StartsWith("Mate in "))
            {
                string mateStr = evalStr.Replace("Mate in ", "").Trim();
                if (int.TryParse(mateStr, out int mateIn))
                {
                    // Positive mate = we have mate (best)
                    // Negative mate = opponent has mate (worst)
                    // Shorter mate is better: mate in 2 > mate in 5
                    if (mateIn > 0)
                        return 10000.0 - mateIn; // Positive = good for us, shorter is better
                    else
                        return -10000.0 - Math.Abs(mateIn); // Negative = bad for us
                }
            }

            // Handle centipawn scores
            if (double.TryParse(evalStr.Replace("+", ""), out double eval))
                return eval;

            return double.MinValue;
        }

        private void CleanupProcess()
        {
            try
            {
                State = EngineState.Uninitialized;

                if (engineInput != null)
                {
                    try { engineInput.Close(); } catch { }
                    engineInput = null;
                }

                if (engineOutput != null)
                {
                    try { engineOutput.Close(); } catch { }
                    engineOutput = null;
                }

                if (engineProcess != null)
                {
                    try
                    {
                        if (!engineProcess.HasExited)
                        {
                            engineProcess.Kill();
                        }
                        engineProcess.Dispose();
                    }
                    catch { }
                    engineProcess = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        public async Task RestartAsync()
        {
            Debug.WriteLine("RestartAsync called");

            CleanupProcess();

            // Wait a bit for OS to release resources
            await Task.Delay(100);

            if (!string.IsNullOrEmpty(lastEnginePath))
            {
                try
                {
                    await InitializeAsync(lastEnginePath);
                    Debug.WriteLine("Engine restarted successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to restart engine: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("Cannot restart engine: no engine path stored");
            }
        }

        public void Dispose()
        {
            CleanupProcess();
        }
    }
}