## chessdroid v3.18.0 — User Guide

### Welcome to chessdroid!

chessdroid is an offline chess analysis application built for focused study. Analyze positions, explore ideas, and learn with your favorite UCI engines — with automatic analysis on every move, no buttons to press.

---

## Quick Start

1. **Launch chessdroid** — Run `chessdroid.exe`
2. **Configure your engine** — Click ⚙ Settings, select your UCI engine from the Engines folder
3. **Make moves on the board** — Click pieces to move them, analysis runs automatically
4. **Read the analysis** — See move classifications, explanations, WDL, and opening names
5. **Import a game** — Use Import PGN to analyze a complete game move by move

---

## Key Features

### **1. Analysis Board**

The Analysis Board is the heart of chessdroid — a complete interactive workspace for deep chess analysis.

**Capabilities:**
- **Interactive Board** — Click to make moves with legal move validation
- **Engine Arrows** — Colored arrows on the board showing engine recommended moves (green=best, yellow=2nd, red=3rd)
- **[See line]** — Click to load any PV line into the move tree as a variation with animated playback
- **Free-Draw Arrows** — Right-click and drag to draw arrows from any square to any square for your own analysis
- **Move Tree** — All moves tracked in a navigable list with variation branching
- **Auto-Analysis** — Engine analysis runs automatically on every move, new game, and form open, with caching for instant results
- **FEN Support** — Load any position via FEN string or start from the standard position
- **Flip Board** — View from White or Black's perspective
- **PGN Import/Export** — Import games from any source, export your analysis to standard PGN format
- **Move Classification** — Review games with colored quality symbols — Brilliant (!!), Blunder (??), Mistake (?), Inaccuracy (?!)
- **Move Quality Badge** — Colored badge overlaid on the destination square while navigating moves (teal=Brilliant !!, yellow=Inaccuracy ?!, orange=Mistake ?, red=Blunder ??)
- **Evaluation Bar** — Visual position assessment with smooth transitions

**Navigation:**
- Use arrow keys (← →) to step through moves
- Click any move in the list to jump to that position
- Press Backspace to take back the last move
- Press Ctrl+N for a new game, Ctrl+F to flip the board

---

### **2. Play vs Bot**

Challenge any UCI engine directly on the Analysis Board.

**Starting a game:**
1. Click the **♞ vs Bot** button on the Analysis Board
2. Set your target Elo (1320–3190) or pick a preset: Beginner (1350) / Club (1700) / Advanced (2100) / Expert (2500)
3. Choose **Friendly** or **Challenge** mode — Challenge hides engine arrows, eval bar, and analysis output
4. Pick your engine, color (White or Black), then click Start — the board auto-flips if you play Black

**During the game:**
- In Friendly mode, analysis keeps running so you can see engine recommendations for your own moves
- Click **Take Back** (or Backspace) to undo your last move and the bot's response — only available in Friendly mode
- Click **⏹ Stop Bot** to exit bot mode at any time

**Draw detection:** chessdroid automatically detects threefold repetition, insufficient material, and the 50-move rule.

**Note:** Bot mode and engine match mode are mutually exclusive.

---

### **2b. Chess 960 (Fischer Random)**

Play chess with randomized starting positions for the back rank.

**Starting a Chess 960 game:**
1. Click the **♞960** button in the toolbar to open the position browser
2. Browse all 960 starting positions (each has an SP number and FEN)
3. Click **Load** to set up the position on the board, or **Play vs Bot** to start immediately
4. Click **Random** to jump to a random position

**Rules:** Chess 960 uses standard chess rules except for castling — the king and rook move to the same destination squares as in standard chess (g1/c1 for White, g8/c8 for Black), regardless of where they start. The path between king and rook must be clear of other pieces.

**Note:** When playing Chess 960 vs Bot, the engine is automatically configured for Chess960 mode.

---

### **3. Engine vs Engine Matches**

Watch chess engines battle head-to-head on the Analysis Board.

