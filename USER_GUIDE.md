## ChessDroid v2.0 - User Guide

### Welcome to ChessDroid!

ChessDroid is the most advanced open-source chess analysis tool, combining world-class engine insights with human-readable explanations.

---

## 🎯 Quick Start

1. **Load a position** - Enter FEN or set up pieces on board
2. **Click Analyze** - ChessDroid will show the best moves
3. **Read explanations** - Color-coded, clear explanations for each move
4. **Adjust settings** - Customize complexity level and features
5. **Try Auto-Monitoring** - Enable in Settings for automatic opponent move detection (BETA)

---

## 🎨 Key Features

### **1. Color-Coded Move Quality**

Moves are now color-coded by quality:

- **🟢 Dark Green (!!)** - Brilliant/Excellent move
- **🟢 Green (!)** - Good, solid move
- **🔵 Blue** - Neutral, acceptable move
- **🟠 Orange (?!)** - Questionable/Dubious move
- **🟠 Red Orange (?)** - Mistake
- **🔴 Dark Red (??)** - Blunder

**Example:**
```
Nf3 !! creates threat on undefended queen, only good move
```

---

### **2. Win Percentage Display**

See your winning chances at a glance!

**How to read:**
- **90%+** - Winning position (dark green)
- **70-90%** - Clearly better (green)
- **55-70%** - Advantage (light blue)
- **45-55%** - Balanced (gray)
- **30-45%** - Disadvantage (orange)
- **Below 30%** - Losing (red)

**Example:**
```
+2.5 (White: 82% win chance)
```

---

### **3. Complexity Levels**

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

### **4. Auto-Monitoring (BETA)**

**NEW!** Continuous board monitoring that automatically detects opponent moves and triggers analysis.

**How it works:**
1. Enable "Auto-Monitor Board" checkbox in Settings
2. Set up your board position
3. ChessDroid scans every 1 second for position changes
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
- Occasional engine crashes on rapid position changes (unrelated to turn detection)
- May miss opponent moves if they respond extremely quickly (within 200ms)
- Piece recognition accuracy affects reliability in complex positions
- Auto-monitor is disabled by default on startup

**Tips:**
- Works best with stable board display
- Ensure board is clearly visible
- Use with standard piece sets for best recognition
- Disable if you prefer manual analysis (Alt+X)

---

### **5. Feature Toggles**

Customize which analysis features you want to see:

**Available Toggles:**

✅ **Show Best Line** - Display the engine's top recommendation (always enabled)

✅ **Show Second Line** - Display the 2nd best move option
- Turn OFF if: You only want the top move
- Best for: Comparing alternatives

✅ **Show Third Line** - Display the 3rd best move option
- Turn OFF if: Too much information
- Best for: Exploring multiple strategies

✅ **Tactical Analysis** - Pins, forks, skewers, discovered attacks
- Turn OFF if: You only want positional analysis
- Best for: Tactical puzzle solving

✅ **Positional Analysis** - Pawn structure, outposts, mobility
- Turn OFF if: You only care about tactics
- Best for: Strategic understanding

✅ **Endgame Analysis** - Zugzwang, patterns, endgame techniques
- Turn OFF if: Only analyzing opening/middlegame
- Best for: Endgame study

✅ **Opening Principles** - Center control, development
- Turn OFF if: Past move 20
- Best for: Opening preparation

✅ **Win Percentage** - Show winning chances
- Turn OFF if: You prefer raw evaluations
- Best for: Understanding position assessment

✅ **Tablebase Info** - Endgame position analysis
- Turn OFF if: Not studying endgames
- Best for: Positions with ≤7 pieces
- Note: Uses tablebase-aware pattern matching (not actual Syzygy files)

✅ **Color-Coded Moves** - Visual quality indicators
- Turn OFF if: You prefer plain text
- Best for: Quick visual feedback

✅ **SEE Values** - Static Exchange Evaluation
- Turn OFF if: Numbers are confusing
- Best for: Understanding capture trades

---

## 📊 Understanding Explanations

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

## ⚙️ Settings Guide

### **Accessing Settings**

Click the "Settings" button in the main window to customize ChessDroid's behavior.

### **Recommended Settings by Skill Level**

**Beginner (< 1200 rating):**
- Complexity: Beginner
- Enable: Tactical Analysis, Opening Principles
- Disable: SEE Values, Tablebase Info
- Win Percentage: ON

**Intermediate (1200-1800 rating):**
- Complexity: Intermediate (Default)
- Enable: All tactical and positional features
- Win Percentage: ON
- Color Coding: ON

**Advanced (1800-2200 rating):**
- Complexity: Advanced
- Enable: All features
- SEE Values: ON
- Tablebase Info: ON

**Expert (2200+ rating):**
- Complexity: Master
- Enable: All features
- May disable Opening Principles (already know them)

---

## 💡 Tips & Tricks

### **Tip 1: Start with Beginner Mode**
Even strong players benefit from simple explanations. Start simple, increase complexity as needed.

### **Tip 2: Use Color Coding for Quick Scanning**
Green = consider this move
Red = avoid this move
Blue = acceptable alternative

### **Tip 3: Compare Multiple Lines**
ChessDroid shows explanations for 1st, 2nd, and 3rd best moves. Compare to understand why one is better.

### **Tip 4: Study Endgames with Tablebase-Aware Analysis**
In positions with ≤7 pieces, ChessDroid provides endgame-specific analysis. Study these positions to understand winning techniques.

### **Tip 5: Study Position Changes**
Pay attention to how evaluations change after moves to understand position dynamics.

### **Tip 6: Adjust for Position Type**
- Tactical puzzle? Enable only Tactical Analysis
- Endgame study? Enable only Endgame Analysis + Tablebase
- General study? Enable everything

