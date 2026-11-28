using System.Globalization;
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

                    if (analysisCache.TryGetValue(cacheKey, out var cached) && cached.Depth >= engineDepth)
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

                        Debug.WriteLine($"  [Before] Raw eval: '{analysisResult.evaluation}' -> Parsed: {evalBeforeNullable?.ToString("F2", CultureInfo.InvariantCulture) ?? "NULL"}");

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

                    if (analysisCache.TryGetValue(afterCacheKey, out var afterCached) && afterCached.Depth >= engineDepth)
                    {
                        evalAfterNullable = EvaluationParser.ParseNullable(afterCached.Evaluation);
                    }
                    else
                    {
                        var afterResult = await _engineService.GetBestMoveAsync(node.FEN, engineDepth, 3, ct: ct);

                        Debug.WriteLine($"  [After] Raw eval: '{afterResult.evaluation}' -> Parsed: {EvaluationParser.ParseNullable(afterResult.evaluation)?.ToString("F2", CultureInfo.InvariantCulture) ?? "NULL"}");

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

                    string rawAfterEval = afterCached != null && afterCached.Depth >= engineDepth
                        ? afterCached.Evaluation : analysisCache[afterCacheKey].Evaluation;
                    double evalAfter = ParseAfterEval(rawAfterEval, node.IsWhiteMove) ?? evalAfterNullable.Value;
                    var beforeEvaluations = analysisCache.TryGetValue(cacheKey, out var beforeCached)
                        ? beforeCached.Evaluations : new List<string>();
                    double? prevEval = i > 0 ? classification.MoveResults.LastOrDefault()?.EvalAfter : null;

                    var (finalQuality, finalSymbol, cpLoss) = ClassifyMove(beforeFen, node.UciMove, node.SanMove,
                        node.IsWhiteMove, bestMove, rawBeforeEval, beforeEvaluations, rawAfterEval, prevEval);

                    Debug.WriteLine($"Move {i + 1}: {node.SanMove} | evalBefore={evalBefore:F2} evalAfter={evalAfter:F2} cpLoss={cpLoss:F2} | {finalQuality}");

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

        /// <summary>
        /// The per-move verdict, pure (no engine): everything ClassifyAsync decides once it has
        /// the engine's evals. Evals are the engine's strings, White's point of view ("+0.31",
        /// "Mate in -5"). <paramref name="beforeEvaluations"/> are the MultiPV evals of the
        /// position before the move (best first). Returns cpLoss in pawns, clamped to 0..6.
        /// Public so the review audit harness replays exactly what the app decides.
        /// </summary>
        public static (MoveQualityAnalyzer.MoveQuality quality, string symbol, double cpLoss) ClassifyMove(
            string beforeFen, string uciMove, string san, bool isWhiteMove, string bestMove,
            string beforeEval, IReadOnlyList<string> beforeEvaluations, string afterEval, double? prevEvalAfter)
        {
            // The move delivered checkmate: the engine reports the mated position as an unsigned
            // "Mate in 0", which parses as a White mate — a Black mating move would read as a blunder.
            if (System.Text.RegularExpressions.Regex.IsMatch(afterEval, @"^Mate in [+-]?0$"))
                return (MoveQualityAnalyzer.MoveQuality.Best, "", 0);

            double evalBefore = EvaluationParser.ParseNullable(beforeEval) ?? 0;
            double evalAfter = EvaluationParser.ParseNullable(afterEval) ?? 0;

            double cpLoss = isWhiteMove
                ? (evalBefore - evalAfter)
                : (evalAfter - evalBefore);

            if (IsDraw(evalAfter) && cpLoss > 1.5)
                cpLoss = 1.5;

            if (cpLoss < 0) cpLoss = 0;
            if (cpLoss > 6) cpLoss = 6;

            bool isBestMove = uciMove == bestMove;
            bool isBrilliant = false;

            if (san.EndsWith("!!", StringComparison.Ordinal))
            {
                isBrilliant = true;
            }
            else if (isBestMove || cpLoss <= 0.10)
            {
                var (brilliant, _) = ConsoleOutputFormatter.IsBrilliantMove(
                    beforeFen, uciMove, evalAfter, prevEvalAfter);
                isBrilliant = brilliant;
            }

            double qualityEvalBefore = isWhiteMove ? evalBefore * 100 : -evalBefore * 100;
            double qualityEvalAfter  = isWhiteMove ? evalAfter  * 100 : -evalAfter  * 100;

            var quality = MoveQualityAnalyzer.AnalyzeMoveQuality(
                evalBefore: qualityEvalBefore,
                evalAfter: qualityEvalAfter,
                isBestMove: isBestMove,
                isSacrifice: isBrilliant
            );

            string finalSymbol = quality.Symbol;
            var finalQuality = quality.Quality;

            // Forced mates appearing or vanishing: the win-probability curve flattens out near
            // ±10 pawns, so judge these the way Lichess does instead. Losing a mate is an
            // inaccuracy if still above +10, a mistake above +7, otherwise a blunder; allowing
            // one is the mirror image. (35.Qxd8+ mate-in-9 → +6.67 was an "Inaccuracy";
            // 27.Kf2 -7.91 → mated in 3 was "Good".)
            if (!isBestMove && MateSwing(beforeEval, afterEval, isWhiteMove) is { } swing)
            {
                finalQuality = swing;
                finalSymbol = swing switch
                {
                    MoveQualityAnalyzer.MoveQuality.Blunder => "??",
                    MoveQualityAnalyzer.MoveQuality.Mistake => "?",
                    _ => "?!",
                };
            }

            // An annotator's "!!" from an imported PGN is kept only when the engine doesn't call
            // the move an error — the review states the engine's verdict, not the annotation's.
            if (isBrilliant && san.EndsWith("!!", StringComparison.Ordinal) &&
                finalQuality is MoveQualityAnalyzer.MoveQuality.Inaccuracy or MoveQualityAnalyzer.MoveQuality.Mistake or MoveQualityAnalyzer.MoveQuality.Blunder)
                isBrilliant = false;

            if (isBrilliant && quality.Symbol != "!!")
            {
                finalSymbol = "!!";
                finalQuality = MoveQualityAnalyzer.MoveQuality.Brilliant;
            }

            if (isBestMove && finalSymbol == "" && finalQuality == MoveQualityAnalyzer.MoveQuality.Best &&
                beforeEvaluations.Count >= 2)
            {
                double? bestPvEval   = EvaluationParser.ParseNullable(beforeEvaluations[0]);
                double? secondPvEval = EvaluationParser.ParseNullable(beforeEvaluations[1]);

                if (bestPvEval.HasValue && secondPvEval.HasValue)
                {
                    double evalSwing = Math.Abs(bestPvEval.Value - secondPvEval.Value);
                    int sp = beforeFen.IndexOf(' ');
                    bool whiteToMove = sp >= 0 && sp + 1 < beforeFen.Length && beforeFen[sp + 1] == 'w';

                    bool isOnlyWinningMove;
                    if (whiteToMove)
                    {
                        bool basicTrigger    = evalSwing >= 0.50 && bestPvEval.Value >= 0.40 && secondPvEval.Value <= 0.20;
                        bool swingTrigger    = evalSwing >= 2.0 && bestPvEval.Value >= 0.27 && secondPvEval.Value <= 0.70;
                        bool disasterTrigger = bestPvEval.Value >= 0.0 && secondPvEval.Value <= -1.50;
                        bool nearDrawTrigger = Math.Abs(bestPvEval.Value) <= 0.50 && secondPvEval.Value <= -2.0;
                        isOnlyWinningMove = basicTrigger || swingTrigger || disasterTrigger || nearDrawTrigger;
                    }
                    else
                    {
                        bool basicTrigger    = evalSwing >= 0.50 && bestPvEval.Value <= -0.40 && secondPvEval.Value >= -0.20;
                        bool swingTrigger    = evalSwing >= 2.0 && bestPvEval.Value <= -0.27 && secondPvEval.Value >= -0.70;
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

            return (finalQuality, finalSymbol, cpLoss);
        }

        /// <summary>
        /// Lichess's verdict when a forced mate appears or disappears for the mover, or null when
        /// neither happened (mate kept, still being mated, or no mate either side). Evals are
        /// the engine strings, White's view.
        /// </summary>
        internal static MoveQualityAnalyzer.MoveQuality? MateSwing(string beforeEval, string afterEval, bool moverWhite)
        {
            int? mateBefore = MateForMover(beforeEval, moverWhite);
            int? mateAfter = MateForMover(afterEval, moverWhite);
            if (mateBefore == null && mateAfter == null) return null;

            static MoveQualityAnalyzer.MoveQuality BySeverity(double pawns) =>
                pawns > 9.99 ? MoveQualityAnalyzer.MoveQuality.Inaccuracy
                : pawns > 7.0 ? MoveQualityAnalyzer.MoveQuality.Mistake
                : MoveQualityAnalyzer.MoveQuality.Blunder;

            double Pawns(string e) => (EvaluationParser.ParseNullable(e) ?? 0) * (moverWhite ? 1 : -1);

            // Had a forced mate, now doesn't: by how much is the mover still winning?
            if (mateBefore > 0 && mateAfter == null) return BySeverity(Pawns(afterEval));
            // Had a forced mate, now gets mated.
            if (mateBefore > 0 && mateAfter < 0) return MoveQualityAnalyzer.MoveQuality.Blunder;
            // Wasn't being mated, now is: how lost was the mover already?
            if (mateBefore == null && mateAfter < 0) return BySeverity(-Pawns(beforeEval));
            return null;
        }

        /// <summary>
        /// The eval after a move, White's view. A position that is already checkmate comes back as
        /// an unsigned "Mate in 0", which EvaluationParser reads as a White win; here it counts
        /// for the side that just moved (it delivered the mate), so a Black mating move doesn't
        /// drag Black's accuracy down or flip the eval graph at the end of the game.
        /// </summary>
        public static double? ParseAfterEval(string afterEval, bool moverWhite)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(afterEval ?? "", @"^Mate in [+-]?0$"))
                return moverWhite ? 15 : -15;
            return EvaluationParser.ParseNullable(afterEval ?? "");
        }

        /// <summary>+n: the mover mates in n; -n: the mover gets mated in n; null: no mate.</summary>
        private static int? MateForMover(string eval, bool moverWhite)
        {
            var m = System.Text.RegularExpressions.Regex.Match(eval ?? "", @"^Mate in ([+-]?)(\d+)");
            if (!m.Success) return null;
            int n = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            if (n == 0) return null; // already mated, no sign to go by
            bool whiteMates = m.Groups[1].Value != "-";
            return whiteMates == moverWhite ? n : -n;
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
                double wpBefore = MoveQualityAnalyzer.WinProbability(evalBefore);
                double wpAfter  = MoveQualityAnalyzer.WinProbability(evalAfter);
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
