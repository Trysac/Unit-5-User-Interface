namespace TargetToss.Core
{
    /// <summary>
    /// Points with a combo multiplier: every safe pop adds one to the combo and the multiplier is
    /// 1 + combo / 5, up to x5, so the 5th pop in a row scores double. A mistake breaks the combo.
    /// </summary>
    public sealed class ComboScore
    {
        public const int PopsPerStep = 5;
        public const int MaxMultiplier = 5;

        public int Points { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }

        public int Multiplier => MultiplierFor(Combo);

        public static int MultiplierFor(int combo)
        {
            int multiplier = 1 + combo / PopsPerStep;
            return multiplier > MaxMultiplier ? MaxMultiplier : multiplier;
        }

        /// <summary>Counts one more pop in the combo and returns the points it scored.</summary>
        public int Pop(int basePoints)
        {
            Combo++;
            if (Combo > BestCombo)
            {
                BestCombo = Combo;
            }
            int gained = basePoints * Multiplier;
            Points += gained;
            return gained;
        }

        /// <summary>Adds points outside the combo (they are not multiplied).</summary>
        public void AddFlat(int points) => Points += points;

        /// <summary>Ends the combo; returns the combo that was broken.</summary>
        public int Break()
        {
            int broken = Combo;
            Combo = 0;
            return broken;
        }
    }
}
