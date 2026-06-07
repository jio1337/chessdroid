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

            // If the first child is a PV-only line (no real move played from here yet),
            // display all children as variations so they render with variation styling.
            if (mainChild.IsFromPv)
            {
                for (int i = 0; i < parentNode.Children.Count; i++)
                    AddVariationRows(parentNode.Children[i], indentLevel + 1);
                return;
            }

            if (mainChild.IsWhiteMove)
            {
                // Try to pair white with its black response.
                MoveNode? blackNode = (mainChild.Children.Count > 0 && !mainChild.Children[0].IsWhiteMove)
                    ? mainChild.Children[0] : null;

                if (blackNode != null)
                {
                    AddPairedRow(mainChild, blackNode);
                    // Continue main line from the black node.
                    // BuildPairedMainLine(blackNode) handles blackNode.Children[1..] at its own tail —
                    // do NOT iterate them here or they are emitted twice.
                    BuildPairedMainLine(blackNode, indentLevel);

                    // Alternative black responses to mainChild (e.g. 3...Qd8 instead of 3...Qa5).
                    for (int i = 1; i < mainChild.Children.Count; i++)
                        AddVariationRows(mainChild.Children[i], indentLevel + 1);
                }
                else
                {
                    // White move with no black response (game ends on white's turn).
                    AddSingleRow(mainChild, indentLevel, isVariation: false);
                    BuildPairedMainLine(mainChild, indentLevel);
                }
            }
            else
            {
                // Game started with black to move — single row for black.
                AddSingleRow(mainChild, indentLevel, isVariation: false);
                BuildPairedMainLine(mainChild, indentLevel);
            }

            // Alternative continuations at this node (siblings of the main child).
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

        // Emits a variation branch with white+black pairing, matching the main-line grid style.
        private void AddVariationRows(MoveNode startNode, int indentLevel)
        {
            bool isStart = true;
            MoveNode? node = startNode;

            while (node != null)
            {
                MoveNode? next;

                if (!node.IsWhiteMove)
                {
                    // Variation begins with a black move (alternative to main black response).
                    moveListBox.Items.Add($"({node.MoveNumber}...{node.SanMove}");
                    _movePairs.Add(new MovePair { White = node, IsVariation = true, IndentLevel = indentLevel, IsVariationStart = isStart });
                    isStart = false;

                    for (int i = 1; i < node.Children.Count; i++)
                        AddVariationRows(node.Children[i], indentLevel + 1);

                    next = node.Children.Count > 0 ? node.Children[0] : null;
                }
                else
                {
                    // White move — pair with black response if one follows.
                    MoveNode? blackNode = (node.Children.Count > 0 && !node.Children[0].IsWhiteMove)
                        ? node.Children[0] : null;

                    moveListBox.Items.Add(blackNode != null
                        ? $"{node.MoveNumber}. {node.SanMove} | {blackNode.SanMove}"
                        : $"{node.MoveNumber}. {node.SanMove}");
                    _movePairs.Add(new MovePair { White = node, Black = blackNode, IsVariation = true, IndentLevel = indentLevel, IsVariationStart = isStart });
                    isStart = false;

                    // Alternative black responses to this white move.
                    for (int i = 1; i < node.Children.Count; i++)
                        AddVariationRows(node.Children[i], indentLevel + 1);

                    if (blackNode != null)
                    {
                        // Alternative white continuations after the black response.
                        for (int i = 1; i < blackNode.Children.Count; i++)
                            AddVariationRows(blackNode.Children[i], indentLevel + 1);

                        next = blackNode.Children.Count > 0 ? blackNode.Children[0] : null;
                    }
                    else
                    {
                        next = null;
                    }
                }

                node = next;
            }
        }

        // Ensures a real played move is always the main-line child (Children[0]).
        // When a PV was inserted first and the user later plays a move, that move lands
        // at Children[1+]. This promotes it to Children[0] so BuildPairedMainLine sees it
        // as the main continuation and the PV falls back to a variation.
        private void PromoteToMainLine(MoveNode node)
        {
            if (node.Parent == null) return;
            node.IsFromPv = false;
            node.VariationDepth = node.Parent.VariationDepth;
            var siblings = node.Parent.Children;
            int idx = siblings.IndexOf(node);
            if (idx > 0)
            {
                siblings.RemoveAt(idx);
                siblings.Insert(0, node);
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
                            node.IsFromPv = true;
                            moveTree.GoToNode(node);
                        }
                    }
                    else
                    {
                        node = moveTree.AddMove(uciMove, san, newFen);
                        node.IsFromPv = true;
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
