using System.Collections.Generic;

namespace TargetToss.Core
{
    public enum RoundPhase
    {
        Playing,
        Over,
    }

    public enum EndReason
    {
        None,
        TimeUp,
        OutOfLives,
        Bomb,
    }

    /// <summary>
    /// One round from the first toss to the end: targets fly under gravity, clicks pop them, the
    /// combo multiplies the points, power-ups bend the rules for a few seconds, and the mode decides
    /// what ends it (the clock in Timed mode, the last life in Lives mode, a bomb in both).
    /// Engine-free and deterministic for a given seed.
    /// </summary>
    public sealed class Round
    {
        public const int GoodPoints = 10;
        public const int BonusPoints = 50;
        public const int PowerUpPoints = 5;
        /// <summary>Clicks this far outside a target still count, for fingers and gamepad cursors.</summary>
        public const float ClickSlack = 0.22f;
        public const float SlowSeconds = 5f;
        public const float SlowFactor = 0.45f;
        public const float MagnetSeconds = 6f;
        public const float MagnetRadius = 1.8f;

        readonly SpawnSchedule schedule;
        readonly List<Target> targets = new List<Target>();
        readonly List<RoundEvent> events = new List<RoundEvent>();
        readonly List<Target> hits = new List<Target>();

        public Round(GameMode mode, Difficulty difficulty, int seed)
        {
            Mode = mode;
            Difficulty = difficulty;
            Tuning = DifficultyTuning.For(difficulty);
            schedule = new SpawnSchedule(Tuning, new Rng(seed));
            Score = new ComboScore();
            Lives = mode == GameMode.Lives ? ModeRules.StartingLives : 0;
            TimeLeft = mode == GameMode.Timed ? ModeRules.RoundSeconds : 0f;
        }

        public GameMode Mode { get; }
        public Difficulty Difficulty { get; }
        public DifficultyTuning Tuning { get; }
        public ComboScore Score { get; }
        public RoundPhase Phase { get; private set; }
        public EndReason EndReason { get; private set; }

        /// <summary>Seconds played (real time: slow time does not stretch the clock).</summary>
        public float Elapsed { get; private set; }

        /// <summary>Seconds left in Timed mode (0 in Lives mode).</summary>
        public float TimeLeft { get; private set; }

        /// <summary>Lives left in Lives mode (0 in Timed mode).</summary>
        public int Lives { get; private set; }

        public float SlowLeft { get; private set; }
        public float MagnetLeft { get; private set; }
        public bool MagnetActive => MagnetLeft > 0f;

        /// <summary>How fast targets move compared with normal.</summary>
        public float TimeScale => SlowLeft > 0f ? SlowFactor : 1f;

        /// <summary>When false, no new targets are tossed (the tutorial and scripted scenes).</summary>
        public bool Spawning { get; set; } = true;

        public int Clicks { get; private set; }
        public int HitClicks { get; private set; }
        public int Popped { get; private set; }
        public int GoodFell { get; private set; }

        /// <summary>The share of clicks that popped something safe, 0..1.</summary>
        public float Accuracy => Clicks == 0 ? 0f : (float)HitClicks / Clicks;

        public bool IsOver => Phase == RoundPhase.Over;

        public IReadOnlyList<Target> Targets => targets;

        /// <summary>What happened since the last <see cref="ClearEvents"/>.</summary>
        public IReadOnlyList<RoundEvent> Events => events;

        public void ClearEvents() => events.Clear();

        /// <summary>Advances the round by <paramref name="deltaTime"/> real seconds.</summary>
        public void Step(float deltaTime)
        {
            if (IsOver || deltaTime <= 0f)
            {
                return;
            }
            Elapsed += deltaTime;
            TickPowers(deltaTime);

            float moved = deltaTime * TimeScale;
            if (Spawning)
            {
                int first = targets.Count;
                // No new power-up while one is running.
                schedule.Advance(moved, Elapsed, SlowLeft <= 0f && MagnetLeft <= 0f, targets);
                for (int i = first; i < targets.Count; i++)
                {
                    events.Add(new RoundEvent(RoundEventKind.Spawned, targets[i].Kind, targets[i].Position));
                }
            }

            float gravity = schedule.Gravity;
            for (int i = targets.Count - 1; i >= 0 && !IsOver; i--)
            {
                Target target = targets[i];
                target.Velocity = new Vec2(target.Velocity.X, target.Velocity.Y - gravity * moved);
                target.Position += target.Velocity * moved;
                target.Angle += target.Spin * moved;
                if (target.Position.Y < Field.KillY && target.Velocity.Y < 0f)
                {
                    targets.RemoveAt(i);
                    if (target.Kind == TargetKind.Good)
                    {
                        OnGoodFell(target);
                    }
                }
            }

            if (!IsOver && Mode == GameMode.Timed)
            {
                TimeLeft -= deltaTime;
                if (TimeLeft <= 0f)
                {
                    TimeLeft = 0f;
                    End(EndReason.TimeUp, Vec2.Zero);
                }
            }
        }

        void TickPowers(float deltaTime)
        {
            if (SlowLeft > 0f && (SlowLeft -= deltaTime) <= 0f)
            {
                SlowLeft = 0f;
                events.Add(new RoundEvent(RoundEventKind.PowerEnded, TargetKind.SlowTime, Vec2.Zero));
            }
            if (MagnetLeft > 0f && (MagnetLeft -= deltaTime) <= 0f)
            {
                MagnetLeft = 0f;
                events.Add(new RoundEvent(RoundEventKind.PowerEnded, TargetKind.Magnet, Vec2.Zero));
            }
        }

