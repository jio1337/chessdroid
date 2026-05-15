namespace ChessDroid.Models
{
    public class BotSettings
    {
        public int SkillLevel { get; set; } = 8; // 1–20, maps directly to Stockfish Skill Level
        public bool BotPlaysWhite { get; set; } = false;
        public bool ChallengeMode { get; set; } = false;

        public int GetSkillLevel() => SkillLevel;

        public int GetMoveTimeMs() => SkillLevel switch
        {
            <= 4  => 400,
            <= 9  => 800,
            <= 14 => 1500,
            <= 17 => 2000,
            _     => 3000
        };

        public string GetDifficultyLabel() => $"Level {SkillLevel}";
    }
}
