namespace TargetToss.Core
{
    /// <summary>
    /// Counts game time and says how many targets are due, one every <see cref="Interval"/>
    /// seconds. It replaced the 2021 coroutine that waited <c>spawnRate</c> seconds between spawns; like
    /// that coroutine, an interval of zero or less spawns once per step.
    /// </summary>
    public sealed class SpawnClock
    {
        float elapsed;

        public SpawnClock(float interval) => Interval = interval;

        public float Interval { get; set; }

        /// <summary>Advances the clock by <paramref name="deltaTime"/> seconds and returns how many spawns are due.</summary>
        public int Advance(float deltaTime)
        {
            if (Interval <= 0f)
            {
                return 1;
            }
            elapsed += deltaTime;
            int due = 0;
            while (elapsed >= Interval)
            {
                elapsed -= Interval;
                due++;
            }
            return due;
        }

        public void Reset() => elapsed = 0f;
    }
}
