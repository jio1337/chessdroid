# Changelog

All notable changes to ChessDroid will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [3.1.0] - 2026-02-09

### Added
- **Engine Arrows on Board** — Colored arrows showing engine recommended moves directly on the board
  - Green (best line), Yellow (2nd line), Red (3rd line) — matches PV line visibility settings
  - Toggle on/off via `ShowEngineArrows` checkbox in Settings > Display Options
  - Arrows clear on New Game, on move made, and during PV animation
- **[See line] — Load PV into Move Tree** — Clickable cyan `[See line]` link after each PV line
  - Loads the full PV as a variation branch in the move tree
  - Animated playback stepping through each move (400ms per move)
  - Cancels previous animation on new click, navigation, or board move
- **Free-Draw Right-Click Arrows** — Right-click arrows can now be drawn from any square to any square (no longer restricted to legal moves)
- **Auto-Analysis on Form Open** — Engine starts analyzing the starting position immediately when the form opens
- **Auto-Analysis on New Game** — Clicking New Game triggers auto-analysis instead of showing blank panel

### Changed
- **[See line] always creates variation** — PV lines inserted via [See line] now always create a variation branch, even when at the end of the move tree (previously appended to main line)
- **Engine arrows suppressed during animation** — Arrows hide during [See line] playback to avoid visual clutter

### Removed
- **Puzzle Mode** — Removed entirely to keep chessdroid focused as a pure analysis board
  - Deleted PuzzleForm, Puzzle model, PuzzleService, PuzzleExtractor (~2,100 lines)
  - Removed puzzle button, HintSquares rendering, puzzle config settings
  - Removed Puzzles folder copy rule from project file
- **SEE Display** — Removed `(SEE +X)` text from move explanations
  - No more misleading "wins knight (SEE +3)" on equal trades
  - SEE calculation still runs internally for sacrifice and brilliant move detection
  - Removed SEE checkbox from Settings form
  - `AppConfig.ShowSEEValues` kept for JSON backwards compatibility, defaults to `false`

### Fixed
- **Brilliant move false positives** — Fork safety check (forking piece must survive) and already-hanging piece check
- **Engine arrows persist after New Game** — Fixed by clearing arrows in `BtnNewGame_Click`

---

## [3.0.0] - 2026-02-05

### Major Change — Pure Analysis Board
chessdroid v3.0.0 is a complete pivot from a screen-reading tool to a pure analysis board application. All screen detection, computer vision, and auto-monitoring features have been removed. The Analysis Board (previously a secondary feature) is now the main and only entry point.

### Removed
- **Screen Capture & Board Detection** — Removed `ScreenCaptureService.cs`, `BoardDetectionService.cs`, `PieceRecognitionService.cs`, `BoardMonitorService.cs` (~1,400 lines)
- **MainForm** — Removed `MainForm.cs` and `MainForm.Designer.cs` (~900 lines). AnalysisBoardForm is now the main form
- **BoardVisualizer** — Removed debug visualization for detection (~100 lines)
- **Computer Vision Dependencies** — Removed Emgu.CV and OpenCvSharp4 NuGet packages (~50MB binary savings)
- **Global Hotkeys** — Removed Alt+X (analyze) and Alt+K (auto-monitor) since analysis is now automatic
- **Detection Settings** — Removed match threshold, Canny thresholds, min board area, debug cells, site selection from Settings
- **Website/Site Selection** — Removed site combobox and `PopulateSitesComboBox()` from SettingsForm

### Added
- **Zwischenzug Detection** — Detects intermediate moves (in-between moves) in tactical sequences
- **Blunder Explanations** — Shows WHY moves are bad with detailed reasoning, not just that they are blunders
- **Tactical Explanations for Implicit Sacrifices** — Explains sacrifices that aren't immediately obvious
- **Pin Checkbox** — Toggle pin detection display in settings

### Improved
- **Brilliant Move Detection** — Improved accuracy with better false positive filtering
  - Enhanced detection with more sophisticated sacrifice validation
  - Preserved real-time brilliant detection through ClassifyMoves
- **Mate Sorting** — Fixed sorting of mate-in-N evaluations in analysis output
- **Piece Set Persistence** — Piece set selection now persists across sessions
- **Binary Size** — ~50MB smaller without OpenCV dependencies
- **Startup** — Faster launch, no screen detection initialization needed

### Changed
- **Entry Point** — `Program.cs` now launches `AnalysisBoardForm(config)` directly instead of `MainForm`
- **Window Title** — Changed to `chessdroid v3.0.0`
- **Auto-Analysis** — Always on by default, no toggle needed
- **AppConfig** — Removed detection-related properties (MatchThreshold, CannyThresholds, MinBoardArea, ShowDebugCells, AutoMonitorBoard)
- **ThemeService** — Removed MainForm-specific `ApplyTheme()` overload
- **Settings Button** — Now accessible via ⚙ button directly on the Analysis Board

