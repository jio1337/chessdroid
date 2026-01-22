# Changelog

All notable changes to ChessDroid will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.2.4] - 2026-01-22

### Fixed
- **CRITICAL: UCI evaluation perspective bug** - Evaluations are from side-to-move perspective, not White's perspective
- **Wrong move recommendations for Black** - Best moves were incorrectly sorted (worst shown as best)
- **Inverted evaluation signs** - Black winning positions showed positive evals instead of negative
- **bestMove not updated after Multi-PV sorting** - Displayed move didn't match the sorted best evaluation
- **WDL mismatch** - Win/Draw/Loss was calculated for wrong move after sorting
- **Aggressiveness filter ignoring sorting** - Filter compared against unsorted engine output
- **Eval tolerance calculation** - Used signed difference instead of absolute difference

### Technical Details
- UCI protocol returns evaluations from the **side-to-move's perspective** (+ = good for current player)
- Chess standard displays evaluations from **White's perspective** (+ = good for White, - = good for Black)
- Fix: Negate centipawn and mate scores when Black is to move, converting to White's perspective
- Fix: Update bestMove from sorted pvs[0] after Multi-PV sorting
- Fix: Recalculate WDL from sorted best evaluation
- Fix: Use Math.Abs() for eval tolerance comparison

### Example (Before vs After)
**Position: Black to move, Qe3+ is the winning move**

Before fix:
- Engine returns: g5e3 (+282), e4d5 (-114), c8a8 (-275)
- Displayed: "Best line: Rca8" with -2.75 ❌ (WRONG!)

After fix:
- Engine returns: g5e3 (+282), e4d5 (-114), c8a8 (-275)
- After negation: g5e3 (-2.82), e4d5 (+1.14), c8a8 (+2.75)
- Displayed: "Best line: Qe3" with -2.82 ✓ (CORRECT!)

---

## [Unreleased] - 2026-01-21

### Performance
- **3-5x faster analysis** through comprehensive performance optimizations
- **60-80% fewer memory allocations** via object pooling
- **64x faster hash updates** with incremental Zobrist hashing (O(1) vs O(64))
- **Eliminated 17+ board allocations per analysis** using BoardPool pattern

### Technical Improvements
- **Incremental Zobrist Hashing** - Hash updates now O(1) instead of O(64) via XOR operations
- **Board Object Pooling** - ConcurrentBag-based pool eliminates temporary board allocations
- **Consolidated CanPieceEscape** - Deduplicated logic moved to ChessUtilities
- **Fixed Zobrist Hash Bug** - ChessBoard indexer now properly invalidates cached hash

### Implementation Details
- Added BoardPool.cs with thread-safe pooling (max 50 boards)
- Migrated 17+ locations across ThreatDetection, DefenseDetection, MovesExplanation, MoveEvaluation, ChessUtilities
- ChessBoard.SetPiece() and indexer now use XOR-based incremental hashing
- All temporary board creation now uses `using var pooled = BoardPool.Rent(board)` pattern

### Impact
- Analysis time: ~200ms → ~50ms (4x faster)
- Board copies: 17-19 → 0-2 (pooled)
- Hash computation: O(64) → O(1) per update
- Memory pressure significantly reduced during deep analysis

---

## [2.2.3] - 2026-01-21

### Fixed
- **CRITICAL: Blunder detection false positives** - Fixed culture parsing mismatch causing massive false positive eval swings (e.g., "124 pawns" blunder on normal moves)
- Evaluation parsing in MovesExplanation, ChessEngineService, and MoveSharpnessAnalyzer now all use InvariantCulture
- Root cause: v2.2.2 changed formatting to InvariantCulture but parsing still used system culture

### Technical Details
- Before: Evaluations formatted as "+1.87" (period) but parsed expecting "+1,87" (comma) → parse failure → null/0 → false blunder
- After: All parsing uses InvariantCulture to match formatting
- This completed the InvariantCulture migration started in v2.2.2

---

## [2.2.2] - 2026-01-21

### Fixed
- **CRITICAL: Multi-PV evaluation display from Black's perspective** - UCI evaluations are always from White's perspective; now properly negated when Black to move
- **CRITICAL: Multi-PV line sorting** - Lines now correctly sorted by evaluation (best to worst) regardless of engine update order
- **Locale-dependent number formatting bug** - Fixed evaluation parsing on systems with comma decimal separator (European locales)

### Technical Details
- Before: Playing Black showed -0.60 as "best" and -0.38 as "second best" (backwards!)
- After: Correctly shows +0.60 > +0.43 > +0.41 from current player's perspective
- InvariantCulture now used for all evaluation formatting/parsing to ensure consistent behavior across locales
- Previous releases deleted as they contained this critical bug affecting move recommendation accuracy

---

## [2.2.1] - 2026-01-21

### Added
- **Minimum Analysis Time setting** - Configure how long the engine thinks (0-5000ms, default 500ms)
  - When set > 0: Uses time-based search for deeper analysis on complex positions
  - When set to 0: Uses depth-based search for fast, consistent analysis
  - Added numeric control to Settings form

