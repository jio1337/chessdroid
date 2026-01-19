using System.Diagnostics;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Service that integrates ABK opening book data into the application.
    /// Converts ABK move-tree format to position-based lookup using FEN keys.
    /// This allows book lookups from any position, regardless of move order (handles transpositions).
    /// </summary>
    public class AbkBookService
    {
        private readonly AbkBookReader _reader;
        private Dictionary<string, List<BookMove>> _positionBook = new();
        private bool _isLoaded;
        private string _loadedBookPath = "";
        private int _totalPositions;

        public AbkBookService()
        {
            _reader = new AbkBookReader();
        }

        /// <summary>
        /// Indicates whether an ABK book is currently loaded.
        /// </summary>
        public bool IsBookLoaded => _isLoaded;

        /// <summary>
        /// Gets the path of the currently loaded book.
        /// </summary>
        public string LoadedBookPath => _loadedBookPath;

        /// <summary>
        /// Gets the total number of unique positions in the loaded book.
        /// </summary>
        public int TotalPositions => _totalPositions;

        /// <summary>
        /// Loads an ABK opening book file and builds position-based lookup table.
        /// </summary>
        /// <param name="filePath">Path to the .abk file</param>
        /// <returns>True if loaded successfully</returns>
        public bool LoadBook(string filePath)
        {
            try
            {
                Debug.WriteLine($"[AbkBookService] LoadBook called with path: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    Debug.WriteLine($"[AbkBookService] File path is null or empty");
                    return false;
                }

                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"[AbkBookService] File not found: {filePath}");
                    return false;
                }

                var sw = Stopwatch.StartNew();

                Debug.WriteLine($"[AbkBookService] Loading ABK file...");
                if (!_reader.LoadFile(filePath))
                {
                    Debug.WriteLine($"[AbkBookService] Failed to load ABK file: {filePath}");
                    return false;
                }
                Debug.WriteLine($"[AbkBookService] ABK file loaded, building position book...");

                // Build position-based lookup from ABK tree
                _positionBook = BuildPositionBook();
                _totalPositions = _positionBook.Count;

                sw.Stop();
                Debug.WriteLine($"[AbkBookService] Loaded {filePath} in {sw.ElapsedMilliseconds}ms");
                Debug.WriteLine($"[AbkBookService] Built position book with {_totalPositions} unique positions");

                _isLoaded = true;
                _loadedBookPath = filePath;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AbkBookService] EXCEPTION in LoadBook: {ex.Message}");
                Debug.WriteLine($"[AbkBookService] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Builds a position-based lookup dictionary by traversing the ABK tree
        /// and simulating moves to get FEN positions.
        /// </summary>
        private Dictionary<string, List<BookMove>> BuildPositionBook()
        {
            var book = new Dictionary<string, List<BookMove>>();
            var startingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

            // Get root moves and add them for starting position
            var rootMoves = _reader.GetRootMoves();
            if (rootMoves.Count > 0)
            {
                book[startingFen] = ConvertToBookMoves(rootMoves);
            }

            // BFS traversal of the ABK tree
            var queue = new Queue<(List<string> moveHistory, string fen, string castling, string enPassant)>();
            var visited = new HashSet<string>();

            // Start from root moves
            foreach (var rootEntry in rootMoves)
            {
                string uciMove = rootEntry.ToUciMove();
                var moveList = new List<string> { uciMove };

                // Apply move to get new position
                var (newFen, newCastling, newEnPassant) = ApplyMoveToPosition(
                    startingFen, uciMove, "KQkq", "-");

                if (!string.IsNullOrEmpty(newFen))
                {
                    queue.Enqueue((moveList, newFen, newCastling, newEnPassant));
                }
            }

            // Process queue
            int processedCount = 0;
            while (queue.Count > 0 && processedCount < 100000) // Safety limit
            {
                var (moveHistory, currentFen, castling, enPassant) = queue.Dequeue();
                processedCount++;

                // Skip if already visited this position
                string posKey = currentFen;
                if (visited.Contains(posKey))
                    continue;
                visited.Add(posKey);

                // Get moves available from this position in the ABK tree
                var entries = _reader.GetMovesAfterSequence(moveHistory);
                if (entries.Count == 0)
                    continue;

                // Add to position book (merge if position already exists from different move order)
                if (!book.ContainsKey(currentFen))
                {
                    book[currentFen] = ConvertToBookMoves(entries);
                }
                else
                {
                    // Merge moves (transposition - same position reached different ways)
                    MergeBookMoves(book[currentFen], entries);
                }

                // Continue traversal for each child move
                foreach (var entry in entries)
                {
                    string uciMove = entry.ToUciMove();
                    var newMoveHistory = new List<string>(moveHistory) { uciMove };

                    var (newFen, newCastling, newEnPassant) = ApplyMoveToPosition(
                        currentFen, uciMove, castling, enPassant);

                    if (!string.IsNullOrEmpty(newFen) && !visited.Contains(newFen))
                    {
                        queue.Enqueue((newMoveHistory, newFen, newCastling, newEnPassant));
                    }
                }
            }

            Debug.WriteLine($"[AbkBookService] Processed {processedCount} positions during tree traversal");
            return book;
        }

        /// <summary>
        /// Applies a UCI move to a position and returns the new FEN.
        /// </summary>
        private (string fen, string castling, string enPassant) ApplyMoveToPosition(
            string currentFen, string uciMove, string castling, string enPassant)
        {
            try
            {
                // Create board from FEN
                var board = ChessBoard.FromFEN(currentFen + " w " + castling + " " + enPassant + " 0 1");

                // Apply the move
                ChessRulesService.ApplyUciMove(board, uciMove, ref castling, ref enPassant);

                // Get new FEN (piece placement only)
                string newFen = ChessNotationService.GenerateFENFromBoard(board);

                return (newFen, castling, enPassant);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AbkBookService] Error applying move {uciMove}: {ex.Message}");
                return ("", castling, enPassant);
            }
        }

        /// <summary>
        /// Converts ABK entries to BookMove objects.
        /// </summary>
        private List<BookMove> ConvertToBookMoves(List<AbkBookReader.AbkEntry> entries)
        {
            return entries
                .OrderByDescending(e => e.NGames)
                .Take(10)
                .Select(e => new BookMove
                {
                    UciMove = e.ToUciMove(),
                    Games = e.NGames,
                    Wins = e.NWon,
                    Losses = e.NLost,
                    Draws = e.NGames - e.NWon - e.NLost,
                    WinRate = e.WinRate,
                    Priority = e.Priority
                })
                .ToList();
        }

        /// <summary>
        /// Merges new entries into existing book moves (for transpositions).
        /// </summary>
        private void MergeBookMoves(List<BookMove> existing, List<AbkBookReader.AbkEntry> newEntries)
        {
            foreach (var entry in newEntries)
            {
                string uci = entry.ToUciMove();
                var existingMove = existing.FirstOrDefault(m => m.UciMove == uci);

                if (existingMove == null)
                {
                    existing.Add(new BookMove
                    {
                        UciMove = uci,
                        Games = entry.NGames,
                        Wins = entry.NWon,
                        Losses = entry.NLost,
                        Draws = entry.NGames - entry.NWon - entry.NLost,
                        WinRate = entry.WinRate,
                        Priority = entry.Priority
                    });
                }
                // If move already exists, keep the one with more games (more reliable stats)
                else if (entry.NGames > existingMove.Games)
                {
                    existingMove.Games = entry.NGames;
                    existingMove.Wins = entry.NWon;
                    existingMove.Losses = entry.NLost;
                    existingMove.Draws = entry.NGames - entry.NWon - entry.NLost;
                    existingMove.WinRate = entry.WinRate;
                }
            }

            // Re-sort by games after merge
            existing.Sort((a, b) => b.Games.CompareTo(a.Games));

            // Keep only top 10
            if (existing.Count > 10)
            {
                existing.RemoveRange(10, existing.Count - 10);
            }
        }

        /// <summary>
        /// Gets book moves for the current position using FEN lookup.
        /// Works regardless of how the position was reached (handles transpositions).
        /// </summary>
        /// <param name="fen">The current position FEN (full or piece placement only)</param>
        /// <returns>List of suggested moves with statistics</returns>
        public List<BookMove> GetBookMovesForPosition(string fen)
        {
            if (!_isLoaded || string.IsNullOrEmpty(fen))
                return new List<BookMove>();

            // Extract piece placement only (first part of FEN)
            string piecePlacement = fen.Split(' ')[0];

            if (_positionBook.TryGetValue(piecePlacement, out var moves))
            {
                return moves;
            }

            return new List<BookMove>();
        }

        /// <summary>
        /// Gets the best book move for the current position.
        /// </summary>
        /// <param name="fen">The current position FEN</param>
        /// <returns>Best book move or null if out of book</returns>
        public BookMove? GetBestBookMove(string fen)
        {
            var moves = GetBookMovesForPosition(fen);
            return moves.FirstOrDefault();
        }

        /// <summary>
        /// Checks if the current position is in the book.
        /// </summary>
        /// <param name="fen">The current position FEN</param>
        /// <returns>True if there are book moves available</returns>
        public bool IsInBook(string fen)
        {
            if (!_isLoaded || string.IsNullOrEmpty(fen))
                return false;

            string piecePlacement = fen.Split(' ')[0];
            return _positionBook.ContainsKey(piecePlacement);
        }

        /// <summary>
        /// Unloads the current book.
        /// </summary>
        public void UnloadBook()
        {
            _isLoaded = false;
            _loadedBookPath = "";
            _positionBook.Clear();
            _totalPositions = 0;
        }
    }

    /// <summary>
    /// Represents a book move with statistics.
    /// </summary>
    public class BookMove
    {
        public string UciMove { get; set; } = "";
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double WinRate { get; set; }
        public int Priority { get; set; }

        /// <summary>
        /// Returns draw rate as percentage.
        /// </summary>
        public double DrawRate => Games > 0 ? (double)Draws / Games * 100 : 0;

        /// <summary>
        /// Returns loss rate as percentage.
        /// </summary>
        public double LossRate => Games > 0 ? (double)Losses / Games * 100 : 0;

        public override string ToString()
        {
            return $"{UciMove} (W:{WinRate:F1}% D:{DrawRate:F1}% L:{LossRate:F1}%, {Games} games)";
        }
    }
}
