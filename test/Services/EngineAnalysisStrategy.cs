using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Manages engine analysis with degradation fallback strategy
    /// Extracted from MainForm to centralize engine analysis logic
    /// </summary>
    public class EngineAnalysisStrategy
    {
        private readonly ChessEngineService engineService;
        private readonly Action<string> updateStatus;

        public EngineAnalysisStrategy(ChessEngineService engineService, Action<string> updateStatus)
        {
            this.engineService = engineService ?? throw new ArgumentNullException(nameof(engineService));
            this.updateStatus = updateStatus ?? throw new ArgumentNullException(nameof(updateStatus));
        }

        /// <summary>
        /// Gets engine result with automatic depth degradation fallback
        /// Tries: requested depth → 75% depth → 50% depth (min 3)
        /// </summary>
        public async Task<(string bestMove, string evaluation, List<string> pvs, List<string> evaluations, WDLInfo? wdl)> GetResultWithDegradation(
            string fen, int depth, int multiPVCount)
        {
            try
            {
                Debug.WriteLine($"GetEngineResultWithDegradation - FEN: {fen}, Depth: {depth}, MultiPV: {multiPVCount}");

                // Try with requested depth first
                var result = await engineService.GetBestMoveAsync(fen, depth, multiPVCount);
                Debug.WriteLine($"Engine result - BestMove: '{result.bestMove}', Eval: '{result.evaluation}', PVs: {result.pvs?.Count ?? 0}");

                // If failed, try degraded modes
                if (string.IsNullOrEmpty(result.bestMove) && depth > 5)
                {
                    Debug.WriteLine($"Analysis failed at depth {depth}, trying degraded mode...");
                    updateStatus("Retrying with lower depth...");

                    // Try with 75% depth
                    int degradedDepth = (int)(depth * 0.75);
                    result = await engineService.GetBestMoveAsync(fen, degradedDepth, Math.Min(multiPVCount, degradedDepth));

                    // Still failed? Try with 50% depth
                    if (string.IsNullOrEmpty(result.bestMove) && degradedDepth > 3)
                    {
                        degradedDepth = Math.Max(3, depth / 2);
                        Debug.WriteLine($"Still failed, trying depth {degradedDepth}...");
                        result = await engineService.GetBestMoveAsync(fen, degradedDepth, Math.Min(multiPVCount, degradedDepth));
                    }

                    if (!string.IsNullOrEmpty(result.bestMove))
                    {
                        Debug.WriteLine($"Success with degraded depth {degradedDepth}");
                        updateStatus($"Analysis at depth {degradedDepth}");
                    }
                }

                return (result.bestMove, result.evaluation, result.pvs ?? new List<string>(), result.evaluations ?? new List<string>(), result.wdl);
            }
            catch (Exception ex)
            {
                return await HandleEngineException(ex, fen, depth, multiPVCount);
            }
        }

        /// <summary>
        /// Handles engine exceptions with automatic restart for pipe/crash errors
        /// </summary>
        private async Task<(string, string, List<string>, List<string>, WDLInfo?)> HandleEngineException(
            Exception ex, string fen, int depth, int multiPVCount)
        {
            // Check if it's a pipe closure or engine crash
            if (ex.Message.Contains("pipe") || ex.Message.Contains("closed") || ex is System.IO.IOException)
            {
                Debug.WriteLine($"Engine pipe closed or crashed: {ex.Message}");
                updateStatus("Engine crashed, restarting...");

                // Restart engine immediately
                try
                {
                    await engineService.RestartAsync();
                    updateStatus("Engine restarted");

                    // Try one more time with the restarted engine at reduced depth
                    var result = await engineService.GetBestMoveAsync(fen, Math.Max(3, depth / 2), Math.Max(1, multiPVCount / 2));
                    if (!string.IsNullOrEmpty(result.bestMove))
                    {
                        Debug.WriteLine("Analysis succeeded after engine restart");
                        updateStatus("Ready");
                        return (result.bestMove, result.evaluation, result.pvs ?? new List<string>(), result.evaluations ?? new List<string>(), result.wdl);
                    }
                }
                catch (Exception restartEx)
                {
                    Debug.WriteLine($"Failed to restart engine: {restartEx.Message}");
                    updateStatus("Engine restart failed");
                }
            }
            else
            {
                Debug.WriteLine($"Engine error: {ex.Message}");
                updateStatus($"Engine error: {ex.Message}");
            }

            return ("", "", new List<string>(), new List<string>(), null);
        }
    }
}