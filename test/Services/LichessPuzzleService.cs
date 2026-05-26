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
        /// Picks a random CSV file, seeks to a random offset, and reads `count` consecutive puzzles.
        /// Falls back to the start of the file if it hits EOF before filling the batch.
        /// Returns a shuffled list.
        /// </summary>
        public static List<LichessPuzzle> GetRandomBatch(string folder, int count = 300)
        {
            var files = Directory.GetFiles(folder, "*.csv");
            if (files.Length == 0) return new();

            string file  = files[_rng.Next(files.Length)];
            long fileSize = new FileInfo(file).Length;

            // Seek to a random position leaving enough room to read ~count puzzles
            long margin   = count * 300L;
            long safeEnd  = Math.Max(0, fileSize - margin);
            long startPos = safeEnd > 0 ? (long)(_rng.NextDouble() * safeEnd) : 0;

            var results = new List<LichessPuzzle>(count);

            using (var fs = File.OpenRead(file))
            {
                fs.Seek(startPos, SeekOrigin.Begin);
                using var reader = new StreamReader(fs, Encoding.UTF8);

                if (startPos > 0) reader.ReadLine(); // discard partial line

                string? line;
                while (results.Count < count && (line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("PuzzleId")) continue;
                    var p = ParseLine(line);
                    if (p != null) results.Add(p);
                }
            }

            // Wrap around from top of file if we hit EOF before filling
            if (results.Count < count)
            {
                using var fs2     = File.OpenRead(file);
                using var reader2 = new StreamReader(fs2, Encoding.UTF8);
                reader2.ReadLine(); // skip header
                string? line;
                while (results.Count < count && (line = reader2.ReadLine()) != null)
                {
                    var p = ParseLine(line);
                    if (p != null) results.Add(p);
                }
            }

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
            if (moves.Length < 2) return null; // need at least trigger + one player move
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
