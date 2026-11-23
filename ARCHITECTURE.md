# chessdroid Architecture Documentation

## Overview

chessdroid is a pure offline chess analysis application that combines tactical pattern recognition with deep positional understanding inspired by world-class chess engines (Ethereal and Stockfish). As of v3.21.1, the application is centered around the Analysis Board — an interactive workspace for deep chess analysis with visual engine arrows, threat arrows, eval graph, PV line exploration, piece animations, free-draw annotation, bot mode (Elo targeting 1320–3190, Friendly/Challenge, draw detection), Chess 960 (Fischer Random with position browser and legal castling), continuous analysis, annotated PGN round-trips, square highlighting, auto-play, game accuracy scoring, interactive game review, game library, material strips, SVG piece sets, smooth eval bar animation, neutral engine match arbiter, engine match series with adjudication and opening book, Tournament mode (simultaneous 2×2 board grid), full board customization, 8 UI themes, 22 piece sets, resizable panels, a persistent chessdroid engine rating ladder, sound effects, Endgame & Tactics Drills (10 PGN files), and a full Training suite (Puzzle Training with 5.94M Lichess puzzles, Puzzle Rush, Puzzle Gauntlet, Daily Puzzle, Board Vision, Opening Training, and Square Training).

---

## Solution Structure (v3.21.1)

The codebase is split into two projects plus their test counterparts, the result of the
Phase 0–5 refactor tracked in `CHESSDROID_AUDIT.md`:

```
chessdroid/
├── src/
│   ├── ChessDroid.Core/     # Pure chess logic — no WinForms, no UI dependency
│   └── ChessDroid.App/      # WinForms UI, engine process management, all forms/dialogs
└── tests/
    ├── ChessDroid.Tests/        # Core-only xUnit tests (no WinForms reference)
    ├── ChessDroid.Tests.App/    # References the real chessdroid.csproj; UI-adjacent logic
    └── FakeUciEngine/           # Scripted stdin/stdout UCI stub used by App tests
```

**The split's rule of thumb:** if a type can be exercised with nothing but a FEN string
and no `System.Windows.Forms` reference, it belongs in `ChessDroid.Core`. Anything that
touches a `Form`, `Control`, engine process I/O, file paths, or app configuration belongs
in `ChessDroid.App`. `ChessDroid.App` references `ChessDroid.Core`; `ChessDroid.Core` has
no reference back.

### ChessDroid.Core — pure chess logic

```
Core/
├── Models/
│   ├── ChessBoard.cs, CastlingState.cs, GameState.cs, PieceType.cs
│   ├── MoveNode.cs                  # Variation tree node (tree structure itself lives in App)
│   ├── Score.cs                     # readonly record struct Score(int? Cp, int? Mate)
│   ├── BookMove.cs, BotSettings.cs, EngineMatchConfig.cs, TournamentConfig.cs, SavedGame.cs
└── Services/
    ├── MoveGenerator.cs             # THE single consolidated legal-move-generation and
    │                                # check-detection implementation (standard + Chess960).
    │                                # Perft-verified; everything else that needs
    │                                # "is this move legal" / "is this king in check"
    │                                # delegates here rather than re-deriving it.
    ├── ChessRulesService.cs         # Move application (ApplyUciMove), game-end detection
    ├── ChessNotationService.cs      # UCI↔SAN conversion
    ├── ChessUtilities.cs            # Piece values (GetPieceValue, exchange-oriented, King=100;
    │                                # GetMaterialValue, counting-oriented, King=0 — intentionally
    │                                # NOT merged, see doc comments), attack/path helpers
    ├── Chess960Service.cs           # Fischer Random start-position generation/validation
    ├── BoardCache.cs, BoardPool.cs  # Performance layer: cached piece/attack maps, pooled boards
    ├── EvaluationParser.cs          # UCI info-line → Score parsing
    ├── EloCalculator.cs             # Rating-ladder math
    └── PolyglotBookReader/Service/Zobrist.cs   # Opening-book (.bin) binary format
```

