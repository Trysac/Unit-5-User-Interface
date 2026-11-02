using System.Collections.Generic;
using TargetToss.Core;
using UnityEngine;

namespace TargetToss.Game
{
    public enum GameScreen
    {
        Title,
        HowToPlay,
        Records,
        Settings,
        Playing,
        Paused,
        Results,
    }

    /// <summary>
    /// Runs the game: the menus with a round of tossed targets playing behind the title, and the
    /// rounds themselves. It feeds clicks to the engine-free <see cref="Round"/>, turns its events
    /// into sound, particles, shake, hit-stop and floating score, and keeps the settings and the
    /// high scores.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour
    {
        const float ResultsDelay = 1.4f;
        const float MaxFrame = 1f / 20f;
        const float PromptSeconds = 4.5f;

        [SerializeField] GameArt art;
        [SerializeField] Hud hud;
        [SerializeField] CameraRig cameraRig;

        FieldView field;
        AudioDirector sound;
        readonly GameInput input = new GameInput();
        IRecordStore store;
        HighScores records;
        GameSettings settings;
        int seed;
        float hitStop;
        float overTime;
        int lastTick;
        bool recorded;
        RecordUpdate lastRecord;

        public GameScreen Screen { get; private set; }
        public Round Round { get; private set; }
        public HighScores Records => records;
        public GameSettings Settings => settings;
        public Hud Hud => hud;
        public FieldView Field => field;
        public RecordUpdate LastRecord => lastRecord;

        /// <summary>True while the round behind the menus is the title demo.</summary>
        public bool Demo { get; private set; }

        /// <summary>Tests and the screenshot hook drive the game instead of the devices.</summary>
        public bool ScriptedInput { get; set; }

        /// <summary>Freezes the round for hit-stop; tests turn it off to keep timings exact.</summary>
        public bool HitStopEnabled { get; set; } = true;

        /// <summary>Where the screenshot hook puts the reticle (world units).</summary>
        public Vector2? ScriptedCursor { get; set; }

        void Awake()
        {
            seed = System.Environment.TickCount;
            store = new PlayerPrefsStore();
            LoadOptions();
            sound = gameObject.AddComponent<AudioDirector>();
            sound.Init(settings.Volume / 10f, settings.Muted);
            field = new GameObject("Field").AddComponent<FieldView>();
            field.Build();
            hud.ItemClicked += OnItemClicked;
            hud.PauseClicked += () =>
            {
                if (Screen == GameScreen.Playing)
                {
                    TogglePause();
                }
            };
        }

        void Start()
        {
            ToTitle();
            UpdateSoundLabel();
        }

        void LoadOptions()
        {
            records = new HighScores(store);
            settings = GameSettings.Load(store);
            ApplyOptions();
        }

        void ApplyOptions()
        {
            if (cameraRig != null)
            {
                cameraRig.ShakeScale = settings.ScreenShake ? 1f : 0f;
            }
            input.CursorSpeed = 0.6f + 0.35f * settings.CursorSpeed;
            if (sound != null)
            {
                sound.SetVolume(settings.Volume / 10f);
            }
        }

        /// <summary>Keeps records and settings in memory only (tests and screenshots).</summary>
        public void UseMemoryStore()
        {
            store = new MemoryRecordStore();
            LoadOptions();
        }

        /// <summary>Makes the next rounds repeatable.</summary>
        public void Seed(int value) => seed = value;

        int NextSeed() => seed++;

        // ---- Flow ----

        public void ToTitle(int selected = 0)
        {
            Screen = GameScreen.Title;
            StartDemo();
            hud.ShowPlayHud(false);
            hud.HideBanner();
            hud.ClearFloaters();
            sound.PlayMusic(art.TitleMusic);
            sound.SetDucked(false);
            sound.MusicPitch = 1f;
            ShowTitleMenu(selected);
        }

        void StartDemo()
        {
            Demo = true;
            sound.Silent = true;
            Round = new Round(GameMode.Timed, Difficulty.Normal, NextSeed());
            hitStop = 0f;
        }

        /// <summary>Starts a round with the current mode and difficulty.</summary>
        public void StartRound()
        {
            Demo = false;
            sound.Silent = false;
            Round = new Round(settings.Mode, settings.Difficulty, NextSeed());
            Screen = GameScreen.Playing;
            hitStop = 0f;
            overTime = 0f;
            recorded = false;
            lastRecord = default;
            lastTick = int.MaxValue;
            hud.HideMenu();
            hud.HideBanner();
            hud.ClearFloaters();
            hud.ShowPlayHud(true);
            hud.SetRound(settings.Mode == GameMode.Timed, ModeRules.StartingLives, ModeTag(settings.Mode, settings.Difficulty),
                records.Best(settings.Mode, settings.Difficulty));
            hud.SetTimer(Round.TimeLeft);
            hud.SetLives(Round.Lives);
            cameraRig.Calm();
            input.CenterCursor();
            sound.PlayMusic(art.PlayMusic);
            sound.SetDucked(false);
            sound.MusicPitch = 1f;
            sound.Play(art.Go, 0.8f);
            hud.Banner("GO!", null, 700);
        }

        public void Restart() => StartRound();

        public void TogglePause()
        {
            if (Screen == GameScreen.Playing)
            {
                Screen = GameScreen.Paused;
                sound.SetDucked(true);
                sound.PlayUi(art.Pause);
                ShowPauseMenu(0);
            }
            else if (Screen == GameScreen.Paused)
            {
                Screen = GameScreen.Playing;
                sound.SetDucked(false);
                sound.PlayUi(art.Pause);
                hud.HideMenu();
            }
        }

        void ShowResults()
        {
            Screen = GameScreen.Results;
            sound.SetDucked(true);
            sound.MusicPitch = 1f;
            ShowResultsMenu(0);
        }

        // ---- Frame ----

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxFrame);
            if (!ScriptedInput && input.Mute)
            {
                settings.Muted = !settings.Muted;
                sound.ToggleMute();
                settings.Save(store);
                UpdateSoundLabel();
            }

