using System.Diagnostics;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Manages an engine-vs-engine match with two separate UCI engine instances.
    /// Handles the match loop, clock management, and game-over detection.
    /// </summary>
    public class EngineMatchService : IDisposable
    {
        private ChessEngineService? whiteEngine;
        private ChessEngineService? blackEngine;
        private string whiteEngineName = "";
        private string blackEngineName = "";

        // Time tracking
        private readonly Stopwatch moveStopwatch = new Stopwatch();
        private long whiteRemainingMs;
        private long blackRemainingMs;
        private long whiteElapsedThisMove;
        private long blackElapsedThisMove;

        // Game state
        private GameState gameState = new GameState();
        private readonly Dictionary<string, int> positionCounts = new();
        private int moveCount;
        private bool isRunning;
        private CancellationTokenSource? matchCts;

        private readonly AppConfig config;
        private EngineMatchTimeControl timeControl = new();

        // Events (invoked on background thread - callers must marshal to UI thread)
        public event Action<string, string, long>? OnMovePlayed;  // (uciMove, fen, moveTimeMs)
        public event Action<long, long, bool>? OnClockUpdated;    // (whiteMs, blackMs, whiteToMove)
        public event Action<EngineMatchResult>? OnMatchEnded;
        public event Action<string>? OnStatusChanged;

        public bool IsRunning => isRunning;
        public long WhiteRemainingMs => whiteRemainingMs;
        public long BlackRemainingMs => blackRemainingMs;
        public bool WhiteToMove => gameState.WhiteToMove;

        public EngineMatchService(AppConfig config)
        {
            this.config = config;
        }

        public async Task StartMatchAsync(
            string whiteEnginePath,
            string blackEnginePath,
            string whiteEngine,
            string blackEngine,
            EngineMatchTimeControl timeControl,
            string? startingFen = null)
        {
            this.whiteEngineName = whiteEngine;
            this.blackEngineName = blackEngine;
            this.timeControl = timeControl;
            matchCts = new CancellationTokenSource();

            try
            {
                OnStatusChanged?.Invoke($"Initializing {whiteEngine} (White)...");

                // Create two separate engine instances
                this.whiteEngine = new ChessEngineService(config);
                await this.whiteEngine.InitializeAsync(whiteEnginePath);

                if (this.whiteEngine.State != EngineState.Ready)
                {
                    OnMatchEnded?.Invoke(new EngineMatchResult
                    {
                        Outcome = MatchOutcome.Interrupted,
                        Termination = MatchTermination.EngineError,
                        TotalMoves = 0
                    });
                    return;
                }

                OnStatusChanged?.Invoke($"Initializing {blackEngine} (Black)...");

                this.blackEngine = new ChessEngineService(config);
                await this.blackEngine.InitializeAsync(blackEnginePath);

                if (this.blackEngine.State != EngineState.Ready)
                {
                    OnMatchEnded?.Invoke(new EngineMatchResult
                    {
                        Outcome = MatchOutcome.Interrupted,
                        Termination = MatchTermination.EngineError,
                        TotalMoves = 0
                    });
                    return;
                }

                // Initialize game state
                string fen = startingFen ?? "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
                gameState = GameState.FromFEN(fen);
                positionCounts.Clear();
                moveCount = 0;

                // Initialize clocks
                if (timeControl.Type == TimeControlType.TotalPlusIncrement)
                {
                    whiteRemainingMs = timeControl.TotalTimeMs;
                    blackRemainingMs = timeControl.TotalTimeMs;
                }
                else
                {
                    whiteRemainingMs = 0;
                    blackRemainingMs = 0;
                }

                // Record initial position
                RecordPosition();

                isRunning = true;
                OnStatusChanged?.Invoke($"Match started: {whiteEngine} vs {blackEngine}");

                // Run the match loop
                await RunMatchLoopAsync(matchCts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EngineMatchService: StartMatch error - {ex.Message}");
                OnStatusChanged?.Invoke($"Match error: {ex.Message}");
                OnMatchEnded?.Invoke(new EngineMatchResult
                {
                    Outcome = MatchOutcome.Interrupted,
                    Termination = MatchTermination.EngineError,
                    TotalMoves = moveCount
                });
            }
            finally
            {
                isRunning = false;
            }
        }

        public void StopMatch()
        {
            matchCts?.Cancel();
        }

        private async Task RunMatchLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                bool whiteToMove = gameState.WhiteToMove;
                var activeEngine = whiteToMove ? whiteEngine : blackEngine;
                string activeName = whiteToMove ? whiteEngineName : blackEngineName;

                if (activeEngine == null || !activeEngine.IsEngineAlive())
                {
                    EndMatch(MatchTermination.EngineError,
                        whiteToMove ? MatchOutcome.BlackWins : MatchOutcome.WhiteWins);
                    return;
                }

                OnStatusChanged?.Invoke($"{activeName} ({(whiteToMove ? "White" : "Black")}) thinking...");

                // Build FEN and go command
                string fen = gameState.ToCompleteFEN();
                long activeRemaining = whiteToMove ? whiteRemainingMs : blackRemainingMs;
                string goCommand = timeControl.BuildGoCommand(whiteRemainingMs, blackRemainingMs);
                int timeoutMs = timeControl.GetTimeoutMs(activeRemaining);

                // Fire clock update
                OnClockUpdated?.Invoke(whiteRemainingMs, blackRemainingMs, whiteToMove);

                // Measure thinking time
                moveStopwatch.Restart();

                // Get engine's move
                string? bestMove;
                try
                {
                    bestMove = await activeEngine.GetMoveForMatchAsync(fen, goCommand, timeoutMs, ct);
                }
                catch (OperationCanceledException)
                {
                    // Match was stopped
                    EndMatch(MatchTermination.UserStopped, MatchOutcome.Interrupted);
                    return;
                }

                moveStopwatch.Stop();
                long moveTimeMs = moveStopwatch.ElapsedMilliseconds;

                // Update clock for TotalPlusIncrement mode
                if (timeControl.Type == TimeControlType.TotalPlusIncrement)
                {
                    if (whiteToMove)
                    {
                        whiteRemainingMs -= moveTimeMs;
                        whiteRemainingMs += timeControl.IncrementMs;
                        if (whiteRemainingMs <= 0)
                        {
                            whiteRemainingMs = 0;
                            EndMatch(MatchTermination.TimeForfeit, MatchOutcome.BlackWins);
                            return;
                        }
                    }
                    else
                    {
                        blackRemainingMs -= moveTimeMs;
                        blackRemainingMs += timeControl.IncrementMs;
                        if (blackRemainingMs <= 0)
                        {
                            blackRemainingMs = 0;
                            EndMatch(MatchTermination.TimeForfeit, MatchOutcome.WhiteWins);
                            return;
                        }
                    }
                }

                // Track elapsed for display
                if (whiteToMove)
                    whiteElapsedThisMove = moveTimeMs;
                else
                    blackElapsedThisMove = moveTimeMs;

                // No move returned means engine has no legal moves (checkmate or stalemate)
                if (string.IsNullOrEmpty(bestMove))
                {
                    // Determine if it's checkmate or stalemate by checking if king is in check
                    bool inCheck = IsKingInCheck(gameState.Board, whiteToMove);
                    if (inCheck)
                    {
                        EndMatch(MatchTermination.Checkmate,
                            whiteToMove ? MatchOutcome.BlackWins : MatchOutcome.WhiteWins);
                    }
                    else
                    {
                        EndMatch(MatchTermination.Stalemate, MatchOutcome.Draw);
                    }
                    return;
                }

                // Apply the move to game state
                char movingPiece = GetMovingPiece(bestMove);
                char capturedPiece = GetCapturedPiece(bestMove);
                bool isPawnMove = char.ToLower(movingPiece) == 'p';
                bool isCapture = capturedPiece != '.';

                string castling = gameState.CastlingRights;
                string ep = gameState.EnPassantTarget;
                ChessRulesService.ApplyUciMove(gameState.Board, bestMove, ref castling, ref ep);
                gameState.CastlingRights = castling;
                gameState.EnPassantTarget = ep;

                // Update half-move clock
                if (isPawnMove || isCapture)
                    gameState.HalfMoveClock = 0;
                else
                    gameState.HalfMoveClock++;

                // Update full-move number
                if (!whiteToMove)
                    gameState.FullMoveNumber++;

                // Toggle side to move
                gameState.WhiteToMove = !gameState.WhiteToMove;
                moveCount++;

                // Fire move played event
                string newFen = gameState.ToCompleteFEN();
                OnMovePlayed?.Invoke(bestMove, newFen, moveTimeMs);
                OnClockUpdated?.Invoke(whiteRemainingMs, blackRemainingMs, gameState.WhiteToMove);

                // Check for checkmate or stalemate by verifying legal moves exist
                if (!HasAnyLegalMove())
                {
                    bool inCheck = IsKingInCheck(gameState.Board, gameState.WhiteToMove);
                    if (inCheck)
                    {
                        // The side that just moved delivered checkmate
                        EndMatch(MatchTermination.Checkmate,
                            gameState.WhiteToMove ? MatchOutcome.BlackWins : MatchOutcome.WhiteWins);
                    }
                    else
                    {
                        EndMatch(MatchTermination.Stalemate, MatchOutcome.Draw);
                    }
                    return;
                }

                // Check draw conditions
                if (gameState.HalfMoveClock >= 100)
                {
                    EndMatch(MatchTermination.FiftyMoveRule, MatchOutcome.Draw);
                    return;
                }

                RecordPosition();
                string posKey = GetPositionKey();
                if (positionCounts.TryGetValue(posKey, out int count) && count >= 3)
                {
                    EndMatch(MatchTermination.ThreefoldRepetition, MatchOutcome.Draw);
                    return;
                }

                if (IsInsufficientMaterial())
                {
                    EndMatch(MatchTermination.InsufficientMaterial, MatchOutcome.Draw);
                    return;
                }

                // Brief yield for UI responsiveness
                await Task.Delay(30, ct);
            }

            // If we reach here, cancellation was requested
            EndMatch(MatchTermination.UserStopped, MatchOutcome.Interrupted);
        }

        private char GetMovingPiece(string uciMove)
        {
            if (uciMove.Length < 4) return '.';
            int srcFile = uciMove[0] - 'a';
            int srcRank = 8 - (uciMove[1] - '0');
            if (srcFile < 0 || srcFile > 7 || srcRank < 0 || srcRank > 7) return '.';
            return gameState.Board.GetPiece(srcRank, srcFile);
        }

        private char GetCapturedPiece(string uciMove)
        {
            if (uciMove.Length < 4) return '.';
            int dstFile = uciMove[2] - 'a';
            int dstRank = 8 - (uciMove[3] - '0');
            if (dstFile < 0 || dstFile > 7 || dstRank < 0 || dstRank > 7) return '.';

            char target = gameState.Board.GetPiece(dstRank, dstFile);

            // Check en passant capture
            if (target == '.')
            {
                char moving = GetMovingPiece(uciMove);
                if (char.ToLower(moving) == 'p' && uciMove[0] != uciMove[2])
                {
                    // Diagonal pawn move to empty square = en passant
                    return gameState.WhiteToMove ? 'p' : 'P';
                }
            }

            return target;
        }

        private void EndMatch(MatchTermination termination, MatchOutcome outcome)
        {
            isRunning = false;
            var result = new EngineMatchResult
            {
                Outcome = outcome,
                Termination = termination,
                TotalMoves = moveCount,
                WhiteTimeRemainingMs = whiteRemainingMs,
                BlackTimeRemainingMs = blackRemainingMs
            };

            OnStatusChanged?.Invoke(result.GetResultString());
            OnMatchEnded?.Invoke(result);
        }

        private string GetPositionKey()
        {
            // Position key for threefold repetition: board + turn + castling + ep
            string boardFen = gameState.Board.ToFEN();
            string turn = gameState.WhiteToMove ? "w" : "b";
            string castling = string.IsNullOrEmpty(gameState.CastlingRights) ? "-" : gameState.CastlingRights;
            return $"{boardFen} {turn} {castling} {gameState.EnPassantTarget}";
        }

        private void RecordPosition()
        {
            string key = GetPositionKey();
            if (positionCounts.ContainsKey(key))
                positionCounts[key]++;
            else
                positionCounts[key] = 1;
        }

        private bool IsKingInCheck(ChessBoard board, bool whiteKing)
        {
            char king = whiteKing ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(r, c) == king)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
                if (kingRow != -1) break;
            }

            if (kingRow == -1) return false;

            // Check all enemy pieces for attacks on king
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;
                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite == whiteKing) continue; // Same side

                    if (CanAttack(board, r, c, piece, kingRow, kingCol))
                        return true;
                }
            }

            return false;
        }

        private static bool CanAttack(ChessBoard board, int fromRow, int fromCol, char piece, int toRow, int toCol)
        {
            char lower = char.ToLower(piece);

            switch (lower)
            {
                case 'p':
                    int direction = char.IsUpper(piece) ? -1 : 1;
                    return (toRow - fromRow) == direction && Math.Abs(toCol - fromCol) == 1;

                case 'n':
                    int dr = Math.Abs(toRow - fromRow);
                    int dc = Math.Abs(toCol - fromCol);
                    return (dr == 2 && dc == 1) || (dr == 1 && dc == 2);

                case 'b':
                    return IsDiagonalClear(board, fromRow, fromCol, toRow, toCol);

                case 'r':
                    return IsStraightClear(board, fromRow, fromCol, toRow, toCol);

                case 'q':
                    return IsDiagonalClear(board, fromRow, fromCol, toRow, toCol) ||
                           IsStraightClear(board, fromRow, fromCol, toRow, toCol);

                case 'k':
                    return Math.Abs(toRow - fromRow) <= 1 && Math.Abs(toCol - fromCol) <= 1;
            }

            return false;
        }

        private static bool IsDiagonalClear(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol)
        {
            int dr = toRow - fromRow;
            int dc = toCol - fromCol;
            if (Math.Abs(dr) != Math.Abs(dc) || dr == 0) return false;

            int stepR = dr > 0 ? 1 : -1;
            int stepC = dc > 0 ? 1 : -1;
            int r = fromRow + stepR, c = fromCol + stepC;

            while (r != toRow || c != toCol)
            {
                if (board.GetPiece(r, c) != '.') return false;
                r += stepR;
                c += stepC;
            }

            return true;
        }

        private static bool IsStraightClear(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol)
        {
            if (fromRow != toRow && fromCol != toCol) return false;
            if (fromRow == toRow && fromCol == toCol) return false;

            if (fromRow == toRow)
            {
                int step = toCol > fromCol ? 1 : -1;
                for (int c = fromCol + step; c != toCol; c += step)
                {
                    if (board.GetPiece(fromRow, c) != '.') return false;
                }
            }
            else
            {
                int step = toRow > fromRow ? 1 : -1;
                for (int r = fromRow + step; r != toRow; r += step)
                {
                    if (board.GetPiece(r, fromCol) != '.') return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the side to move has at least one legal move.
        /// Returns false if the position is checkmate or stalemate.
        /// </summary>
        private bool HasAnyLegalMove()
        {
            bool whiteToMove = gameState.WhiteToMove;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = gameState.Board.GetPiece(r, c);
                    if (piece == '.') continue;
                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != whiteToMove) continue;

                    char lower = char.ToLower(piece);

                    // Generate candidate destinations based on piece type
                    switch (lower)
                    {
                        case 'p':
                            if (HasLegalPawnMove(r, c, pieceIsWhite)) return true;
                            break;
                        case 'n':
                            if (HasLegalKnightMove(r, c, pieceIsWhite)) return true;
                            break;
                        case 'b':
                            if (HasLegalSlidingMove(r, c, pieceIsWhite, diagonal: true, straight: false)) return true;
                            break;
                        case 'r':
                            if (HasLegalSlidingMove(r, c, pieceIsWhite, diagonal: false, straight: true)) return true;
                            break;
                        case 'q':
                            if (HasLegalSlidingMove(r, c, pieceIsWhite, diagonal: true, straight: true)) return true;
                            break;
                        case 'k':
                            if (HasLegalKingMove(r, c, pieceIsWhite)) return true;
                            break;
                    }
                }
            }

            return false;
        }

        private bool HasLegalPawnMove(int row, int col, bool isWhite)
        {
            int direction = isWhite ? -1 : 1;
            int startRow = isWhite ? 6 : 1;
            char pawn = isWhite ? 'P' : 'p';

            // Single push
            int fwdRow = row + direction;
            if (fwdRow >= 0 && fwdRow < 8 && gameState.Board.GetPiece(fwdRow, col) == '.')
            {
                if (IsMoveLegal(row, col, fwdRow, col, pawn))
                    return true;

                // Double push from starting row
                if (row == startRow)
                {
                    int dblRow = row + 2 * direction;
                    if (dblRow >= 0 && dblRow < 8 && gameState.Board.GetPiece(dblRow, col) == '.')
                    {
                        if (IsMoveLegal(row, col, dblRow, col, pawn))
                            return true;
                    }
                }
            }

            // Captures (including en passant)
            for (int dc = -1; dc <= 1; dc += 2)
            {
                int capCol = col + dc;
                if (capCol < 0 || capCol > 7 || fwdRow < 0 || fwdRow > 7) continue;

                char target = gameState.Board.GetPiece(fwdRow, capCol);
                bool isCapture = target != '.' && char.IsUpper(target) != isWhite;
                bool isEnPassant = false;

                if (target == '.' && !string.IsNullOrEmpty(gameState.EnPassantTarget) && gameState.EnPassantTarget != "-" && gameState.EnPassantTarget.Length >= 2)
                {
                    int epFile = gameState.EnPassantTarget[0] - 'a';
                    int epRank = 8 - (gameState.EnPassantTarget[1] - '0');
                    isEnPassant = capCol == epFile && fwdRow == epRank;
                }

                if (isCapture || isEnPassant)
                {
                    if (IsMoveLegal(row, col, fwdRow, capCol, pawn))
                        return true;
                }
            }

            return false;
        }

        private bool HasLegalKnightMove(int row, int col, bool isWhite)
        {
            int[,] offsets = { { -2, -1 }, { -2, 1 }, { -1, -2 }, { -1, 2 }, { 1, -2 }, { 1, 2 }, { 2, -1 }, { 2, 1 } };
            char knight = isWhite ? 'N' : 'n';

            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                int tr = row + offsets[i, 0];
                int tc = col + offsets[i, 1];
                if (tr < 0 || tr > 7 || tc < 0 || tc > 7) continue;

                char target = gameState.Board.GetPiece(tr, tc);
                if (target != '.' && char.IsUpper(target) == isWhite) continue; // Own piece

                if (IsMoveLegal(row, col, tr, tc, knight))
                    return true;
            }

            return false;
        }

        private bool HasLegalSlidingMove(int row, int col, bool isWhite, bool diagonal, bool straight)
        {
            char piece = gameState.Board.GetPiece(row, col);
            var directions = new List<(int dr, int dc)>();

            if (diagonal)
            {
                directions.Add((-1, -1));
                directions.Add((-1, 1));
                directions.Add((1, -1));
                directions.Add((1, 1));
            }
            if (straight)
            {
                directions.Add((-1, 0));
                directions.Add((1, 0));
                directions.Add((0, -1));
                directions.Add((0, 1));
            }

            foreach (var (dr, dc) in directions)
            {
                int tr = row + dr;
                int tc = col + dc;

                while (tr >= 0 && tr < 8 && tc >= 0 && tc < 8)
                {
                    char target = gameState.Board.GetPiece(tr, tc);

                    if (target != '.' && char.IsUpper(target) == isWhite)
                        break; // Own piece blocks

                    if (IsMoveLegal(row, col, tr, tc, piece))
                        return true;

                    if (target != '.') break; // Enemy piece: can capture but can't go further

                    tr += dr;
                    tc += dc;
                }
            }

            return false;
        }

        private bool HasLegalKingMove(int row, int col, bool isWhite)
        {
            char king = isWhite ? 'K' : 'k';

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int tr = row + dr;
                    int tc = col + dc;
                    if (tr < 0 || tr > 7 || tc < 0 || tc > 7) continue;

                    char target = gameState.Board.GetPiece(tr, tc);
                    if (target != '.' && char.IsUpper(target) == isWhite) continue;

                    if (IsMoveLegal(row, col, tr, tc, king))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tests if making a move leaves the moving side's king safe (not in check).
        /// </summary>
        private bool IsMoveLegal(int fromRow, int fromCol, int toRow, int toCol, char piece)
        {
            bool isWhite = char.IsUpper(piece);

            // Make the move on a temporary board
            ChessBoard testBoard = new ChessBoard(gameState.Board.GetArray());
            testBoard.SetPiece(toRow, toCol, piece);
            testBoard.SetPiece(fromRow, fromCol, '.');

            // Handle en passant capture: pawn moves diagonally to empty square
            if (char.ToLower(piece) == 'p' && fromCol != toCol && gameState.Board.GetPiece(toRow, toCol) == '.')
            {
                testBoard.SetPiece(fromRow, toCol, '.');
            }

            return !IsKingInCheck(testBoard, isWhite);
        }

        private bool IsInsufficientMaterial()
        {
            var pieces = new List<(char piece, int row, int col)>();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char p = gameState.Board.GetPiece(r, c);
                    if (p != '.')
                        pieces.Add((p, r, c));
                }
            }

            // K vs K
            if (pieces.Count == 2) return true;

            // K+minor vs K
            if (pieces.Count == 3)
            {
                var nonKing = pieces.First(p => char.ToLower(p.piece) != 'k');
                char type = char.ToLower(nonKing.piece);
                if (type == 'n' || type == 'b') return true;
            }

            // K+B vs K+B same color bishops
            if (pieces.Count == 4)
            {
                var bishops = pieces.Where(p => char.ToLower(p.piece) == 'b').ToList();
                if (bishops.Count == 2)
                {
                    bool sameColor = ((bishops[0].row + bishops[0].col) % 2) ==
                                     ((bishops[1].row + bishops[1].col) % 2);
                    if (sameColor) return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            matchCts?.Cancel();
            matchCts?.Dispose();

            if (whiteEngine != null)
            {
                try { whiteEngine.Dispose(); } catch { }
                whiteEngine = null;
            }

            if (blackEngine != null)
            {
                try { blackEngine.Dispose(); } catch { }
                blackEngine = null;
            }
        }
    }
}
