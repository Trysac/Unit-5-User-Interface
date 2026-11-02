using System.Collections.Generic;
using TargetToss.Core;
using UnityEngine;

namespace TargetToss.Game
{
    /// <summary>
    /// Draws a round: the night-sky backdrop, a sprite for every target in flight (matched by id),
    /// the reticle and the magnet's pull radius.
    /// </summary>
    public sealed class FieldView : MonoBehaviour
    {
        sealed class TargetSprite
        {
            public GameObject Root;
            public SpriteRenderer Body;
            public SpriteRenderer Halo;
            public SpriteRenderer Glow;
            public SpriteRenderer Shine;
            public TargetKind Kind;
            public int Variant;
            public bool Seen;
        }

        readonly Dictionary<int, TargetSprite> shown = new Dictionary<int, TargetSprite>();
        readonly Stack<TargetSprite> pool = new Stack<TargetSprite>();
        readonly List<int> gone = new List<int>();
        Transform targetsRoot;
        SpriteRenderer reticle;
        SpriteRenderer magnetRing;
        SpriteRenderer slowTint;
        SpriteRenderer floorGlow;
        float time;

        public void Build()
        {
            CreateBackdrop();
            targetsRoot = new GameObject("Targets").transform;
            targetsRoot.SetParent(transform, false);

            magnetRing = Sprite("Magnet Range", Shapes.Ring, Palette.Magnet.WithAlpha(0.55f), 40);
            reticle = Sprite("Reticle", Shapes.Reticle, Color.white, 41);
            reticle.transform.localScale = Vector3.one * 0.9f;
        }

        void CreateBackdrop()
        {
            // A vertical gradient big enough for any aspect ratio.
            var gradient = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < 64; y++)
            {
                gradient.SetPixel(0, y, Color.Lerp(Palette.Background, Palette.BackgroundTop, y / 63f));
            }
            gradient.Apply(false, true);
            SpriteRenderer sky = Sprite("Sky", UnityEngine.Sprite.Create(gradient, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f), 1f), Color.white, -30);
            sky.transform.localScale = new Vector3(40f, 14f / 64f, 1f);

            var random = new System.Random(5);
            for (int i = 0; i < 70; i++)
            {
                var position = new Vector3((float)random.NextDouble() * 22f - 11f, (float)random.NextDouble() * 12f - 5.5f, 0f);
                float size = 0.04f + (float)random.NextDouble() * 0.09f;
                SpriteRenderer star = Sprite("Star", Shapes.SoftDot, Palette.Star.WithAlpha(0.25f + (float)random.NextDouble() * 0.5f), -29);
                star.transform.localPosition = position;
                star.transform.localScale = Vector3.one * size * 3f;
            }

            floorGlow = Sprite("Floor Glow", Shapes.SoftDot, Palette.Floor.WithAlpha(0.22f), -28);
            floorGlow.transform.localPosition = new Vector3(0f, -5.6f, 0f);
            floorGlow.transform.localScale = new Vector3(26f, 2.2f, 1f);

