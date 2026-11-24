## chessdroid — User Guide

### Welcome to chessdroid!

chessdroid is an offline chess analysis application built for focused study. Analyze positions, explore ideas, run engine tournaments, and train with your favorite UCI engines — with automatic analysis on every move, no buttons to press.

For the exact feature list of the version you're running, see [CHANGELOG.md](CHANGELOG.md).

---

## Quick Start

1. **Launch chessdroid** — Run `chessdroid.exe`
2. **Configure your engine** — Click ⚙ Settings, select your UCI engine from the Engines folder
3. **Make moves on the board** — Click or drag pieces to move them, analysis runs automatically
4. **Read the analysis** — See move classifications, explanations, WDL, and opening names
5. **Import a game** — Use Import PGN to analyze a complete game move by move

---

## Key Features

### **1. Analysis Board**

The Analysis Board is the heart of chessdroid — a complete interactive workspace for deep chess analysis.

**Capabilities:**
- **Interactive Board** — Click or drag to make moves with legal move validation
- **Engine Arrows** — Colored arrows on the board showing engine recommended moves (green=best, yellow=2nd, red=3rd)
- **[See line]** — Click to load any PV line into the move tree as a variation with animated playback
- **Free-Draw Arrows** — Right-click and drag to draw arrows from any square to any square for your own analysis
- **Move Tree** — All moves tracked in a navigable list with variation branching
- **Auto-Analysis** — Engine analysis runs automatically on every move, new game, and form open, with caching for instant results
- **FEN Support** — Load any position via FEN string or start from the standard position
- **Position Editor** — Set up an arbitrary position piece by piece, including castling rights and side to move
- **Flip Board** — View from White or Black's perspective
- **PGN Import/Export** — Import games from any source, including multi-game PGN files (a picker lets you choose which game to load); export your analysis to standard PGN format with embedded move annotations
- **Move Classification** — Review games with colored quality symbols — Brilliant (!!), Blunder (??), Mistake (?), Inaccuracy (?!), Precise (!)
- **Move Quality Badge** — Colored badge overlaid on the destination square while navigating moves (teal=Brilliant !!, yellow=Inaccuracy ?!, orange=Mistake ?, red=Blunder ??)
- **Evaluation Bar** — Visual position assessment with smooth transitions
- **Eval Graph** — Score history chart in the right panel; click any point to jump to that move
- **Game Library** — Save any analyzed game locally with player names, date, result, and accuracy scores; reload it later with the full move tree and classification restored

**Navigation:**
- Use arrow keys (← →) to step through moves
- Use ↑ / ↓ to switch between variations at the current position
- Press Home / End to jump to the start / end of the game
- Click any move in the list to jump to that position
- Press Backspace to take back the last move
- Press Ctrl+N for a new game, Ctrl+F to flip the board

---

### **2. Play vs Bot**

Challenge any UCI engine directly on the Analysis Board.

**Starting a game:**
1. Click the **♞ vs Bot** button on the Analysis Board
2. Set your target Elo (1320–3190) or pick a preset: Beginner (1350) / Club (1700) / Advanced (2100) / Expert (2500)
3. Choose **Friendly** or **Challenge** mode — Challenge hides engine arrows, eval bar, and analysis output, and disables take-back
4. Pick your engine, color (White or Black), then click Start — the board auto-flips if you play Black

**During the game:**
- In Friendly mode, analysis keeps running so you can see engine recommendations for your own moves
- Click **Take Back** (or Backspace) to undo your last move and the bot's response — only available in Friendly mode
- Click **⏹ Stop Bot** to exit bot mode at any time

**Draw detection:** chessdroid automatically detects threefold repetition, insufficient material, and the 50-move rule.

**Note:** Bot mode, Engine Match mode, and Tournament mode are mutually exclusive — only one runs at a time.

**Chess engines that don't support Elo limiting** (e.g. Ethereal, Berserk) always play at full strength regardless of the Elo slider — chessdroid detects this at startup and warns you, showing the engine's CCRL rating instead of a false Elo label.

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

Watch two chess engines battle head-to-head on the Analysis Board.

**Setting up a match:**
1. Look for the **Engine Match** panel on the Analysis Board
2. Select engines for White and Black from your Engines folder
3. Choose a time control: Fixed Depth, Time per Move, or Total Time + Increment
4. Optionally check "Start from current position" to begin from any board position
5. Click Start to begin the match

The match runs automatically until checkmate, stalemate, or you click Stop. All moves are recorded in the move list for post-game analysis, and the finished game is auto-saved to the Game Library. If both engines have a CCRL rating configured, the match also updates their Chessdroid rating (see below).

---

### **3b. Tournament Mode**

Run multiple engine-vs-engine match series at once, in a 2×2 live board grid.

**Setting up a tournament:**
1. Open Tournament mode and choose a pairing mode:
   - **Round-robin** — pick a pool of engines, every pair plays a series against each other
   - **Manual** — set up to 4 specific pairings yourself