**Setting up a match:**
1. Look for the **Engine Match** panel on the Analysis Board
2. Select engines for White and Black from your Engines folder
3. Choose a time control: Fixed Depth, Time per Move, or Classical
4. Optionally check "Start from current position" to begin from any board position
5. Click Start to begin the match

The match runs automatically until checkmate, stalemate, or you click Stop. All moves are recorded in the move list for post-game analysis. Brilliant moves (!!) are detected in real-time during matches.

---

### **4. Move Classification**

chessdroid uses Chess.com-inspired move classification based on win probability calculations:

**For the Best Move:**
- **Brilliant (!!)** — Piece sacrifice that maintains good position
  - Must sacrifice a piece (not recapturable by pawn)
  - Position remains good after the sacrifice
  - Shows explanation like "sacrifices rook for decisive advantage"

**For Alternative Moves (2nd/3rd best):**
- **Blunder (??)** — Loses 20%+ win probability (Crimson)
- **Mistake (?)** — Loses 10-20% win probability (Orange Red)
- **Inaccuracy (?!)** — Loses 5-10% win probability (Orange)

**Blunder Explanations:**
chessdroid explains WHY moves are bad — not just that they are blunders, but what tactical or positional consequences they create.

**Move Quality Labels:**
When the best move is the only good continuation, chessdroid highlights it:
- **⚡ only good move** — Best move keeps a slight/clear advantage; alternatives drop to equal or worse
- **⚡ only winning move** — Best move keeps a decisive advantage (≥1.50); alternatives lose it
- **⚡ only saving move** — Best move barely holds a difficult position; alternatives lose

```
+0.74 exd5 Qxd5 Nc3 ...
  → ⚡ only good move, captures pawn
+0.06 d3 dxe4 ...
  → INACCURACY equal position
```

---

### **5. Win/Draw/Loss Display (WDL)**

See your winning chances with detailed WDL probabilities:

```
Position: W:72% D:20% L:8% (clear advantage)
```

**Position Types:**
- **Decisive** — One side is winning (W% > 80% or L% > 80%)
- **Clear advantage** — Significant edge (65-80% range)
- **Slight edge** — Small advantage (55-65% range)
- **Balanced** — Equal position (45-55% range)
- **Sharp** — High win AND lose chances, low draw chance

---

### **6. Play Style Recommendations**

Choose your preferred playing style from **Very Solid** to **Very Aggressive**.

**How it works:**
- **0-20 (Very Solid)** — Prefers very safe, quiet positional moves
- **21-40 (Solid)** — Prefers safe moves that maintain position
- **41-60 (Balanced)** — Trusts engine's recommendation
- **61-80 (Aggressive)** — Prefers sharp, tactical moves
- **81-100 (Very Aggressive)** — Prefers the sharpest, most tactical moves

When your play style differs from the engine's top choice, chessdroid shows a "Recommended" section with the move that best fits your style while staying within an acceptable evaluation range.

---

### **7. Tactical Pattern Detection**

chessdroid detects 30+ tactical patterns automatically:

- **Fork** — One piece attacks multiple targets
- **Pin** — Piece can't move without exposing more valuable piece
- **Skewer** — Valuable piece must move, exposing less valuable piece
- **Discovered Attack** — Moving one piece reveals attack from another
- **Zwischenzug** — Intermediate move inserted into a tactical sequence
- **Deflection** — Force piece away from defense
- **Desperado** — Piece about to be captured makes final move
- And many more...

---

### **8. Threat & Defense Display**

**Threats** — NEW threats created by the move:
- Attacks on undefended pieces
- Pins, forks, skewers
- Promotion threats
- Check and checkmate threats

**Defenses** — Defensive aspects of the move:
- Protecting attacked pieces
- Blocking attacks on valuable pieces
- Escaping threats
- Improving king safety

Threats show only what CHANGED — if your piece was already attacking something before the move, it won't be reported again.

---

### **9. Opening Book & ECO Database**

