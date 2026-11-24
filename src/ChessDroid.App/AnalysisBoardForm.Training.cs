using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;
using ChessDroid.Views;

namespace ChessDroid
{
    public partial class AnalysisBoardForm : Form
    {
        #region Square Training

        private const int TRAINING_WRONG_MS   = 280;
        private const int TRAINING_CORRECT_MS = 380;
        private const string TRAINING_EMPTY_FEN = "8/8/8/8/8/8/8/8 w - - 0 1";
        private static readonly Random _trainingRng = new();

        private static readonly Color TrainingHintColor  = Color.FromArgb(120, 255, 235, 80);
        private static readonly Color TrainingErrorColor = Color.FromArgb(175, 215, 50, 50);
        private static readonly Color TrainingOkColor    = Color.FromArgb(175, 50, 205, 50);

        private bool _trainingUiVisible;
        internal bool _trainingGameActive;
        private string? _trainingPreFen;
        private bool _trainingPreFlipped;
        private int  _trainingTargetRow, _trainingTargetCol;
        private int  _trainingCorrect, _trainingWrong;
        private int  _trainingQuestions;   // set from numericupdown at round start
        private int  _trainingTimeLimitSec; // 0 = no limit
        internal bool _trainingAwaitingNext;
        private DateTime _trainingRoundStart;
        private bool _trainingInWrongFlash, _trainingInCorrectFlash;
        private int  _trainingFlashMs;
        private int  _trainingCorrectRow, _trainingCorrectCol;

        internal System.Windows.Forms.Timer? _trainingClockTimer;
        internal System.Windows.Forms.Timer? _trainingFlashTimer;

        internal TrainingPanel _trainingPanel = null!;

        // Which training sub-mode (Square/Opening/Puzzle/Vision/Drill) is currently selected.
        internal TrainingSubMode _trainingSubMode = TrainingSubMode.Square;

        // Opening Training state
        private bool _openingRecreatePhase;
        private bool _openingRecreateLocked; // true during flash + restore delay

        private OpeningEntry? _selectedTrainingOpening;
        private int _selectedWatches;
        private readonly List<string> _openingUciMoves = new();
        private readonly List<string> _openingFens = new();
        private int _openingPlayIndex;
        private int _openingWatchesLeft;
        private int _openingRecreateIndex;
        private readonly Dictionary<int, int> _openingPosMistakes = new(); // posIndex → attempt count
        internal System.Windows.Forms.Timer? _openingAutoplayTimer;

        // Opening Training UI refs
        private List<EndgameChapter> _drillChapters = new();
        private string?  _drillsFolder;
        private int     _openingHintsUsed;
        private int     _openingSessionRuns;
        private int     _openingSessionPerfect;

        // Puzzle Training state
        private bool   _puzzleActive;
        private bool   _puzzleLocked;
        internal LichessPuzzle? _currentPuzzle;
        private int    _puzzleMoveIndex;   // index into Moves[] the player must play next
        private readonly List<LichessPuzzle> _puzzleQueue = new();
        private int    _puzzlesClean;      // solved with zero wrong moves and zero hints
        private int    _puzzlesStruggled;  // solved but used hints or wrong moves
        private int    _puzzlesAttempted;
        private bool   _wrongThisPuzzle;   // any wrong move or hint used this puzzle
        private DateTime _puzzleStartTime; // when player first got control (after trigger)
        private string?  _puzzlesFolder;
        internal string?  _puzzleThemeFilter;

        // Puzzle sub-mode: "training" | "rush" | "gauntlet"
        internal string _puzzleSubMode = "training";
        internal string? _puzzleOpeningFilter;
        private int     _puzzleRatingMin;
        private int     _puzzleRatingMax = int.MaxValue;
        private int     _rushDurationSeconds = 180;
        private System.Windows.Forms.Timer? _puzzleRushTimer;
        private int     _rushSecondsRemaining;
        private int     _gauntletStreak;
        internal int     _gauntletBestStreak;
        private int     _puzzleStreak;
        internal int     _puzzleStreakBest;

        // Puzzle Training UI refs
        private int     _puzzleHintsUsed;

        // Board Vision state
        // Endgame Drills state
        private int     _visionCorrect;
        private int     _visionWrong;
        private int     _visionStreak;
        private int     _visionBestStreak;
        private DateTime _visionStartTime;
        private string  _visionCurrentSquare = "a1";

        // Board Vision UI refs

        // Board Vision sub-mode
        private string  _visionSubMode = "training";
        private int     _visionTimedSeconds = 5;
        internal System.Windows.Forms.Timer? _visionQuestionTimer;
        private int     _visionSecondsRemaining;
        private int     _visionLives;
        private int     _visionGlobalDurationSeconds = 180;
        private int     _visionGlobalSecondsRemaining;
        internal System.Windows.Forms.Timer? _visionGlobalTimer;


        // ── BtnTraining_Click (replaces the old ShowDialog version) ──────────

        private void BtnTraining_Click(object? sender, EventArgs e)
        {
            if (_trainingUiVisible)
            {
                bool puzzleTrainingActive = _trainingPanel._pnlPuzzleGame?.Visible == true
                    && _puzzleSubMode == "training"
                    && (_puzzlesClean + _puzzlesStruggled) > 0
                    && _puzzleSubMode != "daily";
                if (puzzleTrainingActive)
                    PuzzleTrainingShowResults();
                else
                    StopTraining();
            }
            else
            {
                // Mutually exclusive with Match panel
                if (_matchPanelActive)
                {
                    _matchPanelActive = false;
                    grpEngineMatch.Visible = false;
                    HighlightButton(btnMatch, false);
                }
                StartTrainingUI();
            }
        }

        private void PuzzleTrainingShowResults()
        {
            _puzzleActive = false;
            _puzzleLocked = false;
            boardControl.ClearTrainingHighlight();
            boardControl.ClearSelection();
            if (_trainingPanel._btnPuzzleNext    != null) _trainingPanel._btnPuzzleNext.Visible    = false;
            if (_trainingPanel._btnPuzzleAnalyze != null) _trainingPanel._btnPuzzleAnalyze.Visible = false;

            int total = _puzzlesClean + _puzzlesStruggled;
            int acc   = total > 0 ? _puzzlesClean * 100 / total : 0;

            if (config != null && total > 0)
            {
                config.PuzzleTrainingTotalAttempted += total;
                config.PuzzleTrainingTotalClean     += _puzzlesClean;
                config.Save();
            }

            if (_trainingPanel._lblTrainingFinalScore != null)
                _trainingPanel._lblTrainingFinalScore.Text = $"Solved: {total}   ·   {acc}% clean   ·   {_puzzlesClean} perfect";
            if (_trainingPanel._lblTrainingPB != null)
                _trainingPanel._lblTrainingPB.Text = _puzzleStreakBest > 0 ? $"Best streak: {_puzzleStreakBest}" : "";
            if (_trainingPanel._lblOpMissedMoves != null) _trainingPanel._lblOpMissedMoves.Visible = false;

            RestoreBoardAfterTraining();

            _trainingPanel._pnlPuzzleGame!.Visible     = false;
            _trainingPanel._pnlTrainingResult!.Visible = true;
        }

        private void StartTrainingUI()
        {
            _puzzlesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Puzzles");
            if (_trainingPanel._btnPuzzleMode != null)
                _trainingPanel._btnPuzzleMode.Visible = LichessPuzzleService.HasPuzzles(_puzzlesFolder);

            _drillsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Drills");
            if (EndgameDrillService.HasDrills(_drillsFolder))
            {
                _drillChapters = EndgameDrillService.LoadFromFolder(_drillsFolder);
                if (_trainingPanel._btnDrillMode != null) _trainingPanel._btnDrillMode.Visible = true;
                PopulateDrillStudies();
            }

            autoAnalysisCts?.Cancel();
            lblStatus.Text = "";

            // Analysis-mode arrows (engine best-move / book moves) belong to Analysis mode's
            // display, not Training's — without this, arrows from whatever was on the board
            // before Training opened stayed visible the whole time the panel was open (e.g.
            // browsing Drills chapters), since TriggerAutoAnalysis's own new suppression below
            // only stops FUTURE analysis from drawing MORE arrows, not the ones already drawn.
            boardControl.ClearEngineArrows();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;

            _trainingUiVisible = true;
            analysisOutput.Visible = false;
            grpEngineMatch.Visible = false;
            lblAnalysis.Visible = false;
            evalBar.Visible = false;
            if (_evalGraph != null) _evalGraph.Visible = false;
            _trainingPanel!.Visible = true;
            _trainingPanel.BringToFront();
            boardControl.InteractionEnabled = false;
            TrainingShowStartPanel();

            btnTraining.Text = "⏹";
            toolTip.SetToolTip(btnTraining, "Stop Training");
            SetTrainingButtonsEnabled(false);
        }

        internal void StopTraining()
        {
            // Drill Watch Engines runs via matchService independently of the training
            // timers below — stopping it here (same path as the "Stop Watch" button and
            // TrainingStartRound's guard) is required or the engines keep playing after
            // the training panel closes. MatchService_OnMatchEnded does the rest of the
            // cleanup (_drillWatchActive, matchRunning) once the match actually stops.
            if (_drillWatchActive) { matchService?.StopMatch(); return; }

            // Drill vs Bot deliberately keeps the training panel visible while the bot game
            // is live (see DrillPracticeAsync's "no StopTraining — we keep the training panel
            // visible" comment). Letting StopTraining run anyway — reachable via BtnMatch_Click
            // or BtnTraining_Click, both of which call it whenever _trainingUiVisible is true —
            // would hide the drill UI and re-enable every button SetDrillControlsEnabled just
            // locked (New Game, mode switchers, etc.) while the bot engine keeps playing
            // underneath, unseen. Block instead; StopBotMode() (the drill's own Stop Practice
            // button, or the general bot toggle) is the only way out of a running drill.
            if (_drillBotActive) { lblStatus.Text = "Stop the drill first"; return; }

            _trainingClockTimer?.Stop();
            _trainingFlashTimer?.Stop();
            _openingAutoplayTimer?.Stop();
            _puzzleRushTimer?.Stop();
            _visionQuestionTimer?.Stop();
            _visionGlobalTimer?.Stop();
            _openingRecreatePhase = false;
            _puzzleActive = false;
            _puzzleLocked = false;
            if (_trainingPanel._btnOpHint     != null) _trainingPanel._btnOpHint.Enabled     = false;
            if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = false;
            if (_trainingPanel._btnPuzzleNext    != null) _trainingPanel._btnPuzzleNext.Visible    = false;
            if (_trainingPanel._btnPuzzleAnalyze != null) _trainingPanel._btnPuzzleAnalyze.Visible = false;
            if (_trainingPanel._btnVisionNext    != null) _trainingPanel._btnVisionNext.Visible    = false;
            if (_trainingPanel._pnlPuzzleGame != null) _trainingPanel._pnlPuzzleGame.Visible = false;
            if (_trainingPanel._pnlVisionGame != null) _trainingPanel._pnlVisionGame.Visible = false;
            boardControl.MonochromeMode  = config?.MonochromeBoard == true;
            boardControl.HideCoordinates = false;
            bool showStrips = config?.ShowMaterialStrips != false;
            _materialTop.Visible    = showStrips;
            _materialBottom.Visible = showStrips;

            if (_trainingGameActive)
            {
                boardControl.ClearTrainingHighlight();
                boardControl.TrainingMode = false;
                RestoreBoardAfterTraining();
            }

            _trainingUiVisible = false;
            _trainingPanel!.Visible = false;
            _trainingPanel.SendToBack();
            analysisOutput.Visible = true;
            grpEngineMatch.Visible = _matchPanelActive;
            lblAnalysis.Visible = true;
            evalBar.Visible = config?.ShowEvalBar != false;
            if (_evalGraph != null) _evalGraph.Visible = config?.ShowEvalGraph ?? true;

            boardControl.InteractionEnabled = true;
            btnTraining.Text = "♟";
            toolTip.SetToolTip(btnTraining, "Training");
            SetTrainingButtonsEnabled(true);
            SetModeSwitchersEnabled(true);

            _ = TriggerAutoAnalysis();
        }

        /// <summary>
        /// Thin IModeController wrapper around StopTraining — see Modes/IModeController.cs.
        /// <see cref="Enter"/> is purely additive bookkeeping, same spirit as BotModeController's
        /// non-interface Enter: Training has 6 separate places that start "a game/drill is
        /// active" (TrainingStartRound dispatches Square/Opening/Vision/Puzzle; Drill vs Bot and
        /// Drill Watch Engines are started independently, bypassing TrainingStartRound
        /// entirely). This records which one, so CurrentModeController's type now tells you
        /// training is active at all 6 of those entry points, not just the first 4 — nothing
        /// reads ActiveSubMode today, so this changes no existing control flow.
        /// </summary>
        private sealed class TrainingModeController : Modes.IModeController
        {
            private readonly AnalysisBoardForm _form;
            public TrainingSubMode? ActiveSubMode { get; private set; }
            public TrainingModeController(AnalysisBoardForm form) => _form = form;
            public void Enter(TrainingSubMode subMode) => ActiveSubMode = subMode;
            public void ExitMode() => _form.StopTraining();
        }