### **Tip 7: Check Win Percentage**
When evaluation is confusing (e.g., +1.5 vs +2.0), win percentage makes it clear (70% vs 82%).

### **Tip 8: Learn from Color-Coded Mistakes**
If a move you wanted to play is red (?? or ?), read the explanation to understand why it's bad.

---

## 🎓 Learning with ChessDroid

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

1. Load an endgame (≤7 pieces)
2. Enable Tablebase Info for endgame-specific analysis
3. Study the engine's recommended moves
4. Learn the winning technique
5. Practice until it's second nature

### **Opening Preparation**

1. Enter starting position
2. Play your planned opening moves
3. Check explanations at each step
4. Ensure moves follow opening principles
5. Compare with best engine recommendations

---

## 🔧 Troubleshooting

### **Problem: Explanations are too technical**
**Solution:** Change to Beginner or Intermediate complexity level

### **Problem: Too much information displayed**
**Solution:** Disable some feature toggles (e.g., turn off SEE Values)

### **Problem: Not seeing endgame-specific analysis**
**Solution:** Ensure Tablebase Info is enabled and position has ≤7 pieces

### **Problem: Colors make text hard to read**
**Solution:** Disable Color-Coded Moves in settings

### **Problem: Want more detail in explanations**
**Solution:** Switch to Advanced or Master complexity level

---

## 📖 Glossary

**SEE (Static Exchange Evaluation)** - Calculation showing material won/lost after all captures on a square

**Tablebase-Aware Analysis** - Endgame-specific analysis for positions with ≤7 pieces using pattern recognition

**Zugzwang** - Position where any move worsens your position

**Outpost** - Strong square for piece that enemy pawns cannot attack

**Fork** - One piece attacking two or more pieces simultaneously

**Pin** - Piece that cannot move without exposing more valuable piece

**Skewer** - Valuable piece must move, exposing less valuable piece behind it

**Discovered Attack** - Moving one piece reveals attack from piece behind it

**Complexity Level** - Amount of detail in explanations (Beginner → Master)

**Win Percentage** - Estimated winning chances based on evaluation

---

## 🚀 Advanced Features

### **Dark Mode**

ChessDroid includes a fully-featured dark mode theme:
1. Open Settings → Check "Dark Mode"
2. Theme applies to all windows and controls
3. Saves your preference automatically

### **Keyboard Shortcuts**

ChessDroid supports global hotkeys (work even when window is minimized):

- **Alt+X** - Analyze position (triggers "Show Lines" button)
- **Alt+K** - Toggle auto-monitoring on/off (enables/disables continuous board scanning)

---

## 📊 Performance Tips

**For Faster Analysis:**
1. Disable unused features (toggles)
2. Use Intermediate complexity (faster than Master)
3. Close other programs while analyzing
4. Ensure engine is properly configured

**For Maximum Detail:**
1. Enable all feature toggles
2. Use Master complexity level
3. Enable SEE Values
4. Enable Tablebase Info

---

## 🆘 Getting Help

**Online Resources:**
- GitHub Issues: https://github.com/jio1337/chessdroid/issues
- Documentation: https://github.com/jio1337/chessdroid
- Chess Programming Wiki: https://www.chessprogramming.org/

**Community:**
- Report bugs on GitHub Issues
- Suggest features via GitHub Discussions
- Contribute code via Pull Requests

---

## 📝 Version History

**v3.0.0-BETA** - Auto-Monitoring with Turn Detection
- ⚡ **Auto-Monitor Board (BETA)** - Continuous board scanning with automatic opponent move detection
- ⚡ FEN-based move detection with 200ms debounce
- ⚡ Turn tracking using piece color analysis (white vs black)
- ⚡ Alt+K hotkey to toggle auto-monitoring on/off
- ⚡ Re-enabled BlunderTracker integration for real-time blunder warnings
- 🔧 Board detection cache optimization (60s TTL, ConfirmCache method)
- 🔧 Size filtering to reject oversized boards (>1000px)
- 📝 Known limitations: Occasional engine crashes, may miss very fast moves

**v2.0.0** - Major UX Update
- ✨ Color-coded move quality (6 levels with thematic colors)
- ✨ Win percentage display (side-aware with Stockfish model)
- ✨ Complexity levels (Beginner → Master)
- ✨ Feature toggles (11 toggles including 2nd/3rd line analysis)
- ✨ Dark mode theme (fully integrated)
- ✨ Global keyboard shortcuts (Alt+X, Alt+K)
- 🔧 Refactored architecture (73.4% reduction in MainForm: 1,577→420 lines)

**v1.6.0** - Optimization & Polish
- ⚡ 50% faster analysis
- 📚 Comprehensive documentation
- 🎯 Performance improvements

**v1.5.0** - Advanced Features
- 🎯 Win rate model
- 📖 Opening history
- ♟️ Tablebase-aware analysis

**v1.4.0** - Stockfish Integration
- 🔍 Threat detection
- ⭐ Singular moves
- 📊 Move quality

**v1.0.0** - Initial Release
- ♟️ 30+ tactical patterns
- 📝 Basic explanations

---

## 🙏 Credits

**ChessDroid** - Created by jio1337

**Inspired by:**
- Ethereal Chess Engine by Andy Grant
- Stockfish Chess Engine by Stockfish Team

**Special Thanks:**
- Chess community for feedback
- Open-source contributors
- Beta testers

---

## 📄 License

ChessDroid is released under the MIT License.
Free and open-source forever!

---

**Enjoy analyzing with ChessDroid! ♟️🎯**

*Last Updated: 2026-01-13*
*Version: 3.0.0-BETA (Auto-Monitoring)*
