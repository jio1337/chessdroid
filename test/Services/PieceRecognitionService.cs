using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Handles piece recognition using template matching with caching
    /// Extracted from MainForm to separate piece detection concerns
    /// </summary>
    public class PieceRecognitionService
    {
        private const int TEMPLATE_SIZE = 64;
        private const int CACHE_VALIDITY_SECONDS = 5;
        private const int OLD_CACHE_THRESHOLD_SECONDS = 10;

        private readonly Dictionary<string, Mat> templates = new Dictionary<string, Mat>();
        private readonly Dictionary<string, Mat> templateMasks = new Dictionary<string, Mat>();
        private readonly ConcurrentDictionary<string, CellMatchCache> cellMatchCache = new ConcurrentDictionary<string, CellMatchCache>();

        private int cacheHits = 0;
        private int cacheMisses = 0;

        private class CellMatchCache
        {
            public string CellHash { get; set; } = "";
            public string DetectedPiece { get; set; } = "";
            public double Confidence { get; set; }
            public DateTime CachedAt { get; set; }
        }

        /// <summary>
        /// Loads piece templates and masks from the specified site folder
        /// </summary>
        public void LoadTemplatesAndMasks(string selectedSite, AppConfig config)
        {
            try
            {
                templates.Clear();
                templateMasks.Clear();

                string templatesPath = Path.Combine(config.GetTemplatesPath(), selectedSite);

                // Verify templates directory exists
                if (!Directory.Exists(templatesPath))
                {
                    throw new Exception($"Templates folder not found at: {templatesPath}\nPlease ensure the Templates/{selectedSite} folder is in the application directory.");
                }

                string[] pieces = { "wK", "wQ", "wR", "wB", "wN", "wP",
                                    "bK", "bQ", "bR", "bB", "bN", "bP" };

                foreach (string piece in pieces)
                {
                    string filePath = Path.Combine(templatesPath, piece + ".png");

                    if (!File.Exists(filePath))
                    {
                        throw new Exception($"Template file not found: {filePath}");
                    }

                    using (Mat templateColor = CvInvoke.Imread(filePath, ImreadModes.Unchanged))
                    {
                        if (templateColor.IsEmpty)
                            throw new Exception($"Failed to load template image: {filePath}");

                        Mat templateGray = new Mat();

                        // If template has alpha channel, use it for mask
                        if (templateColor.NumberOfChannels == 4)
                        {
                            // Extract alpha channel as mask
                            Mat[] channels = templateColor.Split();
                            Mat alphaMask = channels[3]; // Alpha channel

                            // Convert BGR to grayscale (ignoring alpha)
                            CvInvoke.CvtColor(templateColor, templateGray, ColorConversion.Bgra2Gray);

                            // Resize both template and mask to TEMPLATE_SIZE
                            Mat templateGrayResized = new Mat();
                            CvInvoke.Resize(templateGray, templateGrayResized, new Size(TEMPLATE_SIZE, TEMPLATE_SIZE));
                            Mat alphaMaskResized = new Mat();
                            CvInvoke.Resize(alphaMask, alphaMaskResized, new Size(TEMPLATE_SIZE, TEMPLATE_SIZE));

                            templates[piece] = templateGrayResized;
                            templateMasks[piece] = alphaMaskResized;

                            // Dispose other channels
                            channels[0].Dispose();
                            channels[1].Dispose();
                            channels[2].Dispose();
                            templateGray.Dispose();
                            alphaMask.Dispose();
                        }
                        else
                        {
                            // No alpha channel, convert to grayscale and create simple mask
                            CvInvoke.CvtColor(templateColor, templateGray, ColorConversion.Bgr2Gray);
                            Mat templateGrayResized = new Mat();
                            CvInvoke.Resize(templateGray, templateGrayResized, new Size(TEMPLATE_SIZE, TEMPLATE_SIZE));
                            Mat mask = new Mat();
                            CvInvoke.Threshold(templateGrayResized, mask, 10, 255, ThresholdType.Binary);

                            templates[piece] = templateGrayResized;
                            templateMasks[piece] = mask;
                            templateGray.Dispose();
                        }
                    }
                }

                Debug.WriteLine($"Loaded {templates.Count} templates from {templatesPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading templates and masks:\n\n{ex.Message}\n\nApplication Path: {Application.StartupPath}",
                    "Template Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Detects piece in a cell image using template matching with caching
        /// Returns (piece, confidence) tuple
        /// </summary>
        public (string piece, double confidence) DetectPieceAndConfidence(Mat celda, double matchThreshold)
        {
            string cellHash = ComputeCellHash(celda);
            string cacheKey = cellHash;

            // Try cache first
            if (cellMatchCache.TryGetValue(cacheKey, out var cached))
            {
                if ((DateTime.Now - cached.CachedAt).TotalSeconds < CACHE_VALIDITY_SECONDS)
                {
                    cacheHits++;
                    return (cached.DetectedPiece, cached.Confidence);
                }
            }

            cacheMisses++;
            string mejorCoincidencia = "";
            double mejorValor = 0.0;

            // Resize cell to template size
            Mat celdaResized = new Mat();
            CvInvoke.Resize(celda, celdaResized, new Size(TEMPLATE_SIZE, TEMPLATE_SIZE));

            foreach (var kvp in templates)
            {
                string key = kvp.Key;
                Mat templ = kvp.Value;
                Mat mask = templateMasks[key];

                // Try matching without mask first for better black piece detection
                double valorSinMask = 0.0;
                using (Mat resultado = new Mat())
                {
                    CvInvoke.MatchTemplate(celdaResized, templ, resultado, TemplateMatchingType.CcoeffNormed);
                    double[] minVals, maxVals;
                    Point[] minLoc, maxLoc;
                    resultado.MinMax(out minVals, out maxVals, out minLoc, out maxLoc);
                    valorSinMask = maxVals[0];
                }

                // Also try with mask if available
                double valorConMask = 0.0;
                if (mask != null && !mask.IsEmpty)
                {
                    using (Mat resultado = new Mat())
                    {
                        CvInvoke.MatchTemplate(celdaResized, templ, resultado, TemplateMatchingType.CcoeffNormed, mask);
                        double[] minVals, maxVals;
                        Point[] minLoc, maxLoc;
                        resultado.MinMax(out minVals, out maxVals, out minLoc, out maxLoc);
                        valorConMask = maxVals[0];
                    }
                }

                double valor = Math.Max(valorSinMask, valorConMask);

                if (valor > mejorValor && valor >= matchThreshold)
                {
                    mejorValor = valor;
                    if (!string.IsNullOrEmpty(key) && key.Length > 1)
                    {
                        char pieceChar = key[1];
                        if (key[0] == 'b')
                            pieceChar = char.ToLower(pieceChar);
                        mejorCoincidencia = pieceChar.ToString();
                    }
                    else
                    {
                        mejorCoincidencia = key;
                    }
                }
            }

            celdaResized.Dispose();

            // Cache result
            cellMatchCache[cacheKey] = new CellMatchCache
            {
                CellHash = cellHash,
                DetectedPiece = mejorCoincidencia,
                Confidence = mejorValor,
                CachedAt = DateTime.Now
            };

            return (mejorCoincidencia, mejorValor);
        }

        /// <summary>
        /// Computes a simple hash of a cell based on mean and standard deviation
        /// </summary>
        private static string ComputeCellHash(Mat celda)
        {
            MCvScalar mean = new MCvScalar();
            MCvScalar stdDev = new MCvScalar();
            CvInvoke.MeanStdDev(celda, ref mean, ref stdDev);
            return $"{mean.V0:F2}_{stdDev.V0:F2}";
        }

        /// <summary>
        /// Clears old cache entries to prevent memory buildup
        /// </summary>
        public void ClearOldCacheEntries()
        {
            var now = DateTime.Now;
            var oldEntries = cellMatchCache
                .Where(kvp => kvp.Value != null && (now - kvp.Value.CachedAt).TotalSeconds > OLD_CACHE_THRESHOLD_SECONDS)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldEntries)
            {
                cellMatchCache.TryRemove(key, out _);
            }

            // Periodically log cache statistics
            if ((cacheHits + cacheMisses) % 100 == 0 && (cacheHits + cacheMisses) > 0)
            {
                double hitRate = (double)cacheHits / (cacheHits + cacheMisses) * 100;
                Debug.WriteLine($"Template matching cache: {hitRate:F1}% hit rate ({cacheHits} hits, {cacheMisses} misses)");
            }
        }

        /// <summary>
        /// Clears all cache entries
        /// </summary>
        public void ClearAllCache()
        {
            cellMatchCache.Clear();
            cacheHits = 0;
            cacheMisses = 0;
        }

        /// <summary>
        /// Extracts chess board from Mat image and recognizes all pieces
        /// </summary>
        public Models.ChessBoard ExtractBoardFromMat(Mat boardMat, bool blackAtBottom, double matchThreshold)
        {
            const int BOARD_SIZE = 8;

            // Cleanup old cache entries periodically
            ClearOldCacheEntries();

            var swTotal = Stopwatch.StartNew();
            using (Mat grayBoard = new Mat())
            {
                CvInvoke.CvtColor(boardMat, grayBoard, ColorConversion.Bgr2Gray);

                int boardSize = grayBoard.Width;
                int cellSize = boardSize / BOARD_SIZE;
                char[,] board = new char[BOARD_SIZE, BOARD_SIZE];

                // Dynamic board sizing - accept any board size
                Debug.WriteLine($"Board detected at native size: {boardSize}x{boardSize} pixels (cell size: {cellSize}px)");

                System.Threading.Tasks.Parallel.For(0, BOARD_SIZE * BOARD_SIZE, idx =>
                {
                    int row = idx / BOARD_SIZE;
                    int col = idx % BOARD_SIZE;
                    var swCell = Stopwatch.StartNew();
                    Rectangle roi = new Rectangle(col * cellSize, row * cellSize, cellSize, cellSize);
                    using (Mat cell = new Mat(grayBoard, roi))
                    {
                        if (blackAtBottom)
                        {
                            CvInvoke.Flip(cell, cell, FlipType.Vertical);
                            CvInvoke.Flip(cell, cell, FlipType.Horizontal);
                        }

                        (string detectedPiece, double confidence) = DetectPieceAndConfidence(cell, matchThreshold);

                        // Debug logging for each cell - commented out for cleaner logs
                        // string square = $"{(char)('a' + col)}{8 - row}";
                        // if (!string.IsNullOrEmpty(detectedPiece))
                        // {
                        //     Debug.WriteLine($"[{square}] Detected: {detectedPiece} (confidence: {confidence:F3})");
                        // }
                        // else if (confidence > 0.3) // Log close misses
                        // {
                        //     Debug.WriteLine($"[{square}] Empty but had match with confidence: {confidence:F3}");
                        // }

                        board[row, col] = string.IsNullOrEmpty(detectedPiece) || detectedPiece.Length == 0 ? '.' : detectedPiece[0];
                    }
                    swCell.Stop();
                    // Commented out for cleaner debug logs
                    // if (swCell.ElapsedMilliseconds > 10)
                    //     Debug.WriteLine($"[PERF] Cell ({row},{col}) extraction+match: {swCell.ElapsedMilliseconds}ms");
                });
                swTotal.Stop();
                // Commented out for cleaner debug logs
                // Debug.WriteLine($"[PERF] ExtractBoardFromMat TOTAL: {swTotal.ElapsedMilliseconds}ms");
                return new Models.ChessBoard(board);
            }
        }

        /// <summary>
        /// Disposes all loaded templates and masks
        /// </summary>
        public void Dispose()
        {
            foreach (var template in templates.Values)
            {
                template?.Dispose();
            }
            foreach (var mask in templateMasks.Values)
            {
                mask?.Dispose();
            }
            templates.Clear();
            templateMasks.Clear();
            cellMatchCache.Clear();
        }
    }
}