# ChessDroid Architecture Documentation

## Overview

ChessDroid is a pure offline chess analysis application that combines tactical pattern recognition with deep positional understanding inspired by world-class chess engines (Ethereal and Stockfish). As of v3.1.0, the application is centered around the Analysis Board — an interactive workspace for deep chess analysis with visual engine arrows, PV line exploration, and free-draw annotation.

---

## System Architecture (v3.1.0)

```
┌─────────────────────────────────────────────────────────────────┐
│              ChessDroid Core (AnalysisBoardForm.cs)              │
│          Pure analysis board — main and only entry point         │
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

### v3.0.0 Changes from Previous Architecture
- **Removed:** MainForm.cs (old screen-detection entry point)
- **Removed:** ScreenCaptureService, BoardDetectionService, PieceRecognitionService, BoardMonitorService
- **Removed:** BoardVisualizer (debug visualization)
- **Removed:** EmguCV/OpenCvSharp4 dependencies (~50MB savings)
- **Entry point:** AnalysisBoardForm launched directly from Program.cs
- **Auto-analysis:** Always on — engine analyzes after every move automatically

---

## Module Descriptions

### 1. **AnalysisBoardForm.cs** (Main Form)
**Purpose:** The heart of chessdroid — a complete interactive workspace for deep chess analysis

**Key Responsibilities:**
- Interactive chess board with click-to-move
- Engine arrows on board (green/yellow/red per PV line, separate from user arrows)
- [See line] PV loading into move tree with animated playback
- Free-draw right-click arrows (any square to any square)
- Move tree navigation with variation branching
- Auto-analysis on every move, new game, and form open with result caching
- PGN import/export
- Move classification (Brilliant, Blunder, Mistake, Inaccuracy)
- Evaluation bar with smooth transitions
- Engine match management (engine vs engine)
- FEN loading and copying
- Settings access via ⚙ button

**Supporting Controls:**
- `ChessBoardControl` — Renders pieces, engine arrows, and user arrows using PNG templates with Unicode fallback
- `MoveTree.cs` / `MoveNode.cs` — Variation tree data structure
- `MoveQualityAnalyzer.cs` — Centipawn loss classification

---

### 2. **MovesExplanation.cs** (Orchestrator)
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
4. Tactical patterns (pins, forks, skewers, zwischenzug, etc.)
5. Sacrifices (exchange, piece, implicit)
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

### 3. **PositionalEvaluation.cs** (Ethereal Phase 1)
**Purpose:** Evaluate positional features inspired by Ethereal's evaluate.c

**Features:**

#### **Pawn Structure:**
- `DetectPassedPawn()` — No enemy pawns blocking promotion
- `DetectConnectedPawns()` — Mutually supporting pawns
- `DetectIsolatedPawn()` — No friendly pawns on adjacent files
- `DetectDoubledPawns()` — Multiple pawns on same file
- `DetectBackwardPawn()` — Stuck behind neighbors

#### **Piece Activity:**
- `DetectOutpost()` — Piece immune to pawn attacks
- `DetectHighMobility()` — Piece with many legal moves
- `DetectLongDiagonalControl()` — Bishop on powerful diagonal
- `DetectCentralControl()` — Controlling d4, e4, d5, e5

#### **King Safety:**
- `DetectKingShelter()` — Pawn shield strength
- `DetectWeakKingSquares()` — Vulnerable squares near king

---

### 4. **MoveEvaluation.cs** (Ethereal Phase 2)
**Purpose:** Advanced move evaluation with Static Exchange Evaluation (SEE)

**Key Features:**

#### **Static Exchange Evaluation (SEE):**
```csharp
StaticExchangeEvaluation(board, targetRow, targetCol, attackingPiece, isWhite)
```
- Simulates all captures on a square
- Returns final material balance
- Uses MVV-LVA (Most Valuable Victim — Least Valuable Attacker)
- Check-aware: filters out attackers that can't recapture while in check
- Pin-aware: filters out pinned pieces that can't legally capture

**Algorithm:**
```
1. Initial capture: +victimValue
2. Find all attackers to square (both sides)
3. Alternately capture with least valuable piece
4. Stop when one side refuses to recapture (would lose material)
5. Return final material balance
```

---

### 5. **EndgameAnalysis.cs** (Ethereal Phase 3)
**Purpose:** Endgame-specific knowledge and pattern recognition

**Game Phase Detection:**
```
Piece Count    Phase
0-5           Late Endgame (basic endgames)
6-7           Endgame
8-11          Transition
12+           Middlegame/Opening
```

**Advanced Endgame Heuristics (Stockfish/Ethereal-inspired):**
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

### 6. **StockfishFeatures.cs** (Stockfish High Priority)
**Purpose:** Stockfish-inspired search and evaluation features

**Key Features:**

#### **Threat Array Detection:**
```csharp
BuildThreatArray(board, forWhite)
```
- Maps all squares attacked by each side
- Groups attackers by target square
- Identifies low-value attackers on high-value pieces

#### **Singular Move Detection:**
- Compares best move eval vs second-best eval
- Gap >= 1.5 pawns = singular move ("only good move")

#### **Move Quality Classification:**
```
Eval Change    Quality
+1.0 or more   Excellent
+0.3 to +1.0   Good
-0.3 to +0.3   Marginal
-1.0 to -0.3   Bad
-1.0 or worse   Terrible
```

---

### 7. **AdvancedAnalysis.cs** (Medium Priority)
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
    0.0015  if materialCount <= 10 (endgame)
    0.0012  if materialCount <= 20 (transition)
    0.0010  if materialCount > 20 (opening/middlegame)
}
```

