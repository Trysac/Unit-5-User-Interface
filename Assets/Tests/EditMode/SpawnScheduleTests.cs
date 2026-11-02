using System.Collections.Generic;
using NUnit.Framework;

namespace TargetToss.Core.Tests
{
    public class SpawnScheduleTests
    {
        static List<Target> Run(Difficulty difficulty, int seed, float seconds, bool powerUps = true)
        {
            var schedule = new SpawnSchedule(DifficultyTuning.For(difficulty), new Rng(seed));
            var targets = new List<Target>();
            for (float t = 0f; t < seconds; t += 0.02f)
            {
                schedule.Advance(0.02f, t, powerUps, targets);
            }
            return targets;
        }

        [Test]
        public void TheSameSeedTossesTheSameTargets()
        {
            List<Target> a = Run(Difficulty.Hard, 42, 20f);
            List<Target> b = Run(Difficulty.Hard, 42, 20f);
            List<Target> c = Run(Difficulty.Hard, 43, 20f);

            Assert.That(a.Count, Is.EqualTo(b.Count));
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(b[i].Kind, Is.EqualTo(a[i].Kind));
                Assert.That(b[i].Position, Is.EqualTo(a[i].Position));
                Assert.That(b[i].Velocity, Is.EqualTo(a[i].Velocity));
            }
            Assert.That(Describe(c), Is.Not.EqualTo(Describe(a)), "another seed tosses differently");
        }

        [Test]
        public void TheFirstTossComesAfterOneInterval()
        {
            DifficultyTuning easy = DifficultyTuning.For(Difficulty.Easy);
            var schedule = new SpawnSchedule(easy, new Rng(1));
            var targets = new List<Target>();
            schedule.Advance(easy.StartInterval - 0.01f, 0f, true, targets);
            Assert.That(targets, Is.Empty);
            schedule.Advance(0.02f, 0f, true, targets);
            Assert.That(targets.Count, Is.EqualTo(1), "Easy never tosses pairs");
        }

        [Test]
        public void HardTossesMoreTargetsThanEasyInTheSameTime()
        {
            int easy = Run(Difficulty.Easy, 7, 30f).Count;
            int normal = Run(Difficulty.Normal, 7, 30f).Count;
            int hard = Run(Difficulty.Hard, 7, 30f).Count;
            Assert.That(normal, Is.GreaterThan(easy));
            Assert.That(hard, Is.GreaterThan(normal));
        }

        [Test]
        public void EveryTossStartsBelowTheFieldPeaksInsideItAndFallsBackOut()
        {
            foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard })
            {
                DifficultyTuning tuning = DifficultyTuning.For(difficulty);
                foreach (Target target in Run(difficulty, 3, 30f))
                {
                    Assert.That(target.Position.Y, Is.EqualTo(Field.LaunchY));
                    Assert.That(target.Position.X, Is.InRange(-Field.SpawnHalfWidth, Field.SpawnHalfWidth));
                    Assert.That(target.Velocity.Y, Is.GreaterThan(0f));
                    float apex = target.Position.Y + target.Velocity.Y * target.Velocity.Y / (2f * tuning.Gravity);
                    Assert.That(apex, Is.InRange(Field.ApexMin - 1e-3f, Field.ApexMax + 1e-3f));
                    float airTime = 2f * target.Velocity.Y / tuning.Gravity;
                    float landing = target.Position.X + target.Velocity.X * airTime;
                    Assert.That(landing, Is.InRange(-Field.LandHalfWidth - 1e-3f, Field.LandHalfWidth + 1e-3f));
                    Assert.That(System.Math.Abs(target.Velocity.X), Is.LessThanOrEqualTo(Field.MaxSideSpeed));
                }
            }
        }

        [Test]
        public void TheMixFollowsTheDifficultyWeights()
        {
            DifficultyTuning hard = DifficultyTuning.For(Difficulty.Hard);
            var schedule = new SpawnSchedule(hard, new Rng(11));
            var counts = new Dictionary<TargetKind, int>();
            const int picks = 20000;
            for (int i = 0; i < picks; i++)
            {
                TargetKind kind = schedule.PickKind(true);
                counts[kind] = counts.TryGetValue(kind, out int n) ? n + 1 : 1;
            }
            float total = hard.TotalWeight;
            Assert.That(counts[TargetKind.Good] / (float)picks, Is.EqualTo(hard.GoodWeight / total).Within(0.02f));
            Assert.That(counts[TargetKind.Bad] / (float)picks, Is.EqualTo(hard.BadWeight / total).Within(0.02f));
            Assert.That(counts[TargetKind.Bomb] / (float)picks, Is.EqualTo(hard.BombWeight / total).Within(0.015f));
            Assert.That(counts[TargetKind.Bonus] / (float)picks, Is.EqualTo(hard.BonusWeight / total).Within(0.015f));
            Assert.That(counts[TargetKind.SlowTime] + counts[TargetKind.Magnet], Is.GreaterThan(0));
        }

        [Test]
        public void NoPowerUpIsTossedWhenTheyAreNotAllowed()
        {
            foreach (Target target in Run(Difficulty.Easy, 5, 120f, powerUps: false))
            {
                Assert.That(target.Kind.IsPowerUp(), Is.False);
            }
        }

        [Test]
        public void ThePartnerInAPairIsAlwaysAGoodOrBadTarget()
        {
            var bombsOnly = new DifficultyTuning
            {
                StartInterval = 1f, MinInterval = 1f, RampSeconds = 1f, Gravity = 9f, PairChance = 1f, BombWeight = 1,
            };
            var schedule = new SpawnSchedule(bombsOnly, new Rng(9));
            var targets = new List<Target>();
            for (int i = 0; i < 50; i++)
            {
                schedule.Advance(1f, i, true, targets);
            }
            Assert.That(targets.Count, Is.EqualTo(100));
            int lastId = 0;
            for (int i = 0; i < targets.Count; i += 2)
            {
                Assert.That(targets[i].Kind, Is.EqualTo(TargetKind.Bomb));
                Assert.That(new[] { TargetKind.Good, TargetKind.Bad }, Has.Member(targets[i + 1].Kind));
                Assert.That(targets[i].Id, Is.GreaterThan(lastId), "ids grow");
                Assert.That(targets[i + 1].Id, Is.GreaterThan(targets[i].Id));
                lastId = targets[i + 1].Id;
            }
        }

        static string Describe(List<Target> targets)
        {
            var text = new System.Text.StringBuilder();
            foreach (Target target in targets)
            {
                text.Append(target.Kind).Append(target.Position).Append(';');
            }
            return text.ToString();
        }
    }
}
