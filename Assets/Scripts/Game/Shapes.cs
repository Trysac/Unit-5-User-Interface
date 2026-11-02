using System;
using System.Collections.Generic;
using TargetToss.Core;
using UnityEngine;

namespace TargetToss.Game
{
    /// <summary>
    /// Every sprite in the game, drawn in code at start-up from distance functions, so the game
    /// ships without image files. Each sprite is one unit across; a target's hit circle is the
    /// <see cref="HitRadius"/> of its square (0.4 units at scale 1).
    /// </summary>
    public static class Shapes
    {
        public const float HitRadius = 0.8f;
        const int TargetSize = 128;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>The transform scale that makes a sprite's hit circle <paramref name="radius"/> world units.</summary>
        public static float ScaleFor(float radius) => radius / (HitRadius * 0.5f);

        public static Sprite Square => Get("Square", 8, (x, y) => Color.white);

        public static Sprite Circle => Get("Circle", 64, (x, y) => White(Coverage(0.98f - Len(x, y), 0.04f)));

        /// <summary>A soft round glow that fades to nothing at its edge.</summary>
        public static Sprite SoftDot => Get("SoftDot", 64, (x, y) =>
        {
            float a = Mathf.Clamp01(1f - Len(x, y));
            return White(a * a);
        });

        public static Sprite Ring => Get("Ring", 128, (x, y) => White(Coverage(0.05f - Mathf.Abs(Len(x, y) - 0.9f), 0.02f)));

        /// <summary>The aiming reticle that follows the mouse or the gamepad cursor.</summary>
        public static Sprite Reticle => Get("Reticle", 96, (x, y) =>
        {
            float d = Len(x, y);
            float ring = Coverage(0.06f - Mathf.Abs(d - 0.62f), 0.03f);
            float ticks = Mathf.Max(Box(x, y - 0.82f, 0.05f, 0.14f), Box(x, y + 0.82f, 0.05f, 0.14f));
            ticks = Mathf.Max(ticks, Mathf.Max(Box(x - 0.82f, y, 0.14f, 0.05f), Box(x + 0.82f, y, 0.14f, 0.05f)));
            float dot = Coverage(0.09f - d, 0.03f);
            return White(Mathf.Max(ring, Mathf.Max(ticks, dot)));
        });

        /// <summary>The highlight drawn over a candy orb; it does not spin with the orb, so the light stays put.</summary>
        public static Sprite OrbShine => Get("OrbShine", TargetSize, (x, y) =>
        {
            float shine = Coverage(0.02f - Ellipse(x + 0.3f, y - 0.38f, 0.24f, 0.13f, 0.6f), 0.06f);
            float dot = Coverage(0.06f - Len(x + 0.02f, y - 0.52f), 0.03f);
            return White(Mathf.Max(shine * 0.75f, dot * 0.65f));
        });

        /// <summary>A white heart for the lives row (the HUD tints it).</summary>
        public static Sprite Heart => Get("Heart", 64, (x, y) =>
        {
            // Two discs and a rotated square make the classic heart.
            float lobes = Mathf.Max(Coverage(0.42f - Len(x - 0.36f, y - 0.22f), 0.04f), Coverage(0.42f - Len(x + 0.36f, y - 0.22f), 0.04f));
            float point = RotatedBox(x, y + 0.2f, 0.52f, 0.52f, Mathf.PI / 4f) * (y < 0.3f ? 1f : 0f);
            return White(Mathf.Max(lobes, point));
        });

        public static Sprite For(TargetKind kind, int variant)
        {
            switch (kind)
            {
                case TargetKind.Good: return Get("Good" + ((variant % 3) + 3) % 3, TargetSize, (x, y) => Orb(x, y, Palette.Good(variant)));
                case TargetKind.Bad: return Get("Bad", TargetSize, BadPixel);
                case TargetKind.Bonus: return Get("Bonus", TargetSize, StarPixel);
                case TargetKind.Bomb: return Get("Bomb", TargetSize, BombPixel);
                case TargetKind.SlowTime: return Get("Slow", TargetSize, SlowPixel);
                default: return Get("Magnet", TargetSize, MagnetPixel);
            }
        }

