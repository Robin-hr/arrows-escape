using System;

namespace ArrowEscape.Core
{
    /// <summary>Independent per-cue accepted times; rejected requests never extend the window.</summary>
    public sealed class HapticThrottle
    {
        private readonly float[] last = new float[10];
        private readonly bool[] accepted = new bool[10];
        /// <summary>Accepts the first request, a completed interval, or a clock reset.</summary>
        public bool TryAccept(HapticCue cue, float nowSeconds)
        {
            float interval = HapticDesign.MinIntervalSeconds(cue);
            if (float.IsNaN(nowSeconds) || float.IsInfinity(nowSeconds)) return false;
            int index = (int)cue;
            if (accepted[index] && nowSeconds >= last[index] && nowSeconds - last[index] < interval) return false;
            accepted[index] = true; last[index] = nowSeconds; return true;
        }
        /// <summary>Forgets all accepted times.</summary>
        public void Reset() { Array.Clear(last,0,last.Length); Array.Clear(accepted,0,accepted.Length); }
    }
}