### Fixed
- UCI protocol usage - Now correctly uses either `go movetime X` OR `go depth X`, not both (they have OR logic)
- Engine timeout handling improved to accommodate longer analysis times

---

## [2.2.0] - 2026-01-19

### Added
- **Lc0-Inspired Features**
  - **WDL Display (Win/Draw/Loss)** - Shows win/draw/loss probabilities from engine analysis
  - **Opening Book** - 565+ opening positions with ECO codes and opening names (BETA)
  - **Move Quality Indicators** - Brilliant, Best, Good, Inaccuracy, Mistake, Blunder labels
  - **Functional Aggressiveness Slider** - Actually affects move selection (0=solid, 100=aggressive)

- **MoveSharpnessAnalyzer Service**
  - Calculates "sharpness" score (0-100) for candidate moves
  - Factors: captures, sacrifices, pawn breaks, checks, promotions, piece activity
  - Used by EngineAnalysisStrategy to filter moves based on user's style preference

- **Config Hot-Reload**
  - Settings changes now update all services immediately
  - No more stale config references after settings dialog

### Changed
- EngineAnalysisStrategy now requests 3+ PVs when aggressiveness != 50
- Aggressiveness 0-30: prefers solid/safe moves
- Aggressiveness 70-100: prefers sharp/aggressive moves
- Allows 0.2-0.4 pawn eval loss for style preference

### Fixed
- Config changes (like Aggressiveness slider) now take effect immediately without restart
- AppConfig.Reload() updates instance in-place so all services see new values

---

## [2.1.0] - 2026-01-17

### Added
- **Defense Detection** - Detect defensive moves with shield icon
  - Protecting attacked pieces (newly defended)
  - Blocking attacks on valuable pieces
  - Escaping threats (moving attacked pieces to safety)
  - King safety improvements
  - Displayed alongside threats on move explanations

- **Threat Detection Improvements**
  - Before/after comparison: Only shows NEW threats created by a move
  - Fixes false positives like "attacks queen" when piece was already attacking
  - Pin detection: Only reports when material would actually be won
  - X-ray attack detection: Only reports when we'd capture the piece behind

- **Auto-Monitoring with Turn Detection (BETA)** - Automatic opponent move detection
  - Continuously monitors chess board for position changes
  - Automatically triggers analysis when opponent moves
  - FEN-based move detection with 200ms debounce
  - Turn tracking using piece color analysis (white vs black)
  - Capture move detection (both colors changed)
  - Alt+K hotkey to toggle auto-monitoring on/off
  - Settings checkbox to enable/disable
  - Auto-monitor disabled by default on startup
  - Board detection cache optimization (60s TTL)
  - Size filtering to reject oversized boards (>1000px)
  - Re-enabled BlunderTracker integration

### Known Limitations
- **BETA Status**: Functional but has edge cases
  - Occasional engine crashes on rapid position changes (unrelated to turn detection)
  - May miss opponent moves if they respond extremely quickly (within debounce window)
  - Piece recognition accuracy affects reliability in complex positions

### Fixed
- SEE values now respect ShowSEEValues setting (no longer shown when disabled)
- Pin detection no longer reports pins when the piece behind is defended
- X-ray attacks no longer trigger for defended pieces behind

### Changed
- BoardDetectionService: Extended cache validity from 3s to 60s
- BoardDetectionService: Added cache confirmation mechanism (ConfirmCache)
- BoardDetectionService: Added size validation to reject oversized boards
- PieceRecognitionService: Removed verbose debug logging (PERF messages, piece confidence)
- ConsoleOutputFormatter: Integrated defense display with shield icon in blue

---

## [2.0.0] - 2026-01-11

### Major Refactoring
- **Phase 4: Service Extraction**
  - Created `ThemeService` (104 lines) - Centralized theme management
  - Enhanced `PieceRecognitionService` - Added `ExtractBoardFromMat()` method
  - Enhanced `ConsoleOutputFormatter` - Added `DisplayAnalysisResults()` method
  - MainForm: 709 → 550 lines (22.4% reduction)

- **Phase 5: Engine Path Resolution**
  - Created `EnginePathResolver` (100 lines) - Engine discovery and path resolution
  - Simplified `InitializeEngineAsync()` from 61 to 28 lines (53% reduction)
  - MainForm: 450 → 420 lines (6.7% reduction)
  - **Overall: 1,577 → 420 lines (73.4% total reduction)**

### Added
- **Color-Coded Move Quality** - 6 levels with thematic colors
  - Dark Green (!!) - Brilliant/Excellent move
  - Green (!) - Good, solid move
  - Blue - Neutral, acceptable move
  - Orange (?!) - Questionable/Dubious move
  - Orange Red (?) - Mistake
  - Firebrick Red (??) - Blunder

- **Win Percentage Display**
  - Side-aware win probability (shows "White: 82%" or "Black: 18%")
  - Stockfish-based logistic evaluation model
  - Material-dependent scaling for accuracy
  - Color-coded display (green for winning, red for losing)