- **12,379+ ECO positions** — Shows opening name with ECO code (e.g., "B01: Scandinavian Defense")
- **Polyglot Opening Books** — 1M+ positions from grandmaster games with book move percentages
- **Book Moves** — See what masters play in your position with frequency weights

---

### **10. Complexity Levels**

Choose your preferred explanation detail:

- **Beginner** — Simple, clear language (e.g., "knight in good position")
- **Intermediate** — Moderate detail with chess terminology (default)
- **Advanced** — Full technical details with chess terminology
- **Master** — Maximum detail with annotations

---

### **11. Feature Toggles**

Customize which analysis features you want to see:

- **Show Best/Second/Third Line** — Multi-PV control
- **Show Engine Arrows** — Colored arrows on the board for engine recommendations
- **Tactical Analysis** — Pins, forks, skewers, discovered attacks
- **Positional Analysis** — Pawn structure, outposts, mobility
- **Endgame Analysis** — Zugzwang, patterns, endgame techniques
- **Opening Principles** — Center control, development
- **Show Threats** — Threat and defense information
- **Show WDL** — Win/Draw/Loss probabilities
- **Show Opening Name** — Detected opening
- **Show Book Moves** — Opening book suggestions
- **Show Move Quality** — Brilliant/Blunder/Mistake/Inaccuracy labels
- **Show Pins** — Pin detection display

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `←` / `→` | Navigate moves (previous / next) |
| `Backspace` | Take back last move |
| `Ctrl+N` | New game |
| `Ctrl+F` | Flip board |

---

## Settings Guide

### **Accessing Settings**

Click the **⚙** button on the Analysis Board to open Settings.

### **Board Colors**

Under the **Board Colors** group in Settings, click the color buttons next to "Light Squares" and "Dark Squares" to open the full Windows RGB color picker. Your chosen colors are applied immediately when you click **Save** and persist across sessions.

### **Recommended Settings by Skill Level**

**Beginner (< 1200 rating):**
- Complexity: Beginner
- Aggressiveness: 50 (Balanced)
- Enable: Tactical Analysis, Opening Principles, Engine Arrows

**Intermediate (1200-1800 rating):**
- Complexity: Intermediate (Default)
- Aggressiveness: 50 (Balanced)
- Enable: All tactical and positional features
- WDL Display: ON

**Advanced (1800-2200 rating):**
- Complexity: Advanced
- Aggressiveness: Adjust to your style
- Enable: All features

**Expert (2200+ rating):**
- Complexity: Master
- Enable: All features
- Aggressiveness: Set to match your playing style

---

## Tips & Tricks

### **Tip 1: Start with Beginner Mode**
Even strong players benefit from simple explanations. Start simple, increase complexity as needed.

### **Tip 2: Use Move Classification for Quick Scanning**
- **!!** (Brilliant) = exceptional sacrifice move
- **No symbol** = best or good move
- **?!** (Inaccuracy) = slightly suboptimal
- **?** (Mistake) = loses advantage
- **??** (Blunder) = major error

### **Tip 3: Compare Multiple Lines**
Enable 2nd and 3rd best lines to understand why one move is better than another.

### **Tip 4: Study Endgames with Specialized Analysis**
In endgame positions, chessdroid provides insights on opposition, rule of the square, and king activity.

### **Tip 5: Use Play Style to Match Your Games**
Playing a must-win game? Set to 80-100. Need a safe draw? Set to 0-20.

### **Tip 6: Import PGN to Review Your Games**
Paste a game from Chess.com, Lichess, or any PGN source to analyze move by move with the Classify Moves button.

### **Tip 7: Check WDL for Close Decisions**
When evaluation is similar, WDL helps understand the true nature of the position.

### **Tip 8: Add Custom Piece Sets**
Create a folder in `Templates/` with 12 PNG files (wK, wQ, wR, wB, wN, wP, bK, bQ, bR, bB, bN, bP) for your own piece theme.

---

## Learning with chessdroid

