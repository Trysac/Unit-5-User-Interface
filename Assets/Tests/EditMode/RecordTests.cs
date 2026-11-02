using NUnit.Framework;

namespace TargetToss.Core.Tests
{
    public class RecordTests
    {
        [Test]
        public void EachModeAndDifficultyKeepsItsOwnBestScore()
        {
            var store = new MemoryRecordStore();
            var records = new HighScores(store);

            RecordUpdate first = records.Submit(GameMode.Timed, Difficulty.Normal, 420, 12);
            Assert.That(first.NewBest, Is.True);
            Assert.That(first.PreviousBest, Is.EqualTo(0));
            records.Submit(GameMode.Lives, Difficulty.Hard, 900, 3);

            Assert.That(records.Best(GameMode.Timed, Difficulty.Normal), Is.EqualTo(420));
            Assert.That(records.Best(GameMode.Lives, Difficulty.Hard), Is.EqualTo(900));
            Assert.That(records.Best(GameMode.Timed, Difficulty.Hard), Is.EqualTo(0));
            Assert.That(records.Best(GameMode.Lives, Difficulty.Normal), Is.EqualTo(0));
            Assert.That(records.RoundsPlayed, Is.EqualTo(2));
            Assert.That(store.SaveCount, Is.EqualTo(2), "one save per round");
        }

        [Test]
        public void ALowerOrEqualScoreKeepsTheBest()
        {
            var records = new HighScores(new MemoryRecordStore());
            records.Submit(GameMode.Timed, Difficulty.Easy, 500, 4);

            RecordUpdate lower = records.Submit(GameMode.Timed, Difficulty.Easy, 300, 2);
            RecordUpdate equal = records.Submit(GameMode.Timed, Difficulty.Easy, 500, 2);
            Assert.That(lower.NewBest, Is.False);
            Assert.That(equal.NewBest, Is.False);
            Assert.That(lower.PreviousBest, Is.EqualTo(500));
            Assert.That(records.Best(GameMode.Timed, Difficulty.Easy), Is.EqualTo(500));

            RecordUpdate higher = records.Submit(GameMode.Timed, Difficulty.Easy, 640, 9);
            Assert.That(higher.NewBest, Is.True);
            Assert.That(higher.PreviousBest, Is.EqualTo(500));
            Assert.That(records.Best(GameMode.Timed, Difficulty.Easy), Is.EqualTo(640));
        }

        [Test]
        public void TheBestComboIsKeptAcrossModes()
        {
            var records = new HighScores(new MemoryRecordStore());
            Assert.That(records.Submit(GameMode.Timed, Difficulty.Easy, 10, 8).NewBestCombo, Is.True);
            Assert.That(records.Submit(GameMode.Lives, Difficulty.Hard, 10, 5).NewBestCombo, Is.False);
            Assert.That(records.BestCombo, Is.EqualTo(8));
            Assert.That(records.Submit(GameMode.Lives, Difficulty.Hard, 10, 21).NewBestCombo, Is.True);
            Assert.That(records.BestCombo, Is.EqualTo(21));
        }

        [Test]
        public void RecordsSurviveANewHighScoresOverTheSameStore()
        {
            var store = new MemoryRecordStore();
            new HighScores(store).Submit(GameMode.Lives, Difficulty.Easy, 1234, 15);
            var again = new HighScores(store);
            Assert.That(again.Best(GameMode.Lives, Difficulty.Easy), Is.EqualTo(1234));
            Assert.That(again.BestCombo, Is.EqualTo(15));
            Assert.That(again.RoundsPlayed, Is.EqualTo(1));
        }

        [Test]
        public void SettingsAreSavedAndLoadedBack()
        {
            var store = new MemoryRecordStore();
            var settings = new GameSettings
            {
                Mode = GameMode.Lives,
                Difficulty = Difficulty.Hard,
                Volume = 3,
                Muted = true,
                ScreenShake = false,
                CursorSpeed = 5,
            };
            settings.Save(store);
            Assert.That(store.SaveCount, Is.EqualTo(1));

            GameSettings loaded = GameSettings.Load(store);
            Assert.That(loaded.Mode, Is.EqualTo(GameMode.Lives));
            Assert.That(loaded.Difficulty, Is.EqualTo(Difficulty.Hard));
            Assert.That(loaded.Volume, Is.EqualTo(3));
            Assert.That(loaded.Muted, Is.True);
            Assert.That(loaded.ScreenShake, Is.False);
            Assert.That(loaded.CursorSpeed, Is.EqualTo(5));
        }

        [Test]
        public void MissingOrBrokenSettingsFallBackToTheDefaults()
        {
            GameSettings fresh = GameSettings.Load(new MemoryRecordStore());
            Assert.That(fresh.Mode, Is.EqualTo(GameMode.Timed));
            Assert.That(fresh.Difficulty, Is.EqualTo(Difficulty.Normal));
            Assert.That(fresh.Volume, Is.EqualTo(8));
            Assert.That(fresh.ScreenShake, Is.True);

            var store = new MemoryRecordStore();
            store.SetInt("targettoss.settings.mode", 9);
            store.SetInt("targettoss.settings.difficulty", -1);
            store.SetInt("targettoss.settings.volume", 99);
            store.SetInt("targettoss.settings.cursor", 0);
            GameSettings broken = GameSettings.Load(store);
            Assert.That(broken.Mode, Is.EqualTo(GameMode.Timed));
            Assert.That(broken.Difficulty, Is.EqualTo(Difficulty.Normal));
            Assert.That(broken.Volume, Is.EqualTo(10));
            Assert.That(broken.CursorSpeed, Is.EqualTo(1));
        }

        [Test]
        public void CyclingASettingWrapsAroundAndStepsAreClamped()
        {
            var settings = new GameSettings { Difficulty = Difficulty.Hard, Mode = GameMode.Lives, Volume = 9, CursorSpeed = 1 };
            settings.CycleDifficulty(1);
            Assert.That(settings.Difficulty, Is.EqualTo(Difficulty.Easy));
            settings.CycleDifficulty(-1);
            Assert.That(settings.Difficulty, Is.EqualTo(Difficulty.Hard));
            settings.CycleMode(1);
            Assert.That(settings.Mode, Is.EqualTo(GameMode.Timed));
            settings.StepVolume(5);
            Assert.That(settings.Volume, Is.EqualTo(10));
            settings.StepCursorSpeed(-1);
            Assert.That(settings.CursorSpeed, Is.EqualTo(1));
        }
    }
}
