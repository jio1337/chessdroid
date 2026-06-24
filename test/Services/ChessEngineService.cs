using System.Diagnostics;
using ChessDroid.Models;

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

    public class ChessEngineService : IDisposable
    {
        private Process? engineProcess;
        private StreamWriter? engineInput;
        private StreamReader? engineOutput;
        private readonly AppConfig config;
        private string? lastEnginePath;
        private EngineState state = EngineState.Uninitialized;
        private readonly object stateLock = new object();
        private readonly SemaphoreSlim _analysisSemaphore = new SemaphoreSlim(1, 1);


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

        /// True if the engine advertised UCI_LimitStrength during init (supports Elo targeting).
        public bool SupportsEloTargeting { get; private set; }

        /// <summary>
        /// Get the name of the currently loaded engine (derived from file name)
        /// </summary>
        public string EngineName
        {
            get
            {
                if (string.IsNullOrEmpty(lastEnginePath))
                    return "Unknown Engine";
                return Path.GetFileNameWithoutExtension(lastEnginePath);
            }
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
                        string? line = await engineOutput!.ReadLineAsync(cts.Token);
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
                // Resolve engine path - check Engines folder if path doesn't exist directly
                string resolvedPath = enginePath;
                if (!File.Exists(resolvedPath))
                {
                    // Try in Engines subfolder
                    string enginesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Engines");
                    string engineInFolder = Path.Combine(enginesFolder, Path.GetFileName(enginePath));
                    if (File.Exists(engineInFolder))
                    {
                        resolvedPath = engineInFolder;
                        Debug.WriteLine($"Engine resolved to Engines folder: {resolvedPath}");
                    }
                    else
                    {
                        Debug.WriteLine($"Engine not found at: {enginePath} or {engineInFolder}");
                    }
                }

                // Store path for restart capability
                lastEnginePath = resolvedPath;
                State = EngineState.Starting;

                // Clean up any existing process
                CleanupProcess();

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = resolvedPath,
                    // Without this, the child process inherits chessdroid's own CWD
                    // (e.g. wherever `dotnet run` was invoked from), not the engine's
                    // folder. FromZero loads its NNUE weights via a relative path
                    // ("fromzero.bin") -- with the wrong CWD that lookup silently
                    // fails, leaving the engine evaluating every position as a flat
                    // 0 (uninitialized weights) instead of erroring loudly.
                    WorkingDirectory = Path.GetDirectoryName(resolvedPath) ?? AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
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

                // Wait for 'uciok' with timeout — collect option lines to detect capabilities
                bool receivedUciOk = false;
                SupportsEloTargeting = false;
                using (var cts = new CancellationTokenSource(5000))
                {
                    while (!receivedUciOk && IsEngineAlive())
                    {
                        try
                        {
                            string? line = await engineOutput.ReadLineAsync(cts.Token);
                            if (line == null) continue;
                            if (line.Contains("UCI_LimitStrength"))
                                SupportsEloTargeting = true;
                            if (line.StartsWith("uciok"))
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

                // Step 1.6: Configure Syzygy tablebases if path is set and exists
                if (!string.IsNullOrWhiteSpace(config?.SyzygyPath) && Directory.Exists(config.SyzygyPath))
                {
                    await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} SyzygyPath value {config.SyzygyPath}");
                    Debug.WriteLine($"Engine: set SyzygyPath to {config.SyzygyPath}");
                }

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
                            string? line = await engineOutput.ReadLineAsync(cts2.Token);
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
            string fen, int depth, int multiPV, bool preserveHashTables = true, CancellationToken ct = default)
        {
            string bestMove = "";
            string evaluation = "";
            var pvs = new List<string>();
            var evaluations = new List<string>();
            WDLInfo? wdlInfo = null;
            int retryCount = 0;

            bool whiteToMove = FenIsWhiteToMove(fen);

            // Tell any running analysis to stop so the semaphore holder exits fast
            if (State == EngineState.Analyzing && IsEngineAlive())
                await SafeWriteLineAsync("stop");

            // Serialize: only one GetBestMoveAsync at a time
            try { await _analysisSemaphore.WaitAsync(ct); }
            catch (OperationCanceledException) { return (bestMove, evaluation, pvs, evaluations, wdlInfo); }

            try
            {
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

                    // Single thread → fully deterministic depth-based results
                    await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} Threads value 1");

                    // Only send ucinewgame if we want to clear hash tables (e.g., new game)
                    // Preserving hash tables allows engine to reuse previous calculations
                    if (!preserveHashTables)
                    {
                        if (!await SafeWriteLineAsync(UCI_CMD_UCINEWGAME))
                        {
                            throw new IOException("Failed to send ucinewgame");
                        }
                    }

                    // Set position (sanitize FEN so unknown piece chars don't offset engine columns)
                    if (!await SafeWriteLineAsync($"{UCI_CMD_POSITION} {ChessBoard.SanitizeFenForEngine(fen)}"))
                    {
                        throw new IOException("Failed to set position");
                    }

                    // Update state to analyzing
                    State = EngineState.Analyzing;

                    // Always depth-based: deterministic, reproducible results at exact depth.
                    string goCommand = $"{UCI_CMD_GO_DEPTH} {depth}";
                    int effectiveTimeout = config.EngineResponseTimeoutMs;

                    if (!await SafeWriteLineAsync(goCommand))
                    {
                        throw new IOException("Failed to start analysis");
                    }

                    // Read analysis results
                    using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
                    {
                        while (IsEngineAlive())
                        {
                            string? line = await engineOutput!.ReadLineAsync(linkedCts.Token);

                            if (line == null) continue;

                            // Parse info lines containing PV
                            if (line.StartsWith(UCI_RESPONSE_INFO) && line.Contains(UCI_TOKEN_PV))
                            {
                                var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
                                            double displayCp = whiteToMove ? cp : -cp;
                                            evalStr = (displayCp / 100.0 >= 0 ? "+" : "") + (displayCp / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                                        }
                                        else if (tokens[i + 1] == UCI_TOKEN_MATE && int.TryParse(tokens[i + 2], out int mateIn))
                                        {
                                            int displayMate = whiteToMove ? mateIn : -mateIn;
                                            if (displayMate > 0)
                                                evalStr = $"Mate in +{displayMate}";
                                            else if (displayMate < 0)
                                                evalStr = $"Mate in {displayMate}";
                                            else
                                                evalStr = "Mate in 0";
                                        }
                                    }

                                    // Parse WDL (Win/Draw/Loss) data - format: "wdl W D L" (per mille values)
                                    if (tokens[i] == UCI_TOKEN_WDL && i + 3 < tokens.Length)
                                    {
                                        if (int.TryParse(tokens[i + 1], out int w) &&
                                            int.TryParse(tokens[i + 2], out int d) &&
                                            int.TryParse(tokens[i + 3], out int l))
                                        {
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
                                int _bm1 = line.IndexOf(' ');
                                if (_bm1 >= 0)
                                {
                                    int _bm2 = line.IndexOf(' ', _bm1 + 1);
                                    bestMove = _bm2 >= 0 ? line.Substring(_bm1 + 1, _bm2 - _bm1 - 1) : line.Substring(_bm1 + 1);
                                    if (bestMove == "(none)" || bestMove == "0000") bestMove = "";
                                }
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(bestMove))
                    {
                        State = EngineState.Ready;
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Debug.WriteLine("[Analysis] Cancelled by caller — draining engine");
                        if (State == EngineState.Analyzing)
                        {
                            await StopAndDrainAsync();
                            State = EngineState.Ready;
                        }
                        break;
                    }
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
            } // end try
            finally { _analysisSemaphore.Release(); }

            // Trim to requested count using GetRange (faster than LINQ Take.ToList)
            if (pvs.Count > multiPV)
            {
                pvs = pvs.GetRange(0, multiPV);
                evaluations = evaluations.GetRange(0, multiPV);
            }

            // Sort PV lines by evaluation (best to worst for current player)
            // This ensures consistent ordering even if engine sends updates out of order
            if (pvs.Count > 1 && evaluations.Count == pvs.Count)
            {
                var combined = pvs.Zip(evaluations, (pv, eval) => new { Pv = pv, Eval = eval }).ToList();

                // Sort by evaluation based on whose turn it is
                // Evaluations are in White's perspective (UCI standard):
                // - Positive = good for White
                // - Negative = good for Black
                combined.Sort((a, b) =>
                {
                    double evalA = ParseEvaluationForSorting(a.Eval);
                    double evalB = ParseEvaluationForSorting(b.Eval);

                    if (whiteToMove)
                    {
                        // White: higher eval is better (+3.0 > +1.0 > -1.0)
                        return evalB.CompareTo(evalA); // Descending
                    }
                    else
                    {
                        // Black: lower eval is better (-9.0 > -5.0 > +4.0)
                        return evalA.CompareTo(evalB); // Ascending
                    }
                });

                pvs = combined.Select(x => x.Pv).ToList();
                evaluations = combined.Select(x => x.Eval).ToList();

                // Update bestMove and evaluation to match the sorted best line
                if (pvs.Count > 0 && !string.IsNullOrEmpty(pvs[0]))
                {
                    // Extract the first move from the best PV line
                    int _pvSp = pvs[0].IndexOf(' ');
                    string firstMove = _pvSp >= 0 ? pvs[0].Substring(0, _pvSp) : pvs[0];
                    if (!string.IsNullOrEmpty(firstMove))
                    {
                        bestMove = firstMove;
                    }
                }
                if (evaluations.Count > 0)
                    evaluation = evaluations[0];

                // WDL was captured for the engine's raw multipv 1, but after sorting
                // the best move may have changed. Recalculate WDL from sorted best eval.
                // This ensures WDL matches the displayed best move.
                wdlInfo = null; // Clear old WDL, will be recalculated below
            }

            // Estimate WDL from the (sorted) best evaluation
            // This ensures WDL always matches the displayed best move
            if (!string.IsNullOrEmpty(evaluation) && !evaluation.StartsWith("Mate"))
            {
                // Parse centipawns from evaluation string (e.g., "+1.50" or "-0.75")
                if (double.TryParse(evaluation.Replace("+", ""), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double evalPawns))
                {
                    double centipawns = evalPawns * 100;
                    wdlInfo = WDLUtilities.EstimateWDLFromCentipawns(centipawns);
                    Debug.WriteLine($"WDL estimated from sorted best eval {evaluation}: {wdlInfo}");
                }
            }

            return (bestMove, evaluation, pvs, evaluations, wdlInfo);
        }

        private static bool FenIsWhiteToMove(string fen)
        {
            int sp = fen.IndexOf(' ');
            return sp >= 0 && sp + 1 < fen.Length && fen[sp + 1] == 'w';
        }

        private string? ParseEvalFromInfoLine(string line, bool whiteToMove)
        {
            int scp = line.IndexOf(" score cp ", StringComparison.Ordinal);
            if (scp >= 0)
            {
                int vs = scp + 10, ve = line.IndexOf(' ', scp + 10);
                if (double.TryParse(ve >= 0 ? line.AsSpan(vs, ve - vs) : line.AsSpan(vs), out double cp))
                {
                    double dcp = whiteToMove ? cp : -cp;
                    return (dcp / 100.0 >= 0 ? "+" : "") + (dcp / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            else
            {
                int smt = line.IndexOf(" score mate ", StringComparison.Ordinal);
                if (smt >= 0)
                {
                    int vs = smt + 12, ve = line.IndexOf(' ', smt + 12);
                    if (int.TryParse(ve >= 0 ? line.AsSpan(vs, ve - vs) : line.AsSpan(vs), out int mateIn))
                    {
                        int dm = whiteToMove ? mateIn : -mateIn;
                        return dm > 0 ? $"Mate in +{dm}" : dm < 0 ? $"Mate in {dm}" : "Mate in 0";
                    }
                }
            }
            return null;
        }

        private async Task StopAndDrainAsync(int timeoutMs = 300)
        {
            if (!IsEngineAlive()) return;
            await SafeWriteLineAsync("stop");
            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);
                while (IsEngineAlive())
                {
                    if ((await engineOutput!.ReadLineAsync(cts.Token))?.StartsWith(UCI_RESPONSE_BESTMOVE) == true)
                        break;
                }
            }
            catch { }
        }

        /// <summary>
        /// Parse evaluation string for sorting purposes.
        /// All evaluations are now in WHITE's perspective (converted at parse time).
        /// Returns a numeric value where: higher = better for White, lower = better for Black.
        /// </summary>
        private static double ParseEvaluationForSorting(string evalStr)
        {
            if (string.IsNullOrEmpty(evalStr))
                return double.MinValue;

            // Handle mate scores - format is "Mate in +X" or "Mate in -X"
            // All mate scores are now in WHITE's perspective:
            // - Mate in +X: White delivers mate (or Black gets mated) = good for White
            // - Mate in -X: Black delivers mate (or White gets mated) = good for Black
            if (evalStr.StartsWith("Mate in "))
            {
                string mateStr = evalStr.Replace("Mate in ", "").Trim();
                // Parse with + or - sign
                if (int.TryParse(mateStr, out int mateIn))
                {
                    // Sorting logic (White's perspective):
                    // - White sorts DESCENDING: highest first = best for White
                    // - Black sorts ASCENDING: lowest first = best for Black (most negative)
                    //
                    // Mate in +X (good for White): return huge positive
                    // - White descending: comes FIRST ✓
                    // - Black ascending: comes LAST ✓
                    //
                    // Mate in -X (good for Black): return huge negative
                    // - White descending: comes LAST ✓
                    // - Black ascending: comes FIRST ✓
                    if (mateIn > 0)
                    {
                        // White delivers mate - huge positive, shorter mate is better
                        return 100000.0 - mateIn;
                    }
                    else if (mateIn < 0)
                    {
                        // Black delivers mate - huge negative, shorter mate is better
                        // Use + so shorter mate (smaller abs value) gives lower result for ascending sort
                        // Mate in -3: -100000 + 3 = -99997 (comes first in ascending)
                        // Mate in -9: -100000 + 9 = -99991 (comes later in ascending)
                        return -100000.0 + Math.Abs(mateIn);
                    }
                    else
                    {
                        return 100000.0; // Mate in 0 (edge case)
                    }
                }
            }

            // Handle centipawn scores
            // Use InvariantCulture to parse period as decimal separator
            if (double.TryParse(evalStr.Replace("+", ""), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double eval))
                return eval;

            return double.MinValue;
        }

        /// <summary>
        /// Sends "go infinite" and streams live analysis updates as the engine searches deeper.
        /// onUpdate(bestMove, eval, pvs, evals, wdl, depth) is called each time a full depth
        /// completes. Cancelling ct sends "stop" and drains the bestmove response cleanly.
        /// </summary>
        public async Task RunContinuousAnalysisAsync(
            string fen,
            int multiPV,
            Action<string, string, List<string>, List<string>, WDLInfo?, int> onUpdate,
            CancellationToken ct,
            bool preserveHashTables = true)
        {
            if (State == EngineState.Analyzing && IsEngineAlive())
                await SafeWriteLineAsync("stop");

            try { await _analysisSemaphore.WaitAsync(ct); }
            catch (OperationCanceledException) { return; }

            try
            {
                if (!IsEngineAlive())
                {
                    await RestartAsync();
                    if (!IsEngineAlive()) return;
                }
                if (!await SyncWithEngineAsync()) return;

                await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} MultiPV value {multiPV}");

                // Restore all available threads for continuous analysis (go infinite benefits from parallelism)
                int threadCount = Math.Max(1, Environment.ProcessorCount);
                await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} Threads value {threadCount}");

                if (!preserveHashTables)
                    await SafeWriteLineAsync(UCI_CMD_UCINEWGAME);

                await SafeWriteLineAsync($"{UCI_CMD_POSITION} {ChessBoard.SanitizeFenForEngine(fen)}");

                bool whiteToMove = FenIsWhiteToMove(fen);

                State = EngineState.Analyzing;
                await SafeWriteLineAsync("go infinite");

                int    currentDepth    = 0;
                int    lastFiredDepth  = 0;
                int    highestMpvSeen  = 0;
                var    pvBuffer        = new string[multiPV];
                var    evalBuffer      = new string[multiPV];
                string bestMove        = "";
                string bestEval        = "";
                WDLInfo? wdl           = null;

                void FireUpdate(int depth)
                {
                    if (string.IsNullOrEmpty(bestMove) || depth == lastFiredDepth) return;
                    lastFiredDepth = depth;
                    var pvs   = pvBuffer.Take(Math.Max(1, highestMpvSeen)).ToList();
                    var evals = evalBuffer.Take(Math.Max(1, highestMpvSeen)).ToList();
                    onUpdate(bestMove, bestEval, pvs, evals, wdl, depth);
                }

                var pvMoves = new List<string>(64);
                bool completedNaturally = false;
                try
                {
                    while (IsEngineAlive())
                    {
                        string? line = await engineOutput!.ReadLineAsync(ct);
                        if (line == null) continue;
                        if (line.StartsWith(UCI_RESPONSE_BESTMOVE)) { completedNaturally = true; break; }
                        if (!line.StartsWith(UCI_RESPONSE_INFO) || !line.Contains(UCI_TOKEN_PV)) continue;

                        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        int    depth    = currentDepth;
                        int    mpv      = 1;
                        string lineEval = "";
                        pvMoves.Clear();
                        WDLInfo? lineWdl = null;

                        for (int i = 0; i < tokens.Length; i++)
                        {
                            if (tokens[i] == "depth"        && i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out int d))  depth = d;
                            if (tokens[i] == UCI_TOKEN_MULTIPV && i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out int m)) mpv   = m;

                            if (tokens[i] == UCI_TOKEN_SCORE && i + 2 < tokens.Length)
                            {
                                if (tokens[i + 1] == UCI_TOKEN_CP && double.TryParse(tokens[i + 2], out double cp))
                                {
                                    double dcp = whiteToMove ? cp : -cp;
                                    lineEval = (dcp / 100.0 >= 0 ? "+" : "") + (dcp / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                else if (tokens[i + 1] == UCI_TOKEN_MATE && i + 2 < tokens.Length && int.TryParse(tokens[i + 2], out int mate))
                                {
                                    int dm = whiteToMove ? mate : -mate;
                                    lineEval = dm > 0 ? $"Mate in +{dm}" : dm < 0 ? $"Mate in {dm}" : "Mate in 0";
                                }
                            }

                            if (tokens[i] == UCI_TOKEN_WDL && i + 3 < tokens.Length && mpv == 1 &&
                                int.TryParse(tokens[i + 1], out int w) &&
                                int.TryParse(tokens[i + 2], out int dr) &&
                                int.TryParse(tokens[i + 3], out int l))
                                lineWdl = new WDLInfo(w, dr, l);

                            if (tokens[i] == UCI_TOKEN_PV)
                            {
                                for (int j = i + 1; j < tokens.Length; j++) pvMoves.Add(tokens[j]);
                                break;
                            }
                        }

                        // New depth: flush completed previous depth, reset buffers
                        if (depth > currentDepth)
                        {
                            if (currentDepth > 0) FireUpdate(currentDepth);
                            currentDepth   = depth;
                            Array.Clear(pvBuffer,   0, multiPV);
                            Array.Clear(evalBuffer, 0, multiPV);
                            highestMpvSeen = 0;
                        }

                        int idx = mpv - 1;
                        if (idx >= 0 && idx < multiPV)
                        {
                            pvBuffer[idx]   = string.Join(" ", pvMoves);
                            evalBuffer[idx] = lineEval;
                            if (mpv == 1)
                            {
                                bestEval = lineEval;
                                bestMove = pvMoves.Count > 0 ? pvMoves[0] : bestMove;
                                // Stockfish WDL is from the side-to-move's perspective.
                                // Convert to White's perspective so DisplayWDLInfo works correctly.
                                if (lineWdl != null)
                                    wdl = whiteToMove ? lineWdl : new WDLInfo(lineWdl.Loss, lineWdl.Draw, lineWdl.Win);
                            }
                            highestMpvSeen = Math.Max(highestMpvSeen, mpv);
                        }

                        // All multiPV lines for this depth received — push live update
                        if (highestMpvSeen >= multiPV) FireUpdate(currentDepth);
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    // Flush final depth on stop
                    if (currentDepth > 0) FireUpdate(currentDepth);

                    // Only stop+drain when cancelled — if bestmove was already consumed
                    // naturally, sending stop to an idle engine gets no response and the
                    // drain timer runs to completion, stalling the next analysis.
                    if (!completedNaturally)
                        await StopAndDrainAsync();
                    State = EngineState.Ready;
                }
            }
            finally { _analysisSemaphore.Release(); }
        }

        /// <summary>
        /// Streamlined move request for engine-vs-engine matches.
        /// Unlike GetBestMoveAsync, does NOT send ucinewgame (preserves hash tables),
        /// uses MultiPV=1, and accepts an explicit go command string.
        /// Returns the best move and evaluation (in White's perspective).
        /// </summary>
        public async Task<(string? move, string? eval)> GetMoveForMatchAsync(string fen, string goCommand, int timeoutMs, CancellationToken ct)
        {
            if (!IsEngineAlive() || State != EngineState.Ready)
                return (null, null);

            bool whiteToMove = FenIsWhiteToMove(fen);

            try
            {
                // Sync with engine
                if (!await SyncWithEngineAsync())
                    return (null, null);

                // Set MultiPV to 1
                await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} MultiPV value 1");

                // Set position (sanitize FEN so unknown piece chars don't offset engine columns)
                string sanitizedFen = ChessBoard.SanitizeFenForEngine(fen);
                Debug.WriteLine($"[Match:{EngineName}] send: position fen {sanitizedFen}");
                await SafeWriteLineAsync($"{UCI_CMD_POSITION} {sanitizedFen}");

                // Start search
                State = EngineState.Analyzing;
                await SafeWriteLineAsync(goCommand);

                // Read until "bestmove", capturing last evaluation from info lines
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeoutMs);

                string? lastEval = null;

                while (IsEngineAlive())
                {
                    string? line = await engineOutput!.ReadLineAsync(cts.Token);
                    if (line == null) continue;
                    Debug.WriteLine($"[Match:{EngineName}] recv: {line}");

                    if (line.StartsWith(UCI_RESPONSE_INFO) && line.Contains(UCI_TOKEN_SCORE))
                    {
                        string? parsed = ParseEvalFromInfoLine(line, whiteToMove);
                        if (parsed != null) lastEval = parsed;
                    }
                    else if (line.StartsWith(UCI_RESPONSE_BESTMOVE))
                    {
                        State = EngineState.Ready;
                        int _bm1 = line.IndexOf(' ');
                        int _bm2 = _bm1 >= 0 ? line.IndexOf(' ', _bm1 + 1) : -1;
                        string move = _bm1 >= 0 ? (_bm2 >= 0 ? line.Substring(_bm1 + 1, _bm2 - _bm1 - 1) : line.Substring(_bm1 + 1)) : "";
                        if (string.IsNullOrEmpty(move) || move == "(none)" || move == "0000")
                            return (null, lastEval);
                        return (move, lastEval);
                    }
                }

                State = EngineState.Error;
                return (null, null);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // User stopped the match — propagate so the caller records UserStopped, not stalemate
                State = EngineState.Error;
                throw;
            }
            catch (OperationCanceledException)
            {
                // Internal timeout — return null so caller treats it as no legal move
                Debug.WriteLine("GetMoveForMatchAsync: Timed out");
                State = EngineState.Error;
                return (null, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetMoveForMatchAsync: Error - {ex.Message}");
                State = EngineState.Error;
                return (null, null);
            }
        }

        /// <summary>
        /// Evaluates a position to a given depth and returns the eval in White's perspective.
        /// Used for neutral annotation during engine matches (annotator engine only — never called on playing engines).
        /// </summary>
        public async Task<string?> GetPositionEvalAsync(string fen, int depth, CancellationToken ct)
        {
            if (!IsEngineAlive() || State != EngineState.Ready)
                return null;

            bool whiteToMove = FenIsWhiteToMove(fen);
            string? lastEval = null;

            try
            {
                if (!await SyncWithEngineAsync())
                    return null;

                await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} MultiPV value 1");
                await SafeWriteLineAsync($"{UCI_CMD_POSITION} {ChessBoard.SanitizeFenForEngine(fen)}");

                State = EngineState.Analyzing;
                await SafeWriteLineAsync($"go depth {depth}");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(6000);

                while (IsEngineAlive())
                {
                    string? line = await engineOutput!.ReadLineAsync(cts.Token);
                    if (line == null) continue;

                    if (line.StartsWith(UCI_RESPONSE_INFO) && line.Contains(UCI_TOKEN_SCORE))
                    {
                        string? parsed = ParseEvalFromInfoLine(line, whiteToMove);
                        if (parsed != null) lastEval = parsed;
                    }
                    else if (line.StartsWith(UCI_RESPONSE_BESTMOVE))
                    {
                        State = EngineState.Ready;
                        return lastEval;
                    }
                }

                State = EngineState.Error;
                return null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                State = EngineState.Error;
                throw;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("GetPositionEvalAsync: Timed out");
                State = EngineState.Error;
                return lastEval;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetPositionEvalAsync: Error - {ex.Message}");
                State = EngineState.Error;
                return null;
            }
        }

        /// <summary>
        /// Evaluates a position using a fixed time budget (go movetime N).
        /// Preferred over depth-based evaluation for match annotation — always finishes
        /// within timeMsPerMove ms regardless of position complexity.
        /// </summary>
        public async Task<string?> GetPositionEvalTimedAsync(string fen, int timeMsPerMove, CancellationToken ct)
        {
            if (!IsEngineAlive() || State != EngineState.Ready)
                return null;

            bool whiteToMove = FenIsWhiteToMove(fen);
            string? lastEval = null;

            try
            {
                if (!await SyncWithEngineAsync())
                    return null;

                await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} MultiPV value 1");
                await SafeWriteLineAsync($"{UCI_CMD_POSITION} {ChessBoard.SanitizeFenForEngine(fen)}");

                State = EngineState.Analyzing;
                await SafeWriteLineAsync($"go movetime {timeMsPerMove}");

                // Hard timeout = movetime + generous buffer for engine overhead
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeMsPerMove + 2000);

                while (IsEngineAlive())
                {
                    string? line = await engineOutput!.ReadLineAsync(cts.Token);
                    if (line == null) continue;

                    if (line.StartsWith(UCI_RESPONSE_INFO) && line.Contains(UCI_TOKEN_SCORE))
                    {
                        string? parsed = ParseEvalFromInfoLine(line, whiteToMove);
                        if (parsed != null) lastEval = parsed;
                    }
                    else if (line.StartsWith(UCI_RESPONSE_BESTMOVE))
                    {
                        State = EngineState.Ready;
                        return lastEval;
                    }
                }

                State = EngineState.Error;
                return null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                State = EngineState.Error;
                throw;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("GetPositionEvalTimedAsync: Timed out");
                State = EngineState.Error;
                return lastEval;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetPositionEvalTimedAsync: Error - {ex.Message}");
                State = EngineState.Error;
                return null;
            }
        }

        /// <summary>
        /// Evaluates a position with continuous depth-based search, calling onEvalUpdate at each
        /// depth. Runs until maxDepth is reached or the CancellationToken is cancelled.
        /// On cancellation: sends stop, drains to bestmove, sets State=Ready, returns last eval.
        /// </summary>
        public async Task<string?> StreamPositionEvalAsync(string fen, int maxDepth, Action<string> onEvalUpdate, CancellationToken ct)
        {
            if (!IsEngineAlive() || State != EngineState.Ready)
                return null;

            try { await _analysisSemaphore.WaitAsync(ct); }
            catch (OperationCanceledException) { return null; }

            bool whiteToMove = FenIsWhiteToMove(fen);
            string? lastEval = null;

            try
            {
                if (!await SyncWithEngineAsync())
                    return null;

                await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} MultiPV value 1");
                await SafeWriteLineAsync($"{UCI_CMD_POSITION} {ChessBoard.SanitizeFenForEngine(fen)}");

                State = EngineState.Analyzing;
                await SafeWriteLineAsync($"go depth {maxDepth}");

                while (IsEngineAlive())
                {
                    string? line = await engineOutput!.ReadLineAsync(ct);
                    if (line == null) continue;

                    if (line.StartsWith(UCI_RESPONSE_INFO) && line.Contains(UCI_TOKEN_SCORE))
                    {
                        string? parsed = ParseEvalFromInfoLine(line, whiteToMove);
                        if (parsed != null) { lastEval = parsed; onEvalUpdate(lastEval); }
                    }
                    else if (line.StartsWith(UCI_RESPONSE_BESTMOVE))
                    {
                        State = EngineState.Ready;
                        return lastEval;
                    }
                }

                State = EngineState.Error;
                return lastEval;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (State == EngineState.Analyzing)
                    await StopAndDrainAsync();
                State = EngineState.Ready;
                return lastEval;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StreamPositionEvalAsync: Error - {ex.Message}");
                State = EngineState.Error;
                return null;
            }
            finally { _analysisSemaphore.Release(); }
        }

        public async Task SetChess960Async(bool enabled)
        {
            if (!IsEngineAlive() || State != EngineState.Ready) return;
            await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} UCI_Chess960 value {(enabled ? "true" : "false")}");
        }

        public async Task SetSkillLevelAsync(int level)
        {
            if (!IsEngineAlive() || State != EngineState.Ready) return;
            level = Math.Clamp(level, 0, 20);
            await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} Skill Level value {level}");
        }

        /// Sends UCI_LimitStrength + UCI_Elo for engines that support it (SF9+).
        /// Also sends Skill Level as fallback for older engines — unknown setoptions are silently ignored.
        public async Task SetEloTargetAsync(int elo, int skillLevelFallback)
        {
            if (!IsEngineAlive() || State != EngineState.Ready) return;
            elo = Math.Clamp(elo, 1320, 3190);
            await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} UCI_LimitStrength value true");
            await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} UCI_Elo value {elo}");
            await SafeWriteLineAsync($"{UCI_CMD_SETOPTION} Skill Level value {skillLevelFallback}");
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

            // Use lastEnginePath if available, otherwise build full path from config
            string? enginePath = lastEnginePath;
            if (string.IsNullOrEmpty(enginePath) && !string.IsNullOrEmpty(config?.SelectedEngine))
            {
                // Build full path: Engines folder + selected engine filename
                enginePath = Path.Combine(config.GetEnginesPath(), config.SelectedEngine);
            }

            if (!string.IsNullOrEmpty(enginePath))
            {
                try
                {
                    Debug.WriteLine($"Initializing engine from: {enginePath}");
                    await InitializeAsync(enginePath);
                    Debug.WriteLine("Engine started successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to start engine: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("Cannot start engine: no engine path in config or stored");
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CleanupProcess();
            _analysisSemaphore.Dispose();
        }
    }
}