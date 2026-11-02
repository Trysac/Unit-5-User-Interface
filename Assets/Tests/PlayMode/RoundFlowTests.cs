using System.Collections;
using System.Linq;
using NUnit.Framework;
using TargetToss.Core;
using TargetToss.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace TargetToss.Tests
{
    /// <summary>
    /// The game's flow in the real scene: title, start, scoring, game over, restart, pause and
    /// settings, with what the HUD shows at each step.
    /// </summary>
    public class RoundFlowTests
    {
        GameDirector director;
        VisualElement ui;

        IEnumerator Load()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            director = Object.FindAnyObjectByType<GameDirector>();
            Assert.That(director, Is.Not.Null);
            director.UseMemoryStore();
            director.Seed(1);
            director.HitStopEnabled = false;
            director.ScriptedInput = true;
            ui = director.Hud.GetComponent<UIDocument>().rootVisualElement;
        }

        /// <summary>Starts a round with nothing tossed, so the test places every target.</summary>
        IEnumerator StartQuiet(GameMode mode = GameMode.Timed)
        {
            director.Settings.Mode = mode;
            director.Settings.Difficulty = Difficulty.Normal;
            director.Activate("play");
            director.Round.Spawning = false;
            yield return null;
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
        }

        void Pop(TargetKind kind, Vector2 at)
        {
            director.Round.Place(kind, new Vec2(at.x, at.y), Vec2.Zero);
            director.ClickWorld(at);
        }

        string Text(string name) => ui.Q<Label>(name).text;

        static bool Visible(VisualElement element) => !element.ClassListContains("hidden");

        [UnityTest]
        public IEnumerator TheGameOpensOnTheTitleWithTargetsFlyingBehindIt()
        {
            yield return Load();
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Title));
            Assert.That(director.Demo, Is.True);
            Assert.That(Visible(ui.Q("menu")), Is.True);
            Assert.That(Visible(ui.Q("logo")), Is.True);
            Assert.That(Visible(ui.Q("play-hud")), Is.False);
            Assert.That(director.Hud.Menu.Items.Select(item => item.Id).Take(3), Is.EqualTo(new[] { "play", "mode", "difficulty" }));

            yield return new WaitForSecondsRealtime(2f);
            Assert.That(director.Round.Targets.Count, Is.GreaterThan(0));
            Assert.That(director.Field.ShownCount, Is.EqualTo(director.Round.Targets.Count));
        }

        [UnityTest]
        public IEnumerator PlayStartsATimedRoundWithTheClockAndAnEmptyScore()
        {
            yield return Load();
            director.Activate("play");
            yield return null;

            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
            Assert.That(director.Demo, Is.False);
            Assert.That(director.Round.Mode, Is.EqualTo(GameMode.Timed));
            Assert.That(Visible(ui.Q("menu")), Is.False);
            Assert.That(Visible(ui.Q("play-hud")), Is.True);
            Assert.That(Visible(ui.Q("timer")), Is.True);
            Assert.That(Visible(ui.Q("lives")), Is.False);
            Assert.That(Text("timer"), Is.EqualTo("1:00"));
            Assert.That(Text("tag"), Is.EqualTo("TIMED  -  NORMAL"));
            Assert.That(Text("score"), Is.EqualTo("0"));
            Assert.That(Text("best"), Is.EqualTo("0"));
            Assert.That(Text("prompt"), Does.Contain("CANDY"));

            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(director.Round.Targets.Count, Is.GreaterThan(0), "targets are tossed");
            Assert.That(director.Round.TimeLeft, Is.LessThan(ModeRules.RoundSeconds - 2f));
        }

        [UnityTest]
        public IEnumerator PoppingCandyScoresBuildsTheComboAndFloatsThePoints()
        {
            yield return Load();
            yield return StartQuiet();
            for (int i = 0; i < 5; i++)
            {
                Pop(TargetKind.Good, new Vector2(-4f + i * 2f, 1f));
            }
            yield return null;

            Assert.That(director.Round.Score.Points, Is.EqualTo(60));
            Assert.That(Text("score"), Is.EqualTo("60"));
            Assert.That(Text("combo"), Is.EqualTo("x2   COMBO 5"));
            Assert.That(director.Hud.FloaterTexts, Has.Member("+10").And.Member("+20"));
            Assert.That(director.Hud.BannerText, Is.EqualTo("x2 COMBO"));
            Assert.That(director.Field.ShownCount, Is.EqualTo(0), "popped targets leave the field");

            director.ClickWorld(new Vector2(0f, -3f));
            yield return null;
            Assert.That(Text("combo"), Is.Empty, "a miss breaks the combo");
            Assert.That(Text("score"), Is.EqualTo("60"));
        }

        [UnityTest]
        public IEnumerator ABombEndsTheRoundShowsTheResultsAndSavesTheBest()
        {
            yield return Load();
            yield return StartQuiet();
            for (int i = 0; i < 3; i++)
            {
                Pop(TargetKind.Good, new Vector2(i * 2f, 0f));
            }
            Pop(TargetKind.Bomb, new Vector2(-3f, 1f));

            Assert.That(director.Round.IsOver, Is.True);
            Assert.That(director.Round.EndReason, Is.EqualTo(EndReason.Bomb));
            Assert.That(director.Hud.BannerText, Is.EqualTo("BOOM!"));
            Assert.That(director.Records.Best(GameMode.Timed, Difficulty.Normal), Is.EqualTo(30));
            Assert.That(director.LastRecord.NewBest, Is.True);

            float waited = 0f;
            while (director.Screen != GameScreen.Results && waited < 4f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Results));
            Assert.That(Text("menu-title"), Is.EqualTo("BOOM!"));
            Assert.That(Text("menu-subtitle"), Is.EqualTo("TIMED  -  NORMAL"));
            Assert.That(director.Hud.Menu.Body[0], Is.EqualTo(("SCORE", "30   NEW BEST!", true)));
            Assert.That(director.Hud.Menu.Items[0].Id, Is.EqualTo("again"));
            Assert.That(Text("best"), Is.EqualTo("30"));
        }

        [UnityTest]
        public IEnumerator LivesModeShowsHeartsAndADroppedCandyCostsOne()
        {
            yield return Load();
            yield return StartQuiet(GameMode.Lives);
            Assert.That(Visible(ui.Q("lives")), Is.True);
            Assert.That(Visible(ui.Q("timer")), Is.False);
            Assert.That(director.Hud.LivesShown, Is.EqualTo(3));

            director.Round.Place(TargetKind.Good, new Vec2(0f, Core.Field.KillY + 0.05f), new Vec2(0f, -3f));
            float waited = 0f;
            while (director.Round.Lives == 3 && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
            Assert.That(director.Round.Lives, Is.EqualTo(2));
            Assert.That(director.Hud.LivesShown, Is.EqualTo(2));
            Assert.That(director.Hud.FloaterTexts, Has.Member("MISS"));
            Assert.That(director.Round.IsOver, Is.False);
        }

        [UnityTest]
        public IEnumerator PauseFreezesTheTargetsAndResumeCarriesOn()
        {
            yield return Load();
            yield return StartQuiet();
            Target target = director.Round.Place(TargetKind.Good, new Vec2(0f, -2f), new Vec2(1f, 6f));
            yield return null;

            director.TogglePause();
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Paused));
            Assert.That(Text("menu-title"), Is.EqualTo("PAUSED"));
            Vec2 before = target.Position;
            float clock = director.Round.TimeLeft;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(target.Position, Is.EqualTo(before));
            Assert.That(director.Round.TimeLeft, Is.EqualTo(clock));

            director.ClickWorld(new Vector2(before.X, before.Y));
            Assert.That(director.Round.Score.Points, Is.EqualTo(0), "clicks do nothing while paused");

            director.Activate("resume");
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
            Assert.That(Visible(ui.Q("menu")), Is.False);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(target.Position, Is.Not.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator RestartFromThePauseMenuStartsAFreshRound()
        {
            yield return Load();
            yield return StartQuiet();
            Pop(TargetKind.Good, Vector2.zero);
            Pop(TargetKind.Bad, new Vector2(2f, 0f));
            Round old = director.Round;
            Assert.That(old.TimeLeft, Is.LessThan(ModeRules.RoundSeconds - 2f));

            director.TogglePause();
            director.Activate("restart");
            yield return null;

            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
            Assert.That(director.Round, Is.Not.SameAs(old));
            Assert.That(director.Round.Score.Points, Is.EqualTo(0));
            Assert.That(Text("score"), Is.EqualTo("0"));
            Assert.That(Text("timer"), Is.EqualTo("1:00"));
            Assert.That(Visible(ui.Q("menu")), Is.False);
        }

        [UnityTest]
        public IEnumerator PowerUpsShowTheirChipsAndBanners()
        {
            yield return Load();
            yield return StartQuiet();
            Pop(TargetKind.Magnet, new Vector2(-3f, 0f));
            yield return null;
            Assert.That(director.Round.MagnetActive, Is.True);
            Assert.That(director.Hud.BannerText, Is.EqualTo("MAGNET"));
            Assert.That(director.Hud.PowerChips, Is.EqualTo(1));

            Pop(TargetKind.SlowTime, new Vector2(3f, 0f));
            yield return null;
            Assert.That(director.Hud.BannerText, Is.EqualTo("SLOW TIME"));
            Assert.That(director.Hud.PowerChips, Is.EqualTo(2));
            Assert.That(ui.Q("slow-frame").ClassListContains("slow-frame--on"), Is.True);
        }

        [UnityTest]
        public IEnumerator SettingsChangesAreShownAndUsedByTheNextRound()
        {
            yield return Load();
            director.Change("mode", 1);
            director.Change("difficulty", 1);
            MenuModel title = director.Hud.Menu;
            Assert.That(title.Items[title.IndexOf("mode")].Value, Is.EqualTo("LIVES"));
            Assert.That(title.Items[title.IndexOf("difficulty")].Value, Is.EqualTo("HARD"));

            director.Activate("settings");
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Settings));
            director.Change("volume", -3);
            Assert.That(director.Hud.Menu.Items[0].Value, Is.EqualTo("5"));
            director.Activate("back");
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Title));

            director.Activate("play");
            yield return null;
            Assert.That(director.Round.Mode, Is.EqualTo(GameMode.Lives));
            Assert.That(director.Round.Difficulty, Is.EqualTo(Difficulty.Hard));
            Assert.That(Text("tag"), Is.EqualTo("LIVES  -  HARD"));
            Assert.That(director.Settings.Volume, Is.EqualTo(5));
        }
    }
}
