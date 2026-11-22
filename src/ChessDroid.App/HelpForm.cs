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
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

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
            string helpText =
"CHESSDROID v3.17.0 - CHESS ANALYSIS BOARD\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"FEATURES AT A GLANCE\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"  • 30+ tactical pattern detection (pins, forks, skewers, X-rays...)\r\n" +
"  • Move quality classification (Brilliant !! to Blunder ??)\r\n" +
"  • Continuous analysis with live depth-by-depth streaming\r\n" +
"  • Opening book (Polyglot .bin) + ECO database with SF18 evals\r\n" +
"  • Bot mode -- Elo targeting 1320-3190, Challenge & Friendly\r\n" +
"  • Engine vs Engine matches + round-robin Tournament mode\r\n" +
"  • Chess 960 (Fischer Random) -- position browser + bot support\r\n" +
"  • Puzzle Training -- Lichess DB (5.9M puzzles), streaks, Gauntlet\r\n" +
"  • Opening Explorer -- ECO/Name/Eval grid, load into board\r\n" +
"  • Drills -- 10 PGN studies (tactics + endgames), Practice vs Bot\r\n" +
"  • Game Library -- save, rename, reload games\r\n" +
"  • Annotated PGN import/export (NAGs, eval comments)\r\n" +
"  • Eval bar + eval graph + [See line] animated PV exploration\r\n" +
"  • Threat arrows (red) and engine arrows (green/yellow/red)\r\n" +
"  • Sound effects, board effects (gradient, vignette, glow, frame)\r\n" +
"  • 22 piece sets, 6 UI themes, custom board colors\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"KEYBOARD SHORTCUTS\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"  Left / Right     Navigate moves (previous / next)\r\n" +
"  Up / Down        Navigate variations at same level\r\n" +
"  Home / End       Jump to start / end of game\r\n" +
"  Ctrl+N           New Game\r\n" +
"  Ctrl+F           Flip Board\r\n" +
"  Backspace        Take back last move\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"CHESS ENGINE\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"Engine Depth (1-40)\r\n" +
"  • 12-18: Good balance of speed and quality\r\n" +
"  • 19-30: Stronger, slower\r\n" +
"  • 31-40: Deep analysis, may take time on complex positions\r\n" +
"\r\n" +
"Continuous Analysis - Live streaming up to max depth, then full\r\n" +
"  result with explanations at the final depth\r\n" +
"Auto-play speed - Delay between moves in ms (default 600)\r\n" +
"\r\n" +
"Engine Selection - Choose from engines in /Engines folder\r\n" +
"  Bundled: Stockfish 18, Ethereal 14.40, Berserk 13\r\n" +
"Piece Set - Choose from 22 sets in /Templates folder\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"ANALYSIS SETTINGS\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"Feature Toggles (independently controllable):\r\n" +
"  Tactical Analysis  - Pins, forks, sacrifices, zwischenzug\r\n" +
"  Positional         - Pawn structure, outposts, king safety\r\n" +
"  Endgame            - Opposition, rule of the square, zugzwang\r\n" +
"  Opening Principles - Center control, development advice\r\n" +
"  Threats Analysis   - Opponent threats, mate-in-1 detection\r\n" +
"  WDL + Sharpness    - Win/Draw/Loss percentages, sharpness score\r\n" +
"\r\n" +
"Show Best/Second/Third Line - How many engine PV lines to display\r\n" +
"Engine Arrows - None / Best only / Best+2nd / All 3\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"BOT MODE\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"Elo targeting from 1320 (Beginner) to 3190 (Superhuman).\r\n" +
"  Presets: Beginner 1350 / Club 1700 / Advanced 2100 / Expert 2500\r\n" +
"\r\n" +
"Choose engine, color (White or Black), and mode:\r\n" +
"  Friendly  - Analysis runs normally while you play\r\n" +
"  Challenge - Engine eval hidden; no arrows or analysis shown\r\n" +
"\r\n" +
"Take Back goes back 2 moves (your move + bot response).\r\n" +
"Draw detection: insufficient material, 50-move rule, threefold.\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"CHESS 960\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"960 Fischer Random starting positions. Click the 960 toolbar\r\n" +
"button to open the position browser -- filter by SP number or\r\n" +
"piece layout (e.g. 518 or RNBQKBNR). Load a position or launch\r\n" +
"a bot game directly from the browser.\r\n" +
"\r\n" +
"SP-518 = RNBQKBNR = standard chess starting position.\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"PUZZLE TRAINING\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"Training sub-modes:\r\n" +
"  Training    - Solve puzzles one at a time, build streaks\r\n" +
"  Gauntlet    - Escalating difficulty until you fail\r\n" +
"  Puzzle Rush - Timed solve-as-many-as-you-can sprint\r\n" +
"\r\n" +
"Filters: Rating range, Opening theme, Daily Puzzle.\r\n" +
"Personal bests saved per mode. 5.9M puzzles from Lichess DB.\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"DRILLS\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"10 PGN study files -- 5 tactics + 5 endgame series:\r\n" +
"  Tactics: Pins, Back-rank, Forks, Discovered attacks, Checkmates\r\n" +
"  Endgames: Beginner -> Intermediate -> Advanced -> More + Rook DB\r\n" +
"\r\n" +
"Navigate chapters with Prev/Next. Load any position onto the\r\n" +
"board. Practice vs Bot plays from that FEN.\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"DISPLAY OPTIONS\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"Eval Bar      - Vertical bar showing white/black advantage\r\n" +
"Eval Graph    - Move-by-move eval curve across the full game\r\n" +
"Show Coords   - Toggle board coordinates (a-h, 1-8)\r\n" +
"Square Labels - Show square names (e4, d5...) on the board\r\n" +
"Threat Arrows - Red arrows for opponent threats on your pieces\r\n" +
"Book Arrows   - Blue arrows showing opening book moves\r\n" +
"\r\n" +
"Board Effects - Gradient, Vignette, Piece Glow, Board Frame\r\n" +
"Themes        - Dark / Light / Cyberpunk / Dracula / Nord / Sepia\r\n" +
"\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"TIPS\r\n" +
"═══════════════════════════════════════════════════════════════\r\n" +
"\r\n" +
"Engine slow      -> Reduce Depth in Settings\r\n" +
"No book arrows   -> Ensure a .bin file is in /Books folder\r\n" +
"Threat arrows    -> Enable in Settings > Threat Arrows toggle\r\n" +
"Auto-play pauses -> Press '>>' again or press Right arrow\r\n" +
"Bot draw         -> Detected automatically (repetition/50-move)";

            txtHelp.Text = helpText;
            txtHelp.SelectionStart = 0;
            txtHelp.ScrollToCaret();
        }
    }
}
