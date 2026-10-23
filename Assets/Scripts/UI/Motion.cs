using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Small easing helpers shared by UI animations.</summary>
    public static class Motion
    {
        /// <summary>Ease-out with a light overshoot ("pop").</summary>
        public static float BackOut(float from, float to, float t)
        {
            t = Mathf.Clamp01(t); const float s = 1.70158f; float u = t - 1;
            return from + (to - from) * (1 + (s + 1) * u * u * u + s * u * u);
        }
        /// <summary>Smooth ease-out (cubic).</summary>
        public static float Out(float t) { t = Mathf.Clamp01(t); float u = 1 - t; return 1 - u * u * u; }
        /// <summary>Smooth ease-in-out.</summary>
        public static float InOut(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
    }
}
