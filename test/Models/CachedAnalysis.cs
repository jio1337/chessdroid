using ChessDroid.Services;

namespace ChessDroid.Models
{
    public class CachedAnalysis
    {
        public string BestMove { get; set; } = "";
        public string Evaluation { get; set; } = "";
        public List<string> PVs { get; set; } = new();
        public List<string> Evaluations { get; set; } = new();
        public WDLInfo? WDL { get; set; }
        public int Depth { get; set; }
    }
}
