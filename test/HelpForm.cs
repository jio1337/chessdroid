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
            this.Size = new Size(700, 750);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Help text box
            txtHelp = new RichTextBox
            {
                Location = new Point(10, 10),
                Size = new Size(665, 650),
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
                Location = new Point(300, 670),
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
            string helpText = @"CHESSDROID v2.9.0 SETTINGS GUIDE

═══════════════════════════════════════════════════════════════════
📊 BOARD DETECTION
═══════════════════════════════════════════════════════════════════

🎯 Match Threshold (0.1 - 1.0)
Controls how similar a piece must look to be recognized.

  • 0.55-0.75 (Recommended): Balanced accuracy
  • 0.40-0.54 (Relaxed): Use if pieces aren't detected
  • 0.75-1.0 (Strict): Very precise, may miss unusual pieces

📐 Canny Thresholds (Edge Detection)
Controls board outline detection.

  • Low/High should be 1:2 or 1:3 ratio (e.g., 50/150)
  • Increase both: If too many edges confuse detection
  • Decrease both: If board edges aren't found

📏 Min Board Area - Minimum pixel area to detect as board
🔍 Debug Cells - Show detected squares for troubleshooting

═══════════════════════════════════════════════════════════════════
🤖 CHESS ENGINE
═══════════════════════════════════════════════════════════════════

🧠 Engine Depth (1-20)
  • 10-15 (Recommended): Good balance of speed and strength
  • 16-20: Maximum strength but slower

⏱ Response Timeout - Max wait time for engine analysis
🔄 Max Retries - Retry attempts on engine failure
⏳ Move Timeout - Total time for full analysis cycle
⏱ Min Analysis Time - Minimum analysis time (prevents rushed moves)

🎮 Engine Selection - Choose from engines in /Engines folder
🌐 Site Selection - Lichess or Chess.com (affects piece templates)

═══════════════════════════════════════════════════════════════════
📝 EXPLANATION SETTINGS
═══════════════════════════════════════════════════════════════════

📚 Complexity Level
Controls how detailed explanations are:

  • Beginner: Simple terms, basic concepts
  • Intermediate: Standard chess terminology
  • Advanced: Full technical details
  • Master: Complete analysis with all metrics

🎯 Feature Toggles (all independently controllable):

  ♟ Tactical Analysis
    Pins, forks, skewers, discovered attacks, sacrifices,
    tempo attacks, perpetual check detection

  ♟ Positional Analysis
    Pawn structure, outposts, piece mobility, king safety,
    central control, development advice

  ♟ Endgame Analysis
    Opposition detection, rule of the square, king activity,
    insufficient material, fortress detection, zugzwang,
    passed pawn evaluation, mop-up technique

  ♟ Opening Principles
    Opening move descriptions and principles

  ♟ SEE Values
    Static Exchange Evaluation - shows material won/lost
    after all captures on a square

  ♟ Threats Analysis
    Shows threats created by your move (⚔) and
    defenses against opponent threats (🛡)

  ♟ WDL & Sharpness
    Win/Draw/Loss percentages and position sharpness
    Inspired by Lc0's probability-based evaluation

═══════════════════════════════════════════════════════════════════
🎛 LC0-INSPIRED FEATURES
═══════════════════════════════════════════════════════════════════

⚔ Aggressiveness Slider (0-100)
Filters move suggestions based on playing style:

  • 0-20 (Very Solid): Prefer safe, defensive moves
  • 21-40 (Solid): Slightly conservative
  • 41-60 (Balanced): No filtering, show all moves
  • 61-80 (Aggressive): Prefer active, dynamic moves
  • 81-100 (Very Aggressive): Maximum attacking chances

📖 Show Opening Name
Displays the detected opening name (e.g., 'Sicilian Defense')

⭐ Show Move Quality
Shows quality labels: Brilliant (!!) Best (!) Good Inaccuracy (?!)
Mistake (?) Blunder (??)

═══════════════════════════════════════════════════════════════════
📺 DISPLAY OPTIONS
═══════════════════════════════════════════════════════════════════

Show Best Line - Always shows the #1 recommended move
Show Second Line - Shows 2nd best alternative
Show Third Line - Shows 3rd best alternative

Comparing multiple lines helps understand why one move is better!

═══════════════════════════════════════════════════════════════════
🔄 AUTO-MONITOR (BETA)
═══════════════════════════════════════════════════════════════════

Automatically analyzes the board when it's your turn.
Toggle with Alt+K hotkey or checkbox in settings.

⚠ Known limitations:
  • May miss very fast opponent moves (<200ms)
  • Occasional issues with rapid position changes
  • Disabled by default for stability

═══════════════════════════════════════════════════════════════════
⌨ KEYBOARD SHORTCUTS
═══════════════════════════════════════════════════════════════════

  Alt+X     Analyze current position
  Alt+K     Toggle Auto-Monitor on/off

═══════════════════════════════════════════════════════════════════
💡 TROUBLESHOOTING
═══════════════════════════════════════════════════════════════════

Pieces not detected → Lower Match Threshold (0.50-0.60)
Wrong pieces detected → Increase Match Threshold (0.70-0.80)
Board not found → Adjust Canny Thresholds (try 60/180)
Engine timeout → Reduce Depth or increase Timeout
No endgame insights → Enable 'Endgame Analysis' toggle
Explanations too verbose → Lower Complexity level";

            txtHelp.Text = helpText;
            txtHelp.SelectionStart = 0;
            txtHelp.ScrollToCaret();
        }
    }
}