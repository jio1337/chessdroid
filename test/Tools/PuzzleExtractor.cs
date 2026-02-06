namespace ChessDroid.Tools
{
    /// <summary>
    /// Developer tool to filter the full Lichess puzzle CSV (~5.7M puzzles) down to a curated subset.
    ///
    /// Usage:
    ///   1. Download the full Lichess puzzle database from https://database.lichess.org/#puzzles
    ///   2. Decompress the .zst file with 7-Zip or similar (produces ~1.5 GB CSV)
    ///   3. Call PuzzleExtractor.Extract() with the path to the full CSV
    ///   4. Output is written to the Puzzles/ folder as puzzles.csv
    ///
    /// The full Lichess CSV has 10 columns (no header):
    ///   PuzzleId, FEN, Moves, Rating, RatingDeviation, Popularity, NbPlays, Themes, GameUrl, OpeningTags
    ///
    /// Output CSV has 5 columns (with header):
    ///   PuzzleId, FEN, Moves, Rating, Themes
    /// </summary>
    public static class PuzzleExtractor
    {
        private const int TargetPuzzleCount = 100_000;
        private const int MinPopularity = 50;
        private const int MinPlays = 100;
        private const int MaxRatingDeviation = 100;
        private const int RatingBandSize = 200;
        private const int MinRating = 400;
        private const int MaxRating = 3400;

        /// <summary>
        /// Extracts a filtered subset from the full Lichess puzzle CSV.
        /// </summary>
        /// <param name="inputCsvPath">Path to the decompressed full Lichess CSV (no header row)</param>
        /// <param name="outputCsvPath">Path to write the filtered CSV (with header)</param>
        /// <returns>Number of puzzles extracted</returns>
        public static int Extract(string inputCsvPath, string outputCsvPath)
        {
            if (!File.Exists(inputCsvPath))
                throw new FileNotFoundException("Input CSV not found", inputCsvPath);

            int numBands = (MaxRating - MinRating) / RatingBandSize;
            int puzzlesPerBand = TargetPuzzleCount / numBands;

            // Collect puzzles into rating bands using reservoir sampling
            var bands = new Dictionary<int, List<string>>();
            for (int r = MinRating; r < MaxRating; r += RatingBandSize)
                bands[r] = new List<string>();

            var random = new Random(42); // Fixed seed for reproducibility
            int totalRead = 0;
            int totalAccepted = 0;

            Console.WriteLine($"Reading {inputCsvPath}...");

            using (var reader = new StreamReader(inputCsvPath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    totalRead++;
                    if (totalRead % 500_000 == 0)
                        Console.WriteLine($"  Read {totalRead:N0} lines...");

                    // Parse the 10-column Lichess format
                    var parts = line.Split(',');
                    if (parts.Length < 8) continue;

                    if (!int.TryParse(parts[3], out int rating)) continue;
                    if (!int.TryParse(parts[4], out int ratingDev)) continue;
                    if (!int.TryParse(parts[5], out int popularity)) continue;
                    if (!int.TryParse(parts[6], out int nbPlays)) continue;

                    // Quality filters
                    if (popularity < MinPopularity) continue;
                    if (nbPlays < MinPlays) continue;
                    if (ratingDev > MaxRatingDeviation) continue;

                    // Validate moves (need at least 2)
                    var moves = parts[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (moves.Length < 2) continue;

                    // Determine rating band
                    int bandKey = Math.Clamp(rating / RatingBandSize * RatingBandSize, MinRating, MaxRating - RatingBandSize);

                    if (!bands.ContainsKey(bandKey)) continue;

                    var band = bands[bandKey];

                    // Output format: PuzzleId,FEN,Moves,Rating,Themes
                    string outputLine = $"{parts[0]},{parts[1]},{parts[2]},{parts[3]},{(parts.Length >= 8 ? parts[7] : "")}";

                    // Reservoir sampling to get uniform distribution per band
                    if (band.Count < puzzlesPerBand)
                    {
                        band.Add(outputLine);
                    }
                    else
                    {
                        int j = random.Next(totalAccepted + 1);
                        if (j < puzzlesPerBand)
                            band[j] = outputLine;
                    }

                    totalAccepted++;
                }
            }

            Console.WriteLine($"Read {totalRead:N0} total lines, {totalAccepted:N0} passed quality filters");

            // Collect all puzzles and shuffle
            var allPuzzles = new List<string>();
            foreach (var kvp in bands)
            {
                Console.WriteLine($"  Rating {kvp.Key}-{kvp.Key + RatingBandSize}: {kvp.Value.Count} puzzles");
                allPuzzles.AddRange(kvp.Value);
            }

            // Shuffle
            for (int i = allPuzzles.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (allPuzzles[i], allPuzzles[j]) = (allPuzzles[j], allPuzzles[i]);
            }

            // Write output
            string? outputDir = Path.GetDirectoryName(outputCsvPath);
            if (outputDir != null && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            using (var writer = new StreamWriter(outputCsvPath))
            {
                writer.WriteLine("PuzzleId,FEN,Moves,Rating,Themes");
                foreach (var puzzle in allPuzzles)
                    writer.WriteLine(puzzle);
            }

            Console.WriteLine($"Wrote {allPuzzles.Count:N0} puzzles to {outputCsvPath}");
            return allPuzzles.Count;
        }
    }
}
