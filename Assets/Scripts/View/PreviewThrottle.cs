using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Spreads small live previews (shop cards) across alternate frames: each renders at 30 Hz, half of them on even
    /// frames and half on odd frames, so a screen full of animated cards costs about half as much.
    /// </summary>
    public static class PreviewThrottle
    {
        private static int next;
        /// <summary>Hands out alternating phases (0 or 1).</summary>
        public static int NextPhase() => next++ & 1;
        /// <summary>Whether a preview with this phase renders on the current frame.</summary>
        public static bool ShouldRender(int phase) => ((Time.frameCount + phase) & 1) == 0;
    }
}
