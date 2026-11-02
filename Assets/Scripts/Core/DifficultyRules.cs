namespace TargetToss.Core
{
    /// <summary>How the menu's difficulty buttons (1 easy, 2 normal, 3 hard) change the round.</summary>
    public static class DifficultyRules
    {
        public const int Easiest = 1;
        public const int Hardest = 3;

        /// <summary>
        /// Seconds between targets: one per second on easy, two on normal, three on hard. Levels
        /// outside 1..3 are clamped.
        /// </summary>
        public static float SpawnInterval(int level)
        {
            int clamped = level < Easiest ? Easiest : level > Hardest ? Hardest : level;
            return 1f / clamped;
        }
    }
}