#### **Blunder Explanations:**
- Shows WHY moves are bad with detailed reasoning
- Explains what the blunder allows (material loss, tactical vulnerability, positional collapse)

---

### 8. **ThreatDetection.cs** (Threat Analysis)
**Purpose:** Detects chess threats for both sides, showing NEW threats created by moves

**Threat Types Detected:**
- HangingPiece — Undefended piece under attack
- MaterialWin — Attacking higher value piece
- Fork — Attacking multiple pieces
- Pin — Piece pinned to more valuable piece/king
- Skewer — Attacking through a piece
- DiscoveredAttack — Moving reveals attack
- CheckmateThreat — Threatening mate
- Check — Giving check
- Promotion — Pawn about to promote
- TrappedPiece — Piece with no escape

**Before/After Comparison:** Only shows NEW threats created by the move, preventing false positives.

---

### 9. **DefenseDetection.cs** (Defense Analysis)
**Purpose:** Detects defensive aspects of moves

**Defense Types Detected:**
- ProtectPiece — Newly defending a piece
- BlockAttack — Blocking an attack on valuable piece
- Escape — Moving attacked piece to safety
- ProtectKing — Improving king safety

---

### 10. **ChessUtilities.cs** (Core Performance Layer)
**Purpose:** Consolidated shared utility functions used across all analysis modules

**Key Features:**
- Piece Operations: `GetPieceValue()`, `GetPieceName()`
- Attack Detection: `CanAttackSquare()`, `IsSquareDefended()`, `CountAttackers()`
- Path Validation: `IsDiagonalClear()`, `IsLineClear()`, `IsPathClear()`
- Advanced Helpers: `IsPiecePinned()`, `GetAttackers()`

---

### 11. **BoardCache.cs** (Performance Optimization Layer)
**Purpose:** Eliminates repeated O(n^2) board scans by caching piece locations and attack maps

**Core Methods:**
```csharp
GetPieces(isWhite)                    // All pieces for a color
GetPiecesByType(type, isWhite)        // Specific piece type
IsSquareAttacked(row, col, byWhite)   // HashSet lookup (O(1))
GetKingPosition(isWhite)              // Cached position (O(1))
```

**Performance Impact:**
- 4-10x faster tactical analysis
- ~100x faster BuildThreatArray (O(n^3) -> O(n^2))
- ~2KB memory cost per position

---

### 12. **ThemeService.cs** (UI Theme Management)
**Purpose:** Centralized theme management for dark and light modes

**Architecture:**
```csharp
public class ThemeService
{
    private static readonly ColorScheme DarkScheme = new ColorScheme { ... };
    private static readonly ColorScheme LightScheme = new ColorScheme { ... };

    public static void ApplyTheme(Form form, ..., bool isDarkMode)
}
```

- Dark mode (RGB 45, 45, 48 base) and Light mode (WhiteSmoke base)
- 14+ color properties including Analysis Board-specific colors
- Applies to all UI controls consistently