        private void RestoreBoardAfterTraining()
        {
            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null) boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen     = null;
            _trainingGameActive = false;
            if (CurrentModeController is TrainingModeController) CurrentModeController = null;
        }

        private void SetTrainingButtonsEnabled(bool enabled)
        {
            foreach (var btn in new[] { btnNewGame, btnFlipBoard, btnTakeBack, btnPrevMove,
                                        btnNextMove, btnAutoPlay, btnPlayBot, btnEditPosition })
                btn.Enabled = enabled;
        }

        private void SetModeSwitchersEnabled(bool enabled)
        {
            if (_trainingPanel._btnSqMode     != null) _trainingPanel._btnSqMode.Enabled     = enabled;
            if (_trainingPanel._btnOpMode     != null) _trainingPanel._btnOpMode.Enabled     = enabled;
            if (_trainingPanel._btnPuzzleMode != null) _trainingPanel._btnPuzzleMode.Enabled = enabled;
            if (_trainingPanel._btnVisionMode != null) _trainingPanel._btnVisionMode.Enabled = enabled;
            if (_trainingPanel._btnDrillMode  != null) _trainingPanel._btnDrillMode.Enabled  = enabled;
        }

        internal void TrainingShowStartPanel()
        {
            _trainingClockTimer?.Stop();
            _trainingFlashTimer?.Stop();
            _openingAutoplayTimer?.Stop();
            _puzzleRushTimer?.Stop();
            _openingRecreatePhase = false;
            _puzzleActive = false;
            _puzzleLocked = false;
            if (_trainingPanel._btnOpHint     != null) _trainingPanel._btnOpHint.Enabled     = false;
            if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = false;
            if (_trainingPanel._pnlPuzzleGame != null) _trainingPanel._pnlPuzzleGame.Visible = false;
            if (_trainingPanel._pnlVisionGame != null) _trainingPanel._pnlVisionGame.Visible = false;

            if (_trainingGameActive)
            {
                boardControl.ClearTrainingHighlight();
                boardControl.TrainingMode = false;
                boardControl.MonochromeMode = false;
                RestoreBoardAfterTraining();
            }

            SetModeSwitchersEnabled(true);

            if (_trainingPanel._lblOpGameStatus != null) _trainingPanel._lblOpGameStatus.Visible = false;
            if (_trainingPanel._lblTrainingTimer != null) _trainingPanel._lblTrainingTimer.Visible = true;

            _trainingPanel._pnlTrainingStart!.Visible = true;
            _trainingPanel._pnlTrainingGame!.Visible = false;
            _trainingPanel._pnlPuzzleGame!.Visible = false;
            _trainingPanel._pnlVisionGame!.Visible = false;
            _trainingPanel._pnlTrainingResult!.Visible = false;
            boardControl.InteractionEnabled = false;
        }

        private string TrainingModeKey()
        {
            string diff = _trainingPanel._rbTrainingEasy?.Checked == true ? "Easy" : "Challenge";
            string side = _trainingPanel._rbTrainingRandom?.Checked == true ? "Random"
                        : _trainingPanel._rbTrainingBlack?.Checked == true  ? "Black" : "White";
            return $"{diff}-{side}";
        }

        internal void TrainingStartRound()
        {
            boardControl.InteractionEnabled = true;
            if (_drillWatchActive) { matchService?.StopMatch(); return; }
            var trainingController = new TrainingModeController(this);
            trainingController.Enter(_trainingSubMode);
            CurrentModeController = trainingController;
            if (_trainingSubMode == TrainingSubMode.Opening) { OpeningTrainingStart();  return; }
            if (_trainingSubMode == TrainingSubMode.Vision)  { VisionTrainingStart();   return; }
            if (_trainingSubMode == TrainingSubMode.Puzzle)
            {
                if (_puzzleSubMode == "rush")     { _ = PuzzleRushStart();     return; }
                if (_puzzleSubMode == "gauntlet") { _ = PuzzleGauntletStart(); return; }
                if (_puzzleSubMode == "daily")    { PuzzleDailyStart();    return; }
                _ = PuzzleTrainingStart(); return;
            }
            if (_trainingSubMode == TrainingSubMode.Drill) { DrillStart(); return; }

            _trainingPreFen = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingQuestions   = (int)(_trainingPanel._numQuestions?.Value ?? 10);
            _trainingTimeLimitSec = (int)(_trainingPanel._numTimeLimit?.Value ?? 0);
            autoAnalysisCts?.Cancel();

            bool playAsBlack = _trainingPanel._rbTrainingBlack?.Checked == true ||
                               (_trainingPanel._rbTrainingRandom?.Checked == true && _trainingRng.Next(2) == 1);
            boardControl.IsFlipped = playAsBlack;
            boardControl.TrainingMode            = true;
            boardControl.HoverSquareLabelEnabled = _trainingPanel._rbTrainingEasy?.Checked == true;
            boardControl.HideCoordinates         = _trainingPanel._rbTrainingEasy?.Checked != true;
            boardControl.LoadFEN(TRAINING_EMPTY_FEN);

            _trainingCorrect = 0;
            _trainingWrong = 0;
            _trainingAwaitingNext = false;
            _trainingInWrongFlash = false;
            _trainingInCorrectFlash = false;
            _trainingGameActive = true;
            SetModeSwitchersEnabled(false);

            _trainingPanel._pnlTrainingStart!.Visible = false;
            _trainingPanel._pnlTrainingResult!.Visible = false;
            _trainingPanel._pnlTrainingGame!.Visible = true;

            _trainingRoundStart = DateTime.Now;
            _trainingClockTimer?.Start();
            TrainingNextQuestion();
        }

        private void TrainingNextQuestion()
        {
            _trainingAwaitingNext = false;
            boardControl.ClearTrainingHighlight();

            if (_trainingPanel._rbTrainingRandom?.Checked == true)
                boardControl.IsFlipped = _trainingRng.Next(2) == 1;

            _trainingTargetRow = _trainingRng.Next(0, 8);
            _trainingTargetCol = _trainingRng.Next(0, 8);
            string name = $"{(char)('a' + _trainingTargetCol)}{8 - _trainingTargetRow}";

            if (_trainingPanel._lblTrainingRound != null)
                _trainingPanel._lblTrainingRound.Text = $"Question  {_trainingCorrect + _trainingWrong + 1}  /  {_trainingQuestions}";
            if (_trainingPanel._lblTrainingTarget != null)
                _trainingPanel._lblTrainingTarget.Text = name;

            TrainingUpdateStats();
        }

        internal void TrainingSquareClicked(int row, int col)
        {
            if (!_trainingGameActive || _trainingAwaitingNext) return;
            _trainingAwaitingNext = true;
            _trainingFlashMs = 0;

            if (row == _trainingTargetRow && col == _trainingTargetCol)
            {
                _trainingCorrect++;
                _trainingInWrongFlash = false;
                _trainingInCorrectFlash = true;
                boardControl.SetTrainingHighlight(row, col, TrainingOkColor);
            }
            else
            {
                _trainingWrong++;
                _trainingInWrongFlash = true;
                _trainingInCorrectFlash = false;
                _trainingCorrectRow = _trainingTargetRow;
                _trainingCorrectCol = _trainingTargetCol;
                boardControl.SetTrainingHighlight(row, col, TrainingErrorColor);
            }

            TrainingUpdateStats();
            _trainingFlashTimer?.Start();
        }

        internal void TrainingFlashTimer_Tick(object? sender, EventArgs e)
        {
            _trainingFlashMs += 16;

            if (_trainingInWrongFlash && _trainingFlashMs >= TRAINING_WRONG_MS)
            {
                _trainingInWrongFlash = false;
                _trainingInCorrectFlash = true;
                _trainingFlashMs = 0;
                boardControl.SetTrainingHighlight(_trainingCorrectRow, _trainingCorrectCol,
                    TrainingOkColor);
                return;
            }

            if (_trainingInCorrectFlash && _trainingFlashMs >= TRAINING_CORRECT_MS)
            {
                _trainingInCorrectFlash = false;
                _trainingFlashTimer?.Stop();
                boardControl.ClearTrainingHighlight();

                if (_trainingCorrect + _trainingWrong >= _trainingQuestions || TrainingCheckTimeExpired())
                    TrainingShowResult();
                else
                    TrainingNextQuestion();
            }
        }

        internal void TrainingUpdateStats()
        {
            if (_trainingPanel._lblTrainingScore != null)
                _trainingPanel._lblTrainingScore.Text = $"{_trainingCorrect} ✓     {_trainingWrong} ✗";

            if (_trainingPanel._lblTrainingTimer != null)
            {
                double elapsed = (DateTime.Now - _trainingRoundStart).TotalSeconds;
                if (_trainingTimeLimitSec > 0)
                {
                    double remaining = _trainingTimeLimitSec - elapsed;
                    if (remaining < 0) remaining = 0;
                    _trainingPanel._lblTrainingTimer.Text = $"{remaining:F1}s remaining";
                    _trainingPanel._lblTrainingTimer.ForeColor = remaining <= 10 ? Color.OrangeRed : SystemColors.ControlText;
                }
                else
                {
                    _trainingPanel._lblTrainingTimer.Text = $"{elapsed:F1}s";
                    _trainingPanel._lblTrainingTimer.ForeColor = SystemColors.ControlText;
                }
            }
        }

        internal bool TrainingCheckTimeExpired()
        {
            if (_trainingTimeLimitSec <= 0) return false;
            return (DateTime.Now - _trainingRoundStart).TotalSeconds >= _trainingTimeLimitSec;
        }

        internal void TrainingShowResult()
        {
            _trainingClockTimer?.Stop();
            double elapsed = (DateTime.Now - _trainingRoundStart).TotalSeconds;

            boardControl.ClearTrainingHighlight();
            boardControl.TrainingMode = false;
            RestoreBoardAfterTraining();

            if (_trainingPanel._lblTrainingFinalScore != null)
                _trainingPanel._lblTrainingFinalScore.Text = $"{_trainingCorrect} / {_trainingQuestions} correct  ·  {elapsed:F1}s";

            // Personal best
            string key = TrainingModeKey();
            bool newPB = false;
            if (config != null)
            {
                if (!config.TrainingPersonalBests.TryGetValue(key, out var pb))
                    pb = new TrainingPersonalBest();

                if (_trainingCorrect > pb.BestCorrect ||
                    (_trainingCorrect == pb.BestCorrect && elapsed < pb.BestTime))
                {
                    pb.BestCorrect = _trainingCorrect;
                    pb.BestQuestions = _trainingQuestions;
                    pb.BestTime = elapsed;
                    pb.BestTimePerQuestion = _trainingCorrect > 0 ? elapsed / _trainingCorrect : double.MaxValue;
                    pb.LastSet = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    config.TrainingPersonalBests[key] = pb;
                    config.Save();
                    newPB = true;
                }

                if (_trainingPanel._lblTrainingPB != null)
                {
                    string pbText = pb.BestTime == double.MaxValue
                        ? ""
                        : $"PB ({key}): {pb.BestCorrect}/{pb.BestQuestions} in {pb.BestTime:F1}s";
                    _trainingPanel._lblTrainingPB.Text = newPB ? $"New personal best!  {pbText}" : pbText;
                }
            }

            if (_trainingPanel._lblOpMissedMoves != null) _trainingPanel._lblOpMissedMoves.Visible = false;
            _trainingPanel._pnlTrainingGame!.Visible = false;
            _trainingPanel._pnlTrainingResult!.Visible = true;

            _ = TriggerAutoAnalysis();
        }

        // ── Opening Training ───────────────────────────────────────────────

        internal void SetTrainingMode(TrainingSubMode mode)
        {
            _trainingSubMode = mode;
            if (_trainingPanel._lblTrainingTitle != null)
                _trainingPanel._lblTrainingTitle.Text = mode switch
                {
                    TrainingSubMode.Opening => "Opening Training",
                    TrainingSubMode.Puzzle  => "Puzzle Training",
                    TrainingSubMode.Vision  => "Board Vision",
                    TrainingSubMode.Drill   => "Drills",
                    _                       => "Square Training"
                };
            if (_trainingPanel._pnlSquareSettings  != null) _trainingPanel._pnlSquareSettings.Visible  = mode == TrainingSubMode.Square;
            if (_trainingPanel._pnlOpeningSettings != null) _trainingPanel._pnlOpeningSettings.Visible = mode == TrainingSubMode.Opening;
            if (_trainingPanel._pnlPuzzleSettings  != null) _trainingPanel._pnlPuzzleSettings.Visible  = mode == TrainingSubMode.Puzzle;
            if (_trainingPanel._pnlVisionSettings  != null) _trainingPanel._pnlVisionSettings.Visible  = mode == TrainingSubMode.Vision;
            if (_trainingPanel._pnlDrillSettings   != null) _trainingPanel._pnlDrillSettings.Visible   = mode == TrainingSubMode.Drill;
            HighlightButton(_trainingPanel._btnSqMode,     mode == TrainingSubMode.Square);
            HighlightButton(_trainingPanel._btnOpMode,     mode == TrainingSubMode.Opening);
            HighlightButton(_trainingPanel._btnPuzzleMode, mode == TrainingSubMode.Puzzle);
            HighlightButton(_trainingPanel._btnVisionMode, mode == TrainingSubMode.Vision);
            HighlightButton(_trainingPanel._btnDrillMode,  mode == TrainingSubMode.Drill);
            if (_trainingPanel._btnTrainingStart != null)
                _trainingPanel._btnTrainingStart.Text = mode == TrainingSubMode.Drill ? "Load position" : "Start";
            if (mode == TrainingSubMode.Square) UpdateSquarePBLabel();
            if (mode == TrainingSubMode.Puzzle) SetPuzzleSubMode(_puzzleSubMode);
            if (mode == TrainingSubMode.Vision) UpdateVisionPBLabel();
            // Re-measure description height now that the panel has its real width
            if (mode == TrainingSubMode.Drill && IsHandleCreated)
                BeginInvoke(() => SetDrillDescription(_trainingPanel._lblDrillDesc?.Text ?? ""));
        }

