using System.Diagnostics;
using System.Text.RegularExpressions;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    public static class PgnImportService
    {
        private static readonly Regex PgnEvalCommentRegex    = new(@"\[([+-]?\d+\.?\d*)\]", RegexOptions.Compiled);
        private static readonly Regex PgnMoveNumberRegex     = new(@"^\d+\.+$", RegexOptions.Compiled);
        private static readonly Regex PgnAttachedMoveRegex   = new(@"^\d+\.+(.+)$", RegexOptions.Compiled);
        private static readonly Regex StripMovesRegex        = new(@",?\s*\d+\.", RegexOptions.Compiled);

        public static Dictionary<string, string> ParseHeaders(string pgn)
        {
            var headers = new Dictionary<string, string>();
            foreach (var line in pgn.Split('\n'))
            {
                string l = line.Trim();
                if (!l.StartsWith("[") || !l.EndsWith("]")) continue;
                int keyEnd = l.IndexOf(' ');
                if (keyEnd <= 1) continue;
                string key = l.Substring(1, keyEnd - 1);
                int vs = l.IndexOf('"') + 1;
                int ve = l.LastIndexOf('"');
                if (vs > 0 && ve > vs)
                    headers[key] = l.Substring(vs, ve - vs);
            }
            return headers;
        }

        // Returns tokens: 'M'=move, 'N'=NAG ($3 etc), 'C'=comment text
        public static List<(char type, string value)> TokenizeMoveText(string moveText)
        {
            var tokens = new List<(char, string)>();
            int i = 0, len = moveText.Length;
            while (i < len)
            {
                char c = moveText[i];
                if (c == '{')
                {
                    int end = moveText.IndexOf('}', i + 1);
                    if (end < 0) end = len - 1;
                    tokens.Add(('C', moveText.Substring(i + 1, end - i - 1).Trim()));
                    i = end + 1;
                }
                else if (c == '(')
                {
                    int depth = 1; i++;
                    while (i < len && depth > 0)
                    {
                        if (moveText[i] == '(') depth++;
                        else if (moveText[i] == ')') depth--;
                        i++;
                    }
                }
                else if (c == ';')
                {
                    while (i < len && moveText[i] != '\n') i++;
                }
                else if (c == '$')
                {
                    int start = i++;
                    while (i < len && char.IsDigit(moveText[i])) i++;
                    tokens.Add(('N', moveText.Substring(start, i - start)));
                }
                else if (char.IsWhiteSpace(c))
                {
                    i++;
                }
                else
                {
                    int start = i;
                    while (i < len && !char.IsWhiteSpace(moveText[i]) &&
                           moveText[i] != '{' && moveText[i] != '(' &&
                           moveText[i] != '$' && moveText[i] != ';')
                        i++;
                    string token = moveText.Substring(start, i - start);
                    if (token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*")
                        continue;
                    if (PgnMoveNumberRegex.IsMatch(token)) continue;
                    var am = PgnAttachedMoveRegex.Match(token);
                    if (am.Success) token = am.Groups[1].Value.TrimStart('.');
                    if (!string.IsNullOrEmpty(token))
                        tokens.Add(('M', token));
                }
            }
            return tokens;
        }

        public static string? ConvertSanToUci(string san, string fen)
        {
            try
            {
                san = san.Replace("+", "").Replace("#", "").Replace("!", "").Replace("?", "");

                string[] fenParts = fen.Split(' ');
                bool isWhiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                string enPassantSquare = fenParts.Length > 3 ? fenParts[3] : "-";

                if (san == "O-O"  || san == "0-0")   return isWhiteToMove ? "e1g1" : "e8g8";
                if (san == "O-O-O"|| san == "0-0-0") return isWhiteToMove ? "e1c1" : "e8c8";

                char pieceType = 'P';
                int idx = 0;
                if (san.Length > 0 && char.IsUpper(san[0]) && san[0] != 'O')
                {
                    pieceType = san[0];
                    idx = 1;
                }

                char? promotion = null;
                if (san.Contains('='))
                {
                    int eqIdx = san.IndexOf('=');
                    if (eqIdx + 1 < san.Length) promotion = char.ToLower(san[eqIdx + 1]);
                    san = san.Substring(0, eqIdx);
                }
                else if (san.Length >= 2 && char.IsUpper(san[san.Length - 1]) && san[san.Length - 1] != 'O')
                {
                    promotion = char.ToLower(san[san.Length - 1]);
                    san = san.Substring(0, san.Length - 1);
                }

                int destIdx = san.Length - 2;
                while (destIdx >= idx)
                {
                    if (destIdx + 1 < san.Length &&
                        san[destIdx] >= 'a' && san[destIdx] <= 'h' &&
                        san[destIdx + 1] >= '1' && san[destIdx + 1] <= '8')
                        break;
                    destIdx--;
                }
                if (destIdx < idx || destIdx + 1 >= san.Length) return null;

                string destSquare = san.Substring(destIdx, 2);
                int destCol = destSquare[0] - 'a';
                int destRow = 7 - (destSquare[1] - '1');

                char? disambigFile = null;
                char? disambigRank = null;
                if (destIdx > idx)
                {
                    string middle = san.Substring(idx, destIdx - idx).Replace("x", "");
                    foreach (char ch in middle)
                    {
                        if (ch >= 'a' && ch <= 'h') disambigFile = ch;
                        else if (ch >= '1' && ch <= '8') disambigRank = ch;
                    }
                }

                var board = ChessBoard.FromFEN(fen);
                char pieceChar = isWhiteToMove ? pieceType : char.ToLower(pieceType);
                if (pieceType == 'P') pieceChar = isWhiteToMove ? 'P' : 'p';

                var candidates = ChessRulesService.FindAllPiecesOfSameTypeWithEnPassant(
                    board, char.ToLower(pieceChar), isWhiteToMove, destRow, destCol, enPassantSquare);

                foreach (var (row, col) in candidates)
                {
                    char fileChar = (char)('a' + col);
                    char rankChar = (char)('1' + (7 - row));
                    if (disambigFile.HasValue && fileChar != disambigFile.Value) continue;
                    if (disambigRank.HasValue && rankChar != disambigRank.Value) continue;
                    if (ChessRulesService.CanReachSquareWithEnPassant(board, row, col, pieceChar, destRow, destCol, enPassantSquare))
                    {
                        string uci = $"{fileChar}{rankChar}{destSquare}";
                        if (promotion.HasValue) uci += promotion.Value;
                        return uci;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConvertSanToUci error: {ex.Message} for {san}");
                return null;
            }
        }

        public static string StripMovesFromOpeningName(string name)
        {
            var m = StripMovesRegex.Match(name);
            return m.Success ? name[..m.Index].TrimEnd(',', ' ') : name;
        }

        public static string StripAnnotationSymbols(string san)
        {
            if (string.IsNullOrEmpty(san)) return san;
            string[] symbols = { "!!", "??", "!?", "?!", "!", "?" };
            foreach (var symbol in symbols)
            {
                if (san.EndsWith(symbol))
                {
                    san = san.Substring(0, san.Length - symbol.Length);
                    break;
                }
            }
            return san;
        }

        public static string GetNagForSymbol(string symbol) => symbol switch
        {
            "!!" => "$3", "!" => "$1", "!?" => "$5",
            "?!" => "$6", "?" => "$2", "??" => "$4",
            _ => ""
        };

        public static string GetSymbolForNag(string nag) => nag switch
        {
            "$3" => "!!", "$1" => "!", "$5" => "!?",
            "$6" => "?!", "$2" => "?", "$4" => "??",
            _ => ""
        };

        public static MoveQualityAnalyzer.MoveQuality GetQualityForSymbol(string symbol) => symbol switch
        {
            "!!" => MoveQualityAnalyzer.MoveQuality.Brilliant,
            "!"  => MoveQualityAnalyzer.MoveQuality.Precise,
            "?!" => MoveQualityAnalyzer.MoveQuality.Inaccuracy,
            "?"  => MoveQualityAnalyzer.MoveQuality.Mistake,
            "??" => MoveQualityAnalyzer.MoveQuality.Blunder,
            _    => MoveQualityAnalyzer.MoveQuality.Best
        };

        public static string GetInlineSymbol(string san)
        {
            if (string.IsNullOrEmpty(san)) return "";
            string[] symbols = { "!!", "??", "!?", "?!", "!", "?" };
            foreach (var s in symbols)
                if (san.EndsWith(s)) return s;
            return "";
        }

        public static string BuildPgnComment(MoveReviewResult result)
        {
            string eval = $"[{result.EvalAfter.ToString("+0.00;-0.00", System.Globalization.CultureInfo.InvariantCulture)}]";
            string label = result.Quality switch
            {
                MoveQualityAnalyzer.MoveQuality.Brilliant  => "Brilliant",
                MoveQualityAnalyzer.MoveQuality.Precise    => "Precise",
                MoveQualityAnalyzer.MoveQuality.Best       => "Best",
                MoveQualityAnalyzer.MoveQuality.Excellent  => "Excellent",
                MoveQualityAnalyzer.MoveQuality.Good       => "Good",
                MoveQualityAnalyzer.MoveQuality.Book       => "Book",
                MoveQualityAnalyzer.MoveQuality.Inaccuracy => "Inaccuracy",
                MoveQualityAnalyzer.MoveQuality.Mistake    => "Mistake",
                MoveQualityAnalyzer.MoveQuality.Blunder    => "Blunder",
                MoveQualityAnalyzer.MoveQuality.Forced     => "Forced",
                _ => "Best"
            };
            return $"{{ {eval} {label} }}";
        }

        public static double? ParseEvalFromComment(string comment)
        {
            var m = PgnEvalCommentRegex.Match(comment);
            if (!m.Success) return null;
            return double.TryParse(m.Groups[1].Value.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : (double?)null;
        }

        public static MoveQualityAnalyzer.MoveQuality ParseQualityFromComment(string comment)
        {
            if (comment.Contains("Brilliant"))  return MoveQualityAnalyzer.MoveQuality.Brilliant;
            if (comment.Contains("Precise"))    return MoveQualityAnalyzer.MoveQuality.Precise;
            if (comment.Contains("Blunder"))    return MoveQualityAnalyzer.MoveQuality.Blunder;
            if (comment.Contains("Mistake"))    return MoveQualityAnalyzer.MoveQuality.Mistake;
            if (comment.Contains("Inaccuracy")) return MoveQualityAnalyzer.MoveQuality.Inaccuracy;
            if (comment.Contains("Book"))       return MoveQualityAnalyzer.MoveQuality.Book;
            if (comment.Contains("Excellent"))  return MoveQualityAnalyzer.MoveQuality.Excellent;
            if (comment.Contains("Forced"))     return MoveQualityAnalyzer.MoveQuality.Forced;
            if (comment.Contains("Good"))       return MoveQualityAnalyzer.MoveQuality.Good;
            return MoveQualityAnalyzer.MoveQuality.Best;
        }

        public static string SerializeCachedAnalysis(CachedAnalysis ca)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"[%cda d={ca.Depth}");
            if (!string.IsNullOrEmpty(ca.BestMove))   sb.Append($";b={ca.BestMove}");
            if (!string.IsNullOrEmpty(ca.Evaluation)) sb.Append($";e={ca.Evaluation}");
            if (ca.PVs.Count > 0)         sb.Append($";v={string.Join("~", ca.PVs)}");
            if (ca.Evaluations.Count > 0) sb.Append($";f={string.Join("~", ca.Evaluations)}");
            if (ca.WDL != null)           sb.Append($";w={ca.WDL.Win}/{ca.WDL.Draw}/{ca.WDL.Loss}");
            sb.Append("]");
            return $"{{ {sb} }}";
        }

        public static CachedAnalysis? DeserializeCachedAnalysis(string comment)
        {
            if (!comment.StartsWith("[%cda ") || !comment.EndsWith("]")) return null;
            string inner = comment.Substring(6, comment.Length - 7);
            var ca = new CachedAnalysis();
            foreach (var field in inner.Split(';'))
            {
                int eq = field.IndexOf('=');
                if (eq < 0) continue;
                string key = field.Substring(0, eq);
                string val = field.Substring(eq + 1);
                switch (key)
                {
                    case "d": if (int.TryParse(val, out int d)) ca.Depth = d; break;
                    case "b": ca.BestMove = val; break;
                    case "e": ca.Evaluation = val; break;
                    case "v": ca.PVs = val.Split('~').Where(x => !string.IsNullOrEmpty(x)).ToList(); break;
                    case "f": ca.Evaluations = val.Split('~').Where(x => !string.IsNullOrEmpty(x)).ToList(); break;
                    case "w":
                        var wp = val.Split('/');
                        if (wp.Length == 3 &&
                            int.TryParse(wp[0], out int win) &&
                            int.TryParse(wp[1], out int draw) &&
                            int.TryParse(wp[2], out int loss))
                            ca.WDL = new WDLInfo(win, draw, loss);
                        break;
                }
            }
            return ca.Depth > 0 ? ca : null;
        }
    }
}
