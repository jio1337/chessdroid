# ChessDroid Architecture Documentation

## Overview

ChessDroid is an advanced chess analysis tool that combines tactical pattern recognition with deep positional understanding inspired by world-class chess engines (Ethereal and Stockfish).

---

## System Architecture (v2.5.1)

```
┌─────────────────────────────────────────────────────────────────┐
│                    ChessDroid Core (MainForm.cs)                │
│           Refactored: 1,577 → 420 lines (73.4% reduction)      │
└────────────────────┬────────────────────────────────────────────┘
                     │
         ┌───────────┴────────────┬──────────────────────────┐
         │                        │                          │
         ▼                        ▼                          ▼
┌─────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│  Service Layer  │     │  UCI Engine      │     │   UI Services    │
│  (Orchestration)│     │  Integration     │     │   (Display)      │
└────────┬────────┘     └──────────────────┘     └─────────┬────────┘
         │                                                   │
         ├──────────────────────┬────────────────────────────┤
         │                      │                            │
         ▼                      ▼                            ▼
┌─────────────────┐  ┌──────────────────┐      ┌──────────────────┐
│MoveOrchestrator │  │EngineAnalysis    │      │ConsoleOutput     │
│                 │  │Strategy          │      │Formatter         │
└────────┬────────┘  └──────────────────┘      └──────────────────┘
         │
         ├─────────────────┬─────────────────┬──────────────────┐
         │                 │                 │                  │
         ▼                 ▼                 ▼                  ▼
┌─────────────┐  ┌─────────────┐  ┌─────────────┐   ┌──────────────┐
│MovesExplan- │  │MoveEvaluation│  │Positional   │   │  Endgame     │
│ation        │  │   (SEE)      │  │Evaluation   │   │  Analysis    │
└──────┬──────┘  └──────────────┘  └─────────────┘   └──────────────┘
       │
       ├──────────────────┬─────────────────┬────────────────┐
       │                  │                 │                │
       ▼                  ▼                 ▼                ▼
┌──────────┐    ┌──────────────┐  ┌──────────────┐  ┌────────────┐
│Stockfish │    │  Advanced    │  │  Explanation │  │ UI Helpers │
│Features  │    │  Analysis    │  │  Formatter   │  │            │
└────┬─────┘    └──────────────┘  └──────────────┘  └────────────┘
     │
     │      ┌────────────────────────────────────────────────────────┐
     │      │              Performance & Utility Layer               │
     │      │  ┌──────────────┐  ┌──────────────┐                   │
     └─────→│  │ChessUtilities│  │  BoardCache  │                   │
            │  │(Shared Logic)│  │(Performance) │                   │
            │  └──────────────┘  └──────────────┘                   │
            └────────────────────────────────────────────────────────┘

                 ┌────────────────────────────────────────┐
                 │         Supporting Services            │
                 │  ┌─────────────┐  ┌─────────────────┐ │
                 │  │ThemeService │  │EnginePathResolver│ │
                 │  │(UI Theming) │  │  (Discovery)    │ │
                 │  └─────────────┘  └─────────────────┘ │
                 └────────────────────────────────────────┘
```

---

## Module Descriptions

### 1. **MovesExplanation.cs** (Orchestrator)
**Purpose:** Main coordinator that combines all analysis modules into human-readable explanations

**Key Responsibilities:**
- Parse UCI engine output (best moves, evaluations, PV lines)
- Coordinate tactical and positional analysis
- Generate natural language explanations
- Priority management (which features to check first)

**Explanation Priority Order:**
```
1. Singular moves ("only good move")
2. Forced moves (check evasion)
3. Threat creation
4. Tactical patterns (pins, forks, skewers, etc.)
5. Sacrifices (exchange, piece)
6. SEE-enhanced captures
7. Pawn structure analysis
8. Piece activity evaluation
9. Endgame patterns
10. Opening principles
11. Win rate evaluation
12. Generic positional improvement
```

**Performance:** O(n) where n = number of pieces on board

---

### 2. **PositionalEvaluation.cs** (Ethereal Phase 1)
**Purpose:** Evaluate positional features inspired by Ethereal's evaluate.c

**Features:**

#### **Pawn Structure:**
- `DetectPassedPawn()` - No enemy pawns blocking promotion
- `DetectConnectedPawns()` - Mutually supporting pawns
- `DetectIsolatedPawn()` - No friendly pawns on adjacent files
- `DetectDoubledPawns()` - Multiple pawns on same file
- `DetectBackwardPawn()` - Stuck behind neighbors

#### **Piece Activity:**
- `DetectOutpost()` - Piece immune to pawn attacks
- `DetectHighMobility()` - Piece with many legal moves
- `DetectLongDiagonalControl()` - Bishop on powerful diagonal
- `DetectCentralControl()` - Controlling d4, e4, d5, e5