        internal void SetPuzzleSubMode(string subMode) // "training" | "rush" | "gauntlet"
        {
            _puzzleSubMode = subMode;
            bool isTraining = subMode == "training";
            bool isDaily    = subMode == "daily";
            if (_trainingPanel._pnlThemeFilterRow    != null) _trainingPanel._pnlThemeFilterRow.Visible    = isTraining;
            if (_trainingPanel._pnlOpeningFilterRow  != null) _trainingPanel._pnlOpeningFilterRow.Visible  = isTraining;
            if (_trainingPanel._pnlPuzzleAutoNextRow != null) _trainingPanel._pnlPuzzleAutoNextRow.Visible = isTraining;
            if (_trainingPanel._pnlRushTimeRow       != null) _trainingPanel._pnlRushTimeRow.Visible       = subMode == "rush";
            if (_trainingPanel._lblGauntletDesc      != null) _trainingPanel._lblGauntletDesc.Visible      = subMode == "gauntlet";
            if (_trainingPanel._pnlRatingRow         != null) _trainingPanel._pnlRatingRow.Visible         = !isDaily;
            HighlightButton(_trainingPanel._btnPuzzleSubTraining, isTraining);
            HighlightButton(_trainingPanel._btnPuzzleSubRush,     subMode == "rush");
            HighlightButton(_trainingPanel._btnPuzzleSubGauntlet, subMode == "gauntlet");
            HighlightButton(_trainingPanel._btnPuzzleSubDaily,    isDaily);
            if (_trainingPanel._pnlPuzzleSettings != null)
                _trainingPanel._pnlPuzzleSettings.Height = subMode == "rush" ? 142 : subMode == "gauntlet" ? 132 : isDaily ? 110 : 242;
            if (_trainingPanel._lblPuzzleSettingsPB != null)
            {
                if (isDaily)
                {
                    string today  = DateTime.Today.ToString("MMM d", CultureInfo.InvariantCulture);
                    int streak    = config?.DailyPuzzleStreak ?? 0;
                    string pb     = config?.DailyPuzzleBestStreak > 1 ? $"  ·  Best: {config.DailyPuzzleBestStreak}" : "";
                    string solved = config?.DailyPuzzleLastSolvedDate == DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ? "  ·  Solved ✓" : "";
                    _trainingPanel._lblPuzzleSettingsPB.Text = $"{today}  ·  Streak: {streak}{pb}{solved}";
                }
                else
                {
                    _trainingPanel._lblPuzzleSettingsPB.Text = subMode switch
                    {
                        "rush"     => config?.PuzzleRushBest > 0           ? $"Best: {config.PuzzleRushBest} puzzles" : "",
                        "gauntlet" => config?.GauntletBestStreak > 0       ? $"Best streak: {config.GauntletBestStreak}" : "",
                        _          => config?.PuzzleTrainingBestStreak > 0 ? $"Best streak: {config.PuzzleTrainingBestStreak}" : "",
                    };
                }
            }
        }

        internal void SelectRushTime(int minutes)
        {
            _rushDurationSeconds = minutes * 60;
            HighlightButton(_trainingPanel._rushTimeBtn1, minutes == 1);
            HighlightButton(_trainingPanel._rushTimeBtn2, minutes == 2);
            HighlightButton(_trainingPanel._rushTimeBtn3, minutes == 3);
            HighlightButton(_trainingPanel._rushTimeBtn4, minutes == 4);
            HighlightButton(_trainingPanel._rushTimeBtn5, minutes == 5);
        }

        internal void SelectPuzzleRating(int min, int max)
        {
            _puzzleRatingMin = min;
            _puzzleRatingMax = max;
            HighlightButton(_trainingPanel._ratingBtnAny,    min == 0 && max == int.MaxValue);
            HighlightButton(_trainingPanel._ratingBtnBeg,    min == 0 && max == 1199);
            HighlightButton(_trainingPanel._ratingBtnInt,    min == 1400 && max == 1799);
            HighlightButton(_trainingPanel._ratingBtnAdv,    min == 1800 && max == 2199);
            HighlightButton(_trainingPanel._ratingBtnMaster, min == 2200 && max == int.MaxValue);
            if (_trainingPanel._lblRatingRange != null)
                _trainingPanel._lblRatingRange.Text = (min == 0 && max == int.MaxValue) ? "" :
                                       max == int.MaxValue                ? $"≥ {min}" :
                                       min == 0                           ? $"< {max + 1}" :
                                                                            $"{min} – {max + 1}";
        }

        internal void SelectOpeningForTraining()
        {
            string booksFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
            var entries = ChessDroid.Services.EcoBookService.LoadAll(booksFolder);
            using var selDialog = new OpeningExplorerDialog(entries, pgn =>
            {
                // Parse ECO+name from PGN [Opening] tag: "[Opening "ECO — Name"]"
                var match = PgnOpeningHeaderRegex.Match(pgn);
                if (match.Success)
                {
                    string openingTag = match.Groups[1].Value;
                    int dash = openingTag.IndexOf(" — ", StringComparison.Ordinal); // em-dash
                    if (dash >= 0)
                    {
                        string eco = openingTag.Substring(0, dash);
                        string name = openingTag.Substring(dash + 3);
                        _selectedTrainingOpening = entries.FirstOrDefault(e =>
                            e.Eco == eco && e.Name == name) ?? entries.FirstOrDefault(e => e.Eco == eco);
                    }
                }
                if (_selectedTrainingOpening != null && _trainingPanel._lblSelectedOpening != null)
                    _trainingPanel._lblSelectedOpening.Text = $"{_selectedTrainingOpening.Eco}  {_selectedTrainingOpening.Name}";
                if (_trainingPanel._rbOpSelected != null) _trainingPanel._rbOpSelected.Checked = true;
            }, ThemeService.IsDarkTheme(config?.Theme));
            selDialog.ShowDialog(this);
        }

        private void PrepareOpeningLine(string sanMoves)
        {
            _openingUciMoves.Clear();
            _openingFens.Clear();

            const string startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            _openingFens.Add(startFen);

            string savedFen = boardControl.GetFEN();
            bool savedNav = isNavigating;
            isNavigating = true;
            try
            {
                string currentFen = startFen;
                var tokens = FilterSanTokens(sanMoves);

                foreach (var san in tokens)
                {
                    string? uci = PgnImportService.ConvertSanToUci(san, currentFen);
                    if (uci == null) break;

                    boardControl.LoadFEN(currentFen);
                    if (!boardControl.MakeMove(uci)) break;

                    currentFen = boardControl.GetFEN();
                    _openingUciMoves.Add(uci);
                    _openingFens.Add(currentFen);
                }
            }
            finally
            {
                boardControl.LoadFEN(savedFen);
                isNavigating = savedNav;
            }
        }

