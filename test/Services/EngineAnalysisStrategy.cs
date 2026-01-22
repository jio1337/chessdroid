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
        private readonly MoveSharpnessAnalyzer sharpnessAnalyzer;
        private readonly AppConfig config;

        // Minimum extra PVs to request for aggressiveness filtering
        private const int MIN_PVS_FOR_AGGRESSIVENESS = 3;

        public EngineAnalysisStrategy(ChessEngineService engineService, Action<string> updateStatus, AppConfig config)
        {
            this.engineService = engineService ?? throw new ArgumentNullException(nameof(engineService));
            this.updateStatus = updateStatus ?? throw new ArgumentNullException(nameof(updateStatus));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.sharpnessAnalyzer = new MoveSharpnessAnalyzer();
        }

        /// <summary>
        /// Gets engine result with automatic depth degradation fallback
        /// Tries: requested depth → 75% depth → 50% depth (min 3)
        /// Also applies aggressiveness-based move selection when not at extremes (0 or 100)
        /// </summary>
        public async Task<(string bestMove, string evaluation, List<string> pvs, List<string> evaluations, WDLInfo? wdl)> GetResultWithDegradation(
            string fen, int depth, int multiPVCount)
        {
            try
            {
                // Request extra PVs for aggressiveness filtering if not at balanced setting
                int aggressiveness = config.Aggressiveness;
                bool needsAggressivenessFiltering = aggressiveness != 50;
                int actualMultiPV = needsAggressivenessFiltering
                    ? Math.Max(multiPVCount, MIN_PVS_FOR_AGGRESSIVENESS)
                    : multiPVCount;

                Debug.WriteLine($"GetEngineResultWithDegradation - FEN: {fen}, Depth: {depth}, MultiPV: {actualMultiPV}, Aggressiveness: {aggressiveness}");

                // Try with requested depth first
                var result = await engineService.GetBestMoveAsync(fen, depth, actualMultiPV);
                Debug.WriteLine($"Engine result - BestMove: '{result.bestMove}', Eval: '{result.evaluation}', PVs: {result.pvs?.Count ?? 0}");

                // If failed, try degraded modes
                if (string.IsNullOrEmpty(result.bestMove) && depth > 5)
                {
                    Debug.WriteLine($"Analysis failed at depth {depth}, trying degraded mode...");
                    updateStatus("Retrying with lower depth...");

                    // Try with 75% depth
                    int degradedDepth = (int)(depth * 0.75);
                    result = await engineService.GetBestMoveAsync(fen, degradedDepth, Math.Min(actualMultiPV, degradedDepth));

                    // Still failed? Try with 50% depth
                    if (string.IsNullOrEmpty(result.bestMove) && degradedDepth > 3)
                    {
                        degradedDepth = Math.Max(3, depth / 2);
                        Debug.WriteLine($"Still failed, trying depth {degradedDepth}...");
                        result = await engineService.GetBestMoveAsync(fen, degradedDepth, Math.Min(actualMultiPV, degradedDepth));
                    }

                    if (!string.IsNullOrEmpty(result.bestMove))
                    {
                        Debug.WriteLine($"Success with degraded depth {degradedDepth}");
                        updateStatus($"Analysis at depth {degradedDepth}");
                    }
                }

                // Apply aggressiveness-based move selection
                if (needsAggressivenessFiltering && !string.IsNullOrEmpty(result.bestMove))
                {
                    result = ApplyAggressivenessFilter(fen, result, aggressiveness);
                }

                // Trim to requested count for display (use GetRange for efficiency)
                var trimmedPVs = result.pvs != null && result.pvs.Count > 0
                    ? result.pvs.GetRange(0, Math.Min(result.pvs.Count, multiPVCount))
                    : new List<string>();
                var trimmedEvals = result.evaluations != null && result.evaluations.Count > 0
                    ? result.evaluations.GetRange(0, Math.Min(result.evaluations.Count, multiPVCount))
                    : new List<string>();

                return (result.bestMove, result.evaluation, trimmedPVs, trimmedEvals, result.wdl);
            }
            catch (Exception ex)
            {
                return await HandleEngineException(ex, fen, depth, multiPVCount);
            }
        }

        /// <summary>
        /// Applies aggressiveness-based move filtering to select between candidate moves.
        /// May change the "best" move if a stylistically preferred move is within eval tolerance.
        /// </summary>
        private (string bestMove, string evaluation, List<string> pvs, List<string> evaluations, WDLInfo? wdl)
            ApplyAggressivenessFilter(
                string fen,
                (string bestMove, string evaluation, List<string> pvs, List<string> evaluations, WDLInfo? wdl) result,
                int aggressiveness)
        {
            var pvs = result.pvs ?? new List<string>();
            var evals = result.evaluations ?? new List<string>();

            if (pvs.Count < 2)
            {
                // Only one candidate - nothing to filter
                return result;
            }

            // Build candidate list with sharpness scores
            // NOTE: pvs and evals are already sorted by Multi-PV (best to worst for current player)
            // So candidates[0] is the actual best move, NOT result.bestMove which is the raw engine output
            var candidates = new List<(string move, string evaluation, string pvLine, int sharpness)>();

            for (int i = 0; i < pvs.Count && i < evals.Count; i++)
            {
                string pvLine = pvs[i];
                string eval = evals[i];

                if (string.IsNullOrEmpty(pvLine)) continue;

                // Extract first move from PV line
                string firstMove = pvLine.Split(' ')[0];
                if (string.IsNullOrEmpty(firstMove)) continue;

                // Calculate sharpness
                int sharpness = sharpnessAnalyzer.CalculateSharpness(firstMove, fen, eval, pvLine);

                candidates.Add((firstMove, eval, pvLine, sharpness));
            }

            if (candidates.Count < 2)
            {
                return result;
            }

            // Log candidate analysis
            Debug.WriteLine($"[Aggressiveness] Filtering {candidates.Count} candidates (aggressiveness={aggressiveness}):");
            foreach (var c in candidates)
            {
                Debug.WriteLine($"  {c.move}: eval={c.evaluation}, sharpness={c.sharpness}");
            }

            // Determine eval tolerance based on aggressiveness
            // More extreme settings = willing to sacrifice more eval for style
            double evalTolerance = aggressiveness switch
            {
                <= 10 or >= 90 => 0.40, // Very extreme: accept 0.40 pawn loss
                <= 25 or >= 75 => 0.30, // Moderate: accept 0.30 pawn loss
                _ => 0.20                // Near balanced: accept 0.20 pawn loss
            };

            // Select move based on aggressiveness
            int selectedIndex = sharpnessAnalyzer.SelectMoveByAggressiveness(candidates, aggressiveness, evalTolerance);

            if (selectedIndex <= 0)
            {
                // Keep sorted best move (candidates[0] is already the best from Multi-PV sorting)
                Debug.WriteLine($"[Aggressiveness] Keeping sorted best move: {candidates[0].move}");
                return result;
            }

            // Different move selected!
            var selected = candidates[selectedIndex];
            Debug.WriteLine($"[Aggressiveness] CHANGED from {result.bestMove} to {selected.move} (sharpness {candidates[0].sharpness} → {selected.sharpness})");

            // Reorder PVs and evals to put selected move first
            var newPvs = new List<string> { selected.pvLine };
            var newEvals = new List<string> { selected.evaluation };

            for (int i = 0; i < pvs.Count; i++)
            {
                if (i != selectedIndex && !string.IsNullOrEmpty(pvs[i]))
                {
                    newPvs.Add(pvs[i]);
                    if (i < evals.Count)
                        newEvals.Add(evals[i]);
                }
            }

            return (selected.move, selected.evaluation, newPvs, newEvals, result.wdl);
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