#### **King Safety:**
- `DetectKingShelter()` - Pawn shield strength
- `DetectWeakKingSquares()` - Vulnerable squares near king

**Complexity:**
- Pawn structure: O(n) per pawn
- Piece activity: O(n²) for mobility
- King safety: O(1) for shelter check

**Optimization Tips:**
- Cache pawn positions per file
- Use bitboards for mobility calculation (future improvement)

---

### 3. **MoveEvaluation.cs** (Ethereal Phase 2)
**Purpose:** Advanced move evaluation with Static Exchange Evaluation (SEE)

**Key Features:**

#### **Static Exchange Evaluation (SEE):**
```csharp
StaticExchangeEvaluation(board, targetRow, targetCol, attackingPiece, isWhite)
```
- Simulates all captures on a square
- Returns final material balance
- Uses MVV-LVA (Most Valuable Victim - Least Valuable Attacker)

**Algorithm:**
```
1. Initial capture: +victimValue
2. Find all attackers to square (both sides)
3. Alternately capture with least valuable piece
4. Stop when one side refuses to recapture (would lose material)
5. Return final material balance
```

**Example:**
```
Position: White knight on d5, Black rook on e7
Move: Nxe7 (capture rook)

SEE Calculation:
1. Knight takes rook: +5
2. Black queen takes knight: +5 - 3 = +2
3. White bishop takes queen: +2 + 9 = +11
Result: SEE = +11 (winning capture)
```

**Complexity:** O(n) where n = number of pieces attacking square

#### **Move Interestingness Scoring:**
- Checks: 10,000 points
- Good captures (SEE > 0): 1,000-5,000 points
- Promotions: 8,000 points
- Quiet moves: 0-100 points
- Bad captures (SEE < 0): -5,000 points

#### **Position Improvement Tracking:**
- `IsPositionImproving()` - Eval increasing by 0.3+
- `IsPositionWorsening()` - Eval decreasing by 0.3+

---

### 4. **EndgameAnalysis.cs** (Ethereal Phase 3)
**Purpose:** Endgame-specific knowledge and pattern recognition

**Game Phase Detection:**
```
Piece Count    Phase
0-5           Late Endgame (basic endgames)
6-7           Endgame
8-11          Transition
12+           Middlegame/Opening
```

**Endgame Pattern Recognition:**
- King and Pawn vs King (KPvK)
- Opposite-colored bishops (drawish)
- Rook endgames (technical)
- Queen endgames (tactical)
- Bare king (forced checkmate)

**Special Features:**
- `IsPotentialZugzwang()` - Blocked pawns, low mobility
- `DetectMaterialImbalance()` - 3+ point differences
- `DetectQualityImbalance()` - Rook vs minors, Queen vs R+minor

**Zugzwang Detection Algorithm:**
```
1. Check if endgame (≤6 pieces)
2. Count blocked pawns
3. If >50% pawns blocked → potential zugzwang
4. If ≤4 pieces total → likely zugzwang
```

---

### 5. **StockfishFeatures.cs** (Stockfish High Priority)
**Purpose:** Stockfish-inspired search and evaluation features

**Key Features:**

#### **Threat Array Detection:**
```csharp
BuildThreatArray(board, forWhite)
```
- Maps all squares attacked by each side
- Groups attackers by target square
- Identifies low-value attackers on high-value pieces

**Use Case:**
```
Threat Array[e7] = [(Knight@d5, value=3), (Pawn@f6, value=1)]
If Queen on e7 → "queen attacked by pawn" (major threat!)
```

#### **Singular Move Detection:**
- Compares best move eval vs second-best eval
- Gap ≥ 1.5 pawns → singular move
- Indicates "only good move" positions

**Algorithm:**
```
if (|bestEval - secondEval| >= 1.5):
    return "singular move"
```

#### **Move Quality Classification:**
```
Eval Change    Quality
+1.0 or more   Excellent
+0.3 to +1.0   Good
-0.3 to +0.3   Marginal
-1.0 to -0.3   Bad
-1.0 or worse  Terrible
```

#### **Futility Detection:**
- Identifies moves that don't improve position enough
- Skips futility check for tactical moves
- Threshold: |evalChange| < 0.1 pawns

---

### 6. **AdvancedAnalysis.cs** (Medium Priority)
**Purpose:** Advanced user-friendly features

**Key Features:**

#### **Win Rate Model (Stockfish WDL):**
```csharp
EvalToWinningPercentage(eval, materialCount)
```

**Formula:**
```
WinRate = 50 + 50 * (2 / (1 + exp(-eval * scale)) - 1)

Scale = {
    0.0015  if materialCount ≤ 10 (endgame)
    0.0012  if materialCount ≤ 20 (transition)
    0.0010  if materialCount > 20 (opening/middlegame)
}
```