### ChessDroid.App — WinForms UI and orchestration

```
App/
├── Program.cs                      # Entry point → AnalysisBoardForm
├── AnalysisBoardForm.cs + 7 partials   # Main form, split by feature area (see below)
├── AnalysisBoardForm.Designer.cs
├── AppConfig.cs, AppMode.cs, TrainingSubMode.cs
├── SettingsForm.cs / .Designer.cs, HelpForm.cs, StatsDialog.cs
├── BotSettingsDialog.cs, Chess960BrowserDialog.cs, DrillWatchDialog.cs
├── EngineProfilesDialog.cs, GameLibraryDialog.cs, OpeningExplorerDialog.cs + .Designer.cs
├── PgnGamePickerDialog.cs, PositionEditorForm.cs + .Designer.cs, TournamentForm.cs
├── Controls/
│   ├── ChessBoardControl.cs        # Board rendering, arrows, piece animation, click/drag input
│   ├── EvalBarControl.cs, EvalGraphControl.cs
│   ├── MatchBoardPanel.cs          # One board tile within Tournament mode's 2×2 grid
│   └── MaterialStripControl.cs
├── Models/
│   ├── CachedAnalysis.cs, GameReviewResult.cs
├── Modes/
│   └── IModeController.cs          # Narrow lifecycle interface (Exit()) — see note below
└── Services/
    ├── ChessEngineService.cs       # UCI engine process I/O (stdin/stdout), SetSkillLevelAsync
    ├── EngineMatchService.cs       # Engine-vs-engine match running (Tournament mode, match series)
    ├── MovesExplanation.cs         # Orchestrator: combines all analysis modules into
    │                                # human-readable explanations (see Data Flow below)
    ├── PositionalEvaluation.cs, MoveEvaluation.cs (SEE), EndgameAnalysis.cs,
    │   StockfishFeatures.cs, AdvancedAnalysis.cs, ThreatDetection.cs, DefenseDetection.cs,
    │   MoveSharpnessAnalyzer.cs, WDLAnalysis.cs      # Analysis modules, described individually below
    ├── ConsoleOutputFormatter.cs, ExplanationFormatter.cs   # Display-text assembly
    ├── MoveQualityAnalyzer.cs, MoveClassificationService.cs
    ├── ThemeService.cs, EnginePathResolver.cs, CrashLogger.cs, AudioService.cs
    ├── OpeningDatabase.cs, OpeningBook.cs, EcoBookService.cs, EcoFileCache.cs
    ├── PgnImportService.cs, GameLibraryService.cs, EndgameDrillService.cs
    └── LichessPuzzleService.cs
```

**Why analysis modules (`MovesExplanation`, `PositionalEvaluation`, `ThreatDetection`,
etc.) live in `App` and not `Core`:** they're pure functions of a board position in
principle, but in practice they're wired directly to `ChessEngineService` UCI output
parsing and `ConsoleOutputFormatter` display strings, and nothing outside the UI currently
needs them independent of that. They weren't pulled into `Core` because nothing in this
refactor needed them to be — not because they depend on WinForms controls.

**`IModeController` (Modes/IModeController.cs):** deliberately scoped narrow —
`void Exit()` only. `AnalysisBoardForm.Bot.cs`, `.Match.cs`, and `.Training.cs` each
define a private nested `XModeController : IModeController` and set a single
`CurrentModeController` field on mode entry/exit. This replaces three separate ad-hoc
"am I in bot/match/training mode" boolean checks scattered through
`AnalysisBoardForm.cs` with one polymorphic exit call, but stops short of a full
`GameSession`/mode-state extraction — that remains future work requiring live GUI
testing to verify safely (see "Known Gaps" below).

### AnalysisBoardForm partial classes

