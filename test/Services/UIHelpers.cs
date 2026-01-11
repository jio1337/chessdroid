namespace ChessDroid.Services
{
    /// <summary>
    /// UI Helper functions for ChessDroid
    /// Tooltips, visual feedback, and user interface utilities
    /// Note: Main settings dialog lives in SettingsForm.cs
    /// </summary>
    public static class UIHelpers
    {
        // =============================
        // QUICK INFO PANEL
        // Show analysis summary at a glance
        // =============================

        /// <summary>
        /// Create a quick info panel showing key metrics
        /// </summary>
        public static Panel CreateQuickInfoPanel(string evaluation, int materialCount, string gamePhase)
        {
            var panel = new Panel
            {
                Size = new Size(300, 100),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            int yPos = 10;

            // Evaluation with win rate
            double? eval = MovesExplanation.ParseEvaluation(evaluation);
            if (eval.HasValue)
            {
                double winRate = AdvancedAnalysis.EvalToWinningPercentage(Math.Abs(eval.Value), materialCount);
                string side = eval.Value >= 0 ? "White" : "Black";

                var lblEval = new Label
                {
                    Text = $"Eval: {evaluation}",
                    Location = new Point(10, yPos),
                    Size = new Size(280, 20),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                };
                panel.Controls.Add(lblEval);
                yPos += 25;

                var lblWinRate = new Label
                {
                    Text = $"{side} win chance: {winRate:F1}%",
                    Location = new Point(10, yPos),
                    Size = new Size(280, 20),
                    ForeColor = ExplanationFormatter.GetWinRateColor(winRate)
                };
                panel.Controls.Add(lblWinRate);
                yPos += 25;
            }

            // Game phase
            var lblPhase = new Label
            {
                Text = $"Phase: {gamePhase}",
                Location = new Point(10, yPos),
                Size = new Size(280, 20)
            };
            panel.Controls.Add(lblPhase);

            return panel;
        }

        // =============================
        // TOOLTIP HELPERS
        // Helpful tooltips throughout the UI
        // =============================

        /// <summary>
        /// Add helpful tooltip to control
        /// </summary>
        public static void AddTooltip(Control control, string text)
        {
            var tooltip = new ToolTip
            {
                InitialDelay = 500,
                ReshowDelay = 200,
                AutoPopDelay = 5000,
                ShowAlways = true
            };
            tooltip.SetToolTip(control, text);
        }

        /// <summary>
        /// Create help button with information
        /// </summary>
        public static Button CreateHelpButton(string helpText)
        {
            var btnHelp = new Button
            {
                Text = "?",
                Size = new Size(25, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            btnHelp.Click += (s, e) =>
            {
                MessageBox.Show(helpText, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            return btnHelp;
        }

        // =============================
        // VISUAL FEEDBACK
        // Progress indicators and status messages
        // =============================

        /// <summary>
        /// Show analysis in progress indicator
        /// </summary>
        public static void ShowAnalysisProgress(StatusStrip statusStrip, string message)
        {
            if (statusStrip == null || statusStrip.Items.Count == 0)
                return;

            var statusLabel = statusStrip.Items[0] as ToolStripStatusLabel;
            if (statusLabel != null)
            {
                statusLabel.Text = message;
                statusStrip.Refresh();
            }
        }

        /// <summary>
        /// Flash control to draw attention
        /// </summary>
        public static async void FlashControl(Control control, Color flashColor, int durationMs = 500)
        {
            if (control == null) return;

            var originalColor = control.BackColor;
            control.BackColor = flashColor;

            await System.Threading.Tasks.Task.Delay(durationMs);

            control.BackColor = originalColor;
        }
    }
}