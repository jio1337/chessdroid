using System;
using System.Diagnostics;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Manages chess position state including FEN generation, castling rights, and position tracking
    /// Extracted from MainForm to separate position management concerns
    /// </summary>
    public class PositionStateManager
    {
        private GameState? currentGameState;
        private ChessBoard? lastDetectedBoard;
        private DateTime lastMoveTime = DateTime.Now;

        public DateTime LastMoveTime => lastMoveTime;
        public ChessBoard? LastDetectedBoard => lastDetectedBoard;

        /// <summary>
        /// Initializes a new game state
        /// </summary>
        public void InitializeGameState(bool whiteToMove)
        {
            currentGameState = new GameState
            {
                WhiteToMove = whiteToMove,
                CastlingRights = "KQkq",
                EnPassantTarget = "-",
                LastMoveTime = DateTime.Now
            };
            lastDetectedBoard = null;
            lastMoveTime = DateTime.Now;
        }

        /// <summary>
        /// Sets whose turn it is
        /// </summary>
        public void SetWhiteToMove(bool whiteToMove)
        {
            if (currentGameState != null)
            {
                currentGameState.WhiteToMove = whiteToMove;
            }
        }

        /// <summary>
        /// Gets whether it's White's turn
        /// </summary>
        public bool IsWhiteToMove()
        {
            return currentGameState?.WhiteToMove ?? true;
        }

        /// <summary>
        /// Updates position state by detecting moves and updating castling rights
        /// </summary>
        public void UpdatePositionState(ChessBoard currentBoard)
        {
            if (currentGameState == null)
            {
                Debug.WriteLine("currentGameState is null in UpdatePositionState");
                return;
            }

            if (lastDetectedBoard != null)
            {
                (string uciMovePrev, string updatedCastlingPrev, string newEpPrev) =
                    ChessRulesService.DetectMoveAndUpdateCastling(lastDetectedBoard, currentBoard, currentGameState.CastlingRights);
                currentGameState.CastlingRights = updatedCastlingPrev;
                currentGameState.EnPassantTarget = newEpPrev;
            }
            else
            {
                // First position - infer castling rights from current board state
                currentGameState.CastlingRights = ChessRulesService.InferCastlingRights(currentBoard);
                currentGameState.EnPassantTarget = "-";
            }
        }

        /// <summary>
        /// Generates complete FEN string from current board and game state
        /// </summary>
        public string GenerateCompleteFEN(ChessBoard currentBoard)
        {
            if (currentGameState == null)
            {
                Debug.WriteLine("currentGameState is null in GenerateCompleteFEN");
                return ChessNotationService.GenerateFENFromBoard(currentBoard) + " w KQkq - 0 1";
            }

            string fenPosition = ChessNotationService.GenerateFENFromBoard(currentBoard);
            string turn = currentGameState.WhiteToMove ? "w" : "b";
            string castling = string.IsNullOrEmpty(currentGameState.CastlingRights) ? "-" : currentGameState.CastlingRights;
            return $"{fenPosition} {turn} {castling} {currentGameState.EnPassantTarget} 0 1";
        }

        /// <summary>
        /// Saves the current board state and move time
        /// </summary>
        public void SaveMoveState(ChessBoard currentBoard)
        {
            lastMoveTime = DateTime.Now;
            if (currentGameState != null)
            {
                currentGameState.LastMoveTime = lastMoveTime;
                lastDetectedBoard = new ChessBoard(currentBoard.GetArray());
                currentGameState.Board = lastDetectedBoard;
            }
        }

        /// <summary>
        /// Checks if the board is empty (no pieces)
        /// </summary>
        public bool IsBoardEmpty(ChessBoard board)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    char piece = board[row, col];
                    if (piece != '.')
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Resets all state
        /// </summary>
        public void Reset()
        {
            currentGameState = null;
            lastDetectedBoard = null;
            lastMoveTime = DateTime.Now;
        }

        /// <summary>
        /// Gets the current game state (nullable)
        /// </summary>
        public GameState? GetGameState()
        {
            return currentGameState;
        }
    }
}
