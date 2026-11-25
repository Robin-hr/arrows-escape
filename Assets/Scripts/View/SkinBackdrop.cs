using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Step 17: the play-area colour follows the equipped arrow style so the arrows always stand out.
    /// Dark arrows (Classic Ink and the dark-bodied skins) sit on soft paper tinted with the skin's own hue;
    /// bright and glowing arrows sit on a deep shade of their hue. Each level shifts the shade a little.
    /// Night mode moves light-board styles onto their deep board (Classic Ink then turns light).
    /// </summary>
    public static class SkinBackdrop
    {
        /// <summary>Skins whose arrows are dark enough to need a light board.</summary>
        public static readonly string[] DarkArrowSkins = { "classic", "void_whisper", "obsidian_shard", "crimson_relic", "vortex_harpoon", "abyss_fang" };
        /// <summary>Classic Ink colour on the night board.</summary>
        public static readonly Color NightInk = new Color32(238, 231, 244, 255);
        /// <summary>Classic papers (the first is the warm cream of the reference look).</summary>
        public static readonly Color32[] ClassicPaper =
        {
            new Color32(250, 244, 234, 255), new Color32(244, 238, 248, 255), new Color32(234, 245, 238, 255), new Color32(249, 237, 232, 255),
            new Color32(234, 241, 250, 255), new Color32(246, 242, 226, 255), new Color32(241, 236, 230, 255), new Color32(236, 244, 244, 255),
        };
        /// <summary>Classic night boards (deep plum, ink and teal greys).</summary>
        public static readonly Color32[] ClassicNight =
        {
            new Color32(30, 24, 38, 255), new Color32(22, 27, 38, 255), new Color32(20, 32, 32, 255), new Color32(36, 26, 30, 255),
            new Color32(26, 28, 34, 255), new Color32(34, 30, 22, 255), new Color32(24, 22, 40, 255), new Color32(18, 30, 26, 255),
        };
        private static readonly float[] HueSteps = { 0, 14, -14, 28, -28, 7, -7, 21 };
        private static readonly float[] ValueSteps = { 0, .012f, -.01f, .006f, -.016f, .018f, -.004f, .01f };

        /// <summary>True when the skin's arrows are dark (they need a light board by day).</summary>
        public static bool HasDarkArrows(string id) { if (string.IsNullOrEmpty(id)) return true; foreach (var s in DarkArrowSkins) if (s == id) return true; return ArrowSkinRegistry.Find(id) == null; }
        /// <summary>Light or dark board for this skin and night setting.</summary>
        public static bool LightBoard(string id, bool night) => !night && HasDarkArrows(id);
        /// <summary>True when night mode changes this skin's board (only light-board styles have a night board).</summary>
        public static bool NightChangesBoard(string id) => HasDarkArrows(id);

        /// <summary>The play-area colour for a skin, level key and board brightness.</summary>
        public static Color For(string id, int key, bool lightBoard)
        {
            int i = ((key % 8) + 8) % 8;
            var skin = ArrowSkinRegistry.Find(id);
            if (skin == null) return lightBoard ? ClassicPaper[i] : ClassicNight[i];
            Color.RGBToHSV(HueSource(skin, lightBoard), out float h, out _, out _);
            h = Mathf.Repeat(h + HueSteps[i] / 360f, 1f);
            return lightBoard ? Color.HSVToRGB(h, .075f, .978f + ValueSteps[i] * .3f) : Color.HSVToRGB(h, .5f, .125f + ValueSteps[i]);
        }

        /// <summary>The arrow colour a reader sees against the board (the darker one on light boards, the brighter glow on dark).</summary>
        public static Color ArrowFace(string id, bool lightBoard)
        {
            var skin = ArrowSkinRegistry.Find(id);
            if (skin == null) return lightBoard ? ViewTheme.ArrowColor : NightInk;
            bool primaryBrighter = Luminance(skin.Primary) >= Luminance(skin.Secondary);
            return lightBoard == primaryBrighter ? skin.Secondary : skin.Primary;
        }

        /// <summary>WCAG contrast ratio of two colours (1..21).</summary>
        public static float Contrast(Color a, Color b) { float la = Luminance(a), lb = Luminance(b); return (Mathf.Max(la, lb) + .05f) / (Mathf.Min(la, lb) + .05f); }
        /// <summary>Relative luminance of an sRGB colour.</summary>
        public static float Luminance(Color c) => .2126f * Lin(c.r) + .7152f * Lin(c.g) + .0722f * Lin(c.b);
        private static float Lin(float v) => v <= .04045f ? v / 12.92f : Mathf.Pow((v + .055f) / 1.055f, 2.4f);
        // Dark boards take the glow's hue; light boards take the body's hue so the paper feels related to the arrow.
        private static Color HueSource(ArrowSkin skin, bool lightBoard)
        {
            Color brighter = Luminance(skin.Primary) >= Luminance(skin.Secondary) ? skin.Primary : skin.Secondary, other = brighter == skin.Primary ? skin.Secondary : skin.Primary;
            Color pick = lightBoard ? other : brighter;
            Color.RGBToHSV(pick, out _, out float s, out _);
            return s < .08f ? (lightBoard ? brighter : other) : pick;
        }
    }
}
