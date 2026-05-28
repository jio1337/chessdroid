using System.Text;
using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    /// <summary>
    /// Runs multiple engine-vs-engine match series simultaneously in a 2×2 board grid.
    /// Supports Round-Robin (auto-generate all pairings from a pool) and Manual pairing.
    /// Up to 4 matches run concurrently; additional pairings queue and start as slots free.
    /// One shared annotator engine provides live eval on the focused (expanded) board.
    /// </summary>
    public class TournamentForm : Form
    {
        // ── Config ───────────────────────────────────────────────────────────
        private readonly AppConfig _config;
        private readonly string    _engineBasePath;

        // ── Setup UI ─────────────────────────────────────────────────────────
        private readonly Panel           _pnlSetup;
        private readonly RadioButton     _rbRoundRobin;
        private readonly RadioButton     _rbManual;
        private readonly CheckedListBox  _lstRREngines;   // Round-robin pool
        private readonly Panel           _pnlManualPairs; // 4 manual pair rows
        private readonly ComboBox[]      _cmbWhite  = new ComboBox[4];
        private readonly ComboBox[]      _cmbBlack  = new ComboBox[4];
        private readonly Label[]         _lblVs     = new Label[4];
        private readonly NumericUpDown   _numGames;
        private readonly CheckBox        _chkAdjudicate;
        // Time control
        private readonly RadioButton     _rbDepth;
        private readonly RadioButton     _rbMovetime;
        private readonly RadioButton     _rbClock;
        private readonly NumericUpDown   _numDepth;
        private readonly NumericUpDown   _numMovetime;
        private readonly NumericUpDown   _numTotal;
        private readonly NumericUpDown   _numInc;
        private readonly Button          _btnStart;
        private readonly CheckBox        _chkUseOpeningBook;
        private readonly Panel           _pnlBookMode;    // isolates book radios from pairing radios
        private readonly RadioButton     _rbBookRandom;
        private readonly RadioButton     _rbBookChoose;
        private readonly Label           _lblChosenOpening;
        private OpeningEntry?            _bookOpening;

        // ── Match UI ─────────────────────────────────────────────────────────
        private readonly Panel           _pnlMatch;
        private readonly Panel           _pnlBoards;      // hosts the 4 MatchBoardPanels
        private readonly Panel           _pnlStandings;
        private readonly ListView        _lvStandings;
        private readonly Button          _btnStop;
        private readonly Label           _lblTournamentTitle;

        // ── Board slots ───────────────────────────────────────────────────────
        private const int MaxConcurrent = 4;
        private readonly MatchBoardPanel[] _panels = new MatchBoardPanel[MaxConcurrent];
        private MatchBoardPanel?           _focusedPanel;

        // ── Tournament state ─────────────────────────────────────────────────
        private Queue<TournamentPairing>              _queue     = new();
        private Dictionary<string, TournamentStanding> _standings = new();
        private readonly TournamentEngineEntry[]      _engines;   // populated after setup
        private ChessEngineService?                   _annotator;
        private bool                                  _running;
        private PolyglotBookService?                  _bookService;

        // ── Results export ───────────────────────────────────────────────────
        private readonly List<string> _allPgnGames  = new();
        private int    _roundCounter   = 1;
        private int    _totalMatchCount = 0;
        private string _tcDescription  = "";
        private string _tcPgnTag       = "";

        // ── Constructor ──────────────────────────────────────────────────────
        public TournamentForm(AppConfig config)
        {
            _config = config;
            _engineBasePath = config.GetEnginesPath();

            Text            = "Chessdroid Tournament";
            Size            = new Size(520, 510);
            MinimumSize     = new Size(400, 380);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(25, 25, 25);
            ForeColor       = Color.FromArgb(220, 220, 220);
            Font            = new Font("Courier New", 9f);

            // ── Setup panel ─────────────────────────────────────────────────
            _pnlSetup = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var title = new Label
            {
                Text      = "⚔  Tournament Setup",
                Font      = new Font("Courier New", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 180, 80),
                AutoSize  = true,
                Location  = new Point(20, 16)
            };

            // Pairing mode radios
            _rbRoundRobin = MakeRadio("Round-robin (auto-generate all pairings)", new Point(20, 60),  true);
            _rbManual     = MakeRadio("Manual (choose up to 4 pairings)",         new Point(20, 83),  false);

            // Round-robin engine list
            var lblPool = MakeLbl("Select engines for the pool:", new Point(20, 112));
            _lstRREngines = new CheckedListBox
            {
                Location      = new Point(20, 132),
                Size          = new Size(280, 130),
                BackColor     = Color.FromArgb(40, 40, 40),
                ForeColor     = Color.FromArgb(220, 220, 220),
                BorderStyle   = BorderStyle.FixedSingle,
                CheckOnClick  = true
            };

            // Manual pairing panel
            _pnlManualPairs = new Panel
            {
                Location = new Point(20, 132),
                Size     = new Size(420, 130),
                Visible  = false
            };

            var resolver = new EnginePathResolver(_config);
            string[] available = resolver.GetAvailableEngines();

            for (int i = 0; i < 4; i++)
            {
                int row = i;
                int y   = row * 30;
                var lbl = MakeLbl($"Match {row + 1}:", new Point(0, y + 5));
                lbl.Width = 60;
                _cmbWhite[row] = MakeEngineCombo(available, new Point(65, y));
                _lblVs[row]    = MakeLbl("vs", new Point(215, y + 5));
                _lblVs[row].Width = 20;
                _cmbBlack[row] = MakeEngineCombo(available, new Point(240, y));
                _pnlManualPairs.Controls.AddRange(
                    new Control[] { lbl, _cmbWhite[row], _lblVs[row], _cmbBlack[row] });
            }

            // Games per match
            var lblGames = MakeLbl("Games per match:", new Point(330, 112));
            _numGames = new NumericUpDown
            {
                Location = new Point(330, 132),
                Size     = new Size(60, 26),
                Minimum  = 1, Maximum = 20, Value = 2,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(220, 220, 220)
            };

            _chkAdjudicate = new CheckBox
            {
                Text     = "Auto-adjudicate",
                Checked  = true,
                Location = new Point(330, 165),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200)
            };

            // Time control
            var lblTC = MakeLbl("Time Control:", new Point(20, 282));
            lblTC.Font = new Font("Courier New", 9f, FontStyle.Bold);

            _rbDepth    = MakeRadio("Depth",      new Point(20,  302), true);
            _rbMovetime = MakeRadio("Time/move",  new Point(100, 302), false);
            _rbClock    = MakeRadio("Clock",      new Point(200, 302), false);

            _numDepth = new NumericUpDown
            {
                Location = new Point(20, 326),
                Size     = new Size(70, 26),
                Minimum  = 1, Maximum = 40, Value = 12,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            var lblDepthUnit = MakeLbl("plies", new Point(96, 330));

            _numMovetime = new NumericUpDown
            {
                Location = new Point(20, 326),
                Size     = new Size(70, 26),
                Minimum  = 100, Maximum = 60000, Value = 1000, Increment = 100,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(220, 220, 220),
                Visible   = false
            };
            var lblMtUnit = MakeLbl("ms", new Point(96, 330));

            _numTotal = new NumericUpDown
            {
                Location = new Point(20, 326),
                Size     = new Size(70, 26),
                Minimum  = 10, Maximum = 3600, Value = 60,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(220, 220, 220),
                Visible   = false
            };
            var lblTotalUnit = MakeLbl("s  +", new Point(96, 330));
            _numInc = new NumericUpDown
            {
                Location = new Point(130, 326),
                Size     = new Size(60, 26),
                Minimum  = 0, Maximum = 300, Value = 0,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(220, 220, 220),
                Visible   = false
            };
            var lblIncUnit = MakeLbl("s/move", new Point(196, 330));

            void UpdateTCControls()
            {
                _numDepth.Visible    = _rbDepth.Checked;
                lblDepthUnit.Visible = _rbDepth.Checked;
                _numMovetime.Visible = _rbMovetime.Checked;
                lblMtUnit.Visible    = _rbMovetime.Checked;
                _numTotal.Visible    = _rbClock.Checked;
                lblTotalUnit.Visible = _rbClock.Checked;
                _numInc.Visible      = _rbClock.Checked;
                lblIncUnit.Visible   = _rbClock.Checked;
            }
            _rbDepth.CheckedChanged    += (_, _) => UpdateTCControls();
            _rbMovetime.CheckedChanged += (_, _) => UpdateTCControls();
            _rbClock.CheckedChanged    += (_, _) => UpdateTCControls();
            UpdateTCControls();

            // Pairing mode toggle
            _rbRoundRobin.CheckedChanged += (_, _) =>
            {
                _lstRREngines.Visible   = _rbRoundRobin.Checked;
                _pnlManualPairs.Visible = _rbManual.Checked;
            };

            _btnStart = new Button
            {
                Text      = "▶  Start Tournament",
                Location  = new Point(20, 426),
                Size      = new Size(190, 34),
                BackColor = Color.FromArgb(60, 120, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 10f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += BtnStart_Click;

            _chkUseOpeningBook = new CheckBox
            {
                Text      = "Use opening book",
                Location  = new Point(20, 358),
                AutoSize  = true,
                ForeColor = Color.FromArgb(200, 200, 200)
            };

            // Wrap book radios in their own Panel so they don't compete with
            // _rbRoundRobin / _rbManual (WinForms groups all RadioButtons in the
            // same container into one mutual-exclusion set).
            _rbBookRandom = new RadioButton
            {
                Text      = "Random",
                Location  = new Point(0, 2),
                AutoSize  = true,
                Checked   = true,
                ForeColor = Color.FromArgb(200, 200, 200)
            };
            _rbBookChoose = new RadioButton
            {
                Text      = "Choose...",
                Location  = new Point(84, 2),
                AutoSize  = true,
                ForeColor = Color.FromArgb(200, 200, 200)
            };
            _pnlBookMode = new Panel
            {
                Location  = new Point(36, 380),
                Size      = new Size(260, 24),
                Visible   = false,
                BackColor = Color.Transparent
            };
            _pnlBookMode.Controls.Add(_rbBookRandom);
            _pnlBookMode.Controls.Add(_rbBookChoose);

            _lblChosenOpening = new Label
            {
                Text      = "",
                Location  = new Point(36, 406),
                Size      = new Size(340, 18),
                Visible   = false,
                ForeColor = Color.FromArgb(140, 210, 140)
            };

            _chkUseOpeningBook.CheckedChanged += (_, _) =>
            {
                bool on = _chkUseOpeningBook.Checked;
                _pnlBookMode.Visible = on;
                if (!on) { _lblChosenOpening.Visible = false; _bookOpening = null; }
            };
            _rbBookChoose.CheckedChanged += (_, _) =>
            {
                if (!_rbBookChoose.Checked) { _lblChosenOpening.Visible = false; return; }
                SelectTournamentOpening();
            };

            _pnlSetup.Controls.AddRange(new Control[]
            {
                title, _rbRoundRobin, _rbManual,
                lblPool, _lstRREngines, _pnlManualPairs,
                lblGames, _numGames, _chkAdjudicate,
                lblTC, _rbDepth, _rbMovetime, _rbClock,
                _numDepth, lblDepthUnit, _numMovetime, lblMtUnit,
                _numTotal, lblTotalUnit, _numInc, lblIncUnit,
                _chkUseOpeningBook, _pnlBookMode,
                _lblChosenOpening, _btnStart
            });

            // Populate engine lists
            foreach (var eng in available)
            {
                _config.EngineProfiles.TryGetValue(eng, out var prof);
                string label = !string.IsNullOrEmpty(prof?.DisplayName)
                    ? prof.DisplayName : Path.GetFileNameWithoutExtension(eng);
                if (prof?.Elo > 0) label += $" ({prof.Elo})";
                _lstRREngines.Items.Add(label);
            }
            _engines = available.Select(f =>
            {
                _config.EngineProfiles.TryGetValue(f, out var p);
                string dn = !string.IsNullOrEmpty(p?.DisplayName)
                    ? p.DisplayName : Path.GetFileNameWithoutExtension(f);
                return new TournamentEngineEntry { FileName = f, DisplayName = dn, Elo = p?.Elo ?? 0 };
            }).ToArray();

            // ── Match panel ─────────────────────────────────────────────────
            _pnlMatch = new Panel { Dock = DockStyle.Fill, Visible = false };

            _lblTournamentTitle = new Label
            {
                Text      = "Tournament",
                Font      = new Font("Courier New", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 180, 80),
                AutoSize  = false,
                Height    = 28,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnStop = new Button
            {
                Text      = "⏹ Stop",
                Size      = new Size(80, 26),
                BackColor = Color.FromArgb(120, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 9f),
                Cursor    = Cursors.Hand
            };
            _btnStop.FlatAppearance.BorderSize = 0;
            _btnStop.Enabled = false;
            _btnStop.Click += (_, _) => StopTournament();

            _pnlBoards = new Panel { BackColor = Color.FromArgb(20, 20, 20) };

            // Standings list
            _pnlStandings = new Panel
            {
                Height    = 140,
                BackColor = Color.FromArgb(20, 20, 20),
                Dock      = DockStyle.Bottom,
                Padding   = new Padding(8, 4, 8, 4)
            };

            var lblStand = new Label
            {
                Text      = "Standings",
                Font      = new Font("Courier New", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize  = true,
                Location  = new Point(8, 4)
            };

            _lvStandings = new ListView
            {
                View           = View.Details,
                FullRowSelect  = true,
                GridLines      = false,
                BackColor      = Color.FromArgb(30, 30, 30),
                ForeColor      = Color.FromArgb(210, 210, 210),
                BorderStyle    = BorderStyle.None,
                HeaderStyle    = ColumnHeaderStyle.Nonclickable,
                Font           = new Font("Courier New", 8.5f),
                Location       = new Point(8, 24),
                Size           = new Size(600, 106)
            };
            _lvStandings.Columns.Add("Engine", 220);
            _lvStandings.Columns.Add("Score",   60);
            _lvStandings.Columns.Add("W",        40);
            _lvStandings.Columns.Add("D",        40);
            _lvStandings.Columns.Add("L",        40);
            _lvStandings.Columns.Add("Games",    60);

            _pnlStandings.Controls.Add(lblStand);
            _pnlStandings.Controls.Add(_lvStandings);

            // Four distinct board color schemes — one per concurrent slot
            (Color light, Color dark)[] boardColors =
            {
                (ColorTranslator.FromHtml("#F0D9B5"), ColorTranslator.FromHtml("#B58863")), // Classic brown
                (ColorTranslator.FromHtml("#FFFCE6"), ColorTranslator.FromHtml("#769656")), // Forest green
                (ColorTranslator.FromHtml("#DEE3E6"), ColorTranslator.FromHtml("#8CA2AD")), // Slate blue
                (ColorTranslator.FromHtml("#F5DEB3"), ColorTranslator.FromHtml("#AE5859")), // Warm red
            };

            // Create 4 board panels
            for (int i = 0; i < MaxConcurrent; i++)
            {
                var p = new MatchBoardPanel
                {
                    Visible = true,
                    BackColor = Color.FromArgb(30, 30, 30)
                };
                p.SetBoardAppearance(boardColors[i].light, boardColors[i].dark);
                p.ExpandRequested += OnExpandRequested;
                var captured = p;
                p.SeriesEnded += (s1, s2) => OnSeriesEnded(captured, s1, s2);
                _panels[i] = p;
                _pnlBoards.Controls.Add(p);
            }

            _pnlMatch.Controls.Add(_pnlBoards);
            _pnlMatch.Controls.Add(_pnlStandings);
            _pnlMatch.Controls.Add(_lblTournamentTitle);
            _pnlMatch.Controls.Add(_btnStop);
            _pnlMatch.Resize += (_, _) => ArrangeMatchPanel();

            Controls.Add(_pnlSetup);
            Controls.Add(_pnlMatch);

            Shown    += (_, _) => { if (available.Length >= 2) { _lstRREngines.SetItemChecked(0, true); _lstRREngines.SetItemChecked(1, true); } };
            FormClosing += TournamentForm_Closing;
        }

        // ── Start ────────────────────────────────────────────────────────────
        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            var pairings = BuildPairings();
            if (pairings.Count == 0)
            {
                MessageBox.Show(
                    "Select at least 2 engines (Round-robin) or configure at least 1 pairing (Manual).",
                    "No pairings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tc = BuildTimeControl();
            int games = (int)_numGames.Value;
            bool adj  = _chkAdjudicate.Checked;

            // Init export state
            _allPgnGames.Clear();
            _roundCounter    = 1;
            _totalMatchCount = pairings.Count;
            _tcDescription   = BuildTcDescription(tc);
            _tcPgnTag        = BuildTcPgnTag(tc);

            // Build standings skeleton
            _standings.Clear();
            var allEngines = pairings
                .SelectMany(p => new[] { p.Engine1, p.Engine2 })
                .DistinctBy(e => e.FileName)
                .ToList();
            foreach (var eng in allEngines)
                _standings[eng.FileName] = new TournamentStanding { Engine = eng };

            // Queue pairings
            _queue = new Queue<TournamentPairing>(pairings);

            // Load opening book if requested
            _bookService?.Dispose();
            _bookService = null;
            if (_chkUseOpeningBook.Checked)
            {
                string booksPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    _config.OpeningBooksFolder);
                var svc = new PolyglotBookService();
                svc.LoadBooksFromFolder(booksPath);
                if (svc.IsLoaded) _bookService = svc;
                else svc.Dispose();
            }

            // Switch to match panel
            _pnlSetup.Visible  = false;
            _pnlMatch.Visible  = true;
            _running           = true;
            _btnStop.Enabled   = true;
            Size               = new Size(1100, 720);
            MinimumSize        = new Size(800, 560);

            int matchCount   = pairings.Count;
            int engineCount  = allEngines.Count;
            _lblTournamentTitle.Text = engineCount > 2
                ? $"Tournament — {engineCount} engines · {matchCount} match{(matchCount == 1 ? "" : "es")} · {tc}"
                : $"Tournament — {matchCount} match{(matchCount == 1 ? "" : "es")} · {tc}";

            // Init annotator
            if (!string.IsNullOrEmpty(_config.SelectedEngine))
            {
                _annotator = new ChessEngineService(_config);
                string annotPath = Path.Combine(_engineBasePath, _config.SelectedEngine);
                await _annotator.InitializeAsync(annotPath);
            }

            ArrangeMatchPanel();
            UpdateStandingsView();

            // Start up to MaxConcurrent boards
            for (int i = 0; i < MaxConcurrent; i++)
                AssignNextPairing(_panels[i], tc, games, adj);
        }

        // ── Stop ─────────────────────────────────────────────────────────────
        private void StopTournament()
        {
            _running = false;
            _btnStop.Enabled = false;
            foreach (var p in _panels) p.StopSeries();
            _queue.Clear();
            _annotator?.Dispose();
            _annotator = null;
            _bookService?.Dispose();
            _bookService = null;
        }

        // ── Pairing assignment ────────────────────────────────────────────────
        private void AssignNextPairing(MatchBoardPanel panel,
                                       EngineMatchTimeControl tc,
                                       int games, bool adj)
        {
            if (!_running || _queue.Count == 0) return;
            var pairing = _queue.Dequeue();
            bool bookActive = _chkUseOpeningBook.Checked &&
                              (_bookService?.IsLoaded == true ||
                               (_rbBookChoose.Checked && _bookOpening != null));
            string? openingFen = bookActive ? GenerateOpeningFen() : null;
            panel.StartSeries(
                pairing.Engine1.FileName, pairing.Engine2.FileName,
                pairing.Engine1.Label,   pairing.Engine2.Label,
                tc, _engineBasePath, _config, games, adj, openingFen);
        }

        private void SelectTournamentOpening()
        {
            string booksFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
            var entries = EcoBookService.LoadAll(booksFolder);
            if (entries.Count == 0)
            {
                _rbBookRandom.Checked = true;
                return;
            }

            using var dlg = new OpeningExplorerDialog(entries, pgn =>
            {
                var m = System.Text.RegularExpressions.Regex.Match(
                    pgn, @"\[Opening ""([^""]+)""\]");
                if (!m.Success) return;
                string tag  = m.Groups[1].Value;
                int    dash = tag.IndexOf(" — ", StringComparison.Ordinal);
                if (dash < 0) return;
                string eco  = tag[..dash];
                string name = tag[(dash + 3)..];
                _bookOpening = entries.FirstOrDefault(e => e.Eco == eco && e.Name == name)
                            ?? entries.FirstOrDefault(e => e.Eco == eco);
                if (_bookOpening != null)
                {
                    _lblChosenOpening.Text    = $"{_bookOpening.Eco}  {_bookOpening.Name}";
                    _lblChosenOpening.Visible = true;
                }
            }, ThemeService.IsDarkTheme(_config.Theme));

            dlg.ShowDialog(this);
            if (_bookOpening == null) _rbBookRandom.Checked = true;
        }

        private string GenerateOpeningFen()
        {
            // Choose mode: replay ECO opening moves to produce a fixed starting position
            if (_rbBookChoose.Checked && _bookOpening != null)
                return ChessNotationService.GetFenAfterSanMoves(_bookOpening.Moves);

            if (_bookService?.IsLoaded != true) return "";

            const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            const int    BookPlies = 6;

            var    board       = ChessBoard.FromFEN(StartFen);
            string castling    = "KQkq";
            string ep          = "-";
            bool   whiteToMove = true;
            string fen         = StartFen;
            var    rng         = new Random();

            for (int i = 0; i < BookPlies; i++)
            {
                var moves = _bookService.GetBookMovesForPosition(fen);
                if (moves.Count == 0) break;

                int totalWeight = moves.Sum(m => m.Weight);
                if (totalWeight <= 0) break;
                int pick = rng.Next(totalWeight);
                int accum = 0;
                PolyglotBookService.PolyglotMove? chosen = null;
                foreach (var m in moves)
                {
                    accum += m.Weight;
                    if (pick < accum) { chosen = m; break; }
                }
                chosen ??= moves[0];

                ChessRulesService.ApplyUciMove(board, chosen.UciMove, ref castling, ref ep);
                whiteToMove = !whiteToMove;
                fen = $"{board.ToFEN()} {(whiteToMove ? "w" : "b")} {castling} {ep} 0 1";
            }

            return fen;
        }

        // ── Series ended callback ─────────────────────────────────────────────
        private void OnSeriesEnded(MatchBoardPanel panel, TournamentStanding s1, TournamentStanding s2)
        {
            if (InvokeRequired) { Invoke(() => OnSeriesEnded(panel, s1, s2)); return; }

            UpdateAggregate(s1);
            UpdateAggregate(s2);
            UpdateStandingsView();

            if (_focusedPanel == panel)
            {
                panel.AnnotatorEngine = null;
                _focusedPanel = null;
            }

            // Collect PGN before this panel is reassigned to a new series
            int gamesJustPlayed = panel.GamesPlayed;
            string pgn = panel.BuildSeriesPgn(
                "Chessdroid Tournament",
                DateTime.Now.ToString("yyyy.MM.dd"),
                _tcPgnTag,
                _roundCounter);
            if (pgn.Length > 0) _allPgnGames.Add(pgn);
            _roundCounter += gamesJustPlayed;

            if (_running && _queue.Count > 0)
            {
                var tc   = BuildTimeControl();
                int gms  = (int)_numGames.Value;
                bool adj = _chkAdjudicate.Checked;
                AssignNextPairing(panel, tc, gms, adj);
            }
            else if (_queue.Count == 0 && _panels.All(p => !p.IsRunning))
            {
                OnTournamentComplete();
            }
        }

        private void UpdateAggregate(TournamentStanding delta)
        {
            string key = delta.Engine.FileName;
            if (!_standings.TryGetValue(key, out var s))
            {
                s = new TournamentStanding { Engine = delta.Engine };
                _standings[key] = s;
            }
            s.Points += delta.Points;
            s.Wins   += delta.Wins;
            s.Draws  += delta.Draws;
            s.Losses += delta.Losses;
            s.Played += delta.Played;
        }

        private void OnTournamentComplete()
        {
            _running = false;
            _btnStop.Enabled = false;
            _lblTournamentTitle.Text = _lblTournamentTitle.Text.Replace("Tournament —", "Tournament — Complete ✓ —");
            string saved = SaveResultsToFile();
            if (!string.IsNullOrEmpty(saved))
                _lblTournamentTitle.Text += $"  ·  Saved: {saved}";
        }

        // ── Expand / focus ───────────────────────────────────────────────────
        private void OnExpandRequested(object? sender, EventArgs e)
        {
            if (sender is not MatchBoardPanel clicked) return;

            if (_focusedPanel == clicked)
            {
                // Click focused board → return to overview
                UnfocusAll();
            }
            else
            {
                FocusPanel(clicked);
            }
        }

        private void FocusPanel(MatchBoardPanel target)
        {
            // Detach annotator from previous focus
            if (_focusedPanel != null)
            {
                _focusedPanel.AnnotatorEngine = null;
                _focusedPanel.IsExpanded = false;
            }

            _focusedPanel = target;
            target.IsExpanded = true;

            // Attach annotator
            if (_annotator?.State == EngineState.Ready)
                target.AnnotatorEngine = _annotator;

            ArrangeBoards();
        }

        private void UnfocusAll()
        {
            if (_focusedPanel != null)
            {
                _focusedPanel.AnnotatorEngine = null;
                _focusedPanel.IsExpanded = false;
                _focusedPanel = null;
            }
            ArrangeBoards();
        }

        // ── Layout ───────────────────────────────────────────────────────────
        private void ArrangeMatchPanel()
        {
            int w = _pnlMatch.ClientSize.Width;
            int h = _pnlMatch.ClientSize.Height;
            if (w < 10 || h < 10) return;

            const int HeaderH   = 36;
            const int StandH    = 140;
            int       boardsH   = h - HeaderH - StandH;

            _lblTournamentTitle.SetBounds(12, 4, w - 100, 28);
            _btnStop.SetBounds(w - 88, 5, 80, 26);
            _pnlBoards.SetBounds(0, HeaderH, w, boardsH);
            _pnlStandings.SetBounds(0, HeaderH + boardsH, w, StandH);
            _lvStandings.Size = new Size(w - 16, StandH - 30);

            ArrangeBoards();
        }

        private void ArrangeBoards()
        {
            int w = _pnlBoards.Width;
            int h = _pnlBoards.Height;
            if (w < 10 || h < 10) return;

            const int Gap = 3;

            if (_focusedPanel != null)
            {
                // Focused mode: expanded left (~65%), three mini stacked right (~35%)
                int expandW = (int)(w * 0.65) - Gap;
                int miniW   = w - expandW - Gap * 2;
                int miniH   = Math.Max((h - Gap * 2) / 3, 40);

                _focusedPanel.SetBounds(0, 0, expandW, h);

                int slot = 0;
                foreach (var p in _panels)
                {
                    if (p == _focusedPanel) continue;
                    int y = slot * (miniH + Gap);
                    p.SetBounds(expandW + Gap, y, miniW, miniH);
                    slot++;
                }
            }
            else
            {
                // Overview: 2×2 grid
                int cellW = (w - Gap) / 2;
                int cellH = (h - Gap) / 2;
                for (int i = 0; i < MaxConcurrent; i++)
                {
                    int col = i % 2;
                    int row = i / 2;
                    _panels[i].SetBounds(
                        col * (cellW + Gap),
                        row * (cellH + Gap),
                        cellW, cellH);
                }
            }
        }

        private void UpdateStandingsView()
        {
            _lvStandings.BeginUpdate();
            _lvStandings.Items.Clear();
            foreach (var s in _standings.Values.OrderByDescending(s => s.Points))
            {
                var item = new ListViewItem(s.Engine.Label);
                item.SubItems.Add(s.PointStr);
                item.SubItems.Add(s.Wins.ToString());
                item.SubItems.Add(s.Draws.ToString());
                item.SubItems.Add(s.Losses.ToString());
                item.SubItems.Add(s.Played.ToString());
                _lvStandings.Items.Add(item);
            }
            _lvStandings.EndUpdate();
        }

        // ── Pairing builders ─────────────────────────────────────────────────
        private List<TournamentPairing> BuildPairings()
        {
            if (_rbRoundRobin.Checked)
                return BuildRoundRobin();
            return BuildManual();
        }

        private List<TournamentPairing> BuildRoundRobin()
        {
            var selected = _lstRREngines.CheckedIndices
                .Cast<int>()
                .Where(i => i < _engines.Length)
                .Select(i => _engines[i])
                .ToList();

            if (selected.Count < 2) return new();

            var result = new List<TournamentPairing>();
            for (int i = 0; i < selected.Count; i++)
                for (int j = i + 1; j < selected.Count; j++)
                    result.Add(new TournamentPairing
                    {
                        Engine1 = selected[i],
                        Engine2 = selected[j]
                    });
            return result;
        }

        private List<TournamentPairing> BuildManual()
        {
            var result = new List<TournamentPairing>();
            for (int i = 0; i < 4; i++)
            {
                if (_cmbWhite[i].SelectedIndex < 0 || _cmbBlack[i].SelectedIndex < 0) continue;
                int wi = _cmbWhite[i].SelectedIndex;
                int bi = _cmbBlack[i].SelectedIndex;
                if (wi >= _engines.Length || bi >= _engines.Length) continue;
                if (wi == bi) continue;
                result.Add(new TournamentPairing
                {
                    Engine1 = _engines[wi],
                    Engine2 = _engines[bi]
                });
            }
            return result;
        }

        private EngineMatchTimeControl BuildTimeControl()
        {
            var tc = new EngineMatchTimeControl();
            if (_rbDepth.Checked)
            {
                tc.Type  = TimeControlType.FixedDepth;
                tc.Depth = (int)_numDepth.Value;
            }
            else if (_rbMovetime.Checked)
            {
                tc.Type       = TimeControlType.FixedTimePerMove;
                tc.MoveTimeMs = (int)_numMovetime.Value;
            }
            else
            {
                tc.Type        = TimeControlType.TotalPlusIncrement;
                tc.TotalTimeMs = (int)_numTotal.Value * 1000;
                tc.IncrementMs = (int)_numInc.Value * 1000;
            }
            return tc;
        }

        // ── Results export ────────────────────────────────────────────────────
        private string SaveResultsToFile()
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName  = $"tournament_{timestamp}.txt";
                string path      = Path.Combine(Application.StartupPath, fileName);

                var sb = new StringBuilder();

                // ── Header ───────────────────────────────────────────────────
                sb.AppendLine("Chessdroid Tournament Results");
                sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm}  |  {_standings.Count} engines  |  {_totalMatchCount} matches  |  {_tcDescription}");
                sb.AppendLine();

                // ── Standings ─────────────────────────────────────────────────
                const string fmt = "{0,-4}  {1,-35}  {2,6}  {3,4}  {4,4}  {5,4}  {6,5}";
                sb.AppendLine(string.Format(fmt, "Rank", "Engine", "Score", "W", "D", "L", "Games"));
                sb.AppendLine(new string('-', 68));
                int rank = 1;
                foreach (var s in _standings.Values
                    .OrderByDescending(s => s.Points)
                    .ThenByDescending(s => s.Wins))
                {
                    sb.AppendLine(string.Format(fmt,
                        rank++, s.Engine.Label, s.PointStr,
                        s.Wins, s.Draws, s.Losses, s.Played));
                }

                // ── Games ─────────────────────────────────────────────────────
                if (_allPgnGames.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(new string('=', 68));
                    sb.AppendLine($"Games (PGN) — {_roundCounter - 1} total");
                    sb.AppendLine(new string('=', 68));
                    sb.AppendLine();
                    foreach (var g in _allPgnGames)
                        sb.Append(g);
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                return fileName;
            }
            catch
            {
                return "";
            }
        }

        private static string BuildTcDescription(EngineMatchTimeControl tc) => tc.Type switch
        {
            TimeControlType.FixedDepth        => $"Depth {tc.Depth}",
            TimeControlType.FixedTimePerMove  => $"{tc.MoveTimeMs}ms/move",
            TimeControlType.TotalPlusIncrement => $"{tc.TotalTimeMs / 1000}+{tc.IncrementMs / 1000}s",
            _                                  => "?"
        };

        private static string BuildTcPgnTag(EngineMatchTimeControl tc) => tc.Type switch
        {
            TimeControlType.FixedDepth         => $"depth:{tc.Depth}",
            TimeControlType.FixedTimePerMove   => $"movetime:{tc.MoveTimeMs}",
            TimeControlType.TotalPlusIncrement => $"{tc.TotalTimeMs / 1000}+{tc.IncrementMs / 1000}",
            _                                  => "?"
        };

        // ── Cleanup ───────────────────────────────────────────────────────────
        private void TournamentForm_Closing(object? sender, FormClosingEventArgs e)
        {
            StopTournament();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _annotator?.Dispose();
                _bookService?.Dispose();
                foreach (var p in _panels) p.Dispose();
            }
            base.Dispose(disposing);
        }

        // ── Factory helpers ───────────────────────────────────────────────────
        private static Label MakeLbl(string text, Point loc)
            => new Label
            {
                Text      = text,
                Location  = loc,
                AutoSize  = true,
                ForeColor = Color.FromArgb(200, 200, 200)
            };

        private static RadioButton MakeRadio(string text, Point loc, bool chk)
            => new RadioButton
            {
                Text      = text,
                Location  = loc,
                AutoSize  = true,
                Checked   = chk,
                ForeColor = Color.FromArgb(200, 200, 200)
            };

        private static ComboBox MakeEngineCombo(string[] engines, Point loc)
        {
            var c = new ComboBox
            {
                Location      = loc,
                Size          = new Size(145, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor     = Color.FromArgb(40, 40, 40),
                ForeColor     = Color.FromArgb(220, 220, 220),
                FlatStyle     = FlatStyle.Flat
            };
            foreach (var e in engines)
                c.Items.Add(Path.GetFileNameWithoutExtension(e));
            return c;
        }
    }
}
