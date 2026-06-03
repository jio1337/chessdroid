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
            _analysisCache.Clear();
            _cachedDepth = config?.EngineDepth ?? 15;

            var svc = new MoveClassificationService(engineService!);
            var classification = await svc.ClassifyAsync(
                mainLine,
                config?.EngineDepth ?? 15,
                _analysisCache,
                status => lblStatus.Text = status,
                () => RefreshEvalGraph(),
                ct);

            btnClassifyMoves.Enabled = true;
            SetClassifyControlsEnabled(false);
            _classifyCts?.Dispose();
            _classifyCts = null;

            if (classification == null)
            {
                lblStatus.Text = "Classification cancelled";
                return;
            }

            _currentClassification = classification;

            isNavigating = true;
            UpdateMoveListWithClassification();
            isNavigating = false;

            RefreshEvalGraph();
            consoleFormatter?.SetActiveClassification(classification);
            consoleFormatter?.DisplayClassificationSummary(classification);

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
                        string cleanSan = PgnImportService.StripAnnotationSymbols(node.SanMove);

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

        #endregion
    }
}
