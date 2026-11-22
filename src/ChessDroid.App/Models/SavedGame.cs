namespace ChessDroid.Models
{
    public class SavedGame
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string White { get; set; } = "?";
        public string Black { get; set; } = "?";
        public string Event { get; set; } = "Chess Analysis";
        public string Result { get; set; } = "*";
        public DateTime SavedAt { get; set; } = DateTime.Now;
        public string EngineName { get; set; } = "";
        public int EngineDepth { get; set; }
        public double? WhiteAccuracy { get; set; }
        public double? BlackAccuracy { get; set; }
        public bool HasClassification { get; set; }
        public string Opening { get; set; } = "";
        public string Pgn { get; set; } = "";
    }
}
