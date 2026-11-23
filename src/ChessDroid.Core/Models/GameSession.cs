using ChessDroid.Services;

namespace ChessDroid.Models
{
    /// <summary>
    /// Single source of truth for a board's position state — board, side to move, castling
    /// rights (plus Chess960 rook-file tracking), en passant target, and FEN move counters.
    /// Extracted out of ChessBoardControl per CHESSDROID_AUDIT.md §4.2: the control used to
    /// own these fields directly and re-derive legality/application logic itself. Legality
    /// still lives in MoveGenerator and move application in ChessRulesService.ApplyUciMove —
    /// this class only owns the state those operate on, letting ChessBoardControl become a
    /// thin render+input layer over it.
    /// </summary>
    public sealed class GameSession
    {
        public const string StartingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public ChessBoard Board { get; private set; } = ChessBoard.FromFEN(StartingFen);
        public bool WhiteToMove { get; set; } = true;
        public bool WhiteKingsideCastle { get; private set; } = true;
        public bool WhiteQueensideCastle { get; private set; } = true;
        public bool BlackKingsideCastle { get; private set; } = true;
        public bool BlackQueensideCastle { get; private set; } = true;
        public bool Chess960Mode { get; set; }
        public int WhiteKingsideRookFile { get; private set; } = 7;
        public int WhiteQueensideRookFile { get; private set; }
        public int BlackKingsideRookFile { get; private set; } = 7;
        public int BlackQueensideRookFile { get; private set; }
        public int EnPassantRow { get; private set; } = -1;
        public int EnPassantCol { get; private set; } = -1;
        public int HalfmoveClock { get; private set; }
        public int FullmoveNumber { get; private set; } = 1;

        public CastlingState CurrentCastlingState() => new(
            WhiteKingside: WhiteKingsideCastle,
            WhiteQueenside: WhiteQueensideCastle,
            BlackKingside: BlackKingsideCastle,
            BlackQueenside: BlackQueensideCastle,
            Chess960Mode: Chess960Mode,
            WhiteKingsideRookFile: WhiteKingsideRookFile,
            WhiteQueensideRookFile: WhiteQueensideRookFile,
            BlackKingsideRookFile: BlackKingsideRookFile,
            BlackQueensideRookFile: BlackQueensideRookFile);

        public (int Row, int Col)? CurrentEnPassant() =>
            EnPassantRow >= 0 && EnPassantCol >= 0 ? (EnPassantRow, EnPassantCol) : null;

        /// <summary>Resets to the standard starting position.</summary>
        public void Reset() => LoadFen(StartingFen);

        /// <summary>Replaces the board object directly, without touching castling/en-passant/move-counter state.</summary>
        public void ReplaceBoard(ChessBoard newBoard) => Board = newBoard;

        /// <summary>Loads a position from a FEN string, parsing castling (standard or Shredder), en passant, and move counters.</summary>
        public void LoadFen(string fen)
        {
            Board = ChessBoard.FromFEN(fen);
            string[] parts = fen.Split(' ');
            WhiteToMove = parts.Length > 1 ? parts[1] == "w" : true;

            // Parse castling rights — handles both KQkq (standard) and Shredder file-letter FEN (Chess960).
            string castling = parts.Length > 2 ? parts[2] : "KQkq";
            WhiteKingsideCastle = false; WhiteQueensideCastle = false;
            BlackKingsideCastle = false; BlackQueensideCastle = false;
            WhiteKingsideRookFile = 7; WhiteQueensideRookFile = 0;
            BlackKingsideRookFile = 7; BlackQueensideRookFile = 0;

            // Find king files for Shredder FEN resolution
            int wKingFile = -1, bKingFile = -1;
            for (int f = 0; f < 8; f++)
            {
                if (Board.GetPiece(7, f) == 'K') wKingFile = f;
                if (Board.GetPiece(0, f) == 'k') bKingFile = f;
            }

            foreach (char c in castling)
            {
                switch (c)
                {
                    case 'K': WhiteKingsideCastle = true; break;
                    case 'Q': WhiteQueensideCastle = true; break;
                    case 'k': BlackKingsideCastle = true; break;
                    case 'q': BlackQueensideCastle = true; break;
                    default:
                        // Shredder FEN file letter
                        if (c >= 'A' && c <= 'H')
                        {
                            int rf = c - 'A';
                            if (wKingFile >= 0 && rf > wKingFile) { WhiteKingsideCastle = true; WhiteKingsideRookFile = rf; }
                            else { WhiteQueensideCastle = true; WhiteQueensideRookFile = rf; }
                        }
                        else if (c >= 'a' && c <= 'h')
                        {
                            int rf = c - 'a';
                            if (bKingFile >= 0 && rf > bKingFile) { BlackKingsideCastle = true; BlackKingsideRookFile = rf; }
                            else { BlackQueensideCastle = true; BlackQueensideRookFile = rf; }
                        }
                        break;
                }
            }

            // Parse en passant square
            string epSquare = parts.Length > 3 ? parts[3] : "-";
            if (epSquare != "-" && epSquare.Length == 2)
            {
                EnPassantCol = epSquare[0] - 'a';
                EnPassantRow = 8 - (epSquare[1] - '0');
            }
            else
            {
                EnPassantRow = -1;
                EnPassantCol = -1;
            }

            HalfmoveClock = parts.Length > 4 && int.TryParse(parts[4], out int hc) ? hc : 0;
            FullmoveNumber = parts.Length > 5 && int.TryParse(parts[5], out int fm) ? fm : 1;
        }