        void OnGoodFell(Target target)
        {
            GoodFell++;
            var at = new Vec2(target.Position.X, Field.KillY);
            events.Add(new RoundEvent(RoundEventKind.Fell, target.Kind, at));
            BreakCombo(at);
            if (Mode == GameMode.Lives)
            {
                LoseLife(at);
            }
        }

        /// <summary>
        /// A click (or tap) at <paramref name="point"/> in world units. Without the magnet it pops the
        /// nearest target under it; with the magnet it pops every safe target within
        /// <see cref="MagnetRadius"/> and never a hazard. A click that pops nothing breaks the combo.
        /// </summary>
        public void Click(Vec2 point)
        {
            if (IsOver)
            {
                return;
            }
            Clicks++;
            hits.Clear();
            if (MagnetActive)
            {
                foreach (Target target in targets)
                {
                    if (!target.Kind.IsHazard() && Vec2.Distance(point, target.Position) <= MagnetRadius + target.Radius)
                    {
                        hits.Add(target);
                    }
                }
                hits.Sort((a, b) => Vec2.Distance(point, a.Position).CompareTo(Vec2.Distance(point, b.Position)));
            }
            else
            {
                Target nearest = null;
                float best = float.MaxValue;
                foreach (Target target in targets)
                {
                    float distance = Vec2.Distance(point, target.Position);
                    if (distance <= target.Radius + ClickSlack && distance < best)
                    {
                        best = distance;
                        nearest = target;
                    }
                }
                if (nearest != null)
                {
                    hits.Add(nearest);
                }
            }

            if (hits.Count == 0)
            {
                events.Add(new RoundEvent(RoundEventKind.Whiff, TargetKind.Good, point));
                BreakCombo(point);
                return;
            }

            bool safe = false;
            foreach (Target target in hits)
            {
                targets.Remove(target);
                safe |= !target.Kind.IsHazard();
                Hit(target);
                if (IsOver)
                {
                    break;
                }
            }
            if (safe)
            {
                HitClicks++;
            }
        }

        void Hit(Target target)
        {
            Vec2 at = target.Position;
            switch (target.Kind)
            {
                case TargetKind.Good:
                case TargetKind.Bonus:
                    Popped++;
                    int before = Score.Multiplier;
                    int points = Score.Pop(target.Kind == TargetKind.Bonus ? BonusPoints : GoodPoints);
                    events.Add(new RoundEvent(RoundEventKind.Popped, target.Kind, at, points, Score.Combo, target.Variant));
                    if (Score.Multiplier > before)
                    {
                        events.Add(new RoundEvent(RoundEventKind.MultiplierUp, target.Kind, at, 0, Score.Multiplier));
                    }
                    if (target.Kind == TargetKind.Bonus && Mode == GameMode.Timed)
                    {
                        AddTime(ModeRules.BonusSeconds, at);
                    }
                    break;
                case TargetKind.SlowTime:
                case TargetKind.Magnet:
                    Popped++;
                    Score.AddFlat(PowerUpPoints);
                    if (target.Kind == TargetKind.SlowTime)
                    {
                        SlowLeft = SlowSeconds;
                    }
                    else
                    {
                        MagnetLeft = MagnetSeconds;
                    }
                    events.Add(new RoundEvent(RoundEventKind.PowerUp, target.Kind, at, PowerUpPoints));
                    break;
                case TargetKind.Bad:
                    events.Add(new RoundEvent(RoundEventKind.Hazard, target.Kind, at));
                    BreakCombo(at);
                    if (Mode == GameMode.Lives)
                    {
                        LoseLife(at);
                    }
                    else
                    {
                        AddTime(-ModeRules.BadPenaltySeconds, at);
                    }
                    break;
                case TargetKind.Bomb:
                    events.Add(new RoundEvent(RoundEventKind.Bomb, target.Kind, at));
                    BreakCombo(at);
                    End(EndReason.Bomb, at);
                    break;
            }
        }

        void BreakCombo(Vec2 at)
        {
            int broken = Score.Break();
            if (broken >= 2)
            {
                events.Add(new RoundEvent(RoundEventKind.ComboBroken, TargetKind.Good, at, 0, broken));
            }
        }

        void LoseLife(Vec2 at)
        {
            Lives--;
            events.Add(new RoundEvent(RoundEventKind.LifeLost, TargetKind.Good, at, 0, Lives));
            if (Lives <= 0)
            {
                Lives = 0;
                End(EndReason.OutOfLives, at);
            }
        }

        void AddTime(float seconds, Vec2 at)
        {
            TimeLeft += seconds;
            events.Add(new RoundEvent(RoundEventKind.TimeChanged, TargetKind.Good, at, 0, seconds));
            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                End(EndReason.TimeUp, at);
            }
        }

        void End(EndReason reason, Vec2 at)
        {
            if (IsOver)
            {
                return;
            }
            Phase = RoundPhase.Over;
            EndReason = reason;
            events.Add(new RoundEvent(RoundEventKind.Over, TargetKind.Good, at, Score.Points));
        }

        /// <summary>Places a target by hand (tests, the screenshot scenarios).</summary>
        public Target Place(TargetKind kind, Vec2 position, Vec2 velocity, float spin = 0f, int variant = 0)
        {
            var target = new Target
            {
                Id = schedule.NextId(),
                Kind = kind,
                Position = position,
                Velocity = velocity,
                Spin = spin,
                Variant = variant,
            };
            targets.Add(target);
            return target;
        }

        /// <summary>Removes every target in flight (before scripted scenes).</summary>
        public void ClearTargets() => targets.Clear();
    }
}