### **Tactical Training**
1. Load a tactical puzzle position via FEN
2. Try to find the solution
3. Check the analysis — did you spot the tactic?
4. Read the explanation to learn the pattern

### **Game Review**
1. Import your game via PGN
2. Click Classify Moves to review the entire game
3. Step through each move to see classifications
4. Focus on blunders and mistakes — read the explanations for why they're bad

### **Opening Preparation**
1. Start from the standard position
2. Play your planned opening moves
3. Watch the ECO opening name update
4. Check book moves to see what grandmasters play
5. Compare with engine recommendations

### **Style Training**
1. Set your Aggressiveness slider to your preferred style
2. Analyze various positions
3. See when the engine agrees or disagrees with your style
4. Learn which moves are "sharp" vs "solid"

---

## Troubleshooting

### **Problem: No analysis appearing**
**Solution:** Ensure a UCI engine is configured in Settings. Place engine executables in the `Engines` folder.

### **Problem: Explanations are too technical**
**Solution:** Change to Beginner or Intermediate complexity level in Settings.

### **Problem: Too much information displayed**
**Solution:** Disable some feature toggles in Settings.

### **Problem: Want more detail in explanations**
**Solution:** Switch to Advanced or Master complexity level.

### **Problem: Play Style slider doesn't seem to work**
**Solution:** Ensure Aggressiveness is NOT set to exactly 50 (balanced). The filtering only activates at 0-49 or 51-100.

---

## Glossary

**SEE (Static Exchange Evaluation)** — Internal calculation for sacrifice and brilliant move detection (not shown in UI)

**WDL (Win/Draw/Loss)** — Probabilities for each game outcome based on engine evaluation

**Zwischenzug** — An intermediate move inserted into a tactical sequence (German for "in-between move")

**Zugzwang** — Position where any move worsens your position

**Outpost** — Strong square for piece that enemy pawns cannot attack

**Fork** — One piece attacking two or more pieces simultaneously

**Pin** — Piece that cannot move without exposing more valuable piece

**Skewer** — Valuable piece must move, exposing less valuable piece behind it

**Discovered Attack** — Moving one piece reveals attack from piece behind it

**ECO Code** — Encyclopedia of Chess Openings classification system

**Polyglot** — Standard opening book format used by chess GUIs

**UCI** — Universal Chess Interface protocol for engine communication

---

## Version History

**v3.17.0** — The Arena (Current)
- Tournament Mode: simultaneous engine matches, 2×2 live board grid, round-robin/manual pairing, live standings, PGN auto-save
- Engine Match series with adjudication, opening book (Random/Choose), PGN auto-save with eval annotations
- Endgame & Tactics Drills: 10 PGN files, all chapters with researched descriptions, Practice vs Bot, Watch Engines
- Bot Elo targeting (1320–3190 + presets), draw detection (threefold/insufficient material/50-move rule)
- Analysis output overhaul: clickable eval, 10-ply truncation + expandable "...", no blank lines
- Precise (!) move quality — the only winning/saving move
- Daily Puzzle sub-mode, Puzzle by Opening filter, Rating range filter
- Multi-game PGN picker, Drills folder expanded to 10 PGN files

**v3.16.0** — Training Grounds
- Puzzle Training (5.94M Lichess puzzles), Puzzle Rush, Puzzle Gauntlet, Board Vision (Timed + Survival)
- Persistent streaks, accuracy %, personal bests shown before each run, per-section reset links
- Monochromatic board theme, training mode icons, lowercase chessdroid branding

**v3.15.0** — Miami Nightclub
- Sound effects — move, capture, check, game-over, castle
- Live hover preview for piece set and board color dropdowns
- Checkmate detection fix, combo selection revert fix

**v3.14.0** — Theme Suite & Engine Ladder
- 6 UI themes: Dark, Light, Cyberpunk, Dracula, Nord, Sepia
- chessdroid Rating — persistent K=32 engine ladder, seeds from CCRL
- Resizable Board | Moves | Analysis panels with saved splitter positions
- Bot engine picker, position editor piece images, engine info labels on strips

