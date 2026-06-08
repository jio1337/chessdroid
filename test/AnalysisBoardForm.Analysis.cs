using System.Diagnostics;
using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    public partial class AnalysisBoardForm : Form
    {
        #region Analysis

        private async Task AnalyzeCurrentPosition(CancellationToken ct = default)
        {
            if (_challengeSnapshot != null) return; // challenge mode: no hints

            if (engineService == null)
            {
                Debug.WriteLine("[Analysis] FAILED — engineService is null");
                lblStatus.Text = "Engine not available";
                return;
            }

            string fen = boardControl.GetFEN();
            Debug.WriteLine($"[Analysis] Starting — FEN: {fen}  State: {engineService.State}");
            string cacheKey = GetPositionKey(fen);

            // Book arrows are instant — show them before the engine even starts
            UpdateBookArrowsForPosition(fen);
            int depth = config?.EngineDepth ?? 15;
            int multiPV = 3;

            // If a classification is active and the cache already has this position's result,
            // show it directly — skip continuous analysis entirely.
            if (_classificationLookup != null &&
                _analysisCache.TryGetValue(cacheKey, out var classifiedCache) &&
                classifiedCache.Depth >= depth)
            {
                DisplayAnalysisResult(fen, classifiedCache.BestMove, classifiedCache.Evaluation,
                    classifiedCache.PVs, classifiedCache.Evaluations, classifiedCache.WDL,
                    classifiedCache.Depth, fromCache: true);
                return;
            }

            if (config?.ContinuousAnalysis == true)
            {
                lblStatus.Text = "Analyzing...";
                consoleFormatter?.ResetLiveExpand();
                ShowBookInfoImmediate(fen);
                _lastLiveUpdate = DateTime.MinValue; // always show first depth update immediately

                string lastBestMove = "";
                int lastDepth = 0;

                try
                {
                    await engineService.RunContinuousAnalysisAsync(fen, multiPV,
                        (bestMove, eval, pvs, evals, wdl, currentDepth) =>
                        {
                            if (ct.IsCancellationRequested) return;
                            lastBestMove = bestMove;
                            lastDepth = currentDepth;

                            // Throttle UI updates — go infinite fires updates at every depth,
                            // which can be dozens/sec at low depths. Without throttling, rapid
                            // moves or piece dragging floods the BeginInvoke queue and causes
                            // visible stuttering. 150ms cap matches the debounce delay.
                            var now = DateTime.UtcNow;
                            if ((now - _lastLiveUpdate).TotalMilliseconds < 150) return;
                            _lastLiveUpdate = now;

                            void Update()
                            {
                                if (ct.IsCancellationRequested) return;
                                lblStatus.Text = $"depth {currentDepth}";
                                consoleFormatter?.DisplayLiveLines(fen, eval, pvs, evals, wdl, currentDepth);
                                if (!isNavigating && !_bookArrowsActive)
                                {
                                    int arrowCount = config?.EngineArrowCount ?? 1;
                                    if (arrowCount > 0) UpdateEngineArrows(pvs, arrowCount);
                                }
                                UpdateEvalBar(eval);
                            }
                            if (InvokeRequired) BeginInvoke(Update); else Update();
                        }, ct);

                    // Engine stopped — only act on terminal positions (checkmate / stalemate).
                    // For normal stops (navigation, new move) the result is simply discarded.
                    if (!ct.IsCancellationRequested && (lastBestMove == "(none)" || lastBestMove == "0000" || string.IsNullOrEmpty(lastBestMove)))
                    {
                        var board = ChessBoard.FromFEN(fen);
                        bool stmIsWhite = IsWhiteToMove(fen);
                        if (ChessUtilities.IsKingInCheck(board, stmIsWhite))
                        {
                            string winner = stmIsWhite ? "Black" : "White";
                            consoleFormatter?.ShowGameOver($"Checkmate — {winner} wins!");
                            evalBar?.SetMate(stmIsWhite ? -1 : 1);
                            lblStatus.Text = $"Checkmate — {winner} wins";
                            boardControl.TriggerParticles();
                        }
                        else
                        {
                            consoleFormatter?.ShowGameOver("Stalemate — Draw");
                            evalBar?.Reset();
                            lblStatus.Text = "Stalemate — Draw";
                        }
                        PlayGameEndSound();
                    }
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        lblStatus.Text = $"Analysis error: {ex.Message}";
                        Debug.WriteLine($"Analysis error: {ex}");
                    }
                }
                return;
            }

            // Fixed-depth mode

            // Check if depth setting changed - invalidate cache if so
            if (_cachedDepth != depth)
            {
                _analysisCache.Clear();
                _cachedDepth = depth;
            }

            // Check cache first (depth check only - PV count varies by position)
            if (_analysisCache.TryGetValue(cacheKey, out var cached) &&
                cached.Depth >= depth)
            {
                DisplayAnalysisResult(fen, cached.BestMove, cached.Evaluation,
                    cached.PVs, cached.Evaluations, cached.WDL, cached.Depth, fromCache: true);
                return;
            }

            lblStatus.Text = "Analyzing...";
            ShowBookInfoImmediate(fen);

            try
            {
                var result = await engineService.GetBestMoveAsync(fen, depth, multiPV, ct: ct);

                // Guard: if cancelled or navigated away, discard result — an empty bestMove
                // from a cancelled search must not be misread as checkmate/stalemate.
                if (ct.IsCancellationRequested || GetPositionKey(boardControl.GetFEN()) != cacheKey)
                    return;

                if (string.IsNullOrEmpty(result.bestMove) || result.bestMove == "(none)" || result.bestMove == "0000")
                {
                    // No legal moves — checkmate or stalemate
                    var board = ChessBoard.FromFEN(fen);
                    bool sideToMoveIsWhite = IsWhiteToMove(fen);
                    bool inCheck = ChessUtilities.IsKingInCheck(board, sideToMoveIsWhite);
                    if (inCheck)
                    {
                        string winner = sideToMoveIsWhite ? "Black" : "White";
                        consoleFormatter?.ShowGameOver($"Checkmate — {winner} wins!");
                        evalBar?.SetMate(sideToMoveIsWhite ? -1 : 1);
                        lblStatus.Text = $"Checkmate — {winner} wins";
                        boardControl.TriggerParticles();
                    }
                    else
                    {
                        consoleFormatter?.ShowGameOver("Stalemate — Draw");
                        evalBar?.Reset();
                        lblStatus.Text = "Stalemate — Draw";
                    }
                    PlayGameEndSound();
                    return;
                }

                var pvs = result.pvs ?? new List<string>();
                var evals = result.evaluations ?? new List<string>();

                _analysisCache[cacheKey] = new CachedAnalysis
                {
                    BestMove = result.bestMove,
                    Evaluation = result.evaluation,
                    PVs = new List<string>(pvs),
                    Evaluations = new List<string>(evals),
                    WDL = result.wdl,
                    Depth = depth
                };

                // Stale check: discard if user navigated away while engine was thinking.
                if (ct.IsCancellationRequested || GetPositionKey(boardControl.GetFEN()) != cacheKey)
                    return;

                DisplayAnalysisResult(fen, result.bestMove, result.evaluation,
                    pvs, evals, result.wdl, depth, fromCache: false);
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                {
                    lblStatus.Text = $"Analysis error: {ex.Message}";
                    Debug.WriteLine($"Analysis error: {ex}");
                }
            }
        }

        private static List<string> FilterSanTokens(string sanMoves)
            => sanMoves.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                       .Where(t => !PgnMoveTokenPrefixRegex.IsMatch(t) &&
                                    t != "1-0" && t != "0-1" && t != "1/2-1/2" && t != "*")
                       .ToList();

        private static bool IsWhiteToMove(string fen)
        {
            int sp = fen.IndexOf(' ');
            return sp >= 0 && sp + 1 < fen.Length && fen[sp + 1] == 'w';
        }

        private static string GetPositionKey(string fen)
        {
            // FEN format: pieces side castling enpassant halfmove fullmove
            // We only need the first 4 parts for position identity
            var parts = fen.Split(' ');
            if (parts.Length >= 4)
            {
                return $"{parts[0]} {parts[1]} {parts[2]} {parts[3]}";
            }
            return fen;
        }

        private void DisplayAnalysisResult(string fen, string bestMove, string evaluation,
            List<string> pvs, List<string> evals, WDLInfo? wdl,
            int depth, bool fromCache)
        {
            // Apply aggressiveness filter
            var candidates = new List<(string move, string evaluation, string pvLine, int sharpness)>();

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
            string recommendedMove = bestMove;

            if (candidates.Count >= 2 && aggressiveness != 50 && config?.PlayStyleEnabled == true)
            {
                int selectedIndex = sharpnessAnalyzer.SelectMoveByAggressiveness(candidates, aggressiveness, 0.30);
                if (selectedIndex >= 0 && selectedIndex < candidates.Count)
                {
                    recommendedMove = candidates[selectedIndex].move;
                }
            }

            var bookMoves = FetchBookMoves(fen);

            bool showBest   = config?.ShowBestLine   ?? true;
            bool showSecond = config?.ShowSecondLine ?? true;
            bool showThird  = config?.ShowThirdLine  ?? true;

            if (config?.ShowExplanations == false)
            {
                consoleFormatter?.DisplayAnalysisResultsRaw(
                    recommendedMove, evaluation, pvs, evals, fen,
                    showBest, showSecond, showThird, depth, wdl, bookMoves);
            }
            else
            {
                consoleFormatter?.DisplayAnalysisResults(
                    recommendedMove, evaluation, pvs, evals, fen,
                    showBest, showSecond, showThird, wdl, bookMoves);
            }

            // Update engine arrows (book arrows already set upfront in UpdateBookArrowsForPosition)
            if (!isNavigating && !_bookArrowsActive)
            {
                int arrowCount = config?.EngineArrowCount ?? 1;
                if (arrowCount > 0)
                    UpdateEngineArrows(pvs, arrowCount);
                else
                    boardControl.ClearEngineArrows();
            }

            // Store evaluation on the move node so the eval graph can read it
            moveTree.CurrentNode.Evaluation = MovesExplanation.ParseEvaluation(evaluation);
            RefreshEvalGraph();

            // Update eval bar with the evaluation
            UpdateEvalBar(evaluation);

            // Update threat arrows — derived from the same detection as the text output
            if (config?.ShowThreatArrows == true)
            {
                string[] fenParts = fen.Split(' ');
                bool weAreWhite = fenParts.Length > 1 && fenParts[1] == "w";
                string ep = fenParts.Length > 4 ? fenParts[3] : "-";
                var threats = ThreatDetection.GetThreatArrows(boardControl.GetBoardState(), weAreWhite, ep);
                boardControl.SetThreatArrows(threats);
            }
            else
            {
                boardControl.ClearThreatArrows();
            }

            string cacheIndicator = fromCache ? " (cached)" : "";
            lblStatus.Text = $"Analysis complete (depth {depth}){cacheIndicator}";
        }

        private (int fromRow, int fromCol, int toRow, int toCol) UciToSquares(string uci)
        {
            int fromCol = uci[0] - 'a';
            int fromRow = 7 - (uci[1] - '1');
            int toCol = uci[2] - 'a';
            int toRow = 7 - (uci[3] - '1');
            return (fromRow, fromCol, toRow, toCol);
        }

        private List<BookMove>? FetchBookMoves(string fen)
        {
            if (openingBookService?.IsLoaded != true) return null;
            var moves = openingBookService.GetBookMovesForPosition(fen);
            if (moves == null || moves.Count == 0) return null;
            return moves.Select(pm => new BookMove
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

        private void ShowBookInfoImmediate(string fen)
        {
            var bookMoves = FetchBookMoves(fen);
            consoleFormatter?.SetBookContext(fen, bookMoves);
            if (config?.ShowOpeningName == true || config?.ShowBookMoves == true)
                consoleFormatter?.ShowBookContextNow(fen, bookMoves);
        }

        private void UpdateBookArrowsForPosition(string fen)
        {
            if (isNavigating) return;
            bool inBook = config?.ShowBookMoves == true && config?.ShowBookArrows == true && openingBookService?.IsLoaded == true;
            if (inBook)
            {
                var moves = openingBookService!.GetBookMovesForPosition(fen);
                inBook = moves.Count > 0;
                if (inBook)
                {
                    int totalWeight = moves.Sum(m => m.Weight);
                    double topPct = totalWeight > 0 ? moves[0].Weight / (double)totalWeight * 100.0 : 1.0;
                    var arrows = new List<(int, int, int, int, Color)>();
                    foreach (var bm in moves.Take(5))
                    {
                        if (bm.UciMove.Length < 4) continue;
                        var (fromRow, fromCol, toRow, toCol) = UciToSquares(bm.UciMove);
                        double pct = totalWeight > 0 ? bm.Weight / (double)totalWeight * 100.0 : 0;
                        int alpha = Math.Max(80, (int)(pct / topPct * 200));
                        arrows.Add((fromRow, fromCol, toRow, toCol, Color.FromArgb(alpha, 15, 155, 200)));
                    }
                    boardControl.ClearEngineArrows();
                    boardControl.SetBookArrows(arrows);
                }
            }
            if (!inBook)
                boardControl.ClearBookArrows();
            _bookArrowsActive = inBook;
        }

        private void UpdateEngineArrows(List<string> pvs, int arrowCount)
        {
            var arrows = new List<(int fromRow, int fromCol, int toRow, int toCol, Color color)>();

            var colors = new[]
            {
                Color.FromArgb(180, 0, 200, 80),    // Green  — best
                Color.FromArgb(180, 200, 200, 0),   // Yellow — 2nd
                Color.FromArgb(180, 200, 60, 60)    // Red    — 3rd
            };

            for (int i = 0; i < arrowCount && i < pvs.Count; i++)
            {
                if (!string.IsNullOrEmpty(pvs[i]))
                {
                    string firstMove = pvs[i].Split(' ')[0];
                    if (firstMove.Length >= 4)
                    {
                        var sq = UciToSquares(firstMove);
                        arrows.Add((sq.fromRow, sq.fromCol, sq.toRow, sq.toCol, colors[i]));
                    }
                }
            }

            boardControl.SetEngineArrows(arrows);
        }

        private void UpdateEvalBar(string evaluation)
        {
            if (evalBar == null || string.IsNullOrEmpty(evaluation))
                return;

            if (evaluation.StartsWith("Mate in "))
            {
                string mateStr = evaluation.Replace("Mate in ", "").Trim();
                if (int.TryParse(mateStr, out int mateIn))
                {
                    evalBar.SetMate(mateIn);
                }
            }
            else
            {
                string cleaned = evaluation.Replace("+", "");
                if (double.TryParse(cleaned, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double pawns))
                {
                    evalBar.SetEvaluation(pawns * 100.0); // Convert pawns to centipawns
                }
            }
        }

        #endregion
    }
}
