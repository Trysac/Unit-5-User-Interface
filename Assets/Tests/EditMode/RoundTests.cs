using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TargetToss.Core.Tests
{
    public class RoundTests
    {
        static Round Quiet(GameMode mode, Difficulty difficulty = Difficulty.Normal)
        {
            return new Round(mode, difficulty, 1) { Spawning = false };
        }

        static List<RoundEventKind> Kinds(Round round) => round.Events.Select(e => e.Kind).ToList();

        static readonly Vec2 Still = Vec2.Zero;

        [Test]
        public void ARoundStartsWithTheModesClockOrLives()
        {
            var timed = new Round(GameMode.Timed, Difficulty.Easy, 1);
            Assert.That(timed.TimeLeft, Is.EqualTo(ModeRules.RoundSeconds));
            Assert.That(timed.Lives, Is.EqualTo(0));
            Assert.That(timed.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(timed.Score.Points, Is.EqualTo(0));

            var lives = new Round(GameMode.Lives, Difficulty.Easy, 1);
            Assert.That(lives.Lives, Is.EqualTo(ModeRules.StartingLives));
            Assert.That(lives.TimeLeft, Is.EqualTo(0f));
        }

        [Test]
        public void ClickingAGoodTargetPopsItForTenPoints()
        {
            Round round = Quiet(GameMode.Timed);
            Target target = round.Place(TargetKind.Good, new Vec2(1f, 1f), Still, variant: 2);

            round.Click(new Vec2(1.3f, 1.2f));

            Assert.That(round.Targets, Has.No.Member(target));
            Assert.That(round.Score.Points, Is.EqualTo(Round.GoodPoints));
            Assert.That(round.Score.Combo, Is.EqualTo(1));
            Assert.That(round.Popped, Is.EqualTo(1));
            RoundEvent popped = round.Events.Single(e => e.Kind == RoundEventKind.Popped);
            Assert.That(popped.Points, Is.EqualTo(10));
            Assert.That(popped.Position, Is.EqualTo(new Vec2(1f, 1f)));
            Assert.That(popped.Variant, Is.EqualTo(2), "the view colours the burst like the target");
            Assert.That(round.Accuracy, Is.EqualTo(1f));
        }

        [Test]
        public void AClickPopsOnlyTheNearestTargetUnderIt()
        {
            Round round = Quiet(GameMode.Timed);
            Target near = round.Place(TargetKind.Good, new Vec2(0.2f, 0f), Still);
            Target far = round.Place(TargetKind.Bad, new Vec2(-0.5f, 0f), Still);

            round.Click(new Vec2(0f, 0f));

            Assert.That(round.Targets, Has.No.Member(near));
            Assert.That(round.Targets, Has.Member(far));
            Assert.That(Kinds(round), Has.No.Member(RoundEventKind.Hazard));
        }

        [Test]
        public void AClickJustOutsideATargetStillCountsButAWideMissDoesNot()
        {
            Round round = Quiet(GameMode.Timed);
            float edge = TargetKind.Good.Radius() + Round.ClickSlack;
            round.Place(TargetKind.Good, Vec2.Zero, Still);
            round.Click(new Vec2(edge - 0.01f, 0f));
            Assert.That(round.Popped, Is.EqualTo(1));

            round.Place(TargetKind.Good, Vec2.Zero, Still);
            round.ClearEvents();
            round.Click(new Vec2(edge + 0.05f, 0f));
            Assert.That(round.Popped, Is.EqualTo(1));
            Assert.That(Kinds(round), Is.EqualTo(new[] { RoundEventKind.Whiff }));
        }

        [Test]
        public void FivePopsInARowRaiseTheMultiplierAndAMissBreaksIt()
        {
            Round round = Quiet(GameMode.Timed);
            for (int i = 0; i < 5; i++)
            {
                round.Place(TargetKind.Good, new Vec2(i * 2f - 4f, 0f), Still);
                round.Click(new Vec2(i * 2f - 4f, 0f));
            }
            Assert.That(round.Score.Points, Is.EqualTo(4 * 10 + 20));
            Assert.That(round.Score.Multiplier, Is.EqualTo(2));
            RoundEvent up = round.Events.Single(e => e.Kind == RoundEventKind.MultiplierUp);
            Assert.That(up.Value, Is.EqualTo(2f));

            round.ClearEvents();
            round.Click(new Vec2(0f, 4f));
            Assert.That(round.Score.Combo, Is.EqualTo(0));
            Assert.That(round.Score.Points, Is.EqualTo(60), "a miss costs the combo, not points");
            Assert.That(Kinds(round), Is.EqualTo(new[] { RoundEventKind.Whiff, RoundEventKind.ComboBroken }));
            Assert.That(round.Events[1].Value, Is.EqualTo(5f));
            Assert.That(round.Accuracy, Is.EqualTo(5f / 6f).Within(1e-5f));
        }

        [Test]
        public void ABonusStarScoresFiftyTimesTheMultiplierAndAddsTimeInTimedMode()
        {
            Round timed = Quiet(GameMode.Timed);
            for (int i = 0; i < 4; i++)
            {
                timed.Place(TargetKind.Good, new Vec2(i * 2f - 4f, 0f), Still);
                timed.Click(new Vec2(i * 2f - 4f, 0f));
            }
            timed.Place(TargetKind.Bonus, new Vec2(3f, 2f), Still);
            timed.ClearEvents();
            timed.Click(new Vec2(3f, 2f));

            Assert.That(timed.Score.Points, Is.EqualTo(40 + 100));
            Assert.That(timed.TimeLeft, Is.EqualTo(ModeRules.RoundSeconds + ModeRules.BonusSeconds));
            RoundEvent time = timed.Events.Single(e => e.Kind == RoundEventKind.TimeChanged);
            Assert.That(time.Value, Is.EqualTo(ModeRules.BonusSeconds));

            Round lives = Quiet(GameMode.Lives);
            lives.Place(TargetKind.Bonus, Vec2.Zero, Still);
            lives.Click(Vec2.Zero);
            Assert.That(lives.Score.Points, Is.EqualTo(Round.BonusPoints));
            Assert.That(Kinds(lives), Has.No.Member(RoundEventKind.TimeChanged));
        }

        [Test]
        public void ABadTargetCostsALifeInLivesModeAndThreeSecondsInTimedMode()
        {
            Round lives = Quiet(GameMode.Lives);
            lives.Place(TargetKind.Good, new Vec2(3f, 0f), Still);
            lives.Place(TargetKind.Good, new Vec2(-3f, 0f), Still);
            lives.Click(new Vec2(3f, 0f));
            lives.Click(new Vec2(-3f, 0f));
            lives.Place(TargetKind.Bad, Vec2.Zero, Still);
            lives.ClearEvents();
            lives.Click(Vec2.Zero);

            Assert.That(lives.Lives, Is.EqualTo(ModeRules.StartingLives - 1));
            Assert.That(lives.Score.Combo, Is.EqualTo(0));
            Assert.That(lives.Score.Points, Is.EqualTo(20));
            Assert.That(Kinds(lives), Is.EqualTo(new[] { RoundEventKind.Hazard, RoundEventKind.ComboBroken, RoundEventKind.LifeLost }));
            Assert.That(lives.Accuracy, Is.EqualTo(2f / 3f).Within(1e-5f), "a hazard is not a hit");

            Round timed = Quiet(GameMode.Timed);
            timed.Place(TargetKind.Bad, Vec2.Zero, Still);
            timed.Click(Vec2.Zero);
            Assert.That(timed.TimeLeft, Is.EqualTo(ModeRules.RoundSeconds - ModeRules.BadPenaltySeconds));
            Assert.That(timed.Events.Single(e => e.Kind == RoundEventKind.TimeChanged).Value, Is.EqualTo(-3f));
            Assert.That(timed.IsOver, Is.False);
        }

        [Test]
        public void ABombEndsTheRoundInEitherMode()
        {
            foreach (GameMode mode in new[] { GameMode.Timed, GameMode.Lives })
            {
                Round round = Quiet(mode);
                round.Place(TargetKind.Good, new Vec2(4f, 0f), Still);
                round.Click(new Vec2(4f, 0f));
                round.Place(TargetKind.Bomb, Vec2.Zero, Still);
                round.Place(TargetKind.Good, new Vec2(-4f, 0f), Still);
                round.Click(Vec2.Zero);

                Assert.That(round.Phase, Is.EqualTo(RoundPhase.Over));
                Assert.That(round.EndReason, Is.EqualTo(EndReason.Bomb));
                Assert.That(round.Events.Last().Kind, Is.EqualTo(RoundEventKind.Over));
                Assert.That(round.Events.Last().Points, Is.EqualTo(10));

                int clicks = round.Clicks;
                float elapsed = round.Elapsed;
                round.Click(new Vec2(-4f, 0f));
                round.Step(1f);
                Assert.That(round.Clicks, Is.EqualTo(clicks), "clicks after the end are ignored");
                Assert.That(round.Score.Points, Is.EqualTo(10));
                Assert.That(round.Elapsed, Is.EqualTo(elapsed));
            }
        }

        [Test]
        public void TimedRoundsEndWhenTheClockRunsOutAndLivesRoundsDoNot()
        {
            Round timed = Quiet(GameMode.Timed);
            for (int i = 0; i < 59; i++)
            {
                timed.Step(1f);
            }
            Assert.That(timed.IsOver, Is.False);
            Assert.That(timed.TimeLeft, Is.EqualTo(1f).Within(1e-4f));
            timed.Step(1.5f);
            Assert.That(timed.IsOver, Is.True);
            Assert.That(timed.EndReason, Is.EqualTo(EndReason.TimeUp));
            Assert.That(timed.TimeLeft, Is.EqualTo(0f));

            Round lives = Quiet(GameMode.Lives);
            for (int i = 0; i < 300; i++)
            {
                lives.Step(1f);
            }
            Assert.That(lives.IsOver, Is.False);
        }

        [Test]
        public void TargetsFlyUnderGravityAndAGoodOneThatFallsOutCostsALifeInLivesMode()
        {
            Round round = Quiet(GameMode.Lives);
            float gravity = round.Tuning.Gravity;
            Target target = round.Place(TargetKind.Good, new Vec2(0f, Field.LaunchY), new Vec2(1f, 10f));

            round.Step(0.5f);
            Assert.That(target.Velocity.Y, Is.EqualTo(10f - gravity * 0.5f).Within(1e-4f));
            Assert.That(target.Position.X, Is.EqualTo(0.5f).Within(1e-4f));

            int steps = 0;
            while (round.Targets.Count > 0 && steps++ < 1000)
            {
                round.Step(0.02f);
            }
            Assert.That(round.Lives, Is.EqualTo(ModeRules.StartingLives - 1));
            Assert.That(round.GoodFell, Is.EqualTo(1));
            Assert.That(round.Events.Any(e => e.Kind == RoundEventKind.Fell), Is.True);
            Assert.That(round.Events.Single(e => e.Kind == RoundEventKind.LifeLost).Value, Is.EqualTo(2f));
        }

        [Test]
        public void LettingABadTargetOrABombFallCostsNothing()
        {
            Round round = Quiet(GameMode.Lives);
            round.Place(TargetKind.Bad, new Vec2(0f, Field.KillY + 0.1f), new Vec2(0f, -5f));
            round.Place(TargetKind.Bomb, new Vec2(1f, Field.KillY + 0.1f), new Vec2(0f, -5f));
            round.Place(TargetKind.Bonus, new Vec2(2f, Field.KillY + 0.1f), new Vec2(0f, -5f));
            round.Step(0.1f);
            Assert.That(round.Targets, Is.Empty);
            Assert.That(round.Lives, Is.EqualTo(ModeRules.StartingLives));
            Assert.That(round.Events, Is.Empty);
        }

        [Test]
        public void LosingTheLastLifeEndsTheRound()
        {
            Round round = Quiet(GameMode.Lives);
            for (int i = 0; i < ModeRules.StartingLives; i++)
            {
                round.Place(TargetKind.Bad, Vec2.Zero, Still);
                round.Click(Vec2.Zero);
            }
            Assert.That(round.Lives, Is.EqualTo(0));
            Assert.That(round.IsOver, Is.True);
            Assert.That(round.EndReason, Is.EqualTo(EndReason.OutOfLives));
        }

        [Test]
        public void SlowTimeSlowsTheTargetsButNotTheClockForFiveSeconds()
        {
            Round normal = Quiet(GameMode.Timed);
            Target reference = normal.Place(TargetKind.Good, new Vec2(0f, 0f), new Vec2(2f, 0f));
            normal.Step(1f);

            Round slowed = Quiet(GameMode.Timed);
            slowed.Place(TargetKind.SlowTime, new Vec2(5f, 3f), Still);
            slowed.Click(new Vec2(5f, 3f));
            Assert.That(slowed.SlowLeft, Is.EqualTo(Round.SlowSeconds));
            Assert.That(slowed.Score.Points, Is.EqualTo(Round.PowerUpPoints));
            Assert.That(slowed.Events.Single(e => e.Kind == RoundEventKind.PowerUp).Target, Is.EqualTo(TargetKind.SlowTime));
            Target target = slowed.Place(TargetKind.Good, new Vec2(0f, 0f), new Vec2(2f, 0f));
            slowed.Step(1f);

            Assert.That(target.Position.X, Is.EqualTo(reference.Position.X * Round.SlowFactor).Within(1e-4f));
            Assert.That(slowed.TimeLeft, Is.EqualTo(normal.TimeLeft).Within(1e-4f));

            slowed.ClearEvents();
            slowed.Step(4.1f);
            Assert.That(slowed.SlowLeft, Is.EqualTo(0f));
            Assert.That(slowed.TimeScale, Is.EqualTo(1f));
            Assert.That(slowed.Events.Any(e => e.Kind == RoundEventKind.PowerEnded && e.Target == TargetKind.SlowTime), Is.True);
        }

        [Test]
        public void TheMagnetPullsInEverySafeTargetNearTheClickAndNeverAHazard()
        {
            Round round = Quiet(GameMode.Lives);
            round.Place(TargetKind.Magnet, new Vec2(6f, 3f), Still);
            round.Click(new Vec2(6f, 3f));
            Assert.That(round.MagnetActive, Is.True);

            Target a = round.Place(TargetKind.Good, new Vec2(1.2f, 0f), Still);
            Target b = round.Place(TargetKind.Good, new Vec2(-1.5f, 0.5f), Still);
            Target star = round.Place(TargetKind.Bonus, new Vec2(0f, 1.6f), Still);
            Target bad = round.Place(TargetKind.Bad, new Vec2(0f, 0f), Still);
            Target bomb = round.Place(TargetKind.Bomb, new Vec2(0.4f, -0.3f), Still);
            Target away = round.Place(TargetKind.Good, new Vec2(5f, -2f), Still);
            round.ClearEvents();

            round.Click(Vec2.Zero);

            Assert.That(round.Targets, Is.EquivalentTo(new[] { bad, bomb, away }));
            Assert.That(round.Targets, Has.No.Member(a).And.No.Member(b).And.No.Member(star));
            Assert.That(round.Lives, Is.EqualTo(ModeRules.StartingLives));
            Assert.That(round.IsOver, Is.False);
            Assert.That(round.Events.Count(e => e.Kind == RoundEventKind.Popped), Is.EqualTo(3));
            Assert.That(round.Score.Combo, Is.EqualTo(3));

            round.ClearEvents();
            round.Step(Round.MagnetSeconds);
            Assert.That(round.MagnetActive, Is.False);
            Assert.That(round.Events.Any(e => e.Kind == RoundEventKind.PowerEnded && e.Target == TargetKind.Magnet), Is.True);
        }

        [Test]
        public void ARoundWithTheSameSeedPlaysOutTheSame()
        {
            Round a = new Round(GameMode.Lives, Difficulty.Hard, 77);
            Round b = new Round(GameMode.Lives, Difficulty.Hard, 77);
            for (int i = 0; i < 600; i++)
            {
                a.Step(1f / 60f);
                b.Step(1f / 60f);
            }
            Assert.That(a.Targets.Count, Is.GreaterThan(0));
            Assert.That(b.Targets.Select(t => t.Position), Is.EqualTo(a.Targets.Select(t => t.Position)));
            Assert.That(b.Lives, Is.EqualTo(a.Lives));
            Assert.That(b.GoodFell, Is.EqualTo(a.GoodFell));
        }

        [Test]
        public void TurningSpawningOffStopsNewTosses()
        {
            Round round = Quiet(GameMode.Timed);
            round.Step(10f);
            Assert.That(round.Targets, Is.Empty);
            round.Spawning = true;
            round.Step(1f);
            round.Step(1f);
            Assert.That(round.Targets, Is.Not.Empty);
            Assert.That(round.Events.Any(e => e.Kind == RoundEventKind.Spawned), Is.True);
        }
    }
}
