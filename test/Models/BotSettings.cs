namespace ChessDroid.Models
{
    public enum BotDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert,
        Master
    }

    public class BotSettings
    {
        public BotDifficulty Difficulty { get; set; } = BotDifficulty.Medium;
        public bool BotPlaysWhite { get; set; } = false;

        public int GetSkillLevel() => Difficulty switch
        {
            BotDifficulty.Easy => 3,
            BotDifficulty.Medium => 8,
            BotDifficulty.Hard => 14,
            BotDifficulty.Expert => 18,
            BotDifficulty.Master => 20,
            _ => 10
        };

        public int GetMoveTimeMs() => Difficulty switch
        {
            BotDifficulty.Easy => 500,
            BotDifficulty.Medium => 1000,
            BotDifficulty.Hard => 1500,
            BotDifficulty.Expert => 2000,
            BotDifficulty.Master => 3000,
            _ => 1000
        };

        public string GetDifficultyLabel() => Difficulty switch
        {
            BotDifficulty.Easy => "Easy (Skill 3)",
            BotDifficulty.Medium => "Medium (Skill 8)",
            BotDifficulty.Hard => "Hard (Skill 14)",
            BotDifficulty.Expert => "Expert (Skill 18)",
            BotDifficulty.Master => "Master (Skill 20)",
            _ => "Medium"
        };
    }
}
