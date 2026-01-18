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
    /// Service that continuously monitors the chess board for changes and detects moves.
    /// Automatically triggers analysis when it becomes the user's turn.
    ///
    /// BETA STATUS: This feature is functional but has known limitations:
    /// - Occasional engine crashes when positions change rapidly (unrelated to turn detection)
    /// - May miss opponent moves if they respond extremely quickly (within debounce window)
    /// - Piece recognition accuracy affects reliability in complex positions
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
        private string? lastExpandedFEN; // Cached expanded FEN for faster comparison
        private bool isMonitoring = false;
        private bool userIsWhite = true;
        private bool isUserTurn = true;

        // Debouncing: Track candidate position before committing to it
        private string? candidateFEN = null;
        private DateTime candidateFENFirstSeen = DateTime.MinValue;
        private int candidateConfirmations = 0; // Number of consecutive scans with same candidate

        // Adaptive timing: Faster scanning when waiting for opponent's move
        private const int SCAN_INTERVAL_IDLE = 800; // Normal: 800ms when it's user's turn
        private const int SCAN_INTERVAL_WAITING = 400; // Fast: 400ms when waiting for opponent
        private const int DEBOUNCE_MS = 250; // Wait 250ms for position to stabilize (reduced from 350)
        private const int REQUIRED_CONFIRMATIONS = 2; // Require 2 consecutive scans with same FEN

        // Invalid board tracking
        private int consecutiveInvalidBoards = 0;
        private const int MAX_INVALID_BEFORE_CACHE_CLEAR = 3; // Reduced from 5 for faster recovery

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

            // Initialize timer with adaptive interval (starts idle since it's user's turn)
            scanTimer = new System.Windows.Forms.Timer
            {
                Interval = SCAN_INTERVAL_IDLE
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
                lastExpandedFEN = ExpandFEN(lastFEN.Split(' ')[0]); // Cache expanded for faster comparison

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
                    lastExpandedFEN = ExpandFEN(currentFEN.Split(' ')[0]);
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
                        candidateConfirmations = 0;
                    }

                    // DEBUG: Every 5th scan with same FEN, log it to help diagnose stuck scans
                    if (sameFENScans % 5 == 0)
                    {
                        Debug.WriteLine($"BoardMonitorService: [DEBUG] Same FEN for {sameFENScans} scans");
                    }
                    return;
                }

                // FEN changed - reset same-scan counter
                sameFENScans = 0;

                // New position detected - start/continue debouncing
                if (candidateFEN == null || candidateFEN != currentFEN)
                {
                    // First time seeing this new position - validate it's a legal transition
                    if (!IsValidMoveTransition(lastFEN, currentFEN))
                    {
                        Debug.WriteLine($"BoardMonitorService: REJECTED - Invalid move transition (mid-drag or glitch?)");
                        return;
                    }

                    // Start debounce timer
                    candidateFEN = currentFEN;
                    candidateFENFirstSeen = DateTime.Now;
                    candidateConfirmations = 1;
                    Debug.WriteLine($"BoardMonitorService: New position detected, waiting for confirmation...");
                    return;
                }

                // Same candidate position - increment confirmation counter
                candidateConfirmations++;
                Debug.WriteLine($"BoardMonitorService: Candidate confirmed {candidateConfirmations}/{REQUIRED_CONFIRMATIONS} times");

                // Check if debounce period elapsed AND we have enough confirmations
                double elapsedMs = (DateTime.Now - candidateFENFirstSeen).TotalMilliseconds;
                if (elapsedMs < DEBOUNCE_MS || candidateConfirmations < REQUIRED_CONFIRMATIONS)
                {
                    // Still debouncing or need more confirmations - wait longer
                    return;
                }

                // Debounce period complete AND confirmed multiple times - commit to this position
                Debug.WriteLine($"BoardMonitorService: Position CONFIRMED stable ({candidateConfirmations} scans, {elapsedMs:F0}ms)");
                candidateFEN = null; // Clear candidate - we're committing to this move
                candidateConfirmations = 0;

                // Determine who moved by checking which pieces changed (optimized single-pass)
                var (whitePiecesMoved, blackPiecesMoved) = DetectWhichColorMoved(lastFEN, currentFEN);

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
                    // User made a move - update FEN and switch to fast scanning for opponent response
                    Debug.WriteLine($"BoardMonitorService: User move detected (waiting for opponent)");
                    lastDetectedBoard = currentBoard;
                    lastFEN = currentFEN;
                    lastExpandedFEN = ExpandFEN(currentFEN.Split(' ')[0]);
                    isUserTurn = false; // Now waiting for opponent

                    // Switch to faster scanning while waiting for opponent
                    if (scanTimer != null)
                    {
                        scanTimer.Interval = SCAN_INTERVAL_WAITING;
                        Debug.WriteLine($"BoardMonitorService: Switched to FAST scanning ({SCAN_INTERVAL_WAITING}ms)");
                    }
                    return;
                }
                else if (wasOpponentMove)
                {
                    // Opponent made a move - trigger analysis
                    Debug.WriteLine($"BoardMonitorService: Opponent move detected!");

                    // Update last detected position
                    lastDetectedBoard = currentBoard;
                    lastFEN = currentFEN;
                    lastExpandedFEN = ExpandFEN(currentFEN.Split(' ')[0]);

                    // Now it's user's turn - trigger analysis
                    isUserTurn = true;

                    // Switch back to normal scanning (user is thinking)
                    if (scanTimer != null)
                    {
                        scanTimer.Interval = SCAN_INTERVAL_IDLE;
                        Debug.WriteLine($"BoardMonitorService: Switched to NORMAL scanning ({SCAN_INTERVAL_IDLE}ms)");
                    }

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
                    lastExpandedFEN = ExpandFEN(currentFEN.Split(' ')[0]);
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
            int whiteKingCount = 0;
            int blackKingCount = 0;
            int whitePawnCount = 0;
            int blackPawnCount = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board[r, c];
                    if (piece != '.')
                    {
                        pieceCount++;
                        if (piece == 'K') whiteKingCount++;
                        else if (piece == 'k') blackKingCount++;
                        else if (piece == 'P') whitePawnCount++;
                        else if (piece == 'p') blackPawnCount++;
                    }
                }
            }

            // Valid board must have:
            // - Exactly 1 white king and 1 black king - REQUIRED
            // - At least 2 total pieces (bare kings endgame is valid)
            // - No more than 8 pawns per side
            // - Pawns can't be on ranks 1 or 8 (would have promoted)
            bool hasValidKings = whiteKingCount == 1 && blackKingCount == 1;
            bool hasEnoughPieces = pieceCount >= 2;
            bool hasValidPawnCounts = whitePawnCount <= 8 && blackPawnCount <= 8;

            // Check for pawns on invalid ranks (rank 1 or 8)
            bool pawnsOnValidRanks = true;
            for (int c = 0; c < 8; c++)
            {
                char rank1 = board[7, c]; // Rank 1 (row 7 in 0-indexed)
                char rank8 = board[0, c]; // Rank 8 (row 0 in 0-indexed)
                if (rank1 == 'P' || rank1 == 'p' || rank8 == 'P' || rank8 == 'p')
                {
                    pawnsOnValidRanks = false;
                    break;
                }
            }

            bool isValid = hasValidKings && hasEnoughPieces && hasValidPawnCounts && pawnsOnValidRanks;

            if (!isValid)
            {
                Debug.WriteLine($"BoardMonitorService: Invalid board - {pieceCount} pieces, kings(W:{whiteKingCount}/B:{blackKingCount}), pawns(W:{whitePawnCount}/B:{blackPawnCount}), pawnsValid:{pawnsOnValidRanks}");
            }

            return isValid;
        }

        /// <summary>
        /// Validates that the transition from old FEN to new FEN represents a legal chess move.
        /// Rejects impossible transitions like:
        /// - More than 2 pieces disappearing (impossible - max is 1 capture + 1 en passant pawn)
        /// - Piece appearing from nowhere (except promotion)
        /// - Multiple pieces moving at once (except castling)
        /// </summary>
        private bool IsValidMoveTransition(string oldFEN, string newFEN)
        {
            try
            {
                // Count pieces in each position
                int oldPieceCount = CountPiecesInFEN(oldFEN);
                int newPieceCount = CountPiecesInFEN(newFEN);

                // Calculate change in piece count
                int pieceCountChange = newPieceCount - oldPieceCount;

                // Valid transitions:
                // - 0: No capture (piece moved)
                // - -1: Normal capture (one piece taken)
                // - -2: En passant (capturing pawn + captured pawn both "disappear" from original squares)
                //       OR double capture scenario which shouldn't happen
                // - +1: Unlikely but could happen with piece recognition glitch recovery
                if (pieceCountChange < -2)
                {
                    // More than 2 pieces disappeared - definitely invalid (mid-drag or glitch)
                    Debug.WriteLine($"BoardMonitorService: INVALID TRANSITION - {Math.Abs(pieceCountChange)} pieces disappeared (expected max 2)");
                    return false;
                }

                if (pieceCountChange > 1)
                {
                    // More than 1 piece appeared - invalid (pieces can't appear from nowhere)
                    // Allow +1 for potential glitch recovery where a piece wasn't recognized before
                    Debug.WriteLine($"BoardMonitorService: INVALID TRANSITION - {pieceCountChange} pieces appeared (expected max 1)");
                    return false;
                }

                // Count how many squares changed
                int squaresChanged = CountChangedSquares(oldFEN, newFEN);

                // Valid square changes:
                // - 2: Normal move (from + to)
                // - 3: En passant (from + to + captured pawn square) or Castling short/long
                // - 4: Castling (king from/to + rook from/to)
                // - 5+: Something weird happened - reject
                if (squaresChanged > 4)
                {
                    Debug.WriteLine($"BoardMonitorService: INVALID TRANSITION - {squaresChanged} squares changed (expected max 4)");
                    return false;
                }

                if (squaresChanged < 2)
                {
                    // Less than 2 squares changed - not a real move
                    // Could be piece recognition flicker on same square
                    Debug.WriteLine($"BoardMonitorService: INVALID TRANSITION - Only {squaresChanged} square(s) changed (expected min 2)");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BoardMonitorService: Error validating transition: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Counts total pieces in a FEN string
        /// </summary>
        private int CountPiecesInFEN(string fen)
        {
            string boardPart = fen.Split(' ')[0];
            int count = 0;
            foreach (char c in boardPart)
            {
                if (char.IsLetter(c))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Counts how many squares changed between two FEN positions
        /// </summary>
        private int CountChangedSquares(string oldFEN, string newFEN)
        {
            string oldBoard = oldFEN.Split(' ')[0];
            string newBoard = newFEN.Split(' ')[0];

            // Expand FEN to 64-character string (one per square)
            string oldExpanded = ExpandFEN(oldBoard);
            string newExpanded = ExpandFEN(newBoard);

            if (oldExpanded.Length != 64 || newExpanded.Length != 64)
            {
                Debug.WriteLine($"BoardMonitorService: FEN expansion error - old:{oldExpanded.Length}, new:{newExpanded.Length}");
                return 99; // Return high number to reject
            }

            int changedCount = 0;
            for (int i = 0; i < 64; i++)
            {
                if (oldExpanded[i] != newExpanded[i])
                    changedCount++;
            }

            return changedCount;
        }

        /// <summary>
        /// Expands FEN board notation to 64-character string
        /// e.g., "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR" becomes full 64 chars
        /// </summary>
        private string ExpandFEN(string fenBoard)
        {
            var expanded = new System.Text.StringBuilder(64);

            foreach (char c in fenBoard)
            {
                if (c == '/')
                    continue;
                else if (char.IsDigit(c))
                {
                    int emptyCount = c - '0';
                    expanded.Append('.', emptyCount);
                }
                else
                {
                    expanded.Append(c);
                }
            }

            return expanded.ToString();
        }

        /// <summary>
        /// Determines which color's pieces moved by comparing expanded FEN strings.
        /// Returns (whiteMoved, blackMoved) tuple.
        /// Optimized: Single pass through both positions instead of separate parsing.
        /// </summary>
        private (bool whiteMoved, bool blackMoved) DetectWhichColorMoved(string oldFEN, string newFEN)
        {
            // Use cached expanded FEN if available, otherwise expand
            string oldExpanded = lastExpandedFEN ?? ExpandFEN(oldFEN.Split(' ')[0]);
            string newExpanded = ExpandFEN(newFEN.Split(' ')[0]);

            if (oldExpanded.Length != 64 || newExpanded.Length != 64)
            {
                // Invalid expansion - can't determine
                return (false, false);
            }

            bool whiteMoved = false;
            bool blackMoved = false;

            // Single pass through both boards
            for (int i = 0; i < 64; i++)
            {
                char oldPiece = oldExpanded[i];
                char newPiece = newExpanded[i];

                if (oldPiece != newPiece)
                {
                    // This square changed - check which color was affected
                    if (char.IsUpper(oldPiece) || char.IsUpper(newPiece))
                    {
                        whiteMoved = true;
                    }
                    if (char.IsLower(oldPiece) || char.IsLower(newPiece))
                    {
                        blackMoved = true;
                    }

                    // Early exit if both colors already detected as moved
                    if (whiteMoved && blackMoved)
                    {
                        break;
                    }
                }
            }

            return (whiteMoved, blackMoved);
        }

    }
}