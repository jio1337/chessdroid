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

            int chosenDepth = ShowAnalyzeDepthDialog(mainLine.Count, config?.EngineDepth ?? 15);
            if (chosenDepth < 0) return;

            await ClassifyMoves(mainLine, chosenDepth);
        }

        private int ShowAnalyzeDepthDialog(int moveCount, int defaultDepth)
        {
            bool isDark = ThemeService.IsDarkTheme(config?.Theme);
            Color bg    = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            Color fg    = isDark ? Color.White                 : Color.Black;
            Color btnBg = isDark ? Color.FromArgb(55, 55, 55) : SystemColors.ButtonFace;

            using var fnt = new Font("Courier New", 9f);

            // Layout constants
            const int pad  = 10;
            const int btnH = 24;
            const int w    = 258;

            using var dlg = new Form
            {
                Text            = "Analyze Game",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MinimizeBox     = false,
                MaximizeBox     = false,
                ShowInTaskbar   = false,
                ClientSize      = new Size(w, 95),
                BackColor       = bg,
                ForeColor       = fg,
                Font            = fnt,
            };

            var lblInfo = new Label
            {
                Text      = $"Analyzing {moveCount} moves. Engine depth:",
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Bounds    = new Rectangle(pad, 10, w - pad * 2, 18),
                ForeColor = fg,
                BackColor = Color.Transparent,
            };

            var numDepth = new NumericUpDown
            {
                Minimum   = 1,
                Maximum   = 40,
                Value     = Math.Clamp(defaultDepth, 1, 40),
                Bounds    = new Rectangle(pad, 32, 68, 22),
                BackColor = isDark ? Color.FromArgb(45, 45, 45) : SystemColors.Window,
                ForeColor = fg,
            };

            int btnW = (w - pad * 2 - 8) / 2;  // two equal buttons with a small gap
            var btnOk = new Button
            {
                Text         = "Analyze",
                DialogResult = DialogResult.OK,
                Bounds       = new Rectangle(pad, 95 - pad - btnH, btnW, btnH),
                BackColor    = btnBg,
                ForeColor    = fg,
                FlatStyle    = FlatStyle.Flat,
            };
            var btnCancel = new Button
            {
                Text         = "Cancel",
                DialogResult = DialogResult.Cancel,
                Bounds       = new Rectangle(pad + btnW + 8, 95 - pad - btnH, btnW, btnH),
                BackColor    = btnBg,
                ForeColor    = fg,
                FlatStyle    = FlatStyle.Flat,
            };

            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            dlg.Controls.AddRange([lblInfo, numDepth, btnOk, btnCancel]);

            return dlg.ShowDialog(this) == DialogResult.OK ? (int)numDepth.Value : -1;
        }

        private void CancelClassification()
        {
            _classifyCts?.Cancel();
        }

        private async Task ClassifyMoves(List<MoveNode> mainLine, int depth)
        {
            _classifyCts?.Dispose();
            _classifyCts = new CancellationTokenSource();
            var ct = _classifyCts.Token;

            btnClassifyMoves.Enabled = false;
            SetClassifyControlsEnabled(true);
            _analysisCache.Clear();
            _cachedDepth = depth;

            var svc = new MoveClassificationService(engineService!);
            var classification = await svc.ClassifyAsync(
                mainLine,
                depth,
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

            // Store classification symbols on the pairs so DrawItem can render them with colour.
            for (int i = 0; i < _movePairs.Count; i++)
            {
                var pair = _movePairs[i];
                if (pair.White != null && resultLookup.TryGetValue(pair.White, out var wRes) && !string.IsNullOrEmpty(wRes.Symbol))
                    pair.WhiteSymbol = wRes.Symbol;
                if (pair.Black != null && resultLookup.TryGetValue(pair.Black, out var bRes) && !string.IsNullOrEmpty(bRes.Symbol))
                    pair.BlackSymbol = bRes.Symbol;
            }
            moveListBox.Invalidate();
        }

        #endregion
    }
}
