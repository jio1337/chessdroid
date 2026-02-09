# ChessDroid

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-3.1.0-brightgreen.svg)](CHANGELOG.md)

**Offline chess analysis & training for Windows**

chessdroid is a desktop application built for focused chess study. Analyze positions, explore ideas, and learn with your favorite UCI engines — all on your computer, no internet required.

![chessdroid Analysis Board](chessdroid.png)

## Features

### Analysis Board
- **Interactive Chess Board** — Click to make moves with legal move validation
- **Engine Arrows** — Colored arrows on the board showing engine recommended moves (green/yellow/red per PV line)
- **[See line]** — Click to load any PV line into the move tree as a variation with animated playback
- **Free-Draw Arrows** — Right-click to draw arrows from any square to any square for your own analysis
- **Move Tree with Variations** — Navigate through game history with full variation support
- **Auto-Analysis** — Engine analysis runs automatically on every move, new game, and form open
- **PGN Import/Export** — Import games from any source, export your analysis
- **Move Classification** — Review games with Chess.com-style quality symbols (Brilliant !!, Blunder ??, Mistake ?, Inaccuracy ?!)
- **Evaluation Bar** — Visual representation of position evaluation with smooth transitions
- **FEN Support** — Load any position via FEN string or start from the standard position

### Engine Matches
- **Engine vs Engine** — Watch engines battle head-to-head on the analysis board
- **Configurable Time Controls** — Fixed Depth, Time per Move, or Classical time controls
- **Start from Any Position** — Set up openings manually and have engines continue from there
- **Live Clock Display** — Real-time clock with active side highlighting

### Tactical Analysis
- **30+ Tactical Patterns** — Pins, forks, skewers, discovered attacks, zwischenzug, and more
- **Brilliant Move Detection** — Identifies piece sacrifices that maintain or improve position
- **Blunder Explanations** — Shows WHY moves are bad, not just that they are
- **Threat Detection** — Shows NEW threats created by each move
- **Defense Detection** — Shows defensive aspects of moves (protecting, blocking, escaping)
- **Static Exchange Evaluation (SEE)** — Internal evaluation for sacrifice and brilliant move detection

### Play Style & Analysis
- **Play Style Recommendations** — Style-based move suggestions (Very Solid → Very Aggressive)
- **WDL Display** — Win/Draw/Loss probabilities from engine analysis
- **Polyglot Opening Books** — Industry-standard .bin format with 1M+ positions
- **ECO Database** — 12,379+ opening positions with names and ECO codes
- **Multi-PV Analysis** — Display up to 3 best lines simultaneously with evaluations and explanations

### Endgame Analysis
- **Advanced Heuristics** — Rule of the square, opposition, king activity, fortress detection
- **Passed Pawn Evaluation** — Advancement bonuses and king tropism
- **Drawing Patterns** — Wrong color bishop, insufficient material, and more

### Customization
- **Dark/Light Themes** — Fully-featured dark mode with automatic theme persistence
- **Complexity Levels** — Choose from Beginner, Intermediate, Advanced, or Master explanations
- **Feature Toggles** — Customize analysis display (Tactical, Positional, Endgame, Opening, Win%, WDL, Engine Arrows, etc.)
- **Multiple Piece Sets** — Switch between Chess.com, Lichess, or custom piece templates

## Requirements

- **OS**: Windows 10/11 (64-bit)
- **.NET**: .NET 8.0 Runtime
- **Engine**: Any UCI-compatible chess engine (Stockfish recommended)

## Installation

### Option 1: Download Release (Recommended)

1. Download the latest release from the [Releases](../../releases) page
2. Extract the ZIP file to your preferred location
3. Run `chessdroid.exe`

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/jio1337/chessdroid.git
cd chessdroid

