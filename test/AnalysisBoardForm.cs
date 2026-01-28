using System.Diagnostics;
using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    /// <summary>
    /// Offline analysis board form with interactive chess board, move list, and engine analysis.
    /// Provides a complete analysis experience without needing internet connection.
    /// Supports variations/alternative lines through a move tree structure.
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

        // Game state - move tree for variations support
        private MoveTree moveTree = null!;
        private bool isNavigating = false;

        // Track nodes for listbox mapping
        private List<MoveNode> displayedNodes = new List<MoveNode>();

        public AnalysisBoardForm(AppConfig config, ChessEngineService? sharedEngineService = null)
        {
            this.config = config;
            this.engineService = sharedEngineService ?? new ChessEngineService(config);
            this.sharpnessAnalyzer = new MoveSharpnessAnalyzer();

            InitializeComponent();
            ApplyTheme();
            InitializeServices();

            // Initialize move tree with starting position
            moveTree = new MoveTree(boardControl.GetFEN());
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

            // Convert UCI to SAN for display
            string san = ConvertUciToSan(e.UciMove, moveTree.CurrentNode.FEN);

            // Add move to tree (handles variations automatically)
            moveTree.AddMove(e.UciMove, san, e.FEN);

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
            moveTree.Clear(boardControl.GetFEN());
            moveListBox.Items.Clear();
            displayedNodes.Clear();
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
            // Take back removes the current move from the tree
            if (moveTree.CurrentNode != moveTree.Root)
            {
                var parent = moveTree.CurrentNode.Parent;
                if (parent != null)
                {
                    // Remove this node from parent's children
                    parent.Children.Remove(moveTree.CurrentNode);
                    moveTree.CurrentNode = parent;

                    // Load previous position
                    boardControl.LoadFEN(parent.FEN);

                    UpdateMoveList();
                    UpdateFenDisplay();
                    UpdateTurnLabel();
                    lblStatus.Text = "Move taken back";
                }
            }
        }

        private void BtnPrevMove_Click(object? sender, EventArgs e)
        {
            if (moveTree.GoBack())
            {
                isNavigating = true;
                try
                {
                    boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                    UpdateFenDisplay();
                    UpdateTurnLabel();
                    UpdateMoveListSelection();

                    string statusText = moveTree.CurrentNode == moveTree.Root
                        ? "Start position"
                        : $"Move {moveTree.CurrentNode.MoveNumber}";
                    if (moveTree.CurrentNode.VariationDepth > 0)
                        statusText += $" (variation)";
                    lblStatus.Text = statusText;

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
        }

        private void BtnNextMove_Click(object? sender, EventArgs e)
        {
            if (moveTree.GoForward())
            {
                isNavigating = true;
                try
                {
                    boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                    UpdateFenDisplay();
                    UpdateTurnLabel();
                    UpdateMoveListSelection();

                    string statusText = $"Move {moveTree.CurrentNode.MoveNumber}";
                    if (moveTree.CurrentNode.VariationDepth > 0)
                        statusText += $" (variation)";
                    lblStatus.Text = statusText;

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
                    moveTree.Clear(fen);
                    moveListBox.Items.Clear();
                    displayedNodes.Clear();
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
            if (selected >= 0 && selected < displayedNodes.Count)
            {
                isNavigating = true;
                try
                {
                    var node = displayedNodes[selected];
                    moveTree.GoToNode(node);
                    boardControl.LoadFEN(node.FEN);
                    UpdateFenDisplay();
                    UpdateTurnLabel();

                    string statusText = $"Move {node.MoveNumber}";
                    if (node.VariationDepth > 0)
                        statusText += $" (variation)";
                    lblStatus.Text = statusText;

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
                case Keys.Up:
                    // Navigate to previous variation at same level
                    NavigateVariation(-1);
                    return true;
                case Keys.Down:
                    // Navigate to next variation at same level
                    NavigateVariation(1);
                    return true;
                case Keys.Home:
                    NavigateToStart();
                    return true;
                case Keys.End:
                    NavigateToEnd();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void NavigateToStart()
        {
            isNavigating = true;
            try
            {
                moveTree.GoToStart();
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
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

        private void NavigateToEnd()
        {
            isNavigating = true;
            try
            {
                moveTree.GoToEnd();
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                UpdateFenDisplay();
                UpdateTurnLabel();
                UpdateMoveListSelection();

                string statusText = moveTree.CurrentNode == moveTree.Root
                    ? "Start position"
                    : $"Move {moveTree.CurrentNode.MoveNumber}";
                if (moveTree.CurrentNode.VariationDepth > 0)
                    statusText += $" (variation)";
                lblStatus.Text = statusText;

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

        private void NavigateVariation(int direction)
        {
            var current = moveTree.CurrentNode;
            MoveNode? target = direction > 0 ? current.NextVariation() : current.PreviousVariation();

            if (target != null)
            {
                isNavigating = true;
                try
                {
                    moveTree.GoToNode(target);
                    boardControl.LoadFEN(target.FEN);
                    UpdateFenDisplay();
                    UpdateTurnLabel();
                    UpdateMoveListSelection();

                    int varIdx = target.GetVariationIndex();
                    lblStatus.Text = $"Move {target.MoveNumber} - Variation {varIdx + 1}";

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
            displayedNodes.Clear();

            // Build the display list with variations
            BuildMoveListRecursive(moveTree.Root, 0);

            // Scroll to current position
            UpdateMoveListSelection();
        }

        private void BuildMoveListRecursive(MoveNode node, int indentLevel)
        {
            // Process main line first
            foreach (var child in node.Children)
            {
                // Add the move to display
                string indent = new string(' ', indentLevel * 2);
                string moveText;

                if (child.IsWhiteMove)
                {
                    moveText = $"{indent}{child.MoveNumber}. {child.SanMove}";
                }
                else
                {
                    moveText = $"{indent}{child.MoveNumber}...{child.SanMove}";
                }

                // Mark variations
                if (child.VariationDepth > 0 || child.GetVariationIndex() > 0)
                {
                    moveText = $"{indent}({child.SanMove})";
                }

                moveListBox.Items.Add(moveText);
                displayedNodes.Add(child);

                // Process this node's children (continue main line)
                if (child.Children.Count > 0)
                {
                    // First child continues the line
                    BuildMoveListRecursive(child, indentLevel);
                }

                // Process variations (children after the first one were already handled)
                // Variations are added when their parent is processed
                break; // Only process first child as main line continuation
            }

            // Now process variations (non-first children)
            if (node.Children.Count > 1)
            {
                for (int i = 1; i < node.Children.Count; i++)
                {
                    var variation = node.Children[i];
                    string indent = new string(' ', (indentLevel + 1) * 2);

                    string varText;
                    if (variation.IsWhiteMove)
                    {
                        varText = $"{indent}({variation.MoveNumber}. {variation.SanMove}";
                    }
                    else
                    {
                        varText = $"{indent}({variation.MoveNumber}...{variation.SanMove}";
                    }

                    moveListBox.Items.Add(varText);
                    displayedNodes.Add(variation);

                    // Continue the variation line
                    BuildVariationLine(variation, indentLevel + 1);
                }
            }
        }

        private void BuildVariationLine(MoveNode node, int indentLevel)
        {
            var current = node;
            while (current.Children.Count > 0)
            {
                var next = current.Children[0];
                string indent = new string(' ', indentLevel * 2);

                string moveText = $"{indent}{next.SanMove}";
                if (next.IsWhiteMove)
                {
                    moveText = $"{indent}{next.MoveNumber}. {next.SanMove}";
                }

                moveListBox.Items.Add(moveText);
                displayedNodes.Add(next);

                // Handle nested variations in this line
                if (current.Children.Count > 1)
                {
                    for (int i = 1; i < current.Children.Count; i++)
                    {
                        var nestedVar = current.Children[i];
                        string nestedIndent = new string(' ', (indentLevel + 1) * 2);
                        moveListBox.Items.Add($"{nestedIndent}({nestedVar.SanMove}");
                        displayedNodes.Add(nestedVar);
                        BuildVariationLine(nestedVar, indentLevel + 2);
                    }
                }

                current = next;
            }

            // Close the variation parenthesis
            if (indentLevel > 0)
            {
                string indent = new string(' ', (indentLevel - 1) * 2);
                // Just visual indication - we don't add a node for closing paren
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
            // Find current node in displayed nodes
            var current = moveTree.CurrentNode;
            if (current == moveTree.Root)
            {
                moveListBox.ClearSelected();
                return;
            }

            int idx = displayedNodes.IndexOf(current);
            if (idx >= 0 && idx < moveListBox.Items.Count)
            {
                moveListBox.SelectedIndex = idx;
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
