using NUnit.Framework;

namespace TargetToss.Core.Tests
{
    public class DifficultyRulesTests
    {
        [TestCase(1, 1f)]
        [TestCase(2, 0.5f)]
        [TestCase(3, 1f / 3f)]
        public void EachLevelSpawnsFasterThanTheOneBefore(int level, float interval)
        {
            Assert.That(DifficultyRules.SpawnInterval(level), Is.EqualTo(interval).Within(1e-6f));
        }

        [TestCase(0, 1f)]
        [TestCase(-4, 1f)]
        [TestCase(7, 1f / 3f)]
        public void LevelsOutsideTheMenuAreClamped(int level, float interval)
        {
            Assert.That(DifficultyRules.SpawnInterval(level), Is.EqualTo(interval).Within(1e-6f));
        }
    }
}