# Navigate to project directory
cd test

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the application
dotnet run --configuration Release
```

## Quick Start

1. **Launch ChessDroid** — Run `chessdroid.exe`
2. **Configure Engine** — Click ⚙ Settings and select your UCI engine
3. **Make Moves** — Click on pieces to make moves on the board
4. **Read Analysis** — Analysis runs automatically after every move with explanations
5. **Import a Game** — Use Import PGN to analyze a complete game move by move

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `←` / `→` | Navigate moves (previous / next) |
| `Backspace` | Take back last move |
| `Ctrl+N` | New game |
| `Ctrl+F` | Flip board |

## Configuration

chessdroid stores settings in `config.json`. You can customize:

### Engine & Analysis
- **Selected Engine** — Choose from available UCI engines in the Engines folder
- **Engine Depth** — Analysis depth (1-30, default: 15)
- **Multi-PV Lines** — Show 2nd and 3rd best lines (optional)
- **Min Analysis Time** — How long the engine thinks (0-5000ms)

### Play Style
- **Aggressiveness** — 0-100 slider (0=solid, 50=balanced, 100=aggressive)

### Display & UI
- **Theme** — Dark or Light mode
- **Complexity Level** — Beginner, Intermediate, Advanced, or Master
- **Feature Toggles** — Enable/disable specific analysis features
- **Piece Set** — Choose from available template folders

## Technology Stack

- **.NET 8.0** — Application framework
- **WinForms** — User interface
- **UCI Protocol** — Chess engine communication
- **Service-Oriented Architecture** — Clean separation of concerns

## Important Notice

**chessdroid is for educational purposes only.**

This tool is designed for:
- Analyzing finished games
- Understanding chess puzzles
- Exploring positions offline
- Chess study and training

**DO NOT use this tool:**
- During live online games
- To gain unfair advantage in rated games
- To violate chess platform terms of service

By using chessdroid, you agree to use it ethically and in accordance with all applicable chess platform policies. Please read our terms on chessdroid.net before downloading/installing.

## Development

### Prerequisites

- Visual Studio 2022 or VS Code
- .NET 8.0 SDK
- Windows 10/11

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build --configuration Release

# Run tests (if available)
dotnet test
```

## Known Issues

- **Opening Book (BETA)** — Coverage is not exhaustive, strongest for main line openings

## FAQ

**Q: Which chess engine should I use?**
A: Stockfish is recommended. Download the latest version from [stockfishchess.org](https://stockfishchess.org/download/) and place it in the Engines folder.

**Q: Can I use this on Mac or Linux?**
A: Currently ChessDroid is Windows-only. Cross-platform support may be added in the future.

**Q: Is this tool allowed on chess platforms?**
A: No. Using analysis tools during live games violates most chess platform terms of service and is considered cheating. Please read our terms on chessdroid.net before downloading/installing.

**Q: How does the Play Style slider work?**
A: The slider (0-100) affects which moves are recommended. At 0 (solid), ChessDroid prefers safe, quiet moves. At 100 (aggressive), it prefers sharp, tactical moves. The engine still calculates all moves, but the selection is filtered based on "sharpness" metrics within acceptable evaluation tolerance.

**Q: How do I add custom piece sets?**
A: Create a folder in `Templates/` with 12 PNG files (wK, wQ, wR, wB, wN, wP, bK, bQ, bR, bB, bN, bP). The folder name will appear in the piece set dropdown.

## Support

Chessdroid is free and always will be. If it's helped you improve your chess, consider supporting the project:

[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/chessdroid)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Stockfish](https://stockfishchess.org/) — World's strongest chess engine, inspiration for move evaluation features
- [Ethereal](https://github.com/AndyGrant/Ethereal) — High-performance chess engine, inspiration for positional evaluation
- [Lc0](https://lczero.org/) — Leela Chess Zero, inspiration for WDL display and aggressiveness features
- [Chess.com](https://www.chess.com/) — Inspiration for move classification system and quality symbols
- [Claude](https://claude.ai/) — AI assistant by Anthropic, instrumental in developing chessdroid's architecture, analysis features, and codebase

## Disclaimer

chessdroid is an independent project and is not affiliated with, endorsed by, or connected to Chess.com, Lichess.org, or any other chess platform. All trademarks belong to their respective owners.

---

**Made with ♟️ by the chessdroid team**

*There would be no chessdroid without [Claude](https://claude.ai/) — from architecture design to tactical pattern detection, Claude has been the co-pilot behind every feature.*
