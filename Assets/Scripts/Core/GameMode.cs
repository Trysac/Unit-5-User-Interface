namespace TargetToss.Core
{
    public enum GameMode
    {
        /// <summary>Score as much as possible in 60 seconds; hazards cost time.</summary>
        Timed,
        /// <summary>No clock: the round lasts until the last of three lives is lost.</summary>
        Lives,
    }

    /// <summary>The numbers that differ between the two modes.</summary>
    public static class ModeRules
    {
        public const float RoundSeconds = 60f;
        public const float BadPenaltySeconds = 3f;
        public const float BonusSeconds = 2f;
        public const int StartingLives = 3;
    }
}
