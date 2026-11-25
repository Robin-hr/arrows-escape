using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Step 16: plain, solid play-area colours — one per level, no gradients, particles or moving shapes, so arrows are
    /// always easy to see. Dark boards (glowing skins) get deep colours; light boards (Classic Ink) get soft pastels.
    /// </summary>
    public static class LevelColors
    {
        /// <summary>Deep colours behind glowing arrow skins.</summary>
        public static readonly Color32[] Dark =
        {
            new Color32(15, 23, 42, 255),   // midnight navy
            new Color32(30, 18, 53, 255),   // deep plum
            new Color32(9, 42, 46, 255),    // deep teal
            new Color32(18, 38, 26, 255),   // forest
            new Color32(42, 17, 31, 255),   // wine
            new Color32(26, 29, 36, 255),   // graphite
            new Color32(36, 26, 14, 255),   // espresso
            new Color32(13, 34, 56, 255),   // ocean
            new Color32(33, 20, 48, 255),   // aubergine
            new Color32(11, 31, 36, 255),   // petrol
        };
        /// <summary>Soft pastels behind the dark Classic Ink arrows.</summary>
        public static readonly Color32[] Light =
        {
            new Color32(232, 242, 242, 255), // paper mint
            new Color32(243, 233, 220, 255), // sand
            new Color32(230, 236, 247, 255), // ice blue
            new Color32(242, 230, 239, 255), // blush
            new Color32(233, 242, 227, 255), // sage
            new Color32(239, 234, 248, 255), // lavender
            new Color32(247, 238, 219, 255), // cream
            new Color32(227, 240, 244, 255), // sky
            new Color32(244, 235, 230, 255), // shell
            new Color32(232, 239, 233, 255), // pale jade
        };
        /// <summary>The colour for a level key (any integer) on a light or dark board.</summary>
        public static Color For(int key, bool lightBoard)
        {
            var set = lightBoard ? Light : Dark; int n = set.Length; return set[((key % n) + n) % n];
        }
    }
}
