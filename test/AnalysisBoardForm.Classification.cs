using System.Diagnostics;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    public partial class AnalysisBoardForm : Form
    {
        #region Move Classification

        // Store the current classification result
        private MoveClassificationResult? _currentClassification;
        private CancellationTokenSource? _classifyCts;

        private async void BtnClassifyMoves_Click(object? sender, EventArgs e)
        {
            var mainLine = moveTree.GetMainLine();
            if (mainLine.Count == 0)
            {
                lblStatus.Text = "No moves to classify";
                return;
            }

            if (engineService == null)
            {
                lblStatus.Text = "Engine not available";
                return;
            }

            // Confirm with user
            var result = MessageBox.Show(
                $"This will analyze all {mainLine.Count} moves and add quality symbols.\n" +
                $"This may take a while depending on engine depth ({config?.EngineDepth ?? 15}).\n\n" +
                "Continue?",
                "Classify Moves",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            await ClassifyMoves(mainLine);
        }

        private void CancelClassification()
        {
            _classifyCts?.Cancel();
        }

        private async Task ClassifyMoves(List<MoveNode> mainLine)
        {
            _classifyCts?.Dispose();
            _classifyCts = new CancellationTokenSource();
            var ct = _classifyCts.Token;

            btnClassifyMoves.Enabled = false;
            SetClassifyControlsEnabled(true);

            // Clear analysis cache to ensure fresh evaluations with correct perspective
            _analysisCache.Clear();

            // Set cached depth so navigation after classification uses the cache
            _cachedDepth = config?.EngineDepth ?? 15;

            var classification = new MoveClassificationResult
            {
                EngineName = engineService!.EngineName,
                EngineDepth = config?.EngineDepth ?? 15
            };

            // Initialize classification counts
            foreach (MoveQualityAnalyzer.MoveQuality q in _allMoveQualities)
            {
                classification.WhiteCounts[q] = 0;
                classification.BlackCounts[q] = 0;
            }

            int whiteMoves = 0;
            int blackMoves = 0;
            var lastGraphRefresh = DateTime.UtcNow;

            for (int i = 0; i < mainLine.Count; i++)
            {
                if (ct.IsCancellationRequested) break;

                var node = mainLine[i];
                lblStatus.Text = $"Classifying move {i + 1}/{mainLine.Count}: {node.SanMove}...";
                Application.DoEvents();

                try
                {
                    // Use ParentFEN for position before the move (more reliable than tracking)
                    string beforeFen = !string.IsNullOrEmpty(node.ParentFEN)
                        ? node.ParentFEN
                        : (i > 0 ? mainLine[i - 1].FEN : moveTree.Root.FEN);

                    // Analyze the position BEFORE the move to get the best move and eval
                    string cacheKey = GetPositionKey(beforeFen);
                    double? evalBeforeNullable = null;
                    string bestMove;
                    double evalBestMove;
                    string rawBeforeEval = "";

                    if (_analysisCache.TryGetValue(cacheKey, out var cached))
                    {
                        bestMove = cached.BestMove;
                        rawBeforeEval = cached.Evaluation;
                        evalBeforeNullable = ParseEvalNullable(cached.Evaluation);
                        evalBestMove = evalBeforeNullable ?? 0;
                    }
                    else
                    {
                        // Run engine analysis with 3 PVs so cache is valid for position navigation
                        var analysisResult = await engineService.GetBestMoveAsync(beforeFen, classification.EngineDepth, 3, ct: ct);
                        bestMove = analysisResult.bestMove ?? "";
                        rawBeforeEval = analysisResult.evaluation;
                        evalBeforeNullable = ParseEvalNullable(analysisResult.evaluation);
                        evalBestMove = evalBeforeNullable ?? 0;

                        Debug.WriteLine($"  [Before] Raw eval: '{analysisResult.evaluation}' -> Parsed: {evalBeforeNullable?.ToString("F2") ?? "NULL"}");

                        // Cache it
                        _analysisCache[cacheKey] = new CachedAnalysis
                        {
                            BestMove = bestMove,
                            Evaluation = analysisResult.evaluation,
                            PVs = analysisResult.pvs ?? new List<string>(),
                            Evaluations = analysisResult.evaluations ?? new List<string>(),
                            WDL = analysisResult.wdl,
                            Depth = classification.EngineDepth
                        };
                    }

                    // Skip this move if we couldn't get a valid before evaluation
                    if (!evalBeforeNullable.HasValue)
                    {
                        Debug.WriteLine($"  SKIPPING move {i + 1} - empty before evaluation from engine");
                        continue;
                    }

                    double evalBefore = evalBeforeNullable.Value;

                    // The played move's result is the evaluation AFTER the move
                    string afterCacheKey = GetPositionKey(node.FEN);
                    double? evalAfterNullable = null;
                    string rawAfterEval = "";

                    if (_analysisCache.TryGetValue(afterCacheKey, out var afterCached))
                    {
                        rawAfterEval = afterCached.Evaluation;
                        evalAfterNullable = ParseEvalNullable(afterCached.Evaluation);
                    }
                    else
                    {
                        // Use 3 PVs so cache is valid for position navigation
                        var afterResult = await engineService.GetBestMoveAsync(node.FEN, classification.EngineDepth, 3, ct: ct);
                        rawAfterEval = afterResult.evaluation;
                        evalAfterNullable = ParseEvalNullable(afterResult.evaluation);

                        Debug.WriteLine($"  [After] Raw eval: '{afterResult.evaluation}' -> Parsed: {evalAfterNullable?.ToString("F2") ?? "NULL"}");

                        _analysisCache[afterCacheKey] = new CachedAnalysis
                        {
                            BestMove = afterResult.bestMove ?? "",
                            Evaluation = afterResult.evaluation,
                            PVs = afterResult.pvs ?? new List<string>(),
                            Evaluations = afterResult.evaluations ?? new List<string>(),
                            WDL = afterResult.wdl,
                            Depth = classification.EngineDepth
                        };
                    }

                    // Skip this move if we couldn't get a valid evaluation
                    if (!evalAfterNullable.HasValue)
                    {
                        Debug.WriteLine($"  SKIPPING move {i + 1} - empty evaluation from engine");
                        continue;
                    }

                    double evalAfter = evalAfterNullable.Value;

                    // Calculate centipawn loss (from the moving side's perspective)
                    // All evaluations are in White's perspective (positive = good for White)
                    // For White's move: cpLoss = evalBefore - evalAfter (losing advantage is bad)
                    // For Black's move: cpLoss = evalAfter - evalBefore (opponent gaining advantage is bad)
                    double cpLoss = node.IsWhiteMove
                        ? (evalBefore - evalAfter)
                        : (evalAfter - evalBefore);

                    // Special handling for draw positions:
                    // If evalAfter is ~0.00 (draw), the player is accepting a draw.
                    // Cap the cpLoss at 1.5 pawns to avoid massive "blunders" for accepting draws.
                    if (IsDraw(evalAfter) && cpLoss > 1.5)
                    {
                        Debug.WriteLine($"  Draw position detected - capping cpLoss from {cpLoss:F2} to 1.50");
                        cpLoss = 1.5;
                    }

                    // Debug output for troubleshooting
                    Debug.WriteLine($"Move {i + 1}: {node.SanMove} | evalBefore={evalBefore:F2} evalAfter={evalAfter:F2} cpLoss={cpLoss:F2} (raw) | White={node.IsWhiteMove}");

                    // Clamp extreme values (cpLoss is in pawns, cap at 6 pawns = 600 centipawns)
                    if (cpLoss < 0) cpLoss = 0; // Can't have negative cp loss
                    if (cpLoss > 6) cpLoss = 6; // Cap extreme blunders at 6 pawns

                    // Check if it was the best move
                    bool isBestMove = node.UciMove == bestMove;

                    // Check for brilliant move using our dedicated detection
                    // This handles both capture sacrifices and implicit sacrifices (leaving pieces en prise)
                    bool isBrilliant = false;

                    // If move was already detected as brilliant in real-time (has !! in SanMove), preserve that
                    if (node.SanMove.EndsWith("!!"))
                    {
                        isBrilliant = true;
                    }
                    else if (isBestMove || cpLoss <= 0.10) // Only check moves that are best or very close
                    {
                        // Get the previous move's eval for context
                        double? prevEval = i > 0 ? classification.MoveResults.LastOrDefault()?.EvalAfter : null;

                        var (brilliant, _) = ConsoleOutputFormatter.IsBrilliantMove(
                            beforeFen, node.UciMove, evalAfter, prevEval);
                        isBrilliant = brilliant;
                    }

                    // Classify the move
                    // MoveQualityAnalyzer expects evals from the moving player's perspective
                    // For White: pass as-is (White's perspective)
                    // For Black: negate both to convert to Black's perspective
                    double qualityEvalBefore = node.IsWhiteMove ? evalBefore * 100 : -evalBefore * 100;
                    double qualityEvalAfter = node.IsWhiteMove ? evalAfter * 100 : -evalAfter * 100;

                    var quality = MoveQualityAnalyzer.AnalyzeMoveQuality(
                        evalBefore: qualityEvalBefore,
                        evalAfter: qualityEvalAfter,
                        isBestMove: isBestMove,
                        isSacrifice: isBrilliant
                    );

                    // If real-time detection marked this as brilliant, preserve that regardless of what
                    // the analyzer says. Real-time uses board analysis (actual sacrifice detection),
                    // which can catch brilliancies the eval-based analyzer misses.
                    string finalSymbol = quality.Symbol;
                    var finalQuality = quality.Quality;
                    if (isBrilliant && quality.Symbol != "!!")
                    {
                        finalSymbol = "!!";
                        finalQuality = MoveQualityAnalyzer.MoveQuality.Brilliant;
                    }

                    // Detect "only winning move" — best move where alternatives lose the advantage
                    if (isBestMove && finalSymbol == "" && finalQuality == MoveQualityAnalyzer.MoveQuality.Best)
                    {
                        if (_analysisCache.TryGetValue(cacheKey, out var beforeCached) &&
                            beforeCached.Evaluations.Count >= 2)
                        {
                            double? bestPvEval = ParseEvalNullable(beforeCached.Evaluations[0]);
                            double? secondPvEval = ParseEvalNullable(beforeCached.Evaluations[1]);

                            if (bestPvEval.HasValue && secondPvEval.HasValue)
                            {
                                double evalSwing = Math.Abs(bestPvEval.Value - secondPvEval.Value);
                                int _sp = beforeFen.IndexOf(' ');
                                bool whiteToMove = _sp >= 0 && _sp + 1 < beforeFen.Length && beforeFen[_sp + 1] == 'w';

                                bool isOnlyWinningMove;
                                if (whiteToMove)
                                {
                                    bool basicTrigger = bestPvEval.Value >= 0.70 && secondPvEval.Value <= 0.50;
                                    bool swingTrigger = evalSwing >= 2.0 && bestPvEval.Value >= 0.27 && secondPvEval.Value <= 0.70;
                                    bool disasterTrigger = bestPvEval.Value >= 0.0 && secondPvEval.Value <= -1.50;
                                    bool nearDrawTrigger = Math.Abs(bestPvEval.Value) <= 0.50 && secondPvEval.Value <= -2.0;
                                    isOnlyWinningMove = basicTrigger || swingTrigger || disasterTrigger || nearDrawTrigger;
                                }
                                else
                                {
                                    bool basicTrigger = bestPvEval.Value <= -0.70 && secondPvEval.Value >= -0.50;
                                    bool swingTrigger = evalSwing >= 2.0 && bestPvEval.Value <= -0.27 && secondPvEval.Value >= -0.70;
                                    bool disasterTrigger = bestPvEval.Value <= 0.0 && secondPvEval.Value >= 1.50;
                                    bool nearDrawTrigger = Math.Abs(bestPvEval.Value) <= 0.50 && secondPvEval.Value >= 2.0;
                                    isOnlyWinningMove = basicTrigger || swingTrigger || disasterTrigger || nearDrawTrigger;
                                }

                                if (isOnlyWinningMove)
                                {
                                    finalSymbol = "!";
                                    finalQuality = MoveQualityAnalyzer.MoveQuality.Precise;
                                }
                            }
                        }
                    }

                    // Store eval on node so the graph has data for every move
                    node.Evaluation = evalAfter;

                    // Store the result
                    var moveResult = new MoveReviewResult
                    {
                        Node = node,
                        PlayedMove = node.SanMove,
                        BestMove = ConvertUciToSan(bestMove, beforeFen),
                        EvalBefore = evalBefore,
                        EvalAfter = evalAfter,
                        EvalBestMove = evalBestMove,
                        CentipawnLoss = cpLoss * 100, // Store in centipawns
                        Quality = finalQuality,
                        Symbol = finalSymbol,
                        IsWhiteMove = node.IsWhiteMove
                    };
                    classification.MoveResults.Add(moveResult);

                    // Update stats — use finalQuality so brilliant overrides are counted correctly
                    if (node.IsWhiteMove)
                    {
                        whiteMoves++;
                        classification.WhiteCounts[finalQuality]++;
                    }
                    else
                    {
                        blackMoves++;
                        classification.BlackCounts[finalQuality]++;
                    }

                    // Throttled graph refresh — update the visual at most every 500ms so the
                    // user sees the graph fill in progressively without hammering GDI on every move.
                    if ((DateTime.UtcNow - lastGraphRefresh).TotalMilliseconds >= 500)
                    {
                        RefreshEvalGraph();
                        lastGraphRefresh = DateTime.UtcNow;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error classifying move {i + 1}: {ex.Message}");
                }
            }

            btnClassifyMoves.Enabled = true;
            SetClassifyControlsEnabled(false);
            _classifyCts?.Dispose();
            _classifyCts = null;

            if (ct.IsCancellationRequested)
            {
                lblStatus.Text = "Classification cancelled";
                return;
            }

            // Store final stats
            classification.WhiteMoveCount = whiteMoves;
            classification.BlackMoveCount = blackMoves;
            classification.WhiteAccuracy = ComputeAccuracy(classification.MoveResults, forWhite: true);
            classification.BlackAccuracy = ComputeAccuracy(classification.MoveResults, forWhite: false);

            _currentClassification = classification;

            // isNavigating prevents MoveListBox_SelectedIndexChanged from firing TriggerAutoAnalysis
            // when we update item text with classification symbols — otherwise analysis overwrites the summary
            isNavigating = true;
            UpdateMoveListWithClassification();
            isNavigating = false;

            RefreshEvalGraph();
            consoleFormatter?.SetActiveClassification(classification);
            consoleFormatter?.DisplayClassificationSummary(classification);

            // Append Elo performance if player ratings are known (PGN headers or engine profiles)
            var eloText = TryGetEloPerformanceText();
            if (eloText != null) analysisOutput.AppendText(eloText);

            lblStatus.Text = $"Game review — White {classification.WhiteAccuracy:F1}%  Black {classification.BlackAccuracy:F1}%";
        }

        private string? TryGetEloPerformanceText()
        {
            // PGN headers take priority (imported games); fall back to engine match fields
            int whiteElo = 0, blackElo = 0;

            if (_pgnHeaders.TryGetValue("WhiteElo", out string? wStr) && int.TryParse(wStr, out int we) && we > 0)
                whiteElo = we;
            else if (_matchWhiteElo > 0)
                whiteElo = _matchWhiteElo;

            if (_pgnHeaders.TryGetValue("BlackElo", out string? bStr) && int.TryParse(bStr, out int be) && be > 0)
                blackElo = be;
            else if (_matchBlackElo > 0)
                blackElo = _matchBlackElo;

            if (whiteElo <= 0 || blackElo <= 0) return null;

            string resultStr = _pgnHeaders.GetValueOrDefault("Result", "*");
            double whiteScore = resultStr switch
            {
                "1-0"     => 1.0,
                "0-1"     => 0.0,
                "1/2-1/2" => 0.5,
                _         => -1.0
            };
            if (whiteScore < 0) return null;

            string white = !string.IsNullOrEmpty(_matchWhiteFileName) ? GetEngineLabel(_matchWhiteFileName, false)
                : _pgnHeaders.GetValueOrDefault("White", "White");
            string black = !string.IsNullOrEmpty(_matchBlackFileName) ? GetEngineLabel(_matchBlackFileName, false)
                : _pgnHeaders.GetValueOrDefault("Black", "Black");

            int whiteDelta = Services.EloCalculator.EloChange(whiteElo, blackElo, whiteScore);
            int blackDelta = Services.EloCalculator.EloChange(blackElo, whiteElo, 1.0 - whiteScore);

            return $"\nElo change: {white} {Services.EloCalculator.FormatDelta(whiteDelta)}  {black} {Services.EloCalculator.FormatDelta(blackDelta)}\n";
        }

        private double? ParseEvalNullable(string evalStr)
        {
            if (string.IsNullOrEmpty(evalStr))
                return null; // Empty = unknown, not 0!

            // Handle mate scores
            if (evalStr.Contains("Mate") || evalStr.StartsWith("M") || evalStr.StartsWith("+M") || evalStr.StartsWith("-M"))
            {
                string numPart = evalStr
                    .Replace("Mate in", "")
                    .Replace("M", "")
                    .Replace("+", "")
                    .Replace("-", "")
                    .Trim();

                if (int.TryParse(numPart, out int mateIn))
                {
                    double mateScore = Math.Max(10, 15 - mateIn * 0.5);
                    bool isNegative = evalStr.Contains("-");
                    return isNegative ? -mateScore : mateScore;
                }
                return evalStr.Contains("-") ? -12 : 12;
            }

            // Regular eval like "+1.25" or "-0.50" or "+-0.00" (draw)
            evalStr = evalStr.Replace("+", "").Trim();

            if (double.TryParse(evalStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double eval))
            {
                return eval;
            }

            if (double.TryParse(evalStr, out eval))
            {
                return eval;
            }

            return null;
        }

        private double ParseEval(string evalStr)
        {
            return ParseEvalNullable(evalStr) ?? 0;
        }

        private bool IsDraw(double eval)
        {
            return Math.Abs(eval) < 0.05;
        }

        private static double ComputeAccuracy(List<MoveReviewResult> results, bool forWhite)
        {
            var moves = results.Where(r => r.IsWhiteMove == forWhite).ToList();
            if (moves.Count == 0) return 100.0;

            double totalWinLoss = 0;
            foreach (var m in moves)
            {
                // EvalBefore/EvalAfter are in White's perspective (pawns). Flip for Black.
                double evalBefore = forWhite ? m.EvalBefore : -m.EvalBefore;
                double evalAfter  = forWhite ? m.EvalAfter  : -m.EvalAfter;

                // Logistic win probability from player's perspective (same scale as GetMoveClassification)
                double wpBefore = 1.0 / (1.0 + Math.Pow(10, -evalBefore / 4.0));
                double wpAfter  = 1.0 / (1.0 + Math.Pow(10, -evalAfter  / 4.0));
                totalWinLoss += Math.Max(0, wpBefore - wpAfter);
            }

            // Lichess accuracy formula: avgWinLoss in percentage points (0–100)
            double avgWinLoss = totalWinLoss / moves.Count * 100.0;
            return Math.Max(0, Math.Min(100, 103.1668 * Math.Exp(-0.04354 * avgWinLoss) - 3.1669));
        }

        private void UpdateMoveListWithClassification()
        {
            if (_currentClassification == null) return;

            // Build dictionary for O(1) lookup instead of O(n) FirstOrDefault per item
            var resultLookup = _currentClassification.MoveResults.ToDictionary(r => r.Node, r => r);

            // Cache for DrawItem color lookups
            _classificationLookup = resultLookup;

            // Rebuild the move list items with classification symbols
            moveListBox.BeginUpdate();
            try
            {
                for (int i = 0; i < displayedNodes.Count && i < moveListBox.Items.Count; i++)
                {
                    var node = displayedNodes[i];
                    if (resultLookup.TryGetValue(node, out var result) && !string.IsNullOrEmpty(result.Symbol))
                    {
                        // Strip any existing annotation symbols from SanMove (e.g., from real-time detection)
                        // to avoid duplicates like "Nxd4!!!!" or "Bc5!!?!"
                        string cleanSan = StripAnnotationSymbols(node.SanMove);

                        // Update the item text to include the symbol
                        string moveText = node.IsWhiteMove
                            ? $"{node.MoveNumber}. {cleanSan}"
                            : $"{node.MoveNumber}...{cleanSan}";

                        moveListBox.Items[i] = $"{moveText} {result.Symbol}";
                    }
                }
            }
            finally
            {
                moveListBox.EndUpdate();
            }
        }

        private static string StripMovesFromOpeningName(string name)
        {
            var m = StripMovesRegex.Match(name);
            return m.Success ? name[..m.Index].TrimEnd(',', ' ') : name;
        }

        private static string StripAnnotationSymbols(string san)
        {
            if (string.IsNullOrEmpty(san)) return san;

            // Remove annotation symbols from the end (order matters - check longer patterns first)
            string[] symbols = { "!!", "??", "!?", "?!", "!", "?" };
            foreach (var symbol in symbols)
            {
                if (san.EndsWith(symbol))
                {
                    san = san.Substring(0, san.Length - symbol.Length);
                    break;
                }
            }
            return san;
        }

        private static string SerializeCachedAnalysis(CachedAnalysis ca)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"[%cda d={ca.Depth}");
            if (!string.IsNullOrEmpty(ca.BestMove)) sb.Append($";b={ca.BestMove}");
            if (!string.IsNullOrEmpty(ca.Evaluation)) sb.Append($";e={ca.Evaluation}");
            if (ca.PVs.Count > 0) sb.Append($";v={string.Join("~", ca.PVs)}");
            if (ca.Evaluations.Count > 0) sb.Append($";f={string.Join("~", ca.Evaluations)}");
            if (ca.WDL != null) sb.Append($";w={ca.WDL.Win}/{ca.WDL.Draw}/{ca.WDL.Loss}");
            sb.Append("]");
            return $"{{ {sb} }}";
        }

        private static CachedAnalysis? DeserializeCachedAnalysis(string comment)
        {
            if (!comment.StartsWith("[%cda ") || !comment.EndsWith("]")) return null;
            string inner = comment.Substring(6, comment.Length - 7);
            var ca = new CachedAnalysis();
            foreach (var field in inner.Split(';'))
            {
                int eq = field.IndexOf('=');
                if (eq < 0) continue;
                string key = field.Substring(0, eq);
                string val = field.Substring(eq + 1);
                switch (key)
                {
                    case "d":
                        if (int.TryParse(val, out int d)) ca.Depth = d;
                        break;
                    case "b":
                        ca.BestMove = val;
                        break;
                    case "e":
                        ca.Evaluation = val;
                        break;
                    case "v":
                        ca.PVs = val.Split('~').Where(x => !string.IsNullOrEmpty(x)).ToList();
                        break;
                    case "f":
                        ca.Evaluations = val.Split('~').Where(x => !string.IsNullOrEmpty(x)).ToList();
                        break;
                    case "w":
                        var wp = val.Split('/');
                        if (wp.Length == 3 &&
                            int.TryParse(wp[0], out int win) &&
                            int.TryParse(wp[1], out int draw) &&
                            int.TryParse(wp[2], out int loss))
                            ca.WDL = new WDLInfo(win, draw, loss);
                        break;
                }
            }
            return ca.Depth > 0 ? ca : null;
        }

        private static string GetNagForSymbol(string symbol) => symbol switch
        {
            "!!" => "$3",
            "!" => "$1",
            "!?" => "$5",
            "?!" => "$6",
            "?" => "$2",
            "??" => "$4",
            _ => ""
        };

        private static string GetSymbolForNag(string nag) => nag switch
        {
            "$3" => "!!",
            "$1" => "!",
            "$5" => "!?",
            "$6" => "?!",
            "$2" => "?",
            "$4" => "??",
            _ => ""
        };

        private static MoveQualityAnalyzer.MoveQuality GetQualityForSymbol(string symbol) => symbol switch
        {
            "!!" => MoveQualityAnalyzer.MoveQuality.Brilliant,
            "!"  => MoveQualityAnalyzer.MoveQuality.Precise,
            "?!" => MoveQualityAnalyzer.MoveQuality.Inaccuracy,
            "?"  => MoveQualityAnalyzer.MoveQuality.Mistake,
            "??" => MoveQualityAnalyzer.MoveQuality.Blunder,
            _    => MoveQualityAnalyzer.MoveQuality.Best
        };

        private static string GetInlineSymbol(string san)
        {
            if (string.IsNullOrEmpty(san)) return "";
            string[] symbols = { "!!", "??", "!?", "?!", "!", "?" };
            foreach (var s in symbols)
                if (san.EndsWith(s)) return s;
            return "";
        }

        private static string BuildPgnComment(MoveReviewResult result)
        {
            string eval = $"[{result.EvalAfter.ToString("+0.00;-0.00", System.Globalization.CultureInfo.InvariantCulture)}]";
            string label = result.Quality switch
            {
                MoveQualityAnalyzer.MoveQuality.Brilliant => "Brilliant",
                MoveQualityAnalyzer.MoveQuality.Precise   => "Precise",
                MoveQualityAnalyzer.MoveQuality.Best => "Best",
                MoveQualityAnalyzer.MoveQuality.Excellent => "Excellent",
                MoveQualityAnalyzer.MoveQuality.Good => "Good",
                MoveQualityAnalyzer.MoveQuality.Book => "Book",
                MoveQualityAnalyzer.MoveQuality.Inaccuracy => "Inaccuracy",
                MoveQualityAnalyzer.MoveQuality.Mistake => "Mistake",
                MoveQualityAnalyzer.MoveQuality.Blunder => "Blunder",
                MoveQualityAnalyzer.MoveQuality.Forced => "Forced",
                _ => "Best"
            };
            return $"{{ {eval} {label} }}";
        }

        private static double? ParseEvalFromComment(string comment)
        {
            var m = PgnEvalCommentRegex.Match(comment);
            if (!m.Success) return null;
            return double.TryParse(m.Groups[1].Value.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : (double?)null;
        }

        private static MoveQualityAnalyzer.MoveQuality ParseQualityFromComment(string comment)
        {
            if (comment.Contains("Brilliant")) return MoveQualityAnalyzer.MoveQuality.Brilliant;
            if (comment.Contains("Precise"))   return MoveQualityAnalyzer.MoveQuality.Precise;
            if (comment.Contains("Blunder")) return MoveQualityAnalyzer.MoveQuality.Blunder;
            if (comment.Contains("Mistake")) return MoveQualityAnalyzer.MoveQuality.Mistake;
            if (comment.Contains("Inaccuracy")) return MoveQualityAnalyzer.MoveQuality.Inaccuracy;
            if (comment.Contains("Book")) return MoveQualityAnalyzer.MoveQuality.Book;
            if (comment.Contains("Excellent")) return MoveQualityAnalyzer.MoveQuality.Excellent;
            if (comment.Contains("Forced")) return MoveQualityAnalyzer.MoveQuality.Forced;
            if (comment.Contains("Good")) return MoveQualityAnalyzer.MoveQuality.Good;
            return MoveQualityAnalyzer.MoveQuality.Best;
        }

        // Returns tokens: 'M'=move, 'N'=NAG ($3 etc), 'C'=comment text
        private static List<(char type, string value)> TokenizePgnMoveText(string moveText)
        {
            var tokens = new List<(char, string)>();
            int i = 0, len = moveText.Length;
            while (i < len)
            {
                char c = moveText[i];
                if (c == '{')
                {
                    int end = moveText.IndexOf('}', i + 1);
                    if (end < 0) end = len - 1;
                    tokens.Add(('C', moveText.Substring(i + 1, end - i - 1).Trim()));
                    i = end + 1;
                }
                else if (c == '(')
                {
                    int depth = 1; i++;
                    while (i < len && depth > 0)
                    {
                        if (moveText[i] == '(') depth++;
                        else if (moveText[i] == ')') depth--;
                        i++;
                    }
                }
                else if (c == ';')
                {
                    while (i < len && moveText[i] != '\n') i++;
                }
                else if (c == '$')
                {
                    int start = i++;
                    while (i < len && char.IsDigit(moveText[i])) i++;
                    tokens.Add(('N', moveText.Substring(start, i - start)));
                }
                else if (char.IsWhiteSpace(c))
                {
                    i++;
                }
                else
                {
                    int start = i;
                    while (i < len && !char.IsWhiteSpace(moveText[i]) &&
                           moveText[i] != '{' && moveText[i] != '(' &&
                           moveText[i] != '$' && moveText[i] != ';')
                        i++;
                    string token = moveText.Substring(start, i - start);
                    if (token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*")
                        continue;
                    if (PgnMoveNumberRegex.IsMatch(token)) continue;
                    var am = PgnAttachedMoveRegex.Match(token);
                    if (am.Success) token = am.Groups[1].Value.TrimStart('.');
                    if (!string.IsNullOrEmpty(token))
                        tokens.Add(('M', token));
                }
            }
            return tokens;
        }

        #endregion
    }
}