`AnalysisBoardForm` is one `partial class` split by feature area, not by layer — each
partial owns both its UI event handlers and the orchestration logic specific to that
feature:

| File | Owns |
|---|---|
| `AnalysisBoardForm.cs` | Core board interaction, move tree, PGN/FEN load, settings wiring |
| `AnalysisBoardForm.Analysis.cs` | Auto-analysis triggering, debounce/cancellation, caching |
| `AnalysisBoardForm.Bot.cs` | Bot mode (play vs. engine), `BotModeController` |
| `AnalysisBoardForm.Match.cs` | Engine-vs-engine match mode, `MatchModeController` |
| `AnalysisBoardForm.Training.cs` | Training suite (puzzles, drills, opening/square/board-vision training), `TrainingModeController` |
| `AnalysisBoardForm.Classification.cs` | Move quality classification (Brilliant/Blunder/etc.), game accuracy scoring |
| `AnalysisBoardForm.PgnImportExport.cs` | Annotated PGN import/export |
| `AnalysisBoardForm.Helpers.cs` | Shared small utility methods used across the other partials |
| `AnalysisBoardForm.Designer.cs` | WinForms designer-generated layout |

### Test projects

- **`ChessDroid.Tests`** — xUnit, references only `ChessDroid.Core`. Covers
  `MoveGenerator` (including perft tests for standard and Chess960 positions),
  `ChessUtilities`, `Score`, `IsKingInCheck`/`IsKingMated` consistency.