---

## [2.9.0] - 2026-02-01

### Added
- **Start from Current Position** - Engine matches can now begin from any board position
  - New "Start from current position" checkbox in Engine Match panel
  - Set up openings manually (e.g., London System) and have engines continue from there
  - Match log indicates when starting from custom position
- **Brilliant Move Detection in Matches** - Real-time brilliant move (!!) detection during engine vs engine games
  - Detects piece sacrifices that maintain or improve position
  - Shows "!!" symbol in move list and analysis log
  - Displays explanation (e.g., "sacrifices knight for decisive advantage")
- **Dynamic Template Selection** - Piece templates now auto-populate from Templates folder
  - Combobox in Analysis Board for switching piece sets on the fly
  - Settings form dynamically lists available template folders
  - Supports any folder name (Chess.com, Lichess, custom themes)

### Fixed
- **Analysis Board Pieces Combobox** - Fixed positioning and responsiveness on form resize

---

## [2.8.1] - 2026-01-31

### Fixed
- **Sliding Pieces Ignoring Blockers** - Queen, bishop, and rook could illegally move through other pieces in the Analysis Board click-to-move feature. Move validation now properly checks that the path is clear.

---

## [2.8.0] - 2026-01-30

### Added
- **Analysis Board** - Full-featured offline analysis board with interactive chess board
  - Drag-and-drop piece movement with legal move validation
  - Move tree with variations support for exploring alternative lines
  - Click on moves to navigate through game history
  - Flip board, new game, take back, and FEN load/copy functionality
- **PGN Import/Export** - Standard chess notation support
  - Import games from PGN files with header parsing
  - Export games with move history and metadata
  - Handles move numbers, comments, and variations
- **Move Classification** - Chess.com-inspired
  - Colored move quality symbols (Brilliant !!, Inaccuracy ?!, Mistake ?, Blunder ??) 
- **Evaluation Bar** - Visual representation of position evaluation
  - Smooth transitions between evaluations
  - Shows both centipawn and mate scores
- **Engine Matches** - Watch engines play against each other
  - Configurable time controls (depth, move time, total time + increment)
  - Real-time clock display with active side highlighting
  - Start/stop controls for match management
- **Analysis Caching** - Instant navigation through analyzed positions
  - Thread-safe ConcurrentDictionary for async operations
  - Cached evaluations, best moves, PVs, and WDL data

### Improved
- **Theme Centralization** - All Analysis Board colors now use ThemeService
  - Added 14+ new color properties to ColorScheme class
  - Centralized move classification symbol colors (Brilliant, Blunder, Inaccuracy, Mistake)
  - Reduced ~50 hardcoded RGB values to theme-based lookups
- **Regex Performance** - PGN parsing uses compiled regex patterns
  - 6 static compiled patterns for comments, variations, NAGs, continuations, move numbers
  - Faster parsing for large PGN files
- **Code Quality** - Various improvements for maintainability
  - Modern C# string slicing syntax (text[..^2] instead of Substring)
  - Better exception handling in UCI-to-SAN conversion
  - Removed dead code and unused variables

### Technical
- Added `MoveTree.cs` and `MoveNode.cs` for variation support
- Added `MoveQualityAnalyzer.cs` for centipawn loss classification
- Added `GameReviewResult.cs` and `MoveReviewResult.cs` models
- Extended `ThemeService.cs` with Analysis Board color properties
- Extended `AnalysisBoardForm.cs` with caching, variations, and PGN support

---

## [2.7.0] - 2026-01-26

### Added
- **Play Style Recommendations** - Style-based move suggestions now always visible when non-balanced
  - Shows "Recommended" section for Very Solid, Solid, Aggressive, and Very Aggressive styles
  - Displays even when recommended move matches engine's best (confirms style alignment)
  - Green coloring when shown alone (all lines hidden), orange when shown with best lines

### Fixed
- **Very Solid Play Style Not Working** - Critical fix for aggressiveness 0-40
  - Recommended move's PV was being trimmed before display, causing lookup failure
  - Now ensures recommended move's PV line is always included for display
  - Fixed format mismatch between bestMove and PV list entries
- **Evaluation Line Ordering** - PV lines now stay sorted by evaluation (best first)
  - Aggressiveness filter only changes the recommended move, not display order
  - Prevents confusing display where "Best line" showed worse eval than "Second best"
- **Sharpness Calculation** - More accurate style-based move selection
  - Central piece development (c-f files) now scores as slightly sharp (+3)
  - Flank pawn moves now score as solid (-5) in the opening
  - Removed incorrect penalty for knight/bishop development

