using System.Globalization;
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
                if (!l.StartsWith('[') || !l.EndsWith(']')) continue;
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
                    tokens.Add(('(', ""));
                    i++;
                }
                else if (c == ')')
                {
                    tokens.Add((')', ""));
                    i++;
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
                           moveText[i] != '{' && moveText[i] != '(' && moveText[i] != ')' &&
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

        /// <summary>Delegates to the Core converter (this used to be a second, identical copy).</summary>
        public static string? ConvertSanToUci(string san, string fen) => ChessNotationService.ConvertSanToUci(san, fen);

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
                if (san.EndsWith(symbol, StringComparison.Ordinal))
                {
                    san = san.Substring(0, san.Length - symbol.Length);
                    break;
                }
            }
            return san;
        }

        /// <summary>
        /// The SAN as written, with its check mark set from the position it leads to:
        /// "#" for mate, "+" for check, none otherwise; annotation symbols ("!", "?!"…) are kept.
        /// Old game records often mark mate with a plain "+" (Bock–Mackenzie 1874, 13...Rxd4+),
        /// and some PGNs leave checks unmarked.
        /// </summary>
        public static string NormalizeCheckSuffix(string san, string fenAfter)
        {
            try
            {
                if (string.IsNullOrEmpty(san)) return san;
                string symbol = GetInlineSymbol(san);
                string core = StripAnnotationSymbols(san).TrimEnd('+', '#');

                string[] parts = fenAfter.Split(' ');
                var board = ChessBoard.FromFEN(fenAfter);
                bool sideToMoveWhite = parts.Length > 1 && parts[1] == "w";
                if (!Services.MoveGenerator.IsKingInCheck(board, sideToMoveWhite)) return core + symbol;

                (int Row, int Col)? ep = parts.Length > 3 && parts[3].Length == 2 && parts[3] != "-"
                    ? ('8' - parts[3][1], parts[3][0] - 'a') : null;
                bool hasMove = Services.MoveGenerator.GetAllLegalMovesUci(board, sideToMoveWhite,
                    new CastlingState(false, false, false, false), ep).Count > 0;
                return core + (hasMove ? "+" : "#") + symbol;
            }
            catch
            {
                return san; // keep the PGN's own text if the position can't be read
            }
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
                if (san.EndsWith(s, StringComparison.Ordinal)) return s;
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
            sb.Append(CultureInfo.InvariantCulture, $"[%cda d={ca.Depth}");
            if (!string.IsNullOrEmpty(ca.BestMove))   sb.Append(CultureInfo.InvariantCulture, $";b={ca.BestMove}");
            if (!string.IsNullOrEmpty(ca.Evaluation)) sb.Append(CultureInfo.InvariantCulture, $";e={ca.Evaluation}");
            if (ca.PVs.Count > 0)         sb.Append(CultureInfo.InvariantCulture, $";v={string.Join("~", ca.PVs)}");
            if (ca.Evaluations.Count > 0) sb.Append(CultureInfo.InvariantCulture, $";f={string.Join("~", ca.Evaluations)}");
            if (ca.WDL != null)           sb.Append(CultureInfo.InvariantCulture, $";w={ca.WDL.Win}/{ca.WDL.Draw}/{ca.WDL.Loss}");
            sb.Append(']');
            return $"{{ {sb} }}";
        }

        public static CachedAnalysis? DeserializeCachedAnalysis(string comment)
        {
            if (!comment.StartsWith("[%cda ", StringComparison.Ordinal) || !comment.EndsWith(']')) return null;
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