        // ---- Targets ----

        /// <summary>A glossy candy orb with a rim, a shine and a swirl.</summary>
        static Color Orb(float x, float y, Color tint)
        {
            float d = Len(x, y);
            float body = Coverage(HitRadius - d, 0.02f);
            if (body <= 0f)
            {
                return Color.clear;
            }
            float light = Mathf.Clamp01(0.5f + 0.5f * (-x * 0.6f + y * 0.8f) / HitRadius);
            Color color = Color.Lerp(tint * 0.62f, Color.Lerp(tint, Color.white, 0.25f), light);
            // A darker rim.
            Color rim = tint * 0.45f;
            rim.a = 1f;
            color = Color.Lerp(color, rim, Coverage(d - (HitRadius - 0.09f), 0.03f));
            // A candy swirl band.
            float angle = Mathf.Atan2(y, x);
            float swirl = Mathf.Sin(angle * 3f + d * 7f);
            color = Color.Lerp(color, Color.white, Coverage(swirl - 0.72f, 0.12f) * 0.28f * Coverage(0.66f - d, 0.05f));
            color.a = body;
            return color;
        }

        /// <summary>A spiky purple mine with an angry face.</summary>
        static Color BadPixel(float x, float y)
        {
            float d = Len(x, y);
            float angle = Mathf.Atan2(y, x);
            float spike = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 8f)), 4f);
            float outline = 0.62f + 0.3f * spike;
            float spikes = Coverage(outline - d, 0.025f);
            float body = Coverage(0.62f - d, 0.02f);
            if (spikes <= 0f)
            {
                return Color.clear;
            }
            Color color = Palette.BadDark;
            float light = Mathf.Clamp01(0.5f + 0.6f * (-x * 0.5f + y * 0.8f));
            Color skin = Color.Lerp(Palette.Bad * 0.7f, Color.Lerp(Palette.Bad, Color.white, 0.2f), light);
            color = Color.Lerp(color, skin, body);
            // Eyes, slanted brows and a jagged mouth.
            float eyes = Mathf.Max(Coverage(0.13f - Len(x - 0.22f, y - 0.06f), 0.02f), Coverage(0.13f - Len(x + 0.22f, y - 0.06f), 0.02f));
            color = Color.Lerp(color, Color.white, eyes);
            float pupils = Mathf.Max(Coverage(0.06f - Len(x - 0.2f, y - 0.03f), 0.02f), Coverage(0.06f - Len(x + 0.2f, y - 0.03f), 0.02f));
            color = Color.Lerp(color, Palette.BadDark, pupils);
            float brows = Mathf.Max(RotatedBox(x - 0.22f, y - 0.27f, 0.16f, 0.04f, -0.45f), RotatedBox(x + 0.22f, y - 0.27f, 0.16f, 0.04f, 0.45f));
            color = Color.Lerp(color, Palette.BadDark, brows);
            float zig = -0.26f + 0.05f * Mathf.Abs(Mathf.Repeat(x * 6f, 2f) - 1f);
            float mouth = Coverage(0.035f - Mathf.Abs(y - zig), 0.02f) * Coverage(0.24f - Mathf.Abs(x), 0.02f);
            color = Color.Lerp(color, Palette.BadDark, mouth);
            color.a = spikes;
            return color;
        }

        /// <summary>A plump golden star.</summary>
        static Color StarPixel(float x, float y)
        {
            float star = Star(x, y + 0.04f, 0.86f, 0.5f);
            float fill = Coverage(-star, 0.025f);
            float glow = Mathf.Clamp01(1f - Mathf.Max(star, 0f) / 0.16f) * 0.45f;
            if (fill <= 0f && glow <= 0f)
            {
                return Color.clear;
            }
            float light = Mathf.Clamp01(0.5f + 0.6f * (-x * 0.4f + y * 0.9f));
            Color color = Color.Lerp(Palette.Hex("#ff9d1f"), Palette.Hex("#fff3a6"), light);
            color = Color.Lerp(color, Palette.Hex("#d9700f"), Coverage(star + 0.07f, 0.03f));
            color = Color.Lerp(color, Color.white, Coverage(0.09f - Len(x + 0.12f, y - 0.2f), 0.03f) * 0.8f);
            color.a = fill;
            Color halo = Palette.Gold.WithAlpha(glow);
            return Over(halo, color);
        }

        /// <summary>A round black bomb with a red warning band, a fuse and a spark.</summary>
        static Color BombPixel(float x, float y)
        {
            float cy = y + 0.08f;
            float d = Len(x, cy);
            float body = Coverage(0.68f - d, 0.02f);
            float cap = Box(x, y - 0.66f, 0.16f, 0.1f);
            float fuse = Coverage(0.045f - Mathf.Abs(Len(x - 0.24f, y - 0.66f) - 0.24f), 0.02f) * (x > 0.04f && y > 0.7f ? 1f : 0f);
            float spark = Coverage(0.13f - Len(x - 0.46f, y - 0.8f), 0.04f);
            float sparkGlow = Mathf.Clamp01(1f - Len(x - 0.46f, y - 0.8f) / 0.3f);
            Color color = Color.clear;
            color = Over(color, Palette.Hex("#ffb84a").WithAlpha(sparkGlow * 0.6f));
            color = Over(color, Palette.Hex("#8a7a60").WithAlpha(fuse));
            color = Over(color, Palette.Hex("#6b6880").WithAlpha(cap));
            if (body > 0f)
            {
                float light = Mathf.Clamp01(0.5f + 0.6f * (-x * 0.6f + cy * 0.8f));
                Color skin = Color.Lerp(Palette.Bomb * 0.6f, Palette.Hex("#4a4466"), light);
                skin = Color.Lerp(skin, Palette.Danger, Coverage(0.06f - Mathf.Abs(d - 0.58f), 0.02f));
                skin = Color.Lerp(skin, Color.white, Coverage(0.17f - Ellipse(x + 0.26f, cy - 0.28f, 0.2f, 0.11f, 0.6f), 0.04f) * 0.6f);
                // A white skull-free warning mark: an exclamation point.
                float mark = Mathf.Max(Box(x, cy - 0.06f, 0.06f, 0.22f), Coverage(0.07f - Len(x, cy + 0.28f), 0.02f));
                skin = Color.Lerp(skin, Palette.Hex("#ffe1e5"), mark);
                color = Over(color, skin.WithAlpha(body));
            }
            color = Over(color, Palette.Hex("#fff1b8").WithAlpha(spark));
            return color;
        }

        static Color Badge(float x, float y, Color tint, out float inside)
        {
            float d = Len(x, y);
            float disk = Coverage(HitRadius - d, 0.02f);
            inside = Coverage(HitRadius - 0.12f - d, 0.02f);
            if (disk <= 0f)
            {
                return Color.clear;
            }
            Color rim = Color.Lerp(tint, Color.white, 0.35f);
            Color face = Color.Lerp(Palette.Hex("#1b1442"), tint * 0.35f, Mathf.Clamp01(0.5f + 0.5f * y));
            Color color = Color.Lerp(rim, face, inside);
            color.a = disk;
            return color;
        }

        /// <summary>The slow-time power-up: an ice-blue clock face.</summary>
        static Color SlowPixel(float x, float y)
        {
            Color color = Badge(x, y, Palette.Ice, out float inside);
            if (color.a <= 0f)
            {
                return color;
            }
            float marks = 0f;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                float length = i % 3 == 0 ? 0.1f : 0.05f;
                marks = Mathf.Max(marks, RotatedBox(x - Mathf.Cos(a) * 0.5f, y - Mathf.Sin(a) * 0.5f, length, 0.025f, a));
            }
            float hands = Mathf.Max(RotatedBox(x - 0.12f, y - 0.07f, 0.16f, 0.035f, 0.52f), RotatedBox(x, y - 0.17f, 0.035f, 0.21f, 0f));
            hands = Mathf.Max(hands, Coverage(0.06f - Len(x, y), 0.02f));
            color = Color.Lerp(color, Palette.Ice, Mathf.Max(marks, hands) * inside);
            return color;
        }

        /// <summary>The magnet power-up: a red horseshoe magnet with steel tips.</summary>
        static Color MagnetPixel(float x, float y)
        {
            Color color = Badge(x, y, Palette.Magnet, out float inside);
            if (color.a <= 0f)
            {
                return color;
            }
            float cy = y + 0.05f;
            float arc = Coverage(0.11f - Mathf.Abs(Len(x, cy) - 0.3f), 0.02f) * (cy < 0f ? 1f : 0f);
            float legs = Mathf.Max(Box(x - 0.3f, cy - 0.2f, 0.11f, 0.2f), Box(x + 0.3f, cy - 0.2f, 0.11f, 0.2f));
            float magnet = Mathf.Max(arc, legs);
            bool tip = cy > 0.25f;
            color = Color.Lerp(color, tip ? Palette.Steel : Palette.Magnet, magnet * inside);
            return color;
        }

        // ---- Helpers ----

        static Sprite Get(string name, int size, Func<float, float, Color> paint)
        {
            if (cache.TryGetValue(name, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float u = (px + 0.5f) / size * 2f - 1f;
                    float v = (py + 0.5f) / size * 2f - 1f;
                    pixels[py * size + px] = paint(u, v);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            cache[name] = sprite;
            return sprite;
        }

        static Color White(float alpha) => new Color(1f, 1f, 1f, alpha);

        static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);

        /// <summary>0 outside, 1 inside, with a soft edge <paramref name="width"/> wide.</summary>
        static float Coverage(float inside, float width) => Mathf.Clamp01(inside / width + 0.5f);

        static float Box(float x, float y, float halfWidth, float halfHeight) =>
            Mathf.Min(Coverage(halfWidth - Mathf.Abs(x), 0.02f), Coverage(halfHeight - Mathf.Abs(y), 0.02f));

        static float RotatedBox(float x, float y, float halfWidth, float halfHeight, float radians)
        {
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);
            return Box(c * x + s * y, -s * x + c * y, halfWidth, halfHeight);
        }

        static float Ellipse(float x, float y, float rx, float ry, float radians)
        {
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);
            float u = (c * x + s * y) / rx;
            float v = (-s * x + c * y) / ry;
            return (Mathf.Sqrt(u * u + v * v) - 1f) * Mathf.Min(rx, ry);
        }

        /// <summary>Signed distance to a five-pointed star (negative inside).</summary>
        static float Star(float x, float y, float radius, float innerRatio)
        {
            float angle = Mathf.Atan2(x, y);
            float sector = Mathf.PI * 2f / 5f;
            float a = Mathf.Repeat(angle + sector / 2f, sector) - sector / 2f;
            float d = Len(x, y);
            var p = new Vector2(Mathf.Abs(Mathf.Sin(a)) * d, Mathf.Cos(a) * d);
            var tip = new Vector2(0f, radius);
            var valley = new Vector2(Mathf.Sin(sector / 2f), Mathf.Cos(sector / 2f)) * (radius * innerRatio);
            Vector2 edge = valley - tip;
            float t = Mathf.Clamp01(Vector2.Dot(p - tip, edge) / edge.sqrMagnitude);
            float distance = (p - (tip + edge * t)).magnitude;
            float side = edge.x * (p.y - tip.y) - edge.y * (p.x - tip.x);
            return side < 0f ? -distance : distance;
        }

        static Color Over(Color below, Color above)
        {
            float a = above.a + below.a * (1f - above.a);
            if (a <= 1e-5f)
            {
                return Color.clear;
            }
            Color mixed = (above * above.a + below * below.a * (1f - above.a)) / a;
            mixed.a = a;
            return mixed;
        }
    }
}
