using NUnit.Framework;

namespace TargetToss.Core.Tests
{
    public class SpawnClockTests
    {
        [Test]
        public void TheSpawnClockReleasesOneTargetPerInterval()
        {
            var clock = new SpawnClock(0.5f);
            Assert.That(clock.Advance(0.3f), Is.EqualTo(0));
            Assert.That(clock.Advance(0.3f), Is.EqualTo(1));
            Assert.That(clock.Advance(0.3f), Is.EqualTo(0));
            Assert.That(clock.Advance(0.5f), Is.EqualTo(1));
        }

        [Test]
        public void ALongStepReleasesEveryTargetThatFellDue()
        {
            var clock = new SpawnClock(0.25f);
            Assert.That(clock.Advance(1.1f), Is.EqualTo(4));
            Assert.That(clock.Advance(0.15f), Is.EqualTo(1));
        }

        [Test]
        public void ResettingTheClockStartsTheIntervalAgain()
        {
            var clock = new SpawnClock(1f);
            clock.Advance(0.9f);
            clock.Reset();
            Assert.That(clock.Advance(0.5f), Is.EqualTo(0));
            Assert.That(clock.Advance(0.5f), Is.EqualTo(1));
        }

        [Test]
        public void AnIntervalOfZeroSpawnsOncePerStepLikeTheOldCoroutine()
        {
            var clock = new SpawnClock(0f);
            Assert.That(clock.Advance(0.016f), Is.EqualTo(1));
            Assert.That(clock.Advance(0.016f), Is.EqualTo(1));
        }
    }
}
