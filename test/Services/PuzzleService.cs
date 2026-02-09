using System.Diagnostics;
using System.Text.Json;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Manages puzzle loading, filtering, session state, and statistics.
    /// Loads puzzles from a CSV file in Lichess format (PuzzleId,FEN,Moves,Rating,Themes).
    /// </summary>
    public class PuzzleService
    {
        private List<Puzzle> _puzzles = new();
        private bool _loaded = false;
        private PuzzleStats _stats = new();
        private readonly Random _random = new();

        // Pre-built indices for fast filtering
        private Dictionary<string, List<int>> _themeIndex = new();
        private List<int> _sortedByRating = new(); // puzzle indices sorted by rating

        private static readonly string StatsFilePath = Path.Combine(
            Application.StartupPath, "puzzle_stats.json");

        /// <summary>
        /// Whether puzzles have been loaded.
        /// </summary>
        public bool IsLoaded => _loaded;

        /// <summary>
        /// Total number of loaded puzzles.
        /// </summary>
        public int PuzzleCount => _puzzles.Count;

        /// <summary>
        /// Loads puzzles from the CSV file asynchronously.
        /// CSV format: PuzzleId,FEN,Moves,Rating,Themes (with header row)
        /// </summary>
        public async Task<int> LoadPuzzlesAsync(string csvPath)
        {
            if (!File.Exists(csvPath))
                return 0;

            _puzzles.Clear();
            _themeIndex.Clear();
            _sortedByRating.Clear();

            await Task.Run(() =>
            {
                using var reader = new StreamReader(csvPath);
                string? line;
                bool isFirstLine = true;

                while ((line = reader.ReadLine()) != null)
                {
                    // Skip header row if present
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        if (line.StartsWith("PuzzleId"))
                            continue;
                    }

                    var puzzle = ParseCsvLine(line);
                    if (puzzle != null)
                        _puzzles.Add(puzzle);
                }
            });

            // Build indices
            BuildIndices();
            _loaded = true;

            // Load stats
            LoadStats();

            return _puzzles.Count;
        }

        /// <summary>
        /// Parses a single CSV line into a Puzzle object.
        /// </summary>
        private static Puzzle? ParseCsvLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // Split by comma — FEN contains spaces but no commas, so simple split works
            var parts = line.Split(',');
            if (parts.Length < 4)
                return null;

            if (!int.TryParse(parts[3], out int rating))
                return null;

            var moves = parts[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (moves.Length < 2) // Need at least setup move + one solution move
                return null;

            return new Puzzle
            {
                PuzzleId = parts[0],
                FEN = parts[1],
                Moves = moves,
                Rating = rating,
                Themes = parts.Length >= 5
                    ? parts[4].Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    : Array.Empty<string>()
            };
        }

        /// <summary>
        /// Builds theme and rating indices for fast filtering.
        /// </summary>
        private void BuildIndices()
        {
            _themeIndex.Clear();
            _sortedByRating.Clear();

            for (int i = 0; i < _puzzles.Count; i++)
            {
                var puzzle = _puzzles[i];

                // Theme index
                foreach (var theme in puzzle.Themes)
                {
                    if (!_themeIndex.ContainsKey(theme))
                        _themeIndex[theme] = new List<int>();
                    _themeIndex[theme].Add(i);
                }

                _sortedByRating.Add(i);
            }

            // Sort by rating for efficient range queries
            _sortedByRating.Sort((a, b) => _puzzles[a].Rating.CompareTo(_puzzles[b].Rating));
        }

        /// <summary>
        /// Gets sorted list of available themes.
        /// </summary>
        public List<string> GetAvailableThemes()
        {
            return _themeIndex.Keys.OrderBy(t => t).ToList();
        }

        /// <summary>
        /// Gets the next puzzle matching the given filters.
        /// Excludes already-attempted puzzles unless all matching ones have been attempted.
        /// </summary>
        public Puzzle? GetNextPuzzle(int minRating, int maxRating, string? theme = null, string? excludePuzzleId = null)
        {
            if (!_loaded || _puzzles.Count == 0)
                return null;

            // Build candidate list
            IEnumerable<int> candidates;

            if (!string.IsNullOrEmpty(theme) && theme != "All Themes" && _themeIndex.ContainsKey(theme))
            {
                // Filter by theme first (smaller set), then by rating
                candidates = _themeIndex[theme]
                    .Where(i => _puzzles[i].Rating >= minRating && _puzzles[i].Rating <= maxRating);
            }
            else
            {
                // Filter by rating only
                candidates = _sortedByRating
                    .Where(i => _puzzles[i].Rating >= minRating && _puzzles[i].Rating <= maxRating);
            }

            var candidateList = candidates.ToList();
            if (candidateList.Count == 0)
                return null;

            // Exclude all previously attempted puzzles (solved, failed, or skipped)
            var fresh = candidateList
                .Where(i => !_stats.AttemptedPuzzleIds.Contains(_puzzles[i].PuzzleId))
                .ToList();

            // If all have been attempted, allow repeats
            var pool = fresh.Count > 0 ? fresh : candidateList;

            // Avoid showing the same puzzle twice in a row
            if (!string.IsNullOrEmpty(excludePuzzleId) && pool.Count > 1)
            {
                pool = pool.Where(i => _puzzles[i].PuzzleId != excludePuzzleId).ToList();
            }

            // Random selection
            int idx = _random.Next(pool.Count);
            return _puzzles[pool[idx]];
        }

        /// <summary>
        /// Gets a specific puzzle by ID.
        /// </summary>
        public Puzzle? GetPuzzleById(string puzzleId)
        {
            return _puzzles.FirstOrDefault(p => p.PuzzleId == puzzleId);
        }

        // --- Stats Management ---

        /// <summary>
        /// Gets the current puzzle stats.
        /// </summary>
        public PuzzleStats GetStats() => _stats;

        /// <summary>
        /// Marks a puzzle as completed for skip-tracking purposes.
        /// Called when user finishes all moves (even with mistakes).
        /// Skipped/failed puzzles are NOT marked here — they can reappear.
        /// </summary>
        public void MarkPuzzleCompleted(string puzzleId)
        {
            _stats.SolvedPuzzleIds.Add(puzzleId);
        }

        /// <summary>
        /// Resets all puzzle statistics and deletes the stats file.
        /// </summary>
        public void ResetStats()
        {
            _stats = new PuzzleStats();
            try
            {
                if (File.Exists(StatsFilePath))
                    File.Delete(StatsFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting puzzle stats file: {ex.Message}");
            }
        }

        /// <summary>
        /// Records a puzzle attempt and updates all stats.
        /// </summary>
        public void RecordAttempt(string puzzleId, int puzzleRating, string[] themes, bool solved, int hintsUsed)
        {
            _stats.TotalAttempted++;
            _stats.HintsUsed += hintsUsed;
            _stats.AttemptedPuzzleIds.Add(puzzleId);

            if (solved)
            {
                _stats.TotalSolved++;
                _stats.SolvedPuzzleIds.Add(puzzleId);
                _stats.CurrentStreak++;
                if (_stats.CurrentStreak > _stats.BestStreak)
                    _stats.BestStreak = _stats.CurrentStreak;
            }
            else
            {
                _stats.TotalFailed++;
                _stats.CurrentStreak = 0;
            }

            // Update per-theme stats
            foreach (var theme in themes)
            {
                if (!_stats.ThemePerformance.ContainsKey(theme))
                    _stats.ThemePerformance[theme] = new ThemeStats();

                _stats.ThemePerformance[theme].Attempted++;
                if (solved)
                    _stats.ThemePerformance[theme].Solved++;
            }

            // Update estimated rating (Elo formula)
            UpdateEstimatedRating(puzzleRating, solved);

            // Save after each attempt
            SaveStats();
        }

        /// <summary>
        /// Updates the estimated puzzle rating using a simple Elo formula.
        /// </summary>
        private void UpdateEstimatedRating(int puzzleRating, bool solved)
        {
            const int K = 32;
            double expected = 1.0 / (1.0 + Math.Pow(10, (puzzleRating - _stats.EstimatedRating) / 400.0));
            double actual = solved ? 1.0 : 0.0;
            _stats.EstimatedRating = (int)Math.Round(_stats.EstimatedRating + K * (actual - expected));

            // Clamp to reasonable range
            _stats.EstimatedRating = Math.Clamp(_stats.EstimatedRating, 200, 3500);
        }

        /// <summary>
        /// Saves stats to puzzle_stats.json.
        /// </summary>
        public void SaveStats()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_stats, options);
                File.WriteAllText(StatsFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving puzzle stats: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads stats from puzzle_stats.json.
        /// </summary>
        public void LoadStats()
        {
            try
            {
                if (File.Exists(StatsFilePath))
                {
                    string json = File.ReadAllText(StatsFilePath);
                    _stats = JsonSerializer.Deserialize<PuzzleStats>(json) ?? new PuzzleStats();
                }
                else
                {
                    _stats = new PuzzleStats();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading puzzle stats: {ex.Message}");
                _stats = new PuzzleStats();
            }
        }

        /// <summary>
        /// Checks if a given UCI move results in checkmate when applied to the given position.
        /// Used for mate-in-1 acceptance (any mating move is correct on the final move).
        /// </summary>
        public static bool IsMoveCheckmate(string fen, string uciMove)
        {
            try
            {
                // Parse FEN
                var parts = fen.Split(' ');
                if (parts.Length < 4) return false;

                var board = ChessBoard.FromFEN(fen);
                bool whiteToMove = parts[1] == "w";
                string castling = parts[2];
                string enPassant = parts[3];

                // Apply the move
                ChessRulesService.ApplyUciMove(board, uciMove, ref castling, ref enPassant);

                // After the move, the OTHER side is to move
                bool sideToCheck = !whiteToMove; // The side that just got mated

                // Check if the king is in check
                var kingPos = board.GetKingPosition(sideToCheck);
                bool isInCheck = IsSquareAttacked(board, kingPos.row, kingPos.col, !sideToCheck);

                if (!isInCheck) return false; // Not even in check

                // Check if there are any legal moves for the side to move
                return !HasAnyLegalMove(board, sideToCheck, castling, enPassant);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a square is attacked by any piece of the given color.
        /// </summary>
        private static bool IsSquareAttacked(ChessBoard board, int targetRow, int targetCol, bool byWhite)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    char piece = board[row, col];
                    if (piece == '.') continue;
                    if (byWhite != char.IsUpper(piece)) continue;

                    if (ChessRulesService.CanReachSquare(board, row, col, piece, targetRow, targetCol))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the given side has any legal move (a move that doesn't leave their king in check).
        /// </summary>
        private static bool HasAnyLegalMove(ChessBoard board, bool isWhite, string castling, string enPassant)
        {
            for (int fromRow = 0; fromRow < 8; fromRow++)
            {
                for (int fromCol = 0; fromCol < 8; fromCol++)
                {
                    char piece = board[fromRow, fromCol];
                    if (piece == '.') continue;
                    if (isWhite != char.IsUpper(piece)) continue;

                    // Try all destination squares
                    for (int toRow = 0; toRow < 8; toRow++)
                    {
                        for (int toCol = 0; toCol < 8; toCol++)
                        {
                            if (fromRow == toRow && fromCol == toCol) continue;

                            // Check if piece can reach the square
                            char target = board[toRow, toCol];
                            if (target != '.' && char.IsUpper(target) == isWhite) continue; // Can't capture own piece

                            if (!ChessRulesService.CanReachSquareWithEnPassant(board, fromRow, fromCol, piece, toRow, toCol, enPassant))
                                continue;

                            // Try the move and check if king is still in check
                            string uciMove = $"{(char)('a' + fromCol)}{8 - fromRow}{(char)('a' + toCol)}{8 - toRow}";

                            // Clone board state for the test
                            var testBoard = ChessBoard.FromFEN(board.ToFEN());
                            string testCastling = castling;
                            string testEnPassant = enPassant;
                            ChessRulesService.ApplyUciMove(testBoard, uciMove, ref testCastling, ref testEnPassant);

                            // Check if our king is safe after the move
                            var kingPos = testBoard.GetKingPosition(isWhite);
                            if (!IsSquareAttacked(testBoard, kingPos.row, kingPos.col, !isWhite))
                                return true; // Found a legal move
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the path to the puzzles CSV file.
        /// </summary>
        public static string GetPuzzlesPath()
        {
            string[] possiblePaths = new[]
            {
                Path.Combine(Application.StartupPath, "Puzzles", "puzzles.csv"),
                Path.Combine(AppContext.BaseDirectory, "Puzzles", "puzzles.csv"),
                Path.Combine(Directory.GetCurrentDirectory(), "Puzzles", "puzzles.csv")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return possiblePaths[0]; // Default
        }
    }
}