        private void OpeningTrainingStart()
        {
            OpeningEntry? entry;
            if (_trainingPanel._rbOpRandom?.Checked == true)
            {
                string booksFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
                var all = ChessDroid.Services.EcoBookService.LoadAll(booksFolder);
                if (all.Count == 0) return;
                entry = all[_trainingRng.Next(all.Count)];
            }
            else
            {
                if (_selectedTrainingOpening == null) { SelectOpeningForTraining(); if (_selectedTrainingOpening == null) return; }
                entry = _selectedTrainingOpening;
            }

            PrepareOpeningLine(entry.Moves);
            if (_openingUciMoves.Count == 0)
            {
                MessageBox.Show("Could not parse the opening line.", "Opening Training", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _trainingPreFen = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _selectedTrainingOpening = entry;
            _selectedWatches = _trainingPanel._rbWatches3?.Checked == true ? 3
                             : _trainingPanel._rbWatches2?.Checked == true ? 2
                             : _trainingPanel._rbWatches1?.Checked == true ? 1 : 0;
            _openingWatchesLeft = _selectedWatches;

            autoAnalysisCts?.Cancel();
            _trainingGameActive = true;
            SetModeSwitchersEnabled(false);

            _trainingPanel._pnlTrainingStart!.Visible = false;
            _trainingPanel._pnlTrainingResult!.Visible = false;
            _trainingPanel._pnlTrainingGame!.Visible = true;

            if (_selectedWatches == 0)
                OpeningTrainingStartRecreate();
            else
                OpeningTrainingStartWatch();
        }

        private void OpeningTrainingStartWatch()
        {
            _openingPlayIndex = 0;
            _openingRecreatePhase = false;
            _openingAutoplayTimer?.Stop();

            isNavigating = true;
            boardControl.IsFlipped = false;
            boardControl.LoadFEN(_openingFens[0]);
            isNavigating = false;
            boardControl.ClearTrainingHighlight();

            if (_trainingPanel._lblOpGameStatus != null) _trainingPanel._lblOpGameStatus.Visible = true;
            if (_trainingPanel._lblTrainingTimer != null) _trainingPanel._lblTrainingTimer.Visible = false;

            UpdateOpeningWatchDisplay();
            _openingAutoplayTimer?.Start();
        }

        internal void OpeningAutoplayTimer_Tick(object? sender, EventArgs e)
        {
            if (_openingPlayIndex >= _openingUciMoves.Count)
            {
                _openingAutoplayTimer?.Stop();
                _openingWatchesLeft--;

                if (_openingWatchesLeft > 0)
                    ScheduleInvoke(800, OpeningTrainingStartWatch);
                else
                    ScheduleInvoke(800, OpeningTrainingStartRecreate);
                return;
            }

            string uci = _openingUciMoves[_openingPlayIndex];
            string fen = _openingFens[_openingPlayIndex + 1];
            string preFen = _openingFens[_openingPlayIndex];

            string san = ConvertUciToSan(uci, preFen);

            isNavigating = true;
            boardControl.LoadFEN(fen);
            isNavigating = false;

            if (config?.ShowAnimations == true)
                boardControl.StartAnimation(uci);
            PlayMoveSound(san.Contains('x'), san);

            SetText(_trainingPanel._lblTrainingTarget, san);

            _openingPlayIndex++;
            UpdateOpeningWatchDisplay();
        }

        private void UpdateOpeningWatchDisplay()
        {
            int watchNum = _selectedWatches - _openingWatchesLeft + 1;
            SetText(_trainingPanel._lblTrainingRound, $"Watch  {watchNum} / {_selectedWatches}");
            SetText(_trainingPanel._lblTrainingScore, $"{_selectedTrainingOpening?.Eco}  {PgnImportService.StripMovesFromOpeningName(_selectedTrainingOpening?.Name ?? "")}");
            SetText(_trainingPanel._lblOpGameStatus,  $"Move  {_openingPlayIndex} / {_openingUciMoves.Count}");
            if (_openingPlayIndex == 0) SetText(_trainingPanel._lblTrainingTarget, "…");
        }

        private void OpeningTrainingStartRecreate()
        {
            _openingRecreateIndex = 0;
            _openingHintsUsed     = 0;
            _openingPosMistakes.Clear();
            _openingRecreateLocked = false;
            _openingRecreatePhase  = true;
            if (_trainingPanel._btnOpHint != null) _trainingPanel._btnOpHint.Enabled = true;
            boardControl.ClearTrainingHighlight();

            isNavigating = true;
            boardControl.LoadFEN(_openingFens[0]);
            isNavigating = false;

            SetText(_trainingPanel._lblTrainingRound,  "Recreate");
            SetText(_trainingPanel._lblTrainingTarget, "?");
            SetText(_trainingPanel._lblTrainingScore,  $"{_selectedTrainingOpening?.Eco}  {PgnImportService.StripMovesFromOpeningName(_selectedTrainingOpening?.Name ?? "")}");
            SetText(_trainingPanel._lblOpGameStatus,   $"Move  1 / {_openingUciMoves.Count}");
        }

        internal void BtnOpHint_Click(object? sender, EventArgs e)
        {
            if (!_openingRecreatePhase || _openingRecreateLocked) return;
            if (_openingRecreateIndex >= _openingUciMoves.Count) return;
            _openingHintsUsed++;
            string uci = _openingUciMoves[_openingRecreateIndex];
            ShowTrainingHint(uci, _trainingPanel._btnOpHint, () =>
            {
                if (_openingRecreatePhase && !_openingRecreateLocked && _trainingPanel._btnOpHint != null)
                    _trainingPanel._btnOpHint.Enabled = true;
            });
        }

        private void OpeningTrainingValidateMove(string uciMove)
        {
            if (_openingRecreateIndex >= _openingUciMoves.Count || _openingRecreateLocked) return;

            string expected = _openingUciMoves[_openingRecreateIndex];
            int toCol = uciMove[2] - 'a';
            int toRow = 7 - (uciMove[3] - '1');

            if (uciMove == expected)
            {
                _openingRecreateIndex++;
                if (_trainingPanel._btnOpHint != null) _trainingPanel._btnOpHint.Enabled = true;

                if (_openingRecreateIndex >= _openingUciMoves.Count)
                {
                    // Last move — lock briefly then show result
                    _openingRecreateLocked = true;
                    ScheduleInvoke(700, OpeningTrainingEnd);
                }
                else
                {
                    // Correct but not last
                    UpdateOpeningRecreateDisplay();
                }
            }
            else
            {
                _openingPosMistakes[_openingRecreateIndex] =
                    _openingPosMistakes.GetValueOrDefault(_openingRecreateIndex) + 1;
                _openingRecreateLocked = true;
                boardControl.SetTrainingHighlight(toRow, toCol, TrainingErrorColor);

                string correctFen = _openingFens[_openingRecreateIndex];
                Task.Delay(600).ContinueWith(_ =>
                {
                    if (IsDisposed) return;
                    Invoke(() =>
                    {
                        boardControl.ClearTrainingHighlight();
                        isNavigating = true;
                        boardControl.LoadFEN(correctFen);
                        isNavigating = false;
                        _openingRecreateLocked = false;
                        if (_trainingPanel._btnOpHint != null) _trainingPanel._btnOpHint.Enabled = true;
                    });
                });
                UpdateOpeningRecreateDisplay();
            }
        }

        // Called from BoardControl_MoveAttempted when a recreate-phase move is vetoed before
        // it ever reaches the board — so unlike the old post-commit "wrong" handling, there's
        // nothing to revert here, only the mistake count/highlight/lock bookkeeping.
        private void RejectOpeningTrainingMove(int toRow, int toCol)
        {
            _openingPosMistakes[_openingRecreateIndex] =
                _openingPosMistakes.GetValueOrDefault(_openingRecreateIndex) + 1;
            _openingRecreateLocked = true;
            boardControl.SetTrainingHighlight(toRow, toCol, TrainingErrorColor);

            Task.Delay(600).ContinueWith(_ =>
            {
                if (IsDisposed) return;
                Invoke(() =>
                {
                    boardControl.ClearTrainingHighlight();
                    _openingRecreateLocked = false;
                    if (_trainingPanel._btnOpHint != null) _trainingPanel._btnOpHint.Enabled = true;
                });
            });
            UpdateOpeningRecreateDisplay();
        }

        private void UpdateOpeningRecreateDisplay()
        {
            int total = _openingUciMoves.Count;
            int done = _openingRecreateIndex;
            SetText(_trainingPanel._lblOpGameStatus,   $"Move  {Math.Min(done + 1, total)} / {total}    ✗ {_openingPosMistakes.Count}");
            SetText(_trainingPanel._lblTrainingTarget, "?");
        }

        private void OpeningTrainingEnd()
        {
            _openingAutoplayTimer?.Stop();
            _openingRecreatePhase = false;
            _openingRecreateLocked = false;
            _trainingGameActive = false;
            if (_trainingPanel._btnOpHint != null) _trainingPanel._btnOpHint.Enabled = false;

            boardControl.ClearTrainingHighlight();
            isNavigating = true;
            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null) boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen = null;
            isNavigating = false;

            int total = _openingUciMoves.Count;
            int wrongPositions = _openingPosMistakes.Count;
            int correct = total - wrongPositions;

            bool isPerfect = wrongPositions == 0 && _openingHintsUsed == 0;
            _openingSessionRuns++;
            if (isPerfect) _openingSessionPerfect++;

            // Persist lifetime stats
            OpeningStats? lifetimeStats = null;
            if (_selectedTrainingOpening != null)
            {
                string key = $"{_selectedTrainingOpening.Eco}|{_selectedTrainingOpening.Name}";
                if (!config.OpeningTrainingStats.TryGetValue(key, out lifetimeStats))
                    lifetimeStats = new OpeningStats();

                lifetimeStats.TotalRuns++;
                if (isPerfect) lifetimeStats.PerfectRuns++;
                lifetimeStats.TotalHintsUsed += _openingHintsUsed;
                int runMistakes = _openingPosMistakes.Values.Sum();
                lifetimeStats.TotalMistakes += runMistakes;
                if (runMistakes < lifetimeStats.BestRunMistakes) lifetimeStats.BestRunMistakes = runMistakes;
                lifetimeStats.LastAttempted = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                config.OpeningTrainingStats[key] = lifetimeStats;
                config.Save();
            }

            if (_trainingPanel._lblTrainingFinalScore != null)
                _trainingPanel._lblTrainingFinalScore.Text = $"{correct} / {total} correct"
                    + (wrongPositions == 0 ? "  —  Perfect!" : "")
                    + (_openingHintsUsed > 0 ? $"  ({_openingHintsUsed} hint{(_openingHintsUsed == 1 ? "" : "s")})" : "");

            if (_trainingPanel._lblTrainingPB != null)
            {
                string openingName = $"{_selectedTrainingOpening?.Eco}  {_selectedTrainingOpening?.Name}";
                string sessionStr  = $"Session: {_openingSessionRuns} run{(_openingSessionRuns == 1 ? "" : "s")} · {_openingSessionPerfect} perfect";
                string lifetimeStr = lifetimeStats != null
                    ? $"All time: {lifetimeStats.TotalRuns} run{(lifetimeStats.TotalRuns == 1 ? "" : "s")} · {lifetimeStats.PerfectRuns} perfect"
                      + (lifetimeStats.BestRunMistakes == 0 ? " · clean ✓" : lifetimeStats.BestRunMistakes < int.MaxValue ? $" · best run: {lifetimeStats.BestRunMistakes} mistake{(lifetimeStats.BestRunMistakes == 1 ? "" : "s")}" : "")
                    : "";
                _trainingPanel._lblTrainingPB.Text = openingName + Environment.NewLine + sessionStr + "  ·  " + lifetimeStr;
            }

            if (_trainingPanel._lblOpMissedMoves != null)
            {
                if (wrongPositions == 0)
                {
                    _trainingPanel._lblOpMissedMoves.Visible = false;
                }
                else
                {
                    var parts = _openingPosMistakes.OrderBy(kv => kv.Key).Select(kv =>
                    {
                        string san = ConvertUciToSan(_openingUciMoves[kv.Key], _openingFens[kv.Key]);
                        int moveNum = kv.Key / 2 + 1;
                        string prefix = kv.Key % 2 == 0 ? $"{moveNum}." : $"{moveNum}…";
                        return $"{prefix}{san} ×{kv.Value}";
                    });
                    _trainingPanel._lblOpMissedMoves.Text = "Missed:  " + string.Join("   ", parts);
                    _trainingPanel._lblOpMissedMoves.Visible = true;
                }
            }

            _trainingPanel._pnlTrainingGame!.Visible = false;
            _trainingPanel._pnlTrainingResult!.Visible = true;

            if (_trainingPanel._lblOpGameStatus != null) _trainingPanel._lblOpGameStatus.Visible = false;
            if (_trainingPanel._lblTrainingTimer != null) _trainingPanel._lblTrainingTimer.Visible = true;

            _ = TriggerAutoAnalysis();
        }

        // ── Puzzle Training ────────────────────────────────────────────────────

        // ── Endgame Drills ─────────────────────────────────────────────────────

        private void SetDrillControlsEnabled(bool enabled)
        {
            if (_trainingPanel._cmbDrillStudy         != null) _trainingPanel._cmbDrillStudy.Enabled         = enabled;
            if (_trainingPanel._cmbDrillChapter       != null) _trainingPanel._cmbDrillChapter.Enabled       = enabled;
            if (_trainingPanel._btnTrainingStart      != null) _trainingPanel._btnTrainingStart.Enabled      = enabled;
            if (_trainingPanel._btnDrillWatchEngines  != null) _trainingPanel._btnDrillWatchEngines.Enabled  = enabled;
            // Lock mode-switcher buttons so the user can't leave drill mode mid-game
            if (_trainingPanel._btnSqMode     != null) _trainingPanel._btnSqMode.Enabled     = enabled;
            if (_trainingPanel._btnOpMode     != null) _trainingPanel._btnOpMode.Enabled     = enabled;
            if (_trainingPanel._btnPuzzleMode != null) _trainingPanel._btnPuzzleMode.Enabled = enabled;
            if (_trainingPanel._btnVisionMode != null) _trainingPanel._btnVisionMode.Enabled = enabled;
            if (_trainingPanel._btnDrillMode  != null) _trainingPanel._btnDrillMode.Enabled  = enabled;
        }

        internal void SetDrillDescription(string text)
        {
            if (_trainingPanel._lblDrillDesc == null || _trainingPanel._pnlDrillSettings == null) return;
            _trainingPanel._lblDrillDesc.Text = text;
            const int baseHeight = 152; // panel height without the description
            const int maxDescHeight = 120; // max visible lines before scrollbar kicks in
            if (string.IsNullOrEmpty(text))
            {
                _trainingPanel._lblDrillDesc.Height = 0;
            }
            else
            {
                int w = Math.Max(_trainingPanel._pnlDrillSettings.ClientSize.Width, 80);
                var sz = TextRenderer.MeasureText(text, _trainingPanel._lblDrillDesc.Font,
                    new Size(w, 9999), TextFormatFlags.WordBreak);
                _trainingPanel._lblDrillDesc.Height = Math.Min(sz.Height + 6, maxDescHeight);
            }
            _trainingPanel._pnlDrillSettings.Height = baseHeight + _trainingPanel._lblDrillDesc.Height;
        }

        private List<EndgameChapter> GetChaptersForStudy(string? study)
            => _drillChapters.Where(c => c.StudyName == study).ToList();

        private void PopulateDrillStudies()
        {
            if (_trainingPanel._cmbDrillStudy == null) return;
            _trainingPanel._cmbDrillStudy.Items.Clear();
            foreach (var name in EndgameDrillService.GetStudyNames(_drillChapters))
                _trainingPanel._cmbDrillStudy.Items.Add(name);
            if (_trainingPanel._cmbDrillStudy.Items.Count > 0) _trainingPanel._cmbDrillStudy.SelectedIndex = 0;
        }

        internal void PopulateDrillChapters()
        {
            if (_trainingPanel._cmbDrillStudy == null || _trainingPanel._cmbDrillChapter == null) return;
            string? study = _trainingPanel._cmbDrillStudy.SelectedItem?.ToString();
            _trainingPanel._cmbDrillChapter.Items.Clear();
            int n = 1;
            foreach (var ch in GetChaptersForStudy(study))
                _trainingPanel._cmbDrillChapter.Items.Add($"Ch.{n++}: {ch.ChapterName}");
            if (_trainingPanel._cmbDrillChapter.Items.Count > 0) _trainingPanel._cmbDrillChapter.SelectedIndex = 0;
        }

        internal EndgameChapter? SelectedDrillChapter()
        {
            if (_trainingPanel._cmbDrillStudy == null || _trainingPanel._cmbDrillChapter == null) return null;
            string? study = _trainingPanel._cmbDrillStudy.SelectedItem?.ToString();
            int idx = _trainingPanel._cmbDrillChapter.SelectedIndex;
            if (idx < 0) return null;
            var chapters = GetChaptersForStudy(study);
            return idx < chapters.Count ? chapters[idx] : null;
        }

        private void DrillStart()
        {
            var chapter = SelectedDrillChapter();
            if (chapter == null) { lblStatus.Text = "Select a position first."; return; }

            StopTraining();

            boardControl.LoadFEN(chapter.Fen);
            moveTree.Clear(chapter.Fen);
            UpdateMoveList();
            UpdateFenDisplay();
            UpdateTurnLabel();

            // Flip board to match the active side
            if (!chapter.WhiteToMove && !boardControl.IsFlipped) boardControl.FlipBoard();
            else if (chapter.WhiteToMove && boardControl.IsFlipped) boardControl.FlipBoard();

            lblStatus.Text = chapter.ChapterName;
            _ = TriggerAutoAnalysis();
        }

        internal async Task DrillPracticeAsync()
        {
            var chapter = SelectedDrillChapter();
            if (chapter == null) { lblStatus.Text = "Select a position first."; return; }

            // Button acts as toggle — stop if already running
            if (_botModeActive) { StopBotMode(); return; }

            if (matchRunning) { lblStatus.Text = "Stop the engine match first"; return; }
            if (string.IsNullOrEmpty(config?.SelectedEngine)) { lblStatus.Text = "No engine configured — click ⚙ to set up"; return; }

            string[] availableEngines = Directory.Exists(config.GetEnginesPath())
                ? Directory.GetFiles(config.GetEnginesPath(), "*.exe").Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToArray()
                : Array.Empty<string>();
            using var dialog = new BotSettingsDialog(ThemeService.IsDarkTheme(config?.Theme),
                availableEngines, config?.EngineProfiles ?? new(), config?.SelectedEngine ?? "",
                drillMode: true);
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _botSettings = dialog.Settings;
            // User always plays the active side in the drill position
            _botSettings.BotPlaysWhite = !chapter.WhiteToMove;

            // Lock down drill controls — training panel stays visible the whole session
            SetDrillControlsEnabled(false);
            if (_trainingPanel._btnDrillVsBot != null) { _trainingPanel._btnDrillVsBot.Text = "⏹ Stop Practice"; _trainingPanel._btnDrillVsBot.Enabled = true; }

            // Board setup (no StopTraining — we keep the training panel visible)
            CancelClassification();
            boardControl.ClearEngineArrows();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            boardControl.LoadFEN(chapter.Fen);
            moveTree.Clear(chapter.Fen);
            _botPositionCounts.Clear();
            _botPositionCounts[GetPositionKey(chapter.Fen)] = 1;
            moveListBox.Items.Clear();
            _movePairs.Clear();
            _analysisCache.Clear();
            _currentClassification = null;
            _classificationLookup = null;
            consoleFormatter?.SetActiveClassification(null);
            analysisOutput.Clear();
            evalBar?.Reset();
            UpdateMoveList();
            UpdateFenDisplay();
            UpdateTurnLabel();

            // Flip board so user plays the active side
            bool userPlaysBlack = _botSettings.BotPlaysWhite;
            if (userPlaysBlack && !boardControl.IsFlipped) boardControl.FlipBoard();
            else if (!userPlaysBlack && boardControl.IsFlipped) boardControl.FlipBoard();

            // Start bot engine
            try
            {
                lblStatus.Text = "Starting bot engine...";
                string enginesPath = config!.GetEnginesPath();
                string engineFile  = !string.IsNullOrEmpty(_botSettings.EngineFileName)
                    ? _botSettings.EngineFileName : config.SelectedEngine;
                string enginePath  = Path.Combine(enginesPath, engineFile);

                _botEngine = new ChessEngineService(config);
                await _botEngine.InitializeAsync(enginePath);

                if (_botEngine.State != EngineState.Ready)
                {
                    lblStatus.Text = "Failed to start bot engine";
                    _botEngine.Dispose();
                    _botEngine = null;
                    SetDrillControlsEnabled(true);
                    if (_trainingPanel._btnDrillVsBot != null) _trainingPanel._btnDrillVsBot.Text = "⚔ Practice vs Bot";
                    return;
                }

                await _botEngine.SetEloTargetAsync(_botSettings.EloTarget, _botSettings.GetSkillLevel());

                if (!_botEngine.SupportsEloTargeting)
                    MessageBox.Show(
                        $"{_botEngine.EngineName} doesn't support Elo targeting (UCI_LimitStrength).\n" +
                        "The engine will play at full strength regardless of the Elo setting.",
                        "Elo Targeting Not Supported",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Bot engine error: {ex.Message}";
                _botEngine?.Dispose();
                _botEngine = null;
                SetDrillControlsEnabled(true);
                if (_trainingPanel._btnDrillVsBot != null) _trainingPanel._btnDrillVsBot.Text = "⚔ Practice vs Bot";
                return;
            }

            _drillBotActive = true;
            _botModeActive  = true;
            _botMoveCts     = new CancellationTokenSource();
            var drillBotController = new TrainingModeController(this);
            drillBotController.Enter(TrainingSubMode.Drill);
            CurrentModeController = drillBotController;
            btnPlayBot.Text = "⏹";
            toolTip.SetToolTip(btnPlayBot, "Stop Bot");
            boardControl.InteractionEnabled = true;
            btnStartMatch.Enabled = false;

            // Always challenge mode for drills — no eval bar, no engine lines, no hints
            ApplyChallengeMode();

            string colorLabel = userPlaysBlack ? "Black" : "White";
            lblStatus.Text = $"Drill — {chapter.ChapterName}  |  {GetBotDifficultyLabel()}  |  You play {colorLabel}";

            // Bot moves first only if it's their turn in the drill starting position
            bool botMovesFirst = _botSettings.BotPlaysWhite == chapter.WhiteToMove;
            if (botMovesFirst)
                _botMoveTask = MakeBotMoveAsync();
            // No TriggerAutoAnalysis — challenge mode suppresses analysis anyway
        }

        internal async Task DrillWatchEnginesAsync()
        {
            var chapter = SelectedDrillChapter();
            if (chapter == null) { lblStatus.Text = "Select a position first."; return; }

            if (matchRunning) { lblStatus.Text = "Stop the engine match first"; return; }
            if (_botModeActive) { lblStatus.Text = "Stop bot mode first"; return; }

            string[] availableEngines = Directory.Exists(config?.GetEnginesPath())
                ? Directory.GetFiles(config!.GetEnginesPath(), "*.exe")
                    .Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToArray()
                : Array.Empty<string>();

            if (availableEngines.Length == 0) { lblStatus.Text = "No engines found in Engines folder"; return; }

            using var dlg = new DrillWatchDialog(ThemeService.IsDarkTheme(config?.Theme),
                availableEngines, config?.EngineProfiles ?? new(), config?.SelectedEngine ?? "");
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string enginesPath = config!.GetEnginesPath();
            string whitePath = Path.Combine(enginesPath, dlg.WhiteEngine);
            string blackPath = Path.Combine(enginesPath, dlg.BlackEngine);
            if (!File.Exists(whitePath) || !File.Exists(blackPath))
            { lblStatus.Text = "Engine file not found"; return; }

            // Lock drill controls
            SetDrillControlsEnabled(false);
            _drillWatchActive = true;
            var drillWatchController = new TrainingModeController(this);
            drillWatchController.Enter(TrainingSubMode.Drill);
            CurrentModeController = drillWatchController;
            if (_trainingPanel._btnDrillWatchEngines != null) { _trainingPanel._btnDrillWatchEngines.Text = "⏹ Stop Watch"; _trainingPanel._btnDrillWatchEngines.Enabled = true; }

            // Board setup
            boardControl.LoadFEN(chapter.Fen);
            moveTree.Clear(chapter.Fen);
            moveListBox.Items.Clear();
            _movePairs.Clear();
            _analysisCache.Clear();
            analysisOutput.Clear();
            evalBar?.Reset();
            UpdateMoveList();
            UpdateFenDisplay();
            UpdateTurnLabel();

            // Flip to match active side
            if (!chapter.WhiteToMove && !boardControl.IsFlipped) boardControl.FlipBoard();
            else if (chapter.WhiteToMove && boardControl.IsFlipped) boardControl.FlipBoard();

            // Engine names for display
            config.EngineProfiles.TryGetValue(dlg.WhiteEngine, out var wProf);
            config.EngineProfiles.TryGetValue(dlg.BlackEngine, out var bProf);
            _matchWhiteName     = !string.IsNullOrEmpty(wProf?.DisplayName) ? wProf!.DisplayName : Path.GetFileNameWithoutExtension(dlg.WhiteEngine);
            _matchBlackName     = !string.IsNullOrEmpty(bProf?.DisplayName) ? bProf!.DisplayName : Path.GetFileNameWithoutExtension(dlg.BlackEngine);
            _matchWhiteElo      = wProf?.Elo ?? 0;
            _matchBlackElo      = bProf?.Elo ?? 0;
            _matchWhiteFileName = dlg.WhiteEngine;
            _matchBlackFileName = dlg.BlackEngine;

            // Series init (single game)
            _seriesTotal            = 1;
            _seriesPlayed           = 0;
            _seriesEng1Score        = 0;
            _seriesEng2Score        = 0;
            _seriesEng1File         = dlg.WhiteEngine;
            _seriesEng2File         = dlg.BlackEngine;
            _seriesCurrentWhiteFile = dlg.WhiteEngine;
            lblSeriesScore.Text     = "";

            // Seed position counts for threefold detection
            _watchPositionCounts.Clear();
            _watchPositionCounts[GetPositionKey(chapter.Fen)] = 1;

            // Output header
            analysisOutput.AppendText($"Engine Watch — {chapter.ChapterName}\n");
            analysisOutput.AppendText($"{_matchWhiteName} vs {_matchBlackName}  |  60+0\n\n");
            _matchTranscript.Clear();
            _matchTranscript.Append(analysisOutput.Text);

            // Set up match service
            matchService?.Dispose();
            matchService = new Services.EngineMatchService(config);
            _previousMatchEval = null;
            matchService.OnMovePlayed           += MatchService_OnMovePlayed;
            matchService.OnClockUpdated         += MatchService_OnClockUpdated;
            matchService.OnMatchEnded           += MatchService_OnMatchEnded;
            matchService.OnStatusChanged        += MatchService_OnStatusChanged;
            matchService.OnAnnotatorEvalUpdated += MatchService_OnAnnotatorEvalUpdated;
            boardControl.AnimationCompleted     += MatchBoard_AnimationCompleted;

            // Set annotator engine for eval bar
            matchService.AnnotatorEngine = engineService;
            matchService.AnnotatorDepth  = config.EngineDepth;
            SetEngineInfoLabels(wProf, bProf, dlg.WhiteEngine, dlg.BlackEngine);

            // 60+0 time control
            var tc = new EngineMatchTimeControl
            {
                Type        = TimeControlType.TotalPlusIncrement,
                TotalTimeMs = 60_000,
                IncrementMs = 0
            };

            clockTimer.Start();
            matchRunning = true;
            btnStartMatch.Enabled = false;
            _matchLiveAnchor = moveTree.CurrentNode;

            await matchService.StartMatchAsync(whitePath, blackPath, _matchWhiteName, _matchBlackName, tc, chapter.Fen);
        }

        internal static readonly (string Label, string? Theme)[] _puzzleThemeOptions =
        {
            ("Any",                  null),
            ("Mate in 1",            "mateIn1"),
            ("Mate in 2",            "mateIn2"),
            ("Mate in 3",            "mateIn3"),
            ("Fork",                 "fork"),
            ("Pin",                  "pin"),
            ("Skewer",               "skewer"),
            ("Sacrifice",            "sacrifice"),
            ("Discovered Attack",    "discoveredAttack"),
            ("Back Rank Mate",       "backRankMate"),
            ("Endgame",              "endgame"),
        };

        internal static readonly (string Label, string? Opening)[] _puzzleOpeningOptions =
        {
            ("Any Opening",             null),
            ("Sicilian Defense",        "Sicilian_Defense"),
            ("French Defense",          "French_Defense"),
            ("Ruy Lopez",               "Ruy_Lopez"),
            ("Italian Game",            "Italian_Game"),
            ("Caro-Kann Defense",       "Caro-Kann_Defense"),
            ("King's Indian Defense",   "Kings_Indian_Defense"),
            ("Queen's Gambit",          "Queens_Gambit"),
            ("English Opening",         "English_Opening"),
            ("Nimzo-Indian Defense",    "Nimzo-Indian_Defense"),
            ("Slav Defense",            "Slav_Defense"),
            ("Dutch Defense",           "Dutch_Defense"),
            ("King's Gambit",           "Kings_Gambit"),
            ("Scandinavian Defense",    "Scandinavian_Defense"),
            ("Grunfeld Defense",        "Grunfeld_Defense"),
            ("Scotch Game",             "Scotch_Game"),
            ("Pirc Defense",            "Pirc_Defense"),
            ("Modern Defense",          "Modern_Defense"),
            ("Benoni Defense",          "Benoni_Defense"),
            ("Four Knights Game",       "Four_Knights_Game"),
            ("Petrov's Defense",        "Petrovs_Defense"),
            ("Indian Defense",          "Indian_Defense"),
        };

        private void ResetPuzzleCounters()
        {
            _puzzlesClean     = 0;
            _puzzlesStruggled = 0;
            _puzzlesAttempted = 0;
            _puzzleHintsUsed  = 0;
            _puzzleQueue.Clear();
        }

        private async Task PuzzleTrainingStart()
        {
            if (string.IsNullOrEmpty(_puzzlesFolder)) return;
            ResetPuzzleCounters();
            _puzzleStreak    = 0;
            _puzzleStreakBest = config?.PuzzleTrainingBestStreak ?? 0;
            var batch = await Task.Run(() => LichessPuzzleService.GetRandomBatch(
                _puzzlesFolder, 200, _puzzleThemeFilter, _puzzleRatingMin, _puzzleRatingMax, _puzzleOpeningFilter));
            _puzzleQueue.AddRange(batch);

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;
            SetModeSwitchersEnabled(false);

            _trainingPanel._pnlTrainingStart!.Visible  = false;
            _trainingPanel._pnlTrainingResult!.Visible = false;
            _trainingPanel._pnlPuzzleGame!.Visible     = true;

            await PuzzleLoadNext();
        }

        private void PuzzleDailyStart()
        {
            if (string.IsNullOrEmpty(_puzzlesFolder)) return;
            var daily = LichessPuzzleService.GetDailyPuzzle(_puzzlesFolder);
            if (daily == null) { lblStatus.Text = "No puzzles found."; return; }

            ResetPuzzleCounters();
            _puzzleStreak    = 0;
            _puzzleStreakBest = 0;
            _puzzleQueue.Add(daily);

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;
            SetModeSwitchersEnabled(false);

            _trainingPanel._pnlTrainingStart!.Visible  = false;
            _trainingPanel._pnlTrainingResult!.Visible = false;
            _trainingPanel._pnlPuzzleGame!.Visible     = true;

            _ = PuzzleLoadNext();
        }

        private void DailyPuzzleCompleted()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (config != null && config.DailyPuzzleLastSolvedDate != today)
            {
                // Update streak
                bool consecutive = config.DailyPuzzleLastSolvedDate ==
                                   DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                config.DailyPuzzleStreak = consecutive ? config.DailyPuzzleStreak + 1 : 1;
                if (config.DailyPuzzleStreak > config.DailyPuzzleBestStreak)
                    config.DailyPuzzleBestStreak = config.DailyPuzzleStreak;
                config.DailyPuzzleLastSolvedDate = today;
                config.Save();
            }

            bool clean = _puzzlesStruggled == 0 && _puzzleHintsUsed == 0;
            if (clean && config != null) { config.DailyPuzzleCleanSolves++; config.Save(); }
            string streakStr = (config?.DailyPuzzleStreak ?? 0) > 0
                ? $"  ·  Streak: {config!.DailyPuzzleStreak}" : "";
            string result = clean ? $"Solved clean!{streakStr}" : $"Solved{streakStr}";
            SetText(_trainingPanel._lblPuzzleFeedback, result);

            if (_trainingPanel._btnPuzzleAnalyze != null) _trainingPanel._btnPuzzleAnalyze.Visible = true;
            if (_trainingPanel._btnPuzzleNext    != null) _trainingPanel._btnPuzzleNext.Visible    = false;
            if (_trainingPanel._btnPuzzleSkip    != null) _trainingPanel._btnPuzzleSkip.Visible    = false;
            _puzzleLocked = true;
        }

        internal async Task PuzzleLoadNext()
        {
            if (_trainingPanel._btnPuzzleNext    != null) _trainingPanel._btnPuzzleNext.Visible    = false;
            if (_trainingPanel._btnPuzzleAnalyze != null) _trainingPanel._btnPuzzleAnalyze.Visible = false;
            if (_trainingPanel._btnPuzzleSkip    != null) _trainingPanel._btnPuzzleSkip.Visible    = _puzzleSubMode == "training";
            boardControl.ClearTrainingHighlight();
            boardControl.ClearSelection();
            _puzzleLocked      = false;
            _wrongThisPuzzle   = false;
            _currentPuzzle     = null;

            if (_puzzleQueue.Count == 0)
            {
                if (_puzzleSubMode == "daily") { DailyPuzzleCompleted(); return; }

                // GetRandomBatch reads from one randomly chosen file/offset, so a narrow
                // combination of theme + rating + opening filters can legitimately come
                // back empty on a given attempt. Retry with fresh randomness a few times
                // before giving up, instead of indexing into an empty queue below.
                const int maxAttempts = 5;
                string puzzlesFolder = _puzzlesFolder!;
                string? themeFilter = _puzzleThemeFilter, openingFilter = _puzzleOpeningFilter;
                int ratingMin = _puzzleRatingMin, ratingMax = _puzzleRatingMax;
                for (int attempt = 0; attempt < maxAttempts && _puzzleQueue.Count == 0; attempt++)
                {
                    var batch = await Task.Run(() => LichessPuzzleService.GetRandomBatch(
                        puzzlesFolder, 200, themeFilter, ratingMin, ratingMax, openingFilter));
                    _puzzleQueue.AddRange(batch);
                }

                if (_puzzleQueue.Count == 0)
                {
                    SetText(_trainingPanel._lblPuzzleFeedback, "No puzzles match these filters — try widening theme/rating/opening.");
                    if (_trainingPanel._btnPuzzleNext != null) _trainingPanel._btnPuzzleNext.Visible = false;
                    if (_trainingPanel._btnPuzzleSkip != null) _trainingPanel._btnPuzzleSkip.Visible = false;
                    _puzzleActive = false;
                    return;
                }
            }

            _currentPuzzle = _puzzleQueue[0];
            _puzzleQueue.RemoveAt(0);
            _puzzleMoveIndex  = 1;
            _puzzlesAttempted++;
            _puzzleActive     = true;

            SetText(_trainingPanel._lblPuzzleFeedback, "");
            if (_trainingPanel._lblPuzzleThemes  != null) _trainingPanel._lblPuzzleThemes.Text =
                _puzzleThemeFilter != null ? $"▸ {_trainingPanel._cmbPuzzleTheme?.Text}" : "";
            if (_trainingPanel._lblPuzzleRating  != null) _trainingPanel._lblPuzzleRating.Text =
                $"Puzzle #{_puzzlesAttempted}  ·  Rating {_currentPuzzle.Rating}";
            if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = false;  // enabled after trigger plays
            UpdatePuzzleStats();

            // Determine player side from FEN side-to-move (player is opposite of trigger mover)
            bool fenWhiteToMove = _currentPuzzle.Fen.Contains(" w ");
            isNavigating = true;
            boardControl.LoadFEN(_currentPuzzle.Fen);
            boardControl.IsFlipped = fenWhiteToMove; // fenWhiteToMove=true → opponent is white → player is black → flip
            isNavigating = false;

            // Short pause then play trigger move
            _puzzleLocked = true;
            ScheduleInvoke(500, PuzzlePlayTrigger);
        }

        private void PuzzlePlayTrigger()
        {
            if (_currentPuzzle == null) return;
            string trigger = _currentPuzzle.Moves[0];
            string preFen  = boardControl.GetFEN();

            isNavigating = true;
            boardControl.MakeMove(trigger);
            isNavigating = false;

            string san = ConvertUciToSan(trigger, preFen);
            PlayMoveSound(san.Contains('x'), san);
            if (config?.ShowAnimations == true)
                boardControl.StartAnimation(trigger);

            int delay = config?.ShowAnimations == true ? (config.AnimationDurationMs + 100) : 150;
            Task.Delay(delay).ContinueWith(_ =>
            {
                if (!IsDisposed) Invoke(() =>
                {
                    _puzzleStartTime = DateTime.Now;
                    _puzzleLocked    = false;
                    if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = true;
                });
            });
        }

        internal void BtnPuzzleHint_Click(object? sender, EventArgs e)
        {
            if (!_puzzleActive || _puzzleLocked || _currentPuzzle == null) return;
            if (_puzzleMoveIndex >= _currentPuzzle.Moves.Length) return;
            _puzzleHintsUsed++;
            _wrongThisPuzzle = true;
            _puzzleStreak    = 0;
            UpdatePuzzleStats();
            string uci = _currentPuzzle.Moves[_puzzleMoveIndex];
            ShowTrainingHint(uci, _trainingPanel._btnPuzzleHint, () =>
            {
                if (_puzzleActive && !_puzzleLocked && _trainingPanel._btnPuzzleHint != null)
                    _trainingPanel._btnPuzzleHint.Enabled = true;
            });
        }

        private void PuzzleValidateMove(string uciMove)
        {
            if (_currentPuzzle == null || _puzzleMoveIndex >= _currentPuzzle.Moves.Length) return;
            string expected = _currentPuzzle.Moves[_puzzleMoveIndex];

            if (uciMove == expected)
            {
                _puzzleMoveIndex++;
                if (_puzzleMoveIndex >= _currentPuzzle.Moves.Length)
                {
                    // Puzzle solved
                    if (_wrongThisPuzzle) { _puzzlesStruggled++; _puzzleStreak = 0; }
                    else
                    {
                        _puzzlesClean++;
                        if (_puzzleSubMode == "training")
                        {
                            _puzzleStreak++;
                            if (_puzzleStreak > _puzzleStreakBest)
                            {
                                _puzzleStreakBest = _puzzleStreak;
                                if (config != null)
                                {
                                    config.PuzzleTrainingBestStreak     = _puzzleStreakBest;
                                    config.PuzzleTrainingBestStreakDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                                    config.Save();
                                }
                            }
                        }
                    }
                    if (_puzzleSubMode == "gauntlet") _gauntletStreak++;
                    _puzzleLocked = true;
                    if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = false;
                    double elapsed = (DateTime.Now - _puzzleStartTime).TotalSeconds;
                    string timeStr = _puzzleSubMode == "gauntlet" ? "" : elapsed < 60
                        ? $"  {elapsed:F1}s"
                        : $"  {(int)(elapsed/60)}m {(int)(elapsed%60)}s";
                    SetText(_trainingPanel._lblPuzzleFeedback, $"✓ Correct!{timeStr}");
                    PuzzleRevealThemes();
                    UpdatePuzzleStats();
                    int solveDelay = _puzzleSubMode == "gauntlet" ? 700 : 1400;
                    bool manualNext = _puzzleSubMode == "training" && _trainingPanel._chkPuzzleAutoNext?.Checked == false;
                    if (manualNext)
                        BeginInvoke(() =>
                        {
                            if (_trainingPanel._btnPuzzleNext    != null) _trainingPanel._btnPuzzleNext.Visible    = true;
                            if (_trainingPanel._btnPuzzleAnalyze != null) _trainingPanel._btnPuzzleAnalyze.Visible = true;
                            if (_trainingPanel._btnPuzzleSkip    != null) _trainingPanel._btnPuzzleSkip.Visible    = false;
                        });
                    else
                        ScheduleInvoke(solveDelay, () => _ = PuzzleLoadNext());
                }
                else
                {
                    // Correct move, opponent responds
                    _puzzleLocked = true;
                    SetText(_trainingPanel._lblPuzzleFeedback, "");
                    ScheduleInvoke(400, PuzzlePlayOpponentResponse);
                }
            }
            else
            {
                _wrongThisPuzzle = true;
                _puzzleLocked    = true;
                int toRow = 7 - (uciMove[3] - '1');
                int toCol = uciMove[2] - 'a';
                boardControl.SetTrainingHighlight(toRow, toCol, TrainingErrorColor);
                SetText(_trainingPanel._lblPuzzleFeedback, "✗ Wrong");

                if (_puzzleSubMode == "gauntlet")
                {
                    // Restore correct position, then end after brief flash
                    isNavigating = true;
                    boardControl.LoadFEN(_currentPuzzle.Fen);
                    for (int i = 0; i < _puzzleMoveIndex; i++)
                        boardControl.MakeMove(_currentPuzzle.Moves[i]);
                    isNavigating = false;
                    ScheduleInvoke(900, GauntletEnd);
                    return;
                }

                // Training / Rush: restore position and let player retry
                isNavigating = true;
                boardControl.LoadFEN(_currentPuzzle.Fen);
                for (int i = 0; i < _puzzleMoveIndex; i++)
                    boardControl.MakeMove(_currentPuzzle.Moves[i]);
                isNavigating = false;

                Task.Delay(800).ContinueWith(_ =>
                {
                    if (!IsDisposed) Invoke(() =>
                    {
                        boardControl.ClearTrainingHighlight();
                        SetText(_trainingPanel._lblPuzzleFeedback, "");
                        _puzzleLocked = false;
                        if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = true;
                    });
                });
            }
        }

        // Called from BoardControl_MoveAttempted when a puzzle move is vetoed before it ever
        // reaches the board — nothing to revert, only the wrong-move bookkeeping/feedback.
        private void RejectPuzzleMove(int toRow, int toCol)
        {
            _wrongThisPuzzle = true;
            _puzzleLocked    = true;
            boardControl.SetTrainingHighlight(toRow, toCol, TrainingErrorColor);
            SetText(_trainingPanel._lblPuzzleFeedback, "✗ Wrong");

            if (_puzzleSubMode == "gauntlet")
            {
                ScheduleInvoke(900, GauntletEnd);
                return;
            }

            // Training / Rush: let the player retry after a brief flash.
            Task.Delay(800).ContinueWith(_ =>
            {
                if (!IsDisposed) Invoke(() =>
                {
                    boardControl.ClearTrainingHighlight();
                    SetText(_trainingPanel._lblPuzzleFeedback, "");
                    _puzzleLocked = false;
                    if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = true;
                });
            });
        }

        private void PuzzlePlayOpponentResponse()
        {
            if (_currentPuzzle == null || _puzzleMoveIndex >= _currentPuzzle.Moves.Length) return;
            string response = _currentPuzzle.Moves[_puzzleMoveIndex];
            string preFen   = boardControl.GetFEN();

            isNavigating = true;
            boardControl.MakeMove(response);
            isNavigating = false;

            string san = ConvertUciToSan(response, preFen);
            PlayMoveSound(san.Contains('x'), san);
            if (config?.ShowAnimations == true)
                boardControl.StartAnimation(response);

            _puzzleMoveIndex++;
            int delay = config?.ShowAnimations == true ? (config.AnimationDurationMs + 100) : 150;
            Task.Delay(delay).ContinueWith(_ =>
            {
                if (!IsDisposed) Invoke(() =>
                {
                    _puzzleLocked = false;
                    if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = true;
                });
            });
        }

        internal void PuzzleSkip()
        {
            if (!_puzzleActive) return;
            _puzzlesAttempted = Math.Max(0, _puzzlesAttempted - 1); // don't count skips
            _puzzleLocked = true;
            if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = false;
            PuzzleRevealThemes();
            SetText(_trainingPanel._lblPuzzleFeedback, "");
            bool manualNext = _puzzleSubMode == "training" && _trainingPanel._chkPuzzleAutoNext?.Checked == false;
            if (manualNext)
            {
                if (_trainingPanel._btnPuzzleNext    != null) _trainingPanel._btnPuzzleNext.Visible    = true;
                if (_trainingPanel._btnPuzzleAnalyze != null) _trainingPanel._btnPuzzleAnalyze.Visible = true;
                if (_trainingPanel._btnPuzzleSkip    != null) _trainingPanel._btnPuzzleSkip.Visible    = false;
            }
            else
                ScheduleInvoke(900, () => _ = PuzzleLoadNext());
        }

        private void PuzzleRevealThemes()
        {
            if (_trainingPanel._lblPuzzleThemes == null || _currentPuzzle == null) return;
            string themes  = string.Join(" · ", _currentPuzzle.Themes.Take(5));
            string opening = _currentPuzzle.OpeningTags.Replace('_', ' ').Trim();
            _trainingPanel._lblPuzzleThemes.Text = string.IsNullOrEmpty(opening)
                ? themes
                : $"{themes}\n{opening}";
        }

        internal void UpdatePuzzleStats()
        {
            if (_trainingPanel._lblPuzzleStats == null) return;
            string s;
            if (_puzzleSubMode == "gauntlet")
            {
                s = $"Streak: {_gauntletStreak}   Best: {_gauntletBestStreak}";
            }
            else if (_puzzleSubMode == "rush")
            {
                int pb = config?.PuzzleRushBest ?? 0;
                string pbStr = pb > 0 ? $"   ·   PB: {pb}" : "";
                s = $"Solved: {_puzzlesClean + _puzzlesStruggled}   ~ Helped: {_puzzlesStruggled}{pbStr}";
            }
            else
            {
                int total = _puzzlesClean + _puzzlesStruggled;
                string accStr = total > 0 ? $"   ·   {_puzzlesClean * 100 / total}% clean" : "";
                string streakStr = _puzzleStreak > 0 ? $"   ·   Streak {_puzzleStreak}" : "";
                string pbStr = _puzzleStreakBest > 1 ? $" (best {_puzzleStreakBest})" : "";
                s = $"✓ {_puzzlesClean}   ~ {_puzzlesStruggled}{accStr}{streakStr}{pbStr}";
            }
            _trainingPanel._lblPuzzleStats.Text = s;
        }

        // ── Puzzle Rush ────────────────────────────────────────────────────────

        private async Task PuzzleRushStart()
        {
            if (string.IsNullOrEmpty(_puzzlesFolder)) return;
            ResetPuzzleCounters();
            var batch = await Task.Run(() => LichessPuzzleService.GetRandomBatch(
                _puzzlesFolder, 300, _puzzleThemeFilter, _puzzleRatingMin, _puzzleRatingMax, _puzzleOpeningFilter));
            _puzzleQueue.AddRange(batch);

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;
            SetModeSwitchersEnabled(false);

            _trainingPanel._pnlTrainingStart!.Visible  = false;
            _trainingPanel._pnlTrainingResult!.Visible = false;
            _trainingPanel._pnlPuzzleGame!.Visible     = true;

            // Show rush timer, hide skip (rushing = no time to skip)
            if (_trainingPanel._lblRushTimer    != null) _trainingPanel._lblRushTimer.Visible    = true;
            if (_trainingPanel._btnPuzzleSkip   != null) _trainingPanel._btnPuzzleSkip.Visible   = false;
            if (_trainingPanel._btnPuzzleHint   != null) _trainingPanel._btnPuzzleHint.Visible   = true;

            _rushSecondsRemaining = _rushDurationSeconds;
            UpdateRushDisplay();

            _puzzleRushTimer?.Dispose();
            _puzzleRushTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _puzzleRushTimer.Tick += PuzzleRushTick;
            _puzzleRushTimer.Start();

            await PuzzleLoadNext();
        }

        private void PuzzleRushTick(object? sender, EventArgs e)
        {
            _rushSecondsRemaining--;
            UpdateRushDisplay();
            if (_rushSecondsRemaining <= 0)
            {
                _puzzleRushTimer?.Stop();
                _puzzleLocked = true;
                if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Enabled = false;
                ScheduleInvoke(300, PuzzleRushEnd);
            }
        }

        private void UpdateRushDisplay()
        {
            if (_trainingPanel._lblRushTimer == null) return;
            int m = _rushSecondsRemaining / 60;
            int s = _rushSecondsRemaining % 60;
            _trainingPanel._lblRushTimer.Text = $"{m}:{s:D2}";
            _trainingPanel._lblRushTimer.ForeColor = _rushSecondsRemaining <= 10
                ? Color.FromArgb(220, 80, 80)
                : ThemeService.GetColorScheme(config?.Theme ?? "Dark").TextColor;
        }

        private void PuzzleRushEnd()
        {
            _puzzleActive = false;
            _puzzleRushTimer?.Stop();
            boardControl.ClearTrainingHighlight();
            boardControl.ClearSelection();
            if (_trainingPanel._lblRushTimer  != null) _trainingPanel._lblRushTimer.Visible  = false;
            if (_trainingPanel._btnPuzzleSkip != null) _trainingPanel._btnPuzzleSkip.Visible = true;

            int solved = _puzzlesClean + _puzzlesStruggled;
            if (_trainingPanel._lblTrainingFinalScore != null)
                _trainingPanel._lblTrainingFinalScore.Text = $"Puzzles solved: {solved}  ({_puzzlesClean} clean)";
            if (_trainingPanel._lblTrainingPB != null)
            {
                int durationMin = _rushDurationSeconds / 60;
                int prevBest = 0;
                config?.PuzzleRushBestByMinutes.TryGetValue(durationMin, out prevBest);
                bool newPb = solved > prevBest;
                if (newPb && config != null)
                {
                    config.PuzzleRushBestByMinutes[durationMin] = solved;
                    config.PuzzleRushBest = Math.Max(config.PuzzleRushBest, solved);
                    config.Save();
                }
                _trainingPanel._lblTrainingPB.Text = newPb ? $"New best!  {solved}" : prevBest > 0 ? $"Best ({durationMin} min): {prevBest}" : "";
            }
            if (_trainingPanel._lblOpMissedMoves != null) _trainingPanel._lblOpMissedMoves.Visible = false;

            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null) boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen    = null;
            _trainingGameActive = false;

            _trainingPanel._pnlPuzzleGame!.Visible    = false;
            _trainingPanel._pnlTrainingResult!.Visible = true;
        }

