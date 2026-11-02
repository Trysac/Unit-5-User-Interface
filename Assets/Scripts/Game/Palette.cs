using TargetToss.Core;
using UnityEngine;

namespace TargetToss.Game
{
    /// <summary>The candy-on-midnight colours shared by the field, the targets, the effects and the HUD.</summary>
    public static class Palette
    {
        public static readonly Color Background = Hex("#140f2e");
        public static readonly Color BackgroundTop = Hex("#2a1d5c");
        public static readonly Color Star = Hex("#7d6fd1");
        public static readonly Color Floor = Hex("#ff5fa2");
        public static readonly Color Text = Hex("#f4f1ff");
        public static readonly Color Gold = Hex("#ffd84a");
        public static readonly Color Mint = Hex("#4ef2a5");
        public static readonly Color Sky = Hex("#46c8ff");
        public static readonly Color Tangerine = Hex("#ffa53d");
        public static readonly Color Bad = Hex("#a24bff");
        public static readonly Color BadDark = Hex("#3b1466");
        public static readonly Color Bomb = Hex("#262236");
        public static readonly Color Danger = Hex("#ff3b4f");
        public static readonly Color Ice = Hex("#8fe9ff");
        public static readonly Color Magnet = Hex("#ff4f6d");
        public static readonly Color Steel = Hex("#dfe6f5");

        static readonly Color[] GoodColors = { Mint, Sky, Tangerine };

        public static Color Good(int variant) => GoodColors[((variant % 3) + 3) % 3];

        /// <summary>The main colour of a target, for its particles and floating text.</summary>
        public static Color Of(TargetKind kind, int variant = 0)
        {
            switch (kind)
            {
                case TargetKind.Good: return Good(variant);
                case TargetKind.Bad: return Bad;
                case TargetKind.Bonus: return Gold;
                case TargetKind.Bomb: return Danger;
                case TargetKind.SlowTime: return Ice;
                default: return Magnet;
            }
        }

        public static Color WithAlpha(this Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;
    }
}