**Example Outputs:**
```
Eval +3.0 → 95% win probability → "winning position"
Eval +2.0 → 80% win probability → "clearly better position"
Eval +1.0 → 65% win probability → "advantage"
Eval +0.5 → 55% win probability → "slight edge"
```

#### **Low-Ply History Tracking:**
- Tracks opening moves (first 20 plies)
- Records success/failure of moves
- Identifies historically strong openings

**Built-in Opening Knowledge:**
```
e2e4, d2d4 → "controls center"
Nf3, Nc3  → "develops piece to good square"
```

#### **Position Complexity:**
- `IsSharpPosition()` - Many pieces + material imbalance
- `IsEvaluationUnstable()` - Large eval swings
- Output: "sharp tactical position" or "technical endgame"

---

### 7. **OptimizationHelpers.cs** (Performance Layer)
**Purpose:** Caching and performance optimization

**Caching System:**
```csharp
// Clear at start of new position
OptimizationHelpers.ClearCache();

// Cached operations (O(1) after first call)
var (white, black, total) = GetPieceCounts(board);
int balance = GetMaterialBalance(board);
```

**Fast Utility Functions:**
- `GetPieceValueFast()` - Switch expression (faster than dictionary)
- `IsPathClearFast()` - Optimized path checking
- `CanAttackFast()` - No safety checks (use carefully)
- `IsSquareAttackedFast()` - Early exit on first attacker

**Batch Operations:**
- `FindPieces()` - Get all pieces of a type
- `FindAttackers()` - All pieces attacking a square
- `IsSquareAttackedFast()` - Boolean check with early exit

**Pre-computed Constants:**
```csharp
CENTRAL_SQUARES      // d4, e4, d5, e5
EXTENDED_CENTER      // c3-f6 square range
FILES                // ['a'..'h']
PIECE_VALUES         // Dictionary for O(1) lookup
```

**Performance Impact:**
- 30-50% faster position analysis with caching
- Reduced memory allocations
- Early exit optimizations save 20-40% CPU cycles

---

### 8. **ChessUtilities.cs** (Core Performance Layer) ⚡ NEW
**Purpose:** Consolidated shared utility functions used across all analysis modules

**Key Features:**

#### **Piece Operations:**
```csharp
GetPieceValue(pieceType)    // Material values (P=1, N=3, B=3, R=5, Q=9, K=100)
GetPieceName(pieceType)     // Human-readable names
```

#### **Attack Detection:**
```csharp
CanAttackSquare(board, fromRow, fromCol, piece, toRow, toCol)
IsSquareDefended(board, row, col, byWhite)
CountAttackers(board, row, col, byWhite)
```

#### **Path Validation:**
```csharp
IsDiagonalClear(board, fromRow, fromCol, toRow, toCol)
IsLineClear(board, fromRow, fromCol, toRow, toCol)
IsPathClear(board, fromRow, fromCol, toRow, toCol)  // Both diagonal & line
```

#### **Advanced Helpers:**
```csharp
IsPiecePinned(board, row, col)              // Detects absolute/relative pins
GetAttackers(board, row, col, byWhite)      // Returns list of attackers
```

**Performance Impact:**
- **Code Reuse:** Eliminated 410+ lines of duplicate code across 4 files
- **Consistency:** Single source of truth for all chess logic
- **Maintainability:** Bug fixes apply to all modules simultaneously

**Before Refactoring (v1.5):**
- `CanAttackSquare()` duplicated in: MovesExplanation, MoveEvaluation, PositionalEvaluation, StockfishFeatures
- `IsSquareDefended()` duplicated 4 times
- `GetPieceValue()` duplicated 5+ times

**After Refactoring (v1.6):**
- All modules import `ChessUtilities`
- Single implementation, tested once
- Easier to add new features

---

### 9. **BoardCache.cs** (Performance Optimization Layer) ⚡ NEW
**Purpose:** Eliminates repeated O(n²) board scans by caching piece locations and attack maps

**Architecture:**
```csharp
public class BoardCache
{
    private List<(int row, int col, char piece)> whitePieces;
    private List<(int row, int col, char piece)> blackPieces;
    private HashSet<(int row, int col)> whiteAttacks;
    private HashSet<(int row, int col)> blackAttacks;
    private (int row, int col) whiteKingPos;
    private (int row, int col) blackKingPos;
}
```

**Core Methods:**
```csharp
// FAST: O(1) lookups
GetPieces(isWhite)                    // All pieces for a color
GetPiecesByType(type, isWhite)        // Specific piece type
IsSquareAttacked(row, col, byWhite)   // HashSet lookup (O(1))
GetKingPosition(isWhite)              // Cached position (O(1))

// OPTIMIZED: Only iterate over relevant pieces
CountAttackers(row, col, byWhite)     // Use piece list instead of 64 squares
GetAttackers(row, col, byWhite)       // Return specific attackers
```

**Performance Analysis:**

