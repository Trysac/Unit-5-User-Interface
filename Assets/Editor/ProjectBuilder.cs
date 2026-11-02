using System;
using System.IO;
using TargetToss.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace TargetToss.EditorTools
{
    /// <summary>
    /// Generates the materials, font atlas, art asset, icon, scene and player settings from code,
    /// and builds the players, so every binary asset can be recreated from text. Batch mode:
    /// <code>
    /// Unity -batchmode -quit -projectPath . -executeMethod TargetToss.EditorTools.ProjectBuilder.SetUpProject
    /// Unity -batchmode -quit -projectPath . -executeMethod TargetToss.EditorTools.ProjectBuilder.BuildWindows -buildOutput &lt;folder&gt;
    /// Unity -batchmode -quit -projectPath . -executeMethod TargetToss.EditorTools.ProjectBuilder.BuildWebGL -buildOutput &lt;folder&gt;
    /// </code>
    /// </summary>
    public static class ProjectBuilder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string ArtFolder = "Assets/Art";
        const string ArtPath = "Assets/Art/GameArt.asset";
        const string SoftDotPath = "Assets/Art/SoftDot.png";
        const string IconPath = "Assets/Art/AppIcon.png";
        const string AudioFolder = "Assets/Audio";
        const string DisplayFontSource = "Assets/Fonts/Exo2-Black.ttf";
        const string DisplayFontPath = "Assets/UI/ExoBlack.asset";
        const string TextFontSource = "Assets/Fonts/Exo2-SemiBold.ttf";
        const string TextFontPath = "Assets/UI/ExoSemiBold.asset";
        const string LayoutPath = "Assets/UI/Hud.uxml";
        const string StylePath = "Assets/UI/Hud.uss";
        const string ThemePath = "Assets/UI/HudTheme.tss";
        const string PanelSettingsPath = "Assets/UI/HudPanelSettings.asset";
        const string Ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        [UnityEditor.MenuItem("Target Toss/Regenerate Art, Scene and Player Settings")]
        public static void SetUpProject()
        {
            CreateFont(DisplayFontSource, DisplayFontPath, "ExoBlack");
            CreateFont(TextFontSource, TextFontPath, "ExoSemiBold");
            // The style sheet refers to the fonts by path; re-import it so it picks up the new assets.
            AssetDatabase.ImportAsset(StylePath, ImportAssetOptions.ForceUpdate);
            GameArt art = CreateArt();
            PanelSettings panel = CreatePanelSettings();
            CreateScene(art, panel);
            ConfigurePlayer(CreateIcon());
            AssetDatabase.SaveAssets();
            Debug.Log("Target Toss project set up.");
        }

        /// <summary>
        /// Enters play mode and lets ScreenshotScenarios capture the README screenshots, then exits.
        /// Run in batch mode without -quit: <c>-executeMethod ...CaptureScreenshots -screenshot &lt;folder&gt;</c>.
        /// </summary>
        public static void CaptureScreenshots()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "TargetToss.exe");

        public static void BuildWebGL() => Build(BuildTarget.WebGL, string.Empty);

        // ---- Font ----

        /// <summary>
        /// A static font atlas of the printable ASCII range from an Exo 2 TrueType font (used
        /// unmodified under the SIL Open Font License; the atlas only rasterises it).
        /// </summary>
        static FontAsset CreateFont(string sourcePath, string assetPath, string name)
        {
            if (AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            FontAsset font = FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, false);
            font.name = name;
            if (!font.TryAddCharacters(Ascii, out string missing))
            {
                Debug.LogWarning($"{name} is missing: {missing}");
            }
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, assetPath);
            foreach (Texture2D atlas in font.atlasTextures)
            {
                atlas.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
            font.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            return font;
        }

        // ---- Art ----

        static GameArt CreateArt()
        {
            Directory.CreateDirectory(ArtFolder);
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(ArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<GameArt>();
                AssetDatabase.CreateAsset(art, ArtPath);
            }

            Texture2D dot = CreateSoftDot();
            art.Glow = MaterialAt("Glow", Shader.Find("Legacy Shaders/Particles/Additive"));
            art.Glow.mainTexture = dot;
            art.Glow.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorUtility.SetDirty(art.Glow);
            art.TitleMusic = Clip("title_theme", music: true);
            art.PlayMusic = Clip("play_theme", music: true);
            art.Pop = Clip("pop");
            art.Bonus = Clip("bonus");
            art.Bad = Clip("bad");
            art.Bomb = Clip("bomb");
            art.Whiff = Clip("whiff");
            art.Fell = Clip("fell");
            art.SlowTime = Clip("slow_time");
            art.Magnet = Clip("magnet");
            art.PowerEnd = Clip("power_end");
            art.Multiplier = Clip("multiplier");
            art.ComboBreak = Clip("combo_break");
            art.LifeLost = Clip("life_lost");
            art.Tick = Clip("tick");
            art.TimeUp = Clip("time_up");
            art.NewRecord = Clip("new_record");
            art.RoundOver = Clip("round_over");
            art.Go = Clip("go");
            art.Move = Clip("move");
            art.Select = Clip("select");
            art.Pause = Clip("pause");
            EditorUtility.SetDirty(art);
            return art;
        }

        static Material MaterialAt(string name, Shader shader)
        {
            string path = $"{ArtFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            return material;
        }

        /// <summary>A small white disc with a soft edge for every particle and glow.</summary>
        static Texture2D CreateSoftDot()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha * (3f - 2f * alpha)));
                }
            }
            texture.Apply();
            File.WriteAllBytes(SoftDotPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SoftDotPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SoftDotPath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(SoftDotPath);
        }

        static AudioClip Clip(string name, bool music = false)
        {
            string path = $"{AudioFolder}/{name}.ogg";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Missing {path}: run python tools/audio/build.py first.");
                return null;
            }
            AudioImporterSampleSettings sample = importer.defaultSampleSettings;
            sample.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = sample;
            importer.forceToMono = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---- UI ----

        static PanelSettings CreatePanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.clearColor = false;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        // ---- Scene ----

        static void CreateScene(GameArt art, PanelSettings panel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            cameraObject.AddComponent<AudioListener>();
            CameraRig rig = cameraObject.AddComponent<CameraRig>();

            var ui = new GameObject("HUD");
            var document = ui.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            Hud hud = ui.AddComponent<Hud>();

            var game = new GameObject("Game");
            var director = game.AddComponent<GameDirector>();
            var serialized = new SerializedObject(director);
            serialized.FindProperty("art").objectReferenceValue = art;
            serialized.FindProperty("hud").objectReferenceValue = hud;
            serialized.FindProperty("cameraRig").objectReferenceValue = rig;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer(Texture2D icon)
        {
            PlayerSettings.companyName = "Trysac";
            PlayerSettings.productName = "Target Toss";
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);
            var standaloneIcons = new Texture2D[sizes.Length];
            Array.Fill(standaloneIcons, icon);
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, standaloneIcons, IconKind.Any);
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            // Static hosts such as GitHub Pages do not send Content-Encoding for .br/.gz files.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
        }

        // ---- Icon: a candy orb in the reticle, tossed up an arc, with a gold star ----

        static Texture2D CreateIcon()
        {
            const int size = 1024;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = IconPixel(x + 0.5f - size / 2f, y + 0.5f - size / 2f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(IconPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        }

        static Color IconPixel(float x, float y)
        {
            const float aa = 1.5f;
            float tile = RoundedBox(x, y, 470f, 470f, 170f);
            Color color = Color.Lerp(Palette.Background, Palette.BackgroundTop, Mathf.Clamp01((y + 470f) / 940f));
            color.a = Coverage(-tile, aa);

            // The toss: a dotted arc rising from the bottom-left.
            for (int i = 0; i < 5; i++)
            {
                float t = i / 5f;
                float dx = -330f + t * 240f;
                float dy = -380f + 520f * t - 260f * t * t;
                float dot = Length(x - dx, y - dy) - (14f + i * 4f);
                color = Over(color, Palette.Mint, Coverage(-dot, aa) * (0.25f + 0.12f * i) * color.a);
            }

            // A glossy candy orb with its glow.
            float cx = x + 20f, cy = y + 10f;
            float orb = Length(cx, cy) - 250f;
            color = Over(color, Palette.Mint, Mathf.Clamp01(1f - orb / 120f) * 0.35f * color.a);
            if (orb < aa)
            {
                float light = Mathf.Clamp01(0.5f + 0.5f * (-cx * 0.6f + cy * 0.8f) / 250f);
                Color body = Color.Lerp(Palette.Mint * 0.62f, Color.Lerp(Palette.Mint, Color.white, 0.25f), light);
                body = Color.Lerp(body, Palette.Mint * 0.45f, Coverage(Length(cx, cy) - 222f, 6f));
                float shine = Length((cx + 95f) / 1.7f, cy - 110f) - 48f;
                body = Color.Lerp(body, Color.white, Coverage(-shine, 6f) * 0.85f);
                color = Over(color, body, Coverage(-orb, aa));
            }

            // The reticle over it.
            float ring = Mathf.Abs(Length(cx, cy) - 330f) - 16f;
            color = Over(color, Color.white, Coverage(-ring, aa) * 0.95f);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI / 2f;
                float c = Mathf.Cos(angle), sn = Mathf.Sin(angle);
                float u = c * cx + sn * cy, v = -sn * cx + c * cy;
                float tick = RoundedBox(u - 390f, v, 50f, 14f, 10f);
                color = Over(color, Color.white, Coverage(-tick, aa) * 0.95f);
            }

            // A gold star in the corner.
            float star = Length(x - 300f, y - 300f) - 70f;
            color = Over(color, Palette.Gold, Mathf.Clamp01(1f - star / 60f) * 0.4f * color.a);
            float arms = Mathf.Min(RoundedBox(x - 300f, y - 300f, 85f, 18f, 18f), RoundedBox(x - 300f, y - 300f, 18f, 85f, 18f));
            color = Over(color, Palette.Gold, Coverage(-Mathf.Min(arms, Length(x - 300f, y - 300f) - 34f), aa));
            return color;
        }

        static float Length(float x, float y) => Mathf.Sqrt(x * x + y * y);

        static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(x) - (halfWidth - radius);
            float qy = Mathf.Abs(y) - (halfHeight - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static float Coverage(float inside, float width) => Mathf.Clamp01(inside / width + 0.5f);

        static Color Over(Color below, Color above, float amount)
        {
            Color mixed = Color.Lerp(below, above, amount);
            mixed.a = below.a;
            return mixed;
        }

        // ---- Builds ----

        static void Build(BuildTarget target, string fileName)
        {
            string folder = Argument("-buildOutput");
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentException("Pass -buildOutput <folder> outside the repository.");
            }
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                locationPathName = string.IsNullOrEmpty(fileName) ? folder : Path.Combine(folder, fileName),
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Build {target}: {summary.result}, {summary.totalSize / (1024 * 1024f):0.0} MB, {summary.totalErrors} errors.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
