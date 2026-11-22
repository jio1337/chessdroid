namespace ChessDroid.Services
{
    public static class EloCalculator
    {
        private const int KFactor = 10;
        private const int KFactorChessdroid = 32;

        public static double ExpectedScore(int myElo, int opponentElo)
            => 1.0 / (1.0 + Math.Pow(10.0, (opponentElo - myElo) / 400.0));

        /// <summary>FIDE Elo change (K=10). score: 1.0=win, 0.5=draw, 0.0=loss</summary>
        public static int EloChange(int myElo, int opponentElo, double score)
        {
            double expected = ExpectedScore(myElo, opponentElo);
            return (int)Math.Round(KFactor * (score - expected));
        }

        /// <summary>Chessdroid internal rating change (K=32). Moves faster than FIDE for a small engine pool.</summary>
        public static int EloChangeChessdroid(int myElo, int opponentElo, double score)
        {
            double expected = ExpectedScore(myElo, opponentElo);
            return (int)Math.Round(KFactorChessdroid * (score - expected));
        }

        public static string FormatDelta(int delta)
            => delta >= 0 ? $"+{delta}" : $"{delta}";
    }
}
