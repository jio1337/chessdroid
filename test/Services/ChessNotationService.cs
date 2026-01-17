using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Delegate for applying UCI moves with ref parameters for castling and en passant
    /// </summary>
    public delegate void ApplyUciMoveDelegate(ChessBoard board, string uciMove, ref string castlingRights, ref string enPassant);

    /// <summary>
    /// Service for chess notation conversions (UCI to SAN, FEN parsing, etc.)
    /// </summary>
    public class ChessNotationService
    {
        /// <summary>
        /// Validates if a string is a valid UCI move format
        /// </summary>
        public static bool IsValidUCIMove(string? s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 4)
                return false;
            char a = s[0];
            char b = s[1];
            char c = s[2];
            char d = s[3];
            bool filesOk = (a >= 'a' && a <= 'h') && (c >= 'a' && c <= 'h');
            bool ranksOk = (b >= '1' && b <= '8') && (d >= '1' && d <= '8');
            return filesOk && ranksOk;
        }

        /// <summary>
        /// Converts algebraic notation (e.g., "e4") to board indices
        /// </summary>
        public static (int row, int col) NotationToIndices(string square, bool blackAtBottom)
        {
            // Validate input
            if (string.IsNullOrEmpty(square) || square.Length < 2)
                return (0, 0); // Return default position if invalid

            // Validate characters are in valid range
            if (square[0] < 'a' || square[0] > 'h' || square[1] < '1' || square[1] > '8')
                return (0, 0); // Return default position if invalid

            int fileChar = square[0] - 'a';
            int rank = square[1] - '0';
            if (!blackAtBottom)
            {
                int row = 8 - rank;
                int col = fileChar;
                return (row, col);
            }
            else
            {
                int row = rank - 1;
                int col = 7 - fileChar;
                return (row, col);
            }
        }

        /// <summary>
        /// Extracts position part from FEN (ignores move counters)
        /// </summary>
        public static string GetPositionFromFEN(string fen)
        {
            if (string.IsNullOrEmpty(fen))
                return "";

            var parts = fen.Split(' ');
            if (parts.Length >= 4)
            {
                // Return position + turn + castling + en passant
                return $"{parts[0]} {parts[1]} {parts[2]} {parts[3]}";
            }
            return fen;
        }

        /// <summary>
        /// Generates FEN position string from board array
        /// </summary>
        public static string GenerateFENFromBoard(ChessBoard board)
        {
            List<string> rows = new List<string>();
            for (int row = 0; row < 8; row++)
            {
                int emptyCount = 0;
                string rowFen = "";
                for (int col = 0; col < 8; col++)
                {
                    char piece = board[row, col];
                    if (piece == '.')
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            rowFen += emptyCount.ToString();
                            emptyCount = 0;
                        }
                        rowFen += piece;
                    }
                }
                if (emptyCount > 0)
                {
                    rowFen += emptyCount.ToString();
                }
                rows.Add(rowFen);
            }
            return string.Join("/", rows);
        }

        /// <summary>
        /// Converts UCI move (e.g., "e2e4") to Standard Algebraic Notation (e.g., "e4", "Nf3")
        /// </summary>
        public static string ConvertUCIToSAN(string uciMove, string fen, Func<ChessBoard, int, int, char, int, int, bool> canReachSquare,
            Func<ChessBoard, char, bool, int, int, List<(int row, int col)>> findAllPiecesOfSameType)
        {
            // Validate input
            if (string.IsNullOrEmpty(uciMove) || uciMove.Length < 4)
                return uciMove ?? "";

            ChessBoard board = ChessBoard.FromFEN(fen);

            string source = uciMove.Substring(0, 2);
            string dest = uciMove.Substring(2, 2);

            // Validate source and dest
            if (source.Length != 2 || dest.Length != 2)
                return uciMove;
            if (source[0] < 'a' || source[0] > 'h' || source[1] < '1' || source[1] > '8')
                return uciMove;
            if (dest[0] < 'a' || dest[0] > 'h' || dest[1] < '1' || dest[1] > '8')
                return uciMove;

            int srcFile = source[0] - 'a';
            int srcRank = 8 - (source[1] - '0');
            int destFile = dest[0] - 'a';
            int destRank = 8 - (dest[1] - '0');

            char piece = board[srcRank, srcFile];

            // Guard against empty source square (board state may be desynced)
            if (piece == '.' || PieceHelper.GetPieceType(piece) == PieceType.None)
            {
                // Return the UCI move as-is if we can't convert it properly
                // This prevents broken notation like ".b1"
                return uciMove;
            }

            string pieceLetter = "";
            if (PieceHelper.GetPieceType(piece) != PieceType.Pawn)
            {
                pieceLetter = piece.ToString().ToUpper();
                if (pieceLetter == "K") pieceLetter = "K";
                else if (pieceLetter == "Q") pieceLetter = "Q";
                else if (pieceLetter == "R") pieceLetter = "R";
                else if (pieceLetter == "B") pieceLetter = "B";
                else if (pieceLetter == "N") pieceLetter = "N";
            }

            bool isCapture = board[destRank, destFile] != '.';

            // Check for castling - king moves 2 squares AND it's not a capture
            // (After castling, king on c1 capturing on a1 moves 2 squares but isn't castling!)
            if (PieceHelper.GetPieceType(piece) == PieceType.King && !isCapture)
            {
                if (Math.Abs(destFile - srcFile) == 2)
                {
                    // Additional check: king must be on starting file (e-file = 4)
                    // to be a valid castling move
                    if (srcFile == 4)
                    {
                        return destFile > srcFile ? "O-O" : "O-O-O";
                    }
                }
            }

            // For pawn captures, we need to show source file
            if (PieceHelper.GetPieceType(piece) == PieceType.Pawn && isCapture)
            {
                pieceLetter = source[0].ToString();
            }

            // Disambiguation
            string disambiguator = "";
            if (PieceHelper.GetPieceType(piece) != PieceType.Pawn && PieceHelper.GetPieceType(piece) != PieceType.King)
            {
                char pieceLower = char.ToLower(piece);
                bool isWhite = char.IsUpper(piece);
                var sameTypePieces = findAllPiecesOfSameType(board, pieceLower, isWhite, destRank, destFile);

                if (sameTypePieces.Count > 1)
                {
                    bool sameFile = sameTypePieces.Any(p => p.col == srcFile && (p.row != srcRank || p.col != srcFile));
                    bool sameRank = sameTypePieces.Any(p => p.row == srcRank && (p.row != srcRank || p.col != srcFile));

                    if (!sameFile)
                    {
                        disambiguator = source[0].ToString();
                    }
                    else if (!sameRank)
                    {
                        disambiguator = source[1].ToString();
                    }
                    else
                    {
                        disambiguator = source;
                    }
                }
            }

            string captureSymbol = isCapture ? "x" : "";
            string promotion = "";
            if (uciMove.Length > 4)
            {
                promotion = "=" + uciMove[4].ToString().ToUpper();
            }

            return $"{pieceLetter}{disambiguator}{captureSymbol}{dest}{promotion}";
        }

        /// <summary>
        /// Converts a full PV line (space-separated UCI moves) into SAN sequence
        /// </summary>
        public static string ConvertFullPvToSan(string pv, string startingFen,
            ApplyUciMoveDelegate applyUciMove,
            Func<ChessBoard, int, int, char, int, int, bool> canReachSquare,
            Func<ChessBoard, char, bool, int, int, List<(int row, int col)>> findAllPiecesOfSameType)
        {
            if (string.IsNullOrWhiteSpace(pv)) return "";
            var tokens = pv.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Parse starting FEN
            string[] fenParts = startingFen.Split(' ');
            string side = fenParts.Length > 1 ? fenParts[1] : "w";
            string castling = fenParts.Length > 2 ? fenParts[2] : "-";
            string ep = fenParts.Length > 3 ? fenParts[3] : "-";

            // Build board using shared parser
            ChessBoard board = ChessBoard.FromFEN(startingFen);

            List<string> sanMoves = new List<string>();
            string currentSide = side;

            foreach (var token in tokens)
            {
                if (!IsValidUCIMove(token))
                {
                    sanMoves.Add(token);
                    continue;
                }

                // Build temporary FEN
                string posFen = GenerateFENFromBoard(board);
                string tempFen = $"{posFen} {currentSide} {castling} {ep} 0 1";

                // Convert to SAN
                string sanMove = ConvertUCIToSAN(token, tempFen, canReachSquare, findAllPiecesOfSameType);
                sanMoves.Add(sanMove);

                // Apply move and update state
                applyUciMove(board, token, ref castling, ref ep);
                currentSide = (currentSide == "w") ? "b" : "w";
            }

            return string.Join(" ", sanMoves);
        }

        /// <summary>
        /// Converts PV with fallback to best move
        /// </summary>
        public static string ConvertPvToSan(List<string> pvs, int index, string fallbackMove, string completeFen,
            Func<string, string, string> convertFullPvToSan)
        {
            if (pvs != null && pvs.Count > index && !string.IsNullOrWhiteSpace(pvs[index]))
            {
                return convertFullPvToSan(pvs[index], completeFen);
            }
            return convertFullPvToSan(fallbackMove, completeFen);
        }

        /// <summary>
        /// Extracts and validates UCI move from PV string
        /// </summary>
        public static string? ExtractValidUciMove(List<string> pvs, int index, string? fallback)
        {
            if (pvs != null && pvs.Count > index && !string.IsNullOrEmpty(pvs[index]))
            {
                var tokens = pvs[index].Split(' ');
                if (tokens.Length > 0 && !string.IsNullOrEmpty(tokens[0]))
                {
                    if (IsValidUCIMove(tokens[0]))
                        return tokens[0];
                }
            }
            return IsValidUCIMove(fallback) ? fallback : null;
        }
    }
}