**Traditional Approach (Without Cache):**
```csharp
// O(n²) - Scan entire 8x8 board for each query
for (int r = 0; r < 8; r++)
    for (int c = 0; c < 8; c++)
        if (board.GetPiece(r, c) == targetPiece)
            // Found it!
```

**Cached Approach (With BoardCache):**
```csharp
// O(1) or O(n) where n = number of pieces (~16), not squares (64)
var pieces = cache.GetPiecesByType(PieceType.Rook, true);
// Instant lookup
```

**Critical Optimization - StockfishFeatures.BuildThreatArray():**
- **Before (v1.5):** O(n³) triple-nested loop = 262,144 iterations worst case
- **After (v1.6):** O(n²) using BoardCache = ~2,000 iterations typical case
- **Performance:** ~100x faster in typical positions

**Memory vs Speed Trade-off:**
- **Memory Cost:** ~2KB per position (piece lists + attack maps)
- **Speed Benefit:** 4-10x faster tactical analysis
- **Verdict:** Excellent trade-off for modern systems

**When to Use BoardCache:**
- Multiple queries on the same position
- Threat detection algorithms
- SEE (Static Exchange Evaluation)
- Tactical pattern recognition

**When NOT to Use BoardCache:**
- Single query on a position
- Rapidly changing board states (each move invalidates cache)

---

### 10. **ThemeService.cs** (UI Theme Management) 🎨 NEW v2.0
**Purpose:** Centralized theme management for dark and light modes

**Architecture:**
```csharp
public class ColorScheme
{
    public Color BackgroundColor { get; set; }
    public Color LabelBackColor { get; set; }
    public Color ForeColor { get; set; }
    // ... 8 color properties
}

public class ThemeService
{
    private static readonly ColorScheme DarkScheme = new ColorScheme { ... };
    private static readonly ColorScheme LightScheme = new ColorScheme { ... };

    public static void ApplyTheme(Form form, ..., bool isDarkMode)
}
```

**Key Features:**
- Dark mode color scheme (RGB 45, 45, 48 base)
- Light mode color scheme (WhiteSmoke base)
- Applies to all UI controls consistently
- Suspends/Resumes layout for performance

**Impact:**
- Reduced MainForm `ApplyTheme()` from 59 lines to 3 lines
- Reusable across multiple forms
- Single source of truth for theming

---

### 11. **BoardMonitorService.cs** (Auto-Monitoring with Turn Detection) ⚡ NEW - BETA
**Purpose:** Continuously monitors chess board for position changes and automatically detects moves

**Status:** BETA - Functional but has known limitations (see Known Limitations section)

**Architecture:**
```csharp
public class BoardMonitorService
{
    private System.Windows.Forms.Timer scanTimer;       // 1000ms interval
    private string lastDetectedFEN;                     // Previous position
    private const int DEBOUNCE_MS = 200;                // Position stability wait

    public void StartMonitoring(bool userIsWhite);
    public void StopMonitoring();
    public bool IsMonitoring();

    // Event triggered when user's turn begins
    public event EventHandler<TurnChangedEventArgs> UserTurnDetected;
}
```

**Key Features:**

#### **FEN-Based Move Detection:**
- Compares current board FEN with last detected FEN
- Detects position changes (moves) automatically
- 200ms debounce ensures position stability

#### **Turn Tracking Algorithm:**
```csharp
// Analyzes which pieces changed color
bool whiteMoved = DidWhitePiecesMove(oldFEN, newFEN);
bool blackMoved = DidBlackPiecesMove(oldFEN, newFEN);

if (whiteMoved && blackMoved)
{
    // Both colors changed = capture move
    // Use turn state from PositionStateManager
}
else if (whiteMoved)
{
    // White moved, now Black's turn
}
else if (blackMoved)
{
    // Black moved, now White's turn
}
```

#### **Automatic Analysis Trigger:**
- Monitors board every 1 second
- Detects opponent moves via FEN comparison
- Triggers analysis only when it becomes user's turn
- Prevents spam with `moveInProgress` guard

**Performance:**
- CPU overhead: ~5-10% during active monitoring
- Scan interval: 1000ms (configurable)
- Debounce delay: 200ms
- Memory footprint: <10KB (stores 2 FEN strings)

**Integration:**
- Integrates with existing BoardDetectionService (60s cache)
- Uses PieceRecognitionService for piece identification
- Triggers MainForm.ExecuteMoveAsync() on user's turn
- Alt+K hotkey toggles monitoring on/off
- Settings checkbox for persistent enable/disable

**Known Limitations (BETA):**
- Occasional engine crashes on rapid position changes (unrelated to turn detection)
- May miss opponent moves if they respond extremely quickly (within debounce window)
- Piece recognition accuracy affects reliability in complex positions
- TODO v3.1: Improve robustness and handle edge cases

