namespace ChessDroid.Models
{
    public class BotSettings
    {
        public int  EloTarget     { get; set; } = 1500;
        public bool BotPlaysWhite { get; set; } = false;
        public bool ChallengeMode { get; set; } = false;
        public string EngineFileName { get; set; } = "";

        // Fallback Skill Level for engines that don't support UCI_LimitStrength
        public int GetSkillLevel() => EloTarget switch
        {
            <= 1400 => 1,
            <= 1600 => 3,
            <= 1800 => 6,
            <= 2000 => 10,
            <= 2200 => 13,
            <= 2400 => 16,
            <= 2600 => 18,
            _       => 20
        };

        public int GetMoveTimeMs() => EloTarget switch
        {
            <= 1600 => 400,
            <= 2000 => 800,
            <= 2400 => 1500,
            <= 2800 => 2000,
            _       => 3000
        };

        public string GetDifficultyLabel() => $"{EloTarget} Elo";
    }
}
