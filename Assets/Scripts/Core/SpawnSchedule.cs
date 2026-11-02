using System;
using System.Collections.Generic;

namespace TargetToss.Core
{
    /// <summary>
    /// Decides when targets are tossed, what they are and how they fly. Seeded, so a round with the
    /// same seed and difficulty tosses the same targets.
    /// </summary>
    public sealed class SpawnSchedule
    {
        readonly DifficultyTuning tuning;
        readonly Rng rng;
        readonly SpawnClock clock;
        int nextId = 1;

        public SpawnSchedule(DifficultyTuning tuning, Rng rng)
        {
            this.tuning = tuning;
            this.rng = rng;
            clock = new SpawnClock(tuning.IntervalAt(0f));
        }

        public float Gravity => tuning.Gravity;

        /// <summary>
        /// Moves the schedule <paramref name="deltaTime"/> seconds on, at <paramref name="roundSeconds"/>
        /// into the round, and adds the targets that are due to <paramref name="into"/>.
        /// </summary>
        public void Advance(float deltaTime, float roundSeconds, bool powerUpsAllowed, List<Target> into)
        {
            clock.Interval = tuning.IntervalAt(roundSeconds);
            int due = clock.Advance(deltaTime);
            for (int i = 0; i < due; i++)
            {
                TargetKind kind = PickKind(powerUpsAllowed);
                into.Add(Toss(kind));
                // A pair is always a second, harmless target, never two bombs.
                if (rng.Value() < tuning.PairChance)
                {
                    into.Add(Toss(rng.Value() < 0.7f ? TargetKind.Good : TargetKind.Bad));
                }
            }
        }

        public TargetKind PickKind(bool powerUpsAllowed)
        {
            int roll = rng.Range(0, tuning.TotalWeight);
            if ((roll -= tuning.GoodWeight) < 0) return TargetKind.Good;
            if ((roll -= tuning.BadWeight) < 0) return TargetKind.Bad;
            if ((roll -= tuning.BonusWeight) < 0) return TargetKind.Bonus;
            if ((roll -= tuning.BombWeight) < 0) return TargetKind.Bomb;
            if (!powerUpsAllowed) return TargetKind.Good;
            return rng.Value() < 0.5f ? TargetKind.SlowTime : TargetKind.Magnet;
        }

        /// <summary>A target thrown from below the field so that it peaks inside it and falls back out.</summary>
        public Target Toss(TargetKind kind)
        {
            float startX = rng.Range(-Field.SpawnHalfWidth, Field.SpawnHalfWidth);
            float apexMin = kind == TargetKind.Bonus ? 1.6f : Field.ApexMin;
            float apexY = rng.Range(apexMin, Field.ApexMax);
            float riseSpeed = MathF.Sqrt(2f * tuning.Gravity * (apexY - Field.LaunchY));
            float airTime = 2f * riseSpeed / tuning.Gravity;
            float landX = rng.Range(-Field.LandHalfWidth, Field.LandHalfWidth);
            float side = (landX - startX) / airTime;
            side = side > Field.MaxSideSpeed ? Field.MaxSideSpeed : side < -Field.MaxSideSpeed ? -Field.MaxSideSpeed : side;
            return new Target
            {
                Id = nextId++,
                Kind = kind,
                Position = new Vec2(startX, Field.LaunchY),
                Velocity = new Vec2(side, riseSpeed),
                Angle = rng.Range(0f, 360f),
                Spin = rng.Range(-200f, 200f),
                Variant = rng.Range(0, 3),
            };
        }

        /// <summary>The next free target id (for targets placed by hand).</summary>
        public int NextId() => nextId++;
    }
}