- **`ChessDroid.Tests.App`** — xUnit, references the real `chessdroid.csproj` so it can
  exercise UI-adjacent logic (PGN round-trips through `PgnImportService`, the
  `MovesExplanation` golden/snapshot corpus, `EngineMatchService` concurrency, perft via
  the App's board types) without a WinForms message loop.
- **`FakeUciEngine`** — a minimal console app that speaks just enough UCI protocol to
  stand in for a real engine binary in `EngineMatchService` tests.

---

## Data Flow

### Typical Analysis Flow:

```
1. User makes a move on the Analysis Board
   ↓
2. Auto-analysis triggers (no button press needed) — unless suppressed by an active
   mode (bot turn in progress, training panel open, auto-play running; see the guard
   in AnalysisBoardForm.Analysis.cs's TriggerAutoAnalysis)
   ↓
3. Check analysis cache (ConcurrentDictionary)
   ├─→ Cache hit: Display cached results instantly
   └─→ Cache miss: Continue to step 4
   ↓
4. ChessEngineService sends "go" to the UCI engine process, reads "info"/"bestmove" lines
   ↓
5. EvaluationParser (Core) turns the info line's score into a Score struct
   ↓
6. MovesExplanation.GenerateMoveExplanation() called
   ↓
7. Parse move notation (ChessNotationService, Core) and create temp board
   (ChessRulesService.ApplyUciMove, Core)
   ↓
8. Check features in priority order:
   │
   ├─→ StockfishFeatures.IsSingularMove() / DetectThreatCreation()
   ├─→ DetectTacticalPattern() [30+ patterns]
   ├─→ MoveEvaluation.DetectWinningCapture() [SEE]
   ├─→ PositionalEvaluation.DetectPassedPawn() / DetectOutpost()
   ├─→ EndgameAnalysis.DetectKPvK()
   ├─→ AdvancedAnalysis.GetWinningChanceDescription()
   └─→ Generic fallback
   ↓
9. Combine top 2 reasons into explanation via ConsoleOutputFormatter
   ↓
10. Display results with WDL, opening name, book moves, threats
   ↓
11. Cache results for instant replay
```

**Explanation priority order** (`MovesExplanation`), unchanged since v3.0.0:
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

---

## Module Reference

### MoveGenerator.cs (Core) — the single source of truth for legality

Consolidates what used to be independently-reimplemented legal-move-generation and
check-detection logic scattered across `ChessRulesService`, `EngineMatchService`,
`DefenseDetection`, and `ConsoleOutputFormatter`. Handles standard castling and Chess960
castling (including the "king captures own rook" UCI notation) through the same legality
path. Verified against published perft reference node counts for both variants
(`MoveGeneratorPerftTests.cs`, `PerftTests.cs`). Everywhere else that needs
"is this move legal" or "is this king in check" delegates here rather than re-deriving
it — `IsKingInCheck` in `ChessRulesService`, `EngineMatchService`, and
`ConsoleOutputFormatter`/`ChessUtilities` is now a thin redirect.

### MovesExplanation.cs (App, Orchestrator)
Combines all analysis modules into a single human-readable explanation string; see Data
Flow above for the priority order. O(n) where n = number of pieces on board. Covered by
a golden/snapshot test corpus (`MovesExplanationGoldenTests.cs`) rather than a
correctness oracle — it locks today's behavior against unintended regressions without
claiming today's wording is independently verified as "right."

### PositionalEvaluation.cs (App, Ethereal Phase 1)
Pawn structure (passed/connected/isolated/doubled/backward pawns), piece activity
(outposts, mobility, long diagonals, central control), king safety (shelter, weak
squares) — all inspired by Ethereal's `evaluate.c`.

### MoveEvaluation.cs (App, Ethereal Phase 2) — Static Exchange Evaluation
```csharp
StaticExchangeEvaluation(board, targetRow, targetCol, attackingPiece, isWhite)
```
Simulates all captures on a square using MVV-LVA ordering; check-aware (filters
attackers that can't recapture while in check) and pin-aware (filters pinned pieces that
can't legally capture). Returns the final material balance.

### EndgameAnalysis.cs (App, Ethereal Phase 3)
Game-phase detection by piece count (0–5 late endgame, 6–7 endgame, 8–11 transition, 12+
middlegame/opening), rule of the square, king centralization, opposition, wrong-color
bishop draws, FIDE insufficient-material rules, mop-up evaluation, passed-pawn/king
tropism bonuses, fortress detection.

### StockfishFeatures.cs (App, Stockfish-inspired)
Threat-array construction (`BuildThreatArray`, O(n²) via `BoardCache`), singular-move
detection (best vs. second-best eval gap ≥ 1.5 pawns), move-quality classification
thresholds (Excellent/Good/Marginal/Bad/Terrible).

### AdvancedAnalysis.cs (App)
Stockfish WDL win-rate model:
```
WinRate = 50 + 50 * (2 / (1 + exp(-eval * scale)) - 1)
Scale = 0.0015 (≤10 pieces) | 0.0012 (≤20) | 0.0010 (>20)
```
Plus detailed blunder explanations (what the blunder allows).

### ThreatDetection.cs / DefenseDetection.cs (App)
Before/after threat comparison — only reports threats *newly created* by a move, to
avoid false positives. Threat types: HangingPiece, MaterialWin, Fork, Pin, Skewer,
DiscoveredAttack, CheckmateThreat, Check, Promotion, TrappedPiece. Defense types:
ProtectPiece, BlockAttack, Escape, ProtectKing.

### ChessUtilities.cs (Core)
Shared helpers used across analysis modules: `GetPieceValue` (exchange-oriented, King =
100, used by SEE/MVV-LVA where a king can never be a "free" capture) vs.
`GetMaterialValue` (counting-oriented, King = 0, used wherever material totals must
exclude kings) — a deliberate, non-mergeable divergence, locked by
`ChessUtilitiesTests.cs`. Also: attack detection, path validation, pin detection,
`IsKingInCheck`/`IsKingMated` (delegating to `MoveGenerator`).

### BoardCache.cs (Core, Performance)
Eliminates repeated O(n²) board scans via cached piece locations and attack maps.
`IsSquareAttacked`/`GetKingPosition` are O(1) lookups; ~4–10x faster tactical analysis,
~100x faster `BuildThreatArray` (O(n³) → O(n²)), ~2KB memory cost per position.

### ThemeService.cs / EnginePathResolver.cs (App)
Theme management (8 themes, dark/light base schemes applied consistently across
controls) and UCI engine binary auto-discovery (config → `Engines/` folder scan →
`stockfish.exe` fallback).

### Opening & Book Services
- `OpeningDatabase.cs` (App) — lazy-loaded ECO lookup singleton (12,379+ positions)
- `PolyglotBookService.cs` / `PolyglotBookReader.cs` / `PolyglotZobrist.cs` (Core) —
  Polyglot `.bin` opening-book loading and Zobrist hashing
- `EcoBookService.cs` / `EcoFileCache.cs` (App) — ECO file loading/caching

### MoveSharpnessAnalyzer.cs (App)
Scores move "sharpness" (captures/sacrifices/pawn breaks/checks = aggressive; castling/
quiet development/defense = solid) for style-based move filtering (0 = solid, 100 =
aggressive), used by the engine analysis strategy.

### Score.cs (Core)
`readonly record struct Score(int? Cp, int? Mate)` — typed replacement for hand-
formatted eval strings at the point they're produced (`ChessEngineService`'s three eval-
string-formatting call sites). The 5 downstream parsers of eval strings were **not**
unified onto `Score`, because they have genuinely incompatible numeric semantics (flat
±100 scoring, distance-scaled ±10–15, sort-dominant ±100000, exact-preserved) — see
`ScoreTests.cs` for the locked string format.

---

## Known Gaps (tracked against CHESSDROID_AUDIT.md)

These remain open, deliberately deferred rather than attempted blind:

- **`GameSession` extraction / full `TrainingPanel` extraction / full `IModeController`
  rewrite** — `AnalysisBoardForm` still owns a large amount of per-mode state directly
  rather than through an extracted session/panel abstraction. The current
  `IModeController` is intentionally narrow (see above). A full extraction touches
  enough UI wiring and timing-sensitive behavior (training panel visibility, bot-turn
  gating, match-board refresh) that it needs live GUI verification, not just unit tests,
  to do safely.
- **Analyzer/`.editorconfig` cleanup** — enabling `AnalysisLevel=latest-recommended`
  surfaces 1,040 warnings (864 in `src/`) on this pre-existing codebase. Investigated,
  not attempted; a real future task, explicitly deferred by user choice rather than
  silently skipped.

---

## Folder Structure (build artifacts, assets)

```
chessdroid/
├── src/
│   ├── ChessDroid.Core/chessdroid.core.csproj
│   └── ChessDroid.App/
│       ├── chessdroid.csproj
│       ├── Templates/                 # Piece set images (gitignored)
│       │   ├── Chess.com/             # 12 PNGs (wK, wQ, ..., bP)
│       │   └── Lichess/               # 12 PNGs
│       ├── Engines/                   # UCI engines (gitignored)
│       │   └── stockfish17.exe
│       ├── Books/                     # Opening books
│       │   ├── codekiddy.bin          # Polyglot book (~1M positions)
│       │   └── ecoA-E.json            # ECO database
│       └── config.json                # User settings (gitignored)
├── tests/
│   ├── ChessDroid.Tests/
│   ├── ChessDroid.Tests.App/
│   └── FakeUciEngine/
└── Directory.Build.props              # <Version>3.21.1</Version>, shared across all projects
```

---

## Configuration & Tuning

### AppConfig.cs — representative properties:
```csharp
public string LightSquareColor { get; set; } = "#F0D9B5"; // hex, persisted to config.json
public string DarkSquareColor  { get; set; } = "#B58863"; // hex, persisted to config.json
```

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

### Adding a new tactical pattern:
Add detection logic in `MovesExplanation.cs`'s `DetectTacticalPattern()` (App), returning
a description string. If the underlying check is a pure board-position query reusable
outside the UI, consider whether it belongs in `ChessDroid.Core` instead.

### Adding a new positional feature:
```csharp
// In PositionalEvaluation.cs
public static string? DetectMyFeature(ChessBoard board, ...)
{
    return "feature description";
}
```

### Adding a new piece set:
Create a folder in `Templates/` with 12 PNG files named: wK, wQ, wR, wB, wN, wP, bK, bQ,
bR, bB, bN, bP. The folder will automatically appear in the piece set dropdown.

### Adding a Core vs. App decision:
If the new code needs nothing but a `ChessBoard`/FEN and produces a value, string, or
bool — `ChessDroid.Core`, and add coverage in `ChessDroid.Tests`. If it touches
`ChessEngineService`, a `Form`/`Control`, file I/O, or `AppConfig` — `ChessDroid.App`,
and add coverage in `ChessDroid.Tests.App`.

---

## Testing Strategy

- **Core logic** (`ChessDroid.Tests`): correctness-critical paths (move generation,
  check detection, notation conversion) are perft-verified or cross-checked against an
  independently-constructed ground-truth corpus, not just asserted against the
  implementation's own prior output.
- **App logic** (`ChessDroid.Tests.App`): PGN round-trips through real `ChessBoard`
  positions, `EngineMatchService` concurrency (via `FakeUciEngine`), and a golden/
  snapshot corpus for `MovesExplanation` output.
- **Target performance:** position analysis < 50ms; explanation generation < 100ms
  total; analysis with cache hit < 1ms.
- 158 tests total (92 Core + 66 App) as of this writing, all green in Debug and Release.

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

**v3.21.1** — see CHANGELOG.md for the full, up-to-date version history from here; this
section was last kept current at v3.18.0 and isn't being hand-maintained in parallel with
CHANGELOG.md going forward.

**v3.18.0** — The Arena
- Tournament Mode — simultaneous engine matches on a 2×2 live board grid; round-robin and manual pairing; live standings; full PGN auto-saved
- Engine Match series — N games per match with color swap, auto-adjudication, opening book (Random/Choose), PGN auto-save with eval annotations
- Endgame & Tactics Drills — 10 PGN files (5 tactics + 4 endgame series + rook database); all chapters with researched descriptions; Practice vs Bot; Watch Engines
- Bot mode Elo targeting — direct Elo input 1320–3190 with presets; UCI_LimitStrength support; draw detection (threefold/insufficient material/50-move)
- Analysis output overhaul — clickable eval, 10-ply truncation with expandable "...", no blank lines
- Precise (!) quality tier — only winning/saving move; round-trips through PGN
- Daily Puzzle, Opening filter, Rating filter for Puzzle Training
- Multi-game PGN picker dialog
- Drills folder expanded: `Endgames/` renamed `Drills/`, 9 new PGN files added, filenames prefixed for load order

**v3.16.0** — Training Grounds
- Full Training suite: Puzzle Training (5.94M Lichess puzzles), Puzzle Rush, Puzzle Gauntlet, Board Vision (Training/Timed/Survival), Opening Training, Square Training
- Persistent streaks, accuracy %, and personal bests across all training modes; PBs shown in settings panels before starting; per-section reset links
- Monochromatic board theme; training mode icons; board locked on settings screen

**v3.9.0** — Eval Graph, Animations & QoL
- Eval graph: `EvalGraphControl` (65px right panel), shows score history across all moves; click-to-navigate; `ShowEvalGraph` toggle
- Piece animations: 150ms GDI+ lerp timer in `ChessBoardControl`; castling animates rook; speed configurable 50–500ms; `ShowAnimations` toggle
- Bot difficulty: `BotDifficulty` enum removed; `BotSettings.SkillLevel` (int 1–20) is now primary; `BotSettingsDialog` uses TrackBar 1–20
- `ShowBookArrows` independent of `ShowBookMoves` — each toggleable separately
- Classification cancellation: `CancellationTokenSource _classifyCts` cleanly cancels mid-classification on New Game / Load FEN / Load PGN / bot start
- Engine depth: NumericUpDown 1–40 replaces ComboBox 1–20 (Stockfish has no hard depth cap)
- `ExplanationComplexity` system removed entirely: `ComplexityLevel` enum, `AdjustForComplexity`, `SimplifyForBeginner/Intermediate` all gone
- `HandleBotGameEnd` now calls `RestoreChallengeSnapshot` — settings correctly restored after natural game end
- Performance: `_labelFont` / `_badgeFont` / `_badgeSf` cached in `ChessBoardControl` (were recreated every 60fps paint)
- Window title: `chessdroid v3.9.0`

**v3.8.1** — Bug Fixes & Light Theme Polish
- [See line] board corruption fix: `SanitizeFenForEngine()` normalizes unknown FEN piece chars before every `position fen` command; `ApplyUciMove` guard returns early on empty source square
- Light theme readability: `GetSharpnessColor` now theme-aware (Firebrick/SaddleBrown/DimGray/SteelBlue in light mode instead of Orange/Gold/LightBlue); separator and Recommended colors darkened
- Window title: `chessdroid v3.8.1`

**v3.8.0** — Threat Arrows
- Threat arrows (red) on board: show opponent threats against your pieces; derived from same detection as "⚠ Opponent threats:" text — always in sync; `ShowThreatArrows` setting
- Book moves immediate display: opening name + book moves shown before engine starts; `ShowBookInfoImmediate(fen)` → `ConsoleOutputFormatter.ShowBookContextNow`
- Color preset combobox: pre-selects matching preset on SettingsForm open via `Array.FindIndex`
- Board right-aligned in leftPanel: `groupX = panel.Width - groupWidth`; empty space left-side
- Minimum window size = actual rendered size set in `Shown` event, DPI-safe
- Opponent threats section added to `DisplayLiveLines` (continuous analysis live updates)
- Desperado false positive fix: `HasSafeRetreat` added — desperado requires no safe retreat AND truly in danger
- Overload false positive fix: exploit board simulation verifies enemy can reach second defended target after exchange
- Pinned attacker false positive fix: `HasLegalAttacker` simulates capture + checks attacker's king exposure
- Window title: `chessdroid v3.8.0`

**v3.7.0** — Checkmate Detection & Auto-Play
- Full mate-in-1 scan across all pieces: detects battery setups, quiet moves that threaten forced mate; displays "threatens checkmate on g2"
- Opponent mate threat display: `AnalyzeOpponentThreats` runs same scan; "stops checkmate threat" defense label on blocking moves
- `IsCheckmate` king-capture bug fixed: king no longer counted as non-king capturer
- `GetPieceMoves` pawn case added: pawn pushes, diagonal captures, and queen-promotion simulation
- Auto-play button (`▶▶`/`⏸`): steps through move list at configurable speed; analysis suppressed during playback
- Auto-play speed setting in Settings (200–2000ms, default 600ms)
- Book arrows cleared on auto-play start and engine match start
- Endgame analysis noise removed: king centralization, obvious passed pawn remarks, vague king activity, distant opposition in non-K+P endgames, zugzwang potential
- Window title: `chessdroid v3.7.0`

**v3.6.0** — Explanation Accuracy
- Opening book arrows on board: book moves shown as arrows, replacing engine arrows during opening
- 25+ move explanation false positive fixes: checkmate threat, pin, double attack, blocking, desperado, pawn escape, rook open file, x-ray, discovered attack, passed pawn, pawn tempo, positional labels on recaptured pieces
- 13 new explanation patterns: centralization, outpost, rook battery, bishop pair, king safety, and more
- Undefended pawn attack detection: "attacks pawn on X" label when move newly attacks an undefended pawn
- FEN load now triggers automatic analysis
- Window title: `chessdroid v3.6.0`

**v3.5.0** — Annotated PGN & Polish
- Annotated PGN export/import: NAGs, eval comments, engine cache data embedded per move; import restores classification and analysis instantly
- Right-click square highlighting: tap toggles orange highlight; drag draws arrows; left-click clears all
- Challenge/Friendly bot mode selector added to `BotSettingsDialog`
- WDL labels perspective-corrected: "completely winning/losing", "clear disadvantage" etc. reflect the side to move
- `MoveListBox_DrawItem` catch path: classification quality colors (Best/Excellent/Good) now restored via `GetQualityColor()`
- `AnimatePvLineAsync`: `boardControl.LastMove` set per move + `Refresh()` for immediate repaint; `TriggerAutoAnalysis()` fires on completion
- Window title: `chessdroid v3.5.0`

**v3.4.0** — Analysis Performance
- `EndgameAnalysis` int-based overloads: `IsEndgame(int)`, `IsLateEndgame(int)`, `IsMiddlegame(int)`, `GetGamePhase(int)` — callers compute `CountTotalPieces` once and reuse
- `FilterPinnedAttackers` in SEE: single combined board pass finds both king positions; `IsPiecePinnedToKing` accepts precomputed coords, eliminating N×64 redundant scans per filter call
- `ChessEngineService`: `_spaceSep` static readonly replaces `new[] { ' ' }` allocation per engine line
- Window title: `chessdroid v3.4.0`

**v3.3.0** — Continuous Analysis & Stability
- Continuous analysis mode: live depth-by-depth PV streaming via `go depth N`; compact display during analysis, full annotated result at max depth; `[See line]` suppressed during live updates
- Continuous analysis max depth setting (default 50, range 10–100); `ContinuousAnalysisMaxDepth` in `AppConfig`
- Custom position editor: arbitrary board setup for analysis
- Font settings: configurable font family and size for analysis panel and move list; deferred font disposal via `BeginInvoke` + `IsHandleCreated` guard
- Engine freeze fix: `ReadLineAsync(token)` throughout `ChessEngineService`; stop+drain in `GetBestMoveAsync` cancellation path
- MoveListBox crash fix: font disposal race eliminated; fallback rendering uses `moveListBox.Font` (current valid font)
- Brilliant move detection: pinned attackers excluded from attacker list
- Window title: `chessdroid v3.3.0`

**v3.2.1** — Bug Fixes
- Bot mode: `TriggerAutoAnalysis` skipped in `BoardControl_MoveMade` when `_botModeActive`; only fires after bot responds
- `UpdateMoveList()` wrapped with `isNavigating = true` to suppress spurious `MoveListBox_SelectedIndexChanged` analysis
- `isBestMove` guard moved before centipawn thresholds in `MoveQualityAnalyzer`
- Cancellation token plumbed through debounce; stale-position check added after `GetBestMoveAsync`
- Window title: `chessdroid v3.2.1`

**v3.2.0** — Bot Mode & Board Customization
- Bot mode: play vs Stockfish with 5 difficulty presets; separate engine instance; board interaction disabled during bot turn
- Move quality badge: colored overlay on destination square during navigation (`ChessBoardControl.SetMoveAnnotation`)
- Custom board colors: `ColorDialog` in SettingsForm; `LightSquareColor`/`DarkSquareColor` in `AppConfig`; `ChessBoardControl.SetSquareColors()`
- New files: `Models/BotSettings.cs`, `BotSettingsDialog.cs`
- Window title: `chessdroid v3.2.0`

**v3.1.0** — Engine Arrows & Visual Analysis
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

**Project:** chessdroid
**License:** MIT
**Repository:** https://github.com/jio1337/chessdroid

For questions, bug reports, or feature requests:
https://github.com/jio1337/chessdroid/issues

---

**Last Updated:** 2026-11-23 — full rewrite reflecting the Core/App split
**Document Version:** 3.21.1
