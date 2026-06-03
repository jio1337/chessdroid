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
                displayedNodes.Clear();

                // Build the display list with variations
                BuildMoveListRecursive(moveTree.Root, 0);
            }
            finally
            {
                moveListBox.EndUpdate();
                // Re-assert font and item height — WinForms OwnerDrawFixed can reset these
                // after Items.Clear() + EndUpdate in some scenarios.
                moveListBox.Font = _consoleFont;
                moveListBox.ItemHeight = Math.Max(14, (int)Math.Ceiling(_consoleFont.GetHeight()));
            }

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

                // Process this node's children (continue main line at same indent)
                if (child.Children.Count > 0)
                {
                    // First child continues the main line
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
                moveListBox.ClearSelected();
                RefreshEvalGraph();
                return;
            }

            int idx = displayedNodes.IndexOf(current);
            if (idx >= 0 && idx < moveListBox.Items.Count)
                moveListBox.SelectedIndex = idx;

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
