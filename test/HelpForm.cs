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
            string helpText = @"CHESSDROID v3.0.0 - CHESS ANALYSIS BOARD

═══════════════════════════════════════════════════════════════════
🎮 GETTING STARTED
═══════════════════════════════════════════════════════════════════

Chessdroid is a powerful chess analysis tool featuring:
  • 30+ tactical pattern detection (pins, forks, skewers, etc.)
  • Move quality classification (Brilliant, Best, Good, etc.)
  • Opening book support (Polyglot .bin format)
  • Engine vs Engine matches with adjustable strength
  • PGN import/export with full move tree support

═══════════════════════════════════════════════════════════════════
🤖 CHESS ENGINE
═══════════════════════════════════════════════════════════════════

🧠 Engine Depth (1-20)
  • 10-15 (Recommended): Good balance of speed and strength
  • 16-20: Maximum strength but slower

⏱ Response Timeout - Max wait time for engine analysis
🔄 Max Retries - Retry attempts on engine failure
⏳ Move Timeout - Total time for full analysis cycle
⏱ Min Analysis Time - Minimum analysis time

🎮 Engine Selection - Choose from engines in /Engines folder
🎨 Piece Set - Choose piece style from /Templates folder

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
    tempo attacks, zwischenzug, perpetual check

  ♟ Positional Analysis
    Pawn structure, outposts, piece mobility, king safety,
    central control, development advice

  ♟ Endgame Analysis
    Opposition detection, rule of the square, king activity,
    insufficient material, fortress detection, zugzwang

  ♟ Opening Principles
    Opening move descriptions and principles

  ♟ SEE Values
    Static Exchange Evaluation - material won/lost after captures

  ♟ Threats Analysis
    Threats created by your move and opponent threats

  ♟ WDL & Sharpness
    Win/Draw/Loss percentages and position sharpness

═══════════════════════════════════════════════════════════════════
🎛 PLAY STYLE
═══════════════════════════════════════════════════════════════════

⚔ Aggressiveness Slider (0-100)

  • 0-20 (Very Solid): Prefer safe, defensive moves
  • 21-40 (Solid): Slightly conservative
  • 41-60 (Balanced): No filtering, show all moves
  • 61-80 (Aggressive): Prefer active, dynamic moves
  • 81-100 (Very Aggressive): Maximum attacking chances

📖 Show Opening Name
Displays the detected opening (e.g., 'Sicilian Defense')

⭐ Show Move Quality
Shows: Brilliant (!!) Best (!) Good Inaccuracy (?!)
       Mistake (?) Blunder (??)

═══════════════════════════════════════════════════════════════════
📺 DISPLAY OPTIONS
═══════════════════════════════════════════════════════════════════

Show Best/Second/Third Line - Control how many engine lines to show
Comparing multiple lines helps understand why one move is better!

═══════════════════════════════════════════════════════════════════
⌨ KEYBOARD SHORTCUTS
═══════════════════════════════════════════════════════════════════

  Ctrl+O    Open PGN file
  Ctrl+S    Save PGN file
  Ctrl+V    Paste FEN/PGN from clipboard
  Ctrl+C    Copy current FEN
  Left/Right arrows - Navigate moves

═══════════════════════════════════════════════════════════════════
💡 TIPS
═══════════════════════════════════════════════════════════════════

Engine timeout → Reduce Depth or increase Timeout
No endgame insights → Enable 'Endgame Analysis' toggle
Explanations too verbose → Lower Complexity level
Want book moves → Enable 'Show Book Moves' in settings";

            txtHelp.Text = helpText;
            txtHelp.SelectionStart = 0;
            txtHelp.ScrollToCaret();
        }
    }
}