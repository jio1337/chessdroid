namespace ChessDroid.Models
{
    public class TournamentEngineEntry
    {
        public string FileName    { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public int    Elo         { get; set; }
        public string Label => Elo > 0 ? $"{DisplayName} ({Elo})" : DisplayName;
    }

    public class TournamentPairing
    {
        public TournamentEngineEntry Engine1 { get; set; } = new();
        public TournamentEngineEntry Engine2 { get; set; } = new();
    }

    public class TournamentStanding
    {
        public TournamentEngineEntry Engine   { get; set; } = new();
        public double Points   { get; set; }
        public int    Wins     { get; set; }
        public int    Draws    { get; set; }
        public int    Losses   { get; set; }
        public int    Played   { get; set; }
        public string PointStr => Points % 1 == 0 ? $"{Points:0}" : $"{Points:0.0}";
    }
}
