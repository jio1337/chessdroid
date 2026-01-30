## ChessDroid v2.8.0 - User Guide

### Welcome to ChessDroid!

ChessDroid is the most advanced open-source chess analysis tool, combining world-class engine insights with human-readable explanations. Inspired by Chess.com, Stockfish, Ethereal, and Leela Chess Zero.

---

## Quick Start

1. **Load a position** - Enter FEN or set up pieces on board
2. **Click Analyze** - ChessDroid will show the best moves with explanations
3. **Read move classifications** - See Brilliant (!!), Blunder (??), Mistake (?), Inaccuracy (?!) labels
4. **Adjust settings** - Customize complexity level and features
5. **Try Auto-Monitoring** - Enable in Settings for automatic opponent move detection (BETA)

---

## Key Features

### **1. Move Classification**

ChessDroid uses Chess.com-inspired move classification based on win probability calculations:

**For the Best Move:**
- **Brilliant (!!)** - Piece sacrifice that maintains good position
  - Must sacrifice a piece (knight, bishop, rook, or queen)
  - Can't be recaptured by a pawn
  - Position remains good after the sacrifice
  - Shows explanation like "sacrifices rook for decisive advantage"

**For Alternative Moves (2nd/3rd best):**
- **Blunder (??)** - Loses 20%+ win probability (Crimson)
- **Mistake (?)** - Loses 10-20% win probability (Orange Red)
- **Inaccuracy (?!)** - Loses 5-10% win probability (Orange)

**Example:**
```
Best line: Rxe7 Kb8 Rb7 Kxb7 +2.17 !!
  → BRILLIANT | sacrifices rook for decisive advantage

Second best: Qe2 a5 Bd2 +0.30 ??
  → BLUNDER | controls center
```

**"Only Winning Move" Detection:**
When the best move maintains a winning position but alternatives throw away the advantage, ChessDroid highlights this:
```
Best line: Nf6+ Kh8 Qg8 +5.20
  → ⚡ only winning move, fork on king and queen
```

---

### **2. Win/Draw/Loss Display (WDL)**

See your winning chances with detailed WDL probabilities!

**How to read:**
- **W%** - Win probability
- **D%** - Draw probability
- **L%** - Loss probability

**Example:**
```
WDL: 72% / 20% / 8%
```

This shows 72% chance to win, 20% chance to draw, 8% chance to lose.

---

### **3. Position Character**

ChessDroid shows the nature of the position alongside WDL:

**Position Types:**
- **Decisive** - One side is winning (W% > 80% or L% > 80%)
- **Clear advantage** - Significant edge (65-80% range)
- **Slight edge** - Small advantage (55-65% range)
- **Balanced** - Equal position (45-55% range)
- **Sharp** - High win AND lose chances, low draw chance

**Example:**
```
Position: W:78% D:8% L:14% (decisive)
```

This shows White has a decisive advantage with 78% win probability.

---

### **4. Complexity Levels**

Choose your preferred explanation detail:

#### **Beginner Mode**
- Simple, clear language
- No technical jargon
- Focus on basic concepts

**Example:**
```
Before: "knight on strong outpost (SEE +6)"
After:  "knight in good position (wins 6)"
```

#### **Intermediate Mode** (Default)
- Moderate detail
- Some chess terminology
- Balanced explanations

**Example:**
```
"creates threat on queen, knight on strong outpost"
```

#### **Advanced Mode**
- Full technical details
- All SEE values
- Complete analysis

**Example:**
```
"knight on strong outpost attacks undefended queen (SEE +9)"
```

#### **Master Mode**
- Maximum detail
- Technical annotations
- Expert-level information

---

### **5. Threat & Defense Display**

ChessDroid shows threats and defenses for each move!

**Threats** - NEW threats created by the move:
- Attacks on undefended pieces
- Pins (piece pinned to king or valuable piece)
- Forks (attacking multiple pieces)
- Promotion threats
- Check and checkmate threats

