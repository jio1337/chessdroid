namespace ChessDroid
{
    public class HelpForm : Form
    {
        private RichTextBox txtHelp = null!;
        private Button btnClose = null!;

        public HelpForm()
        {
            InitializeComponent();
            LoadHelpText();
        }

        private void InitializeComponent()
        {
            this.Text = "chessdroid://help";
            this.Size = new Size(700, 650);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Help text box
            txtHelp = new RichTextBox
            {
                Location = new Point(10, 10),
                Size = new Size(665, 550),
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Courier New", 9F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            this.Controls.Add(txtHelp);

            // Close button
            btnClose = new Button
            {
                Text = "Close",
                Location = new Point(300, 570),
                Size = new Size(85, 30),
                DialogResult = DialogResult.OK
            };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            this.AcceptButton = btnClose;
            this.CancelButton = btnClose;
        }

        private void LoadHelpText()
        {
            string helpText = @"CHESSDROID SETTINGS GUIDE

═══════════════════════════════════════════════════════════════════
📊 BOARD DETECTION
═══════════════════════════════════════════════════════════════════

🎯 Match Threshold (0.1 - 1.0)
Controls how similar a piece must look to be recognized.

  • 0.55-0.75 (Recommended): Balanced accuracy
    ✓ Works for most boards and lighting conditions
    ✓ Minimizes false positives

  • 0.40-0.54 (Relaxed): More forgiving
    ✓ Use if pieces aren't being detected
    ✓ Better for unusual piece sets
    ⚠ May detect wrong pieces occasionally

  • 0.75-1.0 (Strict): Very precise
    ✓ Eliminates almost all errors
    ⚠ May miss pieces like Neo on Chess.com
    ⚠ Requires perfect templates (Classic sets are recommended)

📐 Canny Thresholds (Edge Detection)
Controls board outline detection for finding the chess board.

  • Low Threshold: Starting point for edge detection
  • High Threshold: Strong edge confirmation (should be 2-3x low value)

  • Default (50/150): Works for most boards
  • Increase both: If board not detected (too many edges confusing it)
  • Decrease both: If board detection misses squares

📏 Min Board Area (pixels²)
Minimum size to consider as a chess board.

  • Increase: If detecting wrong rectangular objects
  • Decrease: If your board appears too small on screen
  • Default (5000): Good for standard screen sizes

═══════════════════════════════════════════════════════════════════
🤖 CHESS ENGINE
═══════════════════════════════════════════════════════════════════

⏱ Response Timeout (ms)
Maximum time to wait for the engine to analyze a position.

  • 3000-5000ms: quick suggestions
  • 8000-15000ms: better analysis
  • 20000ms+: Deep analysis for complex positions
  • Default (10000ms): Good for most use cases

🔄 Max Retries
How many times to retry if the engine fails or times out.

  • 3 (Recommended): Standard retry behavior
  • Higher values: More resilient but slower recovery
  • Lower values: Faster failure, less waiting

⏳ Move Timeout (ms)
Maximum time for the entire move analysis sequence:
(board detection → position analysis → engine evaluation)

  • Increase: If you frequently see timeout errors
  • Default (30000ms): Usually sufficient for normal play
  • Lower: For faster detection cycles

🧠 Engine Depth (1-20)
Controls how deeply the engine analyzes each position.

  • 1-8: Lightning fast, basic analysis
    ⚠ May miss tactical nuances

  • 10-15 (Recommended): Balanced strength
    ✓ Solid tactical awareness
    ✓ Default (15): Sweet spot for most games

  • 16-20: Maximum strength, slower
    ✓ Deep positional understanding
    ⚠ Analysis takes longer
    ⚠ May timeout

═══════════════════════════════════════════════════════════════════
💡 TROUBLESHOOTING TIPS
═══════════════════════════════════════════════════════════════════

Problem: Pieces not being detected
→ Solution: Lower Match Threshold to 0.50-0.60

Problem: Wrong pieces detected
→ Solution: Increase Match Threshold to 0.70-0.80

Problem: Board outline not found
→ Solution: Adjust Canny Thresholds (try 60/180 or 40/120)

Problem: Detecting wrong objects as board
→ Solution: Increase Min Board Area to 8000-15000

Problem: Engine timeout errors
→ Solution: Reduce Engine Depth or increase Response Timeout

Problem: Analysis too slow
→ Solution: Decrease Engine Depth to 10-12 or reduce Response Timeout

Problem: Weak move suggestions
→ Solution: Increase Engine Depth to 16-18 for stronger analysis

═══════════════════════════════════════════════════════════════════
🚀 QUICK START
═══════════════════════════════════════════════════════════════════

1. Start with default settings (click 'Defaults' button if needed)
2. Set Engine Depth based on your needs
3. If pieces aren't detected → Adjust Match Threshold
4. If board outline is wrong → Adjust Canny Thresholds
5. If you see timeout errors → Reduce Engine Depth or increase timeouts
6. Click 'Save & Apply' when done

chessdroid displays analysis in the console/moves list. All lines
and evaluations are shown";

            txtHelp.Text = helpText;
            txtHelp.SelectionStart = 0;
            txtHelp.ScrollToCaret();
        }
    }
}