        // ── Puzzle Gauntlet ────────────────────────────────────────────────────

        private async Task PuzzleGauntletStart()
        {
            if (string.IsNullOrEmpty(_puzzlesFolder)) return;
            ResetPuzzleCounters();
            _gauntletStreak     = 0;
            _gauntletBestStreak = config?.GauntletBestStreak ?? 0;
            _puzzleQueue.Clear();
            var batch = await Task.Run(() => LichessPuzzleService.GetRandomBatch(
                _puzzlesFolder, 300, _puzzleThemeFilter, _puzzleRatingMin, _puzzleRatingMax, _puzzleOpeningFilter));
            _puzzleQueue.AddRange(batch);

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;
            SetModeSwitchersEnabled(false);

            _trainingPanel._pnlTrainingStart!.Visible  = false;
            _trainingPanel._pnlTrainingResult!.Visible = false;
            _trainingPanel._pnlPuzzleGame!.Visible     = true;

            if (_trainingPanel._lblRushTimer  != null) _trainingPanel._lblRushTimer.Visible  = false;
            if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Visible = false; // no hints in gauntlet
            if (_trainingPanel._btnPuzzleSkip != null) _trainingPanel._btnPuzzleSkip.Visible = false; // no skips in gauntlet

            await PuzzleLoadNext();
        }

