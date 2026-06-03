using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    public partial class AnalysisBoardForm : Form
    {
        #region Bot Mode

        private async void BtnPlayBot_Click(object? sender, EventArgs e)
        {
            if (_botModeActive)
            {
                StopBotMode();
                return;
            }

            if (matchRunning)
            {
                lblStatus.Text = "Stop the engine match first";
                return;
            }

            if (string.IsNullOrEmpty(config?.SelectedEngine))
            {
                lblStatus.Text = "No engine configured — click ⚙ to set up";
                return;
            }

            string[] availableEngines = Directory.Exists(config.GetEnginesPath())
                ? Directory.GetFiles(config.GetEnginesPath(), "*.exe").Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToArray()
                : Array.Empty<string>();
            using var dialog = new BotSettingsDialog(ThemeService.IsDarkTheme(config?.Theme),
                availableEngines, config?.EngineProfiles ?? new(), config?.SelectedEngine ?? "");
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            _botSettings = dialog.Settings;

            // Reset the board for a new game
            CancelClassification();
            _botModeActive = true;       // block TriggerAutoAnalysis before any await
            SetBotControlsEnabled(true); // lock sidebar buttons immediately
            boardControl.ClearEngineArrows();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            boardControl.ResetBoard();
            moveTree.Clear(boardControl.GetFEN());
            _botPositionCounts.Clear();
            _botPositionCounts[GetPositionKey(boardControl.GetFEN())] = 1;
            moveListBox.Items.Clear();
            displayedNodes.Clear();
            _analysisCache.Clear();
            _currentClassification = null;
            _classificationLookup = null;
            consoleFormatter?.SetActiveClassification(null);
            analysisOutput.Clear();
            evalBar?.Reset();

            // Flip board if user plays Black
            bool userPlaysBlack = _botSettings.BotPlaysWhite;
            if (userPlaysBlack && !boardControl.IsFlipped)
                boardControl.FlipBoard();
            else if (!userPlaysBlack && boardControl.IsFlipped)
                boardControl.FlipBoard();

            // Initialize bot engine
            try
            {
                lblStatus.Text = "Starting bot engine...";
                string enginesPath = config!.GetEnginesPath();
                string engineFile = !string.IsNullOrEmpty(_botSettings.EngineFileName)
                    ? _botSettings.EngineFileName : config.SelectedEngine;
                string enginePath = Path.Combine(enginesPath, engineFile);

                _botEngine = new ChessEngineService(config);
                await _botEngine.InitializeAsync(enginePath);

                if (_botEngine.State != EngineState.Ready)
                {
                    lblStatus.Text = "Failed to start bot engine";
                    _botEngine.Dispose();
                    _botEngine = null;
                    _botModeActive = false;
                    SetBotControlsEnabled(false);
                    return;
                }

                // Set skill level
                await _botEngine.SetEloTargetAsync(_botSettings.EloTarget, _botSettings.GetSkillLevel());
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Bot engine error: {ex.Message}";
                _botEngine?.Dispose();
                _botEngine = null;
                _botModeActive = false;
                SetBotControlsEnabled(false);
                return;
            }

            _botMoveCts = new CancellationTokenSource();
            btnPlayBot.Text = "⏹";
            toolTip.SetToolTip(btnPlayBot, "Stop Bot");
            boardControl.InteractionEnabled = true;

            if (_botSettings.ChallengeMode)
                ApplyChallengeMode();

            string diffLabel = _botSettings.GetDifficultyLabel();
            string colorLabel = userPlaysBlack ? "Black" : "White";
            string typeLabel = _botSettings.ChallengeMode ? "Challenge" : "Friendly";
            analysisOutput.AppendText($"Bot Mode: You play {colorLabel}\n");
            analysisOutput.AppendText($"Difficulty: {diffLabel}  |  {typeLabel}\n\n");
            lblStatus.Text = $"Bot mode — {diffLabel}";

            // If bot plays White, make the first move
            if (_botSettings.BotPlaysWhite)
            {
                _ = MakeBotMoveAsync();
            }
            else
            {
                // Trigger analysis for the starting position
                _ = TriggerAutoAnalysis();
            }
        }

        private async Task MakeBotMoveAsync()
        {
            if (!_botModeActive || _botEngine == null || _botSettings == null)
                return;

            // Brief delay so user can see their move before bot responds
            try
            {
                await Task.Delay(300, _botMoveCts?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException) { return; }

            boardControl.InteractionEnabled = false;
            lblStatus.Text = "Bot thinking...";

            try
            {
                string fen = boardControl.GetFEN();
                string goCommand = $"go movetime {_botSettings.GetMoveTimeMs()}";
                int timeoutMs = _botSettings.GetMoveTimeMs() + 5000;
                var token = _botMoveCts?.Token ?? CancellationToken.None;

                var (bestMove, eval) = await _botEngine.GetMoveForMatchAsync(fen, goCommand, timeoutMs, token);

                if (string.IsNullOrEmpty(bestMove))
                {
                    // No legal moves — game over
                    HandleBotGameEnd(fen);
                    return;
                }

                // Apply bot's move to the board
                isNavigating = true;
                try
                {
                    string fenBeforeMove = moveTree.CurrentNode.FEN;
                    boardControl.MakeMove(bestMove);
                    string san = ConvertUciToSan(bestMove, fenBeforeMove);
                    if (config?.ShowAnimations == true)
                        boardControl.StartAnimation(bestMove);
                    PlayMoveSound(san.Contains('x'), san);
                    string newFen = boardControl.GetFEN();
                    moveTree.AddMove(bestMove, san, newFen);

                    UpdateMoveList();
                    UpdateFenDisplay();
                    UpdateTurnLabel();

                    if (!string.IsNullOrEmpty(eval))
                        UpdateEvalBar(eval);
                }
                finally
                {
                    isNavigating = false;
                }

                // Check if user has any legal moves after bot's move
                string currentFen = boardControl.GetFEN();
                if (!HasAnyLegalMoveFromFen(currentFen))
                {
                    HandleBotGameEnd(currentFen);
                    return;
                }

                string? draw = CheckBotDrawConditions(currentFen);
                if (draw != null) { HandleBotGameEnd(currentFen, draw); return; }

                boardControl.InteractionEnabled = true;
                string diffLabel = _botSettings.GetDifficultyLabel();
                lblStatus.Text = $"Your turn — {diffLabel}";

                // Trigger analysis for the new position (human's turn)
                _ = TriggerAutoAnalysis();
            }
            catch (OperationCanceledException)
            {
                // Bot move was cancelled (take back, stop, etc.)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BotMove error: {ex.Message}");
                lblStatus.Text = "Bot error — try again";
                boardControl.InteractionEnabled = true;
            }
        }

        private static bool IsInsufficientMaterial(string fenPosition)
        {
            var pieces = fenPosition.Where(char.IsLetter).ToList();
            if (pieces.Count == 2) return true; // KvK
            if (pieces.Count == 3) return pieces.All(c => "KkBbNn".Contains(c)); // K+minor vs K
            return false;
        }

        private string? CheckBotDrawConditions(string fen)
        {
            var parts = fen.Split(' ');
            if (parts.Length >= 1 && IsInsufficientMaterial(parts[0]))
                return "Draw — insufficient material";
            if (parts.Length >= 5 && int.TryParse(parts[4], out int halfMoves) && halfMoves >= 100)
                return "Draw — 50-move rule";
            string posKey = GetPositionKey(fen);
            _botPositionCounts.TryGetValue(posKey, out int count);
            _botPositionCounts[posKey] = count + 1;
            if (_botPositionCounts[posKey] >= 3)
                return "Draw — threefold repetition";
            return null;
        }

        private void HandleBotGameEnd(string fen, string? forcedResult = null)
        {
            _botModeActive = false;

            // Determine result by checking if king is in check
            var fenParts = fen.Split(' ');
            bool whiteToMove = fenParts.Length >= 2 && fenParts[1] == "w";

            bool inCheck = false;
            string result;
            if (forcedResult != null)
            {
                result = forcedResult;
            }
            else
            {
                try { inCheck = IsSideInCheck(whiteToMove); } catch { }
                if (inCheck)
                {
                    bool botWins = (_botSettings?.BotPlaysWhite == true && !whiteToMove) ||
                                   (_botSettings?.BotPlaysWhite == false && whiteToMove);
                    result = botWins ? "Checkmate — Bot wins!" : "Checkmate — You win!";
                }
                else
                {
                    result = "Stalemate — Draw!";
                }
            }

            analysisOutput.AppendText($"\n{result}\n");
            if (inCheck) boardControl.TriggerParticles();
            PlayGameEndSound();
            boardControl.InteractionEnabled = false;
            btnPlayBot.Text = "♞";
            toolTip.SetToolTip(btnPlayBot, "Play vs Bot");
            btnStartMatch.Enabled = true;

            _botEngine?.Dispose();
            _botEngine = null;

            if (_drillBotActive)
            {
                _drillBotActive = false;
                RestoreChallengeSnapshot(triggerAnalysis: false);
                SetDrillControlsEnabled(true);
                if (_btnDrillVsBot != null) _btnDrillVsBot.Text = "⚔ Practice vs Bot";
            }
            else
            {
                RestoreChallengeSnapshot();
            }
            lblStatus.Text = result;
        }

        private bool IsSideInCheck(bool whiteKing)
        {
            // Check if the king of the given color is in check on the current board
            var board = boardControl.GetBoardState();
            if (board == null) return false;

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

            // Check if any enemy piece attacks the king
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;
                    bool isEnemy = whiteKing ? char.IsLower(piece) : char.IsUpper(piece);
                    if (isEnemy && ChessRulesService.CanReachSquare(board, r, c, piece, kingRow, kingCol))
                    {
                        // For sliding pieces, also check path is clear
                        char pl = char.ToLower(piece);
                        if (pl == 'n' || pl == 'k' || pl == 'p')
                        {
                            if (pl == 'p' && c == kingCol) continue; // Pawns don't attack forward
                            return true;
                        }
                        // Sliding piece — check path
                        int dr = Math.Sign(kingRow - r);
                        int dc = Math.Sign(kingCol - c);
                        int cr = r + dr, cc = c + dc;
                        bool pathClear = true;
                        while (cr != kingRow || cc != kingCol)
                        {
                            if (board.GetPiece(cr, cc) != '.') { pathClear = false; break; }
                            cr += dr; cc += dc;
                        }
                        if (pathClear) return true;
                    }
                }
            }
            return false;
        }

        private bool HasAnyLegalMoveFromFen(string fen)
        {
            // Quick check: ask the board control if the current side has any legal moves
            // by checking all pieces of the current side
            var fenParts = fen.Split(' ');
            bool whiteToMove = fenParts.Length >= 2 && fenParts[1] == "w";
            var board = boardControl.GetBoardState();
            if (board == null) return true;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;
                    bool isOwn = whiteToMove ? char.IsUpper(piece) : char.IsLower(piece);
                    if (!isOwn) continue;

                    // Check all target squares for this piece
                    for (int tr = 0; tr < 8; tr++)
                    {
                        for (int tc = 0; tc < 8; tc++)
                        {
                            if (r == tr && c == tc) continue;
                            char target = board.GetPiece(tr, tc);
                            // Can't capture own piece
                            if (target != '.' && ((whiteToMove && char.IsUpper(target)) ||
                                                   (!whiteToMove && char.IsLower(target))))
                                continue;

                            if (ChessRulesService.CanReachSquare(board, r, c, piece, tr, tc))
                            {
                                // Verify path is clear for sliding pieces
                                char pl = char.ToLower(piece);
                                if (pl != 'n' && pl != 'k' && pl != 'p')
                                {
                                    int dr = Math.Sign(tr - r);
                                    int dc = Math.Sign(tc - c);
                                    int cr = r + dr, cc = c + dc;
                                    bool blocked = false;
                                    while (cr != tr || cc != tc)
                                    {
                                        if (board.GetPiece(cr, cc) != '.') { blocked = true; break; }
                                        cr += dr; cc += dc;
                                    }
                                    if (blocked) continue;
                                }

                                // Simulate the move and check if king is safe
                                using var pooled = BoardPool.Rent(board);
                                var testBoard = pooled.Board;
                                testBoard.SetPiece(tr, tc, piece);
                                testBoard.SetPiece(r, c, '.');

                                // Check en passant capture
                                if (pl == 'p' && c != tc && target == '.')
                                    testBoard.SetPiece(r, tc, '.');

                                if (!IsKingInCheckOnBoard(testBoard, whiteToMove))
                                    return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool IsKingInCheckOnBoard(ChessBoard testBoard, bool whiteKing)
        {
            char king = whiteKing ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (testBoard.GetPiece(r, c) == king)
                    {
                        kingRow = r; kingCol = c; break;
                    }
                }
                if (kingRow != -1) break;
            }
            if (kingRow == -1) return false;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = testBoard.GetPiece(r, c);
                    if (piece == '.') continue;
                    bool isEnemy = whiteKing ? char.IsLower(piece) : char.IsUpper(piece);
                    if (!isEnemy) continue;

                    if (!ChessRulesService.CanReachSquare(testBoard, r, c, piece, kingRow, kingCol))
                        continue;

                    char pl = char.ToLower(piece);
                    if (pl == 'n' || pl == 'k')
                        return true;
                    if (pl == 'p')
                        return c != kingCol; // Pawns only attack diagonally

                    // Sliding piece path check
                    int dr = Math.Sign(kingRow - r);
                    int dc = Math.Sign(kingCol - c);
                    int cr = r + dr, cc = c + dc;
                    bool pathClear = true;
                    while (cr != kingRow || cc != kingCol)
                    {
                        if (testBoard.GetPiece(cr, cc) != '.') { pathClear = false; break; }
                        cr += dr; cc += dc;
                    }
                    if (pathClear) return true;
                }
            }
            return false;
        }

        private void StopBotMode()
        {
            _botMoveCts?.Cancel();
            _botModeActive = false;

            _botEngine?.Dispose();
            _botEngine = null;
            _botSettings = null;

            btnPlayBot.Text = "♞";
            toolTip.SetToolTip(btnPlayBot, "Play vs Bot");
            SetBotControlsEnabled(false);
            boardControl.InteractionEnabled = true;

            if (_drillBotActive)
            {
                _drillBotActive = false;
                RestoreChallengeSnapshot(triggerAnalysis: false);
                SetDrillControlsEnabled(true);
                if (_btnDrillVsBot != null) _btnDrillVsBot.Text = "⚔ Practice vs Bot";
                lblStatus.Text = "Drill stopped";
            }
            else
            {
                RestoreChallengeSnapshot();
            }
        }

        private void RestoreChallengeSnapshot(bool triggerAnalysis = true)
        {
            if (_challengeSnapshot == null) return;
            config?.CopyFrom(_challengeSnapshot);
            _challengeSnapshot = null;
            ApplyTheme();
            ApplyConsoleFont();
            LeftPanel_Resize(leftPanel, EventArgs.Empty);
            evalBar?.Reset();
            if (triggerAnalysis)
            {
                lblStatus.Text = "Bot mode stopped — analysis restored";
                _ = TriggerAutoAnalysis();
            }
        }

        private void ApplyChallengeMode()
        {
            if (config == null) return;
            _challengeSnapshot = new AppConfig();
            _challengeSnapshot.CopyFrom(config);

            config.ShowBestLine = false;
            config.ShowSecondLine = false;
            config.ShowThirdLine = false;
            config.ShowEngineArrows = false;
            config.ShowEvalBar = false;
            LeftPanel_Resize(leftPanel, EventArgs.Empty);
            config.ShowTacticalAnalysis = false;
            config.ShowPositionalAnalysis = false;
            config.ShowEndgameAnalysis = false;
            config.ShowOpeningPrinciples = false;
            config.ShowThreats = false;
            config.ShowWDL = false;
            config.PlayStyleEnabled = false;
            config.ShowOpeningName = false;
            config.ShowMoveQuality = false;
            config.ContinuousAnalysis = false;
            config.ShowBookMoves = false;

            boardControl.ClearEngineArrows();
            evalBar?.Reset();
            analysisOutput.Clear();
        }

        #endregion
    }
}
