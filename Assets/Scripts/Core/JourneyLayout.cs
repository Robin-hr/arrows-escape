using System;

namespace ArrowEscape.Core
{
    /// <summary>
    /// Geometry of the Journey road map (reference units, y up from the bottom of the map): level 1 sits at the
    /// bottom and the road winds upward from one level to the next, swinging left and right.
    /// </summary>
    public static class JourneyLayout
    {
        /// <summary>Vertical distance between consecutive levels.</summary>
        public const float Spacing = 220f;
        /// <summary>Space below level 1.</summary>
        public const float Bottom = 300f;
        /// <summary>Space above the last level.</summary>
        public const float Top = 460f;
        /// <summary>Largest sideways swing of the road.</summary>
        public const float Amplitude = 265f;
        /// <summary>Levels per world (chapter banner).</summary>
        public const int WorldSize = 30;
        /// <summary>Every tenth level is a milestone node.</summary>
        public const int MilestoneEvery = 10;

        private static readonly string[] worlds =
        {
            "NEON GATE", "CYBER GROVE", "PRISM PEAKS", "VOID HARBOR", "SOLAR RIFT", "CRYSTAL CAVES",
            "AURORA SEA", "QUANTUM CITY", "NEBULA FIELDS", "STARFORGE", "OMEGA WARP", "CROWN NEXUS",
        };

        /// <summary>Sideways position for a (fractional) level index.</summary>
        public static float X(float index) => (float)(Amplitude * Math.Sin(index * Math.PI / 4.0) * (0.86 + 0.14 * Math.Cos(index * 0.37)));

        /// <summary>Height above the bottom of the map for a (fractional) level index.</summary>
        public static float Y(float index) => Bottom + index * Spacing;

        /// <summary>Total map height for <paramref name="count"/> levels.</summary>
        public static float Height(int count) => Bottom + Math.Max(0, count - 1) * Spacing + Top;

        /// <summary>True for milestone levels (10, 20, 30 ...).</summary>
        public static bool IsMilestone(int index) => (index + 1) % MilestoneEvery == 0;

        /// <summary>World number (1-based) of a level.</summary>
        public static int World(int index) => index / WorldSize + 1;

        /// <summary>Display name of a world.</summary>
        public static string WorldName(int world) => worlds[(Math.Max(1, world) - 1) % worlds.Length];

        /// <summary>First and last level index whose nodes may be visible between two map heights (with one spare each side).</summary>
        public static void VisibleRange(float low, float high, int count, out int first, out int last)
        {
            first = Math.Max(0, (int)Math.Floor((low - Bottom) / Spacing) - 1);
            last = Math.Min(count - 1, (int)Math.Ceiling((high - Bottom) / Spacing) + 1);
        }
    }
}