        private void GauntletEnd()
        {
            _puzzleActive = false;
            boardControl.ClearTrainingHighlight();
            boardControl.ClearSelection();

            if (_gauntletStreak > _gauntletBestStreak)
            {
                _gauntletBestStreak = _gauntletStreak;
                if (config != null)
                {
                    config.GauntletBestStreak     = _gauntletBestStreak;
                    config.GauntletBestStreakDate  = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
            }
            if (config != null)
            {
                config.GauntletRecentScores.Add(_gauntletStreak);
                if (config.GauntletRecentScores.Count > 5)
                    config.GauntletRecentScores.RemoveAt(0);
                config.Save();
            }

            if (_trainingPanel._btnPuzzleHint != null) _trainingPanel._btnPuzzleHint.Visible = true;
            if (_trainingPanel._btnPuzzleSkip != null) _trainingPanel._btnPuzzleSkip.Visible = true;

            if (_trainingPanel._lblTrainingFinalScore != null)
                _trainingPanel._lblTrainingFinalScore.Text = $"Streak: {_gauntletStreak}";
            if (_trainingPanel._lblTrainingPB != null)
                _trainingPanel._lblTrainingPB.Text = _gauntletBestStreak > _gauntletStreak
                    ? $"Best streak: {_gauntletBestStreak}"
                    : _gauntletStreak > 0 ? "New personal best!" : "";
            if (_trainingPanel._lblOpMissedMoves != null) _trainingPanel._lblOpMissedMoves.Visible = false;

            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null) boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen    = null;
            _trainingGameActive = false;

            _trainingPanel._pnlPuzzleGame!.Visible    = false;
            _trainingPanel._pnlTrainingResult!.Visible = true;
        }