        /// <summary>Serializes the current state back to a full FEN string (including move counters).</summary>
        public string GetFen()
        {
            string boardFen = Board.ToFEN();
            string turn = WhiteToMove ? "w" : "b";

            string castling = "";
            if (Chess960Mode)
            {
                // Shredder FEN: file letter of each rook, queenside first (ascending file order)
                if (WhiteQueensideCastle) castling += (char)('A' + WhiteQueensideRookFile);
                if (WhiteKingsideCastle) castling += (char)('A' + WhiteKingsideRookFile);
                if (BlackQueensideCastle) castling += (char)('a' + BlackQueensideRookFile);
                if (BlackKingsideCastle) castling += (char)('a' + BlackKingsideRookFile);
            }
            else
            {
                if (WhiteKingsideCastle) castling += "K";
                if (WhiteQueensideCastle) castling += "Q";
                if (BlackKingsideCastle) castling += "k";
                if (BlackQueensideCastle) castling += "q";
            }
            if (string.IsNullOrEmpty(castling)) castling = "-";

            string epSquare = "-";
            if (EnPassantRow >= 0 && EnPassantCol >= 0)
                epSquare = $"{(char)('a' + EnPassantCol)}{8 - EnPassantRow}";

            return $"{boardFen} {turn} {castling} {epSquare} {HalfmoveClock} {FullmoveNumber}";
        }

        public List<string> GetAllLegalMovesUci()
            => MoveGenerator.GetAllLegalMovesUci(Board, WhiteToMove, CurrentCastlingState(), CurrentEnPassant());

        /// <summary>
        /// Applies an already-decided UCI move (promotion piece, if any, already chosen by
        /// the caller) to this session's state. Returns whether it was a capture (including
        /// en passant) and whether it was a Chess960 king-captures-own-rook castling move —
        /// both needed by callers for UI feedback (sound, move highlighting).
        /// </summary>
        public (bool IsCapture, bool IsChess960Castle) ExecuteMove(string uciMove)
        {
            var (fromRow, fromCol, toRow, toCol) = ParseUciSquares(uciMove);
            char movingPiece = Board.GetPiece(fromRow, fromCol);

            // Chess960 castling is king-captures-own-rook notation (e.g. e1h1) — ApplyUciMove
            // detects this itself from the board state, so it's not a separate code path here
            // any more; it's only distinguished for isCapture/lastMove purposes, which it is
            // NOT (it's not a real capture).
            bool isChess960Castle = Chess960Mode && char.ToLowerInvariant(movingPiece) == 'k' && fromRow == toRow &&
                Board.GetPiece(toRow, toCol) == (char.IsUpper(movingPiece) ? 'R' : 'r');

            char captured = Board.GetPiece(toRow, toCol);
            bool isCapture = !isChess960Castle && (captured != '.' ||
                (char.ToLowerInvariant(movingPiece) == 'p' && fromCol != toCol && captured == '.')); // en passant

            string castlingRights = BuildCastlingRightsString();
            string enPassantStr = CurrentEnPassant() is { } ep ? $"{(char)('a' + ep.Col)}{8 - ep.Row}" : "-";
            ChessRulesService.ApplyUciMove(Board, uciMove, ref castlingRights, ref enPassantStr);
            ApplyCastlingRightsString(castlingRights);
            (EnPassantRow, EnPassantCol) = ParseEnPassantString(enPassantStr);

            // FEN halfmove clock: resets on a pawn move or capture, otherwise increments.
            bool isPawnMove = char.ToLowerInvariant(movingPiece) == 'p';
            HalfmoveClock = (isPawnMove || isCapture) ? 0 : HalfmoveClock + 1;

            if (!WhiteToMove) FullmoveNumber++; // Black just moved — a full move has elapsed
            WhiteToMove = !WhiteToMove;

            return (isCapture, isChess960Castle);
        }

        // Standard (non-Chess960) "KQkq"-style castling rights string, for handoff to
        // ChessRulesService.ApplyUciMove — rook starting files there are always 0/7, so
        // Shredder file-letter notation isn't needed for this round-trip.
        private string BuildCastlingRightsString()
        {
            string s = (WhiteKingsideCastle ? "K" : "") + (WhiteQueensideCastle ? "Q" : "")
                     + (BlackKingsideCastle ? "k" : "") + (BlackQueensideCastle ? "q" : "");
            return s.Length == 0 ? "-" : s;
        }

        private void ApplyCastlingRightsString(string rights)
        {
            WhiteKingsideCastle = rights.Contains('K');
            WhiteQueensideCastle = rights.Contains('Q');
            BlackKingsideCastle = rights.Contains('k');
            BlackQueensideCastle = rights.Contains('q');
        }

        private static (int Row, int Col) ParseEnPassantString(string enPassant)
        {
            if (enPassant == "-" || enPassant.Length != 2) return (-1, -1);
            return (8 - (enPassant[1] - '0'), enPassant[0] - 'a');
        }

        private static (int fromRow, int fromCol, int toRow, int toCol) ParseUciSquares(string uciMove)
        {
            int fromCol = uciMove[0] - 'a';
            int fromRowAlg = uciMove[1] - '1';
            int toCol = uciMove[2] - 'a';
            int toRowAlg = uciMove[3] - '1';
            return (7 - fromRowAlg, fromCol, 7 - toRowAlg, toCol);
        }
    }
}
