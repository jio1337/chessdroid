using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ChessDroid;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

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
