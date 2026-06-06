using System.ComponentModel;
using System.Diagnostics;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Controls
{
    /// <summary>
    /// Custom control for rendering and interacting with a chess board.
    /// Supports click-to-move, legal move highlighting, and board flipping.
    /// </summary>
    public class ChessBoardControl : Control
    {
        // Board state
        private ChessBoard board;
        private bool isFlipped = false;
        private bool whiteToMove = true;

        // Castling rights
        private bool whiteKingsideCastle = true;
        private bool whiteQueensideCastle = true;
        private bool blackKingsideCastle = true;
        private bool blackQueensideCastle = true;

        // En passant target square (-1, -1 if none)
        private int enPassantRow = -1;
        private int enPassantCol = -1;

        // Selection state
        private int selectedRow = -1;
        private int selectedCol = -1;
        private HashSet<(int row, int col)> legalMoveSquares = new HashSet<(int, int)>();

        // Drag state
        private bool isDragging = false;
        private int dragFromRow = -1;
        private int dragFromCol = -1;
        private char dragPiece = '.';
        private Point dragPosition = Point.Empty;
        private const int DRAG_THRESHOLD = 5; // Pixels before drag starts
        private Point mouseDownPosition = Point.Empty;
        private bool mouseDownOnPiece = false;

        // Right-click arrow drawing
        private List<(int fromRow, int fromCol, int toRow, int toCol)> userArrows = new();
        private bool isDrawingArrow = false;
        private int arrowFromRow = -1;
        private int arrowFromCol = -1;
        private Point arrowDragPosition = Point.Empty;
        private static readonly Color ArrowColor = Color.FromArgb(180, 0, 180, 80);

        // Right-click square highlights
        private HashSet<(int row, int col)> _highlightedSquares = new();
        private static readonly Color SquareHighlightColor = Color.FromArgb(190, 235, 97, 40);

        // Engine analysis arrows (separate from user arrows)
        private List<(int fromRow, int fromCol, int toRow, int toCol, Color color)> engineArrows = new();

        // Opening book arrows (drawn underneath engine arrows)
        private List<(int fromRow, int fromCol, int toRow, int toCol, Color color)> _bookArrows = new();

        // Threat arrows (attacker → hanging piece, drawn in red under engine arrows)
        private List<(int fromRow, int fromCol, int toRow, int toCol, Color color)> _threatArrows = new();
        private static readonly Color ThreatArrowColor = Color.FromArgb(170, 200, 45, 45);

        // Rainbow / Wave mode (share one hue-advance timer)
        private bool _rainbowMode = false;
        private bool _waveMode   = false;
        private float _rainbowHue = 0f;
        private readonly System.Windows.Forms.Timer _rainbowTimer;

        // Visual FX
        private bool _gradientBoard    = false;
        private Bitmap? _gradientLightBitmap;
        private Bitmap? _gradientDarkBitmap;
        private Bitmap? _glowWhiteBitmap;
        private Bitmap? _glowBlackBitmap;
        private bool _vignetteEnabled  = false;
        private int  _vignetteAlpha    = 110;
        private bool _pieceGlowEnabled = false;

        // Board frame
        private bool  _boardFrameEnabled = false;
        private int   _boardFrameWidth   = 26;
        private Color _boardFrameColor   = Color.FromArgb(80, 50, 25);

        // Particle system (game-end celebration)
        private struct Particle { public float X, Y, VX, VY, Life, Size; public Color Color; public char Symbol; }
        private readonly List<Particle> _particles = new();
        private readonly System.Windows.Forms.Timer _particleTimer;
        private Font? _particleFont;
        private static readonly char[] _particleSymbols = { '♟','♛','♚','♜','♝','♞','♙','♕','♔','♖','♗','♘' };
        private static readonly Random _particleRng = new();

        // Training mode
        private bool _trainingMode = false;
        private bool _hoverSquareLabelEnabled = false;
        private int _hoverRow = -1;
        private int _hoverCol = -1;
        private int _trainingHighlightRow = -1;
        private int _trainingHighlightCol = -1;
        private Color _trainingHighlightColor = Color.Transparent;
        private Font? _hoverLabelFont;

        // Visual settings
        private Color lightSquareColor = Color.FromArgb(240, 217, 181);
        private Color darkSquareColor = Color.FromArgb(181, 136, 99);
        private bool showSquareLabels = false;
        private bool _monochromeMode = false;
        private Color selectedSquareColor = Color.FromArgb(130, 186, 221, 97);
        private Color legalMoveColor = Color.FromArgb(100, 130, 151, 105);
        private Color lastMoveFromColor = Color.FromArgb(100, 255, 255, 100);
        private Color lastMoveToColor = Color.FromArgb(120, 255, 255, 100);

        // Last move tracking
        private (int fromRow, int fromCol, int toRow, int toCol)? lastMove;

        // Piece move animation
        private bool _animating = false;
        private char _animPiece = '.';
        private int _animFromRow, _animFromCol, _animToRow, _animToCol;
        private float _animProgress = 0f;
        private float _animDurationMs = 150f;
        public int AnimationDurationMs
        {
            get => (int)_animDurationMs;
            set => _animDurationMs = Math.Max(50, Math.Min(500, value));
        }
        private readonly System.Windows.Forms.Timer _animTimer;

        // Move annotation badge (e.g. "!!", "?", "??")
        private string _moveAnnotationSymbol = "";
        private int _annotationRow = -1;
        private int _annotationCol = -1;

        // Piece images — source bitmaps + pre-scaled cache for fast drag painting
        private Dictionary<char, Image?> pieceImages = new Dictionary<char, Image?>();
        private Dictionary<char, Bitmap?> _scaledImages = new();
        private int _lastScaledSquareSize = 0;
        private string currentTemplateSet = "Lichess";

        // Font for coordinates (fallback for pieces if images fail)
        private Font coordFont = new Font("Segoe UI", 10f, FontStyle.Bold);

        // Cached font for fallback Unicode piece rendering (prevents GDI+ leak)
        private Font? pieceFallbackFont;
        private float lastPieceFontSize = 0f;

        // Cached fonts for OnPaint — recreated only when squareSize changes
        private Font? _labelFont;
        private Font? _badgeFont;
        private static readonly System.Drawing.StringFormat _badgeSf = new System.Drawing.StringFormat
        {
            Alignment = System.Drawing.StringAlignment.Center,
            LineAlignment = System.Drawing.StringAlignment.Center
        };

        // Cached GDI+ objects for OnPaint — reused every frame, color updated before each use
        private readonly SolidBrush _paintBrush = new SolidBrush(Color.Black);
        private readonly Pen _legalMovePen = new Pen(Color.Black, 3);
        private readonly Pen _arrowPen = new Pen(Color.Black, 1f) { EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor };
        private readonly Pen _gridPen  = new Pen(Color.FromArgb(160, 160, 160), 1f);

        /// <summary>
        /// When false, disables mouse interaction (for engine-vs-engine matches).
        /// </summary>
        public bool InteractionEnabled { get; set; } = true;

        public bool TrainingMode
        {
            get => _trainingMode;
            set { _trainingMode = value; _hoverRow = -1; _hoverCol = -1; Invalidate(); }
        }

        public bool HoverSquareLabelEnabled
        {
            get => _hoverSquareLabelEnabled;
            set { _hoverSquareLabelEnabled = value; Invalidate(); }
        }

        public bool RainbowMode
        {
            get => _rainbowMode;
            set { _rainbowMode = value; if (value) _waveMode = false; UpdateHueTimer(); }
        }
        public bool WaveMode
        {
            get => _waveMode;
            set { _waveMode = value; if (value) _rainbowMode = false; UpdateHueTimer(); }
        }
        private void UpdateHueTimer()
        {
            if (_rainbowMode || _waveMode) _rainbowTimer.Start();
            else { _rainbowTimer.Stop(); Invalidate(); }
        }

        public bool GradientBoard
        {
            get => _gradientBoard;
            set
            {
                _gradientBoard = value;
                if (value && _lastScaledSquareSize > 0) RegenerateGradientBitmaps(_lastScaledSquareSize);
                Invalidate();
            }
        }
        public bool VignetteEnabled  { get => _vignetteEnabled;  set { _vignetteEnabled  = value; Invalidate(); } }
        public int  VignetteAlpha    { get => _vignetteAlpha;    set { _vignetteAlpha    = Math.Clamp(value, 0, 255); Invalidate(); } }
        public bool PieceGlowEnabled
        {
            get => _pieceGlowEnabled;
            set
            {
                _pieceGlowEnabled = value;
                if (value && _lastScaledSquareSize > 0) RegenerateGlowBitmaps(_lastScaledSquareSize);
                Invalidate();
            }
        }

        public bool  BoardFrameEnabled { get => _boardFrameEnabled; set { _boardFrameEnabled = value; Invalidate(); } }
        public int   BoardFrameWidth   { get => _boardFrameWidth;   set { _boardFrameWidth   = value; Invalidate(); } }
        public Color BoardFrameColor   { get => _boardFrameColor;   set { _boardFrameColor   = value; Invalidate(); } }

        // Events
        public event EventHandler<MoveEventArgs>? MoveMade;
        public event EventHandler? BoardChanged;
        public event EventHandler? AnimationCompleted;
        public event Action<int, int>? SquareClicked;

        public bool ShowLastMoveHighlight { get; set; } = true;

        private const string STARTING_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public ChessBoardControl()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, // Redraw when resized
                true);

            board = ChessBoard.FromFEN(STARTING_FEN);

            // Load piece images
            LoadPieceImages("Lichess");

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += AnimTimer_Tick;

            _rainbowTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _rainbowTimer.Tick += RainbowTimer_Tick;

            _particleTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _particleTimer.Tick += ParticleTimer_Tick;
        }

        /// <summary>
        /// Sets the template set to use (Lichess or Chess.com)
        /// </summary>
        public void SetTemplateSet(string templateSet)
        {
            if (templateSet != currentTemplateSet)
            {
                currentTemplateSet = templateSet;
                LoadPieceImages(templateSet);
                Invalidate();
            }
        }

        private void LoadPieceImages(string templateSet)
        {
            foreach (var img in pieceImages.Values)
                img?.Dispose();
            pieceImages.Clear();
            foreach (var bmp in _scaledImages.Values) bmp?.Dispose();
            _scaledImages.Clear();
            _lastScaledSquareSize = 0;

            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", templateSet);

            // stem = color prefix + uppercase piece letter (e.g. "wK", "bP")
            var pieceStems = new Dictionary<char, string>
            {
                { 'K', "wK" }, { 'Q', "wQ" }, { 'R', "wR" },
                { 'B', "wB" }, { 'N', "wN" }, { 'P', "wP" },
                { 'k', "bK" }, { 'q', "bQ" }, { 'r', "bR" },
                { 'b', "bB" }, { 'n', "bN" }, { 'p', "bP" }
            };

            foreach (var kvp in pieceStems)
                pieceImages[kvp.Key] = TryLoadPiece(basePath, kvp.Value);
        }

        private static Image? TryLoadPiece(string basePath, string stem)
        {
            // 1. PNG — existing sets (wK.png)
            string png = Path.Combine(basePath, stem + ".png");
            if (File.Exists(png))
            {
                try { return Image.FromFile(png); }
                catch (Exception ex) { Debug.WriteLine($"Failed to load {png}: {ex.Message}"); }
            }

            // 2. SVG — Lichess convention (wK.svg, uppercase piece)
            string svg = Path.Combine(basePath, stem + ".svg");
            if (File.Exists(svg))
                return RenderSvg(svg);

            // 3. SVG — PyChess convention (wk.svg, all lowercase)
            string svgLower = Path.Combine(basePath, stem.ToLowerInvariant() + ".svg");
            if (File.Exists(svgLower))
                return RenderSvg(svgLower);

            Debug.WriteLine($"Piece not found: {stem} in {basePath}");
            return null;
        }

        private static readonly System.Text.RegularExpressions.Regex _svgFilterAttr =
            new(@"\s+filter=""url\([^)]+\)""", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static Image? RenderSvg(string path)
        {
            try
            {
                // Strip filter="url(#...)" attributes before rendering — the Svg library
                // mishandles chained filter primitives (feComposite/feFlood/feOffset) and
                // produces garbage output. Pieces render correctly without the drop-shadow filters.
                string xml = File.ReadAllText(path);
                xml = _svgFilterAttr.Replace(xml, "");
                var doc = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(xml);
                return doc.Draw(256, 256);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to render SVG {path}: {ex.Message}");
                return null;
            }
        }

        // Pre-scales all piece images to the actual draw size so DrawImage is a 1:1 copy every frame.
        // Called once per squareSize change — bicubic quality here, zero cost at paint time.
        private void RescaleImages(int squareSize)
        {
            foreach (var bmp in _scaledImages.Values) bmp?.Dispose();
            _scaledImages.Clear();

            int sz = squareSize - 2 * (squareSize / 16); // mirrors DrawPiece padding
            if (sz <= 0) { _lastScaledSquareSize = squareSize; return; }

            foreach (var kvp in pieceImages)
            {
                if (kvp.Value == null) { _scaledImages[kvp.Key] = null; continue; }
                var bmp = new Bitmap(sz, sz, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using var gfx = Graphics.FromImage(bmp);
                gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                gfx.DrawImage(kvp.Value, 0, 0, sz, sz);
                _scaledImages[kvp.Key] = bmp;
            }
            _lastScaledSquareSize = squareSize;

            if (_gradientBoard)    RegenerateGradientBitmaps(squareSize);
            if (_pieceGlowEnabled) RegenerateGlowBitmaps(squareSize);
        }

        private void RegenerateGradientBitmaps(int squareSize)
        {
            _gradientLightBitmap?.Dispose();
            _gradientDarkBitmap?.Dispose();
            _gradientLightBitmap = RenderGradientSquare(squareSize, lightSquareColor);
            _gradientDarkBitmap  = RenderGradientSquare(squareSize, darkSquareColor);
        }

        private static Bitmap RenderGradientSquare(int size, Color color)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using var gfx = Graphics.FromImage(bmp);
            var rect = new Rectangle(0, 0, size, size);
            using var lgb = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, LightenColor(color, 0.12f), DarkenColor(color, 0.12f), 45f);
            gfx.FillRectangle(lgb, rect);
            return bmp;
        }

        private void RegenerateGlowBitmaps(int squareSize)
        {
            _glowWhiteBitmap?.Dispose();
            _glowBlackBitmap?.Dispose();
            _glowWhiteBitmap = RenderGlowBitmap(squareSize, Color.FromArgb(255, 215, 0));
            _glowBlackBitmap = RenderGlowBitmap(squareSize, Color.FromArgb(64, 144, 255));
        }

        private static Bitmap RenderGlowBitmap(int size, Color glowColor)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var gfx = Graphics.FromImage(bmp);
            gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, size, size);
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(rect);
            using var pgb = new System.Drawing.Drawing2D.PathGradientBrush(path);
            pgb.CenterColor    = Color.FromArgb(80, glowColor);
            pgb.SurroundColors = new[] { Color.FromArgb(0, glowColor) };
            gfx.FillEllipse(pgb, rect);
            return bmp;
        }

        #region Public Properties

        /// <summary>
        /// Gets or sets the current board position
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ChessBoard Board
        {
            get => board;
            set
            {
                board = value;
                ClearSelection();
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets whether the board is flipped (black at bottom)
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsFlipped
        {
            get => isFlipped;
            set
            {
                isFlipped = value;
                Invalidate();
            }
        }

        public bool ShowSquareLabels
        {
            get => showSquareLabels;
            set { if (showSquareLabels != value) { showSquareLabels = value; Invalidate(); } }
        }

        public bool MonochromeMode
        {
            get => _monochromeMode;
            set { if (_monochromeMode != value) { _monochromeMode = value; Invalidate(); } }
        }

        private bool _hideCoordinates = false;
        public bool HideCoordinates
        {
            get => _hideCoordinates;
            set { if (_hideCoordinates != value) { _hideCoordinates = value; Invalidate(); } }
        }

        public ChessBoard GetBoardState() => board;

        /// <summary>
        /// Gets or sets whether it's white's turn to move
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool WhiteToMove
        {
            get => whiteToMove;
            set
            {
                whiteToMove = value;
                ClearSelection();
            }
        }

        /// <summary>
        /// Gets or sets the last move for highlighting
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public (int fromRow, int fromCol, int toRow, int toCol)? LastMove
        {
            get => lastMove;
            set
            {
                lastMove = value;
                Invalidate();
            }
        }

        public void SetSquareColors(Color light, Color dark)
        {
            lightSquareColor = light;
            darkSquareColor = dark;
            if (_gradientBoard && _lastScaledSquareSize > 0) RegenerateGradientBitmaps(_lastScaledSquareSize);
            Invalidate();
        }

        public void SetMoveAnnotation(string symbol, int row, int col)
        {
            _moveAnnotationSymbol = symbol;
            _annotationRow = row;
            _annotationCol = col;
            Invalidate();
        }

        public void ClearMoveAnnotation()
        {
            _moveAnnotationSymbol = "";
            _annotationRow = -1;
            _annotationCol = -1;
            Invalidate();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the board to starting position
        /// </summary>
        public void ResetBoard()
        {
            board = ChessBoard.FromFEN(STARTING_FEN);
            whiteToMove = true;
            whiteKingsideCastle = true;
            whiteQueensideCastle = true;
            blackKingsideCastle = true;
            blackQueensideCastle = true;
            enPassantRow = -1;
            enPassantCol = -1;
            lastMove = null;
            _threatArrows.Clear();
            ClearSelection();
            Invalidate();
            BoardChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Loads a position from FEN string
        /// </summary>
        public void LoadFEN(string fen)
        {
            _animating = false;
            _animTimer?.Stop();
            try
            {
                userArrows.Clear();
                _highlightedSquares.Clear();
                _threatArrows.Clear();
                board = ChessBoard.FromFEN(fen);
                string[] parts = fen.Split(' ');
                whiteToMove = parts.Length > 1 ? parts[1] == "w" : true;

                // Parse castling rights
                string castling = parts.Length > 2 ? parts[2] : "KQkq";
                whiteKingsideCastle = castling.Contains('K');
                whiteQueensideCastle = castling.Contains('Q');
                blackKingsideCastle = castling.Contains('k');
                blackQueensideCastle = castling.Contains('q');

                // Parse en passant square
                string epSquare = parts.Length > 3 ? parts[3] : "-";
                if (epSquare != "-" && epSquare.Length == 2)
                {
                    enPassantCol = epSquare[0] - 'a';
                    enPassantRow = 8 - (epSquare[1] - '0');
                }
                else
                {
                    enPassantRow = -1;
                    enPassantCol = -1;
                }

                lastMove = null;
                ClearMoveAnnotation();
                ClearSelection();
                Invalidate();
                BoardChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading FEN: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current position as FEN string
        /// </summary>
        public string GetFEN()
        {
            string boardFen = board.ToFEN();
            string turn = whiteToMove ? "w" : "b";

            // Build castling rights string
            string castling = "";
            if (whiteKingsideCastle) castling += "K";
            if (whiteQueensideCastle) castling += "Q";
            if (blackKingsideCastle) castling += "k";
            if (blackQueensideCastle) castling += "q";
            if (string.IsNullOrEmpty(castling)) castling = "-";

            // Build en passant square string
            string epSquare = "-";
            if (enPassantRow >= 0 && enPassantCol >= 0)
            {
                epSquare = $"{(char)('a' + enPassantCol)}{8 - enPassantRow}";
            }

            return $"{boardFen} {turn} {castling} {epSquare} 0 1";
        }

        /// <summary>
        /// Flips the board orientation
        /// </summary>
        public void FlipBoard()
        {
            IsFlipped = !IsFlipped;
        }

        /// <summary>
        /// Clears all user-drawn arrows from the board.
        /// </summary>
        public void ClearArrows()
        {
            if (userArrows.Count > 0)
            {
                userArrows.Clear();
                Invalidate();
            }
        }

        public void SetEngineArrows(List<(int fromRow, int fromCol, int toRow, int toCol, Color color)> arrows)
        {
            engineArrows = arrows;
            Invalidate();
        }

        public void ClearEngineArrows()
        {
            if (engineArrows.Count > 0)
            {
                engineArrows.Clear();
                Invalidate();
            }
        }

        public void SetBookArrows(IEnumerable<(int fromRow, int fromCol, int toRow, int toCol, Color color)> arrows)
        {
            _bookArrows = arrows.ToList();
            Invalidate();
        }

        public void ClearBookArrows()
        {
            if (_bookArrows.Count > 0)
            {
                _bookArrows.Clear();
                Invalidate();
            }
        }

        public void SetThreatArrows(IEnumerable<(int fromRow, int fromCol, int toRow, int toCol)> arrows)
        {
            _threatArrows = arrows.Select(a => (a.fromRow, a.fromCol, a.toRow, a.toCol, ThreatArrowColor)).ToList();
            Invalidate();
        }

        public void ClearThreatArrows()
        {
            if (_threatArrows.Count > 0)
            {
                _threatArrows.Clear();
                Invalidate();
            }
        }

        public void SetTrainingHighlight(int row, int col, Color color)
        {
            _trainingHighlightRow = row;
            _trainingHighlightCol = col;
            _trainingHighlightColor = color;
            Invalidate();
        }

        public void ClearTrainingHighlight()
        {
            _trainingHighlightRow = -1;
            _trainingHighlightCol = -1;
            Invalidate();
        }

        /// <summary>
        /// Gets the piece character at the given internal coordinates (0=rank8, 7=rank1).
        /// </summary>
        public char GetPieceAt(int row, int col)
        {
            if (row < 0 || row > 7 || col < 0 || col > 7) return '.';
            return board.GetPiece(row, col);
        }

        /// <summary>
        /// Makes a move on the board (for external use, e.g., from move list)
        /// </summary>
        public bool MakeMove(string uciMove)
        {
            if (string.IsNullOrEmpty(uciMove) || uciMove.Length < 4)
                return false;

            int fromCol = uciMove[0] - 'a';
            int fromRow = uciMove[1] - '1';
            int toCol = uciMove[2] - 'a';
            int toRow = uciMove[3] - '1';

            // Convert to internal coordinates (0 = rank 8, 7 = rank 1)
            int srcRow = 7 - fromRow;
            int srcCol = fromCol;
            int destRow = 7 - toRow;
            int destCol = toCol;

            char? promotion = uciMove.Length > 4 ? uciMove[4] : null;

            return ExecuteMove(srcRow, srcCol, destRow, destCol, promotion);
        }

        /// <summary>
        /// Starts a smooth slide animation for the piece that just moved via uciMove.
        /// Must be called AFTER the move has been applied to the board state.
        /// </summary>
        public void StartAnimation(string uciMove)
        {
            if (uciMove.Length < 4) return;
            int fromCol = uciMove[0] - 'a';
            int fromRow = 7 - (uciMove[1] - '1');
            int toCol   = uciMove[2] - 'a';
            int toRow   = 7 - (uciMove[3] - '1');

            char movedPiece = board.GetPiece(toRow, toCol);

            // Castling: king moves 2 squares — animate the rook sliding over instead.
            // In real chess you move the king first then grab the rook, so the rook is
            // what visually "travels" across the board.
            bool isCastling = (movedPiece == 'K' || movedPiece == 'k')
                              && fromRow == toRow
                              && Math.Abs(fromCol - toCol) == 2;
            if (isCastling)
            {
                bool kingside  = toCol > fromCol;
                int rookFromCol = kingside ? 7 : 0;
                int rookToCol   = kingside ? toCol - 1 : toCol + 1;
                _animFromRow = fromRow; _animFromCol = rookFromCol;
                _animToRow   = toRow;   _animToCol   = rookToCol;
                _animPiece   = board.GetPiece(toRow, rookToCol);
            }
            else
            {
                if (movedPiece == '.') return;
                _animFromRow = fromRow; _animFromCol = fromCol;
                _animToRow   = toRow;   _animToCol   = toCol;
                _animPiece   = movedPiece;
            }

            if (_animPiece == '.') return;
            _animProgress = 0f;
            _animating = true;
            _animTimer.Start();
        }

        private void AnimTimer_Tick(object? sender, EventArgs e)
        {
            _animProgress += 16f / _animDurationMs;
            if (_animProgress >= 1f)
            {
                _animProgress = 1f;
                _animating = false;
                _animTimer.Stop();
                AnimationCompleted?.Invoke(this, EventArgs.Empty);
            }
            Invalidate();
        }

        private void RainbowTimer_Tick(object? sender, EventArgs e)
        {
            _rainbowHue = (_rainbowHue + 0.4f) % 360f;
            Invalidate();
        }

        // Converts HSV (h=0-360, s=0-1, v=0-1) to a GDI Color.
        private static Color HsvToRgb(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1f - MathF.Abs(h / 60f % 2f - 1f));
            float m = v - c;
            float r, g, b;
            if      (h < 60f)  { r = c; g = x; b = 0; }
            else if (h < 120f) { r = x; g = c; b = 0; }
            else if (h < 180f) { r = 0; g = c; b = x; }
            else if (h < 240f) { r = 0; g = x; b = c; }
            else if (h < 300f) { r = x; g = 0; b = c; }
            else               { r = c; g = 0; b = x; }
            return Color.FromArgb(
                Math.Clamp((int)((r + m) * 255), 0, 255),
                Math.Clamp((int)((g + m) * 255), 0, 255),
                Math.Clamp((int)((b + m) * 255), 0, 255));
        }

        public void TriggerParticles()
        {
            _particles.Clear();
            _particleFont?.Dispose();
            _particleFont = new Font("Segoe UI", 18f, FontStyle.Bold);
            int cx = Width / 2, cy = Height / 2;
            for (int i = 0; i < 55; i++)
            {
                float angle = (float)(_particleRng.NextDouble() * Math.PI * 2);
                float speed = (float)(_particleRng.NextDouble() * 9 + 2);
                _particles.Add(new Particle
                {
                    X = cx, Y = cy,
                    VX = MathF.Cos(angle) * speed,
                    VY = MathF.Sin(angle) * speed - 5f,
                    Life = 1.0f,
                    Size = (float)(_particleRng.NextDouble() * 14 + 10),
                    Color = Color.FromArgb(
                        _particleRng.Next(120, 256),
                        _particleRng.Next(120, 256),
                        _particleRng.Next(120, 256)),
                    Symbol = _particleSymbols[_particleRng.Next(_particleSymbols.Length)]
                });
            }
            _particleTimer.Start();
            Invalidate();
        }

        private void ParticleTimer_Tick(object? sender, EventArgs e)
        {
            bool any = false;
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X  += p.VX; p.Y  += p.VY;
                p.VY += 0.35f;          // gravity
                p.Life -= 0.018f;
                if (p.Life <= 0) _particles.RemoveAt(i);
                else { _particles[i] = p; any = true; }
            }
            if (!any)
            {
                _particleTimer.Stop();
                _particleFont?.Dispose();
                _particleFont = null;
            }
            Invalidate();
        }

        private static Color LightenColor(Color c, float amt) => Color.FromArgb(
            Math.Clamp((int)(c.R + 255 * amt), 0, 255),
            Math.Clamp((int)(c.G + 255 * amt), 0, 255),
            Math.Clamp((int)(c.B + 255 * amt), 0, 255));
        private static Color DarkenColor(Color c, float amt) => Color.FromArgb(
            Math.Clamp((int)(c.R - 255 * amt), 0, 255),
            Math.Clamp((int)(c.G - 255 * amt), 0, 255),
            Math.Clamp((int)(c.B - 255 * amt), 0, 255));

        #endregion

        #region Painting

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            int frameOff   = _boardFrameEnabled ? _boardFrameWidth : 0;
            int squareSize = Math.Min(Width - 2 * frameOff, Height - 2 * frameOff) / 8;
            if (squareSize < 1) squareSize = 1;

            // Draw board frame background
            if (_boardFrameEnabled)
            {
                _paintBrush.Color = _boardFrameColor;
                g.FillRectangle(_paintBrush, 0, 0, Width, Height);
            }

            // Draw squares and pieces
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    int displayRow = isFlipped ? 7 - row : row;
                    int displayCol = isFlipped ? 7 - col : col;

                    Rectangle rect = new Rectangle(
                        frameOff + displayCol * squareSize,
                        frameOff + displayRow * squareSize,
                        squareSize, squareSize);

                    // Determine square color
                    bool isLightSquare = (row + col) % 2 == 0;
                    Color squareColor;
                    if (_monochromeMode)
                    {
                        squareColor = Color.FromArgb(108, 108, 108);
                    }
                    else if (_rainbowMode || _waveMode)
                    {
                        float baseHue = _waveMode
                            ? (_rainbowHue + (row + col) * 20f) % 360f
                            : _rainbowHue;
                        float rHue = isLightSquare ? baseHue : (baseHue + 40f) % 360f;
                        squareColor = HsvToRgb(rHue,
                            isLightSquare ? 0.50f : 0.70f,
                            isLightSquare ? 0.88f : 0.58f);
                    }
                    else
                    {
                        squareColor = isLightSquare ? lightSquareColor : darkSquareColor;
                    }

                    // Highlight last move
                    if (lastMove.HasValue && ShowLastMoveHighlight)
                    {
                        if (row == lastMove.Value.fromRow && col == lastMove.Value.fromCol)
                            squareColor = BlendColors(squareColor, lastMoveFromColor);
                        else if (row == lastMove.Value.toRow && col == lastMove.Value.toCol)
                            squareColor = BlendColors(squareColor, lastMoveToColor);
                    }

                    // User-highlighted squares (right-click tap)
                    if (_highlightedSquares.Contains((row, col)))
                        squareColor = BlendColors(squareColor, SquareHighlightColor);

                    // Highlight selected square
                    if (row == selectedRow && col == selectedCol)
                        squareColor = BlendColors(squareColor, selectedSquareColor);

                    // Draw square (gradient or flat)
                    if (_gradientBoard)
                    {
                        bool isBase = squareColor == (isLightSquare ? lightSquareColor : darkSquareColor);
                        if (isBase && _gradientLightBitmap != null)
                        {
                            g.DrawImage(isLightSquare ? _gradientLightBitmap : _gradientDarkBitmap!, rect);
                        }
                        else
                        {
                            using var lgb = new System.Drawing.Drawing2D.LinearGradientBrush(
                                rect, LightenColor(squareColor, 0.12f), DarkenColor(squareColor, 0.12f), 45f);
                            g.FillRectangle(lgb, rect);
                        }
                    }
                    else
                    {
                        _paintBrush.Color = squareColor;
                        g.FillRectangle(_paintBrush, rect);
                    }

                    // Draw legal move indicators
                    if (legalMoveSquares.Contains((row, col)))
                    {
                        char pieceAtTarget = board.GetPiece(row, col);
                        if (pieceAtTarget != '.')
                        {
                            // Capture - draw ring
                            _legalMovePen.Color = legalMoveColor;
                            int margin = squareSize / 10;
                            g.DrawEllipse(_legalMovePen, rect.X + margin, rect.Y + margin,
                                rect.Width - 2 * margin, rect.Height - 2 * margin);
                        }
                        else
                        {
                            // Empty square - draw dot
                            _paintBrush.Color = legalMoveColor;
                            int dotSize = squareSize / 4;
                            int offset = (squareSize - dotSize) / 2;
                            g.FillEllipse(_paintBrush, rect.X + offset, rect.Y + offset, dotSize, dotSize);
                        }
                    }

                    // Draw piece (skip if being dragged or is the animation's in-flight piece)
                    char piece = board.GetPiece(row, col);
                    bool isBeingDragged = isDragging && row == dragFromRow && col == dragFromCol;
                    bool isInFlight = _animating && row == _animToRow && col == _animToCol;
                    if (piece != '.' && !isBeingDragged && !isInFlight)
                    {
                        if (_pieceGlowEnabled)
                        {
                            var glowBmp = char.IsUpper(piece) ? _glowWhiteBitmap : _glowBlackBitmap;
                            if (glowBmp != null) g.DrawImage(glowBmp, rect);
                        }
                        DrawPiece(g, piece, rect, squareSize);
                    }
                }
            }

            // Draw training highlight (colored flash over a square)
            if (_trainingHighlightRow >= 0)
            {
                int displayRow = isFlipped ? 7 - _trainingHighlightRow : _trainingHighlightRow;
                int displayCol = isFlipped ? 7 - _trainingHighlightCol : _trainingHighlightCol;
                _paintBrush.Color = _trainingHighlightColor;
                g.FillRectangle(_paintBrush,
                    frameOff + displayCol * squareSize, frameOff + displayRow * squareSize,
                    squareSize, squareSize);
            }

            // Draw user arrows
            DrawArrows(g, squareSize, frameOff);

            // Draw the animating piece floating above the board
            if (_animating)
            {
                float fx = frameOff + (isFlipped ? 7 - _animFromCol : _animFromCol) * squareSize;
                float fy = frameOff + (isFlipped ? 7 - _animFromRow : _animFromRow) * squareSize;
                float tx = frameOff + (isFlipped ? 7 - _animToCol   : _animToCol)   * squareSize;
                float ty = frameOff + (isFlipped ? 7 - _animToRow   : _animToRow)   * squareSize;
                float cx = fx + (tx - fx) * _animProgress;
                float cy = fy + (ty - fy) * _animProgress;
                DrawPiece(g, _animPiece, new Rectangle((int)cx, (int)cy, squareSize, squareSize), squareSize);
            }

            // Draw dragged piece at cursor (on top of everything)
            if (isDragging && dragPiece != '.')
            {
                int pieceSize = (int)(squareSize * 1.1); // Slightly larger when dragging
                Rectangle dragRect = new Rectangle(
                    dragPosition.X - pieceSize / 2,
                    dragPosition.Y - pieceSize / 2,
                    pieceSize,
                    pieceSize
                );
                DrawPiece(g, dragPiece, dragRect, squareSize);
            }

            // Draw coordinates
            float coordFontSize = Math.Max(8f, squareSize * 0.15f);
            if (Math.Abs(coordFont.Size - coordFontSize) > 0.5f)
            {
                coordFont.Dispose();
                coordFont = new Font("Segoe UI", coordFontSize, FontStyle.Bold);
            }

            // Monochrome grid lines
            if (_monochromeMode)
            {
                for (int i = 1; i < 8; i++)
                {
                    int px = frameOff + i * squareSize;
                    g.DrawLine(_gridPen, px, frameOff, px, frameOff + 8 * squareSize);
                    g.DrawLine(_gridPen, frameOff, px, frameOff + 8 * squareSize, px);
                }
            }

            if (!_hideCoordinates)
            {
                for (int i = 0; i < 8; i++)
                {
                    // File letters (a-h)
                    int fileIndex = isFlipped ? 7 - i : i;
                    string file = ((char)('a' + fileIndex)).ToString();
                    bool isLight = isFlipped ? fileIndex % 2 == 0 : (7 + fileIndex) % 2 == 0;
                    _paintBrush.Color = _monochromeMode ? Color.White
                        : (_rainbowMode || _waveMode)
                        ? HsvToRgb(isLight ? (_rainbowHue + 40f) % 360f : _rainbowHue,
                                   isLight ? 0.70f : 0.50f,
                                   isLight ? 0.58f : 0.88f)
                        : (_boardFrameEnabled ? LightenColor(_boardFrameColor, 0.45f)
                                             : (isLight ? darkSquareColor : lightSquareColor));
                    g.DrawString(file, coordFont, _paintBrush,
                        frameOff + i * squareSize + 2,
                        frameOff + 8 * squareSize - coordFont.Height - 2);

                    // Rank numbers (1-8)
                    int rankIndex = isFlipped ? i : 7 - i;
                    string rank = (rankIndex + 1).ToString();
                    isLight = i % 2 == 0;
                    _paintBrush.Color = _monochromeMode ? Color.White
                        : (_rainbowMode || _waveMode)
                        ? HsvToRgb(isLight ? (_rainbowHue + 40f) % 360f : _rainbowHue,
                                   isLight ? 0.70f : 0.50f,
                                   isLight ? 0.58f : 0.88f)
                        : (_boardFrameEnabled ? LightenColor(_boardFrameColor, 0.45f)
                                             : (isLight ? darkSquareColor : lightSquareColor));
                    g.DrawString(rank, coordFont, _paintBrush, frameOff + 2, frameOff + i * squareSize + 2);
                }
            }

            // Draw square name labels if enabled (e.g. "e4", "d5")
            if (showSquareLabels)
            {
                float labelFontSize = Math.Max(8f, squareSize * 0.20f);
                if (_labelFont == null || Math.Abs(_labelFont.Size - labelFontSize) > 0.1f)
                {
                    _labelFont?.Dispose();
                    _labelFont = new Font("Segoe UI", labelFontSize, FontStyle.Regular);
                }
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        int displayRow = isFlipped ? 7 - row : row;
                        int displayCol = isFlipped ? 7 - col : col;
                        bool isLightSq = (row + col) % 2 == 0;
                        Color baseColor = isLightSq ? darkSquareColor : lightSquareColor;
                        string squareName = $"{(char)('a' + col)}{8 - row}";
                        SizeF textSize = g.MeasureString(squareName, _labelFont);
                        float x = frameOff + displayCol * squareSize + (squareSize - textSize.Width) / 2f;
                        float y = frameOff + displayRow * squareSize + (squareSize - textSize.Height) / 2f;
                        _paintBrush.Color = Color.FromArgb(140, baseColor);
                        g.DrawString(squareName, _labelFont, _paintBrush, x, y);
                    }
                }
            }

            // Draw hover square label (training mode, easy difficulty)
            if (_trainingMode && _hoverSquareLabelEnabled && _hoverRow >= 0 && _hoverCol >= 0)
            {
                float hoverFontSize = Math.Max(14f, squareSize * 0.32f);
                if (_hoverLabelFont == null || Math.Abs(_hoverLabelFont.Size - hoverFontSize) > 0.1f)
                {
                    _hoverLabelFont?.Dispose();
                    _hoverLabelFont = new Font("Segoe UI", hoverFontSize, FontStyle.Bold);
                }
                int displayRow = isFlipped ? 7 - _hoverRow : _hoverRow;
                int displayCol = isFlipped ? 7 - _hoverCol : _hoverCol;
                string squareName = $"{(char)('a' + _hoverCol)}{8 - _hoverRow}";
                SizeF textSize = g.MeasureString(squareName, _hoverLabelFont);
                float x = frameOff + displayCol * squareSize + (squareSize - textSize.Width) / 2f;
                float y = frameOff + displayRow * squareSize + (squareSize - textSize.Height) / 2f;
                bool hoverLightSq = (_hoverRow + _hoverCol) % 2 == 0;
                _paintBrush.Color = hoverLightSq
                    ? Color.FromArgb(210, 40, 40, 40)
                    : Color.FromArgb(210, 220, 220, 220);
                g.DrawString(squareName, _hoverLabelFont, _paintBrush, x, y);
            }

            // Draw move annotation badge (e.g. "!!", "?", "??")
            if (!string.IsNullOrEmpty(_moveAnnotationSymbol) && _annotationRow >= 0)
            {
                int displayRow = isFlipped ? 7 - _annotationRow : _annotationRow;
                int displayCol = isFlipped ? 7 - _annotationCol : _annotationCol;

                Color badgeColor = _moveAnnotationSymbol switch
                {
                    "!!" => Color.FromArgb(26, 179, 148),   // teal  — brilliant
                    "!"  => Color.FromArgb(91, 139, 245),   // blue  — only move
                    "?!" => Color.FromArgb(247, 199, 72),   // yellow — inaccuracy
                    "?"  => Color.FromArgb(232, 106, 51),   // orange — mistake
                    "??" => Color.FromArgb(202, 52, 49),    // red   — blunder
                    _    => Color.FromArgb(150, 150, 150),
                };

                int badgeSize = Math.Max(14, squareSize / 3);
                int badgeX = frameOff + (displayCol + 1) * squareSize - badgeSize - 2;
                int badgeY = frameOff + displayRow * squareSize + 2;

                _paintBrush.Color = badgeColor;
                g.FillEllipse(_paintBrush, badgeX, badgeY, badgeSize, badgeSize);

                float fontSize = Math.Max(6f, badgeSize * 0.42f);
                if (_badgeFont == null || Math.Abs(_badgeFont.Size - fontSize) > 0.1f)
                {
                    _badgeFont?.Dispose();
                    _badgeFont = new Font("Segoe UI", fontSize, FontStyle.Bold);
                }
                _paintBrush.Color = Color.White;
                g.DrawString(_moveAnnotationSymbol, _badgeFont, _paintBrush,
                    new RectangleF(badgeX, badgeY, badgeSize, badgeSize), _badgeSf);
            }

            // Vignette — four gradient strips fading inward from each edge
            if (_vignetteEnabled)
            {
                int boardPx = squareSize * 8;
                int vw = Math.Max(1, boardPx / 5);
                var dark = Color.FromArgb(_vignetteAlpha, 0, 0, 0);
                DrawVignetteStrip(g, frameOff, frameOff, boardPx, vw,
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical, dark, Color.Transparent);           // top
                DrawVignetteStrip(g, frameOff, frameOff + boardPx - vw, boardPx, vw,
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical, Color.Transparent, dark);           // bottom
                DrawVignetteStrip(g, frameOff, frameOff, vw, boardPx,
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal, dark, Color.Transparent);         // left
                DrawVignetteStrip(g, frameOff + boardPx - vw, frameOff, vw, boardPx,
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal, Color.Transparent, dark);         // right
            }

            // Particles — game-end celebration
            if (_particles.Count > 0 && _particleFont != null)
            {
                foreach (var p in _particles)
                {
                    _paintBrush.Color = Color.FromArgb((int)(p.Life * 230), p.Color);
                    g.DrawString(p.Symbol.ToString(), _particleFont, _paintBrush, p.X, p.Y);
                }
            }

        }

        private static void DrawVignetteStrip(Graphics g, int x, int y, int w, int h,
            System.Drawing.Drawing2D.LinearGradientMode mode, Color c1, Color c2)
        {
            using var lgb = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(x, y, w, h), c1, c2, mode);
            g.FillRectangle(lgb, x, y, w, h);
        }

        private void DrawArrows(Graphics g, int squareSize, int frameOff)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var arrow in _bookArrows)
                DrawArrow(g, squareSize, frameOff, arrow.fromRow, arrow.fromCol, arrow.toRow, arrow.toCol, arrow.color);
            foreach (var arrow in _threatArrows)
                DrawArrow(g, squareSize, frameOff, arrow.fromRow, arrow.fromCol, arrow.toRow, arrow.toCol, arrow.color);
            foreach (var arrow in engineArrows)
                DrawArrow(g, squareSize, frameOff, arrow.fromRow, arrow.fromCol, arrow.toRow, arrow.toCol, arrow.color);
            foreach (var arrow in userArrows)
                DrawArrow(g, squareSize, frameOff, arrow.fromRow, arrow.fromCol, arrow.toRow, arrow.toCol, ArrowColor);

            if (isDrawingArrow && arrowFromRow >= 0)
            {
                int hoverCol = (arrowDragPosition.X - frameOff) / squareSize;
                int hoverRow = (arrowDragPosition.Y - frameOff) / squareSize;
                int hoverBoardRow = isFlipped ? 7 - hoverRow : hoverRow;
                int hoverBoardCol = isFlipped ? 7 - hoverCol : hoverCol;
                if (hoverBoardRow >= 0 && hoverBoardRow <= 7 && hoverBoardCol >= 0 && hoverBoardCol <= 7
                    && !(hoverBoardRow == arrowFromRow && hoverBoardCol == arrowFromCol))
                    DrawArrow(g, squareSize, frameOff, arrowFromRow, arrowFromCol, hoverBoardRow, hoverBoardCol, ArrowColor);
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
        }

        private void DrawArrow(Graphics g, int squareSize, int frameOff, int fromRow, int fromCol, int toRow, int toCol, Color color)
        {
            int x1 = frameOff + (isFlipped ? 7 - fromCol : fromCol) * squareSize + squareSize / 2;
            int y1 = frameOff + (isFlipped ? 7 - fromRow : fromRow) * squareSize + squareSize / 2;
            int x2 = frameOff + (isFlipped ? 7 - toCol   : toCol)   * squareSize + squareSize / 2;
            int y2 = frameOff + (isFlipped ? 7 - toRow   : toRow)   * squareSize + squareSize / 2;

            float lineWidth = squareSize * 0.22f;
            _arrowPen.Color = color;
            _arrowPen.Width = lineWidth;
            g.DrawLine(_arrowPen, x1, y1, x2, y2);
        }

        private void DrawPiece(Graphics g, char piece, Rectangle rect, int squareSize)
        {
            // Rebuild scaled cache if squareSize changed (window resize or first paint)
            if (_lastScaledSquareSize != squareSize)
                RescaleImages(squareSize);

            // Try to draw from pre-scaled cache — 1:1 pixel copy, no per-frame scaling cost
            if (_scaledImages.TryGetValue(piece, out Bitmap? scaled) && scaled != null)
            {
                int padding = squareSize / 16;
                Rectangle pieceRect = new Rectangle(
                    rect.X + padding,
                    rect.Y + padding,
                    rect.Width - 2 * padding,
                    rect.Height - 2 * padding
                );
                g.DrawImage(scaled, pieceRect);
            }
            else
            {
                // Fallback to Unicode if image not available
                // Use cached font to prevent GDI+ handle leaks
                float targetFontSize = squareSize * 0.7f;
                if (pieceFallbackFont == null || Math.Abs(lastPieceFontSize - targetFontSize) > 0.5f)
                {
                    pieceFallbackFont?.Dispose();
                    pieceFallbackFont = new Font("Segoe UI Symbol", targetFontSize, FontStyle.Regular);
                    lastPieceFontSize = targetFontSize;
                }

                string pieceChar = GetPieceUnicode(piece);
                using (SolidBrush brush = new SolidBrush(char.IsUpper(piece) ? Color.White : Color.Black))
                {
                    // Add shadow for white pieces
                    if (char.IsUpper(piece))
                    {
                        using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                        {
                            SizeF textSize = g.MeasureString(pieceChar, pieceFallbackFont);
                            float x = rect.X + (rect.Width - textSize.Width) / 2 + 1;
                            float y = rect.Y + (rect.Height - textSize.Height) / 2 + 1;
                            g.DrawString(pieceChar, pieceFallbackFont, shadowBrush, x, y);
                        }
                    }

                    SizeF size = g.MeasureString(pieceChar, pieceFallbackFont);
                    float px = rect.X + (rect.Width - size.Width) / 2;
                    float py = rect.Y + (rect.Height - size.Height) / 2;
                    g.DrawString(pieceChar, pieceFallbackFont, brush, px, py);
                }
            }
        }

        private string GetPieceUnicode(char piece)
        {
            return piece switch
            {
                'K' => "\u2654", // ♔
                'Q' => "\u2655", // ♕
                'R' => "\u2656", // ♖
                'B' => "\u2657", // ♗
                'N' => "\u2658", // ♘
                'P' => "\u2659", // ♙
                'k' => "\u265A", // ♚
                'q' => "\u265B", // ♛
                'r' => "\u265C", // ♜
                'b' => "\u265D", // ♝
                'n' => "\u265E", // ♞
                'p' => "\u265F", // ♟
                _ => ""
            };
        }

        private Color BlendColors(Color background, Color overlay)
        {
            int alpha = overlay.A;
            int r = (overlay.R * alpha + background.R * (255 - alpha)) / 255;
            int g = (overlay.G * alpha + background.G * (255 - alpha)) / 255;
            int b = (overlay.B * alpha + background.B * (255 - alpha)) / 255;
            return Color.FromArgb(r, g, b);
        }

        #endregion

        #region Mouse Interaction

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (_trainingMode)
            {
                if (e.Button == MouseButtons.Left)
                {
                    int fo = GetFrameOff();
                    int sq = Math.Max(1, Math.Min(Width - 2 * fo, Height - 2 * fo) / 8);
                    int bRow = isFlipped ? 7 - (e.Y - fo) / sq : (e.Y - fo) / sq;
                    int bCol = isFlipped ? 7 - (e.X - fo) / sq : (e.X - fo) / sq;
                    if (bRow >= 0 && bRow <= 7 && bCol >= 0 && bCol <= 7)
                        SquareClicked?.Invoke(bRow, bCol);
                }
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                HandleRightMouseDown(e);
                return;
            }

            // Left-click clears arrows and square highlights
            if (e.Button == MouseButtons.Left && (userArrows.Count > 0 || _highlightedSquares.Count > 0))
            {
                userArrows.Clear();
                _highlightedSquares.Clear();
                Invalidate();
            }

            if (!InteractionEnabled) return;
            if (e.Button != MouseButtons.Left) return;

            int fo2 = GetFrameOff();
            int squareSize = Math.Max(1, Math.Min(Width - 2 * fo2, Height - 2 * fo2) / 8);
            int clickCol = (e.X - fo2) / squareSize;
            int clickRow = (e.Y - fo2) / squareSize;

            int boardRow = isFlipped ? 7 - clickRow : clickRow;
            int boardCol = isFlipped ? 7 - clickCol : clickCol;

            if (boardRow < 0 || boardRow > 7 || boardCol < 0 || boardCol > 7)
                return;

            char clickedPiece = board.GetPiece(boardRow, boardCol);
            bool isOwnPiece = clickedPiece != '.' &&
                ((whiteToMove && char.IsUpper(clickedPiece)) ||
                 (!whiteToMove && char.IsLower(clickedPiece)));

            mouseDownPosition = e.Location;
            mouseDownOnPiece = isOwnPiece;

            if (isOwnPiece)
            {
                // Prepare for potential drag
                dragFromRow = boardRow;
                dragFromCol = boardCol;
                dragPiece = clickedPiece;

                // Also select the piece for click-to-move fallback
                SelectPiece(boardRow, boardCol);
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Button == MouseButtons.Right && isDrawingArrow)
            {
                arrowDragPosition = e.Location;
                Invalidate();
                return;
            }

            if (_trainingMode)
            {
                if (_hoverSquareLabelEnabled)
                {
                    int fo = GetFrameOff();
                    int squareSize = Math.Max(1, Math.Min(Width - 2 * fo, Height - 2 * fo) / 8);
                    int bRow = isFlipped ? 7 - (e.Y - fo) / squareSize : (e.Y - fo) / squareSize;
                    int bCol = isFlipped ? 7 - (e.X - fo) / squareSize : (e.X - fo) / squareSize;
                    if (bRow >= 0 && bRow <= 7 && bCol >= 0 && bCol <= 7)
                    {
                        if (bRow != _hoverRow || bCol != _hoverCol)
                        {
                            _hoverRow = bRow;
                            _hoverCol = bCol;
                            Invalidate();
                        }
                    }
                    else if (_hoverRow >= 0)
                    {
                        _hoverRow = -1;
                        _hoverCol = -1;
                        Invalidate();
                    }
                }
                return;
            }

            if (!InteractionEnabled) return;

            if (e.Button != MouseButtons.Left || !mouseDownOnPiece)
                return;

            // Check if we've exceeded the drag threshold
            if (!isDragging)
            {
                int dx = Math.Abs(e.X - mouseDownPosition.X);
                int dy = Math.Abs(e.Y - mouseDownPosition.Y);

                if (dx > DRAG_THRESHOLD || dy > DRAG_THRESHOLD)
                    isDragging = true;
            }

            if (isDragging)
            {
                dragPosition = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Right && isDrawingArrow)
            {
                HandleRightMouseUp(e);
                return;
            }

            if (!InteractionEnabled) return;
            if (e.Button != MouseButtons.Left) return;

            int fo3 = GetFrameOff();
            int squareSize = Math.Max(1, Math.Min(Width - 2 * fo3, Height - 2 * fo3) / 8);
            int clickCol = (e.X - fo3) / squareSize;
            int clickRow = (e.Y - fo3) / squareSize;

            int boardRow = isFlipped ? 7 - clickRow : clickRow;
            int boardCol = isFlipped ? 7 - clickCol : clickCol;

            if (isDragging)
            {
                // End drag - try to execute move
                isDragging = false;

                if (boardRow >= 0 && boardRow <= 7 && boardCol >= 0 && boardCol <= 7)
                {
                    if (legalMoveSquares.Contains((boardRow, boardCol)))
                    {
                        ExecuteMove(dragFromRow, dragFromCol, boardRow, boardCol, null);
                    }
                    else
                    {
                        // Invalid drop - just redraw
                        Invalidate();
                    }
                }
                else
                {
                    // Dropped outside board
                    Invalidate();
                }

                dragFromRow = -1;
                dragFromCol = -1;
                dragPiece = '.';
            }
            else if (mouseDownOnPiece)
            {
                // Click-to-move: if we released on the same square, keep selection
                // If released on a different square, try to move or select
                if (boardRow >= 0 && boardRow <= 7 && boardCol >= 0 && boardCol <= 7)
                {
                    HandleSquareClick(boardRow, boardCol);
                }
            }
            else
            {
                // Clicked on empty or opponent piece
                if (boardRow >= 0 && boardRow <= 7 && boardCol >= 0 && boardCol <= 7)
                {
                    HandleSquareClick(boardRow, boardCol);
                }
            }

            mouseDownOnPiece = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_trainingMode && _hoverRow >= 0)
            {
                _hoverRow = -1;
                _hoverCol = -1;
                if (_hoverSquareLabelEnabled) Invalidate();
            }
        }

        private int GetFrameOff() => _boardFrameEnabled ? _boardFrameWidth : 0;

        private (int row, int col) GetBoardSquare(MouseEventArgs e)
        {
            int fo = GetFrameOff();
            int squareSize = Math.Min(Width - 2 * fo, Height - 2 * fo) / 8;
            if (squareSize < 1) squareSize = 1;
            int clickCol = (e.X - fo) / squareSize;
            int clickRow = (e.Y - fo) / squareSize;
            int boardRow = isFlipped ? 7 - clickRow : clickRow;
            int boardCol = isFlipped ? 7 - clickCol : clickCol;
            return (boardRow, boardCol);
        }

        private void HandleRightMouseDown(MouseEventArgs e)
        {
            var (boardRow, boardCol) = GetBoardSquare(e);
            if (boardRow >= 0 && boardRow <= 7 && boardCol >= 0 && boardCol <= 7)
            {
                isDrawingArrow = true;
                arrowFromRow = boardRow;
                arrowFromCol = boardCol;
                arrowDragPosition = e.Location;
            }
        }

        private void HandleRightMouseUp(MouseEventArgs e)
        {
            isDrawingArrow = false;
            var (boardRow, boardCol) = GetBoardSquare(e);

            if (boardRow >= 0 && boardRow <= 7 && boardCol >= 0 && boardCol <= 7)
            {
                if (boardRow == arrowFromRow && boardCol == arrowFromCol)
                {
                    // Tap on same square — toggle square highlight
                    var sq = (boardRow, boardCol);
                    if (_highlightedSquares.Contains(sq))
                        _highlightedSquares.Remove(sq);
                    else
                        _highlightedSquares.Add(sq);
                }
                else
                {
                    var arrow = (arrowFromRow, arrowFromCol, boardRow, boardCol);
                    if (userArrows.Contains(arrow))
                        userArrows.Remove(arrow);
                    else
                        userArrows.Add(arrow);
                }
            }

            arrowFromRow = -1;
            arrowFromCol = -1;
            Invalidate();
        }

        private void HandleSquareClick(int row, int col)
        {
            char clickedPiece = board.GetPiece(row, col);
            bool isOwnPiece = clickedPiece != '.' &&
                ((whiteToMove && char.IsUpper(clickedPiece)) ||
                 (!whiteToMove && char.IsLower(clickedPiece)));

            if (selectedRow == -1)
            {
                // No piece selected - try to select
                if (isOwnPiece)
                {
                    SelectPiece(row, col);
                }
            }
            else
            {
                // Piece already selected
                if (row == selectedRow && col == selectedCol)
                {
                    // Clicked same square - deselect
                    ClearSelection();
                }
                else if (isOwnPiece)
                {
                    // Clicked another own piece - select it instead
                    SelectPiece(row, col);
                }
                else if (legalMoveSquares.Contains((row, col)))
                {
                    // Legal move - execute it
                    ExecuteMove(selectedRow, selectedCol, row, col, null);
                }
                else
                {
                    // Invalid move - deselect
                    ClearSelection();
                }
            }

            Invalidate();
        }

        private void SelectPiece(int row, int col)
        {
            selectedRow = row;
            selectedCol = col;

            // Get legal moves for this piece
            legalMoveSquares = GetLegalMovesForPiece(row, col);
        }

        private void ClearSelection()
        {
            selectedRow = -1;
            selectedCol = -1;
            legalMoveSquares.Clear();
            Invalidate();
        }

        private HashSet<(int row, int col)> GetLegalMovesForPiece(int row, int col)
        {
            var moves = new HashSet<(int row, int col)>();
            char piece = board.GetPiece(row, col);

            if (piece == '.') return moves;

            // Generate all pseudo-legal moves for this piece
            for (int toRow = 0; toRow < 8; toRow++)
            {
                for (int toCol = 0; toCol < 8; toCol++)
                {
                    if (IsLegalMove(row, col, toRow, toCol))
                    {
                        moves.Add((toRow, toCol));
                    }
                }
            }

            return moves;
        }

        private bool IsLegalMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            // Quick check: can't capture own pieces
            char targetPiece = board.GetPiece(toRow, toCol);
            char movingPiece = board.GetPiece(fromRow, fromCol);
            if (targetPiece != '.' &&
                ((char.IsUpper(movingPiece) && char.IsUpper(targetPiece)) ||
                 (char.IsLower(movingPiece) && char.IsLower(targetPiece))))
            {
                return false;
            }

            // Special castling validation
            if (char.ToLower(movingPiece) == 'k' && Math.Abs(toCol - fromCol) == 2 && fromRow == toRow)
            {
                if (!IsValidCastling(fromRow, fromCol, toCol))
                {
                    return false;
                }
            }
            // Special pawn move validation
            else if (char.ToLower(movingPiece) == 'p')
            {
                if (!IsValidPawnMove(fromRow, fromCol, toRow, toCol, movingPiece, targetPiece))
                {
                    return false;
                }
            }
            else
            {
                // Use the existing CanReachSquare for non-pawn pieces
                if (!ChessRulesService.CanReachSquare(board, fromRow, fromCol, movingPiece, toRow, toCol))
                {
                    return false;
                }
            }

            // Check if move leaves king in check
            using var pooled = BoardPool.Rent(board);
            ChessBoard testBoard = pooled.Board;

            // Make the move
            testBoard.SetPiece(toRow, toCol, movingPiece);
            testBoard.SetPiece(fromRow, fromCol, '.');

            // Handle castling
            if (char.ToLower(movingPiece) == 'k' && Math.Abs(fromCol - toCol) == 2)
            {
                // Kingside
                if (toCol > fromCol)
                {
                    testBoard.SetPiece(fromRow, 5, testBoard.GetPiece(fromRow, 7));
                    testBoard.SetPiece(fromRow, 7, '.');
                }
                // Queenside
                else
                {
                    testBoard.SetPiece(fromRow, 3, testBoard.GetPiece(fromRow, 0));
                    testBoard.SetPiece(fromRow, 0, '.');
                }
            }

            // Handle en passant capture
            if (char.ToLower(movingPiece) == 'p' && fromCol != toCol && targetPiece == '.')
            {
                testBoard.SetPiece(fromRow, toCol, '.');
            }

            // Check if own king is in check
            return !IsKingInCheck(testBoard, whiteToMove);
        }

        private bool IsValidPawnMove(int fromRow, int fromCol, int toRow, int toCol, char pawn, char targetPiece)
        {
            bool isWhite = char.IsUpper(pawn);
            int direction = isWhite ? -1 : 1; // White moves up (decreasing row), black moves down
            int startRow = isWhite ? 6 : 1;
            int dr = toRow - fromRow;
            int df = toCol - fromCol;

            // Forward move (non-capture)
            if (df == 0)
            {
                // Single square forward
                if (dr == direction)
                {
                    return targetPiece == '.'; // Must be empty
                }
                // Double square forward from starting position
                if (dr == 2 * direction && fromRow == startRow)
                {
                    // Both squares must be empty
                    char middlePiece = board.GetPiece(fromRow + direction, fromCol);
                    return targetPiece == '.' && middlePiece == '.';
                }
                return false;
            }

            // Diagonal move (capture)
            if (Math.Abs(df) == 1 && dr == direction)
            {
                // Regular capture - must have enemy piece
                if (targetPiece != '.')
                {
                    bool targetIsEnemy = isWhite ? char.IsLower(targetPiece) : char.IsUpper(targetPiece);
                    return targetIsEnemy;
                }

                // En passant - only valid if target square matches the tracked en passant square
                if (toRow == enPassantRow && toCol == enPassantCol)
                {
                    return true;
                }
                return false;
            }

            return false;
        }

        private bool IsValidCastling(int kingRow, int kingCol, int targetCol)
        {
            bool isWhiteKing = whiteToMove;
            int expectedRow = isWhiteKing ? 7 : 0;

            // King must be on starting square
            if (kingRow != expectedRow || kingCol != 4)
                return false;

            bool isKingside = targetCol > kingCol;

            // Check castling rights
            if (isWhiteKing)
            {
                if (isKingside && !whiteKingsideCastle) return false;
                if (!isKingside && !whiteQueensideCastle) return false;
            }
            else
            {
                if (isKingside && !blackKingsideCastle) return false;
                if (!isKingside && !blackQueensideCastle) return false;
            }

            // Check that rook is present
            int rookCol = isKingside ? 7 : 0;
            char expectedRook = isWhiteKing ? 'R' : 'r';
            if (board.GetPiece(kingRow, rookCol) != expectedRook)
                return false;

            // Check that squares between king and rook are empty
            int startCol = Math.Min(kingCol, rookCol) + 1;
            int endCol = Math.Max(kingCol, rookCol);
            for (int col = startCol; col < endCol; col++)
            {
                if (board.GetPiece(kingRow, col) != '.')
                    return false;
            }

            // King cannot be in check
            if (IsKingInCheck(board, isWhiteKing))
                return false;

            // King cannot pass through check
            // Check the square the king passes through
            int passThroughCol = isKingside ? 5 : 3;
            using var pooled = BoardPool.Rent(board);
            ChessBoard testBoard = pooled.Board;
            char king = isWhiteKing ? 'K' : 'k';
            testBoard.SetPiece(kingRow, passThroughCol, king);
            testBoard.SetPiece(kingRow, kingCol, '.');
            if (IsKingInCheck(testBoard, isWhiteKing))
                return false;

            return true;
        }

        private bool IsKingInCheck(ChessBoard testBoard, bool whiteKing)
        {
            // Find king position
            char king = whiteKing ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (testBoard.GetPiece(r, c) == king)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
                if (kingRow != -1) break;
            }

            if (kingRow == -1) return false; // No king found (shouldn't happen)

            // Check if any enemy piece can attack the king
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = testBoard.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool isEnemyPiece = whiteKing ? char.IsLower(piece) : char.IsUpper(piece);
                    if (isEnemyPiece && CanAttackSquare(testBoard, r, c, piece, kingRow, kingCol))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a piece can attack a square (geometry + path clear for sliding pieces)
        /// </summary>
        private bool CanAttackSquare(ChessBoard testBoard, int fromRow, int fromCol, char piece, int toRow, int toCol)
        {
            // First check if the geometry allows this move
            if (!ChessRulesService.CanReachSquare(testBoard, fromRow, fromCol, piece, toRow, toCol))
            {
                return false;
            }

            char pieceLower = char.ToLower(piece);

            // Knights and kings don't need path checking (they jump or move 1 square)
            if (pieceLower == 'n' || pieceLower == 'k')
            {
                return true;
            }

            // Pawns attack diagonally - already validated by CanReachSquare
            if (pieceLower == 'p')
            {
                // Pawns only attack diagonally, not forward
                return fromCol != toCol;
            }

            // For sliding pieces (queen, rook, bishop), check if path is clear
            int dr = Math.Sign(toRow - fromRow);
            int dc = Math.Sign(toCol - fromCol);

            int currentRow = fromRow + dr;
            int currentCol = fromCol + dc;

            while (currentRow != toRow || currentCol != toCol)
            {
                if (testBoard.GetPiece(currentRow, currentCol) != '.')
                {
                    return false; // Path is blocked
                }
                currentRow += dr;
                currentCol += dc;
            }

            return true;
        }

        private string ToUciMove(int fromRow, int fromCol, int toRow, int toCol, char? promotion = null)
        {
            char fromFile = (char)('a' + fromCol);
            char fromRank = (char)('1' + (7 - fromRow));
            char toFile = (char)('a' + toCol);
            char toRank = (char)('1' + (7 - toRow));

            string move = $"{fromFile}{fromRank}{toFile}{toRank}";
            if (promotion.HasValue)
                move += char.ToLower(promotion.Value);

            return move;
        }

        #endregion

        #region Move Execution

        private bool ExecuteMove(int fromRow, int fromCol, int toRow, int toCol, char? promotion)
        {
            userArrows.Clear();
            _highlightedSquares.Clear();

            char movingPiece = board.GetPiece(fromRow, fromCol);

            // Check for pawn promotion
            if (char.ToLower(movingPiece) == 'p')
            {
                bool isPromotion = (char.IsUpper(movingPiece) && toRow == 0) ||
                                   (char.IsLower(movingPiece) && toRow == 7);

                if (isPromotion && !promotion.HasValue)
                {
                    // Show promotion dialog
                    promotion = ShowPromotionDialog(char.IsUpper(movingPiece));
                    if (!promotion.HasValue)
                    {
                        ClearSelection();
                        return false; // Cancelled
                    }
                }
            }

            // Build UCI move
            string uciMove = ToUciMove(fromRow, fromCol, toRow, toCol, promotion);

            // Store move for highlighting
            lastMove = (fromRow, fromCol, toRow, toCol);

            // Apply the move to the board
            char captured = board.GetPiece(toRow, toCol);
            bool isCapture = captured != '.' ||
                (char.ToLower(movingPiece) == 'p' && fromCol != toCol && captured == '.'); // en passant

            // Handle special moves
            if (char.ToLower(movingPiece) == 'k' && Math.Abs(fromCol - toCol) == 2)
            {
                // Castling
                if (toCol > fromCol) // Kingside
                {
                    board.SetPiece(fromRow, 5, board.GetPiece(fromRow, 7));
                    board.SetPiece(fromRow, 7, '.');
                }
                else // Queenside
                {
                    board.SetPiece(fromRow, 3, board.GetPiece(fromRow, 0));
                    board.SetPiece(fromRow, 0, '.');
                }
            }
            else if (char.ToLower(movingPiece) == 'p' && fromCol != toCol && captured == '.')
            {
                // En passant
                board.SetPiece(fromRow, toCol, '.');
            }

            // Move the piece
            char finalPiece = promotion.HasValue
                ? (char.IsUpper(movingPiece) ? char.ToUpper(promotion.Value) : char.ToLower(promotion.Value))
                : movingPiece;

            board.SetPiece(toRow, toCol, finalPiece);
            board.SetPiece(fromRow, fromCol, '.');

            // Update castling rights
            UpdateCastlingRights(movingPiece, fromRow, fromCol, toRow, toCol);

            // Update en passant square
            if (char.ToLower(movingPiece) == 'p' && Math.Abs(toRow - fromRow) == 2)
            {
                // Pawn moved 2 squares - set en passant target square (the square behind the pawn)
                enPassantRow = (fromRow + toRow) / 2;
                enPassantCol = fromCol;
            }
            else
            {
                // Any other move clears en passant
                enPassantRow = -1;
                enPassantCol = -1;
            }

            // Switch turn
            whiteToMove = !whiteToMove;

            ClearSelection();
            Invalidate();

            // Fire event
            MoveMade?.Invoke(this, new MoveEventArgs(uciMove, GetFEN(), isCapture));
            BoardChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }

        private void UpdateCastlingRights(char movingPiece, int fromRow, int fromCol, int toRow, int toCol)
        {
            // King moves - lose all castling rights for that color
            if (movingPiece == 'K')
            {
                whiteKingsideCastle = false;
                whiteQueensideCastle = false;
            }
            else if (movingPiece == 'k')
            {
                blackKingsideCastle = false;
                blackQueensideCastle = false;
            }

            // Rook moves from starting square - lose that side's castling right
            if (movingPiece == 'R')
            {
                if (fromRow == 7 && fromCol == 7) whiteKingsideCastle = false;
                if (fromRow == 7 && fromCol == 0) whiteQueensideCastle = false;
            }
            else if (movingPiece == 'r')
            {
                if (fromRow == 0 && fromCol == 7) blackKingsideCastle = false;
                if (fromRow == 0 && fromCol == 0) blackQueensideCastle = false;
            }

            // Rook captured - lose that side's castling right
            if (toRow == 7 && toCol == 7) whiteKingsideCastle = false;
            if (toRow == 7 && toCol == 0) whiteQueensideCastle = false;
            if (toRow == 0 && toCol == 7) blackKingsideCastle = false;
            if (toRow == 0 && toCol == 0) blackQueensideCastle = false;
        }

        private char? ShowPromotionDialog(bool isWhite)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "Promote Pawn";
                dialog.Size = new Size(280, 100);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                char? result = null;
                string[] pieces = { "Q", "R", "B", "N" };
                string[] labels = { "Queen", "Rook", "Bishop", "Knight" };

                for (int i = 0; i < 4; i++)
                {
                    string pieceChar = pieces[i];
                    Button btn = new Button
                    {
                        Text = labels[i],
                        Location = new Point(10 + i * 65, 15),
                        Size = new Size(60, 35),
                        Tag = pieceChar
                    };
                    btn.Click += (s, e) =>
                    {
                        result = ((string)((Button)s!).Tag!)[0];
                        dialog.DialogResult = DialogResult.OK;
                        dialog.Close();
                    };
                    dialog.Controls.Add(btn);
                }

                dialog.ShowDialog();
                return result;
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                coordFont?.Dispose();
                pieceFallbackFont?.Dispose();
                _labelFont?.Dispose();
                _hoverLabelFont?.Dispose();
                _badgeFont?.Dispose();
                _paintBrush.Dispose();
                _legalMovePen.Dispose();
                _arrowPen.Dispose();
                _gridPen.Dispose();
                _gradientLightBitmap?.Dispose();
                _gradientDarkBitmap?.Dispose();
                _glowWhiteBitmap?.Dispose();
                _glowBlackBitmap?.Dispose();
                foreach (var img in pieceImages.Values) img?.Dispose();
                pieceImages.Clear();
                foreach (var bmp in _scaledImages.Values) bmp?.Dispose();
                _scaledImages.Clear();
                _rainbowTimer.Stop();
                _rainbowTimer.Dispose();
                _particleTimer.Stop();
                _particleTimer.Dispose();
                _particleFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Event args for when a move is made on the board
    /// </summary>
    public class MoveEventArgs : EventArgs
    {
        public string UciMove { get; }
        public string FEN { get; }
        public bool IsCapture { get; }

        public MoveEventArgs(string uciMove, string fen, bool isCapture = false)
        {
            UciMove = uciMove;
            FEN = fen;
            IsCapture = isCapture;
        }
    }
}
