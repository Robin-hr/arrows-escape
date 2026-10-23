using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Play-area backgrounds (Step 14J): six gradient themes and three animated 3D geometric wallpapers. Paper (-1)
    /// keeps the original light board. Visual identity only; independent of inventory persistence.
    /// </summary>
    public static class ThemeDesign
    {
        /// <summary>Maximum animated wallpaper pieces on screen.</summary>
        public const int ParticleLimit = 150;
        private static readonly string[] ids = { "grad_midnight", "grad_aurora", "grad_sunset", "grad_lagoon", "grad_emerald", "grad_rose", "geo_crystal", "geo_neon", "geo_gold" };
        private static readonly Color32[][] stops =
        {
            new[] { new Color32(20, 30, 48, 255), new Color32(36, 59, 85, 255), new Color32(46, 90, 136, 255) },
            new[] { new Color32(29, 43, 100, 255), new Color32(75, 44, 127, 255), new Color32(142, 60, 143, 255) },
            new[] { new Color32(43, 19, 49, 255), new Color32(122, 41, 72, 255), new Color32(194, 85, 58, 255) },
            new[] { new Color32(6, 43, 58, 255), new Color32(14, 85, 102, 255), new Color32(26, 138, 134, 255) },
            new[] { new Color32(4, 36, 26, 255), new Color32(15, 74, 54, 255), new Color32(46, 125, 91, 255) },
            new[] { new Color32(42, 15, 51, 255), new Color32(94, 26, 87, 255), new Color32(163, 51, 111, 255) },
            new[] { new Color32(98, 178, 242, 255), new Color32(150, 125, 230, 255), new Color32(214, 140, 222, 255) },
            new[] { new Color32(7, 9, 31, 255), new Color32(18, 10, 50, 255), new Color32(36, 12, 66, 255) },
            new[] { new Color32(6, 26, 30, 255), new Color32(12, 40, 46, 255), new Color32(22, 56, 60, 255) },
        };
        private static readonly Color32[][] palettes =
        {
            new[] { new Color32(90, 160, 255, 255), new Color32(120, 200, 255, 255), new Color32(160, 140, 255, 255) },
            new[] { new Color32(120, 140, 255, 255), new Color32(200, 120, 255, 255), new Color32(255, 130, 210, 255) },
            new[] { new Color32(255, 150, 90, 255), new Color32(255, 110, 140, 255), new Color32(255, 200, 120, 255) },
            new[] { new Color32(80, 220, 220, 255), new Color32(90, 180, 255, 255), new Color32(140, 255, 210, 255) },
            new[] { new Color32(90, 230, 160, 255), new Color32(160, 255, 140, 255), new Color32(80, 200, 220, 255) },
            new[] { new Color32(255, 120, 190, 255), new Color32(200, 110, 255, 255), new Color32(255, 170, 200, 255) },
            new[] { new Color32(110, 200, 245, 255), new Color32(169, 154, 240, 255), new Color32(232, 140, 216, 255) },
            new[] { new Color32(56, 225, 255, 255), new Color32(138, 107, 255, 255), new Color32(255, 79, 216, 255) },
            new[] { new Color32(63, 224, 197, 255), new Color32(255, 201, 74, 255), new Color32(255, 159, 67, 255) },
        };
        // The colour behind the arrows (the frosted board panel over the backdrop); used for contrast and the camera.
        private static readonly Color32[] bases =
        {
            new Color32(22, 34, 54, 255), new Color32(36, 30, 78, 255), new Color32(48, 22, 46, 255), new Color32(8, 42, 54, 255), new Color32(6, 38, 28, 255),
            new Color32(44, 16, 50, 255), new Color32(40, 46, 96, 255), new Color32(10, 10, 34, 255), new Color32(8, 28, 32, 255),
        };
        /// <summary>Returns minus one for Paper or an unknown id.</summary>
        public static int Index(string id) { for (int i = 0; i < ids.Length; i++) if (ids[i] == id) return i; return -1; }
        /// <summary>Number of designed themes.</summary>
        public static int Count => ids.Length;
        /// <summary>Theme id by index.</summary>
        public static string Id(int index) => ids[index];
        /// <summary>True for the animated 3D geometric premium wallpapers.</summary>
        public static bool IsAnimated(int index) => index >= 6;
        /// <summary>Three gradient stops, top-left to bottom-right.</summary>
        public static Color32[] Stops(int index) => stops[index];
        /// <summary>Three tint colours for the wallpaper pieces or light orbs.</summary>
        public static Color32[] Palette(int index) => palettes[index];
        /// <summary>Returns the colour behind the arrows, or exact Paper.</summary>
        public static Color Background(string id) { int index = Index(id); return index < 0 ? ViewTheme.Background : (Color)bases[index]; }
        /// <summary>Light ink on a theme, retaining exact Classic on Paper.</summary>
        public static Color ClassicInk(string id) => Index(id) < 0 ? ViewTheme.ArrowColor : new Color32(233, 238, 242, 255);
    }
}