### Changed
- **Book Moves Display** - Now shows SAN notation (Nf3) instead of UCI (g1f3)
- **Settings UI Reorganized**
  - Renamed "Lc0-Inspired Features" → "Play Style" (now just contains aggressiveness slider)
  - Moved "Show Opening Name", "Move Quality", "Show Book Moves" to Explanations section
  - Removed "Auto-Monitor Board" checkbox from settings (functionality still available via hotkey)

---

## [2.6.0] - 2026-01-25

### Added
- **Polyglot Opening Book Support** - Full support for industry-standard Polyglot `.bin` format
  - Ships with `codekiddy.bin` (~1M positions, 16MB)
  - Displays book moves with weights and percentages
  - Multi-book support (loads all `.bin` files from Books folder)
  - "Show Book Moves" toggle in Settings
- **Comprehensive ECO Opening Database** - 12,379 opening positions with names
  - Data sourced from [hayatbiralem/eco.json](https://github.com/hayatbiralem/eco.json)
  - Shows opening name with ECO code (e.g., "B01: Scandinavian Defense")
  - Loads from `ecoA.json` through `ecoE.json` in Books folder
  - Falls back to built-in 500+ position database if files missing
- **New OpeningDatabase Service** - Lazy-loaded singleton for fast opening lookups
  - Supports JSON (eco.json format) and TSV file formats
  - Uses FEN piece-placement as key for O(1) lookups
  - Automatically loads on first access

### Changed
- **Books folder structure** - Now contains:
  - `codekiddy.bin` - Polyglot opening book (replaces ABK format)
  - `ecoA-E.json` - ECO opening names database
- **Opening name display** - Enhanced from ~500 to 12,379+ recognized positions

### Removed
- **ABK book format support** - Removed `AbkBookReader.cs` and `AbkBookService.cs`
  - ABK format replaced by superior Polyglot `.bin` format
  - Polyglot is the industry standard used by Arena, Fritz, etc.

### Technical
- Added `PolyglotBookService.cs` - Multi-book loading and position lookup
- Added `PolyglotBookReader.cs` - Binary file parser for Polyglot format
- Added `PolyglotZobrist.cs` - Polyglot-compatible Zobrist hash computation
- Added `OpeningDatabase.cs` - ECO database loader and lookup service
- Added `BookMove.cs` model class
- Updated `chessdroid.csproj` to copy Books folder to output

---

## [2.5.1] - 2026-01-24

### Fixed
- **SEE explanation bug** - Fixed incorrect "wins X (SEE +Y)" when our pieces become vulnerable after the move
  - Added `GetHangingPieceValueAfterMove` to detect exposed pieces
  - Now checks if any attacker is worth less than our piece (vulnerable even if defended)
- **False "rook sacrifice" detection** - No longer shows "rook sacrifice" when piece is defended after capture
- **Redundant check phrases** - Removed "gives check", "check with attack" from explanations (shown in threats section)
- **"Creates threat on king"** - Removed ambiguous phrase, now properly handled by threat detection as "gives check"

### Improved
- **Brilliant move explanations** - Now shows meaningful context like "sacrifices rook for decisive advantage" instead of generic threat descriptions

### Removed
- **Color-Coded Moves setting** - Obsolete feature replaced by v2.5 Chess.com-style classification

---

## [2.5.0] - 2026-01-24

### Added
- **Chess.com-style move classification** - Second/third best moves now show classification labels when best move is the "only winning move":
  - **BLUNDER (??)** - Win probability drop ≥ 20%
  - **MISTAKE (?)** - Win probability drop 10-20%
  - **INACCURACY (?!)** - Win probability drop 5-10%
- **Brilliant move detection (!!)** - Identifies piece sacrifices that work, following Chess.com criteria:
  - Must be a true sacrifice (give up more material than captured)
  - Piece not defended and no pawn can recapture
  - Position still good after the move
  - Wasn't already completely winning before
- **Greek Gift sacrifice detection** - Recognizes the classic Bxh7+/Bxh2+ bishop sacrifice pattern
- **Win probability calculation** - Uses standard logistic function (same as Chess.com/Lichess) to convert centipawn evaluation to win percentage

### Improved
- **Static Exchange Evaluation (SEE)** - Major enhancements:
  - Now check-aware: filters out attackers that can't recapture while dealing with check
  - Now pin-aware: filters out pinned pieces that can't legally capture
  - Fixed bug where SEE was called on post-capture board instead of original position
  - Added source square parameter for accurate attacker removal
- **"Only winning move" detection** - More accurate triggering based on evaluation swing thresholds
- **Move explanations** - Removed misleading "(loses exchange)" suffix from best moves

### Fixed
- **False "wins pawn" for king captures** - SEE no longer shows incorrect values when king captures a piece
- **Brilliant false positives** - Multiple safeguards prevent mislabeling normal captures as brilliant:
  - Capturing equal or higher value piece is not a sacrifice
  - Piece defended after capture is not a sacrifice
  - Pawn can recapture = not a sacrifice

---

## [2.4.1] - 2026-01-24

### Fixed
- **Misleading "equalizes" description** - Moves leaving you at a disadvantage (0.5-1.5 pawns behind) now correctly show "under slight pressure" instead of "equalizes"
- **Incorrect keyboard shortcuts in documentation** - Help popup and website now show correct hotkeys: Alt+X (analyze) and Alt+K (auto-monitor); removed non-existent Alt+A, Alt+S, and Escape shortcuts

### Improved
- **Responsive MainForm** - Window can now be freely resized; console area expands/shrinks with window, buttons stay anchored to their corners; added minimum size (350x300) to prevent layout issues
- **Cleaner console output** - Removed colored background highlights from move line headers; headers now use bold foreground text only for a cleaner look
- **Dark mode readability** - Best lines now use lighter, more readable colors in dark mode (PaleGreen, Khaki, LightCoral) instead of darker variants that were hard to see

---

## [2.4.0] - 2026-01-23

### Fixed
- **Feature toggles now actually work** - Tactical Analysis, Positional Analysis, and Opening Principles toggles were being saved but never read to filter output
- **Zugzwang explanation on winning moves** - Fixed bug where winning moves were incorrectly marked with "?" and "zugzwang position (any move worsens position)" explanation
- Zugzwang is now properly treated as a position characteristic shown in endgame insights, not as a per-move explanation

### Changed
- **Tactical Analysis toggle** now properly controls pins, forks, skewers, sacrifices, and tempo attacks in move explanations
- **Positional Analysis toggle** now properly controls pawn structure, outposts, mobility, king safety, and development advice
- **Opening Principles toggle** now properly controls opening move descriptions
- **Endgame Analysis toggle** now consolidates all endgame insights (opposition, rule of square, king activity, drawing patterns, zugzwang)

### Removed
- **Fake Tablebase feature** - Removed misleading "Tablebase Info" toggle that was not using real tablebases (just duplicated existing endgame analysis)
- Cleaned up ~160 lines of redundant tablebase simulation code from AdvancedAnalysis.cs

### Added
- **Advanced endgame heuristics** inspired by Stockfish/Ethereal:
  - Rule of the Square (unstoppable pawn detection)
  - King centralization scoring with lookup tables
  - Opposition detection (direct and distant)
  - Wrong color bishop + rook pawn draw detection
  - Insufficient material detection (FIDE rules)
  - Mop-up evaluation (push enemy king to corner)
  - Passed pawn advancement bonuses
  - King tropism to passed pawns
  - Fortress detection

---

## [2.3.0] - 2026-01-22

### Performance
- **3-5x faster analysis** through comprehensive performance optimizations
- **60-80% fewer memory allocations** via object pooling
- **64x faster hash updates** with incremental Zobrist hashing (O(1) vs O(64))
- **Eliminated 17+ board allocations per analysis** using BoardPool pattern
- **O(1) king position lookups** - Cached king positions eliminate O(n²) board scans
- **8x faster pin detection** - BoardCache provides O(n) sliding piece lookup vs O(64) board scan

### Technical Improvements
- **Incremental Zobrist Hashing** - Hash updates now O(1) instead of O(64) via XOR operations
- **Board Object Pooling** - ConcurrentBag-based pool eliminates temporary board allocations
- **Consolidated Direction Constants** - Shared static arrays for piece movement directions
- **King Position Caching** - `GetKingPosition()` method with lazy initialization
- **Pin Detection with BoardCache** - `GetSlidingPieces()` method for fast Bishop/Rook/Queen lookups
- **Fixed Zobrist Hash Bug** - ChessBoard indexer now properly invalidates cached hash

### Implementation Details
- Added BoardPool.cs with thread-safe pooling (max 50 boards)
- Migrated 17+ locations across ThreatDetection, DefenseDetection, MovesExplanation, MoveEvaluation, ChessUtilities
- ChessBoard.SetPiece() and indexer now use XOR-based incremental hashing
- All temporary board creation now uses `using var pooled = BoardPool.Rent(board)` pattern
- Added `BishopDirections`, `RookDirections`, `QueenDirections`, `KnightOffsets` to ChessUtilities
- Replaced 10+ O(n²) king-finding loops with O(1) `GetKingPosition()` calls
- Added `GetSlidingPieces()` to BoardCache for pin detection optimization
- `DetectPins` and `DetectPinsAgainstUs` now use BoardCache when available

### Impact
- Analysis time: ~200ms → ~50ms (4x faster)
- Board copies: 17-19 → 0-2 (pooled)
- Hash computation: O(64) → O(1) per update
- King lookups: O(64) → O(1)
- Pin detection: O(64) → O(~8) per call
- Memory pressure significantly reduced during deep analysis

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
