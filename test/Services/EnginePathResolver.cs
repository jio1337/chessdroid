namespace ChessDroid.Services
{
    /// <summary>
    /// Handles engine path discovery and resolution
    /// </summary>
    public class EnginePathResolver
    {
        private readonly AppConfig config;

        public EnginePathResolver(AppConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Resolves the engine path from config or discovers available engines
        /// </summary>
        /// <returns>Tuple of (enginePath, wasAutoDiscovered)</returns>
        public (string enginePath, bool wasAutoDiscovered) ResolveEnginePath()
        {
            string selectedEngine;
            bool autoDiscovered = false;

            // Check if config already has a selected engine
            if (!string.IsNullOrEmpty(config?.SelectedEngine))
            {
                selectedEngine = config.SelectedEngine;
            }
            else
            {
                // Auto-discover first available engine
                selectedEngine = DiscoverFirstAvailableEngine();
                autoDiscovered = true;

                // Save the discovered engine to config
                if (config != null && !string.IsNullOrEmpty(selectedEngine))
                {
                    config.SelectedEngine = selectedEngine;
                    config.Save();
                }
            }

            string enginesPath = config?.GetEnginesPath() ?? Path.Combine(AppContext.BaseDirectory, "Engines");
            string enginePath = Path.Combine(enginesPath, selectedEngine);
            return (enginePath, autoDiscovered);
        }

        /// <summary>
        /// Discovers the first available engine in the Engines folder
        /// </summary>
        /// <returns>Engine filename or "stockfish.exe" as fallback</returns>
        private string DiscoverFirstAvailableEngine()
        {
            string enginesFolder = config.GetEnginesPath();

            if (Directory.Exists(enginesFolder))
            {
                string[] engineFiles = Directory.GetFiles(enginesFolder, "*.exe");
                if (engineFiles.Length > 0)
                {
                    return Path.GetFileName(engineFiles[0]);
                }
            }

            // Fallback to default engine name
            return "stockfish.exe";
        }

        /// <summary>
        /// Validates that the engine path exists
        /// </summary>
        public bool ValidateEnginePath(string enginePath)
        {
            return File.Exists(enginePath);
        }

        /// <summary>
        /// Gets all available engine executables in the Engines folder
        /// </summary>
        public string[] GetAvailableEngines()
        {
            string enginesFolder = config.GetEnginesPath();

            if (Directory.Exists(enginesFolder))
            {
                return Directory.GetFiles(enginesFolder, "*.exe")
                    .Select(Path.GetFileName)
                    .Where(name => name != null)
                    .Select(name => name!)
                    .ToArray();
            }

            return Array.Empty<string>();
        }
    }
}