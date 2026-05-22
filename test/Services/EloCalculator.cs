namespace ChessDroid.Services
{
    public static class EloCalculator
    {
        private const int KFactor = 10;

        public static double ExpectedScore(int myElo, int opponentElo)
            => 1.0 / (1.0 + Math.Pow(10.0, (opponentElo - myElo) / 400.0));

        /// <summary>
        /// FIDE Elo change for one player in a single game.
        /// score: 1.0 = win, 0.5 = draw, 0.0 = loss
        /// </summary>
        public static int EloChange(int myElo, int opponentElo, double score)
        {
            double expected = ExpectedScore(myElo, opponentElo);
            return (int)Math.Round(KFactor * (score - expected));
        }

        public static string FormatDelta(int delta)
            => delta >= 0 ? $"+{delta}" : $"{delta}";
    }
}
