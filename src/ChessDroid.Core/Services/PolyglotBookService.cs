using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChessDroid.Services
{
    /// <summary>
    /// Service for looking up positions in Polyglot opening books (.bin files).
    /// Supports loading multiple books and merging results.
    /// </summary>
    public class PolyglotBookService : IDisposable
    {
        private readonly List<PolyglotBookReader> _readers = new();
        private readonly List<string> _loadedBookPaths = new();
        private bool _disposed;

        /// <summary>
        /// Represents a book move with weight information.
        /// </summary>
        public class PolyglotMove
        {
            public string UciMove { get; set; } = "";
            public int Weight { get; set; }
            public double WeightPercent { get; set; }
            public int Priority => Weight;

            public override string ToString()
            {
                return $"{UciMove} (weight={Weight}, {WeightPercent:F1}%)";
            }
        }

        /// <summary>
        /// Whether any books are loaded.
        /// </summary>
        public bool IsLoaded => _readers.Count > 0;

        /// <summary>
        /// Number of books loaded.
        /// </summary>
        public int BookCount => _readers.Count;

        /// <summary>
        /// Total entries across all loaded books.
        /// </summary>
        public int TotalEntries => _readers.Sum(r => r.EntryCount);

        /// <summary>
        /// Loads a single Polyglot book file.
        /// </summary>
        public bool LoadBook(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext != ".bin")
                    return false;

                var reader = new PolyglotBookReader();
                if (reader.LoadFile(filePath))
                {
                    _readers.Add(reader);
                    _loadedBookPaths.Add(filePath);
                    return true;
                }

                reader.Dispose();
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Loads all .bin files from a folder.
        /// </summary>
        public int LoadBooksFromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return 0;

            var binFiles = Directory.GetFiles(folderPath, "*.bin");
            int loaded = 0;

            foreach (var file in binFiles)
            {
                if (LoadBook(file))
                    loaded++;
            }

            return loaded;
        }

        /// <summary>
        /// Gets all book moves for a position, merged from all loaded books.
        /// Same moves have their weights summed.
        /// </summary>
        public List<PolyglotMove> GetBookMovesForPosition(string fen)
        {
            if (_readers.Count == 0 || string.IsNullOrEmpty(fen))
                return new List<PolyglotMove>();

            try
            {
                ulong key = PolyglotZobrist.ComputeKey(fen);

                // Collect moves from all books, merging weights for same UCI move
                var moveWeights = new Dictionary<string, int>();

                foreach (var reader in _readers)
                {
                    var entries = reader.FindEntries(key);
                    foreach (var entry in entries)
                    {
                        string uci = entry.ToUciMove();
                        if (moveWeights.ContainsKey(uci))
                            moveWeights[uci] += entry.Weight;
                        else
                            moveWeights[uci] = entry.Weight;
                    }
                }

                if (moveWeights.Count == 0)
                    return new List<PolyglotMove>();

                int totalWeight = moveWeights.Values.Sum();

                var moves = moveWeights.Select(kv => new PolyglotMove
                {
                    UciMove = kv.Key,
                    Weight = kv.Value,
                    WeightPercent = totalWeight > 0 ? (double)kv.Value / totalWeight * 100 : 0
                })
                .OrderByDescending(m => m.Weight)
                .ToList();

                return moves;
            }
            catch
            {
                return new List<PolyglotMove>();
            }
        }

        /// <summary>
        /// Gets the best (highest weight) book move for a position.
        /// </summary>
        public PolyglotMove? GetBestBookMove(string fen)
        {
            var moves = GetBookMovesForPosition(fen);
            return moves.Count > 0 ? moves[0] : null;
        }

        /// <summary>
        /// Checks if a position is in any loaded book.
        /// </summary>
        public bool IsInBook(string fen)
        {
            if (_readers.Count == 0 || string.IsNullOrEmpty(fen))
                return false;

            ulong key = PolyglotZobrist.ComputeKey(fen);
            return _readers.Any(r => r.FindEntries(key).Count > 0);
        }

        /// <summary>
        /// Gets statistics about all loaded books.
        /// </summary>
        public (int books, int totalEntries, List<string> bookNames) GetStats()
        {
            var names = _loadedBookPaths.Select(p => Path.GetFileName(p)).ToList();
            return (_readers.Count, TotalEntries, names);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var reader in _readers)
                    reader.Dispose();
                _readers.Clear();
                _loadedBookPaths.Clear();
                _disposed = true;
            }
        }
    }
}
