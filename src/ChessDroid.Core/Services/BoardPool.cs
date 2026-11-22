using System.Collections.Concurrent;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Object pool for ChessBoard instances to reduce allocations.
    /// Analysis code creates 17+ temporary boards per move - pooling eliminates most allocations.
    /// </summary>
    public static class BoardPool
    {
        private static readonly ConcurrentBag<ChessBoard> pool = new ConcurrentBag<ChessBoard>();
        private const int MAX_POOL_SIZE = 50;

        /// <summary>
        /// Rent a board from the pool and copy the source board's state into it.
        /// Must be returned via Dispose() to avoid leaks.
        /// </summary>
        public static PooledBoard Rent(ChessBoard sourceBoard)
        {
            ChessBoard board = pool.TryTake(out var b) ? b : new ChessBoard();
            CopyBoard(sourceBoard, board);
            return new PooledBoard(board);
        }

        /// <summary>
        /// Internal method to return a board to the pool.
        /// Called automatically by PooledBoard.Dispose().
        /// </summary>
        internal static void Return(ChessBoard board)
        {
            ClearBoard(board);
            if (pool.Count < MAX_POOL_SIZE)
                pool.Add(board);
        }

        /// <summary>
        /// Copy all pieces from source board to destination board.
        /// Faster than new ChessBoard(source.GetArray()) - no array allocation.
        /// </summary>
        private static void CopyBoard(ChessBoard source, ChessBoard dest)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    dest[r, c] = source[r, c];
        }

        /// <summary>
        /// Clear all pieces from the board (set to empty squares).
        /// </summary>
        private static void ClearBoard(ChessBoard board)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    board[r, c] = '.';
        }
    }

    /// <summary>
    /// RAII wrapper for pooled ChessBoard.
    /// Use with 'using' statement to ensure automatic return to pool.
    /// Example: using var pooled = BoardPool.Rent(board);
    ///          ChessBoard tempBoard = pooled.Board;
    /// </summary>
    public struct PooledBoard : IDisposable
    {
        private ChessBoard? board;

        internal PooledBoard(ChessBoard board)
        {
            this.board = board;
        }

        /// <summary>
        /// Get the pooled board instance.
        /// Throws if already disposed.
        /// </summary>
        public ChessBoard Board => board ?? throw new ObjectDisposedException("PooledBoard");

        /// <summary>
        /// Return the board to the pool.
        /// Automatically called at end of 'using' block.
        /// </summary>
        public void Dispose()
        {
            if (board != null)
            {
                BoardPool.Return(board);
                board = null;
            }
        }
    }
}
