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

        private StreamWriter? _matchLog;
        private void LogM(string msg) { _matchLog?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [MatchSvc] {msg}"); _matchLog?.Flush(); }

        // Time tracking
        private readonly Stopwatch moveStopwatch = new Stopwatch();
        private long whiteRemainingMs;
        private long blackRemainingMs;
        private long whiteElapsedThisMove;
        private long blackElapsedThisMove;
        private int whitePeriodMovesLeft;
        private int blackPeriodMovesLeft;

        // Game state
        private GameState gameState = new GameState();
        private readonly Dictionary<string, int> positionCounts = new();
        private int moveCount;
        private bool isRunning;
        private CancellationTokenSource? matchCts;

        // Animation sync — set WaitForAnimation = true when board animations are enabled
        public bool WaitForAnimation { get; set; }

        // Adjudication — resign when |eval| >= 500cp for 10 plies; draw when |eval| <= 10cp for 10 plies
        // Thresholds aligned with Lichess (600cp resign / 10cp draw / 10 plies).
        // Draw cp matches exactly; resign cp kept at 500 (slightly more aggressive).
        public bool AdjudicationEnabled { get; set; }
        private const int AdjudicateResignCp = 500;
        private const int AdjudicateDrawCp   = 10;
        private const int AdjudicatePlies    = 10;
        private readonly List<int> _adjEvalHistory = new();

        // When set, this engine annotates every move with a neutral evaluation instead of
        // using each playing engine's self-reported score (which oscillates between engines).
        public ChessEngineService? AnnotatorEngine { get; set; }
        public int AnnotatorDepth { get; set; } = DefaultAnnotatorDepth;
        public const int DefaultAnnotatorDepth = 14;

        // Continuous annotation state
        private CancellationTokenSource? _annotCts;
        private Task<string?>? _annotTask;

        private TaskCompletionSource<bool>? _animTcs;

        private readonly AppConfig config;
        private EngineMatchTimeControl timeControl = new();

        // Events (invoked on background thread - callers must marshal to UI thread)
        public event Action<string, string, long, string?>? OnMovePlayed;  // (uciMove, fen, moveTimeMs, eval)
        public event Action<long, long, bool>? OnClockUpdated;    // (whiteMs, blackMs, whiteToMove)
        public event Action<EngineMatchResult>? OnMatchEnded;
        public event Action<string>? OnStatusChanged;
        public event Action<string>? OnAnnotatorEvalUpdated;       // fired at each depth during continuous annotation

        public bool IsRunning => isRunning;
        private bool IsClockRunning => timeControl.Type == TimeControlType.TotalPlusIncrement
                                    || timeControl.Type == TimeControlType.RepeatingPeriod;
        public long WhiteRemainingMs => isRunning && IsClockRunning
            && gameState.WhiteToMove && moveStopwatch.IsRunning
            ? Math.Max(0, whiteRemainingMs - moveStopwatch.ElapsedMilliseconds)
            : whiteRemainingMs;
        public long BlackRemainingMs => isRunning && IsClockRunning
            && !gameState.WhiteToMove && moveStopwatch.IsRunning
            ? Math.Max(0, blackRemainingMs - moveStopwatch.ElapsedMilliseconds)
            : blackRemainingMs;
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
                // Open log file. Used to be a single fixed "match_debug.txt" in the exe
                // directory, overwritten on every match — fine for one match at a time, but
                // Tournament mode runs up to 4 EngineMatchService instances concurrently, and
                // StreamWriter opens a file with exclusive write access by default: only the
                // first of the 4 concurrent StartMatchAsync calls could actually open it, and
                // the other 3 threw IOException here, BEFORE this try block existed around
                // this line — so the exception propagated straight out of this fire-and-forget
                // async method as a silent unobserved task exception, and those 3 matches'
                // engines never even got created. Now unique per match and inside the try, so
                // any failure here is caught below and surfaces via OnMatchEnded instead.
                string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logsDir);
                string logPath = Path.Combine(logsDir, $"match_debug_{Guid.NewGuid():N}.txt");
                _matchLog = new StreamWriter(logPath, append: false) { AutoFlush = true };
                LogM($"=== MATCH START: {whiteEngine} (W) vs {blackEngine} (B) | TC={timeControl} | FEN={startingFen ?? "startpos"} ===");

                OnStatusChanged?.Invoke($"Initializing {whiteEngine} (White)...");

                // Create two separate engine instances
                this.whiteEngine = new ChessEngineService(config);
                await this.whiteEngine.InitializeAsync(whiteEnginePath);
                this.whiteEngine.MatchLog = _matchLog;

                if (this.whiteEngine.State != EngineState.Ready)
                {
                    LogM($"ERROR: white engine failed to initialize (state={this.whiteEngine.State})");
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
                this.blackEngine.MatchLog = _matchLog;

                if (this.blackEngine.State != EngineState.Ready)
                {
                    LogM($"ERROR: black engine failed to initialize (state={this.blackEngine.State})");
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
                _adjEvalHistory.Clear();
                moveCount = 0;

                // Initialize clocks
                if (timeControl.Type == TimeControlType.TotalPlusIncrement ||
                    timeControl.Type == TimeControlType.RepeatingPeriod)
                {
                    whiteRemainingMs = timeControl.TotalTimeMs;
                    blackRemainingMs = timeControl.TotalTimeMs;
                }
                else
                {
                    whiteRemainingMs = 0;
                    blackRemainingMs = 0;
                }

                if (timeControl.Type == TimeControlType.RepeatingPeriod)
                {
                    whitePeriodMovesLeft = timeControl.MovesPerPeriod;
                    blackPeriodMovesLeft = timeControl.MovesPerPeriod;
                }

                // Record initial position
                RecordPosition();

                isRunning = true;
                OnStatusChanged?.Invoke($"Match started: {whiteEngine} vs {blackEngine}");

                // Run the match loop
                await RunMatchLoopAsync(matchCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Cancellation propagated from delay/animation wait between moves (e.g. fixed-depth stop)
                OnStatusChanged?.Invoke("Match stopped");
                OnMatchEnded?.Invoke(new EngineMatchResult
                {
                    Outcome = MatchOutcome.Interrupted,
                    Termination = MatchTermination.UserStopped,
                    TotalMoves = moveCount,
                    TimeControl = timeControl.Type
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EngineMatchService: StartMatch error - {ex.Message}");
                OnStatusChanged?.Invoke($"Match error: {ex.Message}");
                OnMatchEnded?.Invoke(new EngineMatchResult
                {
                    Outcome = MatchOutcome.Interrupted,
                    Termination = MatchTermination.EngineError,
                    TotalMoves = moveCount,
                    TimeControl = timeControl.Type
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

        public void NotifyAnimationCompleted()
        {
            _animTcs?.TrySetResult(true);
        }

        private async Task RunMatchLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                bool whiteToMove = gameState.WhiteToMove;
                var activeEngine = whiteToMove ? whiteEngine : blackEngine;
                string activeName = whiteToMove ? whiteEngineName : blackEngineName;
                string side = whiteToMove ? "White" : "Black";

                // Build FEN and go command (computed before alive check so we can log it on error)
                string fen = gameState.ToCompleteFEN();
                long activeRemaining = whiteToMove ? whiteRemainingMs : blackRemainingMs;
                LogM($"--- Move #{moveCount + 1} | {side} ({activeName}) | wtime={whiteRemainingMs} btime={blackRemainingMs} | FEN: {fen}");

                if (activeEngine == null || !activeEngine.IsEngineAlive())
                {
                    string exitInfo = activeEngine?.GetProcessInfo() ?? "engine=null";
                    LogM($"ENGINE DEAD: {side} ({activeName}) not alive — {exitInfo} — EndMatch(EngineError)");
                    EndMatch(MatchTermination.EngineError,
                        whiteToMove ? MatchOutcome.BlackWins : MatchOutcome.WhiteWins);
                    return;
                }

                OnStatusChanged?.Invoke($"{activeName} ({side}) thinking...");

                int movesToGo = timeControl.Type == TimeControlType.RepeatingPeriod
                    ? (whiteToMove ? whitePeriodMovesLeft : blackPeriodMovesLeft)
                    : 0;
                string goCommand = timeControl.BuildGoCommand(whiteRemainingMs, blackRemainingMs, movesToGo);
                int timeoutMs = timeControl.GetTimeoutMs(activeRemaining);

                // Fire clock update
                OnClockUpdated?.Invoke(whiteRemainingMs, blackRemainingMs, whiteToMove);

                // Measure thinking time
                moveStopwatch.Restart();

                // Get engine's move — discard its self-reported eval (pre-move position,
                // shallow depth). Annotator below provides the authoritative post-move eval.
                string? bestMove;
                try
                {
                    (bestMove, _) = await activeEngine.GetMoveForMatchAsync(fen, goCommand, timeoutMs, ct);
                }
                catch (OperationCanceledException)
                {
                    // Match was stopped
                    EndMatch(MatchTermination.UserStopped, MatchOutcome.Interrupted);
                    return;
                }

                moveStopwatch.Stop();
                long moveTimeMs = moveStopwatch.ElapsedMilliseconds;
                LogM($"GetMoveForMatchAsync returned: move={bestMove ?? "(null)"} in {moveTimeMs}ms");

                // Update clock
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
                else if (timeControl.Type == TimeControlType.RepeatingPeriod)
                {
                    if (whiteToMove)
                    {
                        whiteRemainingMs -= moveTimeMs;
                        if (whiteRemainingMs <= 0)
                        {
                            whiteRemainingMs = 0;
                            EndMatch(MatchTermination.TimeForfeit, MatchOutcome.BlackWins);
                            return;
                        }
                        whitePeriodMovesLeft--;
                        if (whitePeriodMovesLeft <= 0)
                        {
                            whiteRemainingMs += timeControl.TotalTimeMs;
                            whitePeriodMovesLeft = timeControl.MovesPerPeriod;
                        }
                    }
                    else
                    {
                        blackRemainingMs -= moveTimeMs;
                        if (blackRemainingMs <= 0)
                        {
                            blackRemainingMs = 0;
                            EndMatch(MatchTermination.TimeForfeit, MatchOutcome.WhiteWins);
                            return;
                        }
                        blackPeriodMovesLeft--;
                        if (blackPeriodMovesLeft <= 0)
                        {
                            blackRemainingMs += timeControl.TotalTimeMs;
                            blackPeriodMovesLeft = timeControl.MovesPerPeriod;
                        }
                    }
                }

                // Track elapsed for display
                if (whiteToMove)
                    whiteElapsedThisMove = moveTimeMs;
                else
                    blackElapsedThisMove = moveTimeMs;

                // A null move can mean two very different things: the engine genuinely has
                // no legal move (real checkmate/stalemate), or it timed out/crashed/died
                // mid-search (GetMoveForMatchAsync sets State = Error in that case). Only
                // trust the checkmate/stalemate read when the engine is still healthy —
                // otherwise this is a forfeit, not a drawn or lost-on-the-board game.
                if (string.IsNullOrEmpty(bestMove))
                {
                    if (activeEngine.State == EngineState.Error || !activeEngine.IsEngineAlive())
                    {
                        LogM($"ENGINE TIMEOUT/CRASH: {side} ({activeName}) returned no move while unhealthy — EndMatch(EngineError)");
                        EndMatch(MatchTermination.EngineError,
                            whiteToMove ? MatchOutcome.BlackWins : MatchOutcome.WhiteWins);
                        return;
                    }

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

                // Sanity check: the piece being moved must belong to the side to move.
                // If violated, the engine played a piece it doesn't own — corrupted board or engine bug.
                bool wrongColor = movingPiece == '.' ||
                                  (whiteToMove  && char.IsLower(movingPiece)) ||
                                  (!whiteToMove && char.IsUpper(movingPiece));
                if (wrongColor)
                {
                    Debug.WriteLine($"[Match] ILLEGAL MOVE DETECTED: {activeName} played {bestMove} " +
                                    $"(piece='{movingPiece}', side={(whiteToMove ? "White" : "Black")}) in FEN: {fen}");
                    EndMatch(MatchTermination.EngineError,
                        whiteToMove ? MatchOutcome.BlackWins : MatchOutcome.WhiteWins);
                    return;
                }

                char capturedPiece = GetCapturedPiece(bestMove);
                bool isPawnMove = char.ToLowerInvariant(movingPiece) == 'p';
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

                // Stop the previous annotation (it was running while the engine thought).
                // Drain completes in ~50ms (stop + bestmove). Fast enough to be imperceptible.
                if (_annotCts != null && _annotTask != null)
                {
                    _annotCts.Cancel();
                    try { await _annotTask; } catch { }
                    _annotCts.Dispose();
                    _annotCts = null;
                    _annotTask = null;
                }

                string newFen = gameState.ToCompleteFEN();
                string? moveEval = null;

                if (AnnotatorEngine?.State == EngineState.Ready)
                {
                    // Start continuous annotation — fires OnAnnotatorEvalUpdated at each depth.
                    // Wait briefly for the first result (depth ~6, arrives in ~20ms) so the
                    // console output gets an eval. Then annotation keeps running in background
                    // while the next engine thinks.
                    var firstEvalTcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _annotCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                    _annotTask = AnnotatorEngine.StreamPositionEvalAsync(
                        newFen,
                        config.EngineDepth,
                        eval =>
                        {
                            firstEvalTcs.TrySetResult(eval);
                            OnAnnotatorEvalUpdated?.Invoke(eval);
                        },
                        _annotCts.Token);

                    try
                    {
                        using var quickCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        quickCts.CancelAfter(300);
                        moveEval = await firstEvalTcs.Task.WaitAsync(quickCts.Token);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch { /* 300ms timeout — proceed with null eval */ }
                }

                OnMovePlayed?.Invoke(bestMove, newFen, moveTimeMs, moveEval);
                OnClockUpdated?.Invoke(whiteRemainingMs, blackRemainingMs, gameState.WhiteToMove);

                // Adjudication
                if (AdjudicationEnabled && TryCheckAdjudication(moveEval, out var adjTermination, out var adjOutcome))
                {
                    EndMatch(adjTermination, adjOutcome);
                    return;
                }

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

                // Wait for the board animation to finish before requesting the next move.
                // Falls back to a 700ms safety timeout so the match never hangs if the
                // AnimationCompleted event is somehow missed.
                if (WaitForAnimation)
                {
                    _animTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    try { await _animTcs.Task.WaitAsync(TimeSpan.FromMilliseconds(700), ct); }
                    catch (TimeoutException) { }
                    _animTcs = null;
                }
                else
                {
                    await Task.Delay(30, ct);
                }
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
                if (char.ToLowerInvariant(moving) == 'p' && uciMove[0] != uciMove[2])
                {
                    // Diagonal pawn move to empty square = en passant
                    return gameState.WhiteToMove ? 'p' : 'P';
                }
            }

            return target;
        }

        private bool TryCheckAdjudication(string? evalStr, out MatchTermination termination, out MatchOutcome outcome)
        {
            termination = MatchTermination.None;
            outcome = MatchOutcome.Interrupted;

            int? cp = ParseEvalCp(evalStr);
            if (cp == null) return false;

            _adjEvalHistory.Add(cp.Value);
            if (_adjEvalHistory.Count > AdjudicatePlies)
                _adjEvalHistory.RemoveAt(0);

            if (_adjEvalHistory.Count < AdjudicatePlies) return false;

            // Resign: all recent evals heavily favour one side
            if (_adjEvalHistory.All(e => e >= AdjudicateResignCp))
            {
                termination = MatchTermination.AdjudicationResign;
                outcome = MatchOutcome.WhiteWins;
                return true;
            }
            if (_adjEvalHistory.All(e => e <= -AdjudicateResignCp))
            {
                termination = MatchTermination.AdjudicationResign;
                outcome = MatchOutcome.BlackWins;
                return true;
            }

            // Draw: all recent evals are near zero
            if (_adjEvalHistory.All(e => Math.Abs(e) <= AdjudicateDrawCp))
            {
                termination = MatchTermination.AdjudicationDraw;
                outcome = MatchOutcome.Draw;
                return true;
            }

            return false;
        }

        private static int? ParseEvalCp(string? eval)
        {
            if (string.IsNullOrEmpty(eval)) return null;

            // Mate scores: "#3" or "#-3"
            if (eval.StartsWith('#'))
            {
                if (int.TryParse(eval.AsSpan(1), out int mate))
                    return mate >= 0 ? 30000 : -30000;
                return null;
            }

            // Mate scores from StreamPositionEvalAsync: "Mate in +3" or "Mate in -5"
            if (eval.StartsWith("Mate in ", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(eval.AsSpan(8), out int mate))
                    return mate >= 0 ? 30000 : -30000;
                return null;
            }

            // Centipawn scores: "+2.50" or "-3.00"
            if (double.TryParse(eval.TrimStart('+'), System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out double pawns))
                return (int)(pawns * 100);

            return null;
        }

        private void EndMatch(MatchTermination termination, MatchOutcome outcome)
        {
            isRunning = false;
            LogM($"=== END MATCH: termination={termination} outcome={outcome} moves={moveCount} ===");
            _annotCts?.Cancel();

            var result = new EngineMatchResult
            {
                Outcome = outcome,
                Termination = termination,
                TotalMoves = moveCount,
                WhiteTimeRemainingMs = whiteRemainingMs,
                BlackTimeRemainingMs = blackRemainingMs,
                TimeControl = timeControl.Type
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

        // CHESSDROID_AUDIT.md §6.3: was an independent copy; redirected onto MoveGenerator's
        // (Phase 4 slice 1, perft-verified) after characterization tests (IsKingInCheckConsistencyTests,
        // in ChessDroid.Tests) confirmed the other 3 independent copies already agreed everywhere.
        private static bool IsKingInCheck(ChessBoard board, bool whiteKing) => MoveGenerator.IsKingInCheck(board, whiteKing);

        /// <summary>
        /// Checks if the side to move has at least one legal move. Returns false if the
        /// position is checkmate or stalemate. This used to be a third independent
        /// reimplementation of legal-move detection (per-piece-type candidate generation +
        /// its own check-simulation) parallel to ChessBoardControl's and
        /// ChessRulesService.HasAnyLegalMove's — the latter was fixed/deleted in Phase 4
        /// slice 3 for never generating castling moves; this one had the same castling gap
        /// (though structurally harmless: castling's legality preconditions on the
        /// intermediate square are a strict subset of a plain one-square king move's, so if
        /// castling is legal a king step is too — there's always another move). Delegates to
        /// MoveGenerator (Phase 4 slice 1) instead of maintaining a third copy.
        /// </summary>
        private bool HasAnyLegalMove()
        {
            var castling = new CastlingState(
                WhiteKingside: gameState.CastlingRights.Contains('K'),
                WhiteQueenside: gameState.CastlingRights.Contains('Q'),
                BlackKingside: gameState.CastlingRights.Contains('k'),
                BlackQueenside: gameState.CastlingRights.Contains('q'));

            string ep = gameState.EnPassantTarget;
            (int Row, int Col)? enPassant = (!string.IsNullOrEmpty(ep) && ep != "-" && ep.Length == 2)
                ? (8 - (ep[1] - '0'), ep[0] - 'a')
                : null;

            return MoveGenerator.GetAllLegalMovesUci(gameState.Board, gameState.WhiteToMove, castling, enPassant).Count > 0;
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
                var nonKing = pieces.First(p => char.ToLowerInvariant(p.piece) != 'k');
                char type = char.ToLowerInvariant(nonKing.piece);
                if (type == 'n' || type == 'b') return true;
            }

            // K+B vs K+B same color bishops
            if (pieces.Count == 4)
            {
                var bishops = pieces.Where(p => char.ToLowerInvariant(p.piece) == 'b').ToList();
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
            _annotCts?.Cancel();
            _annotCts?.Dispose();
            try { _matchLog?.Close(); } catch { }
            _matchLog = null;

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

            GC.SuppressFinalize(this);
        }
    }
}
