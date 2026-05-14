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
            string helpText = @"CHESSDROID v3.8.0 - CHESS ANALYSIS BOARD

═══════════════════════════════════════════════════════════════════
GETTING STARTED
═══════════════════════════════════════════════════════════════════

Interactive chess analysis board with:
  • 30+ tactical pattern detection (pins, forks, skewers, etc.)
  • Move quality classification (Brilliant, Best, Inaccuracy, etc.)
  • Full mate-in-1 threat detection after every move
  • Opening book (Polyglot .bin + ECO database)
  • Bot mode (play vs Stockfish, 5 difficulty levels)
  • Engine vs Engine matches with configurable time controls
  • Annotated PGN import/export (NAGs, eval comments)
  • [See line] animated PV exploration
  • Auto-play through move list
  • Continuous analysis with live depth updates
  • Threat arrows (red) and engine arrows (green/yellow/red)
  • Move sounds (configurable)

═══════════════════════════════════════════════════════════════════
KEYBOARD SHORTCUTS
═══════════════════════════════════════════════════════════════════

  Left / Right     Navigate moves (previous / next)
  Up / Down        Navigate variations at same level
  Home / End       Jump to start / end of game
  Ctrl+N           New Game
  Ctrl+F           Flip Board
  Backspace        Take back last move

═══════════════════════════════════════════════════════════════════
CHESS ENGINE
═══════════════════════════════════════════════════════════════════

Engine Depth (1-20)
  • 10-15: Good balance of speed and analysis quality
  • 16-20: Stronger but slower

Min Analysis Time - Engine thinks for at least this long (ms)
Continuous Analysis - Live depth-by-depth streaming up to max depth
Auto-play speed - Time between moves during auto-play (ms)

Engine Selection - Choose from engines in /Engines folder
Piece Set - Choose piece style from /Templates folder

═══════════════════════════════════════════════════════════════════
EXPLANATION SETTINGS
═══════════════════════════════════════════════════════════════════

Complexity Level controls explanation detail:
  • Beginner: Simple terms, basic concepts
  • Intermediate: Standard chess terminology
  • Advanced: Full technical details
  • Master: All metrics shown

Feature Toggles (independently controllable):
  Tactical Analysis  - Pins, forks, sacrifices, zwischenzug
  Positional         - Pawn structure, outposts, king safety
  Endgame            - Opposition, rule of the square, zugzwang
  Opening Principles - Center control, development advice
  Threats Analysis   - Threats created, opponent threats, mate-in-1
  WDL + Sharpness    - Win/Draw/Loss percentages, position sharpness

═══════════════════════════════════════════════════════════════════
DISPLAY OPTIONS
═══════════════════════════════════════════════════════════════════

Show Best/Second/Third Line - Control how many engine PV lines show
Engine Arrows - None / Best only / Best+2nd / All 3
Move Sounds - Click on move, different sound for captures
Show Eval Bar - Toggle the evaluation bar on/off

Board Colors - Custom light/dark square colors + 7 presets
Square Labels - Show square names (e4, d5...) on the board
Threat Arrows - Red arrows pointing to hanging/capturable pieces

═══════════════════════════════════════════════════════════════════
PLAY STYLE
═══════════════════════════════════════════════════════════════════

Aggressiveness Slider (0-100):
  • 0-20  (Very Solid): Prefer safe, defensive moves
  • 21-40 (Solid): Slightly conservative
  • 41-60 (Balanced): No filtering, show all moves
  • 61-80 (Aggressive): Prefer active, dynamic moves
  • 81-100 (Very Aggressive): Maximum attacking chances

═══════════════════════════════════════════════════════════════════
BOT MODE
═══════════════════════════════════════════════════════════════════

Play vs Stockfish at 5 difficulty levels:
  Easy / Medium / Hard / Expert / Master

Choose your color (White or Black).
Take Back goes back 2 moves (yours + bot's response).
Challenge mode hides engine analysis while you play.

═══════════════════════════════════════════════════════════════════
TIPS
═══════════════════════════════════════════════════════════════════

Engine timeout   -> Reduce Depth or increase Timeout
No book arrows   -> Make sure a .bin file is in /Books folder
Threat arrows    -> Enable in Settings > Board Colors > Threat Arrows
Auto-play pauses -> Press '>>' button again or press Right arrow";

            txtHelp.Text = helpText;
            txtHelp.SelectionStart = 0;
            txtHelp.ScrollToCaret();
        }
    }
}