**Defenses** - Defensive aspects of the move:
- Protecting attacked pieces
- Blocking attacks on valuable pieces
- Escaping threats (moving attacked piece to safety)
- Improving king safety

**Example Display:**
```
Best line: g3 ... - ! creates connected pawns | defends pawn on f4
Second best: Be3 ... - centralizes piece | attacks rook on a7
```

**Key Improvement:** Threats now show only NEW threats created by the move. If your knight was already attacking the queen before the move, it won't say "attacks queen" - it only shows what changed!

---

### **6. Opening Book (BETA)**

ChessDroid now recognizes 565+ opening positions!

**Features:**
- ECO code display (e.g., B90 - Sicilian, Najdorf)
- Opening name display
- Works for both White and Black

**Example:**
```
Opening: B90 - Sicilian Defense, Najdorf Variation
```

**Coverage includes:**
- Main line openings (Sicilian, French, Caro-Kann, Italian, Spanish, Queen's Gambit)
- Popular gambits (King's Gambit, Evans Gambit, Smith-Morra)
- Indian systems (King's Indian, Nimzo-Indian, Queen's Indian, Grunfeld)

**Note:** BETA status - coverage is not exhaustive

---

### **7. Play Style Slider**

Control your playing style with the Aggressiveness slider (0-100).

**How it works:**
- **0-20 (Very Solid)** - Prefers very safe, quiet positional moves
- **21-40 (Solid)** - Prefers safe moves that maintain position
- **41-60 (Balanced)** - Trusts engine's recommendation
- **61-80 (Aggressive)** - Prefers sharp, tactical moves
- **81-100 (Very Aggressive)** - Prefers the sharpest, most tactical moves

**What affects "sharpness":**
- Captures and sacrifices (more aggressive)
- Pawn breaks and central pushes (more aggressive)
- Checks and forcing moves (more aggressive)
- Castling and quiet development (more solid)
- Defensive moves (more solid)

**Evaluation tolerance:**
- At extreme settings, ChessDroid may accept 0.3-0.4 pawn loss for style
- This means a "second-best" move may be recommended if it matches your style

**Example:**
```
Play Style: 90 (Very Aggressive)
Best Move: Be3 (+0.50)
Recommended: d4! (+0.35) - matches your aggressive style
```

When your play style differs from the engine's top choice, ChessDroid shows a
"Recommended" section with the move that best fits your style while staying
within an acceptable evaluation range.

---

### **8. Auto-Monitoring (BETA)**

Continuous board monitoring that automatically detects opponent moves and triggers analysis.

**How it works:**
1. Enable "Auto-Monitor Board" checkbox in Settings
2. Set up your board position
3. ChessDroid scans every 800ms for position changes
4. When opponent moves, ChessDroid automatically analyzes
5. You see best moves without pressing Alt+X!

**Toggle on/off:**
- Alt+K hotkey - Quick toggle during gameplay
- Settings checkbox - Persistent enable/disable

**When to use:**
- Playing games and want real-time analysis suggestions
- Practicing against engine with instant feedback
- Training mode where you want automatic assistance

**Known Limitations (BETA):**
- Occasional engine crashes on rapid position changes
- May miss opponent moves if they respond very quickly (within 250ms)
- Piece recognition accuracy affects reliability in complex positions
- Auto-monitor is disabled by default on startup

**Tips:**
- Works best with stable board display
- Ensure board is clearly visible
- Use with standard piece sets for best recognition
- Disable if you prefer manual analysis (Alt+X)

---

### **9. Feature Toggles**

Customize which analysis features you want to see:

**Available Toggles:**

- **Show Best Line** - Display the engine's top recommendation (always enabled)
- **Show Second Line** - Display the 2nd best move option
- **Show Third Line** - Display the 3rd best move option
- **Tactical Analysis** - Pins, forks, skewers, discovered attacks
- **Positional Analysis** - Pawn structure, outposts, mobility
- **Endgame Analysis** - Zugzwang, patterns, endgame techniques
- **Opening Principles** - Center control, development
- **SEE Values** - Static Exchange Evaluation
- **Show Threats** - Display threat and defense information
- **Show WDL** - Win/Draw/Loss probabilities
- **Show Opening Name** - Display detected opening
- **Show Book Moves** - Display ABK opening book suggestions
- **Show Move Quality** - Brilliant/Blunder/Mistake/Inaccuracy labels

---

## Understanding Explanations

### **Tactical Explanations**

ChessDroid detects 30+ tactical patterns:

**Common Tactics:**
- **Fork** - One piece attacks multiple targets
- **Pin** - Piece can't move without exposing more valuable piece
- **Skewer** - Valuable piece must move, exposing less valuable piece
- **Discovered Attack** - Moving one piece reveals attack from another
- **Deflection** - Force piece away from defense
- **Decoy** - Lure piece to bad square
- **Desperado** - Piece about to be captured makes final move
- **Zugzwang** - Any move worsens position

**Example:**
```
"fork on king and queen" - Your knight attacks both!
```

---

### **Positional Explanations**

**Pawn Structure:**
- **Passed Pawn** - No enemy pawns blocking promotion
- **Connected Pawns** - Mutually supporting pawns
- **Isolated Pawn** - No friendly pawns on adjacent files (weakness)
- **Doubled Pawns** - Two pawns on same file (weakness)
- **Backward Pawn** - Stuck behind neighbors (weakness)

**Piece Activity:**
- **Outpost** - Piece on square enemy pawns can't attack (strong!)
- **High Mobility** - Piece with many legal moves
- **Long Diagonal** - Bishop controlling key diagonal
- **Central Control** - Controlling d4, e4, d5, e5

**King Safety:**
- **King Shelter** - Pawn shield protecting king
- **Weak Squares** - Vulnerable squares near king
- **Exposed King** - King without pawn protection

**Example:**
```
"knight on strong outpost" - Can't be kicked out by pawns!
```

---

### **Endgame Explanations**

**Game Phases:**
- **Middlegame** - 12+ pieces (excluding kings)
- **Transition** - 8-11 pieces
- **Endgame** - 6-7 pieces
- **Late Endgame** - 5 or fewer pieces

**Endgame Patterns:**
- **K+P vs K** - King and pawn vs king (technical win)
- **Opposite Bishops** - Often drawn despite material advantage
- **Rook Endgame** - Complex, technical play required
- **Bare King** - Checkmate inevitable
- **Zugzwang** - Forced to worsen position

**Example:**
```
"endgame technique: activate king and push passed pawn"
```

---

## Settings Guide

### **Accessing Settings**

Click the "Settings" button in the main window to customize ChessDroid's behavior.

### **Recommended Settings by Skill Level**

**Beginner (< 1200 rating):**
- Complexity: Beginner
- Aggressiveness: 50 (Balanced)
- Enable: Tactical Analysis, Opening Principles
- Disable: SEE Values
- WDL Display: ON

**Intermediate (1200-1800 rating):**
- Complexity: Intermediate (Default)
- Aggressiveness: 50 (Balanced)
- Enable: All tactical and positional features
- WDL Display: ON
- Move Quality: ON

**Advanced (1800-2200 rating):**
- Complexity: Advanced
- Aggressiveness: Adjust to your style
- Enable: All features
- SEE Values: ON

**Expert (2200+ rating):**
- Complexity: Master
- Enable: All features
- Aggressiveness: Set to match your playing style
- May disable Opening Principles (already know them)

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
ChessDroid shows explanations for 1st, 2nd, and 3rd best moves. Compare to understand why one is better.

### **Tip 4: Study Endgames with Advanced Analysis**
In endgame positions, ChessDroid provides specialized analysis including opposition detection, rule of the square, and king activity evaluation.

### **Tip 5: Use Aggressiveness to Match Your Style**
Playing a must-win game? Set to 80-100.
Need a safe draw? Set to 0-20.

### **Tip 6: Adjust for Position Type**
- Tactical puzzle? Enable only Tactical Analysis
- Endgame study? Enable only Endgame Analysis
- General study? Enable everything

### **Tip 7: Check WDL for Close Decisions**
When evaluation is similar, WDL helps understand the true nature of the position.

### **Tip 8: Learn from Opening Book**
Pay attention to the opening name display - it helps you understand what opening you're playing!

---

## Learning with ChessDroid

### **Tactical Training**

1. Load tactical puzzle position
2. Try to find the solution
3. Click Analyze
4. Read explanation - did you spot the tactic?
5. If not, learn the pattern!

### **Positional Understanding**

1. Load a quiet middlegame position
2. Click Analyze
3. Read positional explanations
4. Learn WHY certain moves are better
5. Focus on pawn structure and piece placement

### **Endgame Mastery**

1. Load an endgame position
2. Enable Endgame Analysis for specialized insights
3. Study the engine's recommended moves
4. Learn the winning technique
5. Practice until it's second nature

### **Opening Preparation**

1. Enter starting position
2. Play your planned opening moves
3. Check explanations at each step
4. Watch for the Opening Name display
5. Compare with best engine recommendations

### **Style Training**

1. Set Aggressiveness to your preferred style
2. Analyze various positions
3. See how move recommendations change
4. Learn which moves are "sharp" vs "solid"
5. Develop intuition for your playing style

---

## Troubleshooting

### **Problem: Explanations are too technical**
**Solution:** Change to Beginner or Intermediate complexity level

### **Problem: Too much information displayed**
**Solution:** Disable some feature toggles (e.g., turn off SEE Values)

### **Problem: Not seeing endgame-specific analysis**
**Solution:** Ensure Endgame Analysis is enabled in settings

### **Problem: Want more detail in explanations**
**Solution:** Switch to Advanced or Master complexity level

### **Problem: Settings don't take effect**
**Solution:** This is fixed in v2.2.0! Settings now update immediately without restart.

### **Problem: Aggressiveness slider doesn't seem to work**
**Solution:** Ensure Aggressiveness is NOT set to exactly 50 (balanced). The filtering only activates at 0-49 or 51-100.

---

## Glossary

**SEE (Static Exchange Evaluation)** - Calculation showing material won/lost after all captures on a square

**WDL (Win/Draw/Loss)** - Probabilities for each game outcome based on engine evaluation

**Endgame Analysis** - Specialized endgame analysis including opposition detection, rule of the square, king activity, and drawing patterns

**Zugzwang** - Position where any move worsens your position

**Outpost** - Strong square for piece that enemy pawns cannot attack

**Fork** - One piece attacking two or more pieces simultaneously

**Pin** - Piece that cannot move without exposing more valuable piece

**Skewer** - Valuable piece must move, exposing less valuable piece behind it

**Discovered Attack** - Moving one piece reveals attack from piece behind it

**Complexity Level** - Amount of detail in explanations (Beginner to Master)

**Win Percentage** - Estimated winning chances based on evaluation

**Sharpness** - How tactical/aggressive vs quiet/solid a move is (0-100 scale)

**ECO Code** - Encyclopedia of Chess Openings classification system

---

## Advanced Features

### **Dark Mode**

ChessDroid includes a fully-featured dark mode theme:
1. Open Settings - Check "Dark Mode"
2. Theme applies to all windows and controls
3. Saves your preference automatically

### **Keyboard Shortcuts**

ChessDroid supports global hotkeys (work even when window is minimized):

- **Alt+X** - Analyze position (triggers "Show Lines" button)
- **Alt+K** - Toggle auto-monitoring on/off

---

## Performance Tips

**For Faster Analysis:**
1. Disable unused features (toggles)
2. Use Intermediate complexity (faster than Master)
3. Close other programs while analyzing
4. Ensure engine is properly configured

**For Maximum Detail:**
1. Enable all feature toggles
2. Use Master complexity level
3. Enable SEE Values
4. Enable WDL

---

## Getting Help

**Online Resources:**
- GitHub Issues: https://github.com/jio1337/chessdroid/issues
- Documentation: https://github.com/jio1337/chessdroid
- Chess Programming Wiki: https://www.chessprogramming.org/

**Community:**
- Report bugs on GitHub Issues
- Suggest features via GitHub Discussions
- Contribute code via Pull Requests

---

## Version History

**v2.8.0** - Analysis Board & Move Classification (Current)
- Full-featured Analysis Board with interactive chess board
- Move tree with variations support for exploring alternative lines
- PGN import/export functionality
- Move classification with game review and accuracy percentages
- Evaluation bar for visual position assessment
- Engine matches with configurable time controls
- Analysis caching for instant navigation
- Code quality improvements (thread-safe caching, compiled regex)

**v2.7.0** - Play Style Recommendations
- Play Style slider with style-based move recommendations
- Shows "Recommended" section when style differs from engine choice
- Settings UI reorganization (Play Style section)
- Various UI improvements

**v2.6.0** - Opening Book & ECO Database
- Polyglot (.bin) opening book support with 1M+ positions
- Full ECO database for opening recognition
- Opening names with ECO codes (e.g., "B01: Scandinavian Defense")
- Book moves with weights and percentages

**v2.5.1** - SEE, Explanations, and Redundancy Fixes
- Fixed SEE explanation bug when pieces become vulnerable after move
- Better Brilliant move explanations ("sacrifices rook for decisive advantage")
- Fixed false "rook sacrifice" when piece is defended after capture
- Removed redundant check phrases (handled by threats section)
- Removed obsolete Color-Coded Moves feature

**v2.5.0** - Chess.com-Style Classification
- Brilliant Move Detection (!!) - Piece sacrifices that maintain good position
- Move Classification System - Blunder (??), Mistake (?), Inaccuracy (?!)
- Win probability calculations using logistic function
- "Only Winning Move" detection
- ABK Opening Book support (Arena format)

**v2.2.0** - Lc0-Inspired Features
- WDL Display - Win/Draw/Loss probabilities
- Opening Book - 565+ openings with ECO codes
- Functional Aggressiveness - Slider actually affects move selection
- Config Hot-Reload - Settings changes take effect immediately

**v2.1.0** - Threat & Defense Detection
- Threat Detection - Shows NEW threats created by each move
- Defense Detection - Shows defensive aspects (protecting, blocking, escaping)
- Auto-Monitoring (BETA) - Automatic opponent move detection
- Pin/X-ray fixes - Only reports when material would actually be won

**v2.0.0** - Major UX Update
- Win percentage display (side-aware with Stockfish model)
- Complexity levels (Beginner to Master)
- Feature toggles
- Dark mode theme (fully integrated)
- Global keyboard shortcuts (Alt+X, Alt+K)
- Refactored architecture (73.4% reduction in MainForm)

**v1.6.0** - Optimization & Polish
- 4-10x faster analysis
- Comprehensive documentation
- Performance improvements

**v1.0.0** - Initial Release
- 30+ tactical patterns
- Basic explanations

---

## Credits

**ChessDroid** - Created by jio1337

**Inspired by:**
- Ethereal Chess Engine by Andy Grant - Positional evaluation, pawn structure, SEE
- Stockfish Chess Engine by Stockfish Team - Threat detection, win rate model, WDL
- Leela Chess Zero (Lc0) - WDL display, position sharpness concepts
- Chess.com - Move classification system (Brilliant, Blunder, Mistake, Inaccuracy)
- Arena Chess GUI - ABK opening book format support

**Special Thanks:**
- Chess community for feedback
- Open-source contributors
- Beta testers

---

## License

ChessDroid is released under the MIT License.
Free and open-source forever!

---

**Enjoy analyzing with ChessDroid!**

*Last Updated: 2026-01-30*
*Version: 2.8.0 (Analysis Board & Move Classification)*