---

### 13. **EnginePathResolver.cs** (Engine Discovery)
**Purpose:** Automatic discovery and resolution of chess engine paths

**Auto-Discovery Algorithm:**
```
1. Check if config has SelectedEngine
2. If not: scan Engines folder for .exe files
3. Return first found engine, save to config
4. Fallback to "stockfish.exe" if none found
```

---

### 14. **Opening & Book Services**
**Purpose:** Opening recognition and book move lookup

- **OpeningDatabase.cs** — Lazy-loaded singleton for ECO lookups (12,379+ positions)
- **PolyglotBookService.cs** — Multi-book loading from `.bin` files (1M+ positions)
- **PolyglotBookReader.cs** — Binary file parser for Polyglot format
- **PolyglotZobrist.cs** — Polyglot-compatible Zobrist hash computation

---

### 15. **MoveSharpnessAnalyzer.cs** (Play Style)
**Purpose:** Evaluates move "sharpness" for style-based filtering

**Sharpness scoring factors:**
- Captures and sacrifices (more aggressive)
- Pawn breaks and central pushes (more aggressive)
- Checks and forcing moves (more aggressive)
- Castling and quiet development (more solid)
- Defensive moves (more solid)

Used by EngineAnalysisStrategy to filter moves based on user's style preference (0=solid, 100=aggressive).

---

## Data Flow

### Typical Analysis Flow (v3.0.0):

```
1. User makes a move on the Analysis Board
   ↓
2. Auto-analysis triggers (no button press needed)
   ↓
3. Check analysis cache (ConcurrentDictionary)
   ├─→ Cache hit: Display cached results instantly
   └─→ Cache miss: Continue to step 4
   ↓
4. UCI Engine calculates best moves + evaluations
   ↓
5. MovesExplanation.GenerateMoveExplanation() called
   ↓
6. Parse move notation and create temp board
   ↓
7. Check features in priority order:
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
8. Combine top 2 reasons into explanation
   ↓
9. Display results with WDL, opening name, book moves, threats
   ↓
10. Cache results for instant replay
```

---

## Performance Characteristics

### Time Complexity by Module:

| Module | Average Case | Worst Case | Notes |
|--------|-------------|------------|-------|
| Tactical Detection | O(n) | O(n^2) | n = pieces on board |
| Pawn Structure | O(p) | O(p^2) | p = pawns on board |
| Piece Activity | O(n) | O(n^2) | With mobility checks |
| SEE Calculation | O(a) | O(a^2) | a = attackers to square |
| Threat Array | O(n^2) | O(n^3) | Builds full attack map |
| Endgame Detection | O(1) | O(n) | Simple pattern matching |
| Win Rate Model | O(1) | O(1) | Pure calculation |
| Analysis Cache | O(1) | O(1) | ConcurrentDictionary lookup |

### Space Complexity:

| Module | Space Used | Cacheable |
|--------|-----------|-----------|
| PositionalEvaluation | O(1) | No (stateless) |
| MoveEvaluation | O(n) | Attacker lists |
| StockfishFeatures | O(n^2) | Threat arrays |
| EndgameAnalysis | O(1) | No (stateless) |
| AdvancedAnalysis | O(m) | m = history entries |
| OptimizationHelpers | O(k) | k = cache entries |
| Analysis Cache | O(p) | p = analyzed positions |

---

## Folder Structure

```
chessdroid/
├── test/                          # Main project directory
│   ├── Program.cs                 # Entry point → AnalysisBoardForm
│   ├── AnalysisBoardForm.cs       # Main form (analysis board)
│   ├── AnalysisBoardForm.Designer.cs
│   ├── SettingsForm.cs            # Settings dialog
│   ├── HelpForm.cs                # Help dialog
│   ├── AppConfig.cs               # Configuration management
│   ├── Controls/
│   │   └── ChessBoardControl.cs   # Board rendering control
│   ├── Models/
│   │   ├── MoveTree.cs            # Variation tree
│   │   ├── MoveNode.cs            # Tree node
│   │   ├── BookMove.cs            # Opening book move
│   │   └── ...
│   └── Services/
│       ├── ChessEngineService.cs  # UCI engine communication
│       ├── ThemeService.cs        # Dark/light theme management
│       ├── EnginePathResolver.cs  # Engine discovery
│       ├── OpeningDatabase.cs     # ECO lookups
│       ├── PolyglotBookService.cs # Opening book lookups
│       └── ...
├── Templates/                     # Piece set images (gitignored)
│   ├── Chess.com/                 # 12 PNGs (wK, wQ, ..., bP)
│   └── Lichess/                   # 12 PNGs
├── Engines/                       # UCI engines (gitignored)
│   └── stockfish17.exe
├── Books/                         # Opening books
│   ├── codekiddy.bin              # Polyglot book (~1M positions)
│   └── ecoA-E.json               # ECO database
└── config.json                    # User settings (gitignored)
```

