using System.Diagnostics;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    public partial class AnalysisBoardForm : Form
    {
        #region Helpers

        private void UpdateMoveList()
        {
            moveListBox.BeginUpdate();
            try
            {
                moveListBox.Items.Clear();
                _movePairs.Clear();

                BuildPairedMainLine(moveTree.Root, 0);
            }
            finally
            {
                moveListBox.EndUpdate();
                moveListBox.Font = _consoleFont;
                moveListBox.ItemHeight = Math.Max(14, (int)Math.Ceiling(_consoleFont.GetHeight()));
            }

            UpdateMoveListSelection();
        }

        // Builds the main-line move grid (paired white+black rows) plus variation rows.
        // parentNode.Children[0] is the main line; Children[1..] are variations at this node.
        private void BuildPairedMainLine(MoveNode parentNode, int indentLevel)
        {
            if (parentNode.Children.Count == 0) return;

            var mainChild = parentNode.Children[0];

            if (mainChild.IsWhiteMove)
            {
                // Try to pair white with its black response.
                // Only pair if the white node's first child is a black move (no white variation kids).
                MoveNode? blackNode = (mainChild.Children.Count > 0 && !mainChild.Children[0].IsWhiteMove)
                    ? mainChild.Children[0] : null;

                if (blackNode != null)
                {
                    AddPairedRow(mainChild, blackNode);
                    BuildPairedMainLine(blackNode, indentLevel);

                    // Variations from the black node (alternative white continuations).
                    for (int i = 1; i < blackNode.Children.Count; i++)
                        AddVariationRows(blackNode.Children[i], indentLevel + 1);

                    // Variations from the white node (alternative black responses).
                    for (int i = 1; i < mainChild.Children.Count; i++)
                        AddVariationRows(mainChild.Children[i], indentLevel + 1);
                }
                else
                {
                    // White move with no black response yet (game ends on white's turn or
                    // white has child variations but no clean black response).
                    AddSingleRow(mainChild, indentLevel, isVariation: false);
                    BuildPairedMainLine(mainChild, indentLevel);

                    for (int i = 1; i < mainChild.Children.Count; i++)
                        AddVariationRows(mainChild.Children[i], indentLevel + 1);
                }
            }
            else
            {
                // Game started with black to move — single row for black.
                AddSingleRow(mainChild, indentLevel, isVariation: false);
                BuildPairedMainLine(mainChild, indentLevel);

                for (int i = 1; i < mainChild.Children.Count; i++)
                    AddVariationRows(mainChild.Children[i], indentLevel + 1);
            }

            // Variations on the parent (alternatives to the main child).
            for (int i = 1; i < parentNode.Children.Count; i++)
                AddVariationRows(parentNode.Children[i], indentLevel + 1);
        }

        private void AddPairedRow(MoveNode white, MoveNode black)
        {
            var pair = new MovePair { White = white, Black = black };
            moveListBox.Items.Add($"{white.MoveNumber}. {white.SanMove} | {black.SanMove}");
            _movePairs.Add(pair);
        }

        private void AddSingleRow(MoveNode node, int indentLevel, bool isVariation)
        {
            string indent = new string(' ', indentLevel * 2);
            string text = node.IsWhiteMove
                ? $"{indent}{node.MoveNumber}. {node.SanMove}"
                : $"{indent}{node.MoveNumber}...{node.SanMove}";

            var pair = new MovePair { White = node, IsVariation = isVariation };
            moveListBox.Items.Add(text);
            _movePairs.Add(pair);
        }

        // Emits a variation branch as full-width indented rows.
        private void AddVariationRows(MoveNode startNode, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 2);
            string firstText = startNode.IsWhiteMove
                ? $"{indent}({startNode.MoveNumber}. {startNode.SanMove}"
                : $"{indent}({startNode.MoveNumber}...{startNode.SanMove}";

            moveListBox.Items.Add(firstText);
            _movePairs.Add(new MovePair { White = startNode, IsVariation = true });

            var current = startNode;
            while (current.Children.Count > 0)
            {
                var next = current.Children[0];
                string moveText = next.IsWhiteMove
                    ? $"{indent}  {next.MoveNumber}. {next.SanMove}"
                    : $"{indent}  {next.SanMove}";

                moveListBox.Items.Add(moveText);
                _movePairs.Add(new MovePair { White = next, IsVariation = true });

                // Nested variations within this branch.
                for (int i = 1; i < current.Children.Count; i++)
                    AddVariationRows(current.Children[i], indentLevel + 2);

                current = next;
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
                ? (ThemeService.IsDarkTheme(config?.Theme) ? Color.White : Color.Black)
                : (ThemeService.IsDarkTheme(config?.Theme) ? Color.LightGray : Color.DimGray);
        }

        private void UpdateMoveListSelection()
        {
            var current = moveTree.CurrentNode;
            if (current == moveTree.Root)
            {
                _activeNode = null;
                moveListBox.ClearSelected();
                RefreshEvalGraph();
                return;
            }

            int idx = _movePairs.FindIndex(p => p.White == current || p.Black == current);
            if (idx >= 0 && idx < moveListBox.Items.Count)
            {
                _activeNode = current;
                moveListBox.SelectedIndex = idx;
                moveListBox.Invalidate(moveListBox.GetItemRectangle(idx));
            }

            RefreshEvalGraph();
        }

        private void RefreshEvalGraph()
        {
            if (_evalGraph == null || moveTree == null) return;
            var current = moveTree.CurrentNode == moveTree.Root ? null : moveTree.CurrentNode;
            _evalGraph.SetData(moveTree, current, ThemeService.IsDarkTheme(config?.Theme));
        }

        private void EvalGraph_MoveNodeSelected(MoveNode node)
        {
            if (isNavigating || matchRunning) return;
            isNavigating = true;
            try
            {
                moveTree.GoToNode(node);
                boardControl.LoadFEN(node.FEN);
                UpdateMoveAnnotation(node);
                UpdateFenDisplay();
                UpdateTurnLabel();
                UpdateMoveListSelection();
                lblStatus.Text = $"Move {node.MoveNumber}";
                if (!matchRunning) _ = TriggerAutoAnalysis();
            }
            finally
            {
                isNavigating = false;
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
            catch (Exception ex)
            {
                Debug.WriteLine($"ConvertUciToSan failed for '{uciMove}': {ex.Message}");
                return uciMove; // Fallback to UCI notation
            }
        }

        private void InsertPvIntoMoveTree(string pvUci, string startFen)
        {
            if (string.IsNullOrEmpty(pvUci)) return;

            // Verify we're still at the position the PV was computed for
            if (moveTree.CurrentNode.FEN != startFen)
                return;

            // Cancel any running PV animation
            _pvAnimationCts?.Cancel();
            _pvAnimationCts = new CancellationTokenSource();

            var savedNode = moveTree.CurrentNode;
            string currentFen = startFen;
            string[] uciMoves = pvUci.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var insertedNodes = new List<MoveNode>();

            try
            {
                foreach (string uciMove in uciMoves)
                {
                    if (uciMove.Length < 4) break;

                    // Convert UCI to SAN for display
                    string san;
                    try
                    {
                        san = ChessNotationService.ConvertUCIToSAN(
                            uciMove, currentFen,
                            ChessRulesService.CanReachSquare,
                            ChessRulesService.FindAllPiecesOfSameType);
                    }
                    catch
                    {
                        san = uciMove;
                    }

                    // Compute new FEN by applying the move to a temp board
                    var fenParts = currentFen.Split(' ');
                    string castling = fenParts.Length > 2 ? fenParts[2] : "-";
                    string enPassant = fenParts.Length > 3 ? fenParts[3] : "-";
                    bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";

                    var tempBoard = ChessBoard.FromFEN(currentFen);
                    ChessRulesService.ApplyUciMove(tempBoard, uciMove, ref castling, ref enPassant);

                    string nextSide = whiteToMove ? "b" : "w";
                    string newFen = $"{tempBoard.ToFEN()} {nextSide} {castling} {enPassant} 0 1";

                    // First move always starts a variation; rest chain naturally
                    MoveNode node;
                    if (insertedNodes.Count == 0)
                    {
                        // Check if this exact move already exists as a child
                        var existing = moveTree.CurrentNode.FindChild(uciMove);
                        if (existing != null)
                        {
                            node = existing;
                            moveTree.GoToNode(existing);
                        }
                        else
                        {
                            node = moveTree.CurrentNode.AddChild(uciMove, san, newFen, forceVariation: true);
                            moveTree.GoToNode(node);
                        }
                    }
                    else
                    {
                        node = moveTree.AddMove(uciMove, san, newFen);
                    }
                    insertedNodes.Add(node);
                    currentFen = newFen;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InsertPvIntoMoveTree error: {ex.Message}");
            }

            // Navigate back to start, update move list to show the new variation
            moveTree.GoToNode(savedNode);
            UpdateMoveList();

            // Animate through the variation on the board
            if (insertedNodes.Count > 0)
            {
                boardControl.ClearEngineArrows();
                _ = AnimatePvLineAsync(insertedNodes, _pvAnimationCts.Token);
            }
        }

        private async Task AnimatePvLineAsync(List<MoveNode> nodes, CancellationToken ct)
        {
            isNavigating = true;
            bool completed = false;
            try
            {
                foreach (var node in nodes)
                {
                    await Task.Delay(400, ct);
                    if (ct.IsCancellationRequested) return;

                    moveTree.GoToNode(node);
                    boardControl.LoadFEN(node.FEN);

                    // Highlight the move that was just played so it's visually clear
                    if (node.UciMove.Length >= 4)
                    {
                        int fromCol = node.UciMove[0] - 'a';
                        int fromRow = 7 - (node.UciMove[1] - '1');
                        int toCol   = node.UciMove[2] - 'a';
                        int toRow   = 7 - (node.UciMove[3] - '1');
                        boardControl.LastMove = (fromRow, fromCol, toRow, toCol);
                    }

                    // Force immediate repaint so each move is visible before the next delay
                    boardControl.Refresh();

                    UpdateMoveListSelection();
                    UpdateFenDisplay();
                    UpdateTurnLabel();

                    string statusText = $"Move {node.MoveNumber}";
                    if (node.VariationDepth > 0)
                        statusText += " (variation)";
                    lblStatus.Text = statusText;
                }
                completed = true;
            }
            catch (TaskCanceledException) { }
            finally
            {
                isNavigating = false;
            }

            // Analyze the final position once the animation finishes naturally
            if (completed && !matchRunning)
                _ = TriggerAutoAnalysis();
        }

        #endregion
    }
}