            IReadOnlyList<Vector2> clicks = ScriptedInput ? null : input.ReadClicks(dt);
            switch (Screen)
            {
                case GameScreen.Playing:
                    if (!ScriptedInput && input.Pause)
                    {
                        TogglePause();
                        break;
                    }
                    if (clicks != null)
                    {
                        foreach (Vector2 click in clicks)
                        {
                            ClickScreen(click);
                        }
                    }
                    StepRound(dt);
                    break;
                case GameScreen.Paused:
                case GameScreen.Results:
                    if (!ScriptedInput)
                    {
                        MenuInput();
                    }
                    break;
                default:
                    if (!ScriptedInput)
                    {
                        MenuInput();
                    }
                    StepDemo(dt);
                    break;
            }

            // The reticle replaces the system pointer while a round is played with the mouse.
            UnityEngine.Cursor.visible = !(Screen == GameScreen.Playing && input.Source == PointerSource.Mouse && !ScriptedInput);

            if (Round != null)
            {
                bool aiming = Screen == GameScreen.Playing;
                Vector2 cursor = ScriptedCursor ?? ScreenToWorld(input.Cursor);
                field.Sync(Round, cursor, aiming && (ScriptedCursor.HasValue || !ScriptedInput), Screen == GameScreen.Paused ? 0f : dt);
                if (!Demo)
                {
                    UpdateHud();
                }
            }
        }

        void StepDemo(float dt)
        {
            if (Round.IsOver)
            {
                StartDemo();
            }
            Round.Step(dt);
            Round.ClearEvents();
        }

        void StepRound(float dt)
        {
            if (Round.IsOver)
            {
                React();
                // Real time, so a slow frame cannot stretch the pause before the results.
                overTime += Time.unscaledDeltaTime;
                if (overTime >= ResultsDelay)
                {
                    ShowResults();
                }
                return;
            }
            if (!Consume(ref dt))
            {
                Round.Step(dt);
            }
            React();
            TickClock();
        }

        /// <summary>A click or tap at a screen point (pixels, origin bottom-left).</summary>
        public void ClickScreen(Vector2 screen)
        {
            if (Screen != GameScreen.Playing || hud.IsOverPauseButton(screen))
            {
                return;
            }
            ClickWorld(ScreenToWorld(screen));
        }

        /// <summary>A click at a point of the field in world units.</summary>
        public void ClickWorld(Vector2 world)
        {
            if (Screen != GameScreen.Playing || Round == null)
            {
                return;
            }
            Round.Click(new Vec2(world.x, world.y));
            React();
        }

        Vector2 ScreenToWorld(Vector2 screen)
        {
            Camera view = cameraRig.View;
            Vector3 world = view.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            return new Vector2(world.x, world.y);
        }

        /// <summary>Spends hit-stop time; true when the round should not move this frame.</summary>
        bool Consume(ref float dt)
        {
            if (hitStop <= 0f)
            {
                return false;
            }
            hitStop -= dt;
            if (hitStop > 0f)
            {
                return true;
            }
            dt = -hitStop;
            hitStop = 0f;
            return dt <= 0f;
        }

        void Freeze(float seconds)
        {
            if (HitStopEnabled)
            {
                hitStop = Mathf.Max(hitStop, seconds);
            }
        }

        void TickClock()
        {
            if (Round.Mode != GameMode.Timed || Round.IsOver)
            {
                return;
            }
            int whole = Mathf.CeilToInt(Round.TimeLeft);
            if (whole <= 5 && whole < lastTick)
            {
                sound.Play(art.Tick, 0.7f, whole <= 2 ? 1.25f : 1f, 0f);
            }
            lastTick = whole;
        }

        // ---- Game feel ----

        void React()
        {
            foreach (RoundEvent e in Round.Events)
            {
                var at = new Vector2(e.Position.X, e.Position.Y);
                switch (e.Kind)
                {
                    case RoundEventKind.Popped:
                        OnPopped(e, at);
                        break;
                    case RoundEventKind.MultiplierUp:
                        sound.Play(art.Multiplier, 0.8f);
                        hud.Banner($"x{(int)e.Value} COMBO", null, 650);
                        cameraRig.Shake(0.12f);
                        break;
                    case RoundEventKind.PowerUp:
                        OnPowerUp(e, at);
                        break;
                    case RoundEventKind.PowerEnded:
                        sound.Play(art.PowerEnd, 0.7f);
                        if (e.Target == TargetKind.SlowTime)
                        {
                            sound.MusicPitch = 1f;
                        }
                        break;
                    case RoundEventKind.Hazard:
                        sound.Play(art.Bad, 0.9f);
                        Effects.Burst(at, Vector2.zero, art.Glow, Palette.Bad, Palette.Danger, 30, 6.5f, 0.4f, 0.5f);
                        cameraRig.Shake(0.35f);
                        cameraRig.Kick(-at, 0.15f);
                        Freeze(0.06f);
                        break;
                    case RoundEventKind.Bomb:
                        sound.Play(art.Bomb, 1f, 1f, 0f);
                        Effects.Burst(at, Vector2.zero, art.Glow, Palette.Danger, Palette.Gold, 90, 13f, 0.45f, 1.1f);
                        Effects.Burst(at, Vector2.zero, art.Glow, Color.white, Palette.Tangerine, 30, 5f, 0.6f, 0.6f);
                        cameraRig.Shake(1f);
                        cameraRig.Kick(-at, 0.4f);
                        Freeze(0.22f);
                        hud.Banner("BOOM!", "danger", 1300);
                        break;
                    case RoundEventKind.Fell:
                        sound.Play(art.Fell, 0.6f);
                        Effects.Burst(new Vector2(at.x, -4.9f), Vector2.up, art.Glow, Palette.Floor, Palette.Good(0), 10, 3f, 0.16f, 0.4f, 90f);
                        hud.Float(new Vector2(Mathf.Clamp(at.x, -7f, 7f), -4.3f), "MISS", Palette.Floor, 22f);
                        break;
                    case RoundEventKind.Whiff:
                        sound.Play(art.Whiff, 0.45f);
                        Effects.Burst(at, Vector2.zero, art.Glow, Palette.Star, Color.white, 5, 1.5f, 0.1f, 0.25f);
                        break;
                    case RoundEventKind.ComboBroken:
                        if (e.Value >= 5f)
                        {
                            sound.Play(art.ComboBreak, 0.7f);
                            hud.Float(at + new Vector2(0f, 0.8f), "COMBO LOST", Palette.Hex("#a59bd8"), 20f);
                        }
                        break;
                    case RoundEventKind.LifeLost:
                        sound.Play(art.LifeLost, 0.8f);
                        hud.SetLives((int)e.Value);
                        hud.Float(at + new Vector2(0f, 0.9f), "-1 LIFE", Palette.Floor, 24f);
                        cameraRig.Shake(0.25f);
                        break;
                    case RoundEventKind.TimeChanged:
                        bool gain = e.Value > 0f;
                        hud.Float(at + new Vector2(0f, gain ? -0.7f : 0.9f), gain ? $"+{e.Value:0} S" : $"{e.Value:0} S",
                            gain ? Palette.Mint : Palette.Danger, 24f);
                        break;
                    case RoundEventKind.Over:
                        OnOver();
                        break;
                }
            }
            Round.ClearEvents();
        }

        void OnPopped(RoundEvent e, Vector2 at)
        {
            bool bonus = e.Target == TargetKind.Bonus;
            int combo = (int)e.Value;
            int multiplier = ComboScore.MultiplierFor(combo);
            Color color = bonus ? Palette.Gold : Palette.Good(e.Variant);
            if (bonus)
            {
                sound.Play(art.Bonus, 0.9f);
                Effects.Burst(at, Vector2.zero, art.Glow, Palette.Gold, Color.white, 50, 8.5f, 0.45f, 0.7f);
                cameraRig.Shake(0.16f);
                Freeze(0.05f);
            }
            else
            {
                sound.Play(art.Pop, 0.8f, 1f + 0.035f * Mathf.Min(combo, 20));
                Effects.Burst(at, Vector2.zero, art.Glow, color, Color.white, 22 + 3 * multiplier, 5f + multiplier, 0.36f, 0.45f);
                cameraRig.Shake(0.04f + 0.015f * multiplier);
                Freeze(0.012f + 0.006f * multiplier);
            }
            hud.Float(at + new Vector2(0f, 0.55f), $"+{e.Points}", bonus ? Palette.Gold : multiplier > 1 ? Palette.Gold : Color.white,
                22f + 4f * multiplier + (bonus ? 10f : 0f));
        }

        void OnPowerUp(RoundEvent e, Vector2 at)
        {
            bool slow = e.Target == TargetKind.SlowTime;
            sound.Play(slow ? art.SlowTime : art.Magnet, 0.9f, 1f, 0f);
            Color color = Palette.Of(e.Target);
            Effects.Burst(at, Vector2.zero, art.Glow, color, Color.white, 44, 7.5f, 0.4f, 0.7f);
            cameraRig.Shake(0.2f);
            hud.Banner(slow ? "SLOW TIME" : "MAGNET", slow ? "ice" : "magnet", 900);
            if (slow)
            {
                sound.MusicPitch = 0.82f;
            }
        }

        void OnOver()
        {
            sound.MusicPitch = 1f;
            sound.PlayMusic(null);
            RecordResult();
            switch (Round.EndReason)
            {
                case EndReason.TimeUp:
                    sound.Play(art.TimeUp, 0.9f, 1f, 0f);
                    hud.Banner("TIME UP", null, 1300);
                    break;
                case EndReason.OutOfLives:
                    hud.Banner("OUT OF LIVES", "danger", 1300);
                    break;
            }
            if (lastRecord.NewBest)
            {
                sound.Play(art.NewRecord, 0.9f, 1f, 0f);
                hud.SetBest(Round.Score.Points);
            }
            else
            {
                sound.Play(art.RoundOver, 0.7f, 1f, 0f);
            }
        }

        void RecordResult()
        {
            if (recorded)
            {
                return;
            }
            recorded = true;
            lastRecord = records.Submit(Round.Mode, Round.Difficulty, Round.Score.Points, Round.Score.BestCombo);
        }

        void UpdateHud()
        {
            hud.SetScore(Round.Score.Points);
            hud.SetCombo(Round.Score.Combo, Round.Score.Multiplier);
            if (Round.Mode == GameMode.Timed)
            {
                hud.SetTimer(Round.TimeLeft);
            }
            hud.SetLives(Round.Lives);
            hud.SetPowers(Round.SlowLeft, Round.MagnetLeft);
            hud.SetSlowFrame(Round.SlowLeft > 0f && Screen == GameScreen.Playing);
            hud.SetPauseButton(Screen == GameScreen.Playing);
            string prompt = null;
            if (Screen == GameScreen.Playing && Round.Elapsed < PromptSeconds)
            {
                prompt = input.UsingGamepad
                    ? "STICK TO AIM  -  A TO POP  -  START TO PAUSE"
                    : "CLICK OR TAP THE CANDY  -  AVOID SPIKES AND BOMBS";
            }
            hud.SetPrompt(prompt);
        }

        // ---- Menus ----

        void ShowTitleMenu(int selected)
        {
            var menu = new MenuModel
            {
                Logo = true,
                Subtitle = "POP THE CANDY  -  DODGE THE SPIKES  -  NEVER TOUCH A BOMB",
                Selected = selected,
                Hint = MenuHint(withChange: true),
            };
            menu.Add("play", "PLAY");
            menu.Add("mode", "MODE", Name(settings.Mode));
            menu.Add("difficulty", "DIFFICULTY", Name(settings.Difficulty));
            menu.Add("howto", "HOW TO PLAY");
            menu.Add("records", "HIGH SCORES");
            menu.Add("settings", "SETTINGS");
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                menu.Add("quit", "QUIT");
            }
            hud.ShowMenu(menu);
        }

        void ShowHowToPlay()
        {
            Screen = GameScreen.HowToPlay;
            var menu = new MenuModel { Title = "HOW TO PLAY", Subtitle = "CLICK, TAP OR AIM WITH A GAMEPAD", Hint = MenuHint() };
            menu.Body.Add(("CANDY", "+10, BUILDS THE COMBO", false));
            menu.Body.Add(("GOLD STAR", "+50  (+2 S IN TIMED)", false));
            menu.Body.Add(("SPIKY BALL", "-1 LIFE  /  -3 S", false));
            menu.Body.Add(("BOMB", "ENDS THE ROUND", false));
            menu.Body.Add(("CLOCK", "SLOW TIME FOR 5 S", false));
            menu.Body.Add(("MAGNET", "WIDE CLICKS FOR 6 S, SAFE ONLY", false));
            menu.Body.Add(("EVERY 5 POPS IN A ROW", "x2, x3 ... UP TO x5", false));
            menu.Body.Add(("MISSED CLICK OR DROPPED CANDY", "COMBO LOST", false));
            menu.Add("back", "BACK");
            hud.ShowMenu(menu);
        }

        void ShowRecordsMenu()
        {
            Screen = GameScreen.Records;
            var menu = new MenuModel { Title = "HIGH SCORES", Subtitle = "BEST SCORES ON THIS DEVICE", Hint = MenuHint() };
            foreach (GameMode mode in new[] { GameMode.Timed, GameMode.Lives })
            {
                foreach (Difficulty level in new[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard })
                {
                    menu.Body.Add(($"{Name(mode)}  -  {Name(level)}", records.Best(mode, level).ToString(), false));
                }
            }
            menu.Body.Add(("BEST COMBO", records.BestCombo.ToString(), false));
            menu.Body.Add(("ROUNDS PLAYED", records.RoundsPlayed.ToString(), false));
            menu.Add("back", "BACK");
            hud.ShowMenu(menu);
        }

        void ShowSettingsMenu(int selected)
        {
            Screen = GameScreen.Settings;
            var menu = new MenuModel { Title = "SETTINGS", Selected = selected, Hint = MenuHint(withChange: true) };
            menu.Add("volume", "VOLUME", settings.Volume.ToString());
            menu.Add("shake", "SCREEN SHAKE", settings.ScreenShake ? "ON" : "OFF");
            menu.Add("cursor", "GAMEPAD CURSOR SPEED", settings.CursorSpeed.ToString());
            menu.Add("mute", "SOUND", settings.Muted ? "OFF" : "ON");
            menu.Add("back", "BACK");
            hud.ShowMenu(menu);
        }

        void ShowPauseMenu(int selected)
        {
            var menu = new MenuModel
            {
                Title = "PAUSED",
                Subtitle = ModeTag(Round.Mode, Round.Difficulty),
                Selected = selected,
                LightDim = true,
                Hint = MenuHint(),
            };
            menu.Add("resume", "RESUME");
            menu.Add("restart", "RESTART ROUND");
            menu.Add("menu", "MAIN MENU");
            hud.ShowMenu(menu);
        }

        void ShowResultsMenu(int selected)
        {
            string title = Round.EndReason == EndReason.Bomb ? "BOOM!" : Round.EndReason == EndReason.OutOfLives ? "OUT OF LIVES" : "TIME UP";
            var menu = new MenuModel
            {
                Title = title,
                TitleDanger = Round.EndReason != EndReason.TimeUp,
                Subtitle = ModeTag(Round.Mode, Round.Difficulty),
                Selected = selected,
                LightDim = true,
                Hint = MenuHint(),
            };
            int best = records.Best(Round.Mode, Round.Difficulty);
            menu.Body.Add(("SCORE", Round.Score.Points + (lastRecord.NewBest ? "   NEW BEST!" : string.Empty), lastRecord.NewBest));
            menu.Body.Add(("BEST", best.ToString(), false));
            menu.Body.Add(("BEST COMBO", Round.Score.BestCombo + (lastRecord.NewBestCombo ? "   NEW RECORD" : string.Empty), lastRecord.NewBestCombo));
            menu.Body.Add(("TARGETS POPPED", Round.Popped.ToString(), false));
            menu.Body.Add(("ACCURACY", $"{Mathf.RoundToInt(Round.Accuracy * 100f)}%", false));
            menu.Add("again", "PLAY AGAIN");
            menu.Add("menu", "MAIN MENU");
            hud.ShowMenu(menu);
        }

        string MenuHint(bool withChange = false)
        {
            if (input.UsingGamepad)
            {
                return withChange ? "D-PAD TO CHOOSE AND CHANGE  -  A TO SELECT  -  B TO GO BACK" : "D-PAD TO CHOOSE  -  A TO SELECT  -  B TO GO BACK";
            }
            return withChange
                ? "ARROWS TO CHOOSE AND CHANGE  -  ENTER TO SELECT  -  ESC TO GO BACK"
                : "ARROWS TO CHOOSE  -  ENTER TO SELECT  -  ESC TO GO BACK";
        }

        void MenuInput()
        {
            MenuModel menu = hud.Menu;
            if (menu == null)
            {
                return;
            }
            int vertical = input.VerticalStep();
            if (vertical != 0 && menu.Items.Count > 0)
            {
                menu.Selected = (menu.Selected + vertical + menu.Items.Count) % menu.Items.Count;
                sound.PlayUi(art.Move, 0.6f);
                hud.RefreshMenu();
            }
            int horizontal = input.HorizontalStep();
            if (horizontal != 0 && menu.Items.Count > 0 && menu.Items[menu.Selected].Value != null)
            {
                Change(menu.Items[menu.Selected].Id, horizontal);
            }
            if (input.Confirm && menu.Items.Count > 0)
            {
                Activate(menu.Items[menu.Selected].Id);
            }
            else if (input.Back || (Screen == GameScreen.Paused && input.Pause))
            {
                GoBack();
            }
        }

        void OnItemClicked(int index)
        {
            MenuModel menu = hud.Menu;
            if (menu == null || index < 0 || index >= menu.Items.Count)
            {
                return;
            }
            menu.Selected = index;
            Activate(menu.Items[index].Id);
        }

        void GoBack()
        {
            switch (Screen)
            {
                case GameScreen.HowToPlay:
                case GameScreen.Records:
                case GameScreen.Settings:
                    Activate("back");
                    break;
                case GameScreen.Paused:
                    TogglePause();
                    break;
                case GameScreen.Results:
                    sound.PlayUi(art.Move, 0.6f);
                    ToTitle();
                    break;
            }
        }

        /// <summary>Runs a menu row by its id, as Enter, a click or a tap would.</summary>
        public void Activate(string id)
        {
            switch (id)
            {
                case "play":
                case "again":
                case "restart":
                    sound.PlayUi(art.Select);
                    StartRound();
                    return;
                case "howto":
                    sound.PlayUi(art.Select);
                    ShowHowToPlay();
                    return;
                case "records":
                    sound.PlayUi(art.Select);
                    ShowRecordsMenu();
                    return;
                case "settings":
                    sound.PlayUi(art.Select);
                    ShowSettingsMenu(0);
                    return;
                case "quit":
                    Application.Quit();
                    return;
                case "back":
                    sound.PlayUi(art.Move, 0.6f);
                    string from = Screen == GameScreen.HowToPlay ? "howto" : Screen == GameScreen.Records ? "records" : "settings";
                    Screen = GameScreen.Title;
                    ShowTitleMenu(0);
                    hud.Menu.Selected = Mathf.Max(0, hud.Menu.IndexOf(from));
                    hud.RefreshMenu();
                    return;
                case "resume":
                    TogglePause();
                    return;
                case "menu":
                    sound.PlayUi(art.Select);
                    ToTitle();
                    return;
                default:
                    Change(id, 1);
                    return;
            }
        }

        /// <summary>Steps a setting row left or right, saves it and redraws its menu.</summary>
        public void Change(string id, int step)
        {
            switch (id)
            {
                case "mode":
                    settings.CycleMode(step);
                    break;
                case "difficulty":
                    settings.CycleDifficulty(step);
                    break;
                case "volume":
                    settings.StepVolume(step);
                    break;
                case "shake":
                    settings.ScreenShake = !settings.ScreenShake;
                    break;
                case "cursor":
                    settings.StepCursorSpeed(step);
                    break;
                case "mute":
                    settings.Muted = !settings.Muted;
                    sound.ToggleMute();
                    UpdateSoundLabel();
                    break;
                default:
                    return;
            }
            settings.Save(store);
            ApplyOptions();
            if (id == "shake" && settings.ScreenShake)
            {
                cameraRig.Shake(0.4f);
            }
            sound.PlayUi(art.Move, 0.6f);
            int selected = hud.Menu != null ? hud.Menu.Selected : 0;
            if (Screen == GameScreen.Settings)
            {
                ShowSettingsMenu(selected);
            }
            else
            {
                ShowTitleMenu(selected);
            }
        }

        void UpdateSoundLabel() => hud.SetSound(settings.Muted ? "SOUND OFF  (M)" : "SOUND ON  (M)");

        public static string ModeTag(GameMode mode, Difficulty difficulty) => $"{Name(mode)}  -  {Name(difficulty)}";

        static string Name(Difficulty difficulty) => difficulty.ToString().ToUpperInvariant();

        static string Name(GameMode mode) => mode.ToString().ToUpperInvariant();
    }
}
