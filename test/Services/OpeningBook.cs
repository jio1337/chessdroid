using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Opening book database for identifying known chess openings.
    /// Uses FEN position keys to match openings from the Encyclopedia of Chess Openings (ECO).
    ///
    /// BETA STATUS: This opening book contains 500+ positions but is not exhaustive.
    /// Coverage is strongest for:
    /// - Main line openings (Sicilian, French, Caro-Kann, Italian, Spanish, Queen's Gambit)
    /// - Popular gambits (King's Gambit, Evans Gambit, Smith-Morra, etc.)
    /// - Indian systems (King's Indian, Nimzo-Indian, Queen's Indian, Grünfeld)
    ///
    /// TODO: Continue expanding with more transpositions and sidelines.
    /// </summary>
    public static class OpeningBook
    {
        /// <summary>
        /// Represents a chess opening
        /// </summary>
        public class Opening
        {
            public string ECO { get; set; } = "";
            public string Name { get; set; } = "";
            public string Moves { get; set; } = "";
        }

        // Dictionary mapping position FEN (just the piece placement) to opening info
        // Key format: piece placement only (first part of FEN before the space)
        private static readonly Dictionary<string, Opening> _openings = new Dictionary<string, Opening>
        {
            // Starting position
            ["rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"] = new Opening { ECO = "", Name = "Starting Position", Moves = "" },

            // A00 - Uncommon Openings
            ["rnbqkbnr/pppppppp/8/8/8/7N/PPPPPPPP/RNBQKB1R"] = new Opening { ECO = "A00", Name = "Amar Opening", Moves = "1. Nh3" },
            ["rnbqkbnr/pppppppp/8/8/6P1/8/PPPPPP1P/RNBQKBNR"] = new Opening { ECO = "A00", Name = "Grob Attack", Moves = "1. g4" },
            ["rnbqkbnr/pppppppp/8/8/8/P7/1PPPPPPP/RNBQKBNR"] = new Opening { ECO = "A00", Name = "Ware Opening", Moves = "1. a4" },

            // A01 - Nimzo-Larsen Attack
            ["rnbqkbnr/pppppppp/8/8/8/1P6/P1PPPPPP/RNBQKBNR"] = new Opening { ECO = "A01", Name = "Nimzo-Larsen Attack", Moves = "1. b3" },

            // A02-A03 - Bird's Opening
            ["rnbqkbnr/pppppppp/8/8/5P2/8/PPPPP1PP/RNBQKBNR"] = new Opening { ECO = "A02", Name = "Bird's Opening", Moves = "1. f4" },

            // A04-A09 - Reti Opening
            ["rnbqkbnr/pppppppp/8/8/8/5N2/PPPPPPPP/RNBQKB1R"] = new Opening { ECO = "A04", Name = "Reti Opening", Moves = "1. Nf3" },
            ["rnbqkbnr/ppp1pppp/8/3p4/8/5N2/PPPPPPPP/RNBQKB1R"] = new Opening { ECO = "A05", Name = "Reti Opening", Moves = "1. Nf3 d5" },
            ["rnbqkbnr/ppp1pppp/8/3p4/2P5/5N2/PP1PPPPP/RNBQKB1R"] = new Opening { ECO = "A09", Name = "Reti Opening: Advance Variation", Moves = "1. Nf3 d5 2. c4" },

            // A10-A39 - English Opening
            ["rnbqkbnr/pppppppp/8/8/2P5/8/PP1PPPPP/RNBQKBNR"] = new Opening { ECO = "A10", Name = "English Opening", Moves = "1. c4" },
            ["rnbqkbnr/pppp1ppp/8/4p3/2P5/8/PP1PPPPP/RNBQKBNR"] = new Opening { ECO = "A20", Name = "English Opening: Reversed Sicilian", Moves = "1. c4 e5" },
            ["rnbqkbnr/pppp1ppp/8/4p3/2P5/5N2/PP1PPPPP/RNBQKB1R"] = new Opening { ECO = "A21", Name = "English Opening: Reversed Sicilian", Moves = "1. c4 e5 2. Nc3" },
            ["rnbqkb1r/pppp1ppp/5n2/4p3/2P5/2N5/PP1PPPPP/R1BQKBNR"] = new Opening { ECO = "A22", Name = "English Opening: Two Knights", Moves = "1. c4 e5 2. Nc3 Nf6" },
            ["rnbqkbnr/ppp1pppp/8/3p4/2P5/8/PP1PPPPP/RNBQKBNR"] = new Opening { ECO = "A30", Name = "English Opening: Symmetrical", Moves = "1. c4 c5" },

            // A40-A44 - Queen's Pawn Game
            ["rnbqkbnr/pppppppp/8/8/3P4/8/PPP1PPPP/RNBQKBNR"] = new Opening { ECO = "A40", Name = "Queen's Pawn Game", Moves = "1. d4" },

            // A45-A46 - Trompowsky Attack & Indian Game
            ["rnbqkb1r/pppppppp/5n2/8/3P4/8/PPP1PPPP/RNBQKBNR"] = new Opening { ECO = "A45", Name = "Indian Game", Moves = "1. d4 Nf6" },
            ["rnbqkb1r/pppppppp/5n2/6B1/3P4/8/PPP1PPPP/RN1QKBNR"] = new Opening { ECO = "A45", Name = "Trompowsky Attack", Moves = "1. d4 Nf6 2. Bg5" },
            ["rnbqkb1r/pppppppp/5n2/8/3P4/5N2/PPP1PPPP/RNBQKB1R"] = new Opening { ECO = "A46", Name = "Indian Game: Knights Variation", Moves = "1. d4 Nf6 2. Nf3" },

            // A50-A79 - Indian Defenses
            ["rnbqkb1r/pppppppp/5n2/8/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A50", Name = "Indian Defense", Moves = "1. d4 Nf6 2. c4" },
            ["rnbqkb1r/pppp1ppp/4pn2/8/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A53", Name = "Old Indian Defense", Moves = "1. d4 Nf6 2. c4 d6" },
            ["rnbqkb1r/pp1ppppp/5n2/2p5/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A57", Name = "Benko Gambit", Moves = "1. d4 Nf6 2. c4 c5" },
            ["rnbqkb1r/p1pppppp/1p3n2/8/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A60", Name = "Benoni Defense", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 c5 4. d5 b5" },

            // A80-A99 - Dutch Defense
            ["rnbqkbnr/ppppp1pp/8/5p2/3P4/8/PPP1PPPP/RNBQKBNR"] = new Opening { ECO = "A80", Name = "Dutch Defense", Moves = "1. d4 f5" },
            ["rnbqkbnr/ppppp1pp/8/5p2/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A81", Name = "Dutch Defense", Moves = "1. d4 f5 2. c4" },

            // B00-B09 - Uncommon King's Pawn Defenses
            ["rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B00", Name = "King's Pawn Opening", Moves = "1. e4" },
            ["rnbqkbnr/ppp1pppp/3p4/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B06", Name = "Pirc Defense", Moves = "1. e4 d6" },
            ["rnbqkbnr/ppp1pppp/3p4/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B07", Name = "Pirc Defense", Moves = "1. e4 d6 2. d4" },
            ["rnbqkb1r/ppp1pppp/3p1n2/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B07", Name = "Pirc Defense: 2...Nf6", Moves = "1. e4 d6 2. d4 Nf6" },
            ["rnbqkb1r/ppp1pppp/3p1n2/8/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B08", Name = "Pirc Defense: Classical", Moves = "1. e4 d6 2. d4 Nf6 3. Nc3" },
            ["rnbqkb1r/ppp1pp1p/3p1np1/8/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B08", Name = "Pirc Defense: Classical, 3...g6", Moves = "1. e4 d6 2. d4 Nf6 3. Nc3 g6" },
            ["rnbqk2r/ppp1ppbp/3p1np1/8/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B09", Name = "Pirc Defense: Austrian Attack", Moves = "1. e4 d6 2. d4 Nf6 3. Nc3 g6 4. f4" },
            ["rnbqkbnr/pppppp1p/6p1/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B06", Name = "Modern Defense", Moves = "1. e4 g6" },
            ["rnbqkbnr/pppppp1p/6p1/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B06", Name = "Modern Defense: 2.d4", Moves = "1. e4 g6 2. d4" },
            ["rnbqk1nr/ppppppbp/6p1/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B06", Name = "Modern Defense: 2...Bg7", Moves = "1. e4 g6 2. d4 Bg7" },
            ["rnbqk1nr/ppppppbp/6p1/8/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B06", Name = "Modern Defense: 3.Nc3", Moves = "1. e4 g6 2. d4 Bg7 3. Nc3" },

            // B01 - Scandinavian Defense
            ["rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense", Moves = "1. e4 d5" },
            ["rnbqkbnr/ppp1pppp/8/3P4/8/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: 2.exd5", Moves = "1. e4 d5 2. exd5" },
            ["rnb1kbnr/ppp1pppp/8/3q4/8/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: Mieses-Kotroc", Moves = "1. e4 d5 2. exd5 Qxd5" },
            ["rnb1kbnr/ppp1pppp/8/3q4/8/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: 3.Nc3", Moves = "1. e4 d5 2. exd5 Qxd5 3. Nc3" },
            ["rnb1kbnr/ppp1pppp/8/8/8/q1N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: 3...Qa5", Moves = "1. e4 d5 2. exd5 Qxd5 3. Nc3 Qa5" },
            ["rnb1kbnr/ppp1pppp/3q4/8/8/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: 3...Qd6", Moves = "1. e4 d5 2. exd5 Qxd5 3. Nc3 Qd6" },
            ["rnbqkb1r/ppp1pppp/5n2/3P4/8/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: 2...Nf6", Moves = "1. e4 d5 2. exd5 Nf6" },
            ["rnbqkb1r/ppp1pppp/5n2/3P4/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: Modern", Moves = "1. e4 d5 2. exd5 Nf6 3. d4" },
            ["rnbqkb1r/ppp1pppp/8/3Pn3/8/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: 3...Nxd5", Moves = "1. e4 d5 2. exd5 Nf6 3. d4 Nxd5" },

            // B02-B05 - Alekhine's Defense
            ["rnbqkb1r/pppppppp/5n2/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B02", Name = "Alekhine's Defense", Moves = "1. e4 Nf6" },
            ["rnbqkb1r/pppppppp/5n2/4P3/8/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B02", Name = "Alekhine's Defense: 2.e5", Moves = "1. e4 Nf6 2. e5" },
            ["rnbqkb1r/pppppppp/8/4P3/3n4/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B02", Name = "Alekhine's Defense: 2...Nd5", Moves = "1. e4 Nf6 2. e5 Nd5" },
            ["rnbqkb1r/pppppppp/8/4P3/3nP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B03", Name = "Alekhine's Defense: Four Pawns Attack", Moves = "1. e4 Nf6 2. e5 Nd5 3. d4" },
            ["rnbqkb1r/ppp1pppp/8/3pP3/3n4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B03", Name = "Alekhine's Defense: 3...d6", Moves = "1. e4 Nf6 2. e5 Nd5 3. d4 d6" },
            ["rnbqkb1r/ppp1pppp/8/3pP3/3n4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B04", Name = "Alekhine's Defense: Modern", Moves = "1. e4 Nf6 2. e5 Nd5 3. d4 d6 4. Nf3" },

            ["rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B20", Name = "Sicilian Defense", Moves = "1. e4 c5" },

            // B10-B19 - Caro-Kann Defense
            ["rnbqkbnr/pp1ppppp/2p5/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B10", Name = "Caro-Kann Defense", Moves = "1. e4 c6" },
            ["rnbqkbnr/pp1ppppp/2p5/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B12", Name = "Caro-Kann Defense", Moves = "1. e4 c6 2. d4" },
            ["rnbqkbnr/pp2pppp/2p5/3p4/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B12", Name = "Caro-Kann Defense", Moves = "1. e4 c6 2. d4 d5" },
            ["rnbqkbnr/pp2pppp/2p5/3pP3/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B12", Name = "Caro-Kann Defense: Advance Variation", Moves = "1. e4 c6 2. d4 d5 3. e5" },
            ["rnbqkbnr/pp2pppp/2p5/3pP3/3P4/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B12", Name = "Caro-Kann Defense: Advance, Short Variation", Moves = "1. e4 c6 2. d4 d5 3. e5 Bf5 4. Nf3" },
            ["rn1qkbnr/pp2pppp/2p5/3pPb2/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B12", Name = "Caro-Kann Defense: Advance, 3...Bf5", Moves = "1. e4 c6 2. d4 d5 3. e5 Bf5" },
            ["rn1qkbnr/pp2pppp/2p5/3pPb2/3P4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B12", Name = "Caro-Kann Defense: Advance, Van der Wiel Attack", Moves = "1. e4 c6 2. d4 d5 3. e5 Bf5 4. Nc3" },
            ["rnbqkbnr/pp2pppp/2p5/3p4/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B13", Name = "Caro-Kann Defense: Classical Variation", Moves = "1. e4 c6 2. d4 d5 3. Nc3" },
            ["rnbqkbnr/pp2pppp/2p5/8/3Pp3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B13", Name = "Caro-Kann Defense: Classical, 3...dxe4", Moves = "1. e4 c6 2. d4 d5 3. Nc3 dxe4" },
            ["rnbqkbnr/pp2pppp/2p5/8/3PN3/8/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B15", Name = "Caro-Kann Defense: Classical, 4.Nxe4", Moves = "1. e4 c6 2. d4 d5 3. Nc3 dxe4 4. Nxe4" },
            ["rn1qkbnr/pp2pppp/2p5/5b2/3PN3/8/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B15", Name = "Caro-Kann Defense: Classical, 4...Bf5", Moves = "1. e4 c6 2. d4 d5 3. Nc3 dxe4 4. Nxe4 Bf5" },
            ["rn1qkbnr/pp2pppp/2p5/5b2/3PN3/5N2/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B15", Name = "Caro-Kann Defense: Classical, Main Line", Moves = "1. e4 c6 2. d4 d5 3. Nc3 dxe4 4. Nxe4 Bf5 5. Ng3" },
            ["rnbqkb1r/pp2pppp/2p2n2/8/3PN3/8/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B17", Name = "Caro-Kann Defense: Steinitz Variation", Moves = "1. e4 c6 2. d4 d5 3. Nc3 dxe4 4. Nxe4 Nd7" },
            ["rnbqkb1r/pp2pppp/5n2/8/3Pn3/5N2/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B18", Name = "Caro-Kann Defense: Classical, 4...Nf6", Moves = "1. e4 c6 2. d4 d5 3. Nc3 dxe4 4. Nxe4 Nf6" },
            ["rnbqkbnr/pp2pppp/2p5/3P4/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B13", Name = "Caro-Kann Defense: Exchange Variation", Moves = "1. e4 c6 2. d4 d5 3. exd5" },
            ["rnbqkbnr/pp2pppp/8/3p4/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B13", Name = "Caro-Kann Defense: Exchange, 3...cxd5", Moves = "1. e4 c6 2. d4 d5 3. exd5 cxd5" },
            ["rnbqkbnr/pp2pppp/2p5/3p4/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B11", Name = "Caro-Kann Defense: Two Knights Attack", Moves = "1. e4 c6 2. Nc3 d5 3. Nf3" },
            ["rnbqkbnr/pp1ppppp/2p5/8/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "B10", Name = "Caro-Kann Defense: Two Knights", Moves = "1. e4 c6 2. Nc3" },
            ["rnbqkbnr/pp2pppp/2p5/3p4/4P3/2N2N2/PPPP1PPP/R1BQKB1R"] = new Opening { ECO = "B11", Name = "Caro-Kann Defense: Two Knights, 3...Bg4", Moves = "1. e4 c6 2. Nc3 d5 3. Nf3 Bg4" },
            ["rnbqkbnr/pp1ppppp/2p5/8/2P1P3/8/PP1P1PPP/RNBQKBNR"] = new Opening { ECO = "B10", Name = "Caro-Kann Defense: Panov-Botvinnik Attack", Moves = "1. e4 c6 2. d4 d5 3. exd5 cxd5 4. c4" },

            // B20-B99 - Sicilian Defense
            ["rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B27", Name = "Sicilian Defense", Moves = "1. e4 c5 2. Nf3" },
            ["rnbqkbnr/pp2pppp/3p4/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B50", Name = "Sicilian Defense", Moves = "1. e4 c5 2. Nf3 d6" },
            ["rnbqkbnr/pp2pppp/3p4/2p5/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B50", Name = "Sicilian Defense: Open", Moves = "1. e4 c5 2. Nf3 d6 3. d4" },
            ["rnbqkbnr/pp2pppp/3p4/8/3pP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B54", Name = "Sicilian Defense: Open", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4" },
            ["r1bqkbnr/pp1ppppp/2n5/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B30", Name = "Sicilian Defense: Old Sicilian", Moves = "1. e4 c5 2. Nf3 Nc6" },
            ["r1bqkbnr/pp1ppppp/2n5/2p5/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B32", Name = "Sicilian Defense: Open", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4" },
            ["rnbqkbnr/pp1p1ppp/4p3/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B40", Name = "Sicilian Defense: French Variation", Moves = "1. e4 c5 2. Nf3 e6" },
            ["r1bqkb1r/pp1ppppp/2n2n2/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B30", Name = "Sicilian Defense: Rossolimo", Moves = "1. e4 c5 2. Nf3 Nc6 3. Bb5" },
            ["rnbqkb1r/pp1ppppp/5n2/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B27", Name = "Sicilian Defense: Hyperaccelerated Dragon", Moves = "1. e4 c5 2. Nf3 g6" },

            // Sicilian Najdorf
            ["rnbqkb1r/1p2pppp/p2p1n2/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B90", Name = "Sicilian Defense: Najdorf Variation", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6" },
            ["rnbqkb1r/1p2pppp/p2p1n2/6B1/3NP3/2N5/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B94", Name = "Sicilian Defense: Najdorf, Bg5", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6 6. Bg5" },
            ["rnbqkb1r/1p2pppp/p2p1n2/8/3NP1P1/2N5/PPP2P1P/R1BQKB1R"] = new Opening { ECO = "B91", Name = "Sicilian Defense: Najdorf, Zagreb", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6 6. g3" },
            ["rnbqkb1r/1p2pppp/p2p1n2/8/3NP3/2N1B3/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B90", Name = "Sicilian Defense: Najdorf, English Attack", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6 6. Be3" },
            ["rnbqkb1r/1p2pppp/p2p1n2/8/3NP3/2N5/PPP1BPPP/R1BQK2R"] = new Opening { ECO = "B92", Name = "Sicilian Defense: Najdorf, Opocensky", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6 6. Be2" },
            ["rnbqkb1r/1p2pppp/p2p1n2/8/3NP3/2N2P2/PPP3PP/R1BQKB1R"] = new Opening { ECO = "B90", Name = "Sicilian Defense: Najdorf, Adams Attack", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6 6. f3" },

            // Sicilian Dragon
            ["rnbqkb1r/pp2pp1p/3p1np1/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B70", Name = "Sicilian Defense: Dragon Variation", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 g6" },
            ["rnbqk2r/pp2ppbp/3p1np1/8/3NP3/2N1B3/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B72", Name = "Sicilian Defense: Dragon, Classical", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 g6 6. Be3 Bg7" },
            ["rnbqk2r/pp2ppbp/3p1np1/8/3NP3/2N1BP2/PPP3PP/R2QKB1R"] = new Opening { ECO = "B76", Name = "Sicilian Defense: Dragon, Yugoslav Attack", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 g6 6. Be3 Bg7 7. f3" },
            ["rnbq1rk1/pp2ppbp/3p1np1/8/3NP3/2N1BP2/PPPQ2PP/R3KB1R"] = new Opening { ECO = "B77", Name = "Sicilian Defense: Dragon, Yugoslav Attack, Main Line", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 g6 6. Be3 Bg7 7. f3 O-O 8. Qd2" },

            // Sicilian Scheveningen
            ["rnbqkb1r/pp3ppp/3ppn2/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B80", Name = "Sicilian Defense: Scheveningen Variation", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e6" },
            ["rnbqkb1r/pp3ppp/3ppn2/6B1/3NP3/2N5/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B82", Name = "Sicilian Defense: Scheveningen, Classical", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e6 6. Be2" },

            // Sicilian Sveshnikov/Kalashnikov
            ["r1bqkb1r/pp1p1ppp/2n1pn2/2p5/4P3/2N2N2/PPPP1PPP/R1BQKB1R"] = new Opening { ECO = "B30", Name = "Sicilian Defense: Sveshnikov Setup", Moves = "1. e4 c5 2. Nf3 Nc6 3. Nc3 e6" },
            ["r1bqkb1r/pp3ppp/2nppn2/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B33", Name = "Sicilian Defense: Sveshnikov", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e6" },
            ["r1bqkb1r/pp3ppp/2np1n2/4p3/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B33", Name = "Sicilian Defense: Sveshnikov Variation", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e5" },
            ["r1bqkb1r/pp3ppp/2np1n2/1N2p3/4P3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B33", Name = "Sicilian Defense: Sveshnikov, 6.Ndb5", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e5 6. Ndb5" },

            // Sicilian Accelerated Dragon
            ["r1bqkbnr/pp1ppp1p/2n3p1/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B34", Name = "Sicilian Defense: Accelerated Dragon", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 g6" },
            ["r1bqkbnr/pp1ppp1p/2n3p1/8/3NP3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B35", Name = "Sicilian Defense: Accelerated Dragon", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 g6 5. Nc3" },
            ["r1bqk2r/pp1pppbp/2n2np1/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B36", Name = "Sicilian Defense: Accelerated Dragon, Maroczy Bind", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 g6 5. c4" },

            // Sicilian Taimanov
            ["r1bqkbnr/pp1p1ppp/2n1p3/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B44", Name = "Sicilian Defense: Taimanov Variation", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 Nc6" },
            ["r1bqkbnr/pp1p1ppp/2n1p3/8/3NP3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B45", Name = "Sicilian Defense: Taimanov", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 Nc6 5. Nc3" },
            ["r1bqkbnr/1p1p1ppp/p1n1p3/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B46", Name = "Sicilian Defense: Taimanov, 5...a6", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 Nc6 5. Nc3 a6" },

            // Sicilian Kan
            ["rnbqkbnr/pp1p1ppp/4p3/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B41", Name = "Sicilian Defense: Kan Variation", Moves = "1. e4 c5 2. Nf3 e6" },
            ["rnbqkbnr/1p1p1ppp/p3p3/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B42", Name = "Sicilian Defense: Kan, 4...a6", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 a6" },

            // Sicilian Classical/Sozin
            ["r1bqkb1r/pp2pppp/2np1n2/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B56", Name = "Sicilian Defense: Classical Variation", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 Nc6" },
            ["r1bqkb1r/pp2pppp/2np1n2/8/2BNP3/2N5/PPP2PPP/R1BQK2R"] = new Opening { ECO = "B57", Name = "Sicilian Defense: Sozin Attack", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 Nc6 6. Bc4" },
            ["r1bqkb1r/pp2pppp/2Np1n2/8/4P3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B56", Name = "Sicilian Defense: Classical, Anti-Sozin", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 Nc6 6. Nxc6" },

            // Sicilian Closed/Grand Prix
            ["rnbqkbnr/pp1ppppp/8/2p5/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "B23", Name = "Sicilian Defense: Closed", Moves = "1. e4 c5 2. Nc3" },
            ["rnbqkbnr/pp1ppppp/8/2p5/4PP2/2N5/PPPP2PP/R1BQKBNR"] = new Opening { ECO = "B21", Name = "Sicilian Defense: Grand Prix Attack", Moves = "1. e4 c5 2. Nc3 Nc6 3. f4" },
            ["r1bqkbnr/pp1ppppp/2n5/2p5/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "B23", Name = "Sicilian Defense: Closed, 2...Nc6", Moves = "1. e4 c5 2. Nc3 Nc6" },

            // Sicilian Alapin
            ["rnbqkbnr/pp1ppppp/8/2p5/4P3/2P5/PP1P1PPP/RNBQKBNR"] = new Opening { ECO = "B22", Name = "Sicilian Defense: Alapin Variation", Moves = "1. e4 c5 2. c3" },
            ["rnbqkbnr/pp2pppp/8/2pp4/4P3/2P5/PP1P1PPP/RNBQKBNR"] = new Opening { ECO = "B22", Name = "Sicilian Defense: Alapin, 2...d5", Moves = "1. e4 c5 2. c3 d5" },
            ["rnbqkb1r/pp2pppp/5n2/2pp4/4P3/2P5/PP1P1PPP/RNBQKBNR"] = new Opening { ECO = "B22", Name = "Sicilian Defense: Alapin, 2...d5 3.exd5 Nf6", Moves = "1. e4 c5 2. c3 d5 3. exd5 Nf6" },

            // Sicilian Smith-Morra Gambit
            ["rnbqkbnr/pp1ppppp/8/2p5/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B21", Name = "Sicilian Defense: Smith-Morra Gambit", Moves = "1. e4 c5 2. d4" },
            ["rnbqkbnr/pp1ppppp/8/8/3pP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B21", Name = "Sicilian Defense: Smith-Morra, 2...cxd4", Moves = "1. e4 c5 2. d4 cxd4" },
            ["rnbqkbnr/pp1ppppp/8/8/3pP3/2P5/PP3PPP/RNBQKBNR"] = new Opening { ECO = "B21", Name = "Sicilian Defense: Smith-Morra Gambit Accepted", Moves = "1. e4 c5 2. d4 cxd4 3. c3" },

            // Sicilian Moscow/Rossolimo
            ["r1bqkbnr/pp1ppppp/2n5/1Bp5/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "B51", Name = "Sicilian Defense: Moscow Variation", Moves = "1. e4 c5 2. Nf3 d6 3. Bb5+" },
            ["r1bqkb1r/pp1ppppp/2n2n2/1Bp5/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "B52", Name = "Sicilian Defense: Moscow, 3...Bd7", Moves = "1. e4 c5 2. Nf3 d6 3. Bb5+ Bd7" },

            // C00-C19 - French Defense
            ["rnbqkbnr/pppp1ppp/4p3/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "C00", Name = "French Defense", Moves = "1. e4 e6" },
            ["rnbqkbnr/pppp1ppp/4p3/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C00", Name = "French Defense", Moves = "1. e4 e6 2. d4" },
            ["rnbqkbnr/ppp2ppp/4p3/3p4/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C00", Name = "French Defense", Moves = "1. e4 e6 2. d4 d5" },
            ["rnbqkbnr/ppp2ppp/4p3/3pP3/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C02", Name = "French Defense: Advance Variation", Moves = "1. e4 e6 2. d4 d5 3. e5" },
            ["rnbqkbnr/pp3ppp/4p3/2ppP3/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C02", Name = "French Defense: Advance, 3...c5", Moves = "1. e4 e6 2. d4 d5 3. e5 c5" },
            ["rnbqkbnr/pp3ppp/4p3/2PpP3/8/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C02", Name = "French Defense: Advance, Milner-Barry Gambit", Moves = "1. e4 e6 2. d4 d5 3. e5 c5 4. c3 Nc6 5. Nf3 Qb6 6. Bd3" },
            ["rnbqkbnr/ppp2ppp/4p3/3p4/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C03", Name = "French Defense: Tarrasch Variation", Moves = "1. e4 e6 2. d4 d5 3. Nd2" },
            ["rnbqkbnr/ppp2ppp/4pn2/3p4/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C03", Name = "French Defense: Tarrasch, 3...Nf6", Moves = "1. e4 e6 2. d4 d5 3. Nd2 Nf6" },
            ["rnbqkbnr/pp3ppp/4p3/2pp4/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C03", Name = "French Defense: Tarrasch, 3...c5", Moves = "1. e4 e6 2. d4 d5 3. Nd2 c5" },
            ["rnbqkbnr/ppp2ppp/4p3/3P4/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C01", Name = "French Defense: Exchange Variation", Moves = "1. e4 e6 2. d4 d5 3. exd5" },
            ["rnbqkbnr/ppp2ppp/8/3p4/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C01", Name = "French Defense: Exchange, 3...exd5", Moves = "1. e4 e6 2. d4 d5 3. exd5 exd5" },
            ["rnbqkbnr/ppp2ppp/4p3/3p4/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C10", Name = "French Defense: Paulsen Variation", Moves = "1. e4 e6 2. d4 d5 3. Nc3" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C11", Name = "French Defense: Classical, 3...Nf6", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Nf6" },
            ["rnbqkb1r/ppp2ppp/4pn2/3pP3/3P4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C11", Name = "French Defense: Classical, Steinitz Variation", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Nf6 4. e5" },
            ["rnbqkb1r/pppn1ppp/4p3/3pP3/3P4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C11", Name = "French Defense: Classical, Steinitz, 4...Nfd7", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Nf6 4. e5 Nfd7" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/3PP3/2N2N2/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "C11", Name = "French Defense: Classical, Burn Variation", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Nf6 4. Bg5" },
            ["rnbqkbnr/pp3ppp/4p3/2pp4/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C15", Name = "French Defense: Winawer Variation", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4" },
            ["rnbqk1nr/ppp2ppp/4p3/3p4/1b1PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C15", Name = "French Defense: Winawer", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4" },
            ["rnbqk1nr/ppp2ppp/4p3/3pP3/1b1P4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C16", Name = "French Defense: Winawer, Advance", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5" },
            ["rnbqk1nr/pp3ppp/4p3/2ppP3/1b1P4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C17", Name = "French Defense: Winawer, 4...c5", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5 c5" },
            ["rnbqk1nr/pp3ppp/4p3/2ppP3/1b1P4/P1N5/1PP2PPP/R1BQKBNR"] = new Opening { ECO = "C18", Name = "French Defense: Winawer, Classical", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5 c5 5. a3" },
            ["rnbqk2r/pp3ppp/4pn2/2ppP3/3P4/P1P5/2P2PPP/R1BQKBNR"] = new Opening { ECO = "C19", Name = "French Defense: Winawer, Poisoned Pawn", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5 c5 5. a3 Bxc3+ 6. bxc3" },
            ["rnbqkbnr/ppp2ppp/4p3/3p4/3PP3/4B3/PPP2PPP/RN1QKBNR"] = new Opening { ECO = "C00", Name = "French Defense: La Bourdonnais Variation", Moves = "1. e4 e6 2. d4 d5 3. Be3" },
            ["rnbqkbnr/ppp2ppp/4p3/3p4/3PP3/8/PPPN1PPP/R1BQKBNR"] = new Opening { ECO = "C06", Name = "French Defense: Rubinstein Variation", Moves = "1. e4 e6 2. d4 d5 3. Nc3 dxe4" },

            // C20-C29 - King's Pawn Game
            ["rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "C20", Name = "King's Pawn Game", Moves = "1. e4 e5" },
            ["rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C40", Name = "King's Knight Opening", Moves = "1. e4 e5 2. Nf3" },

            // C21-C22 - Center Game & Danish Gambit
            ["rnbqkbnr/pppp1ppp/8/4p3/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C21", Name = "Center Game", Moves = "1. e4 e5 2. d4" },

            // C23-C24 - Bishop's Opening
            ["rnbqkbnr/pppp1ppp/8/4p3/2B1P3/8/PPPP1PPP/RNBQK1NR"] = new Opening { ECO = "C23", Name = "Bishop's Opening", Moves = "1. e4 e5 2. Bc4" },

            // C25-C29 - Vienna Game
            ["rnbqkbnr/pppp1ppp/8/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "C25", Name = "Vienna Game", Moves = "1. e4 e5 2. Nc3" },

            // C30-C39 - King's Gambit
            ["rnbqkbnr/pppp1ppp/8/4p3/4PP2/8/PPPP2PP/RNBQKBNR"] = new Opening { ECO = "C30", Name = "King's Gambit", Moves = "1. e4 e5 2. f4" },
            ["rnbqkbnr/pppp1ppp/8/8/4Pp2/8/PPPP2PP/RNBQKBNR"] = new Opening { ECO = "C33", Name = "King's Gambit Accepted", Moves = "1. e4 e5 2. f4 exf4" },

            // C40-C49 - King's Knight Opening
            ["r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C44", Name = "King's Knight Opening", Moves = "1. e4 e5 2. Nf3 Nc6" },
            ["r1bqkbnr/pppp1ppp/2n5/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C50", Name = "Italian Game", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4" },
            ["r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C44", Name = "Scotch Game", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4" },

            // C42-C43 - Petrov Defense (Russian Game)
            ["rnbqkb1r/pppp1ppp/5n2/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense", Moves = "1. e4 e5 2. Nf3 Nf6" },
            ["rnbqkb1r/pppp1ppp/5n2/4P3/8/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense: Classical Attack", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5" },

            // C44 - Scotch Game
            ["r1bqkbnr/pppp1ppp/2n5/4p3/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C44", Name = "Scotch Game", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4" },
            ["r1bqkbnr/pppp1ppp/2n5/8/3pP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C45", Name = "Scotch Game", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4 exd4" },

            // C50-C59 - Italian Game (Giuoco Piano)
            ["r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C50", Name = "Italian Game", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5" },
            ["r1bqk1nr/pppp1ppp/2n5/2b1p3/2BPP3/5N2/PPP2PPP/RNBQK2R"] = new Opening { ECO = "C53", Name = "Italian Game: Classical Variation", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. c3" },
            ["r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/2P2N2/PP1P1PPP/RNBQK2R"] = new Opening { ECO = "C54", Name = "Italian Game: Giuoco Piano", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. c3" },
            ["r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/3P1N2/PPP2PPP/RNBQK2R"] = new Opening { ECO = "C50", Name = "Italian Game: Giuoco Pianissimo", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. d3" },
            ["r1bqk2r/pppp1ppp/2n2n2/2b1p3/2B1P3/3P1N2/PPP2PPP/RNBQK2R"] = new Opening { ECO = "C50", Name = "Italian Game: Giuoco Pianissimo, 4...Nf6", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. d3 Nf6" },
            ["r1bqk2r/pppp1ppp/2n2n2/2b1p3/2B1P3/2NP1N2/PPP2PPP/R1BQK2R"] = new Opening { ECO = "C50", Name = "Italian Game: Giuoco Pianissimo, Normal", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. d3 Nf6 5. Nc3" },
            ["r1bqk1nr/ppp2ppp/2np4/2b1p3/2B1P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C50", Name = "Italian Game: Hungarian Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Be7" },
            ["r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C55", Name = "Italian Game: Two Knights Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6" },
            ["r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/2N2N2/PPPP1PPP/R1BQK2R"] = new Opening { ECO = "C55", Name = "Italian Game: Two Knights, 4.Nc3", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. Nc3" },
            ["r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/3P1N2/PPP2PPP/RNBQK2R"] = new Opening { ECO = "C55", Name = "Italian Game: Two Knights, 4.d3", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. d3" },
            ["r1bqkb1r/pppp1ppp/2n2n2/4p1N1/2B1P3/8/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C57", Name = "Italian Game: Two Knights, Fried Liver Attack", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. Ng5" },
            ["r1bqkb1r/ppp2ppp/2n2n2/3pp1N1/2B1P3/8/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C57", Name = "Italian Game: Two Knights, 4...d5", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. Ng5 d5" },
            ["r1bqkb1r/ppp2ppp/2n2n2/3Np3/2B1P3/8/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C57", Name = "Italian Game: Two Knights, 5.exd5", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. Ng5 d5 5. exd5" },
            ["r1bqkb1r/ppp2ppp/5n2/3np1N1/2B5/8/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C57", Name = "Italian Game: Fried Liver, 5...Nxd5", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. Ng5 d5 5. exd5 Nxd5" },
            ["r1bqkb1r/p1p2ppp/2n2n2/1p1Np3/2B5/8/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C58", Name = "Italian Game: Two Knights, Polerio Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. Ng5 d5 5. exd5 Na5" },
            ["r1bqkb1r/pppp2pp/2n2n2/4pp2/2B1P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C56", Name = "Italian Game: Scotch Gambit, Max Lange Attack", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. O-O Nf6 5. d4" },

            // C60-C99 - Ruy Lopez (Spanish Game)
            ["r1bqkbnr/pppp1ppp/2n5/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C60", Name = "Ruy Lopez", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5" },
            ["r1bqkbnr/1ppp1ppp/p1n5/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C68", Name = "Ruy Lopez: Morphy Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6" },
            ["r1bqkbnr/1ppp1ppp/p1B5/4p3/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C68", Name = "Ruy Lopez: Exchange Variation", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Bxc6" },
            ["r1bqkbnr/1ppp1ppp/p1n5/4p3/B3P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C70", Name = "Ruy Lopez: Morphy Defense, 4.Ba4", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4" },
            ["r1bqkb1r/1ppp1ppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C78", Name = "Ruy Lopez: Morphy Defense, 4...Nf6", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6" },
            ["r1bqkb1r/1ppp1ppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQ1RK1"] = new Opening { ECO = "C84", Name = "Ruy Lopez: Closed", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O" },
            ["r1bqk2r/1pppbppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQ1RK1"] = new Opening { ECO = "C88", Name = "Ruy Lopez: Closed, 5...Be7", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7" },
            ["r1bqk2r/1pppbppp/p1n2n2/4p3/B3P3/5N2/PPPP1PPP/RNBQR1K1"] = new Opening { ECO = "C88", Name = "Ruy Lopez: Closed, 6.Re1", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1" },
            ["r1bqk2r/2ppbppp/p1n2n2/1p2p3/B3P3/5N2/PPPP1PPP/RNBQR1K1"] = new Opening { ECO = "C88", Name = "Ruy Lopez: Closed, 6...b5", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1 b5" },
            ["r1bqk2r/2ppbppp/p1n2n2/1p2p3/4P3/1B3N2/PPPP1PPP/RNBQR1K1"] = new Opening { ECO = "C88", Name = "Ruy Lopez: Closed, 7.Bb3", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1 b5 7. Bb3" },
            ["r1bq1rk1/2ppbppp/p1n2n2/1p2p3/4P3/1B3N2/PPPP1PPP/RNBQR1K1"] = new Opening { ECO = "C88", Name = "Ruy Lopez: Closed, 7...O-O", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1 b5 7. Bb3 O-O" },
            ["r1bq1rk1/2ppbppp/p1n2n2/1p2p3/4P3/1BP2N2/PP1P1PPP/RNBQR1K1"] = new Opening { ECO = "C92", Name = "Ruy Lopez: Closed, Chigorin Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1 b5 7. Bb3 O-O 8. c3" },
            ["r1bq1rk1/2p1bppp/p1np1n2/1p2p3/4P3/1BP2N2/PP1P1PPP/RNBQR1K1"] = new Opening { ECO = "C92", Name = "Ruy Lopez: Closed, Chigorin, 8...d6", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1 b5 7. Bb3 O-O 8. c3 d6" },
            ["r1bq1rk1/2p1bppp/p1np1n2/1p2p3/3PP3/1BP2N2/PP3PPP/RNBQR1K1"] = new Opening { ECO = "C92", Name = "Ruy Lopez: Closed, 9.h3", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1 b5 7. Bb3 O-O 8. c3 d6 9. h3" },
            ["r1bqkb1r/1ppp1ppp/p1n2n2/4p3/B2PP3/5N2/PPP2PPP/RNBQK2R"] = new Opening { ECO = "C80", Name = "Ruy Lopez: Open", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Nxe4" },
            ["r1bqkb1r/1ppp1ppp/p1n5/4p3/B3n3/5N2/PPPP1PPP/RNBQ1RK1"] = new Opening { ECO = "C80", Name = "Ruy Lopez: Open, 5...Nxe4", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Nxe4" },
            ["r1bqkb1r/1ppp1ppp/p1n5/4p3/B2Pn3/5N2/PPP2PPP/RNBQ1RK1"] = new Opening { ECO = "C80", Name = "Ruy Lopez: Open, 6.d4", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Nxe4 6. d4" },
            ["r1bqkb1r/2pp1ppp/p1n5/1p2p3/B2Pn3/5N2/PPP2PPP/RNBQ1RK1"] = new Opening { ECO = "C81", Name = "Ruy Lopez: Open, Howell Attack", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Nxe4 6. d4 b5" },
            ["r1bqkbnr/pppp1p1p/2n3p1/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C60", Name = "Ruy Lopez: Fianchetto Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 g6" },
            ["r1bqkbnr/1ppp1ppp/p1n5/4p3/B3P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C70", Name = "Ruy Lopez: Morphy Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4" },
            ["r1bqkbnr/pppp2pp/2n2p2/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C63", Name = "Ruy Lopez: Schliemann Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 f5" },
            ["r1bqk1nr/pppp1ppp/2n5/1Bb1p3/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C61", Name = "Ruy Lopez: Bird's Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 Nd4" },
            ["r1bqkbnr/pppp1ppp/8/1B2p3/3nP3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C61", Name = "Ruy Lopez: Bird's Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 Nd4" },
            ["r1bqkb1r/pppp1ppp/2n2n2/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C65", Name = "Ruy Lopez: Berlin Defense", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 Nf6" },
            ["r1bqkb1r/pppp1ppp/2B2n2/4p3/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C67", Name = "Ruy Lopez: Berlin, 4.Bxc6", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 Nf6 4. Bxc6" },
            ["r1bqkb1r/pppp1ppp/2n2n2/1B2p3/4P3/5N2/PPPP1PPP/RNBQ1RK1"] = new Opening { ECO = "C65", Name = "Ruy Lopez: Berlin, 4.O-O", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 Nf6 4. O-O" },
            ["r1bqkb1r/pppp1ppp/2n5/1B2p3/4n3/5N2/PPPP1PPP/RNBQ1RK1"] = new Opening { ECO = "C67", Name = "Ruy Lopez: Berlin, 4...Nxe4", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 Nf6 4. O-O Nxe4" },
            ["r1bqkb1r/pppp1ppp/2n5/1B2p3/3Pn3/5N2/PPP2PPP/RNBQ1RK1"] = new Opening { ECO = "C67", Name = "Ruy Lopez: Berlin Defense, 5.d4", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 Nf6 4. O-O Nxe4 5. d4" },
            ["r2qkb1r/ppp2ppp/2np1n2/1B2p1B1/4P1b1/5N2/PPPP1PPP/RN1Q1RK1"] = new Opening { ECO = "C66", Name = "Ruy Lopez: Berlin, 4.O-O d6", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 Nf6 4. O-O d6" },

            // D00-D05 - Queen's Pawn Game
            ["rnbqkbnr/ppp1pppp/8/3p4/3P4/8/PPP1PPPP/RNBQKBNR"] = new Opening { ECO = "D00", Name = "Queen's Pawn Game", Moves = "1. d4 d5" },
            ["rnbqkbnr/ppp1pppp/8/3p4/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D02", Name = "Queen's Pawn Game", Moves = "1. d4 d5 2. c4" },

            // D06-D69 - Queen's Gambit
            ["rnbqkbnr/ppp1pppp/8/3p4/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D06", Name = "Queen's Gambit", Moves = "1. d4 d5 2. c4" },
            ["rnbqkbnr/ppp2ppp/4p3/3p4/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D30", Name = "Queen's Gambit Declined", Moves = "1. d4 d5 2. c4 e6" },
            ["rnbqkbnr/ppp2ppp/4p3/3p4/2PP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "D31", Name = "Queen's Gambit Declined", Moves = "1. d4 d5 2. c4 e6 3. Nc3" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/2PP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "D35", Name = "Queen's Gambit Declined: Exchange Variation", Moves = "1. d4 d5 2. c4 e6 3. Nc3 Nf6" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "D37", Name = "Queen's Gambit Declined: 3.Nf3", Moves = "1. d4 d5 2. c4 e6 3. Nf3" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/2PP4/5NP1/PP2PP1P/RNBQKB1R"] = new Opening { ECO = "D37", Name = "Queen's Gambit Declined: Three Knights", Moves = "1. d4 d5 2. c4 e6 3. Nf3 Nf6 4. Nc3" },
            ["rnbqkb1r/ppp2ppp/4pn2/3P4/3P4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "D35", Name = "Queen's Gambit Declined: Exchange", Moves = "1. d4 d5 2. c4 e6 3. Nc3 Nf6 4. cxd5" },
            ["rnbqkb1r/ppp2ppp/5n2/3p4/3P4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "D35", Name = "Queen's Gambit Declined: Exchange, 4...exd5", Moves = "1. d4 d5 2. c4 e6 3. Nc3 Nf6 4. cxd5 exd5" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p2B1/2PP4/2N5/PP2PPPP/R2QKBNR"] = new Opening { ECO = "D53", Name = "Queen's Gambit Declined: Orthodox Defense", Moves = "1. d4 d5 2. c4 e6 3. Nc3 Nf6 4. Bg5" },
            ["rnbqk2r/ppp1bppp/4pn2/3p2B1/2PP4/2N5/PP2PPPP/R2QKBNR"] = new Opening { ECO = "D53", Name = "Queen's Gambit Declined: Orthodox, 4...Be7", Moves = "1. d4 d5 2. c4 e6 3. Nc3 Nf6 4. Bg5 Be7" },
            ["rnbqk2r/ppp1bppp/4pn2/3p2B1/2PP4/2N2N2/PP2PPPP/R2QKB1R"] = new Opening { ECO = "D55", Name = "Queen's Gambit Declined: Orthodox, Main Line", Moves = "1. d4 d5 2. c4 e6 3. Nc3 Nf6 4. Bg5 Be7 5. Nf3" },
            ["rnbqk2r/ppp1bppp/4pn2/3p4/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D37", Name = "Queen's Gambit Declined: Ragozin Defense", Moves = "1. d4 d5 2. c4 e6 3. Nc3 Nf6 4. Nf3 Bb4" },
            ["rnbqkbnr/ppp2ppp/4p3/3p4/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "D30", Name = "Queen's Gambit Declined: 2...e6 3.Nf3", Moves = "1. d4 d5 2. c4 e6 3. Nf3" },
            ["rnb1kb1r/pp3ppp/1qp1pn2/3p4/2PP4/5NP1/PP2PPBP/RNBQK2R"] = new Opening { ECO = "D38", Name = "Queen's Gambit Declined: Ragozin, Vienna Variation", Moves = "1. d4 d5 2. c4 e6 3. Nf3 Nf6 4. Nc3 Bb4 5. Bg5 c6" },
            ["rnbqkbnr/ppp2ppp/8/3pp3/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D07", Name = "Queen's Gambit Declined: Chigorin Defense", Moves = "1. d4 d5 2. c4 Nc6" },
            ["r1bqkbnr/ppp2ppp/2n1p3/3p4/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "D07", Name = "Queen's Gambit Declined: Chigorin, 3.Nf3", Moves = "1. d4 d5 2. c4 Nc6 3. Nf3" },
            ["rnbqkbnr/ppp1pppp/8/8/2pP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D20", Name = "Queen's Gambit Accepted", Moves = "1. d4 d5 2. c4 dxc4" },
            ["rnbqkbnr/ppp1pppp/8/8/2pP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "D24", Name = "Queen's Gambit Accepted: 3.Nf3", Moves = "1. d4 d5 2. c4 dxc4 3. Nf3" },
            ["rnbqkbnr/ppp1pppp/8/8/2pP4/4P3/PP3PPP/RNBQKBNR"] = new Opening { ECO = "D20", Name = "Queen's Gambit Accepted: 3.e3", Moves = "1. d4 d5 2. c4 dxc4 3. e3" },
            ["rnbqkb1r/ppp1pppp/5n2/8/2pP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "D24", Name = "Queen's Gambit Accepted: 3...Nf6", Moves = "1. d4 d5 2. c4 dxc4 3. Nf3 Nf6" },
            ["rnbqkb1r/ppp1pppp/5n2/8/2BP4/5N2/PP2PPPP/RNBQK2R"] = new Opening { ECO = "D26", Name = "Queen's Gambit Accepted: Classical", Moves = "1. d4 d5 2. c4 dxc4 3. Nf3 Nf6 4. e3 e6 5. Bxc4" },
            ["rnbqkbnr/pp2pppp/8/2pp4/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D10", Name = "Slav Defense", Moves = "1. d4 d5 2. c4 c6" },
            ["rnbqkbnr/pp2pppp/2p5/3p4/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D10", Name = "Slav Defense", Moves = "1. d4 d5 2. c4 c6" },
            ["rnbqkbnr/pp2pppp/2p5/3p4/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "D11", Name = "Slav Defense: 3.Nf3", Moves = "1. d4 d5 2. c4 c6 3. Nf3" },
            ["rnbqkb1r/pp2pppp/2p2n2/3p4/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "D11", Name = "Slav Defense: 3...Nf6", Moves = "1. d4 d5 2. c4 c6 3. Nf3 Nf6" },
            ["rnbqkb1r/pp2pppp/2p2n2/3p4/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D15", Name = "Slav Defense: Main Line", Moves = "1. d4 d5 2. c4 c6 3. Nf3 Nf6 4. Nc3" },
            ["rn1qkb1r/pp2pppp/2p2n2/3p1b2/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D15", Name = "Slav Defense: 4...Bf5", Moves = "1. d4 d5 2. c4 c6 3. Nf3 Nf6 4. Nc3 Bf5" },
            ["rnbqkb1r/pp2pppp/2p2n2/8/2pP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D15", Name = "Slav Defense: 4...dxc4", Moves = "1. d4 d5 2. c4 c6 3. Nf3 Nf6 4. Nc3 dxc4" },
            ["rnbqkb1r/pp3ppp/2p1pn2/3p4/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D45", Name = "Semi-Slav Defense", Moves = "1. d4 d5 2. c4 c6 3. Nf3 Nf6 4. Nc3 e6" },
            ["rnbqkb1r/pp3ppp/2p1pn2/3p4/2PP4/2N1PN2/PP3PPP/R1BQKB1R"] = new Opening { ECO = "D45", Name = "Semi-Slav Defense: 5.e3", Moves = "1. d4 d5 2. c4 c6 3. Nf3 Nf6 4. Nc3 e6 5. e3" },
            ["rn1qkb1r/pp3ppp/2p1pn2/3p1b2/2PP4/2N1PN2/PP3PPP/R1BQKB1R"] = new Opening { ECO = "D45", Name = "Semi-Slav Defense: Stoltz Variation", Moves = "1. d4 d5 2. c4 c6 3. Nf3 Nf6 4. Nc3 e6 5. e3 Nbd7 6. Bd3 Bd6" },
            ["rnbqkb1r/pp3ppp/2p1pn2/3p2B1/2PP4/2N2N2/PP2PPPP/R2QKB1R"] = new Opening { ECO = "D46", Name = "Semi-Slav Defense: Meran Variation", Moves = "1. d4 d5 2. c4 c6 3. Nf3 Nf6 4. Nc3 e6 5. Bg5" },

            // D70-D99 - Grunfeld Defense
            ["rnbqkb1r/ppp1pp1p/5np1/3p4/2PP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "D70", Name = "Grunfeld Defense", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 d5" },
            ["rnbqkb1r/ppp1pp1p/6p1/3n4/3P4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "D80", Name = "Grunfeld Defense", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 d5 4. cxd5 Nxd5" },

            // E00-E09 - Catalan Opening
            ["rnbqkb1r/pppp1ppp/4pn2/8/2PP4/6P1/PP2PP1P/RNBQKBNR"] = new Opening { ECO = "E00", Name = "Catalan Opening", Moves = "1. d4 Nf6 2. c4 e6 3. g3" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/2PP4/6P1/PP2PP1P/RNBQKBNR"] = new Opening { ECO = "E01", Name = "Catalan Opening", Moves = "1. d4 Nf6 2. c4 e6 3. g3 d5" },

            // E10-E19 - Queen's Indian Defense
            ["rnbqkb1r/p1pp1ppp/1p2pn2/8/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "E12", Name = "Queen's Indian Defense", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 b6" },

            // E20-E59 - Nimzo-Indian Defense
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "E20", Name = "Nimzo-Indian Defense", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4" },
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "E21", Name = "Nimzo-Indian Defense: Three Knights", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4 4. Nf3" },
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/2N5/PPQ1PPPP/R1B1KBNR"] = new Opening { ECO = "E32", Name = "Nimzo-Indian Defense: Classical Variation", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4 4. Qc2" },
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/2N1P3/PP3PPP/R1BQKBNR"] = new Opening { ECO = "E40", Name = "Nimzo-Indian Defense: Rubinstein Variation", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4 4. e3" },

            // E60-E99 - King's Indian Defense
            ["rnbqkb1r/pppppp1p/5np1/8/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "E60", Name = "King's Indian Defense", Moves = "1. d4 Nf6 2. c4 g6" },
            ["rnbqkb1r/pppppp1p/5np1/8/2PP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "E61", Name = "King's Indian Defense", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3" },
            ["rnbqkb1r/ppp1pp1p/5np1/3p4/2PP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "E61", Name = "King's Indian Defense: 3...d5", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 d5" },
            ["rnbqk2r/ppppppbp/5np1/8/2PP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "E62", Name = "King's Indian Defense: Fianchetto", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7" },
            ["rnbqk2r/ppp1ppbp/3p1np1/8/2PP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "E62", Name = "King's Indian Defense: 4...d6", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. Nf3 d6" },
            ["rnbqk2r/ppp1ppbp/3p1np1/8/2PPP3/2N5/PP3PPP/R1BQKBNR"] = new Opening { ECO = "E70", Name = "King's Indian Defense: Classical Variation", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6" },
            ["rnbqk2r/ppp1ppbp/3p1np1/8/2PPP3/2N2N2/PP3PPP/R1BQKB1R"] = new Opening { ECO = "E70", Name = "King's Indian Defense: Classical, 5.Nf3", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3" },
            ["rnbq1rk1/ppp1ppbp/3p1np1/8/2PPP3/2N2N2/PP3PPP/R1BQKB1R"] = new Opening { ECO = "E70", Name = "King's Indian Defense: Classical, 5...O-O", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O" },
            ["rnbq1rk1/ppp1ppbp/3p1np1/8/2PPP3/2N2N2/PP2BPPP/R1BQK2R"] = new Opening { ECO = "E90", Name = "King's Indian Defense: Classical, 6.Be2", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O 6. Be2" },
            ["rnbq1rk1/ppp2pbp/3p1np1/4p3/2PPP3/2N2N2/PP2BPPP/R1BQK2R"] = new Opening { ECO = "E92", Name = "King's Indian Defense: Classical, 6...e5", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O 6. Be2 e5" },
            ["rnbq1rk1/ppp2pbp/3p1np1/4p3/2PPP3/2N2N2/PP2BPPP/R1BQ1RK1"] = new Opening { ECO = "E92", Name = "King's Indian Defense: Classical, 7.O-O", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O 6. Be2 e5 7. O-O" },
            ["r1bq1rk1/pppn1pbp/3p1np1/4p3/2PPP3/2N2N2/PP2BPPP/R1BQ1RK1"] = new Opening { ECO = "E92", Name = "King's Indian Defense: Classical, Mar del Plata", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O 6. Be2 e5 7. O-O Nc6" },
            ["rnbq1rk1/ppp2pbp/3p2p1/4p3/2PPP1n1/2N2N2/PP2BPPP/R1BQ1RK1"] = new Opening { ECO = "E92", Name = "King's Indian Defense: Classical, Petrosian", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O 6. Be2 e5 7. O-O Nh5" },
            ["rnbq1rk1/ppp1ppbp/3p1np1/8/2PPP3/2N1BP2/PP4PP/R2QKBNR"] = new Opening { ECO = "E73", Name = "King's Indian Defense: Averbakh Variation", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Be2 O-O 6. Bg5" },
            ["rnbq1rk1/ppp1ppbp/3p1np1/6B1/2PPP3/2N5/PP2BPPP/R2QK1NR"] = new Opening { ECO = "E73", Name = "King's Indian Defense: Averbakh", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Be2 O-O 6. Bg5" },
            ["rnbq1rk1/ppp1ppbp/3p1np1/8/2PPP3/2N2P2/PP4PP/R1BQKBNR"] = new Opening { ECO = "E74", Name = "King's Indian Defense: Samisch Variation", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. f3" },
            ["rnbq1rk1/ppp2pbp/3p1np1/4p3/2PPP3/2N2P2/PP4PP/R1BQKBNR"] = new Opening { ECO = "E75", Name = "King's Indian Defense: Samisch, 5...O-O 6.f3", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. f3 O-O" },
            ["rnbq1rk1/ppp2pbp/3p1np1/4p3/2PPP3/2N1BP2/PP4PP/R2QKBNR"] = new Opening { ECO = "E81", Name = "King's Indian Defense: Samisch, 6.Be3", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. f3 O-O 6. Be3" },
            ["rnbq1rk1/pp3pbp/3ppnp1/2p5/2PPP3/2N1BP2/PP4PP/R2QKBNR"] = new Opening { ECO = "E81", Name = "King's Indian Defense: Samisch, Byrne Defense", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. f3 O-O 6. Be3 c5" },
            ["rnbq1rk1/ppp1ppbp/3p1np1/6B1/2PP4/2N2NP1/PP2PP1P/R2QKB1R"] = new Opening { ECO = "E62", Name = "King's Indian Defense: Fianchetto, Classical", Moves = "1. d4 Nf6 2. c4 g6 3. g3 Bg7 4. Bg2 O-O 5. Nc3 d6 6. Nf3" },
            ["rnbq1rk1/ppp2pbp/3p1np1/4p3/2PP4/2N2NP1/PP2PPBP/R1BQK2R"] = new Opening { ECO = "E62", Name = "King's Indian Defense: Fianchetto, Panno", Moves = "1. d4 Nf6 2. c4 g6 3. g3 Bg7 4. Bg2 O-O 5. Nc3 d6 6. Nf3 Nc6" },
            ["rnbq1rk1/pp2ppbp/3p1np1/2p5/2PP4/2N2NP1/PP2PPBP/R1BQK2R"] = new Opening { ECO = "E63", Name = "King's Indian Defense: Fianchetto, Panno Variation", Moves = "1. d4 Nf6 2. c4 g6 3. g3 Bg7 4. Bg2 O-O 5. Nc3 d6 6. Nf3 c6" },
            ["rnbq1rk1/pp3pbp/2pp1np1/4p3/2PPP3/2N2N2/PP2BPPP/R1BQK2R"] = new Opening { ECO = "E94", Name = "King's Indian Defense: Orthodox Variation", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O 6. Be2 e5 7. O-O c6" },
            ["r1bq1rk1/pppn1pbp/3p1np1/4p3/2PPP3/2N2N2/PP2BPPP/R1BQK2R"] = new Opening { ECO = "E94", Name = "King's Indian Defense: Orthodox, 7...Nbd7", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O 6. Be2 e5 7. O-O Nbd7" },
            ["rnbq1rk1/ppp2pbp/3p1np1/4p3/2PPP3/5NP1/PP2PPBP/RNBQK2R"] = new Opening { ECO = "E60", Name = "King's Indian Defense: Fianchetto, Yugoslav", Moves = "1. d4 Nf6 2. c4 g6 3. g3 Bg7 4. Bg2 O-O 5. Nf3 d6 6. O-O e5" },

            // London System
            ["rnbqkb1r/ppp1pppp/5n2/3p4/3P1B2/5N2/PPP1PPPP/RN1QKB1R"] = new Opening { ECO = "D00", Name = "London System", Moves = "1. d4 d5 2. Bf4" },
            ["rnbqkb1r/pppppppp/5n2/8/3P1B2/8/PPP1PPPP/RN1QKBNR"] = new Opening { ECO = "A45", Name = "London System", Moves = "1. d4 Nf6 2. Bf4" },
            ["rnbqkb1r/ppp1pppp/5n2/3p4/3P1B2/4P3/PPP2PPP/RN1QKBNR"] = new Opening { ECO = "D00", Name = "London System: 3.e3", Moves = "1. d4 d5 2. Bf4 Nf6 3. e3" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/3P1B2/4P3/PPP2PPP/RN1QKBNR"] = new Opening { ECO = "D00", Name = "London System: 3...e6", Moves = "1. d4 d5 2. Bf4 Nf6 3. e3 e6" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/3P1B2/4PN2/PPP2PPP/RN1QKB1R"] = new Opening { ECO = "D00", Name = "London System: 4.Nf3", Moves = "1. d4 d5 2. Bf4 Nf6 3. e3 e6 4. Nf3" },
            ["rnbqkb1r/pp3ppp/4pn2/2pp4/3P1B2/4PN2/PPP2PPP/RN1QKB1R"] = new Opening { ECO = "D00", Name = "London System: 4...c5", Moves = "1. d4 d5 2. Bf4 Nf6 3. e3 e6 4. Nf3 c5" },
            ["rn1qkb1r/ppp2ppp/4pn2/3p1b2/3P1B2/4PN2/PPP2PPP/RN1QKB1R"] = new Opening { ECO = "D00", Name = "London System: 4...Bf5", Moves = "1. d4 d5 2. Bf4 Nf6 3. e3 e6 4. Nf3 Bf5" },

            // Colle System
            ["rnbqkb1r/ppp1pppp/5n2/3p4/3P4/4PN2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "D05", Name = "Colle System", Moves = "1. d4 d5 2. Nf3 Nf6 3. e3" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/3P4/4PN2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "D05", Name = "Colle System: 3...e6", Moves = "1. d4 d5 2. Nf3 Nf6 3. e3 e6" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/3P4/3BPN2/PPP2PPP/RNBQK2R"] = new Opening { ECO = "D05", Name = "Colle System: 4.Bd3", Moves = "1. d4 d5 2. Nf3 Nf6 3. e3 e6 4. Bd3" },

            // Torre Attack
            ["rnbqkb1r/pppppppp/5n2/6B1/3P4/8/PPP1PPPP/RN1QKBNR"] = new Opening { ECO = "A46", Name = "Torre Attack", Moves = "1. d4 Nf6 2. Nf3 e6 3. Bg5" },
            ["rnbqkb1r/ppp1pppp/5n2/3p2B1/3P4/5N2/PPP1PPPP/RN1QKB1R"] = new Opening { ECO = "D03", Name = "Torre Attack", Moves = "1. d4 d5 2. Nf3 Nf6 3. Bg5" },

            // Veresov Attack
            ["rnbqkb1r/ppp1pppp/5n2/3p4/3P4/2N5/PPP1PPPP/R1BQKBNR"] = new Opening { ECO = "D01", Name = "Veresov Attack", Moves = "1. d4 d5 2. Nc3" },
            ["rnbqkb1r/ppp1pppp/5n2/3p2B1/3P4/2N5/PPP1PPPP/R2QKBNR"] = new Opening { ECO = "D01", Name = "Veresov Attack: 3.Bg5", Moves = "1. d4 d5 2. Nc3 Nf6 3. Bg5" },

            // Blackmar-Diemer Gambit
            ["rnbqkbnr/ppp1pppp/8/3p4/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "D00", Name = "Blackmar-Diemer Gambit", Moves = "1. d4 d5 2. e4" },
            ["rnbqkbnr/ppp1pppp/8/8/3Pp3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "D00", Name = "Blackmar-Diemer Gambit: 2...dxe4", Moves = "1. d4 d5 2. e4 dxe4" },
            ["rnbqkbnr/ppp1pppp/8/8/3Pp3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "D00", Name = "Blackmar-Diemer Gambit: 3.Nc3", Moves = "1. d4 d5 2. e4 dxe4 3. Nc3" },

            // Benoni Defense
            ["rnbqkb1r/pp1ppppp/5n2/2p5/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A56", Name = "Benoni Defense", Moves = "1. d4 Nf6 2. c4 c5" },
            ["rnbqkb1r/pp1ppppp/5n2/2pP4/8/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A56", Name = "Benoni Defense: 3.d5", Moves = "1. d4 Nf6 2. c4 c5 3. d5" },
            ["rnbqkb1r/pp1p1ppp/4pn2/2pP4/8/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A60", Name = "Modern Benoni", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e6" },
            ["rnbqkb1r/pp1p1ppp/5n2/2pPp3/8/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A61", Name = "Modern Benoni: 4.Nc3", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e6 4. Nc3 exd5" },
            ["rnbqkb1r/pp1p1ppp/5n2/2pP4/4P3/8/PP3PPP/RNBQKBNR"] = new Opening { ECO = "A62", Name = "Modern Benoni: Fianchetto", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e6 4. Nc3 exd5 5. cxd5 d6" },
            ["rnbqkb1r/pp3ppp/3p1n2/2pP4/4P3/2N5/PP3PPP/R1BQKBNR"] = new Opening { ECO = "A65", Name = "Modern Benoni: 6.e4", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e6 4. Nc3 exd5 5. cxd5 d6 6. e4" },
            ["rnbqk2r/pp3pbp/3p1np1/2pP4/4P3/2N5/PP3PPP/R1BQKBNR"] = new Opening { ECO = "A67", Name = "Modern Benoni: Taimanov Attack", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e6 4. Nc3 exd5 5. cxd5 d6 6. e4 g6 7. f4" },

            // Benko Gambit (more variations)
            ["rnbqkb1r/p2ppppp/5n2/1ppP4/8/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A57", Name = "Benko Gambit", Moves = "1. d4 Nf6 2. c4 c5 3. d5 b5" },
            ["rnbqkb1r/p2ppppp/5n2/1PpP4/8/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A57", Name = "Benko Gambit Accepted", Moves = "1. d4 Nf6 2. c4 c5 3. d5 b5 4. cxb5" },
            ["rnbqkb1r/3ppppp/p4n2/1PpP4/8/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A58", Name = "Benko Gambit: 4...a6", Moves = "1. d4 Nf6 2. c4 c5 3. d5 b5 4. cxb5 a6" },
            ["rnbqkb1r/3ppppp/P4n2/2pP4/8/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A59", Name = "Benko Gambit: 5.bxa6", Moves = "1. d4 Nf6 2. c4 c5 3. d5 b5 4. cxb5 a6 5. bxa6" },

            // Dutch Defense (more variations)
            ["rnbqkbnr/ppppp1pp/8/5p2/3P4/5N2/PPP1PPPP/RNBQKB1R"] = new Opening { ECO = "A80", Name = "Dutch Defense: 2.Nf3", Moves = "1. d4 f5 2. Nf3" },
            ["rnbqkbnr/pppp2pp/4p3/5p2/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A83", Name = "Dutch Defense: Stonewall", Moves = "1. d4 f5 2. c4 e6" },
            ["rnbqkbnr/pppp2pp/4p3/5p2/2PP4/6P1/PP2PP1P/RNBQKBNR"] = new Opening { ECO = "A81", Name = "Dutch Defense: Leningrad", Moves = "1. d4 f5 2. c4 Nf6 3. g3" },
            ["rnbqk2r/pppp2bp/4pnp1/5p2/2PP4/6P1/PP2PPBP/RNBQK1NR"] = new Opening { ECO = "A87", Name = "Dutch Defense: Leningrad, Main Line", Moves = "1. d4 f5 2. c4 Nf6 3. g3 g6 4. Bg2 Bg7" },
            ["rnbqkb1r/pppp2pp/4pn2/5p2/2PP4/6P1/PP2PP1P/RNBQKBNR"] = new Opening { ECO = "A90", Name = "Dutch Defense: Classical", Moves = "1. d4 f5 2. c4 Nf6 3. g3 e6 4. Bg2" },

            // English Attack variations for Sicilian (more depth)
            ["rnbqkb1r/1p2pppp/p2p1n2/8/3NP3/2N1B3/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B90", Name = "Sicilian Defense: Najdorf, English Attack", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6 6. Be3" },
            ["rnbqkb1r/4pppp/p2p1n2/1p6/3NP3/2N1B3/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B90", Name = "Sicilian Defense: Najdorf, English Attack, 6...b5", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6 6. Be3 e5" },
            ["rnbqkb1r/1p3ppp/p2ppn2/8/3NP3/2N1B3/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B90", Name = "Sicilian Defense: Najdorf, English Attack, 6...e6", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6 6. Be3 e6" },

            // Scotch Game (more depth)
            ["r1bqkbnr/pppp1ppp/2n5/8/3NP3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C45", Name = "Scotch Game: 4.Nxd4", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4 exd4 4. Nxd4" },
            ["r1bqkb1r/pppp1ppp/2n2n2/8/3NP3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C45", Name = "Scotch Game: 4...Nf6", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4 exd4 4. Nxd4 Nf6" },
            ["r1bqkb1r/pppp1ppp/2n2n2/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "C45", Name = "Scotch Game: 5.Nc3", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4 exd4 4. Nxd4 Nf6 5. Nc3" },
            ["r1bqkb1r/pppp1ppp/2N2n2/8/4P3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C45", Name = "Scotch Game: 5.Nxc6", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4 exd4 4. Nxd4 Nf6 5. Nxc6" },
            ["r1bqkb1r/pppp1ppp/2n5/4n3/2B1P3/5N2/PPP2PPP/RNBQK2R"] = new Opening { ECO = "C44", Name = "Scotch Gambit", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4 exd4 4. Bc4" },

            // Four Knights Game
            ["r1bqkb1r/pppp1ppp/2n2n2/4p3/4P3/2N2N2/PPPP1PPP/R1BQKB1R"] = new Opening { ECO = "C46", Name = "Four Knights Game", Moves = "1. e4 e5 2. Nf3 Nc6 3. Nc3 Nf6" },
            ["r1bqkb1r/pppp1ppp/2n2n2/1B2p3/4P3/2N2N2/PPPP1PPP/R1BQK2R"] = new Opening { ECO = "C48", Name = "Four Knights Game: Spanish Variation", Moves = "1. e4 e5 2. Nf3 Nc6 3. Nc3 Nf6 4. Bb5" },
            ["r1bqk2r/pppp1ppp/2n2n2/1Bb1p3/4P3/2N2N2/PPPP1PPP/R1BQK2R"] = new Opening { ECO = "C49", Name = "Four Knights Game: Double Spanish", Moves = "1. e4 e5 2. Nf3 Nc6 3. Nc3 Nf6 4. Bb5 Bb4" },
            ["r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/2N2N2/PPPP1PPP/R1BQK2R"] = new Opening { ECO = "C47", Name = "Four Knights Game: Italian Variation", Moves = "1. e4 e5 2. Nf3 Nc6 3. Nc3 Nf6 4. Bc4" },

            // Philidor Defense
            ["rnbqkbnr/ppp2ppp/3p4/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C41", Name = "Philidor Defense", Moves = "1. e4 e5 2. Nf3 d6" },
            ["rnbqkbnr/ppp2ppp/3p4/4p3/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C41", Name = "Philidor Defense: 3.d4", Moves = "1. e4 e5 2. Nf3 d6 3. d4" },
            ["rnbqkbnr/ppp2ppp/3p4/8/3pP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C41", Name = "Philidor Defense: Exchange Variation", Moves = "1. e4 e5 2. Nf3 d6 3. d4 exd4" },
            ["rnbqkb1r/ppp2ppp/3p1n2/4p3/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C41", Name = "Philidor Defense: 3...Nf6", Moves = "1. e4 e5 2. Nf3 d6 3. d4 Nf6" },

            // King's Gambit (more depth)
            ["rnbqkbnr/pppp1ppp/8/8/4Pp2/5N2/PPPP2PP/RNBQKB1R"] = new Opening { ECO = "C33", Name = "King's Gambit Accepted: 3.Nf3", Moves = "1. e4 e5 2. f4 exf4 3. Nf3" },
            ["rnbqkbnr/pppp1ppp/8/8/2B1Pp2/8/PPPP2PP/RNBQK1NR"] = new Opening { ECO = "C33", Name = "King's Gambit Accepted: Bishop's Gambit", Moves = "1. e4 e5 2. f4 exf4 3. Bc4" },
            ["rnbqkbnr/pppp1p1p/8/6p1/4Pp2/5N2/PPPP2PP/RNBQKB1R"] = new Opening { ECO = "C37", Name = "King's Gambit Accepted: 3...g5", Moves = "1. e4 e5 2. f4 exf4 3. Nf3 g5" },
            ["rnbqkbnr/pppp1ppp/8/4p3/4PP2/8/PPPP2PP/RNBQKBNR"] = new Opening { ECO = "C30", Name = "King's Gambit Declined", Moves = "1. e4 e5 2. f4 Bc5" },
            ["rnbqk1nr/pppp1ppp/8/2b1p3/4PP2/8/PPPP2PP/RNBQKBNR"] = new Opening { ECO = "C30", Name = "King's Gambit Declined: Classical", Moves = "1. e4 e5 2. f4 Bc5" },

            // Vienna Game (more depth)
            ["rnbqkb1r/pppp1ppp/5n2/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "C26", Name = "Vienna Game: 2...Nf6", Moves = "1. e4 e5 2. Nc3 Nf6" },
            ["rnbqkb1r/pppp1ppp/5n2/4p3/4PP2/2N5/PPPP2PP/R1BQKBNR"] = new Opening { ECO = "C29", Name = "Vienna Game: 3.f4", Moves = "1. e4 e5 2. Nc3 Nf6 3. f4" },
            ["rnbqkb1r/pppp1ppp/5n2/4p3/2B1P3/2N5/PPPP1PPP/R1BQK1NR"] = new Opening { ECO = "C27", Name = "Vienna Game: 3.Bc4", Moves = "1. e4 e5 2. Nc3 Nf6 3. Bc4" },
            ["r1bqkbnr/pppp1ppp/2n5/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "C25", Name = "Vienna Game: 2...Nc6", Moves = "1. e4 e5 2. Nc3 Nc6" },

            // Petrov Defense (more depth)
            ["rnbqkb1r/pppp1ppp/5n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "C42", Name = "Petrov Defense: Italian Variation", Moves = "1. e4 e5 2. Nf3 Nf6 3. Bc4" },
            ["rnbqkb1r/pppp1ppp/8/4n3/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense: 3.Nxe5 d6", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5 d6" },
            ["rnbqkb1r/ppp2ppp/3p4/4n3/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense: 4.Nf3", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5 d6 4. Nf3" },
            ["rnbqkb1r/ppp2ppp/3p1n2/8/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C43", Name = "Petrov Defense: 4...Nxe4", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5 d6 4. Nf3 Nxe4" },
            ["rnbqkb1r/ppp2ppp/3p1n2/8/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C43", Name = "Petrov Defense: Steinitz Attack", Moves = "1. e4 e5 2. Nf3 Nf6 3. d4" },

            // Center Game (more depth)
            ["rnbqkbnr/pppp1ppp/8/8/3QP3/8/PPP2PPP/RNB1KBNR"] = new Opening { ECO = "C22", Name = "Center Game: 3.Qxd4", Moves = "1. e4 e5 2. d4 exd4 3. Qxd4" },
            ["r1bqkbnr/pppp1ppp/2n5/8/3QP3/8/PPP2PPP/RNB1KBNR"] = new Opening { ECO = "C22", Name = "Center Game: 3...Nc6", Moves = "1. e4 e5 2. d4 exd4 3. Qxd4 Nc6" },
            ["r1bqkbnr/pppp1ppp/2n5/8/4P3/3Q4/PPP2PPP/RNB1KBNR"] = new Opening { ECO = "C22", Name = "Center Game: 4.Qe3", Moves = "1. e4 e5 2. d4 exd4 3. Qxd4 Nc6 4. Qe3" },

            // Danish Gambit
            ["rnbqkbnr/pppp1ppp/8/8/3pP3/2P5/PP3PPP/RNBQKBNR"] = new Opening { ECO = "C21", Name = "Danish Gambit", Moves = "1. e4 e5 2. d4 exd4 3. c3" },
            ["rnbqkbnr/pppp1ppp/8/8/2B1P3/8/PP3PPP/RNBQK1NR"] = new Opening { ECO = "C21", Name = "Danish Gambit Accepted", Moves = "1. e4 e5 2. d4 exd4 3. c3 dxc3 4. Bc4" },

            // Ponziani Opening
            ["r1bqkbnr/pppp1ppp/2n5/4p3/4P3/2P2N2/PP1P1PPP/RNBQKB1R"] = new Opening { ECO = "C44", Name = "Ponziani Opening", Moves = "1. e4 e5 2. Nf3 Nc6 3. c3" },

            // Evans Gambit
            ["r1bqk1nr/pppp1ppp/2n5/2b1p3/1PB1P3/5N2/P1PP1PPP/RNBQK2R"] = new Opening { ECO = "C51", Name = "Evans Gambit", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. b4" },
            ["r1bqk1nr/pppp1ppp/2n5/4p3/1bB1P3/5N2/P1PP1PPP/RNBQK2R"] = new Opening { ECO = "C51", Name = "Evans Gambit Accepted", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. b4 Bxb4" },
            ["r1bqk1nr/pppp1ppp/2n5/4p3/1bB1P3/2P2N2/P2P1PPP/RNBQK2R"] = new Opening { ECO = "C52", Name = "Evans Gambit: 5.c3", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. b4 Bxb4 5. c3" },

            // Grunfeld Defense (more depth)
            ["rnbqkb1r/ppp1pp1p/5np1/3p4/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "D70", Name = "Grunfeld Defense: 3.Nf3", Moves = "1. d4 Nf6 2. c4 g6 3. Nf3 d5" },
            ["rnbqkb1r/ppp1pppp/5n2/3p4/2PP4/6P1/PP2PP1P/RNBQKBNR"] = new Opening { ECO = "D70", Name = "Grunfeld Defense: Fianchetto", Moves = "1. d4 Nf6 2. c4 g6 3. g3 d5" },
            ["rnbqk2r/ppp1ppbp/5np1/3p4/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D85", Name = "Grunfeld Defense: Exchange Variation", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 d5 4. cxd5 Nxd5 5. e4 Nxc3 6. bxc3 Bg7" },
            ["rnbqk2r/ppp1ppbp/6p1/3n4/3PP3/2N5/PP3PPP/R1BQKBNR"] = new Opening { ECO = "D85", Name = "Grunfeld Defense: Exchange, 5.e4", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 d5 4. cxd5 Nxd5 5. e4" },

            // Nimzo-Indian Defense (more depth)
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "E20", Name = "Nimzo-Indian Defense", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4" },
            ["rnbqk2r/pp1p1ppp/4pn2/2p5/1bPP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "E20", Name = "Nimzo-Indian Defense: 4...c5", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4 4. a3 Bxc3+ 5. bxc3 c5" },
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/P1N5/1P2PPPP/R1BQKBNR"] = new Opening { ECO = "E24", Name = "Nimzo-Indian Defense: Samisch Variation", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4 4. a3" },
            ["rnbq1rk1/pppp1ppp/4pn2/8/1bPP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "E44", Name = "Nimzo-Indian Defense: Fischer Variation", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4 4. e3 O-O 5. Nf3" },

            // Queen's Indian Defense (more depth)
            ["rnbqkb1r/p1pp1ppp/1p2pn2/8/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "E15", Name = "Queen's Indian Defense", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 b6" },
            ["rn1qkb1r/pbpp1ppp/1p2pn2/8/2PP4/5NP1/PP2PP1P/RNBQKB1R"] = new Opening { ECO = "E15", Name = "Queen's Indian Defense: Fianchetto", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 b6 4. g3 Bb7" },
            ["rn1qkb1r/pbpp1ppp/1p2pn2/8/2PP4/5NP1/PP2PPBP/RNBQK2R"] = new Opening { ECO = "E17", Name = "Queen's Indian Defense: 5.Bg2", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 b6 4. g3 Bb7 5. Bg2" },
            ["rn1qk2r/pbppbppp/1p2pn2/8/2PP4/5NP1/PP2PPBP/RNBQK2R"] = new Opening { ECO = "E17", Name = "Queen's Indian Defense: Classical", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 b6 4. g3 Bb7 5. Bg2 Be7" },

            // Catalan Opening (more depth)
            ["rnbqkb1r/pppp1ppp/4pn2/8/2PP4/6P1/PP2PP1P/RNBQKBNR"] = new Opening { ECO = "E01", Name = "Catalan Opening", Moves = "1. d4 Nf6 2. c4 e6 3. g3" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/2PP4/6P1/PP2PP1P/RNBQKBNR"] = new Opening { ECO = "E04", Name = "Catalan Opening: Open", Moves = "1. d4 Nf6 2. c4 e6 3. g3 d5" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/2PP4/6P1/PP2PPBP/RNBQK1NR"] = new Opening { ECO = "E04", Name = "Catalan Opening: 4.Bg2", Moves = "1. d4 Nf6 2. c4 e6 3. g3 d5 4. Bg2" },
            ["rnbqkb1r/ppp2ppp/4pn2/8/2pP4/6P1/PP2PPBP/RNBQK1NR"] = new Opening { ECO = "E04", Name = "Catalan Opening: 4...dxc4", Moves = "1. d4 Nf6 2. c4 e6 3. g3 d5 4. Bg2 dxc4" },
            ["rnbqkb1r/ppp2ppp/4pn2/8/2pP4/5NP1/PP2PPBP/RNBQK2R"] = new Opening { ECO = "E05", Name = "Catalan Opening: 5.Nf3", Moves = "1. d4 Nf6 2. c4 e6 3. g3 d5 4. Bg2 dxc4 5. Nf3" },
            ["rnbqk2r/ppp1bppp/4pn2/8/2pP4/5NP1/PP2PPBP/RNBQK2R"] = new Opening { ECO = "E06", Name = "Catalan Opening: Closed", Moves = "1. d4 Nf6 2. c4 e6 3. g3 d5 4. Bg2 Be7" },

            // Reti Opening (more depth)
            ["rnbqkbnr/ppp1pppp/8/3p4/8/5NP1/PPPPPP1P/RNBQKB1R"] = new Opening { ECO = "A05", Name = "Reti Opening: 2.g3", Moves = "1. Nf3 d5 2. g3" },
            ["rnbqkbnr/ppp1pppp/8/3p4/8/5N2/PPPPPPPP/RNBQKB1R"] = new Opening { ECO = "A06", Name = "Reti Opening: 1...d5", Moves = "1. Nf3 d5" },
            ["rnbqkbnr/pp2pppp/8/2pp4/8/5NP1/PPPPPP1P/RNBQKB1R"] = new Opening { ECO = "A06", Name = "Reti Opening: 2...c5", Moves = "1. Nf3 d5 2. g3 c5" },
            ["rnbqkb1r/ppp1pppp/5n2/3p4/8/5NP1/PPPPPP1P/RNBQKB1R"] = new Opening { ECO = "A07", Name = "Reti Opening: King's Indian Attack", Moves = "1. Nf3 d5 2. g3 Nf6" },
            ["rnbqkb1r/ppp1pppp/5n2/3p4/8/5NP1/PPPPPPBP/RNBQK2R"] = new Opening { ECO = "A07", Name = "Reti Opening: KIA, 3.Bg2", Moves = "1. Nf3 d5 2. g3 Nf6 3. Bg2" },

            // English Opening (more depth)
            ["rnbqkbnr/pppp1ppp/8/4p3/2P5/5N2/PP1PPPPP/RNBQKB1R"] = new Opening { ECO = "A21", Name = "English Opening: Reversed Sicilian", Moves = "1. c4 e5 2. Nc3" },
            ["rnbqkbnr/pppp1ppp/8/4p3/2P5/2N5/PP1PPPPP/R1BQKBNR"] = new Opening { ECO = "A25", Name = "English Opening: Closed", Moves = "1. c4 e5 2. Nc3 Nc6" },
            ["r1bqkbnr/pppp1ppp/2n5/4p3/2P5/2N2N2/PP1PPPPP/R1BQKB1R"] = new Opening { ECO = "A28", Name = "English Opening: Four Knights", Moves = "1. c4 e5 2. Nc3 Nc6 3. Nf3 Nf6" },
            ["rnbqkbnr/ppp2ppp/8/3pp3/2P5/2N5/PP1PPPPP/R1BQKBNR"] = new Opening { ECO = "A29", Name = "English Opening: 3...d5", Moves = "1. c4 e5 2. Nc3 Nf6 3. Nf3 Nc6 4. d4" },
            ["rnbqkbnr/pp1ppppp/8/2p5/2P5/2N5/PP1PPPPP/R1BQKBNR"] = new Opening { ECO = "A30", Name = "English Opening: Symmetrical", Moves = "1. c4 c5 2. Nc3" },
            ["r1bqkbnr/pp1ppppp/2n5/2p5/2P5/2N5/PP1PPPPP/R1BQKBNR"] = new Opening { ECO = "A33", Name = "English Opening: Symmetrical, Two Knights", Moves = "1. c4 c5 2. Nc3 Nc6" },
            ["rnbqkb1r/pp1ppppp/5n2/2p5/2P5/2N5/PP1PPPPP/R1BQKBNR"] = new Opening { ECO = "A36", Name = "English Opening: 2...Nf6", Moves = "1. c4 c5 2. Nc3 Nf6" },

            // Budapest Gambit
            ["rnbqkb1r/pppp1ppp/5n2/4p3/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A51", Name = "Budapest Gambit", Moves = "1. d4 Nf6 2. c4 e5" },
            ["rnbqkb1r/pppp1ppp/5n2/4P3/2P5/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A51", Name = "Budapest Gambit: 3.dxe5", Moves = "1. d4 Nf6 2. c4 e5 3. dxe5" },
            ["rnbqkb1r/pppp1ppp/8/4P3/2Pn4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A52", Name = "Budapest Gambit: 3...Ng4", Moves = "1. d4 Nf6 2. c4 e5 3. dxe5 Ng4" },
            ["rnbqkb1r/pppp1ppp/8/4P3/2Pn4/4P3/PP3PPP/RNBQKBNR"] = new Opening { ECO = "A52", Name = "Budapest Gambit: Adler Variation", Moves = "1. d4 Nf6 2. c4 e5 3. dxe5 Ng4 4. e4" },
            ["rnbqkb1r/pppp1ppp/8/4P3/2Pn4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "A52", Name = "Budapest Gambit: Rubinstein Variation", Moves = "1. d4 Nf6 2. c4 e5 3. dxe5 Ng4 4. Nf3" },

            // Old Indian Defense
            ["rnbqkb1r/ppp1pppp/3p1n2/8/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A53", Name = "Old Indian Defense", Moves = "1. d4 Nf6 2. c4 d6" },
            ["rnbqkb1r/ppp1pppp/3p1n2/8/2PP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "A53", Name = "Old Indian Defense: 3.Nc3", Moves = "1. d4 Nf6 2. c4 d6 3. Nc3" },
            ["rnbqkb1r/ppp2ppp/3p1n2/4p3/2PP4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "A54", Name = "Old Indian Defense: 3...e5", Moves = "1. d4 Nf6 2. c4 d6 3. Nc3 e5" },
            ["rnbqkb1r/ppp2ppp/3p1n2/4p3/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "A55", Name = "Old Indian Defense: Main Line", Moves = "1. d4 Nf6 2. c4 d6 3. Nc3 e5 4. Nf3" },

            // Owen's Defense
            ["rnbqkbnr/p1pppppp/1p6/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B00", Name = "Owen's Defense", Moves = "1. e4 b6" },
            ["rnbqkbnr/p1pppppp/1p6/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B00", Name = "Owen's Defense: 2.d4", Moves = "1. e4 b6 2. d4" },
            ["rn1qkbnr/pbpppppp/1p6/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B00", Name = "Owen's Defense: 2...Bb7", Moves = "1. e4 b6 2. d4 Bb7" },

            // St. George Defense
            ["rnbqkbnr/1ppppppp/p7/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "B00", Name = "St. George Defense", Moves = "1. e4 a6" },
            ["rnbqkbnr/1ppppppp/p7/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B00", Name = "St. George Defense: 2.d4", Moves = "1. e4 a6 2. d4" },
            ["rnbqkbnr/2pppppp/pp6/8/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B00", Name = "St. George Defense: 2...b6", Moves = "1. e4 a6 2. d4 b6" },

            // Hippopotamus Defense
            ["rn1qkbnr/pbpppppp/1p6/8/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B00", Name = "Hippopotamus Defense", Moves = "1. e4 b6 2. d4 Bb7 3. Nc3" },
            ["rn1qkbnr/pbpp1ppp/1p2p3/8/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B00", Name = "Hippopotamus Defense: 3...e6", Moves = "1. e4 b6 2. d4 Bb7 3. Nc3 e6" },

            // Czech Defense (vs 1.e4)
            ["rnbqkbnr/pp1ppppp/8/2p5/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "B20", Name = "Sicilian Defense: Closed", Moves = "1. e4 c5 2. Nc3" },
            ["rnbqkbnr/pp1p1ppp/4p3/2p5/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "B23", Name = "Sicilian Defense: Closed, 2...e6", Moves = "1. e4 c5 2. Nc3 e6" },
            ["r1bqkbnr/pp1ppppp/2n5/2p5/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "B23", Name = "Sicilian Defense: Closed, 2...Nc6", Moves = "1. e4 c5 2. Nc3 Nc6" },
            ["r1bqkbnr/pp1ppppp/2n5/2p5/4PP2/2N5/PPPP2PP/R1BQKBNR"] = new Opening { ECO = "B23", Name = "Sicilian Defense: Grand Prix Attack", Moves = "1. e4 c5 2. Nc3 Nc6 3. f4" },

            // Sicilian Sveshnikov (more depth)
            ["r1bqkb1r/pp1p1ppp/2n1pn2/2p5/4P3/2N2N2/PPPP1PPP/R1BQKB1R"] = new Opening { ECO = "B30", Name = "Sicilian Defense: Sveshnikov", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e5" },
            ["r1bqkb1r/pp1p1ppp/2n1pn2/4N3/4P3/2N5/PPPP1PPP/R1BQKB1R"] = new Opening { ECO = "B33", Name = "Sicilian Defense: Sveshnikov, 6.Ndb5", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e5 6. Ndb5" },
            ["r1bqkb1r/pp3ppp/2nppn2/1N6/4P3/2N5/PPPP1PPP/R1BQKB1R"] = new Opening { ECO = "B33", Name = "Sicilian Defense: Sveshnikov, 6...d6", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e5 6. Ndb5 d6" },
            ["r1bqkb1r/pp3ppp/2nppn2/1N4B1/4P3/2N5/PPPP1PPP/R2QKB1R"] = new Opening { ECO = "B33", Name = "Sicilian Defense: Sveshnikov, 7.Bg5", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e5 6. Ndb5 d6 7. Bg5" },
            ["r1bqkb1r/1p3ppp/p1nppn2/1N4B1/4P3/2N5/PPPP1PPP/R2QKB1R"] = new Opening { ECO = "B33", Name = "Sicilian Defense: Sveshnikov, 7...a6", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e5 6. Ndb5 d6 7. Bg5 a6" },
            ["r1bqkb1r/1p3ppp/p1nppn2/6B1/4P3/N1N5/PPPP1PPP/R2QKB1R"] = new Opening { ECO = "B33", Name = "Sicilian Defense: Sveshnikov, 8.Na3", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e5 6. Ndb5 d6 7. Bg5 a6 8. Na3" },

            // Sicilian Accelerated Dragon (more depth)
            ["r1bqkbnr/pp1ppp1p/2n3p1/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B27", Name = "Sicilian Defense: Accelerated Dragon", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 g6" },
            ["r1bqkbnr/pp1ppp1p/2n3p1/8/3NP3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B34", Name = "Sicilian Defense: Accelerated Dragon, 5.Nc3", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 g6 5. Nc3" },
            ["r1bqk1nr/pp1pppbp/2n3p1/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B35", Name = "Sicilian Defense: Accelerated Dragon, Modern Bc4", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 g6 5. Nc3 Bg7" },
            ["r1bqk1nr/pp1pppbp/2n3p1/8/2BNP3/2N5/PPP2PPP/R1BQK2R"] = new Opening { ECO = "B35", Name = "Sicilian Defense: Accelerated Dragon, 6.Bc4", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 g6 5. Nc3 Bg7 6. Bc4" },
            ["r1bqk1nr/pp1pppbp/2n3p1/8/3NP3/2N1B3/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B36", Name = "Sicilian Defense: Accelerated Dragon, Maroczy Bind", Moves = "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 g6 5. c4" },

            // Sicilian Taimanov (more depth)
            ["rnbqkbnr/pp1p1ppp/4p3/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B40", Name = "Sicilian Defense: Taimanov", Moves = "1. e4 c5 2. Nf3 e6" },
            ["rnbqkbnr/pp1p1ppp/4p3/8/3pP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B44", Name = "Sicilian Defense: Taimanov, 3.d4", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4" },
            ["r1bqkbnr/pp1p1ppp/2n1p3/8/3NP3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B46", Name = "Sicilian Defense: Taimanov, 4...Nc6", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 Nc6" },
            ["r1bqkbnr/pp1p1ppp/2n1p3/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B46", Name = "Sicilian Defense: Taimanov, 5.Nc3", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 Nc6 5. Nc3" },
            ["r1bqkbnr/1p1p1ppp/p1n1p3/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B46", Name = "Sicilian Defense: Taimanov, 5...a6", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 Nc6 5. Nc3 a6" },
            ["r1bqk1nr/1p1p1ppp/p1n1p3/8/1b1NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B47", Name = "Sicilian Defense: Taimanov, Bastrikov Variation", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 Nc6 5. Nc3 Qc7" },

            // Sicilian Kan (more depth)
            ["rnbqkbnr/1p1p1ppp/p3p3/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "B41", Name = "Sicilian Defense: Kan Variation", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 a6" },
            ["rnbqkbnr/1p1p1ppp/p3p3/8/3NP3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B42", Name = "Sicilian Defense: Kan, 5.Bd3", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 a6 5. Bd3" },
            ["rnbqkbnr/1p1p1ppp/p3p3/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B43", Name = "Sicilian Defense: Kan, 5.Nc3", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 a6 5. Nc3" },

            // French Defense Winawer (more depth)
            ["rnbqk1nr/ppp2ppp/4p3/3pP3/1b1P4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C15", Name = "French Defense: Winawer Variation", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4" },
            ["rnbqk1nr/ppp2ppp/4p3/3pP3/1b1P4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C16", Name = "French Defense: Winawer, 4.e5", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5" },
            ["rnbqk1nr/pp3ppp/4p3/2ppP3/1b1P4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C17", Name = "French Defense: Winawer, 4...c5", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5 c5" },
            ["rnbqk1nr/pp3ppp/4p3/2ppP3/1b1P4/P1N5/1PP2PPP/R1BQKBNR"] = new Opening { ECO = "C18", Name = "French Defense: Winawer, 5.a3", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5 c5 5. a3" },
            ["rnbqk1nr/pp3ppp/4p3/2ppP3/3P4/P1b5/1PP2PPP/R1BQKBNR"] = new Opening { ECO = "C18", Name = "French Defense: Winawer, 5...Bxc3+", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5 c5 5. a3 Bxc3+" },
            ["rnbqk1nr/pp3ppp/4p3/2ppP3/3P4/P1P5/2P2PPP/R1BQKBNR"] = new Opening { ECO = "C18", Name = "French Defense: Winawer, 6.bxc3", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5 c5 5. a3 Bxc3+ 6. bxc3" },
            ["rnbqk2r/pp2nppp/4p3/2ppP3/3P4/P1P5/2P2PPP/R1BQKBNR"] = new Opening { ECO = "C18", Name = "French Defense: Winawer, 6...Ne7", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5 c5 5. a3 Bxc3+ 6. bxc3 Ne7" },
            ["rnbqk1nr/pp3ppp/4p3/q1ppP3/3P4/P1P5/2P2PPP/R1BQKBNR"] = new Opening { ECO = "C18", Name = "French Defense: Winawer, Poisoned Pawn", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Bb4 4. e5 c5 5. a3 Bxc3+ 6. bxc3 Qc7" },

            // French Defense Rubinstein (more depth)
            ["rnbqkbnr/ppp2ppp/4p3/8/3Pp3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C10", Name = "French Defense: Rubinstein Variation", Moves = "1. e4 e6 2. d4 d5 3. Nc3 dxe4" },
            ["rnbqkbnr/ppp2ppp/4p3/8/3PN3/8/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C10", Name = "French Defense: Rubinstein, 4.Nxe4", Moves = "1. e4 e6 2. d4 d5 3. Nc3 dxe4 4. Nxe4" },
            ["rnbqkb1r/ppp2ppp/4pn2/8/3PN3/8/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C10", Name = "French Defense: Rubinstein, 4...Nf6", Moves = "1. e4 e6 2. d4 d5 3. Nc3 dxe4 4. Nxe4 Nf6" },

            // French Defense Classical (more depth)
            ["rnbqkb1r/ppp2ppp/4pn2/3p4/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "C11", Name = "French Defense: Classical Variation", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Nf6" },
            ["rnbqkb1r/ppp2ppp/4pn2/3p2B1/3PP3/2N5/PPP2PPP/R2QKBNR"] = new Opening { ECO = "C11", Name = "French Defense: Classical, 4.Bg5", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Nf6 4. Bg5" },
            ["rnbqk2r/ppp1bppp/4pn2/3p2B1/3PP3/2N5/PPP2PPP/R2QKBNR"] = new Opening { ECO = "C13", Name = "French Defense: Classical, 4...Be7", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Nf6 4. Bg5 Be7" },
            ["rnbqk2r/ppp1bppp/4pn2/3pP1B1/3P4/2N5/PPP2PPP/R2QKBNR"] = new Opening { ECO = "C14", Name = "French Defense: Classical, 5.e5", Moves = "1. e4 e6 2. d4 d5 3. Nc3 Nf6 4. Bg5 Be7 5. e5" },

            // King's Indian Attack
            ["rnbqkbnr/ppp1pppp/8/3p4/8/5NP1/PPPPPP1P/RNBQKB1R"] = new Opening { ECO = "A07", Name = "King's Indian Attack", Moves = "1. Nf3 d5 2. g3" },
            ["rnbqkbnr/ppp1pppp/8/3p4/8/5NP1/PPPPPPBP/RNBQK2R"] = new Opening { ECO = "A07", Name = "King's Indian Attack: 3.Bg2", Moves = "1. Nf3 d5 2. g3 Nf6 3. Bg2" },
            ["rnbqkb1r/ppp1pppp/5n2/3p4/8/5NP1/PPPPPPBP/RNBQK2R"] = new Opening { ECO = "A07", Name = "King's Indian Attack: 3...Nf6", Moves = "1. Nf3 d5 2. g3 Nf6 3. Bg2" },
            ["rnbqkbnr/ppp2ppp/4p3/3p4/8/5NP1/PPPPPP1P/RNBQKB1R"] = new Opening { ECO = "A07", Name = "King's Indian Attack vs French", Moves = "1. Nf3 d5 2. g3 e6" },
            ["rnbqkbnr/pp2pppp/8/2pp4/8/5NP1/PPPPPP1P/RNBQKB1R"] = new Opening { ECO = "A07", Name = "King's Indian Attack vs Caro-Kann", Moves = "1. Nf3 d5 2. g3 c5" },

            // Larsen's Opening (1.b3)
            ["rnbqkbnr/pppppppp/8/8/8/1P6/P1PPPPPP/RNBQKBNR"] = new Opening { ECO = "A01", Name = "Larsen's Opening", Moves = "1. b3" },
            ["rnbqkbnr/ppp1pppp/8/3p4/8/1P6/P1PPPPPP/RNBQKBNR"] = new Opening { ECO = "A01", Name = "Larsen's Opening: 1...d5", Moves = "1. b3 d5" },
            ["rnbqkbnr/pppp1ppp/8/4p3/8/1P6/P1PPPPPP/RNBQKBNR"] = new Opening { ECO = "A01", Name = "Larsen's Opening: 1...e5", Moves = "1. b3 e5" },
            ["rnbqkbnr/ppp1pppp/8/3p4/8/1P6/PBPPPPPP/RN1QKBNR"] = new Opening { ECO = "A01", Name = "Larsen's Opening: 2.Bb2", Moves = "1. b3 d5 2. Bb2" },

            // Polish Opening (1.b4)
            ["rnbqkbnr/pppppppp/8/8/1P6/8/P1PPPPPP/RNBQKBNR"] = new Opening { ECO = "A00", Name = "Polish Opening (Sokolsky)", Moves = "1. b4" },
            ["rnbqkbnr/pppp1ppp/8/4p3/1P6/8/P1PPPPPP/RNBQKBNR"] = new Opening { ECO = "A00", Name = "Polish Opening: 1...e5", Moves = "1. b4 e5" },
            ["rnbqkbnr/pppp1ppp/8/4p3/1P6/2P5/P2PPPPP/RNBQKBNR"] = new Opening { ECO = "A00", Name = "Polish Opening: Bugayev Attack", Moves = "1. b4 e5 2. c3" },
            ["rnbqkbnr/pppp1ppp/8/4p3/1P6/5N2/P1PPPPPP/RNBQKB1R"] = new Opening { ECO = "A00", Name = "Polish Opening: Outflank Variation", Moves = "1. b4 e5 2. Bb2" },

            // Saragossa Opening
            ["rnbqkbnr/pppppppp/8/8/8/2P5/PP1PPPPP/RNBQKBNR"] = new Opening { ECO = "A00", Name = "Saragossa Opening", Moves = "1. c3" },

            // Van't Kruijs Opening
            ["rnbqkbnr/pppppppp/8/8/8/4P3/PPPP1PPP/RNBQKBNR"] = new Opening { ECO = "A00", Name = "Van't Kruijs Opening", Moves = "1. e3" },

            // Mieses Opening
            ["rnbqkbnr/pppppppp/8/8/8/3P4/PPP1PPPP/RNBQKBNR"] = new Opening { ECO = "A00", Name = "Mieses Opening", Moves = "1. d3" },

            // Sicilian Alapin (more depth)
            ["rnbqkbnr/pp1ppppp/8/2p5/4P3/2P5/PP1P1PPP/RNBQKBNR"] = new Opening { ECO = "B22", Name = "Sicilian Defense: Alapin Variation", Moves = "1. e4 c5 2. c3" },
            ["rnbqkbnr/pp2pppp/3p4/2p5/4P3/2P5/PP1P1PPP/RNBQKBNR"] = new Opening { ECO = "B22", Name = "Sicilian Defense: Alapin, 2...d6", Moves = "1. e4 c5 2. c3 d6" },
            ["rnbqkbnr/pp1p1ppp/4p3/2p5/4P3/2P5/PP1P1PPP/RNBQKBNR"] = new Opening { ECO = "B22", Name = "Sicilian Defense: Alapin, 2...e6", Moves = "1. e4 c5 2. c3 e6" },
            ["rnbqkb1r/pp1ppppp/5n2/2p5/4P3/2P5/PP1P1PPP/RNBQKBNR"] = new Opening { ECO = "B22", Name = "Sicilian Defense: Alapin, 2...Nf6", Moves = "1. e4 c5 2. c3 Nf6" },
            ["rnbqkbnr/pp1ppppp/8/2p5/3PP3/2P5/PP3PPP/RNBQKBNR"] = new Opening { ECO = "B22", Name = "Sicilian Defense: Alapin, 3.d4", Moves = "1. e4 c5 2. c3 d5 3. exd5" },

            // Sicilian Smith-Morra Gambit (more depth)
            ["rnbqkbnr/pp1ppppp/8/2p5/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B21", Name = "Sicilian Defense: Smith-Morra Gambit", Moves = "1. e4 c5 2. d4" },
            ["rnbqkbnr/pp1ppppp/8/8/3pP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B21", Name = "Sicilian Defense: Smith-Morra, 2...cxd4", Moves = "1. e4 c5 2. d4 cxd4" },
            ["rnbqkbnr/pp1ppppp/8/8/3pP3/2P5/PP3PPP/RNBQKBNR"] = new Opening { ECO = "B21", Name = "Sicilian Defense: Smith-Morra, 3.c3", Moves = "1. e4 c5 2. d4 cxd4 3. c3" },
            ["rnbqkbnr/pp1ppppp/8/8/4P3/2p5/PP3PPP/RNBQKBNR"] = new Opening { ECO = "B21", Name = "Sicilian Defense: Smith-Morra Accepted", Moves = "1. e4 c5 2. d4 cxd4 3. c3 dxc3" },
            ["rnbqkbnr/pp1ppppp/8/8/4P3/2N5/PP3PPP/R1BQKBNR"] = new Opening { ECO = "B21", Name = "Sicilian Defense: Smith-Morra, 4.Nxc3", Moves = "1. e4 c5 2. d4 cxd4 3. c3 dxc3 4. Nxc3" },

            // More King's Gambit variations
            ["rnbqkbnr/pppp1ppp/8/8/4Pp2/8/PPPP2PP/RNBQKBNR"] = new Opening { ECO = "C30", Name = "King's Gambit", Moves = "1. e4 e5 2. f4" },
            ["rnbqkbnr/pppp1ppp/8/4p3/4PP2/8/PPPP2PP/RNBQKBNR"] = new Opening { ECO = "C30", Name = "King's Gambit: Declined", Moves = "1. e4 e5 2. f4 Bc5" },
            ["rnbqkbnr/pppp1p1p/8/6p1/4Pp2/5N2/PPPP2PP/RNBQKB1R"] = new Opening { ECO = "C37", Name = "King's Gambit: Muzio Gambit", Moves = "1. e4 e5 2. f4 exf4 3. Nf3 g5 4. Bc4 g4" },
            ["rnbqkbnr/pppp1p1p/8/6p1/2B1Pp2/5N2/PPPP2PP/RNBQK2R"] = new Opening { ECO = "C37", Name = "King's Gambit: 4.Bc4", Moves = "1. e4 e5 2. f4 exf4 3. Nf3 g5 4. Bc4" },

            // Bird's Opening (more depth)
            ["rnbqkbnr/ppp1pppp/8/3p4/5P2/8/PPPPP1PP/RNBQKBNR"] = new Opening { ECO = "A02", Name = "Bird's Opening: 1...d5", Moves = "1. f4 d5" },
            ["rnbqkbnr/ppp1pppp/8/3p4/5P2/5N2/PPPPP1PP/RNBQKB1R"] = new Opening { ECO = "A02", Name = "Bird's Opening: 2.Nf3", Moves = "1. f4 d5 2. Nf3" },
            ["rnbqkb1r/ppp1pppp/5n2/3p4/5P2/5N2/PPPPP1PP/RNBQKB1R"] = new Opening { ECO = "A03", Name = "Bird's Opening: 2...Nf6", Moves = "1. f4 d5 2. Nf3 Nf6" },
            ["rnbqkbnr/pppp1ppp/8/4p3/5P2/8/PPPPP1PP/RNBQKBNR"] = new Opening { ECO = "A02", Name = "Bird's Opening: From's Gambit", Moves = "1. f4 e5" },
            ["rnbqkbnr/pppp1ppp/8/8/5p2/8/PPPPP1PP/RNBQKBNR"] = new Opening { ECO = "A02", Name = "Bird's Opening: From's Gambit Accepted", Moves = "1. f4 e5 2. fxe5" },

            // Trompowsky Attack (more depth)
            ["rnbqkb1r/pppppppp/5n2/6B1/3P4/8/PPP1PPPP/RN1QKBNR"] = new Opening { ECO = "A45", Name = "Trompowsky Attack", Moves = "1. d4 Nf6 2. Bg5" },
            ["rnbqkb1r/pppp1ppp/5n2/4p1B1/3P4/8/PPP1PPPP/RN1QKBNR"] = new Opening { ECO = "A45", Name = "Trompowsky Attack: 2...e5", Moves = "1. d4 Nf6 2. Bg5 e5" },
            ["rnbqkb1r/ppp1pppp/5n2/3p2B1/3P4/8/PPP1PPPP/RN1QKBNR"] = new Opening { ECO = "A45", Name = "Trompowsky Attack: 2...d5", Moves = "1. d4 Nf6 2. Bg5 d5" },
            ["rnbqkb1r/pppppp1p/5np1/6B1/3P4/8/PPP1PPPP/RN1QKBNR"] = new Opening { ECO = "A45", Name = "Trompowsky Attack: 2...g6", Moves = "1. d4 Nf6 2. Bg5 g6" },
            ["rnbqkb1r/pppppppp/5n2/8/3P2B1/8/PPP1PPPP/RN1QKBNR"] = new Opening { ECO = "A45", Name = "Trompowsky Attack: 2...Ne4", Moves = "1. d4 Nf6 2. Bg5 Ne4" },

            // Vienna Game Gambit
            ["rnbqkbnr/pppp1ppp/8/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR"] = new Opening { ECO = "C25", Name = "Vienna Game", Moves = "1. e4 e5 2. Nc3" },
            ["rnbqkbnr/pppp1ppp/8/4p3/4PP2/2N5/PPPP2PP/R1BQKBNR"] = new Opening { ECO = "C25", Name = "Vienna Gambit", Moves = "1. e4 e5 2. Nc3 Nf6 3. f4" },
            ["rnbqkbnr/pppp1ppp/8/8/4Pp2/2N5/PPPP2PP/R1BQKBNR"] = new Opening { ECO = "C25", Name = "Vienna Gambit Accepted", Moves = "1. e4 e5 2. Nc3 Nf6 3. f4 exf4" },

            // More Petroff Defense depth
            ["rnbqkb1r/pppp1ppp/5n2/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense", Moves = "1. e4 e5 2. Nf3 Nf6" },
            ["rnbqkb1r/pppp1ppp/5n2/4N3/4P3/8/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense: 3.Nxe5", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5" },
            ["rnbqkb1r/pppp1ppp/8/4N3/4n3/8/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense: 3...Nxe4", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5 Nxe4" },
            ["rnbqkb1r/ppp2ppp/3p4/4N3/4n3/8/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense: Classical Attack", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5 d6" },
            ["rnbqkb1r/ppp2ppp/3p4/8/4n3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense: Classical, 4.Nf3 Nxe4", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5 d6 4. Nf3 Nxe4" },
            ["rnbqkb1r/ppp2ppp/3p1n2/8/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense: Classical, 4...Nf6", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5 d6 4. Nf3 Nf6" },
            ["rnbqkb1r/ppp2ppp/3p1n2/8/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C42", Name = "Petrov Defense: Classical, 5.d4", Moves = "1. e4 e5 2. Nf3 Nf6 3. Nxe5 d6 4. Nf3 Nxe4 5. d4" },

            // More Scotch depth
            ["r1bqkbnr/pppp1ppp/2n5/4p3/3PP3/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C44", Name = "Scotch Game: 3.d4", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4" },
            ["r1bqk1nr/pppp1ppp/2n5/2b5/3NP3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C45", Name = "Scotch Game: Classical", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4 exd4 4. Nxd4 Bc5" },
            ["r1bqk1nr/pppp1ppp/2n5/2b5/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "C45", Name = "Scotch Game: Classical, 5.Nc3", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4 exd4 4. Nxd4 Bc5 5. Nc3" },
            ["r1bqk1nr/pppp1ppp/2n5/2b5/2B1P3/5N2/PPP2PPP/RNBQK2R"] = new Opening { ECO = "C45", Name = "Scotch Game: 4.Bc4", Moves = "1. e4 e5 2. Nf3 Nc6 3. d4 exd4 4. Bc4" },

            // Symmetrical English (more depth)
            ["rnbqkbnr/pp1ppppp/8/2p5/2P5/8/PP1PPPPP/RNBQKBNR"] = new Opening { ECO = "A30", Name = "English Opening: Symmetrical Variation", Moves = "1. c4 c5" },
            ["rnbqkbnr/pp1ppppp/8/2p5/2P5/5N2/PP1PPPPP/RNBQKB1R"] = new Opening { ECO = "A30", Name = "English Opening: Symmetrical, 2.Nf3", Moves = "1. c4 c5 2. Nf3" },
            ["rnbqkb1r/pp1ppppp/5n2/2p5/2P5/5N2/PP1PPPPP/RNBQKB1R"] = new Opening { ECO = "A30", Name = "English Opening: Symmetrical, 2...Nf6", Moves = "1. c4 c5 2. Nf3 Nf6" },
            ["rnbqkbnr/pp1p1ppp/4p3/2p5/2P5/5N2/PP1PPPPP/RNBQKB1R"] = new Opening { ECO = "A30", Name = "English Opening: Symmetrical, 2...e6", Moves = "1. c4 c5 2. Nf3 e6" },
            ["rnbqkbnr/pp1ppppp/8/2p5/2P5/6P1/PP1PPP1P/RNBQKBNR"] = new Opening { ECO = "A30", Name = "English Opening: Symmetrical, Hedgehog", Moves = "1. c4 c5 2. g3" },
            ["r1bqkbnr/pp1ppppp/2n5/2p5/2P5/5N2/PP1PPPPP/RNBQKB1R"] = new Opening { ECO = "A30", Name = "English Opening: Symmetrical, 2...Nc6", Moves = "1. c4 c5 2. Nf3 Nc6" },

            // Modern Defense (more depth)
            ["rnbqk1nr/ppppppbp/6p1/8/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B06", Name = "Modern Defense: 3.Nc3", Moves = "1. e4 g6 2. d4 Bg7 3. Nc3" },
            ["rnbqk1nr/ppp1ppbp/3p2p1/8/3PP3/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B06", Name = "Modern Defense: 3...d6", Moves = "1. e4 g6 2. d4 Bg7 3. Nc3 d6" },
            ["rnbqk1nr/ppp1ppbp/3p2p1/8/3PP3/2N2N2/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B06", Name = "Modern Defense: 4.Nf3", Moves = "1. e4 g6 2. d4 Bg7 3. Nc3 d6 4. Nf3" },
            ["r1bqk1nr/ppp1ppbp/2np2p1/8/3PP3/2N2N2/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B06", Name = "Modern Defense: 4...Nc6", Moves = "1. e4 g6 2. d4 Bg7 3. Nc3 d6 4. Nf3 Nc6" },

            // Bogo-Indian Defense
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "E11", Name = "Bogo-Indian Defense", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 Bb4+" },
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/5N2/PP1BPPPP/RN1QKB1R"] = new Opening { ECO = "E11", Name = "Bogo-Indian Defense: 4.Bd2", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 Bb4+ 4. Bd2" },
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "E11", Name = "Bogo-Indian Defense: 4.Nbd2", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 Bb4+ 4. Nbd2" },

            // Tarrasch Defense
            ["rnbqkb1r/pp3ppp/4pn2/2pp4/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D32", Name = "Tarrasch Defense", Moves = "1. d4 d5 2. c4 e6 3. Nc3 c5" },
            ["rnbqkb1r/pp3ppp/4pn2/3p4/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D32", Name = "Tarrasch Defense: 4.cxd5", Moves = "1. d4 d5 2. c4 e6 3. Nc3 c5 4. cxd5" },
            ["rnbqkb1r/pp3ppp/5n2/3pp3/3P4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D34", Name = "Tarrasch Defense: 4...exd5", Moves = "1. d4 d5 2. c4 e6 3. Nc3 c5 4. cxd5 exd5" },

            // Chigorin Defense (Queen's Gambit)
            ["r1bqkbnr/ppp1pppp/2n5/3p4/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D07", Name = "Chigorin Defense", Moves = "1. d4 d5 2. c4 Nc6" },
            ["r1bqkbnr/ppp1pppp/2n5/3P4/3P4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D07", Name = "Chigorin Defense: Exchange", Moves = "1. d4 d5 2. c4 Nc6 3. cxd5" },
            ["r1bqkbnr/ppp1pppp/2n5/3p4/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "D07", Name = "Chigorin Defense: 3.Nf3", Moves = "1. d4 d5 2. c4 Nc6 3. Nf3" },

            // Baltic Defense
            ["rnbqkbnr/ppp2ppp/4p3/3p4/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D02", Name = "Queen's Pawn Game: Baltic Defense", Moves = "1. d4 d5 2. c4 Bf5" },

            // Albin Counter-Gambit
            ["rnbqkbnr/ppp2ppp/8/3pp3/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D08", Name = "Albin Counter-Gambit", Moves = "1. d4 d5 2. c4 e5" },
            ["rnbqkbnr/ppp2ppp/8/3Pp3/3P4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D08", Name = "Albin Counter-Gambit: 3.dxe5", Moves = "1. d4 d5 2. c4 e5 3. dxe5" },
            ["rnbqkbnr/ppp2ppp/8/4P3/3p4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D08", Name = "Albin Counter-Gambit: 3...d4", Moves = "1. d4 d5 2. c4 e5 3. dxe5 d4" },

            // Staunton Gambit vs Dutch
            ["rnbqkbnr/ppppp1pp/8/5p2/3PP3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "A82", Name = "Dutch Defense: Staunton Gambit", Moves = "1. d4 f5 2. e4" },
            ["rnbqkbnr/ppppp1pp/8/8/3Pp3/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "A82", Name = "Dutch Defense: Staunton Gambit Accepted", Moves = "1. d4 f5 2. e4 fxe4" },

            // Marshall Defense
            ["rnbqkbnr/ppp2ppp/4p3/3p4/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "D05", Name = "Queen's Pawn Game: Marshall Defense", Moves = "1. d4 d5 2. c4 e6" },

            // Czech Benoni
            ["rnbqkb1r/pp1ppppp/5n2/2pP4/8/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A56", Name = "Czech Benoni", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e5" },
            ["rnbqkb1r/pp1p1ppp/5n2/2pPp3/8/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A56", Name = "Czech Benoni: 4.Nc3", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e5 4. Nc3" },

            // Snake Benoni
            ["rnbqk2r/pp2ppbp/3p1np1/2pP4/8/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "A60", Name = "Benoni Defense: Snake Variation", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e6 4. Nc3 exd5 5. cxd5 Bd6" },

            // Leningrad Dutch (more depth)
            ["rnbqkbnr/ppppp2p/6p1/5p2/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A85", Name = "Dutch Defense: Leningrad, 2...g6", Moves = "1. d4 f5 2. c4 g6" },
            ["rnbqkb1r/ppppp2p/5np1/5p2/2PP4/8/PP2PPPP/RNBQKBNR"] = new Opening { ECO = "A86", Name = "Dutch Defense: Leningrad, 3...Nf6", Moves = "1. d4 f5 2. c4 Nf6 3. g3 g6" },
            ["rnbqk2r/ppppppbp/5np1/8/2PP4/6P1/PP2PP1P/RNBQKBNR"] = new Opening { ECO = "A87", Name = "Dutch Defense: Leningrad, 4.Bg2", Moves = "1. d4 f5 2. c4 Nf6 3. g3 g6 4. Bg2" },

            // Stonewall Dutch (more depth)
            ["rnbqkb1r/ppp1p1pp/5n2/3p1p2/2PP4/6P1/PP2PP1P/RNBQKBNR"] = new Opening { ECO = "A84", Name = "Dutch Defense: Stonewall, 3...d5", Moves = "1. d4 f5 2. c4 Nf6 3. g3 d5" },
            ["rnbqkb1r/ppp3pp/4pn2/3p1p2/2PP4/6P1/PP2PPBP/RNBQK1NR"] = new Opening { ECO = "A90", Name = "Dutch Defense: Stonewall, Modern", Moves = "1. d4 f5 2. c4 Nf6 3. g3 e6 4. Bg2 d5" },

            // French Tarrasch (more depth)
            ["rnbqkbnr/ppp2ppp/4p3/3p4/3PP3/8/PPPN1PPP/R1BQKBNR"] = new Opening { ECO = "C03", Name = "French Defense: Tarrasch Variation", Moves = "1. e4 e6 2. d4 d5 3. Nd2" },
            ["rnbqkbnr/pp3ppp/4p3/2pp4/3PP3/8/PPPN1PPP/R1BQKBNR"] = new Opening { ECO = "C07", Name = "French Defense: Tarrasch, 3...c5", Moves = "1. e4 e6 2. d4 d5 3. Nd2 c5" },
            ["rnbqkb1r/pp3ppp/4pn2/2pp4/3PP3/8/PPPN1PPP/R1BQKBNR"] = new Opening { ECO = "C07", Name = "French Defense: Tarrasch, 4.exd5", Moves = "1. e4 e6 2. d4 d5 3. Nd2 c5 4. exd5" },
            ["rnbqkb1r/pp3ppp/5n2/2pP4/3P4/8/PPPN1PPP/R1BQKBNR"] = new Opening { ECO = "C07", Name = "French Defense: Tarrasch, Open", Moves = "1. e4 e6 2. d4 d5 3. Nd2 c5 4. exd5 exd5" },
            ["rnbqkb1r/pp3ppp/4pn2/2pP4/3P4/8/PPPN1PPP/R1BQKBNR"] = new Opening { ECO = "C09", Name = "French Defense: Tarrasch, 4...Qxd5", Moves = "1. e4 e6 2. d4 d5 3. Nd2 c5 4. exd5 Qxd5" },

            // French Exchange (more depth)
            ["rnbqkbnr/ppp2ppp/4p3/3P4/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C01", Name = "French Defense: Exchange Variation", Moves = "1. e4 e6 2. d4 d5 3. exd5" },
            ["rnbqkbnr/ppp2ppp/8/3p4/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "C01", Name = "French Defense: Exchange, 3...exd5", Moves = "1. e4 e6 2. d4 d5 3. exd5 exd5" },
            ["rnbqkb1r/ppp2ppp/5n2/3p4/3P4/5N2/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "C01", Name = "French Defense: Exchange, 4.Nf3", Moves = "1. e4 e6 2. d4 d5 3. exd5 exd5 4. Nf3" },

            // Sicilian Scheveningen (more depth)
            ["rnbqkb1r/pp3ppp/3ppn2/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B80", Name = "Sicilian Defense: Scheveningen Variation", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e6" },
            ["rnbqkb1r/pp3ppp/3ppn2/6B1/3NP3/2N5/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B80", Name = "Sicilian Defense: Scheveningen, English Attack", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e6 6. Be3" },
            ["rnbqkb1r/pp3ppp/3ppn2/8/3NP3/2N1B3/PPP2PPP/R2QKB1R"] = new Opening { ECO = "B81", Name = "Sicilian Defense: Scheveningen, Keres Attack", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e6 6. g4" },
            ["rnbqkb1r/1p3ppp/p2ppn2/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new Opening { ECO = "B82", Name = "Sicilian Defense: Scheveningen, 6...a6", Moves = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 e6 6. Be2 a6" },

            // Sicilian Paulsen (more depth)
            ["rnbqkbnr/pp3ppp/4p3/2pP4/8/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "B40", Name = "Sicilian Defense: Paulsen Variation", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4" },
            ["rnbqkbnr/pp3ppp/4p3/8/3NP3/8/PPP2PPP/RNBQKB1R"] = new Opening { ECO = "B44", Name = "Sicilian Defense: Paulsen, 4...a6", Moves = "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 a6" },

            // Anti-Sicilian Lines (more depth)
            ["rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPPNPPP/RNBQKB1R"] = new Opening { ECO = "B20", Name = "Sicilian Defense: Bowdler Attack", Moves = "1. e4 c5 2. Bc4" },
            ["rnbqkbnr/pp1ppppp/8/2p5/2B1P3/8/PPPP1PPP/RNBQK1NR"] = new Opening { ECO = "B20", Name = "Sicilian Defense: Wing Gambit", Moves = "1. e4 c5 2. b4" },
            ["rnbqkbnr/pp1ppppp/8/2p5/1P2P3/8/P1PP1PPP/RNBQKBNR"] = new Opening { ECO = "B20", Name = "Sicilian Defense: Wing Gambit", Moves = "1. e4 c5 2. b4" },
            ["r1bqkbnr/pp1ppppp/2n5/2p5/4P3/1B6/PPPP1PPP/RNBQK1NR"] = new Opening { ECO = "B30", Name = "Sicilian Defense: Rossolimo Variation", Moves = "1. e4 c5 2. Nf3 Nc6 3. Bb5" },
            ["r1bqkbnr/pp1ppppp/2n5/1Bp5/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "B30", Name = "Sicilian Defense: Rossolimo, 3...g6", Moves = "1. e4 c5 2. Nf3 Nc6 3. Bb5 g6" },
            ["r1bqkbnr/pp1p1ppp/2n1p3/1Bp5/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "B30", Name = "Sicilian Defense: Rossolimo, 3...e6", Moves = "1. e4 c5 2. Nf3 Nc6 3. Bb5 e6" },

            // Moscow Variation (Anti-Sicilian)
            ["rnbqkb1r/pp1ppppp/5n2/1Bp5/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "B51", Name = "Sicilian Defense: Moscow Variation", Moves = "1. e4 c5 2. Nf3 d6 3. Bb5+" },
            ["rnbqkb1r/pp2pppp/3p1n2/1Bp5/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "B51", Name = "Sicilian Defense: Moscow, 3...Bd7", Moves = "1. e4 c5 2. Nf3 d6 3. Bb5+ Bd7" },
            ["r1bqkb1r/pp2pppp/2np1n2/1Bp5/4P3/5N2/PPPP1PPP/RNBQK2R"] = new Opening { ECO = "B52", Name = "Sicilian Defense: Moscow, 3...Nc6", Moves = "1. e4 c5 2. Nf3 d6 3. Bb5+ Nc6" },

            // Caro-Kann Fantasy Variation
            ["rnbqkbnr/pp2pppp/2p5/3p4/3PP3/2P5/PP3PPP/RNBQKBNR"] = new Opening { ECO = "B12", Name = "Caro-Kann Defense: Fantasy Variation", Moves = "1. e4 c6 2. d4 d5 3. f3" },

            // Scandinavian Modern (more depth)
            ["rnbqkb1r/ppp1pppp/5n2/3P4/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: Modern, 3.d4", Moves = "1. e4 d5 2. exd5 Nf6 3. d4" },
            ["rnbqkb1r/ppp1pppp/8/3n4/3P4/8/PPP2PPP/RNBQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: Modern, 3...Nxd5", Moves = "1. e4 d5 2. exd5 Nf6 3. d4 Nxd5" },
            ["rnbqkb1r/ppp1pppp/8/3n4/3P4/2N5/PPP2PPP/R1BQKBNR"] = new Opening { ECO = "B01", Name = "Scandinavian Defense: Modern, 4.Nc3", Moves = "1. e4 d5 2. exd5 Nf6 3. d4 Nxd5 4. Nc3" },

            // More Ruy Lopez depth (Marshall Attack)
            ["r1bq1rk1/2ppbppp/p1n2n2/1p2p3/4P3/1B3N2/PPPP1PPP/RNBQR1K1"] = new Opening { ECO = "C88", Name = "Ruy Lopez: Marshall Attack", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1 b5 7. Bb3 O-O 8. c3 d5" },
            ["r1bq1rk1/2p1bppp/p1np1n2/1p2p3/4P3/1BP2N2/PP1P1PPP/RNBQR1K1"] = new Opening { ECO = "C89", Name = "Ruy Lopez: Marshall Attack, Main Line", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1 b5 7. Bb3 O-O 8. c3 d5 9. exd5 Nxd5" },

            // More Italian Game depth
            ["r1bqk2r/pppp1ppp/2n2n2/2b1p3/2B1P3/3P1N2/PPP2PPP/RNBQK2R"] = new Opening { ECO = "C54", Name = "Italian Game: Classical, Greco Gambit", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. c3 Nf6 5. d4" },
            ["r1bqk2r/pppp1ppp/2n2n2/2b1p3/2B1PP2/5N2/PPPP2PP/RNBQK2R"] = new Opening { ECO = "C54", Name = "Italian Game: Giuoco Piano, Moeller Attack", Moves = "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. c3 Nf6 5. d4 exd4 6. cxd4 Bb4+" },

            // Queen's Pawn Opening systems
            ["rnbqkbnr/ppp1pppp/8/3p4/3P4/5N2/PPP1PPPP/RNBQKB1R"] = new Opening { ECO = "D02", Name = "Queen's Pawn Game: 2.Nf3", Moves = "1. d4 d5 2. Nf3" },
            ["rnbqkb1r/ppp1pppp/5n2/3p4/3P4/5N2/PPP1PPPP/RNBQKB1R"] = new Opening { ECO = "D02", Name = "Queen's Pawn Game: 2...Nf6", Moves = "1. d4 d5 2. Nf3 Nf6" },
            ["rnbqkb1r/ppp1pppp/5n2/3p2B1/3P4/5N2/PPP1PPPP/RN1QKB1R"] = new Opening { ECO = "D02", Name = "Queen's Pawn Game: Torre Attack", Moves = "1. d4 d5 2. Nf3 Nf6 3. Bg5" },

            // Richter-Veresov Attack
            ["rnbqkb1r/ppp1pppp/5n2/3p2B1/3P4/2N5/PPP1PPPP/R2QKBNR"] = new Opening { ECO = "D01", Name = "Richter-Veresov Attack", Moves = "1. d4 d5 2. Nc3 Nf6 3. Bg5" },
            ["rnbqkb1r/ppp1pppp/5n2/3p4/3P2B1/2N5/PPP1PPPP/R2QKBNR"] = new Opening { ECO = "D01", Name = "Richter-Veresov Attack: 3.Bf4", Moves = "1. d4 d5 2. Nc3 Nf6 3. Bf4" },

            // Barry Attack
            ["rnbqkb1r/ppp1pppp/5n2/3p4/3P1B2/2N5/PPP1PPPP/R2QKBNR"] = new Opening { ECO = "D01", Name = "Barry Attack", Moves = "1. d4 Nf6 2. Nf3 g6 3. Nc3 d5 4. Bf4" },

            // Zukertort Opening
            ["rnbqkbnr/pppp1ppp/8/4p3/8/5N2/PPPPPPPP/RNBQKB1R"] = new Opening { ECO = "A04", Name = "Zukertort Opening", Moves = "1. Nf3 e5" },
            ["rnbqkbnr/pppp1ppp/8/4p3/8/5NP1/PPPPPP1P/RNBQKB1R"] = new Opening { ECO = "A04", Name = "Zukertort Opening: Kingside Fianchetto", Moves = "1. Nf3 e5 2. g3" },

            // Queen's Indian Petrosian System
            ["rn1qkb1r/pbpp1ppp/1p2pn2/8/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "E12", Name = "Queen's Indian Defense: Petrosian System", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 b6 4. Nc3" },
            ["rn1qkb1r/p1pp1ppp/bp2pn2/8/2PP4/5N2/PP2PPPP/RNBQKB1R"] = new Opening { ECO = "E12", Name = "Queen's Indian Defense: Miles Variation", Moves = "1. d4 Nf6 2. c4 e6 3. Nf3 b6 4. a3 Ba6" },

            // Nimzo-Indian (more variations)
            ["rnbqk2r/pppp1ppp/4pn2/8/1bPP4/2NB4/PP2PPPP/R1BQK1NR"] = new Opening { ECO = "E44", Name = "Nimzo-Indian Defense: Reshevsky Variation", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4 4. e3 O-O 5. Bd3" },
            ["rnbq1rk1/pppp1ppp/4pn2/8/1bPP4/2N1PN2/PP3PPP/R1BQKB1R"] = new Opening { ECO = "E52", Name = "Nimzo-Indian Defense: Huebner Variation", Moves = "1. d4 Nf6 2. c4 e6 3. Nc3 Bb4 4. e3 O-O 5. Nf3 d5" },

            // Grunfeld (more depth)
            ["rnbqk2r/ppp1ppbp/5np1/3p4/2PP4/2N2N2/PP2PPPP/R1BQKB1R"] = new Opening { ECO = "D90", Name = "Grunfeld Defense: Three Knights Variation", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 d5 4. Nf3" },
            ["rnbqk2r/ppp1ppbp/6p1/3n4/3P4/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "D80", Name = "Grunfeld Defense: Stockholm Variation", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 d5 4. Bg5" },
            ["rnbqkb1r/ppp1pp1p/5np1/3p4/2PP1B2/2N5/PP2PPPP/R2QKBNR"] = new Opening { ECO = "D80", Name = "Grunfeld Defense: 4.Bf4", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 d5 4. Bf4" },

            // More King's Indian depth
            ["rnbq1rk1/ppp2pbp/3p1np1/4p3/2PPP3/2N2N2/PP2BPPP/R1BQK2R"] = new Opening { ECO = "E99", Name = "King's Indian Defense: Classical, Mar del Plata Attack", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O 6. Be2 e5 7. O-O Nc6 8. d5" },
            ["r1bq1rk1/ppp2pbp/3p2p1/n3p3/2PPP1n1/2N2N2/PP2BPPP/R1BQ1RK1"] = new Opening { ECO = "E97", Name = "King's Indian Defense: Orthodox, Bayonet Attack", Moves = "1. d4 Nf6 2. c4 g6 3. Nc3 Bg7 4. e4 d6 5. Nf3 O-O 6. Be2 e5 7. O-O Nc6 8. b4" },

            // More Modern Benoni depth
            ["rnbqk2r/pp1p1pbp/4pnp1/2pP4/2P5/2N5/PP2PPPP/R1BQKBNR"] = new Opening { ECO = "A60", Name = "Modern Benoni: Classical Fianchetto", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e6 4. Nc3 exd5 5. cxd5 d6 6. Nf3 g6" },
            ["rnbqk2r/pp3pbp/3p1np1/2pP4/4P3/2N2N2/PP3PPP/R1BQKB1R"] = new Opening { ECO = "A70", Name = "Modern Benoni: Classical, 7.e4", Moves = "1. d4 Nf6 2. c4 c5 3. d5 e6 4. Nc3 exd5 5. cxd5 d6 6. e4 g6 7. Nf3" },
        };

        /// <summary>
        /// Look up the opening name for a given position
        /// </summary>
        /// <param name="fen">The FEN string of the position</param>
        /// <returns>Opening info if found, null otherwise</returns>
        public static Opening? GetOpening(string fen)
        {
            if (string.IsNullOrEmpty(fen))
                return null;

            try
            {
                // Extract just the piece placement (first part of FEN)
                string piecePlacement = fen.Split(' ')[0];

                // First check built-in dictionary
                if (_openings.TryGetValue(piecePlacement, out var opening))
                {
                    return opening;
                }

                // Fall back to external OpeningDatabase (eco.json files)
                var externalOpening = OpeningDatabase.Instance.GetOpening(fen);
                if (externalOpening != null)
                {
                    return new Opening
                    {
                        ECO = externalOpening.ECO,
                        Name = externalOpening.Name,
                        Moves = externalOpening.Moves
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpeningBook error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Check if a position is still in known opening theory
        /// </summary>
        public static bool IsInTheory(string fen)
        {
            return GetOpening(fen) != null;
        }

        /// <summary>
        /// Get a formatted string for display
        /// </summary>
        public static string GetOpeningDisplay(string fen)
        {
            var opening = GetOpening(fen);
            if (opening == null)
                return "";

            if (string.IsNullOrEmpty(opening.ECO))
                return opening.Name;

            return $"{opening.ECO}: {opening.Name}";
        }
    }
}