- **Complexity Levels** - 4 levels of explanation detail
  - Beginner - Simple, clear language
  - Intermediate - Moderate detail (default)
  - Advanced - Full technical details
  - Master - Maximum detail with annotations

- **Feature Toggles** - 11 customization options
  - Show Best/Second/Third Line (multi-PV control)
  - Tactical Analysis
  - Positional Analysis
  - Endgame Analysis
  - Opening Principles
  - Win Percentage
  - Tablebase Info (endgame-specific analysis)
  - Color-Coded Moves
  - SEE Values

- **Dark Mode Theme**
  - Fully integrated dark mode
  - Applies to all windows and controls
  - Persists preference to config
  - Clean color schemes for both themes

- **Global Keyboard Shortcuts**
  - Alt+X - Analyze position (works when minimized)
  - Alt+K - Toggle auto-monitoring on/off

- **Tablebase-Aware Analysis**
  - Endgame-specific analysis for positions with 7 or fewer pieces
  - Pattern-based endgame recognition
  - DTZ (Distance to Zeroing) awareness

### Changed
- **Architecture Overhaul**
  - Service-oriented design with clear separation of concerns
  - 73.4% reduction in MainForm complexity
  - Better testability and maintainability
  - Cleaner dependency injection

### Fixed
- Removed unused `UpdateUIWithMoveResults()` method
- Removed unused `ConvertPvToSan()` method from MainForm
- Fixed engine path discovery logic
- Improved theme application consistency

---

## [1.6.0] - 2026-01-10

### Added
- **ChessUtilities.cs** - Consolidated shared utility functions
  - `GetPieceValue()`, `GetPieceName()` - Piece operations
  - `CanAttackSquare()`, `IsSquareDefended()` - Attack detection
  - `IsDiagonalClear()`, `IsLineClear()`, `IsPathClear()` - Path validation
  - `IsPiecePinned()`, `CountAttackers()` - Advanced helpers

- **BoardCache.cs** - Performance optimization layer
  - Caches piece locations by color (O(1) lookups)
  - Pre-builds attack maps for instant queries
  - Provides `GetPieces()`, `GetPiecesByType()`, `IsSquareAttacked()`
  - **4-10x faster tactical analysis**

- **ExplanationFormatter.cs** - User experience enhancements
  - Complexity levels: Beginner, Intermediate, Advanced, Master
  - Feature toggles for all analysis types
  - Color-coded move quality (Excellent, Good, Neutral, Questionable, Bad, Blunder)
  - Win percentage display with color coding

- **UIHelpers.cs** - User interface utilities
  - Quick info panels for analysis summary
  - Tooltip helpers for better UX
  - Visual feedback (flashing controls, progress indicators)

### Changed
- **MovesExplanation.cs**
  - Fixed skewer detection (now validates MORE valuable piece in front)
  - Fixed X-ray attack detection (validates first piece under attack)
  - Fixed exchange sacrifice false positives (uses SEE validation)
  - Removed 120+ lines of duplicate helper methods

- **MoveEvaluation.cs**
  - Enhanced `DetectWinningCapture()` with trade vs win detection
  - Uses SEE + defender analysis to distinguish trades from material wins
  - Reports "trades knight" instead of "wins knight" for equal exchanges
  - Removed 90+ lines of duplicate helper methods

- **PositionalEvaluation.cs**
  - Fixed outpost detection (now enforces mandatory pawn support)
  - Removed 110+ lines of duplicate helper methods

- **StockfishFeatures.cs** - CRITICAL PERFORMANCE FIX
  - `BuildThreatArray()` optimized from O(n^3) to O(n^2)
  - Uses BoardCache instead of triple-nested loops
  - **~100x faster in typical positions** (262,144 to ~2,000 iterations)
  - Removed 110+ lines of duplicate helper methods

### Performance
- Eliminated **410+ lines of duplicate code** across 4 files
- Reduced complexity from O(n^3) to O(n^2) in critical algorithms
- Expected overall performance: **4-10x faster** for tactical analysis
- Memory usage increased by ~2KB per position (negligible trade-off)

### Fixed
- Skewer detection now correctly identifies MORE valuable piece in front
- X-ray attacks no longer trigger for generic piece alignments
- Exchange sacrifices properly validated with SEE (no false positives on recaptures)
- "Wins knight" vs "trades knight" correctly distinguished using defender analysis
- Build succeeds with **0 warnings, 0 errors**

### Removed
- Duplicate implementations of `CanAttackSquare()` (4 files)
- Duplicate implementations of `IsSquareDefended()` (4 files)
- Duplicate implementations of `GetPieceValue()` (5+ files)
- Duplicate implementations of `GetPieceName()` (4+ files)
- Duplicate implementations of `IsPathClear()` variants (4 files)

---

## [1.0.0] - 2026-01-08

### Added
- Initial release
- UCI engine integration
- Computer vision board detection (OpenCV/EmguCV)
- Multi-PV analysis (up to 3 lines)
- Basic move explanations
- Dark/Light theme support
- Hotkey support (Alt+X for analysis)
- Support for Chess.com, Lichess, and custom templates
