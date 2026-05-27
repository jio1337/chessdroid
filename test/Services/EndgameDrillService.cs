using System.Text.RegularExpressions;

namespace ChessDroid.Services
{
    public record EndgameChapter(
        string StudyName,
        string ChapterName,
        string Fen,
        string Description,
        bool   WhiteToMove
    );

    public static class EndgameDrillService
    {
        private static readonly Regex _tagRx    = new(@"\[(\w+)\s+""([^""]*)""\]", RegexOptions.Compiled);
        private static readonly Regex _commentRx = new(@"\{([^}]+)\}", RegexOptions.Compiled);

        public static bool HasDrills(string folder) =>
            Directory.Exists(folder) && Directory.GetFiles(folder, "*.pgn").Length > 0;

        public static List<EndgameChapter> LoadFromFolder(string folder)
        {
            var result = new List<EndgameChapter>();
            if (!Directory.Exists(folder)) return result;
            foreach (var file in Directory.GetFiles(folder, "*.pgn"))
                result.AddRange(ParsePgn(file));
            return result;
        }

        public static List<EndgameChapter> ParsePgn(string path)
        {
            var chapters = new List<EndgameChapter>();
            string content = File.ReadAllText(path);

            // Split into individual chapters — each starts with [Event "
            var parts = Regex.Split(content, @"(?=\[Event "")");

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                var tags = new Dictionary<string, string>();
                foreach (Match m in _tagRx.Matches(part))
                    tags[m.Groups[1].Value] = m.Groups[2].Value;

                if (!tags.TryGetValue("FEN", out string? fen) || string.IsNullOrEmpty(fen))
                    continue;

                string chapterName = tags.GetValueOrDefault("ChapterName")
                                  ?? tags.GetValueOrDefault("Event")
                                  ?? "Position";
                string studyName   = tags.GetValueOrDefault("StudyName") ?? "Endgame Drills";

                // First { comment } in the moves section = description
                string desc = "";
                var cm = _commentRx.Match(part);
                if (cm.Success) desc = cm.Groups[1].Value.Trim();

                var fenParts = fen.Split(' ');
                bool whiteToMove = fenParts.Length < 2 || fenParts[1] == "w";

                chapters.Add(new EndgameChapter(studyName, chapterName, fen, desc, whiteToMove));
            }

            return chapters;
        }

        /// Returns all unique study names found across all loaded chapters.
        public static List<string> GetStudyNames(IEnumerable<EndgameChapter> chapters) =>
            chapters.Select(c => c.StudyName).Distinct().ToList();
    }
}
