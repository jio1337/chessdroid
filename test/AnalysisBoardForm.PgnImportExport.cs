using System.Diagnostics;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    public partial class AnalysisBoardForm : Form
    {
        #region PGN Import/Export

        private void BtnExportPgn_Click(object? sender, EventArgs e)
        {
            try
            {
                string pgn = GeneratePgn();

                using var dialog = new SaveFileDialog
                {
                    Filter = "PGN Files (*.pgn)|*.pgn|All Files (*.*)|*.*",
                    DefaultExt = "pgn",
                    FileName = $"game_{DateTime.Now:yyyyMMdd_HHmmss}.pgn"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, pgn);
                    lblStatus.Text = $"PGN saved to {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Export error: {ex.Message}";
            }
        }

        private void BtnImportPgn_Click(object? sender, EventArgs e)
        {
            // Show a dialog to paste PGN or open from file
            using var inputForm = new Form
            {
                Text = "Import PGN",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            bool isDark = ThemeService.IsDarkTheme(config?.Theme);
            inputForm.BackColor = isDark ? Color.FromArgb(40, 40, 48) : Color.White;

            var lblInstructions = new Label
            {
                Text = "Paste PGN content below or open a file:",
                Location = new Point(10, 10),
                Size = new Size(360, 20),
                ForeColor = isDark ? Color.White : Color.Black
            };

            var btnOpenFile = new Button
            {
                Text = "Open File...",
                Location = new Point(380, 6),
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = isDark ? Color.FromArgb(50, 50, 58) : Color.FromArgb(230, 230, 230),
                ForeColor = isDark ? Color.White : Color.Black
            };

            var txtPgn = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 35),
                Size = new Size(460, 270),
                Font = new Font("Consolas", 9F),
                BackColor = isDark ? Color.FromArgb(30, 30, 35) : Color.White,
                ForeColor = isDark ? Color.White : Color.Black
            };

            btnOpenFile.Click += (s, args) =>
            {
                using var openDialog = new OpenFileDialog
                {
                    Filter = "PGN Files (*.pgn)|*.pgn|All Files (*.*)|*.*",
                    Title = "Open PGN File"
                };
                if (openDialog.ShowDialog(inputForm) == DialogResult.OK)
                {
                    try
                    {
                        txtPgn.Text = File.ReadAllText(openDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error reading file: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            var btnOk = new Button
            {
                Text = "Import",
                Location = new Point(310, 315),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = isDark ? Color.FromArgb(60, 90, 140) : Color.FromArgb(70, 130, 180),
                ForeColor = Color.White
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(395, 315),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = isDark ? Color.FromArgb(50, 50, 58) : Color.FromArgb(230, 230, 230),
                ForeColor = isDark ? Color.White : Color.Black
            };

            inputForm.Controls.AddRange(new Control[] { lblInstructions, btnOpenFile, txtPgn, btnOk, btnCancel });
            inputForm.AcceptButton = btnOk;
            inputForm.CancelButton = btnCancel;

            if (inputForm.ShowDialog(this) == DialogResult.OK)
            {
                string pgnText = txtPgn.Text.Trim();
                if (string.IsNullOrEmpty(pgnText)) return;

                var games = PgnGamePickerDialog.SplitPgnGames(pgnText);
                if (games.Count > 1)
                {
                    using var picker = new PgnGamePickerDialog(games, isDark);
                    if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedPgn != null)
                        ImportPgn(picker.SelectedPgn);
                }
                else
                {
                    ImportPgn(pgnText);
                }
            }
        }

        private void BtnSaveToLibrary_Click(object? sender, EventArgs e)
        {
            if (_libraryService == null) return;
            if (moveTree.GetMainLine().Count == 0)
            {
                lblStatus.Text = "No game to save";
                return;
            }

            // Find the deepest position in the main line that has a known ECO opening
            string opening = OpeningBook.GetOpeningDisplay(moveTree.Root.FEN);
            foreach (var node in moveTree.GetMainLine())
            {
                string o = OpeningBook.GetOpeningDisplay(node.FEN);
                if (!string.IsNullOrEmpty(o)) opening = o;
            }

            string white = !string.IsNullOrEmpty(_matchWhiteName) ? _matchWhiteName : _pgnHeaders.GetValueOrDefault("White", "?");
            string black = !string.IsNullOrEmpty(_matchBlackName) ? _matchBlackName : _pgnHeaders.GetValueOrDefault("Black", "?");

            // If re-saving a library game and names are still unknown, use whatever was last saved
            if (!string.IsNullOrEmpty(_libraryGameId) && _libraryService != null)
            {
                var existing = _libraryService.Load(_libraryGameId);
                if (existing != null)
                {
                    if (white == "?") white = existing.White;
                    if (black == "?") black = existing.Black;
                }
            }

            var game = new Models.SavedGame
            {
                White = white,
                Black = black,
                Event = _pgnHeaders.GetValueOrDefault("Event", "Chess Analysis"),
                Result = _pgnHeaders.GetValueOrDefault("Result", "*"),
                EngineName = engineService?.EngineName ?? "",
                EngineDepth = config?.EngineDepth ?? 0,
                WhiteAccuracy = _currentClassification?.WhiteAccuracy,
                BlackAccuracy = _currentClassification?.BlackAccuracy,
                HasClassification = _currentClassification != null,
                Opening = opening,
                Pgn = GeneratePgn(white, black)
            };

            // Overwrite existing library record instead of creating a duplicate
            if (!string.IsNullOrEmpty(_libraryGameId))
                game.Id = _libraryGameId;

            _libraryService!.Save(game);
            _libraryGameId = game.Id;

            string label = game.White == "?" && game.Black == "?"
                ? "game"
                : $"{game.White} vs {game.Black}";
            lblStatus.Text = $"Saved \"{label}\" to library";
        }

        private void BtnOpenLibrary_Click(object? sender, EventArgs e)
        {
            if (_libraryService == null) return;

            using var dialog = new GameLibraryDialog(_libraryService, ThemeService.IsDarkTheme(config?.Theme));
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedGame != null)
            {
                var saved = dialog.SelectedGame;
                ImportPgn(saved.Pgn);
                // Restore library identity so re-saving overwrites the same record
                _libraryGameId = saved.Id;
                if (saved.White != "?") _pgnHeaders["White"] = saved.White;
                if (saved.Black != "?") _pgnHeaders["Black"] = saved.Black;
                // Restore accuracy scores and activate the review link
                if (_currentClassification != null && saved.HasClassification)
                {
                    if (saved.WhiteAccuracy.HasValue) _currentClassification.WhiteAccuracy = saved.WhiteAccuracy.Value;
                    if (saved.BlackAccuracy.HasValue) _currentClassification.BlackAccuracy = saved.BlackAccuracy.Value;
                    consoleFormatter?.SetActiveClassification(_currentClassification);
                    consoleFormatter?.DisplayClassificationSummary(_currentClassification);
                }
            }
        }

        private void BtnOpenings_Click(object? sender, EventArgs e)
        {
            string booksFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
            var entries = ChessDroid.Services.EcoBookService.LoadAll(booksFolder);
            using var dialog = new OpeningExplorerDialog(entries, pgn =>
            {
                ImportPgn(pgn);
                NavigateToEnd();
            }, ThemeService.IsDarkTheme(config?.Theme));
            dialog.ShowDialog(this);
        }

        private string GeneratePgn(string? white = null, string? black = null)
        {
            white ??= !string.IsNullOrEmpty(_matchWhiteName) ? _matchWhiteName : _pgnHeaders.GetValueOrDefault("White", "?");
            black ??= !string.IsNullOrEmpty(_matchBlackName) ? _matchBlackName : _pgnHeaders.GetValueOrDefault("Black", "?");

            var sb = new System.Text.StringBuilder();

            // Standard PGN headers
            sb.AppendLine("[Event \"Chess Analysis\"]");
            sb.AppendLine("[Site \"chessdroid\"]");
            sb.AppendLine($"[Date \"{DateTime.Now:yyyy.MM.dd}\"]");
            sb.AppendLine("[Round \"?\"]");
            sb.AppendLine($"[White \"{white}\"]");
            sb.AppendLine($"[Black \"{black}\"]");
            sb.AppendLine("[Result \"*\"]");
            if (_classificationLookup != null)
            {
                string annotator = _currentClassification?.EngineName is { Length: > 0 } n ? n : "chessdroid";
                sb.AppendLine($"[Annotator \"{annotator}\"]");
            }

            // Add FEN if not standard starting position
            string rootFen = moveTree.Root.FEN;
            if (rootFen != "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
            {
                sb.AppendLine($"[FEN \"{rootFen}\"]");
                sb.AppendLine("[SetUp \"1\"]");
            }

            sb.AppendLine();

            // Generate move text with annotations and variations
            if (moveTree.Root.Children.Count > 0)
            {
                var moveText = new System.Text.StringBuilder();
                int lineLength = 0;
                AppendPgnSubtree(moveTree.Root, moveText, ref lineLength, showMoveNum: false);
                sb.Append(moveText);
                sb.Append(" *");
            }
            else
            {
                sb.Append("*");
            }

            sb.AppendLine();
            return sb.ToString();
        }

        private void ImportPgn(string pgn)
        {
            try
            {
                // Parse headers
                var headers = new Dictionary<string, string>();
                var lines = pgn.Split('\n');
                int moveTextStart = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        int keyEnd = line.IndexOf(' ');
                        if (keyEnd > 1)
                        {
                            string key = line.Substring(1, keyEnd - 1);
                            int valueStart = line.IndexOf('"') + 1;
                            int valueEnd = line.LastIndexOf('"');
                            if (valueStart > 0 && valueEnd > valueStart)
                                headers[key] = line.Substring(valueStart, valueEnd - valueStart);
                        }
                        moveTextStart = i + 1;
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        moveTextStart = i;
                        break;
                    }
                }

                _pgnHeaders = headers;
                _matchWhiteName = "";
                _matchBlackName = "";
                _matchWhiteFileName = "";
                _matchBlackFileName = "";
                _lblBlackEngineInfo.Visible = false;
                _lblWhiteEngineInfo.Visible = false;
                _libraryGameId = "";

                string startFen = headers.TryGetValue("FEN", out var fenValue)
                    ? fenValue
                    : "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

                CancelClassification();
                boardControl.LoadFEN(startFen);
                moveTree.Clear(startFen);
                moveListBox.Items.Clear();
                _movePairs.Clear();
                _analysisCache.Clear();
                _currentClassification = null;
                _classificationLookup = null;

                string moveText = string.Join(" ", lines.Skip(moveTextStart))
                    .Replace("\r", " ").Replace("\n", " ");

                var tokens = PgnImportService.TokenizeMoveText(moveText);

                string currentFen = startFen;
                int skippedMoves = 0;
                var skippedMovesList = new List<string>();

                // Track annotations per node in move order
                var annotationList = new List<(MoveNode node, string nag, string comment)>();
                MoveNode? lastAppliedNode = null;

                // Variation stack: save tree position when entering ( ... ) blocks
                var variationStack = new Stack<(MoveNode node, string fen, MoveNode? lastNode)>();

                isNavigating = true;

                foreach (var (type, value) in tokens)
                {
                    if (type == '(')
                    {
                        // Variation starts from the position before the last main-line move.
                        // Save current state and step back to the parent node.
                        variationStack.Push((moveTree.CurrentNode, currentFen, lastAppliedNode));
                        if (moveTree.CurrentNode.Parent != null)
                        {
                            moveTree.GoToNode(moveTree.CurrentNode.Parent);
                            currentFen = moveTree.CurrentNode.FEN;
                        }
                        lastAppliedNode = null;
                        continue;
                    }
                    if (type == ')')
                    {
                        if (variationStack.Count > 0)
                        {
                            var (savedNode, savedFen, savedLast) = variationStack.Pop();
                            moveTree.GoToNode(savedNode);
                            currentFen = savedFen;
                            lastAppliedNode = savedLast;
                        }
                        continue;
                    }
                    if (type == 'M')
                    {
                        string? uciMove = PgnImportService.ConvertSanToUci(value, currentFen);
                        if (uciMove == null)
                        {
                            Debug.WriteLine($"Failed to parse move: {value} in position {currentFen}");
                            skippedMoves++;
                            if (skippedMovesList.Count < 5) skippedMovesList.Add(value);
                            continue;
                        }
                        boardControl.LoadFEN(currentFen);
                        if (!boardControl.MakeMove(uciMove))
                        {
                            Debug.WriteLine($"Failed to apply move: {uciMove}");
                            skippedMoves++;
                            if (skippedMovesList.Count < 5) skippedMovesList.Add(value);
                            continue;
                        }
                        string newFen = boardControl.GetFEN();
                        moveTree.AddMove(uciMove, value, newFen);
                        currentFen = newFen;
                        lastAppliedNode = moveTree.CurrentNode;
                        annotationList.Add((lastAppliedNode, "", ""));
                    }
                    else if (type == 'N' && lastAppliedNode != null && annotationList.Count > 0)
                    {
                        var last = annotationList[^1];
                        annotationList[^1] = (last.node, value, last.comment);
                    }
                    else if (type == 'C' && lastAppliedNode != null && annotationList.Count > 0)
                    {
                        if (value.StartsWith("[%cda "))
                        {
                            var ca = PgnImportService.DeserializeCachedAnalysis(value);
                            if (ca != null)
                                _analysisCache[GetPositionKey(lastAppliedNode.FEN)] = ca;
                        }
                        else
                        {
                            var last = annotationList[^1];
                            annotationList[^1] = (last.node, last.nag, value);
                        }
                    }
                }

                isNavigating = false;

                bool hasAnnotations = annotationList.Any(a =>
                    !string.IsNullOrEmpty(a.nag) || !string.IsNullOrEmpty(a.comment));

                if (hasAnnotations)
                {
                    var moveResults = new List<MoveReviewResult>();
                    int whiteMoveCount = 0, blackMoveCount = 0;

                    foreach (var (node, nag, comment) in annotationList)
                    {
                        string symbol = !string.IsNullOrEmpty(nag) ? PgnImportService.GetSymbolForNag(nag) : "";
                        double? evalAfter = !string.IsNullOrEmpty(comment) ? PgnImportService.ParseEvalFromComment(comment) : null;
                        if (evalAfter.HasValue) node.Evaluation = evalAfter.Value;
                        // Skip moves with no annotation — they have no classification and must not
                        // default to Best, which would color every unannotated move green.
                        if (string.IsNullOrEmpty(nag) && string.IsNullOrEmpty(comment))
                            continue;

                        var quality = !string.IsNullOrEmpty(symbol)
                            ? PgnImportService.GetQualityForSymbol(symbol)
                            : PgnImportService.ParseQualityFromComment(comment);

                        moveResults.Add(new MoveReviewResult
                        {
                            Node = node,
                            PlayedMove = node.SanMove,
                            Quality = quality,
                            Symbol = symbol,
                            EvalAfter = evalAfter ?? 0,
                            IsWhiteMove = node.IsWhiteMove
                        });

                        if (node.IsWhiteMove) whiteMoveCount++;
                        else blackMoveCount++;
                    }

                    string annotator = headers.TryGetValue("Annotator", out var ann) ? ann : "chessdroid";
                    _currentClassification = new MoveClassificationResult
                    {
                        EngineName = annotator,
                        MoveResults = moveResults,
                        WhiteMoveCount = whiteMoveCount,
                        BlackMoveCount = blackMoveCount
                    };

                    foreach (var r in moveResults)
                    {
                        var counts = r.IsWhiteMove
                            ? _currentClassification.WhiteCounts
                            : _currentClassification.BlackCounts;
                        counts.TryGetValue(r.Quality, out int cnt);
                        counts[r.Quality] = cnt + 1;
                    }
                }

                UpdateMoveList();
                UpdateFenDisplay();
                UpdateTurnLabel();

                if (hasAnnotations)
                    UpdateMoveListWithClassification();

                int moveCount = moveTree.GetMainLine().Count;
                if (skippedMoves > 0)
                {
                    string skippedInfo = skippedMovesList.Count < skippedMoves
                        ? $"{string.Join(", ", skippedMovesList)}..."
                        : string.Join(", ", skippedMovesList);
                    lblStatus.Text = $"Imported {moveCount} moves ({skippedMoves} skipped: {skippedInfo})";
                }
                else
                {
                    string suffix = hasAnnotations ? " (with annotations)" : "";
                    lblStatus.Text = $"Imported {moveCount} moves from PGN{suffix}";
                }

                moveTree.GoToStart();
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                SetLastMoveHighlight();
                UpdateMoveListSelection();
                RefreshEvalGraph();
            }
            catch (Exception ex)
            {
                isNavigating = false;
                lblStatus.Text = $"Import error: {ex.Message}";
                Debug.WriteLine($"PGN import error: {ex}");
            }
        }

        // Builds the full annotation token for a node (move text + NAG + comment + cache).
        private string BuildPgnMoveToken(MoveNode node, string moveStr)
        {
            string fullToken = moveStr;
            string nag = "";
            string comment = "";
            if (_classificationLookup != null && _classificationLookup.TryGetValue(node, out var result))
            {
                nag     = PgnImportService.GetNagForSymbol(result.Symbol);
                comment = PgnImportService.BuildPgnComment(result);
            }
            else
            {
                nag = PgnImportService.GetNagForSymbol(PgnImportService.GetInlineSymbol(node.SanMove));
            }
            if (!string.IsNullOrEmpty(nag))     fullToken += " " + nag;
            if (!string.IsNullOrEmpty(comment)) fullToken += " " + comment;

            string posKey = GetPositionKey(node.FEN);
            if (_analysisCache.TryGetValue(posKey, out var cachedEntry) && cachedEntry.Depth > 0)
                fullToken += " " + PgnImportService.SerializeCachedAnalysis(cachedEntry);

            return fullToken;
        }

        private static void WritePgnToken(System.Text.StringBuilder sb, ref int lineLength, string token)
        {
            if (lineLength > 0 && lineLength + token.Length + 1 > 80)
            {
                sb.AppendLine();
                lineLength = 0;
            }
            else if (sb.Length > 0)
            {
                sb.Append(' ');
                lineLength++;
            }
            sb.Append(token);
            lineLength += token.Length;
        }

        // Emits parentNode.Children[0] (the main move) then any sibling variations as (...) then recurses.
        private void AppendPgnSubtree(MoveNode parentNode, System.Text.StringBuilder sb, ref int lineLength, bool showMoveNum)
        {
            if (parentNode.Children.Count == 0) return;
            var main = parentNode.Children[0];

            string cleanSan = PgnImportService.StripAnnotationSymbols(main.SanMove);
            string moveStr = main.IsWhiteMove
                ? $"{main.MoveNumber}. {cleanSan}"
                : (showMoveNum ? $"{main.MoveNumber}... {cleanSan}" : cleanSan);
            WritePgnToken(sb, ref lineLength, BuildPgnMoveToken(main, moveStr));

            bool hadVariations = parentNode.Children.Count > 1;
            for (int i = 1; i < parentNode.Children.Count; i++)
            {
                WritePgnToken(sb, ref lineLength, "(");
                AppendPgnVariation(parentNode.Children[i], sb, ref lineLength);
                WritePgnToken(sb, ref lineLength, ")");
            }

            // After a variation block, black's next move needs a move number to re-establish context.
            bool nextNeedsNum = hadVariations && main.Children.Count > 0 && !main.Children[0].IsWhiteMove;
            AppendPgnSubtree(main, sb, ref lineLength, nextNeedsNum);
        }

        // Emits a variation starting at varNode (always shows move number, handles nesting).
        private void AppendPgnVariation(MoveNode varNode, System.Text.StringBuilder sb, ref int lineLength)
        {
            string cleanSan = PgnImportService.StripAnnotationSymbols(varNode.SanMove);
            string moveStr = varNode.IsWhiteMove
                ? $"{varNode.MoveNumber}. {cleanSan}"
                : $"{varNode.MoveNumber}... {cleanSan}";
            WritePgnToken(sb, ref lineLength, BuildPgnMoveToken(varNode, moveStr));

            bool hadVariations = varNode.Children.Count > 1;
            for (int i = 1; i < varNode.Children.Count; i++)
            {
                WritePgnToken(sb, ref lineLength, "(");
                AppendPgnVariation(varNode.Children[i], sb, ref lineLength);
                WritePgnToken(sb, ref lineLength, ")");
            }

            bool nextNeedsNum = hadVariations && varNode.Children.Count > 0 && !varNode.Children[0].IsWhiteMove;
            AppendPgnSubtree(varNode, sb, ref lineLength, nextNeedsNum);
        }

        #endregion
    }
}