2. Set games per match, time control, and whether to auto-adjudicate clearly decided/drawn positions
3. Optionally enable an opening book so games don't all start from the standard position
4. Leave **"Update Chessdroid ratings after finishing"** checked (default on) if you want these games to count toward the engines' Chessdroid ratings
5. Click Start Tournament

**During the tournament:**
- Up to 4 pairings run concurrently; as a pairing finishes its series, the next queued pairing starts on that board
- Click any board to expand it full-size and attach the live annotator engine; click again to return to the overview
- Live standings (score, wins/draws/losses, games played) update after every series
- Click **Stop** to end the tournament early — this stops every game, not just the focused one, and still saves whatever results were completed

When the tournament finishes, results (standings + full PGN of every game played) are saved to a `tournament_<timestamp>.txt` file next to chessdroid.exe.

---

### **3c. Chessdroid Rating Ladder**

CCRL ratings come from thousands of games under fixed conditions on dedicated hardware — not necessarily representative of how an engine performs under the settings and time controls you actually use. The **Chessdroid rating** is a separate, locally-calibrated rating: it seeds from an engine's CCRL rating the first time it plays a game inside chessdroid, then drifts based only on games it actually plays here (1v1 Engine Matches and, optionally, Tournament games).

It uses a higher K-factor (32, vs. FIDE's 10) so it converges to a meaningful estimate after a few dozen games instead of needing thousands. Think of it as a rough pecking order among the engines you actually have installed, not a competitor to CCRL.

View and compare engine ratings in the **Engine Profiles** dialog.

---

### **4. Move Classification**

chessdroid uses Chess.com-inspired move classification based on centipawn loss (how much evaluation the actual move gave up compared to the engine's best move), with thresholds that adapt slightly to your Aggressiveness setting:

- **Inaccuracy (?!)** — roughly 50+ centipawns lost
- **Mistake (?)** — roughly 100+ centipawns lost
- **Blunder (??)** — roughly 300+ centipawns lost, or missing/allowing forced checkmate

**For the Best Move:**
- **Brilliant (!!)** — Piece sacrifice that maintains good position
  - Must sacrifice a piece (not recapturable by pawn)
  - Position remains good after the sacrifice
  - Shows explanation like "sacrifices rook for decisive advantage"
- **Precise (!)** — The only move that keeps a winning or saving advantage in a critical position

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

### **10. Training & Study**

chessdroid includes a full training suite, accessible from the Training panel:

- **Puzzle Training** — 5.94M Lichess puzzles (CC0). Includes **Puzzle Rush** (as many as you can solve against the clock), **Puzzle Gauntlet** (survival streak — one wrong move ends the run), and a **Daily Puzzle**. Filter by opening or rating range.
- **Opening Training** — Recreate an opening move by move from memory; lifetime stats are kept per opening (total runs, perfect runs, mistakes, best run)
- **Vision Training** — Board coordinate recognition drills, with Timed and Survival modes and personal bests
- **Square Training** — Easy and Challenge difficulty, with separate personal bests for White, Black, and Random square sets
- **Training Stats** — Click the 📊 button for a unified view of personal bests across every training mode

---

### **11. Endgame & Tactics Drills**

Ten curated drill sets covering common endgame and tactical motifs, loaded from PGN study files.

- **Drill vs Bot** — Play out a drill position against the engine. The rest of the UI (mode switcher, New Game, etc.) locks while a drill is active, so you can't accidentally abandon it mid-game.
- **Drill: Watch Engines** — Let two engines play the position out against each other while you observe

---

### **12. Feature Toggles**

Customize which analysis features you want to see, in Settings:

- **Show Best/Second/Third Line** and **Engine Arrow Count** — Multi-PV display control
- **Tactical Analysis** — Pins, forks, skewers, discovered attacks
- **Positional Analysis** — Pawn structure, outposts, mobility
- **Endgame Analysis** — Zugzwang, patterns, endgame techniques
- **Opening Principles** — Center control, development
- **Show Threats** — Threat and defense information
- **Show WDL** — Win/Draw/Loss probabilities
- **Show Opening Name** — Detected opening
- **Show Book Moves** — Opening book suggestions
- **Show Move Quality** — Brilliant/Precise/Blunder/Mistake/Inaccuracy labels
- **Show Explanations** — Toggle full explanations off for a bare depth header + clickable PVs only

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `←` / `→` | Navigate moves (previous / next) |
| `↑` / `↓` | Switch between variations at the current position |
| `Home` / `End` | Jump to the start / end of the game |
| `Backspace` | Take back last move |
| `Ctrl+N` | New game |
| `Ctrl+F` | Flip board |

---

## Settings Guide

### **Accessing Settings**

Click the **⚙** button on the Analysis Board to open Settings, organized into General, Board, Engine, and Training tabs.

### **Board Colors**

Under the **Board Colors** group in Settings, click the color buttons next to "Light Squares" and "Dark Squares" to open the full Windows RGB color picker. Your chosen colors are applied immediately when you click **Save** and persist across sessions.

### **Panel Layout**

Choose how the Board, Moves, and Analysis panels are arranged from 6 presets — applied live, no restart needed.

### **Getting Started**

If you're new to chessdroid, a reasonable starting point:
- Enable Tactical Analysis, Opening Principles, and Engine Arrows so you can see what the engine is suggesting and why
- Leave Aggressiveness at 50 (Balanced) until you have a feel for how the slider affects recommendations
- Turn on WDL Display once you're comfortable reading evaluations, for a more intuitive sense of winning chances

As you get more comfortable, enable Show Threats and Positional/Endgame Analysis for deeper explanations.

---

## Tips & Tricks

### **Tip 1: Use Move Classification for Quick Scanning**
- **!!** (Brilliant) = exceptional sacrifice move
- **!** (Precise) = the only move keeping the advantage
- **No symbol** = best or good move
- **?!** (Inaccuracy) = slightly suboptimal
- **?** (Mistake) = loses advantage
- **??** (Blunder) = major error

### **Tip 2: Compare Multiple Lines**
Enable 2nd and 3rd best lines to understand why one move is better than another.

### **Tip 3: Study Endgames with Specialized Analysis**
In endgame positions, chessdroid provides insights on opposition, rule of the square, and king activity.

### **Tip 4: Use Play Style to Match Your Games**
Playing a must-win game? Set to 80-100. Need a safe draw? Set to 0-20.

### **Tip 5: Import PGN to Review Your Games**
Paste a game from Chess.com, Lichess, or any PGN source to analyze move by move with the Classify Moves button. If you paste a multi-game PGN (e.g. a tournament export), chessdroid will ask which game to load.

### **Tip 6: Check WDL for Close Decisions**
When evaluation is similar, WDL helps understand the true nature of the position.

### **Tip 7: Use Tournament Mode to Compare Engines**
Set up a round-robin between the engines you have installed and let the Chessdroid rating ladder tell you which actually performs best under your own settings and time control.

### **Tip 8: Add Custom Piece Sets**
Create a folder in `Templates/` with 12 PNG files (wK, wQ, wR, wB, wN, wP, bK, bQ, bR, bB, bN, bP) for your own piece theme.

---

## Learning with chessdroid

### **Tactical Training**
1. Load a tactical puzzle position via FEN, or start a Puzzle Training session
2. Try to find the solution
3. Check the analysis — did you spot the tactic?
4. Read the explanation to learn the pattern

### **Game Review**
1. Import your game via PGN
2. Click Classify Moves to review the entire game
3. Step through each move to see classifications
4. Focus on blunders and mistakes — read the explanations for why they're bad
5. Save it to your Game Library if you want to come back to it later

### **Opening Preparation**
1. Start from the standard position
2. Play your planned opening moves
3. Watch the ECO opening name update
4. Check book moves to see what grandmasters play
5. Compare with engine recommendations
6. Use Opening Training to drill the line from memory

### **Style Training**
1. Set your Aggressiveness slider to your preferred style
2. Analyze various positions
3. See when the engine agrees or disagrees with your style
4. Learn which moves are "sharp" vs "solid"

---

## Troubleshooting

### **Problem: No analysis appearing**
**Solution:** Ensure a UCI engine is configured in Settings. Place engine executables in the `Engines` folder.

### **Problem: Too much information displayed**
**Solution:** Disable some feature toggles in Settings, or turn off Show Explanations for a compact depth-header-only view.

### **Problem: Play Style slider doesn't seem to work**
**Solution:** Ensure Aggressiveness is NOT set to exactly 50 (balanced). The filtering only activates at 0-49 or 51-100.

### **Problem: Bot plays at full strength regardless of the Elo slider**
**Solution:** Some engines (e.g. Ethereal, Berserk) don't support UCI Elo limiting and always play at full strength. chessdroid detects and warns about this when the engine starts; the difficulty label switches to showing the engine's CCRL rating instead.

### **Problem: Take Back is greyed out**
**Solution:** Take-back is intentionally disabled in Challenge mode and during an active Endgame/Tactics Drill. Switch to Friendly mode, or finish/stop the drill first.

---

## Glossary

**SEE (Static Exchange Evaluation)** — Internal calculation for sacrifice and brilliant move detection (not shown in UI)

**WDL (Win/Draw/Loss)** — Probabilities for each game outcome based on engine evaluation

**Chessdroid Rating** — A locally-calibrated Elo-style rating, seeded from CCRL and updated from games actually played inside chessdroid (see section 3c above)

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

chessdroid is actively developed. For the full, up-to-date version history, see [CHANGELOG.md](CHANGELOG.md).

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
