using System;
using System.Collections;
using System.IO;
using TargetToss.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TargetToss.Game
{
    /// <summary>
    /// Opt-in capture of README screenshots: <c>TargetToss.exe -screenshot &lt;folder&gt; -scenario title,play,powers,lives,pause,results,howto,records</c>.
    /// Without the -screenshot argument nothing is created, so normal runs and WebGL builds
    /// (which have no command line) are unaffected. The same hook runs inside the editor
    /// (ProjectBuilder.CaptureScreenshots), which renders the camera and the HUD off screen and
    /// blends them, for machines that cannot run the player.
    /// With <c>-frames N -fps F</c> each scenario is recorded instead as N numbered frames
    /// (<c>&lt;folder&gt;/&lt;scenario&gt;/frame_0000.png</c>) at a fixed timestep of 1/F seconds;
    /// the <c>gameplay</c> scenario plays a round with a scripted player for the README GIF.
    /// </summary>
    public sealed class ScreenshotScenarios : MonoBehaviour
    {
        const string DefaultScenarios = "title,play,powers,lives,pause,results,howto,records";
        const int Width = 1280;
        const int Height = 720;

        string folder;
        string[] scenarios;
        RenderTexture sceneTarget;
        RenderTexture uiTarget;
        GameDirector director;
        int frames;
        int fps = 15;
        int nextTarget;
        float clickCooldown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            string target = Argument("-screenshot");
            if (string.IsNullOrEmpty(target))
            {
                return;
            }
            var host = new GameObject(nameof(ScreenshotScenarios)).AddComponent<ScreenshotScenarios>();
            host.folder = target;
            host.scenarios = (Argument("-scenario") ?? DefaultScenarios).Split(',');
            int.TryParse(Argument("-frames"), out host.frames);
            if (int.TryParse(Argument("-fps"), out int rate) && rate > 0)
            {
                host.fps = rate;
            }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            director = FindAnyObjectByType<GameDirector>();
            director.UseMemoryStore();
            if (Application.isEditor)
            {
                RenderOffscreen();
            }
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (string raw in scenarios)
            {
                string scenario = raw.Trim();
                director.Seed(StableSeed(scenario));
                director.ScriptedCursor = null;
                yield return Stage(scenario);
                if (frames > 0)
                {
                    yield return Record(scenario);
                    continue;
                }
                Texture2D shot;
                if (Application.isEditor)
                {
                    yield return null;
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(folder, scenario + ".png"), shot.EncodeToPNG());
                Destroy(shot);
                Debug.Log($"Saved screenshot {scenario}.png");
                director.ScriptedInput = false;
                director.ScriptedCursor = null;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }

        IEnumerator Stage(string scenario)
        {
            director.Settings.Mode = GameMode.Timed;
            director.Settings.Difficulty = Difficulty.Normal;
            switch (scenario)
            {
                case "title":
                    director.ToTitle();
                    yield return new WaitForSecondsRealtime(3.2f);
                    break;
                case "howto":
                    director.ToTitle();
                    director.Activate("howto");
                    yield return new WaitForSecondsRealtime(1.5f);
                    break;
                case "records":
                    director.Records.Submit(GameMode.Timed, Difficulty.Easy, 1840, 22);
                    director.Records.Submit(GameMode.Timed, Difficulty.Normal, 2365, 31);
                    director.Records.Submit(GameMode.Timed, Difficulty.Hard, 1590, 17);
                    director.Records.Submit(GameMode.Lives, Difficulty.Easy, 3120, 26);
                    director.Records.Submit(GameMode.Lives, Difficulty.Normal, 1975, 19);
                    director.ToTitle();
                    director.Activate("records");
                    yield return new WaitForSecondsRealtime(1.5f);
                    break;
                case "play":
                    yield return Play(GameMode.Timed, 23);
                    Arrange(
                        (TargetKind.Good, -5.4f, 1.2f, 0), (TargetKind.Good, -2.6f, 2.6f, 1), (TargetKind.Bad, 0.4f, 0.6f, 0),
                        (TargetKind.Good, 3.2f, 2.2f, 2), (TargetKind.Bonus, 5.6f, 2.9f, 0), (TargetKind.Good, 1.6f, -1.8f, 1),
                        (TargetKind.Good, -1.2f, -2.9f, 2), (TargetKind.Bomb, 4.6f, -2.4f, 0), (TargetKind.Good, -4.2f, -1.4f, 1));
                    yield return PopVisible(new Vector2(-0.8f, 2.4f), 0);
                    director.ScriptedCursor = new Vector2(3.25f, 2.15f);
                    yield return new WaitForSecondsRealtime(0.12f);
                    break;
                case "powers":
                    yield return Play(GameMode.Timed, 9);
                    PopAt(TargetKind.SlowTime, new Vector2(-6f, -4.8f));
                    PopAt(TargetKind.Magnet, new Vector2(6f, -4.8f));
                    yield return new WaitForSecondsRealtime(1.2f);
                    Arrange(
                        (TargetKind.Good, -4.6f, 1.6f, 0), (TargetKind.Good, -3.2f, 2.5f, 2), (TargetKind.Good, -2.4f, 0.9f, 1),
                        (TargetKind.Bad, -3.4f, 1.2f, 0), (TargetKind.Bonus, -4.2f, -0.2f, 0), (TargetKind.Good, 2.8f, 1.8f, 1),
                        (TargetKind.Bomb, 4.6f, 0.4f, 0), (TargetKind.Good, 1.2f, -2.4f, 0), (TargetKind.Bad, 5.4f, 2.6f, 0));
                    director.ScriptedCursor = new Vector2(-3.4f, 1.4f);
                    yield return new WaitForSecondsRealtime(0.15f);
                    break;
                case "lives":
                    director.Settings.Mode = GameMode.Lives;
                    director.Settings.Difficulty = Difficulty.Hard;
                    yield return Play(GameMode.Lives, 12);
                    PopAt(TargetKind.Bad, new Vector2(0f, -4.8f));
                    yield return new WaitForSecondsRealtime(0.8f);
                    Arrange(
                        (TargetKind.Good, -5f, 2.2f, 2), (TargetKind.Bad, -2.8f, 1.1f, 0), (TargetKind.Bomb, -0.6f, 2.8f, 0),
                        (TargetKind.Good, 1.8f, 1.4f, 0), (TargetKind.Bad, 3.9f, -0.6f, 0), (TargetKind.Good, 5.8f, 2.4f, 1),
                        (TargetKind.SlowTime, -4.4f, -1.9f, 0), (TargetKind.Good, 0.9f, -2.6f, 1));
                    yield return PopVisible(new Vector2(3.4f, 3f), 2);
                    director.ScriptedCursor = new Vector2(1.85f, 1.35f);
                    yield return new WaitForSecondsRealtime(0.12f);
                    break;
                case "pause":
                    yield return Play(GameMode.Timed, 14);
                    Arrange(
                        (TargetKind.Good, -4.8f, 1.4f, 1), (TargetKind.Bad, -1.6f, 2.4f, 0), (TargetKind.Good, 1.4f, 0.4f, 2),
                        (TargetKind.Magnet, 4.4f, 2f, 0), (TargetKind.Good, 3f, -2.2f, 0));
                    yield return new WaitForSecondsRealtime(0.1f);
                    director.TogglePause();
                    yield return new WaitForSecondsRealtime(0.6f);
                    break;
                case "results":
                    yield return Play(GameMode.Timed, 34);
                    yield return new WaitForSecondsRealtime(0.3f);
                    director.Round.Step(ModeRules.RoundSeconds);
                    float waited = 0f;
                    while (director.Screen != GameScreen.Results && waited < 8f)
                    {
                        waited += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(0.6f);
                    break;
                case "gameplay":
                    UnityEngine.Random.InitState(7);
                    yield return Play(GameMode.Timed, 5);
                    director.Round.Spawning = true;
                    director.ScriptedCursor = new Vector2(0f, 0f);
                    yield return new WaitForSecondsRealtime(0.6f);
                    break;
                default:
                    Debug.LogWarning($"Unknown screenshot scenario '{scenario}'.");
                    break;
            }
        }

        /// <summary>Writes <see cref="frames"/> frames of the scenario at a fixed timestep.</summary>
        IEnumerator Record(string scenario)
        {
            string target = Path.Combine(folder, scenario);
            Directory.CreateDirectory(target);
            Time.captureFramerate = fps;
            for (int i = 0; i < frames; i++)
            {
                if (scenario == "gameplay")
                {
                    PlayStep(1f / fps);
                }
                yield return null;
                Texture2D shot;
                if (Application.isEditor)
                {
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(target, $"frame_{i:0000}.png"), shot.EncodeToPNG());
                Destroy(shot);
            }
            Time.captureFramerate = 0;
            Debug.Log($"Saved {frames} frames of {scenario}");
        }

        /// <summary>
        /// One frame of the scripted player: glide the reticle to the safe target nearest to it
        /// once it is well on screen, pop it, and keep away from spiky balls and bombs. Extra
        /// candy is tossed in so the GIF stays busy.
        /// </summary>
        void PlayStep(float dt)
        {
            Round round = director.Round;
            if (round == null || round.IsOver)
            {
                return;
            }
            nextTarget -= 1;
            if (nextTarget <= 0)
            {
                nextTarget = Mathf.RoundToInt(fps * 0.32f);
                float x = UnityEngine.Random.Range(-5.5f, 5.5f);
                float roll = UnityEngine.Random.value;
                TargetKind kind = roll < 0.12f ? TargetKind.Bonus : roll < 0.24f ? TargetKind.Bad : roll < 0.32f ? TargetKind.Bomb : TargetKind.Good;
                float rise = UnityEngine.Random.Range(11.5f, 13.5f);
                round.Place(kind, new Vec2(x, Field.LaunchY), new Vec2(-x * 0.12f, rise), UnityEngine.Random.Range(-90f, 90f),
                    UnityEngine.Random.Range(0, 3));
            }
            Vector2 cursor = director.ScriptedCursor ?? Vector2.zero;
            Target best = null;
            float bestDistance = float.MaxValue;
            foreach (Target candidate in round.Targets)
            {
                if (candidate.Kind == TargetKind.Bad || candidate.Kind == TargetKind.Bomb || candidate.Position.Y < -2.2f)
                {
                    continue;
                }
                float distance = Vector2.Distance(cursor, new Vector2(candidate.Position.X, candidate.Position.Y));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            if (best == null)
            {
                director.ScriptedCursor = Vector2.MoveTowards(cursor, new Vector2(0f, 0.5f), 6f * dt);
                return;
            }
            var aim = new Vector2(best.Position.X, best.Position.Y);
            cursor = Vector2.MoveTowards(cursor, aim, 26f * dt);
            director.ScriptedCursor = cursor;
            clickCooldown -= dt;
            if (clickCooldown <= 0f && Vector2.Distance(cursor, aim) < best.Radius * 0.6f)
            {
                director.ClickWorld(aim);
                clickCooldown = 0.12f;
            }
        }

        /// <summary>Starts a round and builds up a score and a combo with pops just below the screen.</summary>
        IEnumerator Play(GameMode mode, int pops)
        {
            director.Settings.Mode = mode;
            director.StartRound();
            director.ScriptedInput = true;
            director.Round.Spawning = false;
            yield return new WaitForSecondsRealtime(0.3f);
            for (int i = 0; i < pops; i++)
            {
                PopAt(i % 9 == 8 ? TargetKind.Bonus : TargetKind.Good, new Vector2(-6f + (i % 7) * 2f, -6f));
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
        }

        void PopAt(TargetKind kind, Vector2 at)
        {
            director.Round.Place(kind, new Vec2(at.x, at.y), Vec2.Zero);
            director.ClickWorld(at);
        }

        IEnumerator PopVisible(Vector2 at, int variant)
        {
            director.Round.Place(TargetKind.Good, new Vec2(at.x, at.y), Vec2.Zero, 0f, variant);
            director.ClickWorld(at);
            yield return new WaitForSecondsRealtime(0.08f);
        }

        /// <summary>Clears the field and lays out targets that drift slowly, as if near the top of their throws.</summary>
        void Arrange(params (TargetKind Kind, float X, float Y, int Variant)[] layout)
        {
            Round round = director.Round;
            round.ClearTargets();
            int i = 0;
            foreach ((TargetKind kind, float x, float y, int variant) in layout)
            {
                Target target = round.Place(kind, new Vec2(x, y), new Vec2(0f, 0.6f), 0f, variant);
                target.Angle = (i++ * 47) % 360;
            }
        }

        /// <summary>Points the camera and the HUD panel at render textures of a fixed size.</summary>
        void RenderOffscreen()
        {
            sceneTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            uiTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Camera.main.targetTexture = sceneTarget;
            var document = director.Hud.GetComponent<UIDocument>();
            PanelSettings panel = Instantiate(document.panelSettings);
            panel.targetTexture = uiTarget;
            panel.clearColor = true;
            panel.colorClearValue = Color.clear;
            document.panelSettings = panel;
        }

        /// <summary>Blends the HUD over the field, like the screen would show them.</summary>
        Texture2D CaptureOffscreen()
        {
            Color[] scene = Read(sceneTarget);
            Color[] ui = Read(uiTarget);
            for (int i = 0; i < scene.Length; i++)
            {
                Color over = ui[i];
                Color result = Color.Lerp(scene[i], new Color(over.r, over.g, over.b), over.a);
                result.a = 1f;
                scene[i] = result;
            }
            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            shot.SetPixels(scene);
            shot.Apply();
            return shot;
        }

        static Color[] Read(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            Color[] pixels = texture.GetPixels();
            Destroy(texture);
            return pixels;
        }

        static int StableSeed(string text)
        {
            int seed = 17;
            foreach (char c in text)
            {
                seed = seed * 31 + c;
            }
            return seed & 0xffff;
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
