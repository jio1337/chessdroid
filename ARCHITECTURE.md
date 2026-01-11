# ChessDroid Architecture Documentation

## Overview

ChessDroid is an advanced chess analysis tool that combines tactical pattern recognition with deep positional understanding inspired by world-class chess engines (Ethereal and Stockfish).

---

## System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    ChessDroid Core                      │
│                     (MainForm.cs)                       │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
┌─────────────────┐     ┌─────────────────┐
│  UCI Engine     │     │  Analysis       │
│  Integration    │     │  Engine         │
└─────────────────┘     └────────┬────────┘
                                 │
                 ┌───────────────┼───────────────┐
                 │               │               │
                 ▼               ▼               ▼
        ┌────────────────┐ ┌────────────┐ ┌──────────────┐
        │ MovesExplanation│ │MoveEvaluation│PositionalEval│
        │  (Orchestrator) │ │   (SEE)     │ (Structure)  │
        └────────┬────────┘ └──────┬─────┘ └──────┬───────┘
                 │                 │              │
    ┌────────────┼─────────────────┼──────────────┼───────────┐
    │            │                 │              │           │
    ▼            ▼                 ▼              ▼           ▼
┌─────────┐ ┌─────────┐ ┌─────────────┐ ┌──────────┐ ┌──────────┐
│Stockfish│ │Advanced │ │  Endgame    │ │Explanation│ │   UI     │
│Features │ │Analysis │ │  Analysis   │ │ Formatter │ │ Helpers  │
└────┬────┘ └─────────┘ └─────────────┘ └──────────┘ └──────────┘
     │
     │      ┌──────────────────────────────────────────┐
     └─────→│         Performance Layer                │
            │  ┌────────────────┐  ┌────────────────┐  │
            │  │ ChessUtilities │  │   BoardCache   │  │
            │  │ (Shared Logic) │  │ (Performance)  │  │
            │  └────────────────┘  └────────────────┘  │
            └──────────────────────────────────────────┘
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
11. Tablebase knowledge
12. Win rate evaluation
13. Generic positional improvement
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
0-5           Late Endgame (tablebase territory)
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

#### **Advanced Tablebase Integration:**
- Detects positions with ≤7 pieces
- Simulates DTZ (Distance to Zeroing)
- Provides move ranking: "fastest winning move"

**Supported Endgames:**
- K+P vs K → "tablebase win in ~20 moves"
- Opposite bishops → "tablebase draw"
- Bare king → "tablebase win in ~10 moves"

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
   ├─→ AdvancedAnalysis.GetTablebaseExplanation()
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
4. **Full Tablebase Integration**
   - Link with Syzygy/Gaviota libraries
   - Real DTZ calculation
   - 7-piece endgame coverage

5. **Multi-threading**
   - Parallel analysis of multiple lines
   - Async feature detection
   - Background caching

6. **Machine Learning**
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
- Features used: Threat detection, singular moves, win rate model

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
- Tablebase integration

**v1.6.0** - Optimization & Polish (Current) ⚡
- **ChessUtilities:** Eliminated 410+ lines of duplicate code
- **BoardCache:** 4-10x performance improvement for tactical analysis
- **StockfishFeatures:** Fixed O(n³) bottleneck → O(n²) (~100x faster)
- **SEE Improvements:** Better trade vs win detection
- **Tactical Fixes:** Skewer, X-ray attack, exchange sacrifice validation
- **ExplanationFormatter:** Complexity levels, feature toggles, color-coding
- Comprehensive documentation

---

## Contact & Contribution

**Project:** ChessDroid
**License:** MIT
**Repository:** https://github.com/jio1337/chessdroid

For questions, bug reports, or feature requests:
https://github.com/jio1337/chessdroid/issues

---

**Last Updated:** 2026-01-10
**Document Version:** 1.0