**Cache Optimizations:**
- BoardDetectionService cache extended from 3s to 60s TTL
- ConfirmCache() method refreshes timestamp only when board is validated
- Size filtering rejects oversized boards (>1000px) to prevent false detections

---

### 12. **ThreatDetection.cs** (Threat Analysis) ⚔️ NEW
**Purpose:** Detects chess threats for both sides, showing NEW threats created by moves

**Architecture:**
```csharp
public static class ThreatDetection
{
    public class Threat
    {
        public string Description { get; set; }
        public ThreatType Type { get; set; }
        public int Severity { get; set; }  // 1-5
        public string Square { get; set; }
    }

    // Main methods
    public static List<Threat> AnalyzeThreatsAfterMove(board, move, isWhite);
    public static List<Threat> AnalyzeOpponentThreats(board, weAreWhite);
}
```

**Key Features:**

**Before/After Comparison:**
- Detects threats BEFORE the move
- Detects threats AFTER the move
- Filters to show only NEW threats created by the move
- Prevents false positives like "attacks queen" when already attacking

**Threat Types Detected:**
- HangingPiece - Undefended piece under attack
- MaterialWin - Attacking higher value piece
- Fork - Attacking multiple pieces
- Pin - Piece pinned to more valuable piece/king
- Skewer - Attacking through a piece
- DiscoveredAttack - Moving reveals attack
- CheckmateThreat - Threatening mate
- Check - Giving check
- Promotion - Pawn about to promote
- TrappedPiece - Piece with no escape

**Pin Detection Algorithm:**
```csharp
// Only report pin if:
// 1. Absolute pin (to king) - always report
// 2. OR we would WIN the piece behind:
//    - Piece is undefended
//    - OR we'd trade up (our value < their value)
```

**Performance:** O(n) where n = pieces on board, returns top 3 threats

---

### 13. **DefenseDetection.cs** (Defense Analysis) 🛡️ NEW
**Purpose:** Detects defensive aspects of moves (protecting pieces, blocking attacks)

**Architecture:**
```csharp
public static class DefenseDetection
{
    public class Defense
    {
        public string Description { get; set; }
        public DefenseType Type { get; set; }
        public int Value { get; set; }  // Material value defended
        public string Square { get; set; }
    }

    public static List<Defense> AnalyzeDefensesAfterMove(board, move, isWhite);
}
```

**Defense Types Detected:**
- ProtectPiece - Newly defending a piece (knight, bishop, rook, queen)
- ProtectPawn - Newly defending a pawn
- BlockAttack - Blocking an attack on valuable piece
- Escape - Moving attacked piece to safety
- ProtectKing - Improving king safety

**Detection Algorithm:**
```csharp
// 1. Find OUR pieces that are attacked by opponent
// 2. Compare BEFORE/AFTER the move:
//    - Which pieces are NOW defended that weren't before?
//    - Which attacked pieces moved to safety?
// 3. Only report significant defenses (value >= 1)
```

**Integration:**
- Displayed in ConsoleOutputFormatter with 🛡 icon
- Uses CornflowerBlue color for visibility
- Shows alongside threats on same line: "→ explanation | 🛡 defends pawn"

**Performance:** O(n²) where n = pieces on board, returns top 2 defenses

---

### 14. **BlunderTracker.cs** (Blunder Detection State) ✅ RE-ENABLED
**Status:** Re-enabled in conjunction with auto-monitoring feature

**Purpose:** Tracks evaluation history and displays blunder warnings

**Key Features:**
- Compares current evaluation vs previous evaluation
- Displays warning if >2 pawn swing detected
- Orange background visual indicator for blunders
- Shows evaluation drop (e.g., "+2.0 → -5.0")

**Integration with Auto-Monitoring:**
```csharp
// MainForm.ExecuteMoveAsync()
blunderTracker.UpdateBoardChangeTracking(completeFen, evaluation);
consoleFormatter?.DisplayAnalysisResults(
    bestMove, evaluation, pvs, evaluations, completeFen,
    blunderTracker.GetPreviousEvaluation(), // Pass previous eval
    config?.ShowSecondLine == true,
    config?.ShowThirdLine == true);
double? currentEval = MovesExplanation.ParseEvaluation(evaluation);
blunderTracker.SetPreviousEvaluation(currentEval);
```

**Blunder Detection Logic:**
- Implemented in ConsoleOutputFormatter.DisplayAnalysisResults()
- Compares previous eval vs current eval
- Accounts for side to move (eval signs)
- Displays blunder warning if >2 pawn drop detected

---

### 13. **EnginePathResolver.cs** (Engine Discovery) 🔍 NEW v2.0
**Purpose:** Automatic discovery and resolution of chess engine paths

**Architecture:**
```csharp
public class EnginePathResolver
{
    private readonly AppConfig config;

    public (string enginePath, bool wasAutoDiscovered) ResolveEnginePath();
    private string DiscoverFirstAvailableEngine();
    public bool ValidateEnginePath(string enginePath);
    public string[] GetAvailableEngines();
}
```

