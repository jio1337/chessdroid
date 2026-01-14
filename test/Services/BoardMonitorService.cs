using ChessDroid.Models;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Event arguments for turn change detection
    /// Contains captured board state to avoid re-capturing
    /// </summary>
    public class TurnChangedEventArgs : EventArgs
    {
        public ChessBoard CurrentBoard { get; set; }
        public Mat BoardMat { get; set; }
        public Rectangle BoardRect { get; set; }
        public bool BlackAtBottom { get; set; }

        public TurnChangedEventArgs(ChessBoard board, Mat mat, Rectangle rect, bool blackAtBottom)
        {
            CurrentBoard = board;
            BoardMat = mat;
            BoardRect = rect;
            BlackAtBottom = blackAtBottom;
        }
    }

    /// <summary>
    /// Service that continuously monitors the chess board for changes and detects moves
    /// Automatically triggers analysis when it becomes the user's turn
    ///
    /// BETA STATUS: This feature is functional but has known limitations:
    /// - Occasional engine crashes when positions change rapidly (unrelated to turn detection)
    /// - May miss opponent moves if they respond extremely quickly (within debounce window)
    /// - Piece recognition accuracy affects reliability in complex positions
    ///
    /// TODO v3.1: Improve robustness and handle edge cases
    /// </summary>
    public class BoardMonitorService
    {
        private readonly ScreenCaptureService screenCaptureService;
        private readonly BoardDetectionService boardDetectionService;
        private readonly PieceRecognitionService pieceRecognitionService;
        private readonly PositionStateManager positionStateManager;
        private readonly AppConfig config;

        private System.Windows.Forms.Timer? scanTimer;
        private ChessBoard? lastDetectedBoard;
        private string? lastFEN; // FEN of last stable position (for reliable move detection)
        private bool isMonitoring = false;
        private bool userIsWhite = true;
        private bool isUserTurn = true;

        // Debouncing: Track candidate position before committing to it
        private string? candidateFEN = null;
        private DateTime candidateFENFirstSeen = DateTime.MinValue;
        private const int DEBOUNCE_MS = 200; // Wait 200ms for position to stabilize (reduced for faster response)

        // Invalid board tracking: Only clear cache after multiple consecutive failures
        private int consecutiveInvalidBoards = 0;
        private const int MAX_INVALID_BEFORE_CACHE_CLEAR = 5; // Allow 5 retries before clearing cache

        // Debug counter for same-position scans
        private int sameFENScans = 0;

        // Event triggered when user's turn begins
        public event EventHandler<TurnChangedEventArgs>? UserTurnDetected;

        public BoardMonitorService(
            ScreenCaptureService screenCapture,
            BoardDetectionService boardDetection,
            PieceRecognitionService pieceRecognition,
            PositionStateManager positionState,
            AppConfig configuration)
        {
            screenCaptureService = screenCapture;
            boardDetectionService = boardDetection;
            pieceRecognitionService = pieceRecognition;
            positionStateManager = positionState;
            config = configuration;
        }

        /// <summary>
        /// Starts continuous board monitoring
        /// </summary>
        /// <param name="userPlaysWhite">True if user is playing as White</param>
        public void StartMonitoring(bool userPlaysWhite)
        {
            if (isMonitoring)
            {
                Debug.WriteLine("BoardMonitorService: Already monitoring");
                return;
            }

            userIsWhite = userPlaysWhite;
            // User enables auto-monitor when it's their turn (before making first move)
            // We'll trigger immediate analysis, then track moves normally
            isUserTurn = true;
            lastDetectedBoard = null;
            lastFEN = null;

            // Clear board detection cache to force fresh detection
            boardDetectionService.ClearCache();

            // Initialize timer
            scanTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1 second scan interval
            };
            scanTimer.Tick += ScanTimer_Tick;
            scanTimer.Start();

            isMonitoring = true;
            Debug.WriteLine($"BoardMonitorService: Monitoring STARTED (user plays {(userIsWhite ? "White" : "Black")})");

            // Trigger immediate analysis on first scan (capture current position)
            TriggerImmediateAnalysis();
        }

        /// <summary>
        /// Stops continuous board monitoring
        /// </summary>
        public void StopMonitoring()
        {
            if (!isMonitoring)
            {
                return;
            }

            scanTimer?.Stop();
            scanTimer?.Dispose();
            scanTimer = null;

            isMonitoring = false;
            lastDetectedBoard = null;
            Debug.WriteLine("BoardMonitorService: Monitoring STOPPED");
        }

        /// <summary>
        /// Returns whether monitoring is currently active
        /// </summary>
        public bool IsMonitoring()
        {
            return isMonitoring;
        }

        /// <summary>
        /// Triggers immediate analysis when monitoring starts
        /// Captures current board position and fires UserTurnDetected event
        /// </summary>
        private void TriggerImmediateAnalysis()
        {
            try
            {
                Debug.WriteLine("BoardMonitorService: Triggering IMMEDIATE analysis on startup");

                // 1. Capture screen
                Mat? fullScreenMat = screenCaptureService.CaptureScreenAsMat();
                if (fullScreenMat == null)
                {
                    Debug.WriteLine("BoardMonitorService: Immediate analysis failed - screen capture failed");
                    return;
                }

                // 2. Detect board
                var detectedResult = boardDetectionService.DetectBoardWithRectangle(fullScreenMat);
                Mat? boardMat = detectedResult.boardMat;
                Rectangle boardRect = detectedResult.boardRect;

                if (boardMat == null)
                {
                    Debug.WriteLine("BoardMonitorService: Immediate analysis failed - board not detected");
                    return;
                }

                // 3. Determine orientation based on user's color
                bool blackAtBottom = !userIsWhite;
                if (blackAtBottom)
                {
                    CvInvoke.Flip(boardMat, boardMat, FlipType.Vertical);
                    CvInvoke.Flip(boardMat, boardMat, FlipType.Horizontal);
                }

                // 4. Recognize pieces
                ChessBoard currentBoard = pieceRecognitionService.ExtractBoardFromMat(
                    boardMat,
                    blackAtBottom,
                    config.MatchThreshold);

                // 5. Validate board
                if (!IsBoardValid(currentBoard))
                {
                    Debug.WriteLine("BoardMonitorService: Immediate analysis failed - invalid board");
                    return;
                }

                // 6. Store as baseline for future move detection
                lastDetectedBoard = currentBoard;
                lastFEN = currentBoard.ToFEN(); // Store FEN for comparison

                // 7. After immediate analysis, next detected move will be USER's move
                //    Set isUserTurn = true so that when user makes their move:
                //    - It toggles to false (OPPONENT turn)
                //    - Then opponent's move toggles to true (USER turn) → triggers analysis
                isUserTurn = true;
                Debug.WriteLine("BoardMonitorService: Setting isUserTurn = true (next detected move will be user's)");

                // 8. Fire analysis event
                Debug.WriteLine($"BoardMonitorService: IMMEDIATE ANALYSIS - Triggering event (FEN: {lastFEN})");
                UserTurnDetected?.Invoke(this, new TurnChangedEventArgs(
                    currentBoard,
                    boardMat,
                    boardRect,
                    blackAtBottom));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BoardMonitorService: Error in TriggerImmediateAnalysis: {ex.Message}");
            }
        }

        /// <summary>
        /// Timer tick handler - scans board and detects changes
        /// </summary>
        private void ScanTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 1. Capture screen
                Mat? fullScreenMat = screenCaptureService.CaptureScreenAsMat();
                if (fullScreenMat == null)
                {
                    Debug.WriteLine("BoardMonitorService: Screen capture failed");
                    return;
                }

                // 2. Detect board
                var detectedResult = boardDetectionService.DetectBoardWithRectangle(fullScreenMat);
                Mat? boardMat = detectedResult.boardMat;
                Rectangle boardRect = detectedResult.boardRect;

                if (boardMat == null)
                {
                    Debug.WriteLine("BoardMonitorService: Board detection failed (board not visible?)");
                    return;
                }

                // 3. Determine orientation based on user's color
                // If user plays White → White at bottom (normal)
                // If user plays Black → Black at bottom (flip board)
                bool blackAtBottom = !userIsWhite;
                if (blackAtBottom)
                {
                    CvInvoke.Flip(boardMat, boardMat, FlipType.Vertical);
                    CvInvoke.Flip(boardMat, boardMat, FlipType.Horizontal);
                }

                // 4. Recognize pieces
                ChessBoard currentBoard = pieceRecognitionService.ExtractBoardFromMat(
                    boardMat,
                    blackAtBottom,
                    config.MatchThreshold);

                // 5. Validate board has pieces (not empty/garbage)
                if (!IsBoardValid(currentBoard))
                {
                    consecutiveInvalidBoards++;
                    Debug.WriteLine($"BoardMonitorService: Invalid board detected ({consecutiveInvalidBoards}/{MAX_INVALID_BEFORE_CACHE_CLEAR})");

                    // Clear cache immediately - don't wait for multiple failures
                    // This forces next scan to try fresh detection instead of using bad cached location
                    Debug.WriteLine("BoardMonitorService: Clearing cache to force fresh board detection");
                    boardDetectionService.ClearCache();
                    return;
                }

                // Valid board found - confirm the cache location is good
                consecutiveInvalidBoards = 0;
                boardDetectionService.ConfirmCache(); // Tell cache this location is valid, keep using it

                // 6. FEN-based move detection with debouncing (prevents flicker false positives)
                string currentFEN = currentBoard.ToFEN(); // Convert board to FEN for comparison

                if (lastFEN == null)
                {
                    // First scan - store current position as baseline
                    lastDetectedBoard = currentBoard;
                    lastFEN = currentFEN;
                    candidateFEN = null; // Clear any pending candidate
                    Debug.WriteLine($"BoardMonitorService: Initial board captured (FEN: {currentFEN})");
                    return;
                }

                // Compare FENs - if same as last confirmed position, reset candidate
                if (currentFEN == lastFEN)
                {
                    sameFENScans++;

                    // No change - same position, clear any pending candidate
                    if (candidateFEN != null)
                    {
                        Debug.WriteLine($"BoardMonitorService: Position reverted to previous (flicker cancelled)");
                        candidateFEN = null;
                    }

                    // DEBUG: Every 5th scan with same FEN, log it to help diagnose stuck scans
                    if (sameFENScans % 5 == 0)
                    {
                        Debug.WriteLine($"BoardMonitorService: [DEBUG] Same FEN for {sameFENScans} scans: {currentFEN}");
                    }
                    return;
                }

                // FEN changed - reset same-scan counter
                sameFENScans = 0;

                // New position detected - start/continue debouncing
                if (candidateFEN == null || candidateFEN != currentFEN)
                {
                    // First time seeing this new position - start debounce timer
                    candidateFEN = currentFEN;
                    candidateFENFirstSeen = DateTime.Now;
                    Debug.WriteLine($"BoardMonitorService: New position detected, waiting {DEBOUNCE_MS}ms to confirm...");
                    Debug.WriteLine($"BoardMonitorService: [DEBUG] Candidate FEN: {currentFEN}");
                    return;
                }

                // Same candidate position - check if debounce period elapsed
                double elapsedMs = (DateTime.Now - candidateFENFirstSeen).TotalMilliseconds;
                if (elapsedMs < DEBOUNCE_MS)
                {
                    // Still debouncing - wait longer
                    return;
                }

                // Debounce period complete - commit to this position as a confirmed move
                Debug.WriteLine($"BoardMonitorService: Position stable for {DEBOUNCE_MS}ms, processing as confirmed move");
                candidateFEN = null; // Clear candidate - we're committing to this move

                // Determine who moved by checking which pieces changed
                bool whitePiecesMoved = DidWhitePiecesMove(lastFEN, currentFEN);
                bool blackPiecesMoved = DidBlackPiecesMove(lastFEN, currentFEN);

                // Handle captures: Both colors change, but only one side actively moved
                // Use turn state to determine who moved when both colors changed
                bool wasUserMove = false;
                bool wasOpponentMove = false;

                if (whitePiecesMoved && blackPiecesMoved)
                {
                    // Both colors changed - this is a capture
                    // Determine who moved based on whose turn it SHOULD be (alternating)
                    wasUserMove = isUserTurn;
                    wasOpponentMove = !isUserTurn;
                    Debug.WriteLine($"BoardMonitorService: Capture detected (both colors changed), using turn state: {(wasUserMove ? "USER" : "OPPONENT")} move");
                }
                else if (whitePiecesMoved)
                {
                    // Only white pieces moved (no capture)
                    wasUserMove = userIsWhite;
                    wasOpponentMove = !userIsWhite;
                }
                else if (blackPiecesMoved)
                {
                    // Only black pieces moved (no capture)
                    wasUserMove = !userIsWhite;
                    wasOpponentMove = userIsWhite;
                }

                if (wasUserMove)
                {
                    // User made a move - just update FEN and wait for opponent
                    Debug.WriteLine($"BoardMonitorService: User move detected (ignoring, waiting for opponent)");
                    Debug.WriteLine($"BoardMonitorService: Old FEN: {lastFEN}");
                    Debug.WriteLine($"BoardMonitorService: New FEN: {currentFEN}");
                    lastDetectedBoard = currentBoard;
                    lastFEN = currentFEN;
                    isUserTurn = false; // Now waiting for opponent
                    Debug.WriteLine($"BoardMonitorService: Turn changed to OPPONENT (waiting for response)");
                    return;
                }
                else if (wasOpponentMove)
                {
                    // Opponent made a move - trigger analysis
                    Debug.WriteLine($"BoardMonitorService: Opponent move detected!");
                    Debug.WriteLine($"BoardMonitorService: Old FEN: {lastFEN}");
                    Debug.WriteLine($"BoardMonitorService: New FEN: {currentFEN}");

                    // Update last detected position
                    lastDetectedBoard = currentBoard;
                    lastFEN = currentFEN;

                    // Now it's user's turn - trigger analysis
                    isUserTurn = true;
                    Debug.WriteLine($"BoardMonitorService: Turn changed to USER - Triggering analysis");

                    // Update position state manager
                    bool whiteToMove = (userIsWhite && isUserTurn) || (!userIsWhite && !isUserTurn);
                    positionStateManager.SetWhiteToMove(whiteToMove);

                    UserTurnDetected?.Invoke(this, new TurnChangedEventArgs(
                        currentBoard,
                        boardMat,
                        boardRect,
                        blackAtBottom));
                }
                else
                {
                    // Couldn't determine who moved (ambiguous) - just update FEN
                    Debug.WriteLine($"BoardMonitorService: Ambiguous move detected - updating FEN");
                    lastDetectedBoard = currentBoard;
                    lastFEN = currentFEN;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BoardMonitorService: Error in ScanTimer_Tick: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets monitoring state (used when user resets application)
        /// </summary>
        public void Reset(bool userPlaysWhite)
        {
            lastDetectedBoard = null;
            lastFEN = null;
            userIsWhite = userPlaysWhite;
            isUserTurn = userPlaysWhite; // Reset to starting turn
            Debug.WriteLine("BoardMonitorService: State reset");
        }

        /// <summary>
        /// Temporarily pauses scanning (used during manual analysis)
        /// </summary>
        public void PauseScanning()
        {
            if (scanTimer != null && isMonitoring)
            {
                scanTimer.Stop();
                // Clear cache so manual analysis gets fresh board detection
                boardDetectionService.ClearCache();
                Debug.WriteLine("BoardMonitorService: Scanning paused (cache cleared)");
            }
        }

        /// <summary>
        /// Resumes scanning after pause
        /// </summary>
        public void ResumeScanning()
        {
            if (scanTimer != null && isMonitoring)
            {
                scanTimer.Start();
                Debug.WriteLine("BoardMonitorService: Scanning resumed");
            }
        }

        /// <summary>
        /// Validates that board contains chess pieces (not empty or garbage)
        /// </summary>
        private bool IsBoardValid(ChessBoard board)
        {
            int pieceCount = 0;
            int kingCount = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board[r, c];
                    if (piece != '.')
                    {
                        pieceCount++;
                        if (char.ToUpper(piece) == 'K')
                            kingCount++;
                    }
                }
            }

            // Valid board must have:
            // - Exactly 2 kings (one white, one black) - REQUIRED
            // - At least 4 total pieces (allows king + pawn endgames)
            // This is lenient to handle endgames and spotty recognition
            bool isValid = kingCount == 2 && pieceCount >= 4;

            if (!isValid)
            {
                Debug.WriteLine($"BoardMonitorService: Invalid board - {pieceCount} pieces, {kingCount} kings");
            }

            return isValid;
        }

        /// <summary>
        /// Determines if white pieces moved by comparing FEN strings
        /// White pieces are uppercase: P, N, B, R, Q, K
        /// </summary>
        private bool DidWhitePiecesMove(string oldFEN, string newFEN)
        {
            // Extract board positions from FEN (first part before space)
            string oldBoard = oldFEN.Split(' ')[0];
            string newBoard = newFEN.Split(' ')[0];

            // Get positions of all white pieces (uppercase letters)
            var oldWhitePieces = GetPiecePositions(oldBoard, char.IsUpper);
            var newWhitePieces = GetPiecePositions(newBoard, char.IsUpper);

            // If positions are different, white pieces moved
            return !oldWhitePieces.SequenceEqual(newWhitePieces);
        }

        /// <summary>
        /// Determines if black pieces moved by comparing FEN strings
        /// Black pieces are lowercase: p, n, b, r, q, k
        /// </summary>
        private bool DidBlackPiecesMove(string oldFEN, string newFEN)
        {
            // Extract board positions from FEN (first part before space)
            string oldBoard = oldFEN.Split(' ')[0];
            string newBoard = newFEN.Split(' ')[0];

            // Get positions of all black pieces (lowercase letters)
            var oldBlackPieces = GetPiecePositions(oldBoard, char.IsLower);
            var newBlackPieces = GetPiecePositions(newBoard, char.IsLower);

            // If positions are different, black pieces moved
            return !oldBlackPieces.SequenceEqual(newBlackPieces);
        }

        /// <summary>
        /// Extracts piece positions from FEN board string based on filter
        /// Returns a sorted string of positions for comparison
        /// </summary>
        private string GetPiecePositions(string fenBoard, Func<char, bool> filter)
        {
            var positions = new List<string>();
            string[] ranks = fenBoard.Split('/');

            for (int rank = 0; rank < ranks.Length; rank++)
            {
                int file = 0;
                foreach (char c in ranks[rank])
                {
                    if (char.IsDigit(c))
                    {
                        // Empty squares - skip ahead
                        file += (c - '0');
                    }
                    else if (filter(c))
                    {
                        // This piece matches our filter (white or black)
                        positions.Add($"{(char)('a' + file)}{8 - rank}{c}");
                        file++;
                    }
                    else
                    {
                        // Piece doesn't match filter - skip
                        file++;
                    }
                }
            }

            // Sort positions for consistent comparison
            positions.Sort();
            return string.Join(",", positions);
        }
    }
}
