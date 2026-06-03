using System.Diagnostics;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    public class MoveClassificationService
    {
        private readonly ChessEngineService _engineService;

        public MoveClassificationService(ChessEngineService engineService)
        {
            _engineService = engineService;
        }

        public async Task<MoveClassificationResult?> ClassifyAsync(
            List<MoveNode> mainLine,
            int engineDepth,
            IDictionary<string, CachedAnalysis> analysisCache,
            Action<string>? onProgress,
            Action? onThrottledRefresh,
            CancellationToken ct)
        {
            var classification = new MoveClassificationResult
            {
                EngineName = _engineService.EngineName,
                EngineDepth = engineDepth
            };

            foreach (MoveQualityAnalyzer.MoveQuality q in Enum.GetValues<MoveQualityAnalyzer.MoveQuality>())
            {
                classification.WhiteCounts[q] = 0;
                classification.BlackCounts[q] = 0;
            }

            int whiteMoves = 0, blackMoves = 0;
            var lastGraphRefresh = DateTime.UtcNow;

            for (int i = 0; i < mainLine.Count; i++)
            {
                if (ct.IsCancellationRequested) break;

                var node = mainLine[i];
                onProgress?.Invoke($"Classifying move {i + 1}/{mainLine.Count}: {node.SanMove}...");

                try
                {
                    string beforeFen = !string.IsNullOrEmpty(node.ParentFEN)
                        ? node.ParentFEN
                        : (i > 0 ? mainLine[i - 1].FEN : "");
                    if (string.IsNullOrEmpty(beforeFen)) continue;

                    string cacheKey = GetPositionKey(beforeFen);
                    double? evalBeforeNullable = null;
                    string bestMove;
                    double evalBestMove;
                    string rawBeforeEval = "";

                    if (analysisCache.TryGetValue(cacheKey, out var cached))
                    {
                        bestMove = cached.BestMove;
                        rawBeforeEval = cached.Evaluation;
                        evalBeforeNullable = EvaluationParser.ParseNullable(cached.Evaluation);
                        evalBestMove = evalBeforeNullable ?? 0;
                    }
                    else
                    {
                        var analysisResult = await _engineService.GetBestMoveAsync(beforeFen, engineDepth, 3, ct: ct);
                        bestMove = analysisResult.bestMove ?? "";
                        rawBeforeEval = analysisResult.evaluation;
                        evalBeforeNullable = EvaluationParser.ParseNullable(analysisResult.evaluation);
                        evalBestMove = evalBeforeNullable ?? 0;

                        Debug.WriteLine($"  [Before] Raw eval: '{analysisResult.evaluation}' -> Parsed: {evalBeforeNullable?.ToString("F2") ?? "NULL"}");

                        analysisCache[cacheKey] = new CachedAnalysis
                        {
                            BestMove = bestMove,
                            Evaluation = analysisResult.evaluation,
                            PVs = analysisResult.pvs ?? new List<string>(),
                            Evaluations = analysisResult.evaluations ?? new List<string>(),
                            WDL = analysisResult.wdl,
                            Depth = engineDepth
                        };
                    }

                    if (!evalBeforeNullable.HasValue)
                    {
                        Debug.WriteLine($"  SKIPPING move {i + 1} - empty before evaluation from engine");
                        continue;
                    }

                    double evalBefore = evalBeforeNullable.Value;

                    string afterCacheKey = GetPositionKey(node.FEN);
                    double? evalAfterNullable = null;

                    if (analysisCache.TryGetValue(afterCacheKey, out var afterCached))
                    {
                        evalAfterNullable = EvaluationParser.ParseNullable(afterCached.Evaluation);
                    }
                    else
                    {
                        var afterResult = await _engineService.GetBestMoveAsync(node.FEN, engineDepth, 3, ct: ct);

                        Debug.WriteLine($"  [After] Raw eval: '{afterResult.evaluation}' -> Parsed: {EvaluationParser.ParseNullable(afterResult.evaluation)?.ToString("F2") ?? "NULL"}");

                        evalAfterNullable = EvaluationParser.ParseNullable(afterResult.evaluation);
                        analysisCache[afterCacheKey] = new CachedAnalysis
                        {
                            BestMove = afterResult.bestMove ?? "",
                            Evaluation = afterResult.evaluation,
                            PVs = afterResult.pvs ?? new List<string>(),
                            Evaluations = afterResult.evaluations ?? new List<string>(),
                            WDL = afterResult.wdl,
                            Depth = engineDepth
                        };
                    }

                    if (!evalAfterNullable.HasValue)
                    {
                        Debug.WriteLine($"  SKIPPING move {i + 1} - empty evaluation from engine");
                        continue;
                    }

                    double evalAfter = evalAfterNullable.Value;

                    double cpLoss = node.IsWhiteMove
                        ? (evalBefore - evalAfter)
                        : (evalAfter - evalBefore);

                    if (IsDraw(evalAfter) && cpLoss > 1.5)
                    {
                        Debug.WriteLine($"  Draw position detected - capping cpLoss from {cpLoss:F2} to 1.50");
                        cpLoss = 1.5;
                    }

                    Debug.WriteLine($"Move {i + 1}: {node.SanMove} | evalBefore={evalBefore:F2} evalAfter={evalAfter:F2} cpLoss={cpLoss:F2} (raw) | White={node.IsWhiteMove}");

                    if (cpLoss < 0) cpLoss = 0;
                    if (cpLoss > 6) cpLoss = 6;

                    bool isBestMove = node.UciMove == bestMove;
                    bool isBrilliant = false;

                    if (node.SanMove.EndsWith("!!"))
                    {
                        isBrilliant = true;
                    }
                    else if (isBestMove || cpLoss <= 0.10)
                    {
                        double? prevEval = i > 0 ? classification.MoveResults.LastOrDefault()?.EvalAfter : null;
                        var (brilliant, _) = ConsoleOutputFormatter.IsBrilliantMove(
                            beforeFen, node.UciMove, evalAfter, prevEval);
                        isBrilliant = brilliant;
                    }

                    double qualityEvalBefore = node.IsWhiteMove ? evalBefore * 100 : -evalBefore * 100;
                    double qualityEvalAfter  = node.IsWhiteMove ? evalAfter  * 100 : -evalAfter  * 100;

                    var quality = MoveQualityAnalyzer.AnalyzeMoveQuality(
                        evalBefore: qualityEvalBefore,
                        evalAfter: qualityEvalAfter,
                        isBestMove: isBestMove,
                        isSacrifice: isBrilliant
                    );

                    string finalSymbol = quality.Symbol;
                    var finalQuality = quality.Quality;
                    if (isBrilliant && quality.Symbol != "!!")
                    {
                        finalSymbol = "!!";
                        finalQuality = MoveQualityAnalyzer.MoveQuality.Brilliant;
                    }

                    if (isBestMove && finalSymbol == "" && finalQuality == MoveQualityAnalyzer.MoveQuality.Best)
                    {
                        if (analysisCache.TryGetValue(cacheKey, out var beforeCached) &&
                            beforeCached.Evaluations.Count >= 2)
                        {
                            double? bestPvEval   = EvaluationParser.ParseNullable(beforeCached.Evaluations[0]);
                            double? secondPvEval = EvaluationParser.ParseNullable(beforeCached.Evaluations[1]);

                            if (bestPvEval.HasValue && secondPvEval.HasValue)
                            {
                                double evalSwing = Math.Abs(bestPvEval.Value - secondPvEval.Value);
                                int sp = beforeFen.IndexOf(' ');
                                bool whiteToMove = sp >= 0 && sp + 1 < beforeFen.Length && beforeFen[sp + 1] == 'w';

                                bool isOnlyWinningMove;
                                if (whiteToMove)
                                {
                                    bool basicTrigger   = bestPvEval.Value >= 0.70 && secondPvEval.Value <= 0.50;
                                    bool swingTrigger   = evalSwing >= 2.0 && bestPvEval.Value >= 0.27 && secondPvEval.Value <= 0.70;
                                    bool disasterTrigger = bestPvEval.Value >= 0.0 && secondPvEval.Value <= -1.50;
                                    bool nearDrawTrigger = Math.Abs(bestPvEval.Value) <= 0.50 && secondPvEval.Value <= -2.0;
                                    isOnlyWinningMove = basicTrigger || swingTrigger || disasterTrigger || nearDrawTrigger;
                                }
                                else
                                {
                                    bool basicTrigger   = bestPvEval.Value <= -0.70 && secondPvEval.Value >= -0.50;
                                    bool swingTrigger   = evalSwing >= 2.0 && bestPvEval.Value <= -0.27 && secondPvEval.Value >= -0.70;
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

                    node.Evaluation = evalAfter;

                    string bestMoveSan = ConvertUciToSan(bestMove, beforeFen);
                    var moveResult = new MoveReviewResult
                    {
                        Node = node,
                        PlayedMove = node.SanMove,
                        BestMove = bestMoveSan,
                        EvalBefore = evalBefore,
                        EvalAfter = evalAfter,
                        EvalBestMove = evalBestMove,
                        CentipawnLoss = cpLoss * 100,
                        Quality = finalQuality,
                        Symbol = finalSymbol,
                        IsWhiteMove = node.IsWhiteMove
                    };
                    classification.MoveResults.Add(moveResult);

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

                    if ((DateTime.UtcNow - lastGraphRefresh).TotalMilliseconds >= 500)
                    {
                        onThrottledRefresh?.Invoke();
                        lastGraphRefresh = DateTime.UtcNow;
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error classifying move {i + 1}: {ex.Message}");
                }
            }

            if (ct.IsCancellationRequested)
                return null;

            classification.WhiteMoveCount = whiteMoves;
            classification.BlackMoveCount = blackMoves;
            classification.WhiteAccuracy = ComputeAccuracy(classification.MoveResults, forWhite: true);
            classification.BlackAccuracy = ComputeAccuracy(classification.MoveResults, forWhite: false);

            return classification;
        }

        private static string GetPositionKey(string fen)
        {
            var parts = fen.Split(' ');
            return parts.Length >= 4 ? $"{parts[0]} {parts[1]} {parts[2]} {parts[3]}" : fen;
        }

        private static bool IsDraw(double eval) => Math.Abs(eval) < 0.05;

        public static double ComputeAccuracy(List<MoveReviewResult> results, bool forWhite)
        {
            var moves = results.Where(r => r.IsWhiteMove == forWhite).ToList();
            if (moves.Count == 0) return 100.0;

            double totalWinLoss = 0;
            foreach (var m in moves)
            {
                double evalBefore = forWhite ? m.EvalBefore : -m.EvalBefore;
                double evalAfter  = forWhite ? m.EvalAfter  : -m.EvalAfter;
                double wpBefore = 1.0 / (1.0 + Math.Pow(10, -evalBefore / 4.0));
                double wpAfter  = 1.0 / (1.0 + Math.Pow(10, -evalAfter  / 4.0));
                totalWinLoss += Math.Max(0, wpBefore - wpAfter);
            }

            double avgWinLoss = totalWinLoss / moves.Count * 100.0;
            return Math.Max(0, Math.Min(100, 103.1668 * Math.Exp(-0.04354 * avgWinLoss) - 3.1669));
        }

        private static string ConvertUciToSan(string uciMove, string fen)
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
                return uciMove;
            }
        }
    }
}