---

## Configuration & Tuning

### Adjustable Thresholds:

**Singular Move Detection:**
```csharp
const double SINGULAR_THRESHOLD = 1.5; // 1.5 pawn gap
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

---

## Extension Points

### Adding New Tactical Patterns:
```csharp
// In MovesExplanation.cs DetectTacticalPattern()
if (IsMyNewPattern(board, ...))
    return "my new pattern description";
```

### Adding New Positional Features:
```csharp
// In PositionalEvaluation.cs
public static string? DetectMyFeature(ChessBoard board, ...)
{
    // Analysis logic
    return "feature description";
}
```

### Adding New Piece Sets:
Create a folder in `Templates/` with 12 PNG files named: wK, wQ, wR, wB, wN, wP, bK, bQ, bR, bB, bN, bP. The folder will automatically appear in the piece set dropdown.

---

## Testing Strategy

### Target Performance:
- Position analysis: < 50ms per position
- Explanation generation: < 100ms total
- Analysis with cache hit: < 1ms
- Memory footprint: < 10MB for typical game

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
- Features used: WDL display, position sharpness, aggressiveness concepts

**Chess.com**
- URL: https://www.chess.com/
- Features used: Move classification system (Brilliant !!, Blunder ??, Mistake ?, Inaccuracy ?!)

---

## Version History

**v3.1.0** — Engine Arrows & Visual Analysis (Current)
- Engine arrows on board (green/yellow/red per PV line)
- [See line] PV loading into move tree with animated playback
- Free-draw right-click arrows, auto-analysis on form open
- Removed SEE display from UI, removed Puzzle Mode (~2,100 lines)
- Variation tree fix for [See line] at end of move tree
- Window title: `chessdroid v3.1.0`

**v3.0.0** — Pure Analysis Board
- Removed all screen detection, computer vision, and auto-monitoring
- AnalysisBoardForm is now the main and only entry point
- Removed Emgu.CV and OpenCvSharp4 dependencies (~50MB savings)
- Added zwischenzug detection, blunder explanations, improved brilliant move detection
- Auto-analysis always on, no manual trigger needed

**v2.9.0** — Engine Match Enhancements
- Start from current position in engine matches
- Brilliant move detection during matches
- Dynamic template selection (piece set switching)

**v2.8.0** — Analysis Board & Move Classification
- Full-featured Analysis Board with interactive chess board
- Move tree with variations (MoveTree.cs, MoveNode.cs)
- PGN import/export, move classification, evaluation bar
- Engine matches with configurable time controls
- Analysis caching with ConcurrentDictionary

**v2.7.0** — Play Style Recommendations
- Style-based move suggestions (Very Solid → Very Aggressive)
- MoveSharpnessAnalyzer for style filtering

**v2.6.0** — Opening Books & ECO Database
- Polyglot .bin format with 1M+ positions
- 12,379+ ECO opening positions

**v2.5.0** — Chess.com-Style Classification
- Brilliant move detection (!!)
- Win probability-based move classification

**v2.0.0** — Major UX Update & Refactoring
- Architecture overhaul (73.4% MainForm reduction)
- ThemeService, EnginePathResolver, dark/light mode
- Feature toggles and complexity levels

**v1.0.0** — Initial Release
- 30+ tactical patterns, basic explanations

---

## Contact & Contribution

**Project:** ChessDroid
**License:** MIT
**Repository:** https://github.com/jio1337/chessdroid

For questions, bug reports, or feature requests:
https://github.com/jio1337/chessdroid/issues

---

**Last Updated:** 2026-02-09
**Document Version:** 3.1.0 (Engine Arrows & Visual Analysis)
