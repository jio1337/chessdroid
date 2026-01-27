using System.Diagnostics;
using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    /// <summary>
    /// Offline analysis board form with interactive chess board, move list, and engine analysis.
    /// Provides a complete analysis experience without needing internet connection.
    /// </summary>
    public partial class AnalysisBoardForm : Form
    {
        // Auto-analysis
        private CancellationTokenSource? autoAnalysisCts;

        // Services
        private ChessEngineService? engineService;
        private MoveSharpnessAnalyzer sharpnessAnalyzer;
        private ConsoleOutputFormatter? consoleFormatter;
        private PolyglotBookService? openingBookService;
        private AppConfig config;

        // Game state
        private List<string> moveHistory = new List<string>();
        private List<string> fenHistory = new List<string>();
        private int currentMoveIndex = -1;
        private bool isNavigating = false;

        public AnalysisBoardForm(AppConfig config, ChessEngineService? sharedEngineService = null)
        {
            this.config = config;
            this.engineService = sharedEngineService ?? new ChessEngineService(config);
            this.sharpnessAnalyzer = new MoveSharpnessAnalyzer();

            InitializeComponent();
            ApplyTheme();
            InitializeServices();

            // Save initial position
            fenHistory.Add(boardControl.GetFEN());
        }

        private void ApplyTheme()
        {
            bool isDarkMode = config?.Theme == "Dark";
            Color panelColor = isDarkMode ? Color.FromArgb(40, 40, 48) : Color.White;
            Color textColor = isDarkMode ? Color.FromArgb(220, 220, 220) : Color.Black;

            // Form colors
            this.BackColor = isDarkMode ? Color.FromArgb(30, 30, 35) : Color.FromArgb(245, 245, 245);

            // Labels
            lblTurn.ForeColor = textColor;
            lblFen.ForeColor = textColor;
            lblStatus.ForeColor = isDarkMode ? Color.Gray : Color.DimGray;
            lblMoves.ForeColor = textColor;
            lblAnalysis.ForeColor = textColor;

            // Buttons
            Color buttonBg = isDarkMode ? Color.FromArgb(50, 50, 58) : Color.FromArgb(230, 230, 230);
            Color buttonFg = isDarkMode ? Color.FromArgb(200, 200, 200) : Color.Black;

            btnNewGame.BackColor = buttonBg;
            btnNewGame.ForeColor = buttonFg;
            btnFlipBoard.BackColor = buttonBg;
            btnFlipBoard.ForeColor = buttonFg;
            btnTakeBack.BackColor = buttonBg;
            btnTakeBack.ForeColor = buttonFg;
            btnPrevMove.BackColor = buttonBg;
            btnPrevMove.ForeColor = buttonFg;
            btnNextMove.BackColor = buttonBg;
            btnNextMove.ForeColor = buttonFg;
            btnLoadFen.BackColor = buttonBg;
            btnLoadFen.ForeColor = buttonFg;
            btnCopyFen.BackColor = buttonBg;
            btnCopyFen.ForeColor = buttonFg;

            // Analyze button special color
            btnAnalyze.BackColor = isDarkMode ? Color.FromArgb(60, 90, 140) : Color.FromArgb(70, 130, 180);
            btnAnalyze.ForeColor = Color.White;

            // Checkbox
            chkAutoAnalyze.ForeColor = textColor;

            // TextBox
            txtFen.BackColor = panelColor;
            txtFen.ForeColor = textColor;

            // ListBox
            moveListBox.BackColor = panelColor;
            moveListBox.ForeColor = textColor;

            // RichTextBox
            analysisOutput.BackColor = panelColor;
            analysisOutput.ForeColor = textColor;

            // Update FEN display
            UpdateFenDisplay();
        }

        private void InitializeServices()
        {
            // Initialize console formatter for analysis output
            consoleFormatter = new ConsoleOutputFormatter(
                analysisOutput,
                config,
                MovesExplanation.GenerateMoveExplanation);

            // Initialize opening book service
            openingBookService = new PolyglotBookService();
            if (config?.UseOpeningBook == true && !string.IsNullOrEmpty(config.OpeningBooksFolder))
            {
                string booksPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.OpeningBooksFolder);
                if (Directory.Exists(booksPath))
                {
                    openingBookService.LoadBooksFromFolder(booksPath);
                }
            }
        }

        #region Event Handlers

        private void LeftPanel_Resize(object? sender, EventArgs e)
        {
            // Guard against resize during initialization
            if (boardControl == null || lblTurn == null || btnNewGame == null || chkAutoAnalyze == null)
                return;

            if (sender is Panel panel)
            {
                // Calculate the largest square that fits in the available space
                // Leave room for controls below (about 110 pixels)
                int availableWidth = panel.Width - 20; // 10px padding on each side
                int availableHeight = panel.Height - 110; // Room for controls below

                int boardSize = Math.Min(availableWidth, availableHeight);
                boardSize = Math.Max(boardSize, 300); // Minimum size

                // Center the board horizontally in the panel
                int boardX = (panel.Width - boardSize) / 2;
                boardX = Math.Max(boardX, 10); // Minimum left margin

                // Resize and position the board
                boardControl.Size = new Size(boardSize, boardSize);
                boardControl.Location = new Point(boardX, 5);

                // Reposition controls below the board
                int controlsY = boardControl.Bottom + 5;

                lblTurn.Location = new Point(boardX, controlsY);

                int buttonY = controlsY + 25;
                int buttonWidth = Math.Min(90, (boardSize - 30) / 5);
                int buttonSpacing = 5;

                btnNewGame.Location = new Point(boardX, buttonY);
                btnNewGame.Width = buttonWidth;

                btnFlipBoard.Location = new Point(boardX + buttonWidth + buttonSpacing, buttonY);
                btnFlipBoard.Width = buttonWidth;

                btnTakeBack.Location = new Point(boardX + 2 * (buttonWidth + buttonSpacing), buttonY);
                btnTakeBack.Width = buttonWidth;

                // Navigation buttons (smaller)
                int navButtonWidth = 35;
                int navX = boardX + 3 * (buttonWidth + buttonSpacing);
                btnPrevMove.Location = new Point(navX, buttonY);
                btnPrevMove.Width = navButtonWidth;
                btnNextMove.Location = new Point(navX + navButtonWidth + 2, buttonY);
                btnNextMove.Width = navButtonWidth;

                btnAnalyze.Location = new Point(btnNextMove.Right + buttonSpacing, buttonY);
                btnAnalyze.Width = buttonWidth + 10;

                // Auto-analyze checkbox
                chkAutoAnalyze.Location = new Point(btnAnalyze.Right + 5, buttonY + 4);

                // FEN input row
                int fenY = buttonY + 32;
                int fenLabelWidth = 35;
                int fenButtonWidth = 55;
                int fenInputWidth = Math.Max(150, boardSize - fenLabelWidth - 2 * fenButtonWidth - 20);

                lblFen.Location = new Point(boardX, fenY + 3);
                txtFen.Location = new Point(boardX + fenLabelWidth, fenY);
                txtFen.Width = fenInputWidth;
                btnLoadFen.Location = new Point(txtFen.Right + 5, fenY);
                btnCopyFen.Location = new Point(btnLoadFen.Right + 5, fenY);

                // Status label
                lblStatus.Location = new Point(boardX, fenY + 30);
                lblStatus.Width = boardSize;

                // Match middle and right panel heights to the board
                int panelHeight = boardControl.Bottom;
                if (middlePanel != null)
                {
                    middlePanel.Height = panelHeight;
                }
                if (rightPanel != null)
                {
                    rightPanel.Height = panelHeight;
                }
            }
        }

        private void BoardControl_MoveMade(object? sender, MoveEventArgs e)
        {
            // Skip if we're navigating (not making a new move)
            if (isNavigating) return;

            // If we're not at the latest position, truncate the history (branching)
            if (currentMoveIndex >= 0 && currentMoveIndex < moveHistory.Count - 1)
            {
                // Remove moves after current position
                int removeFrom = currentMoveIndex + 1;
                moveHistory.RemoveRange(removeFrom, moveHistory.Count - removeFrom);
                // fenHistory has one more entry (initial position), so remove from removeFrom + 1
                fenHistory.RemoveRange(removeFrom + 1, fenHistory.Count - removeFrom - 1);
            }

            // Add move to history
            moveHistory.Add(e.UciMove);
            fenHistory.Add(e.FEN);
            currentMoveIndex = moveHistory.Count - 1;

            // Update move list display
            UpdateMoveList();
            UpdateFenDisplay();
            UpdateTurnLabel();

            // Auto-analyze if enabled
            if (chkAutoAnalyze.Checked)
            {
                _ = TriggerAutoAnalysis();
            }
        }

        private void ChkAutoAnalyze_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkAutoAnalyze.Checked)
            {
                lblStatus.Text = "Auto-analysis enabled";
                // Analyze current position immediately when enabled
                _ = TriggerAutoAnalysis();
            }
            else
            {
                // Cancel any pending analysis
                autoAnalysisCts?.Cancel();
                lblStatus.Text = "Auto-analysis disabled";
            }
        }

        private async Task TriggerAutoAnalysis()
        {
            // Cancel previous analysis if still running
            autoAnalysisCts?.Cancel();
            autoAnalysisCts = new CancellationTokenSource();
            var token = autoAnalysisCts.Token;

            try
            {
                // Small delay to avoid analyzing during rapid moves
                await Task.Delay(150, token);

                if (!token.IsCancellationRequested)
                {
                    await AnalyzeCurrentPosition();
                }
            }
            catch (OperationCanceledException)
            {
                // Analysis was cancelled, that's fine
            }
        }

        private void BoardControl_BoardChanged(object? sender, EventArgs e)
        {
            UpdateFenDisplay();
            UpdateTurnLabel();
        }

        private void BtnNewGame_Click(object? sender, EventArgs e)
        {
            boardControl.ResetBoard();
            moveHistory.Clear();
            fenHistory.Clear();
            fenHistory.Add(boardControl.GetFEN());
            currentMoveIndex = -1;
            moveListBox.Items.Clear();
            analysisOutput.Clear();
            UpdateFenDisplay();
            UpdateTurnLabel();
            lblStatus.Text = "New game started";
        }

        private void BtnFlipBoard_Click(object? sender, EventArgs e)
        {
            boardControl.FlipBoard();
        }

        private void BtnTakeBack_Click(object? sender, EventArgs e)
        {
            if (moveHistory.Count > 0)
            {
                moveHistory.RemoveAt(moveHistory.Count - 1);
                fenHistory.RemoveAt(fenHistory.Count - 1);
                currentMoveIndex = moveHistory.Count - 1;

                // Load previous position
                boardControl.LoadFEN(fenHistory[fenHistory.Count - 1]);

                UpdateMoveList();
                UpdateFenDisplay();
                UpdateTurnLabel();
                lblStatus.Text = "Move taken back";
            }
        }

        private void BtnPrevMove_Click(object? sender, EventArgs e)
        {
            if (currentMoveIndex >= 0)
            {
                isNavigating = true;
                try
                {
                    currentMoveIndex--;
                    int fenIndex = currentMoveIndex + 1;
                    if (fenIndex >= 0 && fenIndex < fenHistory.Count)
                    {
                        boardControl.LoadFEN(fenHistory[fenIndex]);
                        UpdateFenDisplay();
                        UpdateTurnLabel();
                        UpdateMoveListSelection();
                        lblStatus.Text = currentMoveIndex < 0 ? "Start position" : $"Move {currentMoveIndex + 1}";

                        // Auto-analyze if enabled
                        if (chkAutoAnalyze.Checked)
                        {
                            _ = TriggerAutoAnalysis();
                        }
                    }
                }
                finally
                {
                    isNavigating = false;
                }
            }
        }

        private void BtnNextMove_Click(object? sender, EventArgs e)
        {
            if (currentMoveIndex < moveHistory.Count - 1)
            {
                isNavigating = true;
                try
                {
                    currentMoveIndex++;
                    int fenIndex = currentMoveIndex + 1;
                    if (fenIndex < fenHistory.Count)
                    {
                        boardControl.LoadFEN(fenHistory[fenIndex]);
                        UpdateFenDisplay();
                        UpdateTurnLabel();
                        UpdateMoveListSelection();
                        lblStatus.Text = $"Move {currentMoveIndex + 1}";

                        // Auto-analyze if enabled
                        if (chkAutoAnalyze.Checked)
                        {
                            _ = TriggerAutoAnalysis();
                        }
                    }
                }
                finally
                {
                    isNavigating = false;
                }
            }
        }

        private async void BtnAnalyze_Click(object? sender, EventArgs e)
        {
            await AnalyzeCurrentPosition();
        }

        private void BtnLoadFen_Click(object? sender, EventArgs e)
        {
            string fen = txtFen.Text.Trim();
            if (!string.IsNullOrEmpty(fen))
            {
                try
                {
                    boardControl.LoadFEN(fen);
                    moveHistory.Clear();
                    fenHistory.Clear();
                    fenHistory.Add(fen);
                    currentMoveIndex = -1;
                    moveListBox.Items.Clear();
                    analysisOutput.Clear();
                    UpdateTurnLabel();
                    lblStatus.Text = "Position loaded from FEN";
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Invalid FEN: {ex.Message}";
                }
            }
        }

        private void BtnCopyFen_Click(object? sender, EventArgs e)
        {
            string fen = boardControl.GetFEN();
            Clipboard.SetText(fen);
            lblStatus.Text = "FEN copied to clipboard";
        }

        private void MoveListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Skip if we're already navigating (triggered by UpdateMoveListSelection)
            if (isNavigating) return;

            int selected = moveListBox.SelectedIndex;
            if (selected >= 0 && selected < fenHistory.Count - 1)
            {
                isNavigating = true;
                try
                {
                    // Each listbox item contains a full move (white + black)
                    // Navigate to the position after the last move in that pair
                    int halfMoveIndex = (selected + 1) * 2 - 1;
                    if (halfMoveIndex >= moveHistory.Count)
                        halfMoveIndex = moveHistory.Count - 1; // Clamp if odd number of moves

                    int fenIndex = halfMoveIndex + 1;
                    if (fenIndex < fenHistory.Count)
                    {
                        boardControl.LoadFEN(fenHistory[fenIndex]);
                        currentMoveIndex = halfMoveIndex;
                        UpdateFenDisplay();
                        UpdateTurnLabel();

                        // Auto-analyze if enabled
                        if (chkAutoAnalyze.Checked)
                        {
                            _ = TriggerAutoAnalysis();
                        }
                    }
                }
                finally
                {
                    isNavigating = false;
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Intercept arrow keys before they're used for control navigation
            switch (keyData)
            {
                case Keys.Left:
                    BtnPrevMove_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Right:
                    BtnNextMove_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Home:
                    if (fenHistory.Count > 0)
                    {
                        isNavigating = true;
                        try
                        {
                            currentMoveIndex = -1;
                            boardControl.LoadFEN(fenHistory[0]);
                            UpdateFenDisplay();
                            UpdateTurnLabel();
                            UpdateMoveListSelection();
                            lblStatus.Text = "Start position";

                            // Auto-analyze if enabled
                            if (chkAutoAnalyze.Checked)
                            {
                                _ = TriggerAutoAnalysis();
                            }
                        }
                        finally
                        {
                            isNavigating = false;
                        }
                    }
                    return true;
                case Keys.End:
                    if (moveHistory.Count > 0)
                    {
                        isNavigating = true;
                        try
                        {
                            currentMoveIndex = moveHistory.Count - 1;
                            boardControl.LoadFEN(fenHistory[fenHistory.Count - 1]);
                            UpdateFenDisplay();
                            UpdateTurnLabel();
                            UpdateMoveListSelection();
                            lblStatus.Text = $"Move {currentMoveIndex + 1}";

                            // Auto-analyze if enabled
                            if (chkAutoAnalyze.Checked)
                            {
                                _ = TriggerAutoAnalysis();
                            }
                        }
                        finally
                        {
                            isNavigating = false;
                        }
                    }
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void AnalysisBoardForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // Arrow keys and Home/End are handled in ProcessCmdKey
            if (e.KeyCode == Keys.F2)
            {
                BtnAnalyze_Click(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Back)
            {
                BtnTakeBack_Click(sender, e);
                e.Handled = true;
            }
        }

        #endregion

        #region Analysis

        private async Task AnalyzeCurrentPosition()
        {
            if (engineService == null)
            {
                lblStatus.Text = "Engine not available";
                return;
            }

            string fen = boardControl.GetFEN();
            lblStatus.Text = "Analyzing...";
            btnAnalyze.Enabled = false;

            try
            {
                // Get engine analysis
                int depth = config?.EngineDepth ?? 15;
                int multiPV = 3; // Always get 3 lines for analysis board

                var result = await engineService.GetBestMoveAsync(fen, depth, multiPV);

                if (string.IsNullOrEmpty(result.bestMove))
                {
                    lblStatus.Text = "Analysis failed";
                    btnAnalyze.Enabled = true;
                    return;
                }

                // Apply aggressiveness filter
                var candidates = new List<(string move, string evaluation, string pvLine, int sharpness)>();
                var pvs = result.pvs ?? new List<string>();
                var evals = result.evaluations ?? new List<string>();

                for (int i = 0; i < Math.Min(pvs.Count, evals.Count); i++)
                {
                    string pvLine = pvs[i];
                    string eval = evals[i];
                    string firstMove = pvLine.Split(' ')[0];
                    int sharpness = sharpnessAnalyzer.CalculateSharpness(firstMove, fen, eval, pvLine);
                    candidates.Add((firstMove, eval, pvLine, sharpness));
                }

                // Select based on style
                int aggressiveness = config?.Aggressiveness ?? 50;
                string recommendedMove = result.bestMove;

                if (candidates.Count >= 2 && aggressiveness != 50)
                {
                    int selectedIndex = sharpnessAnalyzer.SelectMoveByAggressiveness(candidates, aggressiveness, 0.30);
                    if (selectedIndex >= 0 && selectedIndex < candidates.Count)
                    {
                        recommendedMove = candidates[selectedIndex].move;
                    }
                }

                // Get book moves
                List<BookMove>? bookMoves = null;
                if (openingBookService?.IsLoaded == true)
                {
                    var moves = openingBookService.GetBookMovesForPosition(fen);
                    if (moves != null && moves.Count > 0)
                    {
                        bookMoves = moves.Select(pm => new BookMove
                        {
                            UciMove = pm.UciMove,
                            Games = pm.Weight,
                            Priority = pm.Weight,
                            WinRate = 50,
                            Wins = 0,
                            Losses = 0,
                            Draws = 0,
                            Source = "Book"
                        }).ToList();
                    }
                }

                // Display results
                consoleFormatter?.DisplayAnalysisResults(
                    recommendedMove,
                    result.evaluation,
                    pvs,
                    evals,
                    fen,
                    null, // No previous eval for blunder detection
                    true, // Show best line
                    true, // Show second line
                    true, // Show third line
                    result.wdl,
                    bookMoves);

                lblStatus.Text = $"Analysis complete (depth {depth})";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Analysis error: {ex.Message}";
                Debug.WriteLine($"Analysis error: {ex}");
            }
            finally
            {
                btnAnalyze.Enabled = true;
            }
        }

        #endregion

        #region Helpers

        private void UpdateMoveList()
        {
            moveListBox.Items.Clear();

            for (int i = 0; i < moveHistory.Count; i++)
            {
                int moveNum = (i / 2) + 1;
                bool isWhiteMove = i % 2 == 0;

                string sanMove = ConvertUciToSan(moveHistory[i], fenHistory[i]);

                if (isWhiteMove)
                {
                    moveListBox.Items.Add($"{moveNum}. {sanMove}");
                }
                else
                {
                    // Update last item to include black's move
                    if (moveListBox.Items.Count > 0)
                    {
                        string lastItem = moveListBox.Items[moveListBox.Items.Count - 1].ToString() ?? "";
                        moveListBox.Items[moveListBox.Items.Count - 1] = $"{lastItem} {sanMove}";
                    }
                }
            }

            // Scroll to bottom
            if (moveListBox.Items.Count > 0)
            {
                moveListBox.TopIndex = moveListBox.Items.Count - 1;
            }
        }

        private void UpdateFenDisplay()
        {
            txtFen.Text = boardControl.GetFEN();
        }

        private void UpdateTurnLabel()
        {
            lblTurn.Text = boardControl.WhiteToMove ? "White to move" : "Black to move";
            lblTurn.ForeColor = boardControl.WhiteToMove
                ? (config?.Theme == "Dark" ? Color.White : Color.Black)
                : (config?.Theme == "Dark" ? Color.LightGray : Color.DimGray);
        }

        private void UpdateMoveListSelection()
        {
            // Highlight the current move in the list
            // Each listbox item contains a full move (white + black)
            if (currentMoveIndex < 0)
            {
                moveListBox.ClearSelected();
            }
            else
            {
                int itemIndex = currentMoveIndex / 2; // Each item has 2 half-moves
                if (itemIndex < moveListBox.Items.Count)
                {
                    moveListBox.SelectedIndex = itemIndex;
                }
            }
        }

        private string ConvertUciToSan(string uciMove, string fen)
        {
            try
            {
                return ChessNotationService.ConvertFullPvToSan(
                    uciMove, fen,
                    ChessRulesService.ApplyUciMove,
                    ChessRulesService.CanReachSquare,
                    ChessRulesService.FindAllPiecesOfSameType);
            }
            catch
            {
                return uciMove; // Fallback to UCI notation
            }
        }

        #endregion
    }
}