**v3.13.0** — Engine Match & Analysis Polish
- Neutral eval arbiter in engine matches (Stockfish 18 judges every position)
- Smooth eval bar animation — lerps to new values, full fill on mate
- Correct checkmate notation (`Qh4#`), game-over output, and threat labels

**v3.12.0** — Custom Piece Sets
- SVG piece set support: drop any SVG folder into Templates and chessdroid auto-discovers it
- Analysis output color redesign and spacing improvements
- Material strips are now theme-aware with proper light/dark pill backgrounds
- Board column width driven by window height — no dead horizontal space on any monitor
- SVG drag performance: pre-scaled bitmaps, as smooth as Chess.com PNG sets

**v3.11.0** — Game Library
- Save any game to a persistent local library with player names, date, result, PGN, and accuracy scores
- Load saved games back onto the board with full move tree and accuracy data restored
- Inline rename: double-click any game name in the library to rename it
- Engine vs engine matches auto-saved to the library when they finish
- Re-saving an existing game overwrites rather than duplicating

**v3.10.0** — Game Review & Accuracy
- Game accuracy score per side using the Lichess win-probability formula; shown after Classify Moves
- Interactive game review: click any quality count in the summary to jump to the first move of that quality
- [← Game Review] link in analysis output for instant return to the accuracy summary while navigating
- Material difference strips above and below the board (toggle in Settings → Board Colors)
- Reorganized board controls: icon buttons (↺⇅↩♞), board fills full left-panel height with centering
- Last-move highlight during keyboard navigation (← →)
- Eval graph populated immediately when importing an annotated PGN
- Performance: GDI object caching in EvalBarControl and EvalGraphControl; ThreatDetection LINQ eliminated

**v3.9.0** — Eval Graph, Animations & Bot Levels
- Eval graph (score history chart) in right panel; click any point to navigate to that move
- Smooth piece animations (150ms); castling animates the rook; configurable 50–500ms
- Bot difficulty now a full 1–20 level slider instead of Easy/Medium/Hard tiers
- Classification cancellation: clicking New Game or Load while classifying stops immediately
- Engine depth range expanded to 1–40

**v3.8.1** — Bug Fixes & Light Theme Polish
- [See line] board corruption fixed: clicking any PV line on positions with non-standard FEN characters no longer causes pieces to disappear
- Light theme readability improvements: position labels, continuous analysis separator, and Recommended move section are now clearly visible on white backgrounds

**v3.8.0** — Threat Arrows
- Threat arrows (red) on the board: opponent threats against your pieces shown as visual arrows after each analysis, always matching the "⚠ Opponent threats:" text; toggle in Settings → Board Colors
- Book moves shown immediately: opening name and book moves appear in the console before the engine starts thinking
- Color preset combobox pre-selects your current colors when you open Settings
- Opponent threats now also visible during continuous analysis live updates
- Desperado, overload, and pinned attacker detection accuracy improvements

