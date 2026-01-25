namespace ChessDroid.Models
{
    /// <summary>
    /// Represents an opening book move with weight/priority information.
    /// </summary>
    public class BookMove
    {
        public string UciMove { get; set; } = "";
        public int Games { get; set; }
        public int Priority { get; set; }
        public double WinRate { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public string Source { get; set; } = "Book";
    }
}
