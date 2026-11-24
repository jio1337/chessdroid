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
            if (_botModeActive) { StopBotMode(); return; }
            if (matchRunning) { lblStatus.Text = "Stop the engine match first"; return; }
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
            if (dialog.ShowDialog() != DialogResult.OK) return;

            var controller = new BotModeController(this);
            CurrentModeController = controller;
            await controller.Enter(dialog.Settings, resetBoard: true);
        }

        // Called by BtnPlayBot_Click (resetBoard=true) and Chess960 browser (resetBoard=false).
        internal async Task StartBotEngineAsync(BotSettings settings, bool resetBoard)
        {
            _botSettings = settings;
            CancelClassification();
            autoAnalysisCts?.Cancel();

            _botModeActive = true;
            if (CurrentModeController is not BotModeController) CurrentModeController = new BotModeController(this);
            SetBotControlsEnabled(true);
            boardControl.ClearEngineArrows();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;

            if (resetBoard)
            {
                boardControl.ResetBoard();
                ResetPositionState(boardControl.GetFEN());
                _currentClassification = null;
                _classificationLookup = null;
                consoleFormatter?.SetActiveClassification(null);
            }

            _botPositionCounts.Clear();
            _botPositionCounts[GetPositionKey(boardControl.GetFEN())] = 1;

            bool userPlaysBlack = settings.BotPlaysWhite;
            if (userPlaysBlack && !boardControl.IsFlipped)
                boardControl.FlipBoard();
            else if (!userPlaysBlack && boardControl.IsFlipped)
                boardControl.FlipBoard();

            try
            {
                lblStatus.Text = "Starting bot engine...";
                string enginesPath = config!.GetEnginesPath();
                string engineFile = !string.IsNullOrEmpty(settings.EngineFileName)
                    ? settings.EngineFileName : config.SelectedEngine;

                _botEngine = new ChessEngineService(config);
                await _botEngine.InitializeAsync(Path.Combine(enginesPath, engineFile));

                if (_botEngine.State != EngineState.Ready)
                {
                    lblStatus.Text = "Failed to start bot engine";
                    _botEngine.Dispose(); _botEngine = null;
                    _botModeActive = false; SetBotControlsEnabled(false);
                    return;
                }

                await _botEngine.SetEloTargetAsync(settings.EloTarget, settings.GetSkillLevel());

                if (!_botEngine.SupportsEloTargeting)
                    MessageBox.Show(
                        $"{_botEngine.EngineName} doesn't support Elo targeting (UCI_LimitStrength).\n" +
                        "The engine will play at full strength regardless of the Elo setting.",
                        "Elo Targeting Not Supported",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (_chess960Active)
                    await _botEngine.SetChess960Async(true);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Bot engine error: {ex.Message}";
                _botEngine?.Dispose(); _botEngine = null;
                _botModeActive = false; SetBotControlsEnabled(false);
                return;
            }

            _botMoveCts = new CancellationTokenSource();
            btnPlayBot.Text = "⏹";
            toolTip.SetToolTip(btnPlayBot, "Stop Bot");
            boardControl.InteractionEnabled = true;

            if (settings.ChallengeMode) ApplyChallengeMode();

            string diffLabel  = GetBotDifficultyLabel();
            string colorLabel = userPlaysBlack ? "Black" : "White";
            string typeLabel  = settings.ChallengeMode ? "Challenge" : "Friendly";
            analysisOutput.AppendText($"Bot Mode: You play {colorLabel}\n");
            analysisOutput.AppendText($"Difficulty: {diffLabel}  |  {typeLabel}\n\n");
            lblStatus.Text = $"Bot mode — {diffLabel}";

            if (settings.BotPlaysWhite)
                _botMoveTask = MakeBotMoveAsync();
            else
                _ = TriggerAutoAnalysis();
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
                    // A timed-out or crashed engine also returns a null move, but that is
                    // not the same thing as "no legal move" — don't let a hung bot score
                    // itself a stalemate/checkmate it never actually reached on the board.
                    if (_botEngine.State == EngineState.Error || !_botEngine.IsEngineAlive())
                    {
                        HandleBotGameEnd(fen, "Bot error — engine timed out or crashed");
                        return;
                    }

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
                lblStatus.Text = $"Your turn — {GetBotDifficultyLabel()}";

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

        private string? CheckBotDrawConditions(string fen)
        {
            var parts = fen.Split(' ');
            if (parts.Length >= 1 && ChessRulesService.IsInsufficientMaterial(parts[0]))
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
            _botMoveCts?.Cancel();

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
            SetBotControlsEnabled(false);

            _botEngine?.Dispose();
            _botEngine = null;

            if (_drillBotActive)
            {
                _drillBotActive = false;
                RestoreChallengeSnapshot(triggerAnalysis: false);
                SetDrillControlsEnabled(true);
                if (_trainingPanel._btnDrillVsBot != null) _trainingPanel._btnDrillVsBot.Text = "⚔ Practice vs Bot";
                if (CurrentModeController is TrainingModeController) CurrentModeController = null;
            }
            else
            {
                RestoreChallengeSnapshot();
            }
            lblStatus.Text = result;
        }

        private bool IsSideInCheck(bool whiteKing)
        {
            var board = boardControl.GetBoardState();
            return board != null && ChessRulesService.IsKingInCheck(board, whiteKing);
        }

        private bool HasAnyLegalMoveFromFen(string fen)
        {
            var board = boardControl.GetBoardState();
            if (board == null) return true;

            // ChessRulesService.HasAnyLegalMove never generates castling moves and its
            // en-passant handling is broken, so a position whose only escape is a castle or
            // an en-passant capture was misreported as stalemate/checkmate. MoveGenerator
            // (Phase 4 slice 1) is the complete, perft-verified implementation — use it here
            // instead of fixing HasAnyLegalMove itself, which has no other callers left.
            var fenParts = fen.Split(' ');
            bool whiteToMove = fenParts.Length >= 2 && fenParts[1] == "w";
            string castlingStr = fenParts.Length >= 3 ? fenParts[2] : "-";
            string epStr = fenParts.Length >= 4 ? fenParts[3] : "-";

            var castling = new CastlingState(
                WhiteKingside: castlingStr.Contains('K'),
                WhiteQueenside: castlingStr.Contains('Q'),
                BlackKingside: castlingStr.Contains('k'),
                BlackQueenside: castlingStr.Contains('q'));

            (int Row, int Col)? enPassant = (epStr != "-" && epStr.Length == 2)
                ? (8 - (epStr[1] - '0'), epStr[0] - 'a')
                : null;

            return MoveGenerator.GetAllLegalMovesUci(board, whiteToMove, castling, enPassant).Count > 0;
        }

        private string GetBotDifficultyLabel()
        {
            if (_botEngine?.SupportsEloTargeting == true)
                return _botSettings?.GetDifficultyLabel() ?? "Unknown";

            // Engine plays at full strength — show its CCRL rating instead of the Elo target
            string engineKey = !string.IsNullOrEmpty(_botSettings?.EngineFileName)
                ? _botSettings!.EngineFileName : config!.SelectedEngine;
            if (config?.EngineProfiles.TryGetValue(engineKey, out var profile) == true && profile.Elo > 0)
                return $"~{profile.Elo} CCRL";
            return "full strength";
        }

        /// <summary>
        /// IModeController wrapper for Bot mode — the one mode with an achievable clean
        /// Enter/Exit pair (see Modes/IModeController.cs for why Match/Training don't get
        /// one). Enter isn't on the shared interface since it needs the already-resolved
        /// BotSettingsDialog result as a parameter, which Match/Training can't supply
        /// uniformly; it lives here as a Bot-specific method instead.
        /// </summary>
        private sealed class BotModeController : Modes.IModeController
        {
            private readonly AnalysisBoardForm _form;
            public BotModeController(AnalysisBoardForm form) => _form = form;
            public Task Enter(BotSettings settings, bool resetBoard) => _form.StartBotEngineAsync(settings, resetBoard);
            public void ExitMode() => _form.StopBotMode();
        }

        private void StopBotMode()
        {
            _botMoveCts?.Cancel();
            _botModeActive = false;
            if (CurrentModeController is BotModeController) CurrentModeController = null;

            // MakeBotMoveAsync may still be mid-await on _botEngine.GetMoveForMatchAsync
            // when this runs. Disposing the engine immediately races that read loop —
            // Dispose() closing the process/streams can surface as ObjectDisposedException
            // instead of a clean OperationCanceledException. Null the field out now so
            // nothing new picks this engine up, but defer actually disposing it until the
            // in-flight task (if any) has unwound from the cancellation.
            var engineToDispose = _botEngine;
            var moveTaskToAwait = _botMoveTask;
            _botEngine = null;
            _botSettings = null;
            _ = DisposeBotEngineAsync(engineToDispose, moveTaskToAwait);

            btnPlayBot.Text = "♞";
            toolTip.SetToolTip(btnPlayBot, "Play vs Bot");
            SetBotControlsEnabled(false);
            boardControl.InteractionEnabled = true;

            if (_drillBotActive)
            {
                _drillBotActive = false;
                RestoreChallengeSnapshot(triggerAnalysis: false);
                SetDrillControlsEnabled(true);
                if (_trainingPanel._btnDrillVsBot != null) _trainingPanel._btnDrillVsBot.Text = "⚔ Practice vs Bot";
                if (CurrentModeController is TrainingModeController) CurrentModeController = null;
                lblStatus.Text = "Drill stopped";
            }
            else
            {
                RestoreChallengeSnapshot();
            }
        }

        private static async Task DisposeBotEngineAsync(ChessEngineService? engine, Task? moveTask)
        {
            if (moveTask != null)
            {
                try { await moveTask; }
                catch { /* expected: cancellation or an engine fault we already logged */ }
            }
            engine?.Dispose();
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
