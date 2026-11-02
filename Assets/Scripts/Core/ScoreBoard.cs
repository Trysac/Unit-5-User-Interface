namespace TargetToss.Core
{
    /// <summary>The points scored in one round.</summary>
    public sealed class ScoreBoard
    {
        public int Points { get; private set; }

        /// <summary>Adds <paramref name="points"/> (negative values subtract) and returns the new total.</summary>
        public int Add(int points)
        {
            Points += points;
            return Points;
        }

        public void Reset() => Points = 0;

        /// <summary>The label the HUD shows.</summary>
        public string Label => $"Score: {Points}";
    }
}