        // ── Board Vision ───────────────────────────────────────────────────────

        private static readonly string[] _allSquares = Enumerable.Range(0, 64)
            .Select(i => $"{(char)('a' + i % 8)}{i / 8 + 1}").ToArray();

        private static bool IsLightSquareName(string sq)
        {
            int col  = sq[0] - 'a'; // 0-7
            int rank = sq[1] - '0'; // 1-8
            return (col + rank) % 2 == 0;
        }

        private void VisionTrainingStart()
        {
            _visionCorrect    = 0;
            _visionWrong      = 0;
            _visionStreak     = 0;
            _visionBestStreak = 0;
            _visionLives      = 3;
            _visionStartTime  = DateTime.Now;

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;
            SetModeSwitchersEnabled(false);

            boardControl.TrainingMode      = false;
            boardControl.MonochromeMode    = true;
            boardControl.HideCoordinates   = true;
            boardControl.IsFlipped         = false;
            isNavigating = true;
            boardControl.LoadFEN(TRAINING_EMPTY_FEN);
            isNavigating = false;

            _materialTop.Visible    = false;
            _materialBottom.Visible = false;

            bool hasTimed = _visionSubMode != "training";
            if (_trainingPanel._lblVisionTimer       != null) { _trainingPanel._lblVisionTimer.Visible       = hasTimed;                        _trainingPanel._lblVisionTimer.Text = ""; }
            if (_trainingPanel._lblVisionLives       != null) { _trainingPanel._lblVisionLives.Visible       = _visionSubMode == "survival";    UpdateVisionLivesLabel(); }
            if (_trainingPanel._lblVisionGlobalTimer != null) { _trainingPanel._lblVisionGlobalTimer.Visible = _visionSubMode == "timed";       _trainingPanel._lblVisionGlobalTimer.Text = ""; }

            if (_visionSubMode == "timed")
            {
                _visionGlobalSecondsRemaining = _visionGlobalDurationSeconds;
                UpdateVisionGlobalTimerLabel();
                _visionGlobalTimer?.Start();
            }

            _trainingPanel._pnlTrainingStart!.Visible  = false;
            _trainingPanel._pnlTrainingResult!.Visible = false;
            _trainingPanel._pnlVisionGame!.Visible     = true;

            VisionLoadNext();
        }

        internal void VisionLoadNext()
        {
            if (_trainingPanel._btnVisionNext != null) _trainingPanel._btnVisionNext.Visible = false;
            _visionCurrentSquare = _allSquares[_trainingRng.Next(_allSquares.Length)];
            if (_trainingPanel._lblVisionQuestion != null)
                _trainingPanel._lblVisionQuestion.Text = $"Is  {_visionCurrentSquare}  light or dark?";
            if (_trainingPanel._lblVisionScore != null)
                _trainingPanel._lblVisionScore.Text = $"✓ {_visionCorrect}   ✗ {_visionWrong}   Streak: {_visionStreak}";
            if (_trainingPanel._btnVisionLight != null) _trainingPanel._btnVisionLight.Enabled = true;
            if (_trainingPanel._btnVisionDark  != null) _trainingPanel._btnVisionDark.Enabled  = true;

            if (_visionSubMode != "training")
            {
                _visionSecondsRemaining = _visionTimedSeconds;
                UpdateVisionTimerLabel();
                _visionQuestionTimer?.Start();
            }
        }

        internal void VisionAnswer(bool guessedLight)
        {
            _visionQuestionTimer?.Stop();
            if (_trainingPanel._btnVisionLight != null) _trainingPanel._btnVisionLight.Enabled = false;
            if (_trainingPanel._btnVisionDark  != null) _trainingPanel._btnVisionDark.Enabled  = false;
            VisionHandleResult(IsLightSquareName(_visionCurrentSquare) == guessedLight, timeout: false);
        }

