namespace TargetToss.Core
{
    /// <summary>What a tossed target is, and so what clicking it or letting it fall does.</summary>
    public enum TargetKind
    {
        /// <summary>Pop it for points; letting it fall breaks the combo (and costs a life in Lives mode).</summary>
        Good,
        /// <summary>Leave it: popping it breaks the combo and costs a life, or 3 seconds in Timed mode.</summary>
        Bad,
        /// <summary>A small, fast star worth five good targets (and 2 seconds in Timed mode).</summary>
        Bonus,
        /// <summary>Popping it ends the round.</summary>
        Bomb,
        /// <summary>A power-up that slows every target down for a while.</summary>
        SlowTime,
        /// <summary>A power-up that turns every click into a wide pull that collects only the safe targets.</summary>
        Magnet,
    }

    public static class TargetKinds
    {
        public static bool IsPowerUp(this TargetKind kind) => kind == TargetKind.SlowTime || kind == TargetKind.Magnet;

        /// <summary>Targets that punish a click.</summary>
        public static bool IsHazard(this TargetKind kind) => kind == TargetKind.Bad || kind == TargetKind.Bomb;

        /// <summary>The collision radius in world units.</summary>
        public static float Radius(this TargetKind kind)
        {
            switch (kind)
            {
                case TargetKind.Bonus: return 0.42f;
                case TargetKind.Bomb: return 0.6f;
                case TargetKind.SlowTime:
                case TargetKind.Magnet: return 0.5f;
                default: return 0.56f;
            }
        }
    }

    /// <summary>One target in flight. Positions are in world units, velocities in units per second.</summary>
    public sealed class Target
    {
        public int Id;
        public TargetKind Kind;
        public Vec2 Position;
        public Vec2 Velocity;
        /// <summary>Rotation in degrees and spin in degrees per second, for the view.</summary>
        public float Angle;
        public float Spin;
        /// <summary>A free 0..2 number the view uses to pick a colour for good targets.</summary>
        public int Variant;
        public float Radius => Kind.Radius();
    }
}
