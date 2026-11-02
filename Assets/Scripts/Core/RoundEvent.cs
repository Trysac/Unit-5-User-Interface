namespace TargetToss.Core
{
    public enum RoundEventKind
    {
        /// <summary>A target was tossed into the field.</summary>
        Spawned,
        /// <summary>A good or bonus target was popped; <see cref="RoundEvent.Points"/> says how many points.</summary>
        Popped,
        /// <summary>A power-up target was popped and its power started.</summary>
        PowerUp,
        /// <summary>A power ran out.</summary>
        PowerEnded,
        /// <summary>A bad target was clicked.</summary>
        Hazard,
        /// <summary>A bomb was clicked (the round ends).</summary>
        Bomb,
        /// <summary>A good target fell out of the field without being popped.</summary>
        Fell,
        /// <summary>A click hit nothing.</summary>
        Whiff,
        /// <summary>The multiplier went up; <see cref="RoundEvent.Value"/> is the new multiplier.</summary>
        MultiplierUp,
        /// <summary>A combo of 2 or more ended; <see cref="RoundEvent.Value"/> is its length.</summary>
        ComboBroken,
        /// <summary>A life was lost; <see cref="RoundEvent.Value"/> is the lives left.</summary>
        LifeLost,
        /// <summary>Seconds were added (positive) or taken off (negative) the clock.</summary>
        TimeChanged,
        /// <summary>The round ended; see <see cref="Round.EndReason"/>.</summary>
        Over,
    }

    /// <summary>Something that happened in a round, for the sounds, particles and HUD.</summary>
    public readonly struct RoundEvent
    {
        public readonly RoundEventKind Kind;
        public readonly TargetKind Target;
        public readonly Vec2 Position;
        public readonly int Points;
        public readonly float Value;

        public RoundEvent(RoundEventKind kind, TargetKind target, Vec2 position, int points = 0, float value = 0f)
        {
            Kind = kind;
            Target = target;
            Position = position;
            Points = points;
            Value = value;
        }

        public override string ToString() => $"{Kind} {Target} {Points} {Value}";
    }
}
