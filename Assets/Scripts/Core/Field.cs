namespace TargetToss.Core
{
    /// <summary>The play area in world units: the camera shows about x -8.9..8.9 and y -5..5.</summary>
    public static class Field
    {
        /// <summary>Targets are tossed from just below the bottom edge...</summary>
        public const float LaunchY = -6.3f;
        /// <summary>...and are gone once they fall back below this line.</summary>
        public const float KillY = -6.8f;
        public const float SpawnHalfWidth = 6f;
        public const float LandHalfWidth = 6.6f;
        /// <summary>The highest points of the throws, below the HUD.</summary>
        public const float ApexMin = -0.8f;
        public const float ApexMax = 3.4f;
        public const float MaxSideSpeed = 4f;
    }
}
