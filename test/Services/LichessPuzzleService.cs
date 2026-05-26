using System.Text;

namespace ChessDroid.Services
{
    public record LichessPuzzle(
        string   Id,
        string   Fen,
        string[] Moves,
        int      Rating,
        string[] Themes,
        string   OpeningTags
    );

    public static class LichessPuzzleService
    {
        private static readonly Random _rng = new();

        public static bool HasPuzzles(string folder) =>
            Directory.Exists(folder) && Directory.GetFiles(folder, "*.csv").Length > 0;

        public static int CountFiles(string folder) =>
            Directory.Exists(folder) ? Directory.GetFiles(folder, "*.csv").Length : 0;

        /// <summary>
        /// Picks a random CSV file, seeks to a random offset, and reads puzzles.
        /// When a themeFilter is supplied, over-samples to compensate for sparsity.
        /// Returns a shuffled list of up to `count` puzzles.
        /// </summary>
        public static List<LichessPuzzle> GetRandomBatch(string folder, int count = 300, string? themeFilter = null)
        {
            var files = Directory.GetFiles(folder, "*.csv");
            if (files.Length == 0) return new();

            string file    = files[_rng.Next(files.Length)];
            long fileSize  = new FileInfo(file).Length;
            int  readCount = themeFilter != null ? count * 10 : count;

            long margin   = readCount * 300L;
            long safeEnd  = Math.Max(0, fileSize - margin);
            long startPos = safeEnd > 0 ? (long)(_rng.NextDouble() * safeEnd) : 0;

            var candidates = new List<LichessPuzzle>(readCount);

            using (var fs = File.OpenRead(file))
            {
                fs.Seek(startPos, SeekOrigin.Begin);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                if (startPos > 0) reader.ReadLine(); // discard partial line
                string? line;
                while (candidates.Count < readCount && (line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("PuzzleId")) continue;
                    var p = ParseLine(line);
                    if (p != null) candidates.Add(p);
                }
            }

            // Wrap around from top of file if still under target
            if (candidates.Count < readCount)
            {
                using var fs2     = File.OpenRead(file);
                using var reader2 = new StreamReader(fs2, Encoding.UTF8);
                reader2.ReadLine();
                string? line;
                while (candidates.Count < readCount && (line = reader2.ReadLine()) != null)
                {
                    var p = ParseLine(line);
                    if (p != null) candidates.Add(p);
                }
            }

            // Apply theme filter
            var results = themeFilter != null
                ? candidates.Where(p => p.Themes.Contains(themeFilter)).Take(count).ToList()
                : candidates;

            // Fisher-Yates shuffle
            for (int i = results.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (results[i], results[j]) = (results[j], results[i]);
            }

            return results;
        }

        private static LichessPuzzle? ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            // PuzzleId,FEN,Moves,Rating,RatingDeviation,Popularity,NbPlays,Themes,GameUrl,OpeningTags
            var p = line.Split(',');
            if (p.Length < 9) return null;
            if (!int.TryParse(p[3], out int rating)) return null;
            var moves = p[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (moves.Length < 2) return null;
            return new LichessPuzzle(
                p[0],
                p[1],
                moves,
                rating,
                p[7].Split(' ', StringSplitOptions.RemoveEmptyEntries),
                p.Length > 9 ? p[9] : ""
            );
        }
    }
}