        private void VisionHandleResult(bool correct, bool timeout)
        {
            string squareAnswer = IsLightSquareName(_visionCurrentSquare) ? "Light" : "Dark";
            if (correct)
            {
                _visionCorrect++;
                _visionStreak++;
                if (_trainingPanel._lblVisionQuestion != null)
                    _trainingPanel._lblVisionQuestion.Text = $"{_visionCurrentSquare} — {squareAnswer} ✓";
            }
            else
            {
                _visionWrong++;
                _visionStreak = 0;
                if (_visionSubMode == "survival") { _visionLives--; UpdateVisionLivesLabel(); }
                if (_trainingPanel._lblVisionQuestion != null)
                    _trainingPanel._lblVisionQuestion.Text = $"{_visionCurrentSquare} — {squareAnswer}  {(timeout ? "⏱" : "✗")}";
            }

            if (_trainingPanel._lblVisionScore != null)
                _trainingPanel._lblVisionScore.Text = $"✓ {_visionCorrect}   ✗ {_visionWrong}   Streak: {_visionStreak}";
            if (_trainingPanel._lblVisionTimer != null) _trainingPanel._lblVisionTimer.Text = "";
            if (_visionStreak > _visionBestStreak) _visionBestStreak = _visionStreak;

            if (_visionSubMode == "survival" && _visionLives <= 0)
            {
                ScheduleInvoke(900, VisionSurvivalEnd);
                return;
            }

            bool manualNext = _visionSubMode == "training" && _trainingPanel._chkVisionAutoNext?.Checked == false;
            if (manualNext)
                { if (_trainingPanel._btnVisionNext != null) _trainingPanel._btnVisionNext.Visible = true; }
            else
                ScheduleInvoke(correct ? 350 : 700, VisionLoadNext);
        }

        internal void SetVisionSubMode(string subMode) // "training" | "timed" | "survival"
        {
            _visionSubMode = subMode;
            bool hasTimed = subMode != "training";
            if (_trainingPanel._pnlVisionTimeRow       != null) _trainingPanel._pnlVisionTimeRow.Visible       = hasTimed;
            if (_trainingPanel._pnlVisionGlobalTimeRow != null) _trainingPanel._pnlVisionGlobalTimeRow.Visible = subMode == "timed";
            if (_trainingPanel._pnlVisionAutoNextRow   != null) _trainingPanel._pnlVisionAutoNextRow.Visible   = subMode == "training";
            if (_trainingPanel._pnlVisionSettings != null)
                _trainingPanel._pnlVisionSettings.Height = subMode == "timed" ? 180 : hasTimed ? 150 : 146;
            if (_trainingPanel._lblVisionDesc != null)
                _trainingPanel._lblVisionDesc.Text = subMode == "survival"
                    ? "3 lives — wrong or timeout costs a life.\nRun ends when lives reach zero."
                    : subMode == "timed"
                    ? "Answer before time runs out.\nTimeout counts as wrong."
                    : "Is the square light or dark?\nAll 64 squares — no visual clues.";
            HighlightButton(_trainingPanel._btnVisionSubTraining, subMode == "training");
            HighlightButton(_trainingPanel._btnVisionSubTimed,    subMode == "timed");
            HighlightButton(_trainingPanel._btnVisionSubSurvival, subMode == "survival");
            UpdateVisionPBLabel();
        }

        internal void UpdateSquarePBLabel()
        {
            if (_trainingPanel._lblSquareSettingsPB == null || config == null) return;
            string key = TrainingModeKey();
            _trainingPanel._lblSquareSettingsPB.Text = config.TrainingPersonalBests.TryGetValue(key, out var pb) && pb.BestTime != double.MaxValue
                ? $"Best: {pb.BestCorrect}/{pb.BestQuestions} correct  ·  {pb.BestTime:F1}s"
                : "";
        }

        internal void UpdateVisionPBLabel()
        {
            if (_trainingPanel._lblVisionSettingsPB == null || config == null) return;
            string key = _visionSubMode == "survival" ? "Vision-Survival"
                : $"Vision-Timed-{_visionGlobalDurationSeconds}";
            if (_visionSubMode == "training")
                { _trainingPanel._lblVisionSettingsPB.Text = ""; return; }
            _trainingPanel._lblVisionSettingsPB.Text = config.TrainingPersonalBests.TryGetValue(key, out var pb) && pb.BestCorrect > 0
                ? $"Best: {pb.BestCorrect} correct"
                : "";
        }

        internal void SelectVisionTime(int seconds)
        {
            _visionTimedSeconds = seconds;
            HighlightButton(_trainingPanel._visionTimeBtn3,  seconds == 3);
            HighlightButton(_trainingPanel._visionTimeBtn5,  seconds == 5);
            HighlightButton(_trainingPanel._visionTimeBtn10, seconds == 10);
        }

        internal void SelectVisionGlobalTime(int seconds)
        {
            _visionGlobalDurationSeconds = seconds;
            HighlightButton(_trainingPanel._visionGlobalBtn1, seconds == 60);
            HighlightButton(_trainingPanel._visionGlobalBtn3, seconds == 180);
            HighlightButton(_trainingPanel._visionGlobalBtn5, seconds == 300);
            UpdateVisionPBLabel();
        }

        internal void VisionGlobalTimer_Tick(object? sender, EventArgs e)
        {
            _visionGlobalSecondsRemaining--;
            UpdateVisionGlobalTimerLabel();
            if (_visionGlobalSecondsRemaining <= 0)
            {
                _visionGlobalTimer!.Stop();
                _visionQuestionTimer?.Stop();
                if (_trainingPanel._btnVisionLight != null) _trainingPanel._btnVisionLight.Enabled = false;
                if (_trainingPanel._btnVisionDark  != null) _trainingPanel._btnVisionDark.Enabled  = false;
                if (!IsDisposed) Invoke((Action)VisionTimedEnd);
            }
        }

        private void UpdateVisionGlobalTimerLabel()
        {
            if (_trainingPanel._lblVisionGlobalTimer == null) return;
            int m = _visionGlobalSecondsRemaining / 60;
            int s = _visionGlobalSecondsRemaining % 60;
            _trainingPanel._lblVisionGlobalTimer.Text = $"⏱ {m}:{s:D2}";
            _trainingPanel._lblVisionGlobalTimer.ForeColor = _visionGlobalSecondsRemaining <= 10
                ? Color.FromArgb(220, 80, 80)
                : (Parent?.ForeColor ?? Color.White);
        }

        private void VisionTimedEnd()
        {
            if (config != null)
            {
                string key = $"Vision-Timed-{_visionGlobalDurationSeconds}";
                if (!config.TrainingPersonalBests.TryGetValue(key, out var pb))
                    pb = new TrainingPersonalBest();
                double elapsed = _visionGlobalDurationSeconds; // timed mode always runs full duration
                double tpq     = _visionCorrect > 0 ? elapsed / _visionCorrect : double.MaxValue;
                if (_visionCorrect > pb.BestCorrect ||
                    (_visionCorrect == pb.BestCorrect && tpq < pb.BestTimePerQuestion))
                {
                    pb.BestCorrect         = _visionCorrect;
                    pb.BestQuestions       = _visionCorrect + _visionWrong;
                    pb.BestTime            = elapsed;
                    pb.BestTimePerQuestion = tpq;
                    pb.LastSet             = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    config.TrainingPersonalBests[key] = pb;
                }
                if (_visionBestStreak > config.VisionBestStreak)
                    config.VisionBestStreak = _visionBestStreak;
                config.Save();
            }
            VisionEnd($"✓ {_visionCorrect} correct   ✗ {_visionWrong} wrong", $"Best streak: {_visionBestStreak}");
        }

        internal void VisionQuestionTimer_Tick(object? sender, EventArgs e)
        {
            _visionSecondsRemaining--;
            UpdateVisionTimerLabel();
            if (_visionSecondsRemaining <= 0)
            {
                _visionQuestionTimer!.Stop();
                if (_trainingPanel._btnVisionLight != null) _trainingPanel._btnVisionLight.Enabled = false;
                if (_trainingPanel._btnVisionDark  != null) _trainingPanel._btnVisionDark.Enabled  = false;
                VisionHandleResult(false, timeout: true);
            }
        }

        private void UpdateVisionTimerLabel()
        {
            if (_trainingPanel._lblVisionTimer == null) return;
            _trainingPanel._lblVisionTimer.Text = _visionSecondsRemaining > 0 ? $"⏱ {_visionSecondsRemaining}" : "";
            _trainingPanel._lblVisionTimer.ForeColor = _visionSecondsRemaining <= 2
                ? Color.FromArgb(220, 80, 80)
                : (Parent?.ForeColor ?? Color.White);
        }

        private void UpdateVisionLivesLabel()
        {
            if (_trainingPanel._lblVisionLives == null) return;
            _trainingPanel._lblVisionLives.Text = _visionLives switch { >= 3 => "♥ ♥ ♥", 2 => "♥ ♥", 1 => "♥", _ => "✗" };
            _trainingPanel._lblVisionLives.ForeColor = _visionLives <= 1 ? Color.FromArgb(220, 80, 80) : Color.FromArgb(200, 80, 80);
        }

        private void VisionSurvivalEnd()
        {
            if (config != null)
            {
                string key = "Vision-Survival";
                if (!config.TrainingPersonalBests.TryGetValue(key, out var pb))
                    pb = new TrainingPersonalBest();
                double elapsed = (DateTime.Now - _visionStartTime).TotalSeconds;
                double tpq     = _visionCorrect > 0 ? elapsed / _visionCorrect : double.MaxValue;
                if (_visionCorrect > pb.BestCorrect ||
                    (_visionCorrect == pb.BestCorrect && tpq < pb.BestTimePerQuestion))
                {
                    pb.BestCorrect         = _visionCorrect;
                    pb.BestQuestions       = _visionCorrect + _visionWrong;
                    pb.BestTime            = elapsed;
                    pb.BestTimePerQuestion = tpq;
                    pb.LastSet             = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    config.TrainingPersonalBests[key] = pb;
                }
                if (_visionBestStreak > config.VisionBestStreak)
                    config.VisionBestStreak = _visionBestStreak;
                config.Save();
            }
            VisionEnd($"✓ {_visionCorrect} correct   Best streak: {_visionBestStreak}", "");
        }

        // ── Training helpers ───────────────────────────────────────────────────

        private void VisionEnd(string scoreText, string pbText)
        {
            _trainingPanel._pnlVisionGame!.Visible     = false;
            _trainingPanel._pnlTrainingResult!.Visible = true;
            if (_trainingPanel._lblTrainingFinalScore != null) _trainingPanel._lblTrainingFinalScore.Text = scoreText;
            if (_trainingPanel._lblTrainingPB        != null) _trainingPanel._lblTrainingPB.Text = pbText;
            if (_trainingPanel._lblOpMissedMoves     != null) _trainingPanel._lblOpMissedMoves.Visible = false;
        }

        private void ScheduleInvoke(int delayMs, Action action)
        {
            Task.Delay(delayMs).ContinueWith(_ => { if (!IsDisposed) Invoke(action); });
        }

        private void HighlightButton(Button? btn, bool active)
        {
            if (btn == null) return;
            var scheme = ThemeService.GetColorScheme(config?.Theme ?? "Dark");
            btn.BackColor = active ? scheme.TextColor : scheme.ButtonBackColor;
            btn.ForeColor = active ? scheme.FormBackColor : scheme.ButtonForeColor;
        }

        private static void SetText(Label? lbl, string text)  { if (lbl != null) lbl.Text = text; }

        private void ShowTrainingHint(string uci, Button? hintButton, Action? onExpire = null)
        {
            if (hintButton != null) hintButton.Enabled = false;
            var (fromRow, fromCol, _, _) = UciToSquares(uci);
            boardControl.SetTrainingHighlight(fromRow, fromCol, TrainingHintColor);
            Task.Delay(1500).ContinueWith(_ =>
            {
                if (!IsDisposed) Invoke(() =>
                {
                    boardControl.ClearTrainingHighlight();
                    onExpire?.Invoke();
                });
            });
        }

        internal void ApplyTrainingTheme()
        {
            if (_trainingPanel == null) return;
            var scheme = ThemeService.GetColorScheme(config?.Theme ?? "Dark");

            _trainingPanel.BackColor = scheme.FormBackColor;
            ApplyThemeToChildren(_trainingPanel, scheme);
            // Re-apply mode + sub-mode + rush time button highlights with current theme
            SetTrainingMode(_trainingSubMode);
            SetPuzzleSubMode(_puzzleSubMode);
            SetVisionSubMode(_visionSubMode);
            int rushMins = _rushDurationSeconds / 60;
            SelectRushTime(rushMins > 0 ? rushMins : 3);
            SelectPuzzleRating(_puzzleRatingMin, _puzzleRatingMax);
            SelectVisionTime(_visionTimedSeconds);
            SelectVisionGlobalTime(_visionGlobalDurationSeconds);
        }

        private static void ApplyThemeToChildren(Control parent, ColorScheme scheme)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Label lbl)
                {
                    lbl.BackColor = Color.Transparent;
                    lbl.ForeColor = scheme.TextColor;
                }
                else if (c is RadioButton rb)
                {
                    rb.BackColor = Color.Transparent;
                    rb.ForeColor = scheme.TextColor;
                }
                else if (c is CheckBox chk)
                {
                    chk.BackColor = Color.Transparent;
                    chk.ForeColor = scheme.TextColor;
                }
                else if (c is Button btn)
                {
                    btn.BackColor = scheme.ButtonBackColor;
                    btn.ForeColor = scheme.ButtonForeColor;
                }
                else if (c is ComboBox cmb)
                {
                    cmb.BackColor = scheme.ButtonBackColor;
                    cmb.ForeColor = scheme.TextColor;
                }
                else if (c is NumericUpDown nud)
                {
                    nud.BackColor = scheme.ButtonBackColor;
                    nud.ForeColor = scheme.TextColor;
                }
                else if (c is Panel pnl)
                {
                    pnl.BackColor = Color.Transparent;
                    ApplyThemeToChildren(pnl, scheme);
                }
            }
        }

        #endregion
    }
}
