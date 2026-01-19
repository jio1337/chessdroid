using System.Diagnostics;
using System.Text;

namespace ChessDroid.Services
{
    /// <summary>
    /// Reads Arena Chess GUI opening book files (.abk format).
    ///
    /// ABK Format (from chessprogramming.org/ABK):
    /// - Entry size: 28 bytes each
    /// - Initial position index: 900 (offset 25200 bytes)
    /// - Tree structure with nextMove (child) and nextSibling (alternative) pointers
    ///
    /// Each entry contains:
    /// - from/to squares (0-63)
    /// - promotion piece
    /// - priority
    /// - game statistics (ngames, nwon, nlost, plycount)
    /// - tree pointers (nextMove, nextSibling)
    /// </summary>
    public class AbkBookReader
    {
        private const int ENTRY_SIZE = 28;
        private const int INITIAL_POSITION_INDEX = 900;
        private const int HEADER_SIZE = INITIAL_POSITION_INDEX * ENTRY_SIZE; // 25200 bytes

        /// <summary>
        /// Represents a single move entry from the ABK book.
        /// </summary>
        public class AbkEntry
        {
            public int From { get; set; }           // Source square (0-63)
            public int To { get; set; }             // Destination square (0-63)
            public int Promotion { get; set; }      // 0=none, ±1=rook, ±2=knight, ±3=bishop, ±4=queen
            public int Priority { get; set; }       // Move priority/weight
            public int NGames { get; set; }         // Number of games
            public int NWon { get; set; }           // Games won
            public int NLost { get; set; }          // Games lost
            public int PlyCount { get; set; }       // Halfmove count
            public int NextMove { get; set; }       // Index to next move in variation (-1 if none)
            public int NextSibling { get; set; }    // Index to alternative move (-1 if none)

            /// <summary>
            /// Converts square index (0-63) to algebraic notation (a1-h8).
            /// </summary>
            public static string SquareToAlgebraic(int square)
            {
                if (square < 0 || square > 63) return "??";
                int file = square % 8;  // 0-7 = a-h
                int rank = square / 8;  // 0-7 = 1-8
                return $"{(char)('a' + file)}{rank + 1}";
            }

            /// <summary>
            /// Returns the move in UCI format (e.g., "e2e4", "e7e8q").
            /// </summary>
            public string ToUciMove()
            {
                string move = $"{SquareToAlgebraic(From)}{SquareToAlgebraic(To)}";

                if (Promotion != 0)
                {
                    char promoChar = Math.Abs(Promotion) switch
                    {
                        1 => 'r',
                        2 => 'n',
                        3 => 'b',
                        4 => 'q',
                        _ => '?'
                    };
                    move += promoChar;
                }

                return move;
            }

            /// <summary>
            /// Calculates win rate as percentage (0-100).
            /// </summary>
            public double WinRate => NGames > 0 ? (double)NWon / NGames * 100 : 50.0;

            /// <summary>
            /// Calculates draw rate (games neither won nor lost).
            /// </summary>
            public double DrawRate => NGames > 0 ? (double)(NGames - NWon - NLost) / NGames * 100 : 0.0;

            public override string ToString()
            {
                return $"{ToUciMove()} (games={NGames}, win={WinRate:F1}%, priority={Priority})";
            }
        }

        /// <summary>
        /// Represents a complete opening line with move sequence and statistics.
        /// </summary>
        public class OpeningLine
        {
            public List<string> Moves { get; set; } = new();
            public int TotalGames { get; set; }
            public double WinRate { get; set; }
            public int Depth { get; set; }

            /// <summary>
            /// Returns moves as space-separated UCI string.
            /// </summary>
            public string MovesString => string.Join(" ", Moves);

            public override string ToString()
            {
                return $"{MovesString} (depth={Depth}, games={TotalGames}, win={WinRate:F1}%)";
            }
        }

        private byte[]? _data;
        private int _entryCount;

