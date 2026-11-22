namespace ChessDroid.Services
{
    public static class EvaluationParser
    {
        public static double? ParseNullable(string evalStr)
        {
            if (string.IsNullOrEmpty(evalStr))
                return null;

            if (evalStr.Contains("Mate") || evalStr.StartsWith("M") || evalStr.StartsWith("+M") || evalStr.StartsWith("-M"))
            {
                string numPart = evalStr
                    .Replace("Mate in", "")
                    .Replace("M", "")
                    .Replace("+", "")
                    .Replace("-", "")
                    .Trim();

                if (int.TryParse(numPart, out int mateIn))
                {
                    double mateScore = Math.Max(10, 15 - mateIn * 0.5);
                    bool isNegative = evalStr.Contains("-");
                    return isNegative ? -mateScore : mateScore;
                }
                return evalStr.Contains("-") ? -12 : 12;
            }

            evalStr = evalStr.Replace("+", "").Trim();

            if (double.TryParse(evalStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double eval))
                return eval;

            if (double.TryParse(evalStr, out eval))
                return eval;

            return null;
        }

        public static double Parse(string evalStr) => ParseNullable(evalStr) ?? 0;
    }
}