**Key Features:**

**Auto-Discovery Algorithm:**
```
1. Check if config has SelectedEngine
2. If not:
   a. Scan Engines folder for .exe files
   b. Return first found engine
   c. Save to config for next time
   d. Fallback to "stockfish.exe" if none found
3. Combine with engines path
4. Return (enginePath, wasAutoDiscovered)
```

**Path Resolution:**
- Handles config-based engine selection
- Scans Engines folder automatically
- Saves discovered engines to config
- Validates engine file existence

**Impact:**
- Reduced `InitializeEngineAsync()` from 61 lines to 28 lines (53% reduction)
- Reusable engine discovery logic
- Better error handling and validation

---

### 14. **ConsoleOutputFormatter.cs** (Enhanced v2.0) 📊 ENHANCED
**Purpose:** Rich text console output with color coding, blunder warnings, and multi-line analysis

**New Features in v2.0:**

**DisplayAnalysisResults():**
```csharp
public void DisplayAnalysisResults(
    string bestMove, string evaluation,
    List<string> pvs, List<string> evaluations,
    string completeFen, double? previousEvaluation,
    bool showSecondLine, bool showThirdLine)
```

**Capabilities:**
- Automatic blunder detection and visual warnings
- Side-aware win percentage display
- Display up to 3 analysis lines (best, 2nd, 3rd)
- Color-coded move quality (6 levels)
- Rich text formatting with custom fonts
- Explanation integration with MovesExplanation

**Blunder Warning System:**
```csharp
if (isBlunder)
{
    DisplayBlunderWarning(blunderType, evalDrop, whiteBlundered);
    // Shows: "⚠️ BLUNDER! White lost 5.2 pawns"
}
```

**Move Quality Colors:**
- Excellent (!!) - Forest Green (34, 139, 34)
- Good (!) - Medium Sea Green (60, 179, 113)
- Neutral - Steel Blue (70, 130, 180)
- Questionable (?!) - Orange (255, 165, 0)
- Bad (?) - Orange Red (255, 69, 0)
- Blunder (??) - Firebrick (178, 34, 34)

**Impact:**
- Replaced 82-line `UpdateUIWithMoveResults()` method in MainForm
- Centralized all console display logic
- Enhanced user experience with visual feedback

---

### 15. **ExplanationFormatter.cs** (Enhanced v1.6+) 🎛️ ENHANCED
**Purpose:** Customize explanation complexity and feature toggles

**Complexity Levels:**
```csharp
public enum ComplexityLevel
{
    Beginner,       // Simple language, no jargon
    Intermediate,   // Moderate detail (default)
    Advanced,       // Full technical details
    Master          // Maximum detail with annotations
}
```

**Feature Toggles (9 total):**
- ShowTacticalAnalysis
- ShowPositionalAnalysis
- ShowEndgameAnalysis
- ShowOpeningPrinciples
- ShowWinPercentage
- ShowSEEValues
- ShowBestLine (always enabled)
- ShowSecondLine
- ShowThirdLine

**Simplification for Beginners:**
```csharp
"knight on strong outpost (SEE +6)"
→ "knight in good position (wins 6)"

"creates threat on undefended queen"
→ "attacks queen"
```

**Impact:**
- Customizable user experience for all skill levels
- Reduces information overload for beginners
- Enables power users with full technical details

---

## Data Flow

### Typical Analysis Flow:

```
1. User requests analysis
   ↓
2. UCI Engine calculates best moves + evaluations
   ↓
3. MovesExplanation.GenerateMoveExplanation() called
   ↓
4. Parse move notation (e2e4)
   ↓
5. Create temp board with move applied
   ↓
6. Check features in priority order:
   │
   ├─→ StockfishFeatures.IsSingularMove()
   ├─→ StockfishFeatures.DetectThreatCreation()
   ├─→ DetectTacticalPattern() [30+ patterns]
   ├─→ MoveEvaluation.DetectWinningCapture() [SEE]
   ├─→ PositionalEvaluation.DetectPassedPawn()
   ├─→ PositionalEvaluation.DetectOutpost()
   ├─→ EndgameAnalysis.DetectKPvK()
   ├─→ AdvancedAnalysis.GetWinningChanceDescription()
   └─→ Generic fallback
   ↓
7. Combine top 2 reasons
   ↓
8. Return human-readable explanation
```

---

## Performance Characteristics

### Time Complexity by Module:

| Module | Average Case | Worst Case | Notes |
|--------|-------------|------------|-------|
| Tactical Detection | O(n) | O(n²) | n = pieces on board |
| Pawn Structure | O(p) | O(p²) | p = pawns on board |
| Piece Activity | O(n) | O(n²) | With mobility checks |
| SEE Calculation | O(a) | O(a²) | a = attackers to square |
| Threat Array | O(n²) | O(n³) | Builds full attack map |
| Endgame Detection | O(1) | O(n) | Simple pattern matching |
| Win Rate Model | O(1) | O(1) | Pure calculation |