            slowTint = Sprite("Slow Tint", Shapes.Square, Palette.Ice.WithAlpha(0f), 30);
            slowTint.transform.localScale = new Vector3(40f, 14f, 1f);
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Color color, int order, Transform parent = null)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent != null ? parent : transform, false);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        /// <summary>Matches the sprites to the round's targets and moves the reticle.</summary>
        public void Sync(Round round, Vector2 cursor, bool showCursor, float deltaTime)
        {
            time += deltaTime;
            foreach (TargetSprite sprite in shown.Values)
            {
                sprite.Seen = false;
            }
            if (round != null)
            {
                foreach (Target target in round.Targets)
                {
                    if (!shown.TryGetValue(target.Id, out TargetSprite sprite))
                    {
                        sprite = Take(target.Kind, target.Variant);
                        shown[target.Id] = sprite;
                    }
                    sprite.Seen = true;
                    Place(sprite, target);
                }
            }
            gone.Clear();
            foreach (KeyValuePair<int, TargetSprite> pair in shown)
            {
                if (!pair.Value.Seen)
                {
                    gone.Add(pair.Key);
                }
            }
            foreach (int id in gone)
            {
                Return(shown[id]);
                shown.Remove(id);
            }

            bool magnet = round != null && round.MagnetActive && showCursor;
            reticle.enabled = showCursor;
            reticle.transform.localPosition = new Vector3(cursor.x, cursor.y, 0f);
            reticle.color = magnet ? Palette.Magnet : Color.white;
            magnetRing.enabled = magnet;
            if (magnet)
            {
                float pulse = 1f + 0.04f * Mathf.Sin(time * 10f);
                magnetRing.transform.localPosition = reticle.transform.localPosition;
                // The ring sprite's circle is 0.45 units across its radius at scale 1.
                magnetRing.transform.localScale = Vector3.one * (Round.MagnetRadius / 0.45f * pulse);
                float fade = round.MagnetLeft < 1.2f ? 0.25f + 0.3f * Mathf.Abs(Mathf.Sin(time * 12f)) : 0.55f;
                magnetRing.color = Palette.Magnet.WithAlpha(fade);
            }

            float slow = round != null && round.SlowLeft > 0f ? Mathf.Clamp01(round.SlowLeft / 0.4f) : 0f;
            Color tint = slowTint.color;
            tint.a = Mathf.MoveTowards(tint.a, slow * 0.13f, deltaTime * 0.6f);
            slowTint.color = tint;
            floorGlow.color = Palette.Floor.WithAlpha(0.2f + 0.05f * Mathf.Sin(time * 2f));
        }

        void Place(TargetSprite sprite, Target target)
        {
            float scale = Shapes.ScaleFor(target.Radius);
            sprite.Root.transform.localPosition = new Vector3(target.Position.X, target.Position.Y, 0f);
            // Faces and icons only wobble so they stay readable; candy and stars spin.
            bool upright = target.Kind != TargetKind.Good && target.Kind != TargetKind.Bonus;
            float angle = upright ? Mathf.Sin(time * 3f + target.Id) * 12f : target.Angle;
            sprite.Body.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            sprite.Body.transform.localScale = Vector3.one * scale;
            sprite.Shine.transform.localScale = Vector3.one * scale;
            sprite.Glow.transform.localScale = Vector3.one * scale * 2.1f;
            if (target.Kind == TargetKind.Bomb)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 9f + target.Id);
                sprite.Glow.color = Palette.Danger.WithAlpha(0.25f + 0.3f * pulse);
            }
            if (sprite.Halo.enabled)
            {
                sprite.Halo.transform.localScale = Vector3.one * scale * (1.12f + 0.06f * Mathf.Sin(time * 6f + target.Id));
                sprite.Halo.transform.localRotation = Quaternion.Euler(0f, 0f, time * 90f);
            }
        }

        TargetSprite Take(TargetKind kind, int variant)
        {
            TargetSprite sprite = pool.Count > 0 ? pool.Pop() : CreateSprite();
            sprite.Kind = kind;
            sprite.Variant = variant;
            sprite.Body.sprite = Shapes.For(kind, variant);
            sprite.Glow.color = Palette.Of(kind, variant).WithAlpha(kind == TargetKind.Bad ? 0.18f : 0.28f);
            sprite.Halo.enabled = kind.IsPowerUp() || kind == TargetKind.Bonus;
            sprite.Shine.enabled = kind == TargetKind.Good;
            sprite.Halo.color = Palette.Of(kind).WithAlpha(0.7f);
            sprite.Root.SetActive(true);
            return sprite;
        }

        TargetSprite CreateSprite()
        {
            var root = new GameObject("Target");
            root.transform.SetParent(targetsRoot, false);
            var sprite = new TargetSprite { Root = root };
            sprite.Glow = Sprite("Glow", Shapes.SoftDot, Color.white, 4, root.transform);
            sprite.Halo = Sprite("Halo", Shapes.Ring, Color.white, 6, root.transform);
            sprite.Body = Sprite("Body", Shapes.Circle, Color.white, 5, root.transform);
            sprite.Shine = Sprite("Shine", Shapes.OrbShine, Color.white, 7, root.transform);
            return sprite;
        }

        void Return(TargetSprite sprite)
        {
            sprite.Root.SetActive(false);
            pool.Push(sprite);
        }

        /// <summary>How many target sprites are on screen (for tests).</summary>
        public int ShownCount => shown.Count;
    }
}
