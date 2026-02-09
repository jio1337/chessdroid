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
        private List<(int row, int col)> legalMoveSquares = new List<(int, int)>();

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

        // Engine analysis arrows (separate from user arrows)
        private List<(int fromRow, int fromCol, int toRow, int toCol, Color color)> engineArrows = new();

        // Visual settings
        private Color lightSquareColor = Color.FromArgb(240, 217, 181);
        private Color darkSquareColor = Color.FromArgb(181, 136, 99);
        private Color selectedSquareColor = Color.FromArgb(130, 186, 221, 97);
        private Color legalMoveColor = Color.FromArgb(100, 130, 151, 105);
        private Color lastMoveFromColor = Color.FromArgb(100, 255, 255, 100);
        private Color lastMoveToColor = Color.FromArgb(120, 255, 255, 100);

        // Last move tracking
        private (int fromRow, int fromCol, int toRow, int toCol)? lastMove;

        // Piece images
        private Dictionary<char, Image?> pieceImages = new Dictionary<char, Image?>();
        private string currentTemplateSet = "Lichess";

        // Font for coordinates (fallback for pieces if images fail)
        private Font coordFont = new Font("Segoe UI", 10f, FontStyle.Bold);

        // Cached font for fallback Unicode piece rendering (prevents GDI+ leak)
        private Font? pieceFallbackFont;
        private float lastPieceFontSize = 0f;

        /// <summary>
        /// When false, disables mouse interaction (for engine-vs-engine matches).
        /// </summary>
        public bool InteractionEnabled { get; set; } = true;

        /// <summary>
        /// Squares to highlight as puzzle hints (rendered as orange circles).
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public List<(int row, int col)> HintSquares { get; set; } = new();

        // Events
        public event EventHandler<MoveEventArgs>? MoveMade;
        public event EventHandler? BoardChanged;

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
            // Dispose old images
            foreach (var img in pieceImages.Values)
            {
                img?.Dispose();
            }
            pieceImages.Clear();

            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", templateSet);

            // Map piece chars to file names
            var pieceFiles = new Dictionary<char, string>
            {
                { 'K', "wK.png" }, { 'Q', "wQ.png" }, { 'R', "wR.png" },
                { 'B', "wB.png" }, { 'N', "wN.png" }, { 'P', "wP.png" },
                { 'k', "bK.png" }, { 'q', "bQ.png" }, { 'r', "bR.png" },
                { 'b', "bB.png" }, { 'n', "bN.png" }, { 'p', "bP.png" }
            };

            foreach (var kvp in pieceFiles)
            {
                string filePath = Path.Combine(basePath, kvp.Value);
                if (File.Exists(filePath))
                {
                    try
                    {
                        pieceImages[kvp.Key] = Image.FromFile(filePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to load piece image {filePath}: {ex.Message}");
                        pieceImages[kvp.Key] = null;
                    }
                }
                else
                {
                    Debug.WriteLine($"Piece image not found: {filePath}");
                    pieceImages[kvp.Key] = null;
                }
            }
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
            ClearSelection();
            Invalidate();
            BoardChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Loads a position from FEN string
        /// </summary>
        public void LoadFEN(string fen)
        {
            try
            {
                userArrows.Clear();
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

        #endregion

        #region Painting

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            int squareSize = Math.Min(Width, Height) / 8;

            // Draw squares and pieces
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    int displayRow = isFlipped ? 7 - row : row;
                    int displayCol = isFlipped ? 7 - col : col;

                    Rectangle rect = new Rectangle(displayCol * squareSize, displayRow * squareSize, squareSize, squareSize);

                    // Determine square color
                    bool isLightSquare = (row + col) % 2 == 0;
                    Color squareColor = isLightSquare ? lightSquareColor : darkSquareColor;

                    // Highlight last move
                    if (lastMove.HasValue)
                    {
                        if (row == lastMove.Value.fromRow && col == lastMove.Value.fromCol)
                            squareColor = BlendColors(squareColor, lastMoveFromColor);
                        else if (row == lastMove.Value.toRow && col == lastMove.Value.toCol)
                            squareColor = BlendColors(squareColor, lastMoveToColor);
                    }

                    // Highlight selected square
                    if (row == selectedRow && col == selectedCol)
                        squareColor = BlendColors(squareColor, selectedSquareColor);

                    // Draw square
                    using (SolidBrush brush = new SolidBrush(squareColor))
                    {
                        g.FillRectangle(brush, rect);
                    }

                    // Draw legal move indicators
                    if (legalMoveSquares.Contains((row, col)))
                    {
                        char pieceAtTarget = board.GetPiece(row, col);
                        if (pieceAtTarget != '.')
                        {
                            // Capture - draw ring
                            using (Pen pen = new Pen(legalMoveColor, 3))
                            {
                                int margin = squareSize / 10;
                                g.DrawEllipse(pen, rect.X + margin, rect.Y + margin,
                                    rect.Width - 2 * margin, rect.Height - 2 * margin);
                            }
                        }
                        else
                        {
                            // Empty square - draw dot
                            using (SolidBrush brush = new SolidBrush(legalMoveColor))
                            {
                                int dotSize = squareSize / 4;
                                int offset = (squareSize - dotSize) / 2;
                                g.FillEllipse(brush, rect.X + offset, rect.Y + offset, dotSize, dotSize);
                            }
                        }
                    }

                    // Draw puzzle hint indicators (orange circles)
                    if (HintSquares.Contains((row, col)))
                    {
                        using (Pen pen = new Pen(Color.FromArgb(200, 255, 180, 50), 4))
                        {
                            int margin = squareSize / 8;
                            g.DrawEllipse(pen, rect.X + margin, rect.Y + margin,
                                rect.Width - 2 * margin, rect.Height - 2 * margin);
                        }
                    }

                    // Draw piece (skip if being dragged)
                    char piece = board.GetPiece(row, col);
                    bool isBeingDragged = isDragging && row == dragFromRow && col == dragFromCol;
                    if (piece != '.' && !isBeingDragged)
                    {
                        DrawPiece(g, piece, rect, squareSize);
                    }
                }
            }

            // Draw user arrows
            DrawArrows(g, squareSize);

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
                DrawPiece(g, dragPiece, dragRect, pieceSize);
            }

            // Draw coordinates
            float coordFontSize = Math.Max(8f, squareSize * 0.15f);
            if (Math.Abs(coordFont.Size - coordFontSize) > 0.5f)
            {
                coordFont.Dispose();
                coordFont = new Font("Segoe UI", coordFontSize, FontStyle.Bold);
            }

            for (int i = 0; i < 8; i++)
            {
                // File letters (a-h)
                int fileIndex = isFlipped ? 7 - i : i;
                string file = ((char)('a' + fileIndex)).ToString();
                bool isLight = (7 + fileIndex) % 2 == 0;
                using (SolidBrush brush = new SolidBrush(isLight ? darkSquareColor : lightSquareColor))
                {
                    g.DrawString(file, coordFont, brush, i * squareSize + 2, 8 * squareSize - coordFont.Height - 2);
                }

                // Rank numbers (1-8)
                int rankIndex = isFlipped ? i : 7 - i;
                string rank = (rankIndex + 1).ToString();
                isLight = (i) % 2 == 0;
                using (SolidBrush brush = new SolidBrush(isLight ? darkSquareColor : lightSquareColor))
                {
                    g.DrawString(rank, coordFont, brush, 2, i * squareSize + 2);
                }
            }
        }

        private void DrawArrows(Graphics g, int squareSize)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw engine analysis arrows (underneath user arrows)
            foreach (var arrow in engineArrows)
            {
                DrawArrow(g, squareSize, arrow.fromRow, arrow.fromCol, arrow.toRow, arrow.toCol, arrow.color);
            }

            // Draw user arrows
            foreach (var arrow in userArrows)
            {
                DrawArrow(g, squareSize, arrow.fromRow, arrow.fromCol, arrow.toRow, arrow.toCol, ArrowColor);
            }

            // Draw in-progress arrow while dragging (snap to target square center)
            if (isDrawingArrow && arrowFromRow >= 0)
            {
                int hoverCol = arrowDragPosition.X / squareSize;
                int hoverRow = arrowDragPosition.Y / squareSize;
                int hoverBoardRow = isFlipped ? 7 - hoverRow : hoverRow;
                int hoverBoardCol = isFlipped ? 7 - hoverCol : hoverCol;

                if (hoverBoardRow >= 0 && hoverBoardRow <= 7 && hoverBoardCol >= 0 && hoverBoardCol <= 7
                    && !(hoverBoardRow == arrowFromRow && hoverBoardCol == arrowFromCol))
                {
                    DrawArrow(g, squareSize, arrowFromRow, arrowFromCol, hoverBoardRow, hoverBoardCol, ArrowColor);
                }
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
        }

        private void DrawArrow(Graphics g, int squareSize, int fromRow, int fromCol, int toRow, int toCol, Color color)
        {
            int x1 = (isFlipped ? 7 - fromCol : fromCol) * squareSize + squareSize / 2;
            int y1 = (isFlipped ? 7 - fromRow : fromRow) * squareSize + squareSize / 2;
            int x2 = (isFlipped ? 7 - toCol : toCol) * squareSize + squareSize / 2;
            int y2 = (isFlipped ? 7 - toRow : toRow) * squareSize + squareSize / 2;

            float lineWidth = squareSize * 0.22f;
            using var pen = new Pen(color, lineWidth);
            pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
            g.DrawLine(pen, x1, y1, x2, y2);
        }

        private void DrawPiece(Graphics g, char piece, Rectangle rect, int squareSize)
        {
            // Try to draw from image first
            if (pieceImages.TryGetValue(piece, out Image? img) && img != null)
            {
                // Add small padding so pieces don't touch the edge
                int padding = squareSize / 16;
                Rectangle pieceRect = new Rectangle(
                    rect.X + padding,
                    rect.Y + padding,
                    rect.Width - 2 * padding,
                    rect.Height - 2 * padding
                );
                g.DrawImage(img, pieceRect);
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

            if (e.Button == MouseButtons.Right)
            {
                HandleRightMouseDown(e);
                return;
            }

            // Left-click clears arrows
            if (e.Button == MouseButtons.Left && userArrows.Count > 0)
            {
                userArrows.Clear();
                Invalidate();
            }

            if (!InteractionEnabled) return;
            if (e.Button != MouseButtons.Left) return;

            int squareSize = Math.Min(Width, Height) / 8;
            int clickCol = e.X / squareSize;
            int clickRow = e.Y / squareSize;

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

            if (!InteractionEnabled) return;
            if (e.Button != MouseButtons.Left || !mouseDownOnPiece)
                return;

            // Check if we've exceeded the drag threshold
            if (!isDragging)
            {
                int dx = Math.Abs(e.X - mouseDownPosition.X);
                int dy = Math.Abs(e.Y - mouseDownPosition.Y);

                if (dx > DRAG_THRESHOLD || dy > DRAG_THRESHOLD)
                {
                    isDragging = true;
                    this.Cursor = Cursors.Hand;
                }
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

            int squareSize = Math.Min(Width, Height) / 8;
            int clickCol = e.X / squareSize;
            int clickRow = e.Y / squareSize;

            int boardRow = isFlipped ? 7 - clickRow : clickRow;
            int boardCol = isFlipped ? 7 - clickCol : clickCol;

            if (isDragging)
            {
                // End drag - try to execute move
                isDragging = false;
                this.Cursor = Cursors.Default;

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

        private (int row, int col) GetBoardSquare(MouseEventArgs e)
        {
            int squareSize = Math.Min(Width, Height) / 8;
            int clickCol = e.X / squareSize;
            int clickRow = e.Y / squareSize;
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
                    userArrows.Clear();
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

        private List<(int row, int col)> GetLegalMovesForPiece(int row, int col)
        {
            var moves = new List<(int row, int col)>();
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
            MoveMade?.Invoke(this, new MoveEventArgs(uciMove, GetFEN()));
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
                foreach (var img in pieceImages.Values)
                {
                    img?.Dispose();
                }
                pieceImages.Clear();
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

        public MoveEventArgs(string uciMove, string fen)
        {
            UciMove = uciMove;
            FEN = fen;
        }
    }
}
