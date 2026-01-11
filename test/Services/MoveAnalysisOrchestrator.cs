using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Result cache for engine analysis
    /// </summary>
    public class EngineResultCache
    {
        public string PositionKey { get; set; } = "";
        public int Depth { get; set; }
        public string BestMove { get; set; } = "";
        public string Evaluation { get; set; } = "";
        public List<string> PVs { get; set; } = new();
        public List<string> Evaluations { get; set; } = new();
        public DateTime CachedAt { get; set; }
    }

    /// <summary>
    /// Orchestrates the complete move analysis workflow
    /// Extracted from MainForm to centralize move analysis logic
    /// </summary>
    public class MoveAnalysisOrchestrator
    {
        private readonly PositionStateManager positionStateManager;
        private readonly EngineAnalysisStrategy analysisStrategy;
        private readonly EngineRestartManager restartManager;
        private readonly AppConfig config;
        private readonly Action<string> updateStatus;
        private readonly Action clearConsole;
        private readonly Action<string> appendToConsole;

        private EngineResultCache? lastEngineResult;
        private const int CACHE_TTL_SECONDS = 2;

        public MoveAnalysisOrchestrator(
            PositionStateManager positionStateManager,
            EngineAnalysisStrategy analysisStrategy,
            EngineRestartManager restartManager,
            AppConfig config,
            Action<string> updateStatus,
            Action clearConsole,
            Action<string> appendToConsole)
        {
            this.positionStateManager = positionStateManager ?? throw new ArgumentNullException(nameof(positionStateManager));
            this.analysisStrategy = analysisStrategy ?? throw new ArgumentNullException(nameof(analysisStrategy));
            this.restartManager = restartManager ?? throw new ArgumentNullException(nameof(restartManager));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.updateStatus = updateStatus ?? throw new ArgumentNullException(nameof(updateStatus));
            this.clearConsole = clearConsole ?? throw new ArgumentNullException(nameof(clearConsole));
            this.appendToConsole = appendToConsole ?? throw new ArgumentNullException(nameof(appendToConsole));
        }

        /// <summary>
        /// Analyzes a position and returns engine results
        /// Handles caching, degradation, and failure recovery
        /// Returns null if analysis fails
        /// </summary>
        public async Task<(string bestMove, string evaluation, List<string> pvs, List<string> evaluations, string completeFen)?> AnalyzePosition(
            ChessBoard currentBoard, int depth, bool showSecondLine, bool showThirdLine)
        {
            var swTotal = Stopwatch.StartNew();

            // 1) Update position state
            var swUpdatePos = Stopwatch.StartNew();
            positionStateManager.UpdatePositionState(currentBoard);
            swUpdatePos.Stop();

            // 2) Generate complete FEN
            var swFen = Stopwatch.StartNew();
            string completeFen = positionStateManager.GenerateCompleteFEN(currentBoard);
            swFen.Stop();

            // 3) Determine MultiPV count
            int multiPVCount = showThirdLine ? 3 : showSecondLine ? 2 : 1;
            multiPVCount = Math.Min(multiPVCount, depth);

            // 4) Check cache
            bool useCache = IsCacheValid(completeFen, depth);

            // 5) Get engine result
            var swEngine = Stopwatch.StartNew();
            var result = useCache && lastEngineResult != null
                ? (lastEngineResult.BestMove, lastEngineResult.Evaluation, lastEngineResult.PVs, lastEngineResult.Evaluations)
                : await analysisStrategy.GetResultWithDegradation(completeFen, depth, multiPVCount);
            swEngine.Stop();

            // 6) Handle failure
            if (string.IsNullOrEmpty(result.Item1))
            {
                restartManager.RecordFailure();
                Debug.WriteLine($"ERROR: Analysis failed for this position");
                Debug.WriteLine($"Failed FEN: {completeFen}");

                // Clear cache so next attempt tries again
                lastEngineResult = null;

                // Display failure message
                var (failures, restarts) = restartManager.GetMetrics();
                clearConsole();
                appendToConsole($"Analysis failed - attempt {failures}{Environment.NewLine}");
                appendToConsole($"Position: {completeFen}{Environment.NewLine}");

                // Check if restart is needed
                if (restartManager.ShouldAttemptRestart())
                {
                    if (restartManager.ShouldRestartApplication())
                    {
                        // Signal application restart needed
                        updateStatus("Engine unstable - restarting application...");
                        appendToConsole($"Too many engine restarts ({restarts}). Application restart needed.{Environment.NewLine}");
                        return null; // Caller should handle app restart
                    }

                    updateStatus("Restarting engine...");
                    bool restartSuccess = await restartManager.RestartEngineAsync(null); // Engine service handled internally
                    var (_, restartCount) = restartManager.GetMetrics();

                    if (restartSuccess)
                    {
                        updateStatus("Engine restarted - try again");
                        appendToConsole($"Engine restarted successfully (restart #{restartCount}). Please try again.{Environment.NewLine}");
                    }
                    else
                    {
                        updateStatus("Engine restart failed");
                    }
                }
                else
                {
                    updateStatus($"Analysis failed ({failures}) - try again");
                }

                // Save state and return null
                positionStateManager.SaveMoveState(currentBoard);
                return null;
            }

            // 7) Success - record and cache
            restartManager.RecordSuccess();
            updateStatus("Ready");

            // Cache the result
            if (!string.IsNullOrEmpty(result.Item1))
            {
                lastEngineResult = new EngineResultCache
                {
                    PositionKey = completeFen,
                    Depth = depth,
                    BestMove = result.Item1,
                    Evaluation = result.Item2,
                    PVs = result.Item3,
                    Evaluations = result.Item4,
                    CachedAt = DateTime.Now
                };
            }

            // 8) Save state
            positionStateManager.SaveMoveState(currentBoard);

            swTotal.Stop();
            Debug.WriteLine($"[PERF] AnalyzePosition timings: UpdatePos={swUpdatePos.ElapsedMilliseconds}ms, FEN={swFen.ElapsedMilliseconds}ms, Engine={swEngine.ElapsedMilliseconds}ms, TOTAL={swTotal.ElapsedMilliseconds}ms");

            return (result.Item1, result.Item2, result.Item3, result.Item4, completeFen);
        }

        /// <summary>
        /// Checks if cached result is valid for the given position and depth
        /// </summary>
        private bool IsCacheValid(string fen, int depth)
        {
            return lastEngineResult != null &&
                   lastEngineResult.PositionKey == fen &&
                   lastEngineResult.Depth == depth &&
                   (DateTime.Now - lastEngineResult.CachedAt).TotalSeconds < CACHE_TTL_SECONDS;
        }

        /// <summary>
        /// Clears the engine result cache
        /// </summary>
        public void ClearCache()
        {
            lastEngineResult = null;
        }
    }
}
