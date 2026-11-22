using System;
using System.Collections.Generic;
using System.IO;

namespace ChessDroid.Services
{
    /// <summary>
    /// Reads Polyglot opening book files (.bin format).
    ///
    /// Polyglot Format:
    /// - Each entry is 16 bytes, sorted by Zobrist key
    /// - Entry: 8-byte key, 2-byte move, 2-byte weight, 4-byte learn
    /// - Multiple entries can share the same key (alternative moves)
    ///
    /// Move encoding (16 bits):
    /// - bits 0-2: to file (0-7)
    /// - bits 3-5: to rank (0-7)
    /// - bits 6-8: from file (0-7)
    /// - bits 9-11: from rank (0-7)
    /// - bits 12-14: promotion (0=none, 1=knight, 2=bishop, 3=rook, 4=queen)
    /// </summary>
    public class PolyglotBookReader : IDisposable
    {
        private const int ENTRY_SIZE = 16;

        private byte[]? _bookData;
        private int _entryCount;
        private bool _disposed;

        /// <summary>
        /// Represents a single entry from the Polyglot book.
        /// </summary>
        public class PolyglotEntry
        {
            public ulong Key { get; set; }
            public ushort RawMove { get; set; }
            public ushort Weight { get; set; }
            public uint Learn { get; set; }

            // Decoded move components
            public int FromFile => (RawMove >> 6) & 0x7;
            public int FromRank => (RawMove >> 9) & 0x7;
            public int ToFile => RawMove & 0x7;
            public int ToRank => (RawMove >> 3) & 0x7;
            public int Promotion => (RawMove >> 12) & 0x7;

            public int FromSquare => FromRank * 8 + FromFile;
            public int ToSquare => ToRank * 8 + ToFile;

            /// <summary>
            /// Converts to UCI move notation (e.g., "e2e4", "e7e8q").
            /// </summary>
            public string ToUciMove()
            {
                string from = $"{(char)('a' + FromFile)}{FromRank + 1}";
                string to = $"{(char)('a' + ToFile)}{ToRank + 1}";

                string promo = Promotion switch
                {
                    1 => "n",
                    2 => "b",
                    3 => "r",
                    4 => "q",
                    _ => ""
                };

                // Handle castling - Polyglot uses king captures rook notation
                // e1h1 -> e1g1 (white kingside), e1a1 -> e1c1 (white queenside)
                // e8h8 -> e8g8 (black kingside), e8a8 -> e8c8 (black queenside)
                if (FromFile == 4) // King on e-file
                {
                    if (FromRank == 0) // White king
                    {
                        if (ToFile == 7 && ToRank == 0) return "e1g1"; // Kingside
                        if (ToFile == 0 && ToRank == 0) return "e1c1"; // Queenside
                    }
                    else if (FromRank == 7) // Black king
                    {
                        if (ToFile == 7 && ToRank == 7) return "e8g8"; // Kingside
                        if (ToFile == 0 && ToRank == 7) return "e8c8"; // Queenside
                    }
                }

                return from + to + promo;
            }

            public override string ToString()
            {
                return $"{ToUciMove()} (weight={Weight})";
            }
        }

        /// <summary>
        /// Number of entries in the loaded book.
        /// </summary>
        public int EntryCount => _entryCount;

        /// <summary>
        /// Whether a book is currently loaded.
        /// </summary>
        public bool IsLoaded => _bookData != null && _entryCount > 0;

        /// <summary>
        /// Loads a Polyglot book file into memory.
        /// </summary>
        public bool LoadFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                _bookData = File.ReadAllBytes(filePath);

                if (_bookData.Length < ENTRY_SIZE)
                {
                    _bookData = null;
                    return false;
                }

                _entryCount = _bookData.Length / ENTRY_SIZE;
                return true;
            }
            catch
            {
                _bookData = null;
                _entryCount = 0;
                return false;
            }
        }

        /// <summary>
        /// Reads a single entry at the specified index.
        /// </summary>
        private PolyglotEntry? ReadEntry(int index)
        {
            if (_bookData == null || index < 0 || index >= _entryCount)
                return null;

            int offset = index * ENTRY_SIZE;

            // Polyglot uses big-endian byte order
            ulong key = ReadBigEndianUInt64(_bookData, offset);
            ushort move = ReadBigEndianUInt16(_bookData, offset + 8);
            ushort weight = ReadBigEndianUInt16(_bookData, offset + 10);
            uint learn = ReadBigEndianUInt32(_bookData, offset + 12);

            return new PolyglotEntry
            {
                Key = key,
                RawMove = move,
                Weight = weight,
                Learn = learn
            };
        }

        /// <summary>
        /// Finds all book entries matching the given Polyglot hash key.
        /// Uses binary search since the file is sorted by key.
        /// </summary>
        public List<PolyglotEntry> FindEntries(ulong key)
        {
            var results = new List<PolyglotEntry>();

            if (_bookData == null || _entryCount == 0)
                return results;

            // Binary search for first occurrence
            int left = 0;
            int right = _entryCount - 1;
            int firstMatch = -1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                var entry = ReadEntry(mid);
                if (entry == null) break;

                if (entry.Key == key)
                {
                    firstMatch = mid;
                    right = mid - 1; // Keep searching left for first occurrence
                }
                else if (entry.Key < key)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            if (firstMatch == -1)
                return results;

            // Collect all entries with the same key
            for (int i = firstMatch; i < _entryCount; i++)
            {
                var entry = ReadEntry(i);
                if (entry == null || entry.Key != key)
                    break;
                results.Add(entry);
            }

            // Sort by weight descending
            results.Sort((a, b) => b.Weight.CompareTo(a.Weight));

            return results;
        }

        /// <summary>
        /// Gets statistics about the loaded book.
        /// </summary>
        public (int entries, long fileSize, int uniquePositions) GetStats()
        {
            if (_bookData == null)
                return (0, 0, 0);

            // Count unique positions (keys)
            var uniqueKeys = new HashSet<ulong>();
            for (int i = 0; i < _entryCount; i++)
            {
                var entry = ReadEntry(i);
                if (entry != null)
                    uniqueKeys.Add(entry.Key);
            }

            return (_entryCount, _bookData.Length, uniqueKeys.Count);
        }

        #region Big-Endian Reading Helpers

        private static ulong ReadBigEndianUInt64(byte[] data, int offset)
        {
            return ((ulong)data[offset] << 56) |
                   ((ulong)data[offset + 1] << 48) |
                   ((ulong)data[offset + 2] << 40) |
                   ((ulong)data[offset + 3] << 32) |
                   ((ulong)data[offset + 4] << 24) |
                   ((ulong)data[offset + 5] << 16) |
                   ((ulong)data[offset + 6] << 8) |
                   data[offset + 7];
        }

        private static ushort ReadBigEndianUInt16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        private static uint ReadBigEndianUInt32(byte[] data, int offset)
        {
            return ((uint)data[offset] << 24) |
                   ((uint)data[offset + 1] << 16) |
                   ((uint)data[offset + 2] << 8) |
                   data[offset + 3];
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _bookData = null;
                _entryCount = 0;
                _disposed = true;
            }
        }
    }
}
