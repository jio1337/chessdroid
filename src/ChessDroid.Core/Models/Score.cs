using System.Globalization;

namespace ChessDroid.Models
{
    /// <summary>
    /// A chess engine evaluation: either a centipawn score or a mate-in-N count, never both.
    /// CHESSDROID_AUDIT.md §5.1: evaluations used to travel as hand-formatted strings
    /// ("+0.35", "Mate in -3"), built independently in 3 places inside ChessEngineService —
    /// this centralizes that one format into a single ToString(). Exactly one of Cp/Mate is
    /// set; both represent the value already from the POV the caller wants displayed (the
    /// White/Black flip happens before constructing a Score, same as it did before this
    /// struct existed — this only replaces the final formatting step, not the POV logic).
    /// </summary>
    public readonly record struct Score(int? Cp, int? Mate)
    {
        public static Score FromCentipawns(double displayCentipawns) => new((int)Math.Round(displayCentipawns), null);
        public static Score FromMate(int displayMateIn) => new(null, displayMateIn);

        public override string ToString()
        {
            if (Mate.HasValue)
            {
                int m = Mate.Value;
                return m > 0 ? $"Mate in +{m}" : m < 0 ? $"Mate in {m}" : "Mate in 0";
            }

            double pawns = (Cp ?? 0) / 100.0;
            return (pawns >= 0 ? "+" : "") + pawns.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}
