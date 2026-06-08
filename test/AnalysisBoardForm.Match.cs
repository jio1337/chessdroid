using System.Text.RegularExpressions;
using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    public partial class AnalysisBoardForm : Form
    {
        #region Engine Match

        private void CmbTimeControlType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateTimeControlParams();
        }

        private void UpdateTimeControlParams()
        {
            int idx = cmbTimeControlType.SelectedIndex;

            // Fixed Depth
            lblDepth.Visible = idx == 0;
            numDepth.Visible = idx == 0;

            // Time per Move
            lblMoveTime.Visible = idx == 1;
            numMoveTime.Visible = idx == 1;

            // Total + Increment
            lblTotalTime.Visible = idx == 2;
            numTotalTime.Visible = idx == 2;
            lblIncrement.Visible = idx == 2;
            numIncrement.Visible = idx == 2;
        }

        private async void BtnStartMatch_Click(object? sender, EventArgs e)
        {
            if (_botModeActive)
            {
                lblStatus.Text = "Stop bot mode first";
                return;
            }

            if (cmbWhiteEngine.SelectedIndex < 0 || cmbBlackEngine.SelectedIndex < 0 ||
                cmbWhiteEngine.SelectedIndex >= _matchEngineFiles.Length ||
                cmbBlackEngine.SelectedIndex >= _matchEngineFiles.Length)
            {
                lblStatus.Text = "Select both engines first";
                return;
            }

            string whiteEngineName = _matchEngineFiles[cmbWhiteEngine.SelectedIndex];
            string blackEngineName = _matchEngineFiles[cmbBlackEngine.SelectedIndex];
            _matchWhiteName = Path.GetFileNameWithoutExtension(whiteEngineName);
            _matchBlackName = Path.GetFileNameWithoutExtension(blackEngineName);

            // Load engine profiles for ELO display
            config.EngineProfiles.TryGetValue(whiteEngineName, out var whiteProfile);
            config.EngineProfiles.TryGetValue(blackEngineName, out var blackProfile);
            _matchWhiteElo = whiteProfile?.Elo ?? 0;
            _matchBlackElo = blackProfile?.Elo ?? 0;
            _matchWhiteFileName = whiteEngineName;
            _matchBlackFileName = blackEngineName;
            SetEngineInfoLabels(whiteProfile, blackProfile, whiteEngineName, blackEngineName);

            // Series init
            _seriesTotal         = (int)numGames.Value;
            _seriesPlayed        = 0;
            _seriesEng1Score     = 0;
            _seriesEng2Score     = 0;
            _seriesEng1File      = whiteEngineName;
            _seriesEng2File      = blackEngineName;
            _seriesCurrentWhiteFile = whiteEngineName;
            lblSeriesScore.Text  = "";

            // Resolve engine paths
            string enginesPath = config.GetEnginesPath();
            string whiteEnginePath = Path.Combine(enginesPath, whiteEngineName);
            string blackEnginePath = Path.Combine(enginesPath, blackEngineName);

            if (!File.Exists(whiteEnginePath) || !File.Exists(blackEnginePath))
            {
                lblStatus.Text = "Engine file not found";
                return;
            }

            // Build time control from UI
            var tc = new EngineMatchTimeControl();
            switch (cmbTimeControlType.SelectedIndex)
            {
                case 0: // Fixed Depth
                    tc.Type = TimeControlType.FixedDepth;
                    tc.Depth = (int)numDepth.Value;
                    break;
                case 1: // Time per Move
                    tc.Type = TimeControlType.FixedTimePerMove;
                    tc.MoveTimeMs = (int)numMoveTime.Value;
                    break;
                case 2: // Total + Increment
                    tc.Type = TimeControlType.TotalPlusIncrement;
                    tc.TotalTimeMs = (int)numTotalTime.Value * 1000;
                    tc.IncrementMs = (int)numIncrement.Value * 1000;
                    break;
            }

            // Get starting FEN - either current position or standard starting position
            string startFen;
            if (chkFromPosition.Checked)
            {
                // Use current board position
                startFen = boardControl.GetFEN();
            }
            else
            {
                // Reset to standard starting position
                boardControl.ResetBoard();
                startFen = boardControl.GetFEN();
            }
            _seriesStartFen = startFen; // remember for game 2+ in a series
            // Opening book injection (only from standard start, not custom position)
            bool bookReady = chkUseBook.Checked && !chkFromPosition.Checked &&
                             (rbBookChoose.Checked ? _matchBookOpening != null : openingBookService?.IsLoaded == true);
            bool chooseMode = bookReady && rbBookChoose.Checked && _matchBookOpening != null;
            if (bookReady) startFen = GetBookStartFen(startFen);

            CancelClassification();
            if (!chooseMode)
                moveTree.Clear(startFen);
            moveListBox.Items.Clear();
            _movePairs.Clear();
            _analysisCache.Clear(); // Clear analysis cache for new match
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            if (chooseMode)
            {
                isNavigating = true;
                try { UpdateMoveList(); }
                finally { isNavigating = false; }
            }

            // Set up match log
            analysisOutput.Clear();
            analysisOutput.SelectionColor = analysisOutput.ForeColor;
            analysisOutput.AppendText($"Engine Match: {GetEngineLabel(whiteEngineName, true)} vs {GetEngineLabel(blackEngineName, true)}\n");
            if (_seriesTotal > 1)
                analysisOutput.AppendText($"Series: {_seriesTotal} games\n");
            analysisOutput.AppendText($"Time Control: {tc}\n");
            string arbiterFile = Path.GetFileName(config.SelectedEngine ?? "");
            analysisOutput.AppendText($"Arbiter: {GetEngineLabel(arbiterFile, false)} (depth {config.EngineDepth})\n");
            if (chkFromPosition.Checked)
                analysisOutput.AppendText("Starting from custom position\n");
            if (bookReady)
            {
                if (rbBookChoose.Checked && _matchBookOpening != null)
                    analysisOutput.AppendText($"Opening: {_matchBookOpening.Eco}  {_matchBookOpening.Name}\n");
                else
                    analysisOutput.AppendText($"Book: {startFen}\n");
            }
            analysisOutput.AppendText("\n");

            // Disable conflicting controls
            SetMatchControlsEnabled(true);

            // Create and start match service
            matchService?.Dispose();
            matchService = new EngineMatchService(config);
            _previousMatchEval = null; // Reset for brilliant move detection
            matchService.OnMovePlayed += MatchService_OnMovePlayed;
            matchService.OnClockUpdated += MatchService_OnClockUpdated;
            matchService.OnMatchEnded += MatchService_OnMatchEnded;
            matchService.OnStatusChanged += MatchService_OnStatusChanged;
            matchService.OnAnnotatorEvalUpdated += MatchService_OnAnnotatorEvalUpdated;
            matchService.WaitForAnimation    = config.ShowAnimations;
            matchService.AnnotatorEngine      = engineService;
            matchService.AnnotatorDepth       = config.EngineDepth;
            matchService.AdjudicationEnabled  = chkAdjudicate.Checked;
            boardControl.AnimationCompleted  += MatchBoard_AnimationCompleted;

            // Initialize clocks display
            if (tc.Type == TimeControlType.TotalPlusIncrement)
            {
                UpdateClockDisplay(tc.TotalTimeMs, tc.TotalTimeMs, true);
            }
            else
            {
                lblWhiteClock.Text = "W: --:--";
                lblBlackClock.Text = "B: --:--";
            }

            clockTimer.Start();

            // Run match (fire and forget - events handle the rest)
            await matchService.StartMatchAsync(
                whiteEnginePath, blackEnginePath,
                whiteEngineName, blackEngineName,
                tc,
                startFen);
        }

        private void BtnStopMatch_Click(object? sender, EventArgs e)
        {
            matchService?.StopMatch();
        }

        private void ChkUseBook_CheckedChanged(object? sender, EventArgs e)
        {
            bool on = chkUseBook.Checked;
            rbBookRandom.Enabled = on;
            rbBookChoose.Enabled = on;
            if (!on) lblMatchOpening.Visible = false;
            // Opening book and "start from current position" are mutually exclusive
            chkFromPosition.Enabled = !on;
            if (on) chkFromPosition.Checked = false;
        }

        private void RbBookChoose_CheckedChanged(object? sender, EventArgs e)
        {
            if (!rbBookChoose.Checked) { lblMatchOpening.Visible = false; return; }
            SelectMatchOpening();
        }

        private void SelectMatchOpening()
        {
            string booksFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
            var entries = ChessDroid.Services.EcoBookService.LoadAll(booksFolder);
            if (entries.Count == 0)
            {
                lblStatus.Text = "No openings found in Books folder";
                rbBookRandom.Checked = true;
                return;
            }

            using var dlg = new OpeningExplorerDialog(entries, pgn =>
            {
                var m = PgnOpeningHeaderRegex.Match(pgn);
                if (!m.Success) return;
                string tag = m.Groups[1].Value;
                int dash = tag.IndexOf(" — ", StringComparison.Ordinal);
                if (dash < 0) return;
                string eco = tag[..dash];
                string name = tag[(dash + 3)..];
                _matchBookOpening = entries.FirstOrDefault(e => e.Eco == eco && e.Name == name)
                                 ?? entries.FirstOrDefault(e => e.Eco == eco);
                if (_matchBookOpening != null)
                {
                    lblMatchOpening.Text = $"{_matchBookOpening.Eco}  {_matchBookOpening.Name}";
                    lblMatchOpening.Visible = true;
                }
            }, ThemeService.IsDarkTheme(config?.Theme));
            dlg.ShowDialog(this);

            if (_matchBookOpening == null)
                rbBookRandom.Checked = true;
        }

        private void BtnEngineProfiles_Click(object? sender, EventArgs e)
        {
            bool isDark = ThemeService.IsDarkTheme(config?.Theme);
            using var dlg = new EngineProfilesDialog(config!, isDark);
            dlg.ShowDialog(this);
        }

        private void SetEngineInfoLabels(EngineProfile? whiteProfile, EngineProfile? blackProfile,
            string whiteFileName, string blackFileName)
        {
            static string BuildLabel(EngineProfile? profile, string fileName)
            {
                string name = !string.IsNullOrEmpty(profile?.DisplayName)
                    ? profile.DisplayName
                    : Path.GetFileNameWithoutExtension(fileName);
                return profile?.Elo > 0 ? $"{name} [{profile.Elo}]" : name;
            }

            bool showStrips = config?.ShowMaterialStrips != false;
            _lblBlackEngineInfo.Text = BuildLabel(blackProfile, blackFileName);
            _lblWhiteEngineInfo.Text = BuildLabel(whiteProfile, whiteFileName);
            _lblBlackEngineInfo.Visible = showStrips;
            _lblWhiteEngineInfo.Visible = showStrips;
        }

        private string GetEngineLabel(string fileName, bool includeElo)
        {
            if (string.IsNullOrEmpty(fileName)) return fileName;
            config.EngineProfiles.TryGetValue(fileName, out var profile);
            string name = !string.IsNullOrEmpty(profile?.DisplayName)
                ? profile.DisplayName
                : Path.GetFileNameWithoutExtension(fileName);
            if (includeElo && profile?.Elo > 0)
                return $"{name} ({profile.Elo})";
            return name;
        }

        private void MatchBoard_AnimationCompleted(object? sender, EventArgs e)
        {
            if (!_awaitingMatchAnimation) return;
            _awaitingMatchAnimation = false;
            matchService?.NotifyAnimationCompleted();
        }

        private void MatchService_OnMovePlayed(string uciMove, string fen, long moveTimeMs, string? eval)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnMovePlayed(uciMove, fen, moveTimeMs, eval));
                return;
            }

            isNavigating = true;
            try
            {
                // Get FEN before the move for brilliant detection
                string fenBeforeMove = moveTree.CurrentNode.FEN;

                // Make the move on the visual board
                boardControl.MakeMove(uciMove);
                if (config?.ShowAnimations == true)
                {
                    _awaitingMatchAnimation = true;
                    boardControl.StartAnimation(uciMove);
                }

                // Convert to SAN and add to move tree
                string san = ConvertUciToSan(uciMove, fenBeforeMove);
                string newFen = boardControl.GetFEN();

                PlayMoveSound(san.Contains('x'), san);

                // Check for brilliant move
                string brilliantSymbol = "";
                string? brilliantExplanation = null;
                double? currentEval = null;

                if (!string.IsNullOrEmpty(eval))
                {
                    currentEval = MovesExplanation.ParseEvaluation(eval);
                    if (currentEval.HasValue)
                    {
                        var (isBrilliant, explanation) = ConsoleOutputFormatter.IsBrilliantMove(
                            fenBeforeMove, uciMove, currentEval.Value, _previousMatchEval);

                        if (isBrilliant)
                        {
                            brilliantSymbol = "!!";
                            brilliantExplanation = explanation;
                        }
                    }
                }

                // Add move to tree (with symbol if brilliant)
                string sanWithSymbol = san + brilliantSymbol;
                moveTree.AddMove(uciMove, sanWithSymbol, newFen);

                if (currentEval.HasValue)
                {
                    moveTree.CurrentNode.Evaluation = currentEval.Value;
                    RefreshEvalGraph();
                }

                UpdateMoveList();
                UpdateFenDisplay();
                UpdateTurnLabel();

                // Update eval bar with engine's evaluation
                if (!string.IsNullOrEmpty(eval))
                {
                    UpdateEvalBar(eval);
                }

                // Update previous eval for next move
                _previousMatchEval = currentEval;

                // Threefold repetition check for Watch Engines mode
                if (_drillWatchActive)
                {
                    string posKey = GetPositionKey(newFen);
                    _watchPositionCounts.TryGetValue(posKey, out int posCount);
                    _watchPositionCounts[posKey] = posCount + 1;
                    if (_watchPositionCounts[posKey] >= 3)
                    {
                        analysisOutput.AppendText("\nDraw — threefold repetition\n");
                        matchService?.StopMatch();
                    }
                }

                // Log to analysis output
                var currentNode = moveTree.CurrentNode;
                double timeSec = moveTimeMs / 1000.0;
                string evalStr = !string.IsNullOrEmpty(eval) ? $" [{eval}]" : "";
                string timeStr = $"({timeSec:F1}s)";
                string brilliantStr = !string.IsNullOrEmpty(brilliantExplanation) ? $" {brilliantExplanation}" : "";

                if (currentNode.IsWhiteMove)
                {
                    analysisOutput.AppendText($"{currentNode.MoveNumber}. {sanWithSymbol}{evalStr}{brilliantStr} {timeStr}  ");
                }
                else
                {
                    analysisOutput.AppendText($"{sanWithSymbol}{evalStr}{brilliantStr} {timeStr}\n");
                }
                analysisOutput.ScrollToCaret();
            }
            finally
            {
                isNavigating = false;
            }
        }

        private void MatchService_OnClockUpdated(long whiteMs, long blackMs, bool whiteToMove)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnClockUpdated(whiteMs, blackMs, whiteToMove));
                return;
            }

            UpdateClockDisplay(whiteMs, blackMs, whiteToMove);
        }

        private void MatchService_OnMatchEnded(EngineMatchResult result)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnMatchEnded(result));
                return;
            }

            clockTimer.Stop();
            matchRunning = false;

            // Show result
            analysisOutput.AppendText($"\n\n{result.GetResultString()}\n");
            if (result.TimeControl == TimeControlType.TotalPlusIncrement &&
                result.Termination != MatchTermination.UserStopped)
            {
                analysisOutput.AppendText($"White remaining: {FormatClock(result.WhiteTimeRemainingMs)}\n");
                analysisOutput.AppendText($"Black remaining: {FormatClock(result.BlackTimeRemainingMs)}\n");
            }

            // Elo change (FIDE K=10, informational) + Chessdroid Rating update (K=32, persistent)
            if (_matchWhiteElo > 0 && _matchBlackElo > 0 && result.Outcome != MatchOutcome.Interrupted)
            {
                double whiteScore = result.Outcome == MatchOutcome.WhiteWins ? 1.0
                    : result.Outcome == MatchOutcome.Draw ? 0.5 : 0.0;
                int whiteDelta = Services.EloCalculator.EloChange(_matchWhiteElo, _matchBlackElo, whiteScore);
                int blackDelta = Services.EloCalculator.EloChange(_matchBlackElo, _matchWhiteElo, 1.0 - whiteScore);
                string wLabel = GetEngineLabel(_matchWhiteFileName, true);
                string bLabel = GetEngineLabel(_matchBlackFileName, true);
                analysisOutput.AppendText($"\nElo change: {wLabel} {Services.EloCalculator.FormatDelta(whiteDelta)}  {bLabel} {Services.EloCalculator.FormatDelta(blackDelta)}\n");

                // Chessdroid Rating — seeds from CCRL on first game, then drifts with K=32
                config.EngineProfiles.TryGetValue(_matchWhiteFileName, out var wProf);
                config.EngineProfiles.TryGetValue(_matchBlackFileName, out var bProf);
                if (wProf != null && bProf != null)
                {
                    int wCurr = wProf.ChessdroidElo > 0 ? wProf.ChessdroidElo : wProf.Elo;
                    int bCurr = bProf.ChessdroidElo > 0 ? bProf.ChessdroidElo : bProf.Elo;
                    int wChessDelta = Services.EloCalculator.EloChangeChessdroid(wCurr, bCurr, whiteScore);
                    int bChessDelta = Services.EloCalculator.EloChangeChessdroid(bCurr, wCurr, 1.0 - whiteScore);
                    int wNew = wCurr + wChessDelta;
                    int bNew = bCurr + bChessDelta;

                    config.EngineProfiles[_matchWhiteFileName] = new EngineProfile
                    {
                        DisplayName = wProf.DisplayName, Elo = wProf.Elo,
                        ChessdroidElo = wNew, GamesPlayed = wProf.GamesPlayed + 1
                    };
                    config.EngineProfiles[_matchBlackFileName] = new EngineProfile
                    {
                        DisplayName = bProf.DisplayName, Elo = bProf.Elo,
                        ChessdroidElo = bNew, GamesPlayed = bProf.GamesPlayed + 1
                    };
                    config.Save();

                    string wName = !string.IsNullOrEmpty(wProf.DisplayName) ? wProf.DisplayName : Path.GetFileNameWithoutExtension(_matchWhiteFileName);
                    string bName = !string.IsNullOrEmpty(bProf.DisplayName) ? bProf.DisplayName : Path.GetFileNameWithoutExtension(_matchBlackFileName);
                    analysisOutput.AppendText($"Chessdroid:  {wName} {wCurr} → {wNew} ({Services.EloCalculator.FormatDelta(wChessDelta)})  |  {bName} {bCurr} → {bNew} ({Services.EloCalculator.FormatDelta(bChessDelta)})\n");
                }
            }

            analysisOutput.ScrollToCaret();

            lblStatus.Text = result.GetResultString();

            // Set eval bar to reflect the actual result.
            // The arbiter reports "Mate in 0" for the final checkmate position, which is ambiguous
            // (SetMate(0) always resolves to full-black). Use SetTerminalMate instead.
            if (result.Outcome == MatchOutcome.WhiteWins)
            {
                evalBar?.SetTerminalMate(true);
                boardControl.TriggerParticles();
                PlayGameEndSound();
            }
            else if (result.Outcome == MatchOutcome.BlackWins)
            {
                evalBar?.SetTerminalMate(false);
                boardControl.TriggerParticles();
                PlayGameEndSound();
            }
            else
            {
                PlayGameEndSound();
            }

            // Auto-save PGN
            if (chkAutoSavePgn.Checked && result.Outcome != MatchOutcome.Interrupted)
                AutoSaveMatchPgn();

            // Series: update scores and continue if games remain
            if (result.Outcome != MatchOutcome.Interrupted)
            {
                bool eng1WasWhite = _seriesCurrentWhiteFile == _seriesEng1File;
                if (result.Outcome == MatchOutcome.WhiteWins)
                    (eng1WasWhite ? ref _seriesEng1Score : ref _seriesEng2Score) += 1.0;
                else if (result.Outcome == MatchOutcome.BlackWins)
                    (eng1WasWhite ? ref _seriesEng2Score : ref _seriesEng1Score) += 1.0;
                else if (result.Outcome == MatchOutcome.Draw)
                { _seriesEng1Score += 0.5; _seriesEng2Score += 0.5; }

                _seriesPlayed++;
                UpdateSeriesScoreLabel();
            }

            // Clean up match service
            boardControl.AnimationCompleted -= MatchBoard_AnimationCompleted;
            _awaitingMatchAnimation = false;
            if (matchService != null)
            {
                matchService.OnMovePlayed -= MatchService_OnMovePlayed;
                matchService.OnClockUpdated -= MatchService_OnClockUpdated;
                matchService.OnMatchEnded -= MatchService_OnMatchEnded;
                matchService.OnStatusChanged -= MatchService_OnStatusChanged;
                matchService.OnAnnotatorEvalUpdated -= MatchService_OnAnnotatorEvalUpdated;
                matchService.Dispose();
                matchService = null;
            }

            // Continue series or re-enable controls
            if (result.Outcome != MatchOutcome.Interrupted && _seriesPlayed < _seriesTotal)
                _ = StartNextSeriesGameAsync();
            else
            {
                SetMatchControlsEnabled(false);
                if (_drillWatchActive)
                {
                    _drillWatchActive = false;
                    SetDrillControlsEnabled(true);
                    if (_btnDrillWatchEngines != null) { _btnDrillWatchEngines.Text = "⚙ Watch engines"; _btnDrillWatchEngines.Enabled = true; }
                }
            }
        }

        private void UpdateSeriesScoreLabel()
        {
            if (_seriesTotal <= 1) return;
            string s1 = _seriesEng1Score % 1 == 0 ? $"{_seriesEng1Score:0}" : $"{_seriesEng1Score:0.0}";
            string s2 = _seriesEng2Score % 1 == 0 ? $"{_seriesEng2Score:0}" : $"{_seriesEng2Score:0.0}";
            lblSeriesScore.Text = $"({_seriesPlayed}/{_seriesTotal})  {s1} – {s2}";
        }

        private void AutoSaveMatchPgn()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MatchGames");
                Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string file = Path.Combine(dir, $"match_{stamp}.pgn");
                File.WriteAllText(file, GeneratePgn());
                analysisOutput.AppendText($"Saved: {file}\n");
            }
            catch (Exception ex)
            {
                analysisOutput.AppendText($"Auto-save failed: {ex.Message}\n");
            }
        }

        private async Task StartNextSeriesGameAsync()
        {
            // Swap colors for odd-numbered games
            bool eng1IsWhite = _seriesPlayed % 2 == 0;
            string whiteFile = eng1IsWhite ? _seriesEng1File : _seriesEng2File;
            string blackFile = eng1IsWhite ? _seriesEng2File : _seriesEng1File;
            _seriesCurrentWhiteFile = whiteFile;

            _matchWhiteFileName = whiteFile;
            _matchBlackFileName = blackFile;
            _matchWhiteName = Path.GetFileNameWithoutExtension(whiteFile);
            _matchBlackName = Path.GetFileNameWithoutExtension(blackFile);

            config.EngineProfiles.TryGetValue(whiteFile, out var whiteProfile);
            config.EngineProfiles.TryGetValue(blackFile, out var blackProfile);
            _matchWhiteElo = whiteProfile?.Elo ?? 0;
            _matchBlackElo = blackProfile?.Elo ?? 0;
            SetEngineInfoLabels(whiteProfile, blackProfile, whiteFile, blackFile);

            string enginesPath = config.GetEnginesPath();
            string whitePath = Path.Combine(enginesPath, whiteFile);
            string blackPath = Path.Combine(enginesPath, blackFile);
            if (!File.Exists(whitePath) || !File.Exists(blackPath)) { SetMatchControlsEnabled(false); return; }

            // Build time control from UI
            var tc = new EngineMatchTimeControl();
            switch (cmbTimeControlType.SelectedIndex)
            {
                case 0: tc.Type = TimeControlType.FixedDepth;          tc.Depth       = (int)numDepth.Value;       break;
                case 1: tc.Type = TimeControlType.FixedTimePerMove;    tc.MoveTimeMs  = (int)numMoveTime.Value;    break;
                case 2: tc.Type = TimeControlType.TotalPlusIncrement;
                        tc.TotalTimeMs = (int)numTotalTime.Value * 1000;
                        tc.IncrementMs = (int)numIncrement.Value * 1000;                                            break;
            }

            string startFen = _seriesStartFen;
            boardControl.LoadFEN(startFen);
            bool bookReady = chkUseBook.Checked && !chkFromPosition.Checked &&
                             (rbBookChoose.Checked ? _matchBookOpening != null : openingBookService?.IsLoaded == true);
            bool chooseMode = bookReady && rbBookChoose.Checked && _matchBookOpening != null;
            if (bookReady) startFen = GetBookStartFen(startFen);

            CancelClassification();
            if (!chooseMode)
                moveTree.Clear(startFen);
            moveListBox.Items.Clear();
            _movePairs.Clear();
            _analysisCache.Clear();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            if (chooseMode)
            {
                isNavigating = true;
                try { UpdateMoveList(); }
                finally { isNavigating = false; }
            }

            string whiteName = GetEngineLabel(whiteFile, true);
            string blackName = GetEngineLabel(blackFile, true);
            analysisOutput.AppendText($"\n— Game {_seriesPlayed + 1}/{_seriesTotal}: {whiteName} (W) vs {blackName} (B) —\n\n");

            matchService?.Dispose();
            matchService = new EngineMatchService(config);
            _previousMatchEval = null;
            matchService.OnMovePlayed          += MatchService_OnMovePlayed;
            matchService.OnClockUpdated        += MatchService_OnClockUpdated;
            matchService.OnMatchEnded          += MatchService_OnMatchEnded;
            matchService.OnStatusChanged       += MatchService_OnStatusChanged;
            matchService.OnAnnotatorEvalUpdated += MatchService_OnAnnotatorEvalUpdated;
            matchService.WaitForAnimation      = config.ShowAnimations;
            matchService.AnnotatorEngine       = engineService;
            matchService.AnnotatorDepth        = config.EngineDepth;
            matchService.AdjudicationEnabled   = chkAdjudicate.Checked;
            boardControl.AnimationCompleted    += MatchBoard_AnimationCompleted;

            if (tc.Type == TimeControlType.TotalPlusIncrement)
                UpdateClockDisplay(tc.TotalTimeMs, tc.TotalTimeMs, true);
            else
            { lblWhiteClock.Text = "W: --:--"; lblBlackClock.Text = "B: --:--"; }

            clockTimer.Start();
            await matchService.StartMatchAsync(whitePath, blackPath, _matchWhiteName, _matchBlackName, tc, startFen);
        }

        private string GetBookStartFen(string startFen)
        {
            // Choose mode: replay ECO opening moves, populate move tree from move 1
            if (rbBookChoose.Checked && _matchBookOpening != null)
                return PopulateOpeningMovesToTree(_matchBookOpening.Moves);

            // Random mode: 2 Polyglot plies
            const int BookPlies = 2;
            var board = ChessBoard.FromFEN(startFen);
            string castling = "KQkq";
            string ep = "-";
            bool whiteToMove = true;
            string fenRnd = startFen;
            for (int i = 0; i < BookPlies; i++)
            {
                var move = openingBookService!.GetBestBookMove(fenRnd);
                if (move == null) break;
                ChessRulesService.ApplyUciMove(board, move.UciMove, ref castling, ref ep);
                whiteToMove = !whiteToMove;
                fenRnd = $"{board.ToFEN()} {(whiteToMove ? "w" : "b")} {castling} {ep} 0 1";
            }
            boardControl.LoadFEN(fenRnd);
            return fenRnd;
        }

        private string PopulateOpeningMovesToTree(string sanMoves)
        {
            const string standardStart = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            moveTree.Clear(standardStart);
            string currentFen = standardStart;
            bool savedNav = isNavigating;
            isNavigating = true;
            try
            {
                var tokens = FilterSanTokens(sanMoves);
                foreach (var san in tokens)
                {
                    string? uci = PgnImportService.ConvertSanToUci(san, currentFen);
                    if (uci == null) break;
                    boardControl.LoadFEN(currentFen);
                    if (!boardControl.MakeMove(uci)) break;
                    currentFen = boardControl.GetFEN();
                    moveTree.AddMove(uci, san, currentFen);
                }
            }
            finally
            {
                isNavigating = savedNav;
            }
            return currentFen;
        }

        private void MatchService_OnStatusChanged(string status)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnStatusChanged(status));
                return;
            }

            lblStatus.Text = status;
        }

        private void MatchService_OnAnnotatorEvalUpdated(string eval)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnAnnotatorEvalUpdated(eval));
                return;
            }

            UpdateEvalBar(eval);
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            if (matchService?.IsRunning != true) return;

            UpdateClockDisplay(
                matchService.WhiteRemainingMs,
                matchService.BlackRemainingMs,
                matchService.WhiteToMove);
        }

        private void UpdateClockDisplay(long whiteMs, long blackMs, bool whiteToMove)
        {
            lblWhiteClock.Text = $"W: {FormatClock(whiteMs)}";
            lblBlackClock.Text = $"B: {FormatClock(blackMs)}";

            // Highlight active side using theme colors
            if (matchRunning)
            {
                var scheme = ThemeService.GetColorScheme(config?.Theme ?? "Dark");
                lblWhiteClock.BackColor = whiteToMove ? scheme.ClockActiveBackColor : scheme.ClockBackColor;
                lblBlackClock.BackColor = !whiteToMove ? scheme.ClockActiveBackColor : scheme.ClockBackColor;
            }
        }

        private static string FormatClock(long ms)
        {
            if (ms <= 0) return "0:00.0";
            int totalSeconds = (int)(ms / 1000);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            int tenths = (int)((ms % 1000) / 100);
            return $"{minutes}:{seconds:D2}.{tenths}";
        }

        private void SetClassifyControlsEnabled(bool classifying)
        {
            btnNewGame.Enabled        = !classifying;
            btnLoadFen.Enabled        = !classifying;
            btnEditPosition.Enabled   = !classifying;
            btnImportPgn.Enabled      = !classifying;
            btnExportPgn.Enabled      = !classifying;
            btnSaveToLibrary.Enabled  = !classifying;
            btnOpenLibrary.Enabled    = !classifying;
            btnOpenings.Enabled       = !classifying;
            btnSettings.Enabled       = !classifying;
            btnTraining.Enabled       = !classifying;
            btnTournament.Enabled     = !classifying;
            btnPlayBot.Enabled        = !classifying;
            btnStartMatch.Enabled     = !classifying;
        }

        private void SetBotControlsEnabled(bool active)
        {
            btnNewGame.Enabled        = !active;
            btnLoadFen.Enabled        = !active;
            btnEditPosition.Enabled   = !active;
            btnImportPgn.Enabled      = !active;
            btnExportPgn.Enabled      = !active;
            btnSaveToLibrary.Enabled  = !active;
            btnOpenLibrary.Enabled    = !active;
            btnOpenings.Enabled       = !active;
            btnClassifyMoves.Enabled  = !active;
            btnSettings.Enabled       = !active;
            btnTraining.Enabled       = !active;
            btnTournament.Enabled     = !active;
            btnStartMatch.Enabled     = !active;
            // Takebacks disabled during challenge mode; always re-enabled when bot stops.
            btnTakeBack.Enabled       = !active || _botSettings?.ChallengeMode != true;
        }

        private void SetMatchControlsEnabled(bool running)
        {
            matchRunning = running;

            // Disable/enable game controls
            btnNewGame.Enabled        = !running;
            btnTakeBack.Enabled       = !running;
            btnLoadFen.Enabled        = !running;
            btnEditPosition.Enabled   = !running;
            btnPlayBot.Enabled        = !running;
            btnImportPgn.Enabled      = !running;
            btnExportPgn.Enabled      = !running;
            btnSaveToLibrary.Enabled  = !running;
            btnOpenLibrary.Enabled    = !running;
            btnOpenings.Enabled       = !running;
            btnClassifyMoves.Enabled  = !running;
            btnSettings.Enabled       = !running;
            btnTraining.Enabled       = !running;
            btnTournament.Enabled     = !running;
            boardControl.InteractionEnabled = !running;

            // Match controls
            cmbWhiteEngine.Enabled   = !running;
            cmbBlackEngine.Enabled   = !running;
            cmbTimeControlType.Enabled = !running;
            numDepth.Enabled         = !running;
            numMoveTime.Enabled      = !running;
            numTotalTime.Enabled     = !running;
            numIncrement.Enabled     = !running;
            numGames.Enabled         = !running;
            chkAdjudicate.Enabled    = !running;
            chkAutoSavePgn.Enabled   = !running;
            chkUseBook.Enabled       = !running;
            rbBookRandom.Enabled     = !running && chkUseBook.Checked;
            rbBookChoose.Enabled     = !running && chkUseBook.Checked;

            // Toggle start/stop buttons
            btnStartMatch.Visible = !running;
            btnStopMatch.Visible = running;

            // Cancel auto-analysis during match
            if (running)
            {
                autoAnalysisCts?.Cancel();
            }
        }

        #endregion
    }
}