**v3.7.0** — Checkmate Detection & Auto-Play
- Checkmate threat detection: mate-in-1 scan across all pieces; shows "threatens checkmate on g2" so you always know the exact square to protect
- Opponent mate threats visible in analysis before you move
- "Stops checkmate threat" defense label on blocking moves (e.g. Bf3 blocking Qxg2#)
- Auto-play button (▶▶): steps through the move list automatically; pause/resume at any time
- Auto-play speed configurable in Settings (200–2000ms)
- Cleaner endgame analysis: removed obvious and redundant output

**v3.6.0** — Explanation Accuracy
- Opening book arrows: book moves displayed as arrows on the board during opening positions
- 25+ move explanation false positive fixes across all pattern detectors
- 13 new explanation patterns for richer, more specific move descriptions
- "attacks pawn on X" label when a move newly attacks an undefended enemy pawn
- Analysis now starts automatically after loading a FEN

**v3.5.0** — Annotated PGN & Polish
- Annotated PGN export/import: move symbols, eval comments, and engine cache embedded in PGN; importing a classified game restores all colors and analysis instantly
- Right-click square highlighting: tap any square to toggle an orange highlight; right-click drag still draws arrows
- WDL labels now reflect the side to move: "completely winning", "completely losing", "clear disadvantage" etc.
- [See line] animation: first move now visible with highlight; engine analyzes the final position automatically
- Bot mode Challenge/Friendly selector added

**v3.4.0** — Analysis Performance
- Significantly faster analysis: redundant board scans eliminated across the analysis pipeline
- SEE (Static Exchange Evaluation) king lookup optimized: single combined scan per filter call
- Engine line parsing: cached split separator removes allocations during continuous analysis

**v3.3.0** — Continuous Analysis & Stability
- Continuous analysis mode: live PV streaming with compact display; full annotated result at max depth
- Configurable max depth for continuous analysis (default 50, range 10–100)
- Custom position editor: set up arbitrary positions for analysis
- Font settings: configure analysis panel and move list font family and size
- Engine freeze after move 2 fixed
- MoveListBox crash and empty move list fixed
- Brilliant move detection: pinned attackers no longer cause false positives

**v3.2.1** — Bug Fixes
- Bot mode no longer shows engine arrows for the bot's position
- Best move no longer misclassified as inaccuracy
- Stale analysis discarded when navigating quickly
- Spurious analysis from move list selection suppressed

**v3.2.0** — Bot Mode & Board Customization
- Play vs Bot with 5 difficulty levels (Easy → Master)
- Move quality badge overlaid on board during game navigation
- Custom board color picker (full RGB) for light and dark squares

**v3.1.0** — Engine Arrows & Visual Analysis
- Engine arrows on board (green/yellow/red per PV line)
- [See line] — load PV into move tree with animated playback
- Free-draw right-click arrows, auto-analysis on form open
- Removed SEE display and Puzzle Mode

**v3.0.0** — Pure Analysis Board
- Complete pivot: removed all screen detection and computer vision
- AnalysisBoardForm is now the main and only entry point
- Auto-analysis on every move, no manual trigger needed
- Zwischenzug detection, blunder explanations, improved brilliant detection
- ~50MB smaller without OpenCV dependencies

**v2.9.0** — Engine Match Enhancements
- Start from current position in engine matches
- Brilliant move detection during matches
- Dynamic template selection (piece set switching)

**v2.8.0** — Analysis Board & Move Classification
- Full-featured Analysis Board with interactive chess board
- Move tree with variations, PGN import/export
- Move classification, evaluation bar, engine matches, analysis caching

**v2.7.0** — Play Style Recommendations
- Style-based move suggestions (Very Solid → Very Aggressive)

**v2.6.0** — Opening Book & ECO Database
- Polyglot opening book support with 1M+ positions
- 12,379+ ECO opening positions

**v2.5.0** — Chess.com-Style Classification
- Brilliant move detection (!!) and move classification

**v2.0.0** — Major UX Update
- Dark mode, complexity levels, feature toggles
- Architecture overhaul

**v1.0.0** — Initial Release

---

## Credits

**chessdroid** — Created by jio1337

**Inspired by:**
- Stockfish — Threat detection, win rate model, WDL
- Ethereal by Andy Grant — Positional evaluation, pawn structure, SEE
- Leela Chess Zero (Lc0) — WDL display, position sharpness
- Chess.com — Move classification system
- Lichess (lichess.org) — Puzzle database (5.94M puzzles, CC0), piece set assets, win-probability accuracy formula

---

## Getting Help

- GitHub Issues: https://github.com/jio1337/chessdroid/issues
- Documentation: https://github.com/jio1337/chessdroid

---

## License

chessdroid is released under the MIT License. Free and open-source forever!

---

**Enjoy analyzing with chessdroid!**

*Last Updated: 2026-06-01*
*Version: 3.17.0*
