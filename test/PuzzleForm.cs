using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    public partial class PuzzleForm : Form
    {
        private enum PuzzleState
        {
            Loading,
            SetupMove,
            WaitingForUser,
            OpponentResponse,
            Solved,
            Failed
        }

        private readonly AppConfig _config;
        private readonly PuzzleService _puzzleService;
        private Puzzle? _currentPuzzle;
        private PuzzleState _state = PuzzleState.Loading;
        private int _currentMoveIndex; // Index into puzzle.Moves for the next expected move
        private string _fenBeforeUserMove = ""; // For undo on wrong move
        private bool _isAutoMoving; // Flag to ignore MoveMade events during auto-moves
        private int _hintsUsedThisMove;
        private int _totalHintsThisPuzzle;
        private int _wrongAttemptsThisMove;
        private bool _failedThisPuzzle; // Set on first wrong move or hint auto-play
        private int _sessionSolved;
        private int _sessionAttempted;
        private CancellationTokenSource? _delayCts;
        private bool _solverIsWhite; // Which color the user plays in this puzzle

        /// <summary>
        /// Callback to send a FEN position to the analysis board for deeper analysis.
        /// </summary>
        public Action<string>? OnAnalyzePosition { get; set; }

        public PuzzleForm(AppConfig config)
        {
            _config = config;
            _puzzleService = new PuzzleService();

            InitializeComponent();

            // Apply piece set (same as analysis board)
            if (!string.IsNullOrEmpty(_config.SelectedSite))
                boardControl.SetTemplateSet(_config.SelectedSite);

            // Apply theme
            ApplyTheme();

            // Load filter settings from config
            numMinRating.Value = Math.Clamp(_config.PuzzleMinRating, 400, 3300);
            numMaxRating.Value = Math.Clamp(_config.PuzzleMaxRating, 400, 3300);

            // Start loading puzzles
            _ = LoadPuzzlesAndStartAsync();
        }

        #region Initialization

        private async Task LoadPuzzlesAndStartAsync()
        {
            _state = PuzzleState.Loading;
            lblStatus.Text = "Loading puzzles...";
            lblFeedback.Text = "Please wait...";
            boardControl.InteractionEnabled = false;

            string csvPath = PuzzleService.GetPuzzlesPath();
            int count = await _puzzleService.LoadPuzzlesAsync(csvPath);

            if (count == 0)
            {
                lblStatus.Text = "No puzzles found. Place puzzles.csv in the Puzzles/ folder.";
                lblFeedback.Text = "No puzzles available";
                return;
            }

            // Populate theme filter
            PopulateThemeFilter();

            // Restore theme filter from config
            if (!string.IsNullOrEmpty(_config.PuzzleThemeFilter))
            {
                int idx = cmbThemeFilter.Items.IndexOf(_config.PuzzleThemeFilter);
                if (idx >= 0) cmbThemeFilter.SelectedIndex = idx;
            }

            lblStatus.Text = $"{count} puzzles loaded";
            UpdateStatsDisplay();

            // Load first puzzle
            LoadNextPuzzle();
        }

        private void PopulateThemeFilter()
        {
            cmbThemeFilter.Items.Clear();
            cmbThemeFilter.Items.Add("All Themes");
            foreach (var theme in _puzzleService.GetAvailableThemes())
                cmbThemeFilter.Items.Add(theme);
            cmbThemeFilter.SelectedIndex = 0;
        }

        #endregion

        #region Puzzle Flow

        private void LoadNextPuzzle()
        {
            // Cancel any pending delays
            _delayCts?.Cancel();
            _delayCts = new CancellationTokenSource();

            int minRating = (int)numMinRating.Value;
            int maxRating = (int)numMaxRating.Value;
            string? theme = cmbThemeFilter.SelectedItem?.ToString();

            _currentPuzzle = _puzzleService.GetNextPuzzle(minRating, maxRating, theme, _currentPuzzle?.PuzzleId);
            if (_currentPuzzle == null)
            {
                lblFeedback.Text = "No puzzles match your filters.";
                lblStatus.Text = "Try adjusting rating range or theme.";
                return;
            }

            // Reset puzzle state
            _currentMoveIndex = 0;
            _hintsUsedThisMove = 0;
            _totalHintsThisPuzzle = 0;
            _wrongAttemptsThisMove = 0;
            _failedThisPuzzle = false;

            // Update UI
            btnNext.Visible = false;
            btnAnalyze.Visible = false;
            btnRetry.Visible = false;
            btnHint.Enabled = true;
            btnHint.Text = "Hint (0/3)";
            btnSkip.Enabled = true;
            boardControl.HintSquares.Clear();

            // Display puzzle info
            UpdatePuzzleInfo();

            // Load the FEN (position before opponent's setup move)
            boardControl.LoadFEN(_currentPuzzle.FEN);

            // Determine who the solver is: after setup move, it's the OTHER side's turn
            bool fenWhiteToMove = _currentPuzzle.FEN.Split(' ')[1] == "w";
            _solverIsWhite = fenWhiteToMove; // The side that just moved is the opponent; solver is the one whose turn it is after setup

            // Actually: FEN has the side-to-move which makes the SETUP move.
            // So the solver is the OTHER side.
            _solverIsWhite = !fenWhiteToMove;

            // Auto-flip so solver is at bottom
            boardControl.IsFlipped = !_solverIsWhite;

            // Disable interaction during setup
            boardControl.InteractionEnabled = false;
            _state = PuzzleState.SetupMove;

            string turnText = _solverIsWhite ? "White" : "Black";
            lblFeedback.Text = $"Find the best move for {turnText}!";
            lblFeedback.ForeColor = GetThemeTextColor();

            // Play setup move after a brief delay
            _ = PlaySetupMoveAsync(_delayCts.Token);
        }

        private async Task PlaySetupMoveAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(600, ct);
                if (ct.IsCancellationRequested || _currentPuzzle == null) return;

                // Play the opponent's setup move
                _isAutoMoving = true;
                boardControl.MakeMove(_currentPuzzle.SetupMove);
                _isAutoMoving = false;

                _currentMoveIndex = 1; // First user move
                _fenBeforeUserMove = boardControl.GetFEN();

                // Update turn label
                string turnText = _solverIsWhite ? "White" : "Black";
                lblTurn.Text = $"{turnText} to move";

                UpdateMoveProgress();

                // Enable interaction for the user
                boardControl.InteractionEnabled = true;
                _state = PuzzleState.WaitingForUser;
            }
            catch (TaskCanceledException) { }
        }

        private void BoardControl_MoveMade(object? sender, MoveEventArgs e)
        {
            if (_isAutoMoving) return;
            if (_state != PuzzleState.WaitingForUser || _currentPuzzle == null) return;

            ValidateUserMove(e.UciMove);
        }

        private void ValidateUserMove(string userMove)
        {
            if (_currentPuzzle == null || _currentMoveIndex >= _currentPuzzle.Moves.Length)
                return;

            string expectedMove = _currentPuzzle.Moves[_currentMoveIndex];
            bool isCorrect = false;

            // Direct match
            if (userMove == expectedMove)
            {
                isCorrect = true;
            }
            // Mate acceptance: on the last user move of a mate puzzle, any mating move is correct
            else if (_currentPuzzle.IsMate && _currentMoveIndex >= _currentPuzzle.Moves.Length - 2)
            {
                isCorrect = PuzzleService.IsMoveCheckmate(_fenBeforeUserMove, userMove);
            }

            if (isCorrect)
            {
                OnCorrectMove();
            }
            else
            {
                OnWrongMove();
            }
        }

        private void OnCorrectMove()
        {
            boardControl.HintSquares.Clear();
            _hintsUsedThisMove = 0;
            _wrongAttemptsThisMove = 0;
            _currentMoveIndex++;

            // Check if puzzle is complete
            if (_currentMoveIndex >= _currentPuzzle!.Moves.Length)
            {
                OnPuzzleComplete();
                return;
            }

            // There's an opponent response to play
            lblFeedback.Text = "Correct!";
            lblFeedback.ForeColor = Color.FromArgb(80, 200, 80);
            boardControl.InteractionEnabled = false;
            _state = PuzzleState.OpponentResponse;

            _ = PlayOpponentResponseAsync(_delayCts?.Token ?? CancellationToken.None);
        }

        private async Task PlayOpponentResponseAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(500, ct);
                if (ct.IsCancellationRequested || _currentPuzzle == null) return;

                // Play opponent's response
                _isAutoMoving = true;
                boardControl.MakeMove(_currentPuzzle.Moves[_currentMoveIndex]);
                _isAutoMoving = false;

                _currentMoveIndex++;
                _fenBeforeUserMove = boardControl.GetFEN();

                await Task.Delay(300, ct);
                if (ct.IsCancellationRequested) return;

                // Check if puzzle is complete after opponent's move (shouldn't happen, but safety check)
                if (_currentMoveIndex >= _currentPuzzle.Moves.Length)
                {
                    OnPuzzleComplete();
                    return;
                }

                UpdateMoveProgress();

                string turnText = _solverIsWhite ? "White" : "Black";
                lblFeedback.Text = $"Correct! Find the next move for {turnText}.";
                lblFeedback.ForeColor = GetThemeTextColor();
                lblTurn.Text = $"{turnText} to move";

                boardControl.InteractionEnabled = true;
                _state = PuzzleState.WaitingForUser;
            }
            catch (TaskCanceledException) { }
        }

        private void OnWrongMove()
        {
            _wrongAttemptsThisMove++;
            _failedThisPuzzle = true; // First wrong move = puzzle failed for stats

            lblFeedback.Text = "Incorrect, try again.";
            lblFeedback.ForeColor = Color.FromArgb(200, 60, 60);

            // Undo the wrong move by reloading the position
            boardControl.InteractionEnabled = false;
            _isAutoMoving = true;
            boardControl.LoadFEN(_fenBeforeUserMove);
            _isAutoMoving = false;

            // Restore last move highlight from the previous move
            if (_currentMoveIndex >= 2)
            {
                string prevMove = _currentPuzzle!.Moves[_currentMoveIndex - 1];
                SetLastMoveHighlight(prevMove);
            }
            else if (_currentMoveIndex == 1)
            {
                SetLastMoveHighlight(_currentPuzzle!.SetupMove);
            }

            // Auto-show first hint after 2 wrong attempts
            if (_wrongAttemptsThisMove >= 2 && _hintsUsedThisMove == 0)
            {
                ShowHint();
            }

            btnRetry.Visible = true;
            boardControl.InteractionEnabled = true;
        }

        private void OnPuzzleComplete()
        {
            bool solved = !_failedThisPuzzle;

            _sessionAttempted++;
            if (solved) _sessionSolved++;

            _state = solved ? PuzzleState.Solved : PuzzleState.Failed;

            boardControl.InteractionEnabled = false;
            btnNext.Visible = true;
            btnAnalyze.Visible = OnAnalyzePosition != null;
            btnRetry.Visible = false;
            btnHint.Enabled = false;
            btnSkip.Enabled = false;

            RevealPuzzleInfo();

            // Always mark completed puzzles for skip purposes (even with mistakes)
            // Failed/skipped puzzles from OnPuzzleFailed do NOT get added here
            _puzzleService.MarkPuzzleCompleted(_currentPuzzle!.PuzzleId);

            // Record stats
            _puzzleService.RecordAttempt(
                _currentPuzzle!.PuzzleId,
                _currentPuzzle.Rating,
                _currentPuzzle.Themes,
                solved: solved,
                hintsUsed: _totalHintsThisPuzzle);

            UpdateStatsDisplay();
            lblStatus.Text = $"Session: {_sessionSolved}/{_sessionAttempted} ({(_sessionAttempted > 0 ? _sessionSolved * 100.0 / _sessionAttempted : 0):F0}%)";

            // Show solution review
            ShowSolutionReview(solved);
        }

        private void OnPuzzleFailed()
        {
            _failedThisPuzzle = true;
            _state = PuzzleState.Failed;
            _sessionAttempted++;

            boardControl.InteractionEnabled = false;
            btnNext.Visible = true;
            btnAnalyze.Visible = OnAnalyzePosition != null;
            btnRetry.Visible = false;
            btnHint.Enabled = false;
            btnSkip.Enabled = false;

            RevealPuzzleInfo();

            // Record stats
            _puzzleService.RecordAttempt(
                _currentPuzzle!.PuzzleId,
                _currentPuzzle.Rating,
                _currentPuzzle.Themes,
                solved: false,
                hintsUsed: _totalHintsThisPuzzle);

            UpdateStatsDisplay();
            lblStatus.Text = $"Session: {_sessionSolved}/{_sessionAttempted} ({(_sessionAttempted > 0 ? _sessionSolved * 100.0 / _sessionAttempted : 0):F0}%)";

            // Show the solution animated, then show review
            _ = ShowSolutionAsync(_delayCts?.Token ?? CancellationToken.None);
        }

        private async Task ShowSolutionAsync(CancellationToken ct)
        {
            if (_currentPuzzle == null) return;

            lblFeedback.Text = "Showing solution...";
            lblFeedback.ForeColor = Color.FromArgb(255, 180, 50);

            // Reload from where we are and play remaining moves
            for (int i = _currentMoveIndex; i < _currentPuzzle.Moves.Length; i++)
            {
                try
                {
                    await Task.Delay(700, ct);
                    if (ct.IsCancellationRequested) return;

                    _isAutoMoving = true;
                    boardControl.MakeMove(_currentPuzzle.Moves[i]);
                    _isAutoMoving = false;
                }
                catch (TaskCanceledException) { return; }
            }

            ShowSolutionReview(false);
        }

        private void ShowSolutionReview(bool solved)
        {
            if (_currentPuzzle == null) return;

            // Build solution in SAN notation
            string solutionText = BuildSolutionText();

            if (solved)
            {
                lblFeedback.Text = $"Puzzle solved!\n{solutionText}";
                lblFeedback.ForeColor = Color.FromArgb(80, 200, 80);
            }
            else
            {
                lblFeedback.Text = $"Puzzle failed.\n{solutionText}";
                lblFeedback.ForeColor = Color.FromArgb(200, 60, 60);
            }
        }

        private string BuildSolutionText()
        {
            if (_currentPuzzle == null) return "";

            var parts = new List<string>();

            // Start from the position after setup move
            string currentFen = _currentPuzzle.FEN;

            // Apply setup move to get the puzzle starting position
            try
            {
                var tempBoard = ChessBoard.FromFEN(currentFen);
                var fenParts = currentFen.Split(' ');
                string castling = fenParts.Length > 2 ? fenParts[2] : "-";
                string enPassant = fenParts.Length > 3 ? fenParts[3] : "-";

                ChessRulesService.ApplyUciMove(tempBoard, _currentPuzzle.SetupMove, ref castling, ref enPassant);

                // Rebuild FEN after setup move
                bool setupWhite = fenParts[1] == "w";
                string nextSide = setupWhite ? "b" : "w";
                currentFen = $"{tempBoard.ToFEN()} {nextSide} {castling} {enPassant} 0 1";
            }
            catch
            {
                // If FEN reconstruction fails, just show UCI moves
                return "Solution: " + string.Join(" ", _currentPuzzle.Moves.Skip(1));
            }

            // Convert each solution move to SAN
            bool isWhiteMove = currentFen.Split(' ')[1] == "w";
            int moveNumber = 1;

            for (int i = 1; i < _currentPuzzle.Moves.Length; i++)
            {
                string uciMove = _currentPuzzle.Moves[i];
                string san = ConvertUciToSan(uciMove, currentFen);

                bool isUserMove = (i % 2 == 1); // Odd indices are user moves

                if (isWhiteMove)
                {
                    parts.Add($"{moveNumber}. {san}");
                }
                else
                {
                    if (i == 1) // First move is black
                        parts.Add($"{moveNumber}... {san}");
                    else
                        parts.Add(san);
                }

                if (!isWhiteMove) moveNumber++;

                // Advance FEN for next move
                try
                {
                    var tempBoard = ChessBoard.FromFEN(currentFen);
                    var fenParts = currentFen.Split(' ');
                    string castling = fenParts.Length > 2 ? fenParts[2] : "-";
                    string enPassant = fenParts.Length > 3 ? fenParts[3] : "-";

                    ChessRulesService.ApplyUciMove(tempBoard, uciMove, ref castling, ref enPassant);

                    string nextSide = isWhiteMove ? "b" : "w";
                    currentFen = $"{tempBoard.ToFEN()} {nextSide} {castling} {enPassant} 0 1";
                    isWhiteMove = !isWhiteMove;
                }
                catch { break; }
            }

            return "Solution: " + string.Join(" ", parts);
        }

        private static string ConvertUciToSan(string uciMove, string fen)
        {
            try
            {
                return ChessNotationService.ConvertUCIToSAN(uciMove, fen,
                    ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
            }
            catch
            {
                return uciMove;
            }
        }

        #endregion

        #region Hint System

        private void ShowHint()
        {
            if (_currentPuzzle == null || _currentMoveIndex >= _currentPuzzle.Moves.Length)
                return;

            _hintsUsedThisMove++;
            _totalHintsThisPuzzle++;

            string expectedMove = _currentPuzzle.Moves[_currentMoveIndex];
            string fromSquare = expectedMove.Substring(0, 2);
            string toSquare = expectedMove.Substring(2, 2);

            int fromCol = fromSquare[0] - 'a';
            int fromRow = 7 - (fromSquare[1] - '1');
            int toCol = toSquare[0] - 'a';
            int toRow = 7 - (toSquare[1] - '1');

            string pieceName = GetPieceName(fromRow, fromCol);

            switch (_hintsUsedThisMove)
            {
                case 1:
                    // Hint 1: highlight source square
                    boardControl.HintSquares.Clear();
                    boardControl.HintSquares.Add((fromRow, fromCol));
                    boardControl.Invalidate();
                    lblFeedback.Text = $"Hint: Move your {pieceName}";
                    lblFeedback.ForeColor = Color.FromArgb(255, 180, 50);
                    break;

                case 2:
                    // Hint 2: also highlight destination
                    boardControl.HintSquares.Clear();
                    boardControl.HintSquares.Add((fromRow, fromCol));
                    boardControl.HintSquares.Add((toRow, toCol));
                    boardControl.Invalidate();
                    lblFeedback.Text = $"Hint: Move {pieceName} to {toSquare}";
                    lblFeedback.ForeColor = Color.FromArgb(255, 180, 50);
                    break;

                default:
                    // Hint 3+: show the move notation and auto-play
                    _failedThisPuzzle = true; // Auto-play = not solved cleanly
                    boardControl.HintSquares.Clear();
                    boardControl.Invalidate();
                    lblFeedback.Text = $"Solution: {fromSquare}{toSquare}";
                    lblFeedback.ForeColor = Color.FromArgb(255, 180, 50);

                    // Auto-play the correct move
                    boardControl.InteractionEnabled = false;
                    _isAutoMoving = true;
                    boardControl.MakeMove(expectedMove);
                    _isAutoMoving = false;

                    _hintsUsedThisMove = 0;
                    _wrongAttemptsThisMove = 0;
                    _currentMoveIndex++;

                    if (_currentMoveIndex >= _currentPuzzle.Moves.Length)
                    {
                        OnPuzzleComplete();
                    }
                    else
                    {
                        _state = PuzzleState.OpponentResponse;
                        _ = PlayOpponentResponseAsync(_delayCts?.Token ?? CancellationToken.None);
                    }
                    return;
            }

            btnHint.Text = $"Hint ({_hintsUsedThisMove}/3)";
        }

        private string GetPieceName(int row, int col)
        {
            char piece = boardControl.GetPieceAt(row, col);
            return char.ToUpper(piece) switch
            {
                'K' => "King",
                'Q' => "Queen",
                'R' => "Rook",
                'B' => "Bishop",
                'N' => "Knight",
                'P' => "Pawn",
                _ => "piece"
            };
        }

        #endregion

        #region Button Handlers

        private void BtnHint_Click(object? sender, EventArgs e)
        {
            if (_state == PuzzleState.WaitingForUser)
                ShowHint();
        }

        private void BtnSkip_Click(object? sender, EventArgs e)
        {
            if (_state == PuzzleState.WaitingForUser || _state == PuzzleState.SetupMove)
            {
                OnPuzzleFailed();
            }
        }

        private void BtnNext_Click(object? sender, EventArgs e)
        {
            LoadNextPuzzle();
        }

        private void BtnRetry_Click(object? sender, EventArgs e)
        {
            if (_currentPuzzle == null) return;

            // Reload from the position before the current user move
            boardControl.HintSquares.Clear();
            _isAutoMoving = true;
            boardControl.LoadFEN(_fenBeforeUserMove);
            _isAutoMoving = false;

            if (_currentMoveIndex >= 2)
                SetLastMoveHighlight(_currentPuzzle.Moves[_currentMoveIndex - 1]);
            else if (_currentMoveIndex == 1)
                SetLastMoveHighlight(_currentPuzzle.SetupMove);

            _hintsUsedThisMove = 0;
            _wrongAttemptsThisMove = 0;
            btnHint.Text = "Hint (0/3)";
            btnRetry.Visible = false;

            string turnText = _solverIsWhite ? "White" : "Black";
            lblFeedback.Text = $"Find the best move for {turnText}!";
            lblFeedback.ForeColor = GetThemeTextColor();

            boardControl.InteractionEnabled = true;
            _state = PuzzleState.WaitingForUser;
        }

        private void BtnFlipBoard_Click(object? sender, EventArgs e)
        {
            boardControl.FlipBoard();
        }

        private void BtnResetStats_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all puzzle statistics?\nThis cannot be undone.",
                "Reset Statistics",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _puzzleService.ResetStats();
                _sessionSolved = 0;
                _sessionAttempted = 0;
                UpdateStatsDisplay();
                lblStatus.Text = "Statistics reset";
            }
        }

        private void BtnAnalyze_Click(object? sender, EventArgs e)
        {
            if (_currentPuzzle == null || OnAnalyzePosition == null) return;

            // Load the puzzle starting position (after setup move), not the final position
            string fen = GetPuzzleStartFen();
            OnAnalyzePosition.Invoke(fen);
        }

        private string GetPuzzleStartFen()
        {
            if (_currentPuzzle == null) return boardControl.GetFEN();

            try
            {
                var tempBoard = ChessBoard.FromFEN(_currentPuzzle.FEN);
                var fenParts = _currentPuzzle.FEN.Split(' ');
                string castling = fenParts.Length > 2 ? fenParts[2] : "-";
                string enPassant = fenParts.Length > 3 ? fenParts[3] : "-";

                ChessRulesService.ApplyUciMove(tempBoard, _currentPuzzle.SetupMove, ref castling, ref enPassant);

                bool setupWhite = fenParts[1] == "w";
                string nextSide = setupWhite ? "b" : "w";
                return $"{tempBoard.ToFEN()} {nextSide} {castling} {enPassant} 0 1";
            }
            catch
            {
                return boardControl.GetFEN();
            }
        }

        #endregion

        #region UI Updates

        private void UpdatePuzzleInfo()
        {
            if (_currentPuzzle == null) return;

            bool hideInfo = IsInfoHidden();

            if (hideInfo)
            {
                lblPuzzleRating.Text = "Rating: ???";
                lblPuzzleThemes.Text = "Themes: ???";
                lblPuzzleProgress.Text = "Move — of —";
            }
            else
            {
                lblPuzzleRating.Text = $"Rating: {_currentPuzzle.Rating} ({_currentPuzzle.RatingCategory})";
                lblPuzzleThemes.Text = $"Themes: {string.Join(", ", _currentPuzzle.Themes)}";
                UpdateMoveProgress();
            }
        }

        private void RevealPuzzleInfo()
        {
            if (_currentPuzzle == null) return;

            lblPuzzleRating.Text = $"Rating: {_currentPuzzle.Rating} ({_currentPuzzle.RatingCategory})";
            lblPuzzleThemes.Text = $"Themes: {string.Join(", ", _currentPuzzle.Themes)}";
            UpdateMoveProgress();
        }

        private bool IsInfoHidden()
        {
            string? theme = cmbThemeFilter.SelectedItem?.ToString();
            return string.IsNullOrEmpty(theme) || theme == "All Themes";
        }

        private void UpdateMoveProgress()
        {
            if (_currentPuzzle == null) return;

            // Don't reveal move count during active puzzle when info is hidden
            if (IsInfoHidden() && _state != PuzzleState.Solved && _state != PuzzleState.Failed)
                return;

            int userMoveNum = (_currentMoveIndex - 1) / 2 + 1;
            int totalUserMoves = _currentPuzzle.SolutionLength;
            lblPuzzleProgress.Text = $"Move {Math.Min(userMoveNum, totalUserMoves)} of {totalUserMoves}";
        }

        private void UpdateStatsDisplay()
        {
            var stats = _puzzleService.GetStats();
            lblStatsRating.Text = $"Puzzle Rating: {stats.EstimatedRating}";
            lblStatsSolved.Text = $"Solved: {stats.TotalSolved} / {stats.TotalAttempted}";
            lblStatsAccuracy.Text = $"Accuracy: {stats.SuccessRate:F1}%";
            lblStatsStreak.Text = $"Streak: {stats.CurrentStreak} (Best: {stats.BestStreak})";
            lblStatsHints.Text = $"Hints used: {stats.HintsUsed}";
        }

        private void SetLastMoveHighlight(string uciMove)
        {
            if (uciMove.Length < 4) return;

            int fromCol = uciMove[0] - 'a';
            int fromRow = 7 - (uciMove[1] - '1');
            int toCol = uciMove[2] - 'a';
            int toRow = 7 - (uciMove[3] - '1');

            boardControl.LastMove = (fromRow, fromCol, toRow, toCol);
        }

        #endregion

        #region Theme

        private void ApplyTheme()
        {
            var scheme = ThemeService.GetColorScheme(_config.Theme != "Light");

            BackColor = scheme.FormBackColor;
            leftPanel.BackColor = scheme.PanelColor;
            rightPanel.BackColor = scheme.PanelColor;

            // Labels
            foreach (var lbl in new[] { lblTurn, lblStatus, lblPuzzleRating, lblPuzzleThemes,
                                         lblPuzzleProgress, lblFeedback, lblStatsRating, lblStatsSolved,
                                         lblStatsAccuracy, lblStatsStreak, lblStatsHints,
                                         lblMinRating, lblMaxRating, lblThemeFilter })
            {
                lbl.ForeColor = scheme.TextColor;
                lbl.BackColor = Color.Transparent;
            }

            // GroupBoxes
            foreach (var grp in new[] { grpPuzzleInfo, grpFeedback, grpFilters, grpStats })
            {
                grp.ForeColor = scheme.TextColor;
                grp.BackColor = scheme.PanelColor;
            }

            // Buttons
            foreach (var btn in new[] { btnHint, btnSkip, btnNext, btnFlipBoard, btnRetry, btnAnalyze, btnResetStats })
            {
                btn.BackColor = scheme.ButtonBackColor;
                btn.ForeColor = scheme.ButtonForeColor;
                btn.FlatAppearance.BorderColor = scheme.ButtonBackColor;
            }

            // NumericUpDown
            foreach (var num in new[] { numMinRating, numMaxRating })
            {
                num.BackColor = scheme.FormBackColor;
                num.ForeColor = scheme.TextColor;
            }

            // ComboBox
            cmbThemeFilter.BackColor = scheme.FormBackColor;
            cmbThemeFilter.ForeColor = scheme.TextColor;
        }

        private Color GetThemeTextColor()
        {
            return _config.Theme == "Light"
                ? Color.FromArgb(30, 30, 35)
                : Color.FromArgb(220, 220, 220);
        }

        #endregion

        #region Layout

        private void LeftPanel_Resize(object? sender, EventArgs e)
        {
            if (boardControl == null || lblTurn == null) return;

            int panelWidth = leftPanel.ClientSize.Width;
            int panelHeight = leftPanel.ClientSize.Height;

            // Calculate board size (square, fit within panel with margin)
            int maxBoardWidth = panelWidth - 20;
            int maxBoardHeight = panelHeight - 120; // Leave space for controls below
            int boardSize = Math.Min(maxBoardWidth, maxBoardHeight);
            boardSize = Math.Max(boardSize, 200); // Minimum board size

            // Center the board horizontally
            int boardX = Math.Max(10, (panelWidth - boardSize) / 2);
            int boardY = 10;

            boardControl.Location = new Point(boardX, boardY);
            boardControl.Size = new Size(boardSize, boardSize);

            // Turn label below board
            int belowBoard = boardY + boardSize + 5;
            lblTurn.Location = new Point(boardX, belowBoard);
            lblTurn.Width = boardSize;

            // Buttons row
            int buttonY = belowBoard + 28;
            int buttonSpacing = 5;
            btnHint.Location = new Point(boardX, buttonY);
            btnRetry.Location = new Point(btnHint.Right + buttonSpacing, buttonY);
            btnSkip.Location = new Point(btnRetry.Right + buttonSpacing, buttonY);
            btnNext.Location = new Point(btnSkip.Right + buttonSpacing, buttonY);
            btnFlipBoard.Location = new Point(btnNext.Right + buttonSpacing, buttonY);
            btnAnalyze.Location = new Point(btnFlipBoard.Right + buttonSpacing, buttonY);

            // Status label
            lblStatus.Location = new Point(boardX, buttonY + 35);
            lblStatus.Width = boardSize;
        }

        #endregion

        #region Keyboard Shortcuts

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.H:
                    if (_state == PuzzleState.WaitingForUser)
                        ShowHint();
                    return true;
                case Keys.N:
                    if (_state == PuzzleState.Solved || _state == PuzzleState.Failed)
                        LoadNextPuzzle();
                    return true;
                case Keys.S:
                    if (_state == PuzzleState.WaitingForUser)
                        OnPuzzleFailed();
                    return true;
                case Keys.F:
                    boardControl.FlipBoard();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Form Lifecycle

        private void PuzzleForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Cancel any pending delays
            _delayCts?.Cancel();

            // Save filter settings to config
            _config.PuzzleMinRating = (int)numMinRating.Value;
            _config.PuzzleMaxRating = (int)numMaxRating.Value;
            _config.PuzzleThemeFilter = cmbThemeFilter.SelectedItem?.ToString() ?? "All Themes";
            _config.Save();
        }

        #endregion
    }
}