### Space Complexity:

| Module | Space Used | Cacheable |
|--------|-----------|-----------|
| PositionalEvaluation | O(1) | No (stateless) |
| MoveEvaluation | O(n) | Attacker lists |
| StockfishFeatures | O(n²) | Threat arrays |
| EndgameAnalysis | O(1) | No (stateless) |
| AdvancedAnalysis | O(m) | m = history entries |
| OptimizationHelpers | O(k) | k = cache entries |

---

## Configuration & Tuning

### Adjustable Thresholds:

**Singular Move Detection:**
```csharp
// Current: 1.5 pawn gap
const double SINGULAR_THRESHOLD = 1.5;
```

**High Mobility Thresholds:**
```csharp
Knight: 6+ moves
Bishop: 10+ moves
Rook: 12+ moves
Queen: 20+ moves
```

**Win Rate Scaling:**
```csharp
Endgame:  scale = 0.0015
Midgame:  scale = 0.0010
```

**Futility Margin:**
```csharp
Insignificant: |evalChange| < 0.1
Bad: evalChange < -0.5
```

---

## Extension Points

### Adding New Features:

1. **New Tactical Pattern:**
```csharp
// In MovesExplanation.cs DetectTacticalPattern()
if (IsMyNewPattern(board, ...))
    return "my new pattern description";
```

2. **New Positional Feature:**
```csharp
// In PositionalEvaluation.cs
public static string? DetectMyFeature(ChessBoard board, ...)
{
    // Analysis logic
    return "feature description";
}
```

3. **New Evaluation Model:**
```csharp
// In AdvancedAnalysis.cs
public static string GetMyEvaluation(double eval, ...)
{
    // Custom evaluation logic
    return "evaluation description";
}
```

### Integration Checklist:
- [ ] Add method to appropriate module
- [ ] Update MovesExplanation priority order
- [ ] Add unit tests
- [ ] Update this documentation
- [ ] Consider caching in OptimizationHelpers

---

## Testing Strategy

### Unit Test Coverage:

**Critical Paths:**
1. SEE calculation with complex exchanges
2. Singular move detection edge cases
3. Pawn structure detection accuracy
4. Win rate model boundary values
5. Threat array completeness

**Test Positions:**
```
1. Isolated pawn: FEN with pawn on d-file, no neighbors
2. Outpost knight: FEN with Nd5, no enemy pawns can attack
3. Singular move: Position with one clearly best move
4. Complex SEE: Queen vs Rook+Knight exchange
5. Zugzwang: Reciprocal zugzwang position
```

### Performance Benchmarks:

**Target Performance:**
- Position analysis: < 50ms per position
- Explanation generation: < 100ms total
- Memory footprint: < 10MB for typical game

---

## Future Improvements

### High Priority:
1. **Bitboard Integration**
   - Replace nested loops with bitboard operations
   - 10x faster attack detection
   - Pre-computed attack tables

2. **NNUE Integration**
   - Train custom neural network
   - Position feature extraction
   - Incremental update system

3. **Opening Book Integration**
   - ECO code classification
   - Named opening recognition
   - Opening statistics

### Medium Priority:
4. **Multi-threading**
   - Parallel analysis of multiple lines
   - Async feature detection
   - Background caching

5. **Machine Learning**
   - Learn from user feedback
   - Adaptive explanation complexity
   - Personalized explanation style

---

## References

### Inspiration Sources:

**Ethereal Chess Engine** by Andy Grant
- GitHub: https://github.com/AndyGrant/Ethereal
- Features used: Pawn structure, piece activity, king safety, SEE

**Stockfish Chess Engine** by Stockfish Team
- GitHub: https://github.com/official-stockfish/Stockfish
- Features used: Threat detection, singular moves, win rate model, WDL probabilities

**Leela Chess Zero (Lc0)** by Lc0 Team
- GitHub: https://github.com/LeelaChessZero/lc0
- Features used: WDL display, position sharpness, neural network evaluation concepts

**Chess.com**
- URL: https://www.chess.com/
- Features used: Move classification system (Brilliant !!, Blunder ??, Mistake ?, Inaccuracy ?!), win probability calculations

**Arena Chess GUI**
- URL: http://www.playwitharena.de/
- Features used: ABK opening book format support

**Chess Programming Wiki**
- URL: https://www.chessprogramming.org/
- Concepts: Bitboards, move generation, evaluation

---

## Version History

**v1.0.0** - Initial Release
- 30+ tactical patterns
- Basic explanations

**v1.1.0** - Ethereal Integration Phase 1
- Positional evaluation
- Pawn structure analysis
- Piece activity metrics

