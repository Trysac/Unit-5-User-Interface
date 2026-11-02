using NUnit.Framework;

namespace TargetToss.Core.Tests
{
    public class ScoringTests
    {
        [Test]
        public void TheFifthPopInARowScoresDouble()
        {
            var score = new ComboScore();
            for (int i = 0; i < 4; i++)
            {
                Assert.That(score.Pop(10), Is.EqualTo(10));
                Assert.That(score.Multiplier, Is.EqualTo(1));
            }
            Assert.That(score.Pop(10), Is.EqualTo(20));
            Assert.That(score.Multiplier, Is.EqualTo(2));
            Assert.That(score.Combo, Is.EqualTo(5));
            Assert.That(score.Points, Is.EqualTo(60));
        }

        [Test]
        public void TheMultiplierStopsAtFive()
        {
            var score = new ComboScore();
            for (int i = 0; i < 40; i++)
            {
                score.Pop(10);
            }
            Assert.That(score.Multiplier, Is.EqualTo(ComboScore.MaxMultiplier));
            Assert.That(score.Pop(10), Is.EqualTo(50));
            Assert.That(ComboScore.MultiplierFor(19), Is.EqualTo(4));
            Assert.That(ComboScore.MultiplierFor(20), Is.EqualTo(5));
            Assert.That(ComboScore.MultiplierFor(500), Is.EqualTo(5));
        }

        [Test]
        public void BreakingTheComboKeepsThePointsAndTheBestCombo()
        {
            var score = new ComboScore();
            for (int i = 0; i < 7; i++)
            {
                score.Pop(10);
            }
            int points = score.Points;

            Assert.That(score.Break(), Is.EqualTo(7));
            Assert.That(score.Combo, Is.EqualTo(0));
            Assert.That(score.Multiplier, Is.EqualTo(1));
            Assert.That(score.Points, Is.EqualTo(points));
            Assert.That(score.BestCombo, Is.EqualTo(7));

            score.Pop(10);
            Assert.That(score.BestCombo, Is.EqualTo(7));
            Assert.That(score.Break(), Is.EqualTo(1));
        }

        [Test]
        public void FlatPointsAreNotMultipliedAndLeaveTheComboAlone()
        {
            var score = new ComboScore();
            for (int i = 0; i < 6; i++)
            {
                score.Pop(10);
            }
            int before = score.Points;
            score.AddFlat(5);
            Assert.That(score.Points, Is.EqualTo(before + 5));
            Assert.That(score.Combo, Is.EqualTo(6));
        }

        [Test]
        public void HarderLevelsTossFasterAndMoreDangerously()
        {
            DifficultyTuning easy = DifficultyTuning.For(Difficulty.Easy);
            DifficultyTuning normal = DifficultyTuning.For(Difficulty.Normal);
            DifficultyTuning hard = DifficultyTuning.For(Difficulty.Hard);

            Assert.That(easy.StartInterval, Is.GreaterThan(normal.StartInterval));
            Assert.That(normal.StartInterval, Is.GreaterThan(hard.StartInterval));
            Assert.That(easy.MinInterval, Is.GreaterThan(normal.MinInterval));
            Assert.That(normal.MinInterval, Is.GreaterThan(hard.MinInterval));
            Assert.That(Share(easy.BombWeight, easy), Is.LessThan(Share(normal.BombWeight, normal)));
            Assert.That(Share(normal.BombWeight, normal), Is.LessThan(Share(hard.BombWeight, hard)));
            Assert.That(Share(easy.BadWeight, easy), Is.LessThan(Share(hard.BadWeight, hard)));
            Assert.That(easy.PairChance, Is.LessThan(hard.PairChance));
        }

        [Test]
        public void TheTossIntervalShrinksOverTheRampAndThenHolds()
        {
            DifficultyTuning normal = DifficultyTuning.For(Difficulty.Normal);
            Assert.That(normal.IntervalAt(0f), Is.EqualTo(normal.StartInterval));
            Assert.That(normal.IntervalAt(normal.RampSeconds / 2f),
                Is.EqualTo((normal.StartInterval + normal.MinInterval) / 2f).Within(1e-5f));
            Assert.That(normal.IntervalAt(normal.RampSeconds), Is.EqualTo(normal.MinInterval).Within(1e-5f));
            Assert.That(normal.IntervalAt(normal.RampSeconds * 3f), Is.EqualTo(normal.MinInterval).Within(1e-5f));
            Assert.That(normal.IntervalAt(-5f), Is.EqualTo(normal.StartInterval));
        }

        static float Share(int weight, DifficultyTuning tuning) => (float)weight / tuning.TotalWeight;
    }
}