        /// <summary>
        /// Loads an ABK file into memory.
        /// </summary>
        public bool LoadFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"[ABK] File not found: {filePath}");
                    return false;
                }

                _data = File.ReadAllBytes(filePath);

                // Validate file size
                if (_data.Length < HEADER_SIZE + ENTRY_SIZE)
                {
                    Debug.WriteLine($"[ABK] File too small: {_data.Length} bytes");
                    return false;
                }

                // Validate magic header
                if (_data[0] != 0x03 || _data[1] != 'A' || _data[2] != 'B' || _data[3] != 'K')
                {
                    Debug.WriteLine("[ABK] Invalid file header (not ABK format)");
                    return false;
                }

                _entryCount = (_data.Length - HEADER_SIZE) / ENTRY_SIZE;
                Debug.WriteLine($"[ABK] Loaded {filePath}: {_entryCount} entries, {_data.Length} bytes");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ABK] Error loading file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads a single entry from the book at the given index.
        /// </summary>
        private AbkEntry? ReadEntry(int index)
        {
            if (_data == null) return null;

            // Convert logical index to actual index (add INITIAL_POSITION_INDEX offset for root)
            int offset = index * ENTRY_SIZE;

            if (offset < 0 || offset + ENTRY_SIZE > _data.Length)
            {
                return null;
            }

            try
            {
                return new AbkEntry
                {
                    From = (sbyte)_data[offset],
                    To = (sbyte)_data[offset + 1],
                    Promotion = (sbyte)_data[offset + 2],
                    Priority = (sbyte)_data[offset + 3],
                    NGames = BitConverter.ToInt32(_data, offset + 4),
                    NWon = BitConverter.ToInt32(_data, offset + 8),
                    NLost = BitConverter.ToInt32(_data, offset + 12),
                    PlyCount = BitConverter.ToInt32(_data, offset + 16),
                    NextMove = BitConverter.ToInt32(_data, offset + 20),
                    NextSibling = BitConverter.ToInt32(_data, offset + 24)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ABK] Error reading entry at index {index}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all moves available from the starting position.
        /// </summary>
        public List<AbkEntry> GetRootMoves()
        {
            var moves = new List<AbkEntry>();

            if (_data == null) return moves;

            // Start at index 900 (initial position)
            var entry = ReadEntry(INITIAL_POSITION_INDEX);

            while (entry != null && entry.From >= 0 && entry.To >= 0)
            {
                // Valid move entry
                if (entry.From <= 63 && entry.To <= 63)
                {
                    moves.Add(entry);
                }

                // Move to next sibling (alternative move at same position)
                if (entry.NextSibling > 0 && entry.NextSibling != -1)
                {
                    entry = ReadEntry(entry.NextSibling);
                }
                else
                {
                    break;
                }
            }

            return moves;
        }

        /// <summary>
        /// Gets all moves available after a given sequence of moves.
        /// </summary>
        /// <param name="moveHistory">List of UCI moves (e.g., ["e2e4", "e7e5"])</param>
        public List<AbkEntry> GetMovesAfterSequence(List<string> moveHistory)
        {
            var moves = new List<AbkEntry>();

            if (_data == null || moveHistory.Count == 0)
            {
                return GetRootMoves();
            }

            // Start at root
            var currentEntry = ReadEntry(INITIAL_POSITION_INDEX);

            // Navigate through the move history
            foreach (string targetMove in moveHistory)
            {
                bool found = false;

                while (currentEntry != null)
                {
                    string entryMove = currentEntry.ToUciMove();

                    if (entryMove.Equals(targetMove, StringComparison.OrdinalIgnoreCase))
                    {
                        // Found the move, go to its children
                        if (currentEntry.NextMove > 0 && currentEntry.NextMove != -1)
                        {
                            currentEntry = ReadEntry(currentEntry.NextMove);
                            found = true;
                            break;
                        }
                        else
                        {
                            // No further moves in this line
                            return moves;
                        }
                    }

                    // Try next sibling
                    if (currentEntry.NextSibling > 0 && currentEntry.NextSibling != -1)
                    {
                        currentEntry = ReadEntry(currentEntry.NextSibling);
                    }
                    else
                    {
                        break;
                    }
                }

                if (!found)
                {
                    // Move not found in book
                    return moves;
                }
            }

            // Collect all moves at current position
            while (currentEntry != null && currentEntry.From >= 0 && currentEntry.To >= 0)
            {
                if (currentEntry.From <= 63 && currentEntry.To <= 63)
                {
                    moves.Add(currentEntry);
                }

                if (currentEntry.NextSibling > 0 && currentEntry.NextSibling != -1)
                {
                    currentEntry = ReadEntry(currentEntry.NextSibling);
                }
                else
                {
                    break;
                }
            }

            return moves;
        }

        /// <summary>
        /// Extracts all opening lines up to a maximum depth.
        /// Returns a dictionary mapping move sequences to statistics.
        /// </summary>
        /// <param name="maxDepth">Maximum number of halfmoves to extract</param>
        /// <param name="minGames">Minimum number of games for a line to be included</param>
        public Dictionary<string, OpeningLine> ExtractAllLines(int maxDepth = 20, int minGames = 10)
        {
            var lines = new Dictionary<string, OpeningLine>();

            if (_data == null) return lines;

            // BFS to extract all lines
            var queue = new Queue<(int index, List<string> moves, int depth)>();

            // Start with root moves
            var rootEntry = ReadEntry(INITIAL_POSITION_INDEX);
            if (rootEntry != null && rootEntry.From >= 0)
            {
                // Add first move and its siblings to queue
                var entry = rootEntry;
                while (entry != null && entry.From >= 0 && entry.To >= 0 && entry.From <= 63 && entry.To <= 63)
                {
                    queue.Enqueue((INITIAL_POSITION_INDEX + (entry == rootEntry ? 0 : 1), new List<string> { entry.ToUciMove() }, 1));

                    // Actually we need to track the entry, not just index
                    // Let's use a different approach - recursive with visited set
                    break;
                }
            }

            // Use recursive extraction instead
            ExtractLinesRecursive(INITIAL_POSITION_INDEX, new List<string>(), 0, maxDepth, minGames, lines, new HashSet<int>());

            Debug.WriteLine($"[ABK] Extracted {lines.Count} opening lines (maxDepth={maxDepth}, minGames={minGames})");

            return lines;
        }

        private void ExtractLinesRecursive(
            int entryIndex,
            List<string> currentMoves,
            int depth,
            int maxDepth,
            int minGames,
            Dictionary<string, OpeningLine> lines,
            HashSet<int> visited)
        {
            if (depth > maxDepth || visited.Contains(entryIndex))
                return;

            visited.Add(entryIndex);

            var entry = ReadEntry(entryIndex);
            if (entry == null || entry.From < 0 || entry.From > 63 || entry.To < 0 || entry.To > 63)
                return;

            // Add this move to current path
            var newMoves = new List<string>(currentMoves) { entry.ToUciMove() };

            // Record this line if it has enough games
            if (entry.NGames >= minGames)
            {
                string key = string.Join(" ", newMoves);
                if (!lines.ContainsKey(key))
                {
                    lines[key] = new OpeningLine
                    {
                        Moves = new List<string>(newMoves),
                        TotalGames = entry.NGames,
                        WinRate = entry.WinRate,
                        Depth = newMoves.Count
                    };
                }
            }

            // Recurse to children (next move in the line)
            if (entry.NextMove > 0 && entry.NextMove != -1 && entry.NextMove < _entryCount + INITIAL_POSITION_INDEX)
            {
                ExtractLinesRecursive(entry.NextMove, newMoves, depth + 1, maxDepth, minGames, lines, visited);
            }

            // Recurse to siblings (alternative moves at same position)
            if (entry.NextSibling > 0 && entry.NextSibling != -1 && entry.NextSibling < _entryCount + INITIAL_POSITION_INDEX)
            {
                ExtractLinesRecursive(entry.NextSibling, currentMoves, depth, maxDepth, minGames, lines, visited);
            }
        }

        /// <summary>
        /// Prints book statistics for debugging.
        /// </summary>
        public void PrintStats()
        {
            if (_data == null)
            {
                Debug.WriteLine("[ABK] No data loaded");
                return;
            }

            Debug.WriteLine($"[ABK] === Book Statistics ===");
            Debug.WriteLine($"[ABK] File size: {_data.Length:N0} bytes");
            Debug.WriteLine($"[ABK] Entry count: {_entryCount:N0}");

            var rootMoves = GetRootMoves();
            Debug.WriteLine($"[ABK] Root moves: {rootMoves.Count}");

            foreach (var move in rootMoves.OrderByDescending(m => m.NGames).Take(10))
            {
                Debug.WriteLine($"[ABK]   {move}");
            }
        }

        /// <summary>
        /// Converts extracted lines to a format compatible with OpeningBook.cs.
        /// Returns a dictionary mapping FEN position keys to (ECO, Name, Moves) tuples.
        /// </summary>
        public List<(string moves, int games, double winRate)> GetTopLines(int count = 100, int minGames = 50)
        {
            var allLines = ExtractAllLines(maxDepth: 15, minGames: minGames);

            return allLines.Values
                .OrderByDescending(l => l.TotalGames)
                .Take(count)
                .Select(l => (l.MovesString, l.TotalGames, l.WinRate))
                .ToList();
        }
    }
}