**v1.2.0** - Ethereal Integration Phase 2
- Static Exchange Evaluation (SEE)
- Move ordering
- Position tracking

**v1.3.0** - Ethereal Integration Phase 3
- Endgame pattern recognition
- Game phase detection
- Zugzwang awareness

**v1.4.0** - Stockfish Integration
- Threat array system
- Singular move detection
- Move quality classification

**v1.5.0** - Advanced Features
- Win rate model
- Opening history tracking
- Endgame heuristics

**v1.6.0** - Optimization & Polish
- **ChessUtilities:** Eliminated 410+ lines of duplicate code
- **BoardCache:** 4-10x performance improvement for tactical analysis
- **StockfishFeatures:** Fixed O(n^3) bottleneck to O(n^2) (~100x faster)
- **SEE Improvements:** Better trade vs win detection
- **Tactical Fixes:** Skewer, X-ray attack, exchange sacrifice validation
- **ExplanationFormatter:** Complexity levels, feature toggles, color-coding
- Comprehensive documentation

**v2.0.0** - Major UX Update & Refactoring
- **Architecture Overhaul:** MainForm reduced from 1,577 to 420 lines (73.4% reduction)
- **ThemeService (104 lines):** Centralized dark/light mode theme management
- **EnginePathResolver (100 lines):** Automatic engine discovery and validation
- **ConsoleOutputFormatter Enhanced:** DisplayAnalysisResults method
- **Color-Coded Move Quality:** 6 levels with thematic colors (!!, !, neutral, ?!, ?, ??)
- **Win Percentage Display:** Side-aware probability (White: 82% / Black: 18%)
- **Complexity Levels:** 4 levels (Beginner to Master)
- **Feature Toggles:** 11 customization options
- **Dark Mode:** Fully integrated theme system
- **Global Hotkeys:** Alt+X (analyze), Alt+K (toggle auto-monitor)
- **Service-Oriented Design:** Clean separation of concerns, better testability
- Build: 0 warnings, 0 errors

**v2.1.0** - Threat & Defense Detection
- **ThreatDetection.cs:** Before/after comparison for accurate threat detection
- **DefenseDetection.cs:** Detect defensive moves (protecting, blocking, escaping)
- **Pin/X-ray fixes:** Only report when material would actually be won
- **SEE settings:** Respects ShowSEEValues toggle
- **ConsoleOutputFormatter:** Integrated defense display with shield icon
- **Auto-Monitoring (BETA):** Automatic opponent move detection

**v2.2.0** - Lc0-Inspired Features
- **WDL Display:** Win/Draw/Loss probabilities from engine analysis
- **Opening Book (BETA):** 565+ opening positions with ECO codes
- **Move Quality Indicators:** Brilliant, Best, Good, Inaccuracy, Mistake, Blunder
- **Functional Aggressiveness:** Slider actually affects move selection (0=solid, 100=aggressive)
- **MoveSharpnessAnalyzer:** Evaluates move "sharpness" for style-based filtering
- **Config Hot-Reload:** Settings changes update all services immediately

**v2.5.0** - Chess.com-Style Classification
- **Brilliant Move Detection (!!):** Detects piece sacrifices that maintain good position
  - Validates sacrifice with SEE (must lose material)
  - Checks position wasn't already winning
  - Identifies Greek Gift sacrifices and tactical brilliancies
- **Move Classification System:** Chess.com-style ratings for alternative moves
  - Uses win probability calculations (logistic function)
  - Blunder (??) - 20%+ win probability drop
  - Mistake (?) - 10-20% win probability drop
  - Inaccuracy (?!) - 5-10% win probability drop
- **"Only Winning Move" Detection:** Highlights critical moves where alternatives lose advantage
- **ABK Opening Book Support:** Arena Chess GUI book format integration

**v2.5.1** - SEE, Explanations, and Redundancy Fixes (Current)
- **SEE Explanation Bug Fix:** Fixed incorrect values when pieces become vulnerable after move
  - Added `GetHangingPieceValueAfterMove()` to detect exposed pieces
  - Checks if cheapest attacker < piece value (vulnerable even if defended)
- **Better Brilliant Explanations:** Shows context like "sacrifices rook for decisive advantage"
- **False Sacrifice Fix:** No longer shows "rook sacrifice" when piece is defended after capture
- **Redundant Check Phrases Removed:** "gives check" handled by threats section only
- **"Creates threat on king" Removed:** Ambiguous phrase replaced by proper check detection
- **Color-Coded Moves Feature Removed:** Obsolete, replaced by Chess.com-style classification

---

## Contact & Contribution

**Project:** ChessDroid
**License:** MIT
**Repository:** https://github.com/jio1337/chessdroid

For questions, bug reports, or feature requests:
https://github.com/jio1337/chessdroid/issues

---

**Last Updated:** 2026-01-25
**Document Version:** 2.5.1 (Chess.com-Style Classification)
