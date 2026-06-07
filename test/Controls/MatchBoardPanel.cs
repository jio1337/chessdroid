using System.Diagnostics;
using System.Text;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Controls
{
    /// <summary>
    /// Self-contained panel hosting one engine-vs-engine match series.
    /// Supports two display modes:
    ///   Mini     — board + engine/clock labels only (fits inside a small cell)
    ///   Expanded — board on the left + move log + eval bar on the right
    /// The TournamentForm controls the physical Size; this panel adapts its
    /// internal layout accordingly.
    /// </summary>
    public class MatchBoardPanel : UserControl
    {
        // ── Controls ─────────────────────────────────────────────────────────
        private readonly ChessBoardControl  _board;
        private readonly EvalBarControl     _evalBar;
        private readonly Label              _lblBlack;
        private readonly Label              _lblWhite;
        private readonly Label              _lblBlackClock;
        private readonly Label              _lblWhiteClock;
        private readonly Label              _lblScore;
        private readonly Font               _fontBold;
        private readonly Font               _fontRegular;
        private readonly RichTextBox        _log;
        private readonly Panel              _pnlDetail;
        private readonly System.Windows.Forms.Timer _clockTimer;

        // ── Series state ─────────────────────────────────────────────────────
        private EngineMatchService? _service;
        private AppConfig?          _config;
        private string              _engineBasePath = "";
        private bool                _adjudicate     = true;
        private EngineMatchTimeControl _tc          = new();

        private string _whiteFile = "";
        private string _blackFile = "";
        private string _whiteName = "";
        private string _blackName = "";

        private int    _gamesTotal   = 1;
        private int    _gamesPlayed  = 0;
        private double _score1       = 0;   // engine that played White in game 1
        private double _score2       = 0;
        private int    _wins1        = 0;
        private int    _wins2        = 0;
        private int    _drawCount    = 0;
        private string _eng1File     = "";
        private string _eng2File     = "";
        private string _eng1Name     = "";
        private string _eng2Name     = "";
        private string _curWhiteFile = "";

        private bool _isExpanded    = false;
        private int  _fullMoveNum   = 1;
        private bool _whiteToMove   = true;

        // Raw log lines — replayed into the RichTextBox when panel is expanded
        private readonly List<string> _logLines = new();
        private string _pendingWhiteMoveLine = "";

        // Opening position for this series (empty = standard start)
        private string _openingFen = "";

        // PGN tracking — one entry per half-move, accumulated per game
        private readonly List<string> _currentGameMoves = new();
        private readonly List<(string White, string Black, string Result, List<string> Moves)> _completedGames = new();

        // ── Public API ───────────────────────────────────────────────────────
        public event EventHandler?                                     ExpandRequested;
        public event Action<TournamentStanding, TournamentStanding>?   SeriesEnded;

        public bool   IsRunning   => _service?.IsRunning == true;
        public double Score1      => _score1;
        public double Score2      => _score2;
        public int    GamesPlayed => _gamesPlayed;
        public string Eng1File    => _eng1File;
        public string Eng2File    => _eng2File;
        public string Eng1Name    => _eng1Name;
        public string Eng2Name    => _eng2Name;

        public ChessEngineService? AnnotatorEngine
        {
            set { if (_service != null) _service.AnnotatorEngine = value; }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                _pnlDetail.Visible = value;
                if (value) ReplayLog();
                ArrangeLayout();
            }
        }

        // ── Constructor ──────────────────────────────────────────────────────
        public MatchBoardPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 30, 30);
            Cursor = Cursors.Hand;

            _fontBold    = new Font("Courier New", 8.5f, FontStyle.Bold);
            _fontRegular = new Font("Courier New", 8f,   FontStyle.Regular);

            _board = new ChessBoardControl
            {
                InteractionEnabled    = false,
                ShowLastMoveHighlight = true,
                ShowSquareLabels      = false
            };

            _lblBlack      = MakeLbl("", ContentAlignment.MiddleLeft,   _fontBold);
            _lblWhite      = MakeLbl("", ContentAlignment.MiddleLeft,   _fontBold);
            _lblBlackClock = MakeLbl("", ContentAlignment.MiddleRight,  _fontRegular);
            _lblWhiteClock = MakeLbl("", ContentAlignment.MiddleRight,  _fontRegular);
            _lblScore      = MakeLbl("", ContentAlignment.MiddleCenter, _fontRegular);
            _lblScore.ForeColor = Color.FromArgb(170, 170, 170);

            _evalBar = new EvalBarControl { Width = 18 };

            _log = new RichTextBox
            {
                ReadOnly    = true,
                BackColor   = Color.FromArgb(22, 22, 22),
                ForeColor   = Color.FromArgb(210, 210, 210),
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Font        = new Font("Courier New", 8f),
                WordWrap    = false
            };

            _pnlDetail = new Panel { Visible = false, BackColor = Color.FromArgb(22, 22, 22) };
            _pnlDetail.Controls.Add(_evalBar);
            _pnlDetail.Controls.Add(_log);

            Controls.Add(_board);
            Controls.Add(_lblBlack);
            Controls.Add(_lblBlackClock);
            Controls.Add(_lblWhite);
            Controls.Add(_lblWhiteClock);
            Controls.Add(_lblScore);
            Controls.Add(_pnlDetail);

            _clockTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _clockTimer.Tick += (_, _) => RefreshClocks();

            // Forward clicks to expand handler
            foreach (Control c in new Control[] { this, _board, _lblBlack, _lblWhite, _lblScore })
                c.Click += (_, _) => ExpandRequested?.Invoke(this, EventArgs.Empty);

            Resize += (_, _) => ArrangeLayout();
        }

        // ── Start / Stop ─────────────────────────────────────────────────────
        public void StartSeries(
            string white1File, string black1File,
            string white1Name, string black1Name,
            EngineMatchTimeControl tc,
            string engineBasePath,
            AppConfig config,
            int gamesTotal,
            bool adjudicate,
            string? openingFen = null)
        {
            _config         = config;
            _engineBasePath = engineBasePath;
            _openingFen     = openingFen ?? "";

            if (!string.IsNullOrEmpty(config.SelectedSite))
                _board.SetTemplateSet(config.SelectedSite);
            _tc             = tc;
            _gamesTotal     = gamesTotal;
            _adjudicate     = adjudicate;

            _eng1File = _curWhiteFile = _whiteFile = white1File;
            _eng2File = _blackFile = black1File;
            _eng1Name = _whiteName = white1Name;
            _eng2Name = _blackName = black1Name;

            _score1 = _score2 = 0;
            _wins1 = _wins2 = _drawCount = 0;
            _gamesPlayed = 0;
            _completedGames.Clear();

            UpdateLabels();
            BeginGame();
        }

        public void StopSeries()
        {
            _clockTimer.Stop();
            _service?.StopMatch();
        }

        // ── Internal game loop ───────────────────────────────────────────────
        private void BeginGame()
        {
            _logLines.Clear();
            _currentGameMoves.Clear();
            _pendingWhiteMoveLine = "";
            if (_isExpanded) _log.Clear();

            // Initialise board to opening position (or standard start)
            if (!string.IsNullOrEmpty(_openingFen))
            {
                _board.LoadFEN(_openingFen);
                var parts = _openingFen.Split(' ');
                _whiteToMove = parts.Length < 2 || parts[1] == "w";
                _fullMoveNum = parts.Length >= 6 && int.TryParse(parts[5], out int fm) ? fm : 1;
            }
            else
            {
                _board.ResetBoard();
                _fullMoveNum = 1;
                _whiteToMove = true;
            }

            _service?.Dispose();
            _service = new EngineMatchService(_config!);
            _service.AdjudicationEnabled = _adjudicate;
            _service.WaitForAnimation    = false;

            _service.OnMovePlayed            += OnMovePlayed;
            _service.OnClockUpdated          += OnClockUpdated;
            _service.OnMatchEnded            += OnMatchEnded;
            _service.OnAnnotatorEvalUpdated  += OnAnnotatorEval;

            _clockTimer.Start();

            string wp = Path.Combine(_engineBasePath, _whiteFile);
            string bp = Path.Combine(_engineBasePath, _blackFile);
            _ = _service.StartMatchAsync(wp, bp, _whiteName, _blackName, _tc,
                string.IsNullOrEmpty(_openingFen) ? null : _openingFen);
        }

        // ── Engine callbacks ─────────────────────────────────────────────────
        private void OnMovePlayed(string uciMove, string fen, long moveTimeMs, string? eval)
        {
            if (InvokeRequired) { Invoke(() => OnMovePlayed(uciMove, fen, moveTimeMs, eval)); return; }

            // Grab FEN BEFORE applying the move (board is still at prev position)
            string prevFen = _board.GetFEN();
            _board.MakeMove(uciMove);

            string san     = SafeSan(uciMove, prevFen);
            string evalTag = eval != null ? $" [{ShortEval(eval)}]" : "";
            string timeTag = $" ({moveTimeMs / 1000.0:0.0}s)";

            // PGN-annotated half-move: "e4 { +0.32 (0.1s) }"
            string pgnComment = eval != null
                ? $" {{ {ShortEval(eval)} ({moveTimeMs / 1000.0:0.0}s) }}"
                : $" {{ ({moveTimeMs / 1000.0:0.0}s) }}";
            _currentGameMoves.Add(san + pgnComment);

            if (_whiteToMove)
            {
                // Buffer white's half-move; emit only once black replies
                _pendingWhiteMoveLine = $"{_fullMoveNum}. {san}{evalTag}{timeTag}";
            }
            else
            {
                // Combine white + black on one line
                string line = _pendingWhiteMoveLine.Length > 0
                    ? $"{_pendingWhiteMoveLine}   {san}{evalTag}{timeTag}"
                    : $"  {san}{evalTag}{timeTag}";
                _pendingWhiteMoveLine = "";
                _fullMoveNum++;
                _logLines.Add(line);
                if (_isExpanded) AppendLog(line);
            }

            _whiteToMove = !_whiteToMove;

            if (eval != null) ApplyEval(eval);
        }

        private void OnClockUpdated(long whiteMs, long blackMs, bool whiteToMove)
        {
            if (InvokeRequired) { Invoke(() => OnClockUpdated(whiteMs, blackMs, whiteToMove)); return; }
            _lblWhiteClock.Text = ClockStr(whiteMs);
            _lblBlackClock.Text = ClockStr(blackMs);
            var active   = Color.FromArgb(240, 240, 240);
            var inactive = Color.FromArgb(130, 130, 130);
            _lblWhite.ForeColor      = whiteToMove ? active : inactive;
            _lblWhiteClock.ForeColor = whiteToMove ? active : inactive;
            _lblBlack.ForeColor      = whiteToMove ? inactive : active;
            _lblBlackClock.ForeColor = whiteToMove ? inactive : active;
        }

        private void OnAnnotatorEval(string eval)
        {
            if (InvokeRequired) { Invoke(() => OnAnnotatorEval(eval)); return; }
            ApplyEval(eval);
        }

        private void OnMatchEnded(EngineMatchResult result)
        {
            if (InvokeRequired) { Invoke(() => OnMatchEnded(result)); return; }

            _clockTimer.Stop();

            bool eng1WasWhite = _curWhiteFile == _eng1File;
            switch (result.Outcome)
            {
                case MatchOutcome.WhiteWins:
                    if (eng1WasWhite) { _score1 += 1; _wins1++; } else { _score2 += 1; _wins2++; }
                    break;
                case MatchOutcome.BlackWins:
                    if (eng1WasWhite) { _score2 += 1; _wins2++; } else { _score1 += 1; _wins1++; }
                    break;
                case MatchOutcome.Draw:
                    _score1 += 0.5; _score2 += 0.5; _drawCount++;
                    break;
            }
            _gamesPlayed++;

            // Flush any pending white-only half-move (game ended before black replied)
            if (_pendingWhiteMoveLine.Length > 0)
            {
                _logLines.Add(_pendingWhiteMoveLine);
                if (_isExpanded) AppendLog(_pendingWhiteMoveLine);
                _pendingWhiteMoveLine = "";
            }

            string resultLine = "\n" + result.GetResultString();
            _logLines.Add(resultLine);
            if (_isExpanded) AppendLog(resultLine);

            UpdateScoreLabel();
            _evalBar.Reset();

            // Archive this game for PGN export
            string pgnResult = result.Outcome switch
            {
                MatchOutcome.WhiteWins => "1-0",
                MatchOutcome.BlackWins => "0-1",
                MatchOutcome.Draw      => "1/2-1/2",
                _                      => "*"
            };
            _completedGames.Add((_whiteName, _blackName, pgnResult, new List<string>(_currentGameMoves)));

            bool seriesOver = result.Outcome == MatchOutcome.Interrupted
                           || _gamesPlayed >= _gamesTotal;

            if (!seriesOver)
            {
                // Swap colors
                (_whiteFile, _blackFile) = (_blackFile, _whiteFile);
                (_whiteName, _blackName) = (_blackName, _whiteName);
                _curWhiteFile = _whiteFile;
                UpdateLabels();
                BeginGame();
            }
            else
            {
                var st1 = MakeStanding(_eng1File, _eng1Name, _score1, _wins1, _drawCount, _wins2);
                var st2 = MakeStanding(_eng2File, _eng2Name, _score2, _wins2, _drawCount, _wins1);
                SeriesEnded?.Invoke(st1, st2);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private string SafeSan(string uci, string fen)
        {
            try
            {
                return ChessNotationService.ConvertFullPvToSan(
                    uci, fen,
                    ChessRulesService.ApplyUciMove,
                    ChessRulesService.CanReachSquare,
                    ChessRulesService.FindAllPiecesOfSameType);
            }
            catch { return uci; }
        }

        private static string ShortEval(string eval)
            => eval.StartsWith("Mate in ") ? eval.Replace("Mate in ", "#") : eval;

        private static string ClockStr(long ms)
        {
            if (ms <= 0) return "0:00";
            int s = (int)(ms / 1000);
            return $"{s / 60}:{s % 60:D2}";
        }

        private void RefreshClocks()
        {
            if (_service == null || !_service.IsRunning) return;
            _lblWhiteClock.Text = ClockStr(_service.WhiteRemainingMs);
            _lblBlackClock.Text = ClockStr(_service.BlackRemainingMs);
        }

        private void ApplyEval(string eval)
        {
            if (eval.StartsWith("Mate in "))
            {
                if (int.TryParse(
                    eval["Mate in ".Length..].Replace("+","").Trim(),
                    out int m)) _evalBar.SetMate(m);
            }
            else if (double.TryParse(eval.TrimStart('+'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double cp))
            {
                _evalBar.SetEvaluation(cp);
            }
        }

        private void AppendLog(string line)
        {
            _log.AppendText(line + "\n");
            _log.SelectionStart = _log.Text.Length;
            _log.ScrollToCaret();
        }

        private void ReplayLog()
        {
            _log.Clear();
            foreach (var l in _logLines) _log.AppendText(l + "\n");
            _log.SelectionStart = _log.Text.Length;
            _log.ScrollToCaret();
        }

        private void UpdateLabels()
        {
            if (InvokeRequired) { Invoke(UpdateLabels); return; }
            _lblBlack.Text = _blackName;
            _lblWhite.Text = _whiteName;
        }

        private void UpdateScoreLabel()
        {
            if (_gamesTotal <= 1) { _lblScore.Text = ""; return; }
            string s1 = _score1 % 1 == 0 ? $"{_score1:0}" : $"{_score1:0.0}";
            string s2 = _score2 % 1 == 0 ? $"{_score2:0}" : $"{_score2:0.0}";
            _lblScore.Text = $"({_gamesPlayed}/{_gamesTotal})  {s1} – {s2}";
        }

        private static TournamentStanding MakeStanding(string file, string name,
            double pts, int wins, int draws, int losses)
            => new TournamentStanding
            {
                Engine = new TournamentEngineEntry { FileName = file, DisplayName = name },
                Points = pts,
                Wins   = wins,
                Draws  = draws,
                Losses = losses,
                Played = wins + draws + losses
            };

        public void SetBoardAppearance(Color light, Color dark)
        {
            _board.SetSquareColors(light, dark);
        }

        /// <summary>
        /// Builds a PGN block for all completed games in this series.
        /// Each game gets proper headers and annotated moves (eval + time as comments).
        /// </summary>
        public string BuildSeriesPgn(string eventName, string date, string timeControlTag, int roundStart)
        {
            if (_completedGames.Count == 0) return "";

            var sb = new StringBuilder();
            for (int i = 0; i < _completedGames.Count; i++)
            {
                var (white, black, result, moves) = _completedGames[i];
                sb.AppendLine($"[Event \"{eventName}\"]");
                sb.AppendLine($"[Site \"Chessdroid\"]");
                sb.AppendLine($"[Date \"{date}\"]");
                sb.AppendLine($"[Round \"{roundStart + i}\"]");
                sb.AppendLine($"[White \"{white}\"]");
                sb.AppendLine($"[Black \"{black}\"]");
                sb.AppendLine($"[Result \"{result}\"]");
                if (!string.IsNullOrEmpty(timeControlTag))
                    sb.AppendLine($"[TimeControl \"{timeControlTag}\"]");
                sb.AppendLine();

                // One full move (white + black) per line for readability
                for (int m = 0; m < moves.Count; m++)
                {
                    if (m % 2 == 0)
                    {
                        if (m > 0) sb.AppendLine();
                        sb.Append($"{m / 2 + 1}. {moves[m]}");
                    }
                    else
                    {
                        sb.Append($" {moves[m]}");
                    }
                }
                sb.AppendLine();
                sb.AppendLine(result);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        // ── Layout ───────────────────────────────────────────────────────────
        private void ArrangeLayout()
        {
            const int LH  = 22;   // label height
            const int SH  = 18;   // score label height
            const int Pad = 3;
            const int EBW = 18;   // eval bar width

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w < 20 || h < 20) return;

            int boardAreaW = _isExpanded ? (w - w / 3 - Pad) : w;

            // Detail panel (expanded only)
            if (_isExpanded)
            {
                int dw = w - boardAreaW - Pad;
                _pnlDetail.SetBounds(boardAreaW + Pad, 0, dw, h);
                _evalBar.SetBounds(0, 0, EBW, h);
                _log.SetBounds(EBW + 2, 0, dw - EBW - 2, h);
            }

            // Board sizing inside the board area
            int boardMax = Math.Min(boardAreaW - Pad * 2, h - LH * 2 - SH - Pad * 3);
            int boardSz  = Math.Max(boardMax, 40);
            int boardX   = (boardAreaW - boardSz) / 2;
            int boardY   = LH + Pad;

            _board.SetBounds(boardX, boardY, boardSz, boardSz);

            int hw = boardAreaW / 2;
            _lblBlack.SetBounds(Pad, 0, hw - Pad, LH);
            _lblBlackClock.SetBounds(hw, 0, hw - Pad, LH);

            int wy = boardY + boardSz + Pad;
            _lblWhite.SetBounds(Pad, wy, hw - Pad, LH);
            _lblWhiteClock.SetBounds(hw, wy, hw - Pad, LH);
            _lblScore.SetBounds(Pad, wy + LH + 1, boardAreaW - Pad * 2, SH);
        }

        private static Label MakeLbl(string text, ContentAlignment align, Font font)
            => new Label
            {
                Text      = text,
                TextAlign = align,
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.Transparent,
                Font      = font,
                AutoSize  = false
            };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _clockTimer.Stop();
                _clockTimer.Dispose();
                _fontBold.Dispose();
                _fontRegular.Dispose();
                _service?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
