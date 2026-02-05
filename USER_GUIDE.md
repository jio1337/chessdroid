## ChessDroid v3.0.0 — User Guide

### Welcome to ChessDroid!

ChessDroid is an offline chess analysis application built for focused study. Analyze positions, explore ideas, and learn with your favorite UCI engines — with automatic analysis on every move, no buttons to press.

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
- **Move Tree** — All moves tracked in a navigable list with variation branching
- **Auto-Analysis** — Engine analysis runs automatically after every move, with caching for instant results on revisited positions
- **FEN Support** — Load any position via FEN string or start from the standard position
- **Flip Board** — View from White or Black's perspective
- **PGN Import/Export** — Import games from any source, export your analysis to standard PGN format
- **Move Classification** — Review games with colored quality symbols — Brilliant (!!), Blunder (??), Mistake (?), Inaccuracy (?!)
- **Evaluation Bar** — Visual position assessment with smooth transitions

**Navigation:**
- Use arrow keys (← →) to step through moves
- Click any move in the list to jump to that position
- Press Backspace to take back the last move
- Press Ctrl+N for a new game, Ctrl+F to flip the board

---

### **2. Engine vs Engine Matches**

Watch chess engines battle head-to-head on the Analysis Board.

**Setting up a match:**
1. Look for the **Engine Match** panel on the Analysis Board
2. Select engines for White and Black from your Engines folder
3. Choose a time control: Fixed Depth, Time per Move, or Classical
4. Optionally check "Start from current position" to begin from any board position
5. Click Start to begin the match

The match runs automatically until checkmate, stalemate, or you click Stop. All moves are recorded in the move list for post-game analysis. Brilliant moves (!!) are detected in real-time during matches.

---

### **3. Move Classification**

ChessDroid uses Chess.com-inspired move classification based on win probability calculations:

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
ChessDroid explains WHY moves are bad — not just that they are blunders, but what tactical or positional consequences they create.

**"Only Winning Move" Detection:**
When the best move maintains a winning position but alternatives throw away the advantage, ChessDroid highlights this:
```
Best line: Nf6+ Kh8 Qg8 +5.20
  → only winning move, fork on king and queen
```

---

### **4. Win/Draw/Loss Display (WDL)**

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

### **5. Play Style Recommendations**

Choose your preferred playing style from **Very Solid** to **Very Aggressive**.

**How it works:**
- **0-20 (Very Solid)** — Prefers very safe, quiet positional moves
- **21-40 (Solid)** — Prefers safe moves that maintain position
- **41-60 (Balanced)** — Trusts engine's recommendation
- **61-80 (Aggressive)** — Prefers sharp, tactical moves
- **81-100 (Very Aggressive)** — Prefers the sharpest, most tactical moves

When your play style differs from the engine's top choice, ChessDroid shows a "Recommended" section with the move that best fits your style while staying within an acceptable evaluation range.

---

### **6. Tactical Pattern Detection**

ChessDroid detects 30+ tactical patterns automatically:

- **Fork** — One piece attacks multiple targets
- **Pin** — Piece can't move without exposing more valuable piece
- **Skewer** — Valuable piece must move, exposing less valuable piece
- **Discovered Attack** — Moving one piece reveals attack from another
- **Zwischenzug** — Intermediate move inserted into a tactical sequence
- **Deflection** — Force piece away from defense
- **Desperado** — Piece about to be captured makes final move
- And many more...

---

### **7. Threat & Defense Display**

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

### **8. Opening Book & ECO Database**

- **12,379+ ECO positions** — Shows opening name with ECO code (e.g., "B01: Scandinavian Defense")
- **Polyglot Opening Books** — 1M+ positions from grandmaster games with book move percentages
- **Book Moves** — See what masters play in your position with frequency weights

---

### **9. Complexity Levels**

Choose your preferred explanation detail:

- **Beginner** — Simple, clear language (e.g., "knight in good position")
- **Intermediate** — Moderate detail with chess terminology (default)
- **Advanced** — Full technical details with SEE values
- **Master** — Maximum detail with annotations

---

### **10. Feature Toggles**

Customize which analysis features you want to see:

- **Show Best/Second/Third Line** — Multi-PV control
- **Tactical Analysis** — Pins, forks, skewers, discovered attacks
- **Positional Analysis** — Pawn structure, outposts, mobility
- **Endgame Analysis** — Zugzwang, patterns, endgame techniques
- **Opening Principles** — Center control, development
- **SEE Values** — Static Exchange Evaluation
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

### **Recommended Settings by Skill Level**

**Beginner (< 1200 rating):**
- Complexity: Beginner
- Aggressiveness: 50 (Balanced)
- Enable: Tactical Analysis, Opening Principles
- Disable: SEE Values

**Intermediate (1200-1800 rating):**
- Complexity: Intermediate (Default)
- Aggressiveness: 50 (Balanced)
- Enable: All tactical and positional features
- WDL Display: ON

**Advanced (1800-2200 rating):**
- Complexity: Advanced
- Aggressiveness: Adjust to your style
- Enable: All features
- SEE Values: ON

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
In endgame positions, ChessDroid provides insights on opposition, rule of the square, and king activity.

### **Tip 5: Use Play Style to Match Your Games**
Playing a must-win game? Set to 80-100. Need a safe draw? Set to 0-20.

### **Tip 6: Import PGN to Review Your Games**
Paste a game from Chess.com, Lichess, or any PGN source to analyze move by move with the Classify Moves button.

### **Tip 7: Check WDL for Close Decisions**
When evaluation is similar, WDL helps understand the true nature of the position.

### **Tip 8: Add Custom Piece Sets**
Create a folder in `Templates/` with 12 PNG files (wK, wQ, wR, wB, wN, wP, bK, bQ, bR, bB, bN, bP) for your own piece theme.

---

## Learning with ChessDroid

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

**SEE (Static Exchange Evaluation)** — Calculation showing material won/lost after all captures on a square

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

**v3.0.0** — Pure Analysis Board (Current)
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

**ChessDroid** — Created by jio1337

**Inspired by:**
- Stockfish — Threat detection, win rate model, WDL
- Ethereal by Andy Grant — Positional evaluation, pawn structure, SEE
- Leela Chess Zero (Lc0) — WDL display, position sharpness
- Chess.com — Move classification system

---

## Getting Help

- GitHub Issues: https://github.com/jio1337/chessdroid/issues
- Documentation: https://github.com/jio1337/chessdroid

---

## License

ChessDroid is released under the MIT License. Free and open-source forever!

---

**Enjoy analyzing with ChessDroid!**

*Last Updated: 2026-02-05*
*Version: 3.0.0 (Pure Analysis Board)*
