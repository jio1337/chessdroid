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
                string studyName   = tags.GetValueOrDefault("StudyName") ?? "Drills";

                // Some NoseKnowsAll studies end with an author-credit chapter reusing
                // chapter 1's position and no real moves — not a drill, drop it.
                if (chapterName.Equals("Thanks for enjoying this study!", StringComparison.OrdinalIgnoreCase))
                    continue;

                // A handful of diagram-only chapters (e.g. "Know what can mate!")
                // illustrate material combinations with no black king at all — an
                // illegal position the rules engine then misreads as stalemate.
                if (!HasOneKingPerSide(fen))
                    continue;

                // First { comment } in the moves section = description; strip Lichess arrow/square annotations
                string desc = "";
                var cm = _commentRx.Match(part);
                if (cm.Success)
                {
                    desc = cm.Groups[1].Value;
                    desc = Regex.Replace(desc, @"\[%c[as]l [^\]]*\]", "").Trim();
                }

                var fenParts = fen.Split(' ');
                bool whiteToMove = fenParts.Length < 2 || fenParts[1] == "w";

                chapters.Add(new EndgameChapter(studyName, chapterName, fen, desc, whiteToMove));
            }

            return chapters;
        }

        /// Returns all unique study names found across all loaded chapters.
        public static List<string> GetStudyNames(IEnumerable<EndgameChapter> chapters) =>
            chapters.Select(c => c.StudyName).Distinct().ToList();

        private static bool HasOneKingPerSide(string fen)
        {
            string placement = fen.Split(' ')[0];
            return placement.Count(c => c == 'K') == 1 && placement.Count(c => c == 'k') == 1;
        }
    }
}
