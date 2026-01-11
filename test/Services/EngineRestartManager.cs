using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ChessDroid.Services
{
    /// <summary>
    /// Manages engine failure tracking and restart logic
    /// Extracted from MainForm to centralize engine failure recovery
    /// </summary>
    public class EngineRestartManager
    {
        private const int MAX_CONSECUTIVE_FAILURES = 2;
        private const int MAX_RESTARTS_PER_MINUTE = 3;

        private int consecutiveAnalysisFailures = 0;
        private int engineRestartCount = 0;
        private DateTime lastRestartTime = DateTime.MinValue;

        /// <summary>
        /// Records a successful analysis, resetting failure counter
        /// </summary>
        public void RecordSuccess()
        {
            consecutiveAnalysisFailures = 0;
        }

        /// <summary>
        /// Records an analysis failure, incrementing failure counter
        /// </summary>
        public void RecordFailure()
        {
            consecutiveAnalysisFailures++;
            Debug.WriteLine($"Analysis failure recorded. Total consecutive failures: {consecutiveAnalysisFailures}");
        }

        /// <summary>
        /// Checks if engine restart should be attempted based on consecutive failures
        /// </summary>
        public bool ShouldAttemptRestart()
        {
            return consecutiveAnalysisFailures >= MAX_CONSECUTIVE_FAILURES;
        }

        /// <summary>
        /// Updates restart tracking and determines if application should restart instead
        /// Returns true if application restart is needed, false if engine restart should proceed
        /// </summary>
        public bool ShouldRestartApplication()
        {
            var timeSinceLastRestart = DateTime.Now - lastRestartTime;
            if (timeSinceLastRestart.TotalMinutes < 1)
            {
                engineRestartCount++;
            }
            else
            {
                // Reset counter if more than 1 minute has passed
                engineRestartCount = 1;
            }

            lastRestartTime = DateTime.Now;

            bool tooManyRestarts = engineRestartCount > MAX_RESTARTS_PER_MINUTE;
            if (tooManyRestarts)
            {
                Debug.WriteLine($"Too many engine restarts ({engineRestartCount}), triggering application restart...");
            }

            return tooManyRestarts;
        }

        /// <summary>
        /// Performs engine restart with proper error handling
        /// Returns true if restart succeeded, false otherwise
        /// </summary>
        public async Task<bool> RestartEngineAsync(ChessEngineService? engineService)
        {
            Debug.WriteLine($"Restarting engine (restart #{engineRestartCount})...");

            try
            {
                if (engineService == null)
                {
                    Debug.WriteLine("Engine service is null, cannot restart");
                    throw new InvalidOperationException("Engine service not initialized");
                }

                await engineService.RestartAsync();

                // Wait for engine to fully initialize
                await Task.Delay(1000);

                consecutiveAnalysisFailures = 0;
                Debug.WriteLine("Engine restarted successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Engine restart failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets current restart metrics for logging/display
        /// </summary>
        public (int failures, int restarts) GetMetrics()
        {
            return (consecutiveAnalysisFailures, engineRestartCount);
        }

        /// <summary>
        /// Resets all failure and restart tracking
        /// </summary>
        public void Reset()
        {
            consecutiveAnalysisFailures = 0;
            engineRestartCount = 0;
            lastRestartTime = DateTime.MinValue;
        }
    }
}
