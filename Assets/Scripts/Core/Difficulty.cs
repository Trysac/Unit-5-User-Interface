namespace TargetToss.Core
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
    }

    /// <summary>
    /// How a difficulty level tosses targets: how often (the interval shrinks from
    /// <see cref="StartInterval"/> to <see cref="MinInterval"/> over <see cref="RampSeconds"/>),
    /// the mix of kinds (relative weights), how hard they are thrown and how often two fly at once.
    /// </summary>
    public sealed class DifficultyTuning
    {
        public float StartInterval;
        public float MinInterval;
        public float RampSeconds;
        public float Gravity;
        public float PairChance;
        public int GoodWeight;
        public int BadWeight;
        public int BonusWeight;
        public int BombWeight;
        public int PowerUpWeight;

        public int TotalWeight => GoodWeight + BadWeight + BonusWeight + BombWeight + PowerUpWeight;

        /// <summary>Seconds between tosses <paramref name="seconds"/> into a round.</summary>
        public float IntervalAt(float seconds)
        {
            float t = RampSeconds <= 0f ? 1f : seconds / RampSeconds;
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return StartInterval + (MinInterval - StartInterval) * t;
        }

        public static DifficultyTuning For(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return new DifficultyTuning
                    {
                        StartInterval = 1.05f, MinInterval = 0.6f, RampSeconds = 60f, Gravity = 8.5f, PairChance = 0f,
                        GoodWeight = 70, BadWeight = 14, BonusWeight = 7, BombWeight = 3, PowerUpWeight = 6,
                    };
                case Difficulty.Hard:
                    return new DifficultyTuning
                    {
                        StartInterval = 0.7f, MinInterval = 0.3f, RampSeconds = 60f, Gravity = 10.5f, PairChance = 0.25f,
                        GoodWeight = 55, BadWeight = 24, BonusWeight = 8, BombWeight = 9, PowerUpWeight = 4,
                    };
                default:
                    return new DifficultyTuning
                    {
                        StartInterval = 0.85f, MinInterval = 0.42f, RampSeconds = 60f, Gravity = 9.5f, PairChance = 0.12f,
                        GoodWeight = 62, BadWeight = 20, BonusWeight = 7, BombWeight = 6, PowerUpWeight = 5,
                    };
            }
        }
    }
}
