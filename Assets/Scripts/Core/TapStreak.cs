namespace ArrowEscape.Core
{
    /// <summary>
    /// Counts rapid repeated taps (developer shortcuts). A tap more than <see cref="Gap"/> seconds after the
    /// previous one starts a new streak; reaching <see cref="Target"/> fires once and resets.
    /// </summary>
    public sealed class TapStreak
    {
        private float last = float.NegativeInfinity;
        /// <summary>Taps needed.</summary>
        public int Target { get; }
        /// <summary>Longest pause allowed between taps, in seconds.</summary>
        public float Gap { get; }
        /// <summary>Taps in the current streak.</summary>
        public int Count { get; private set; }
        /// <summary>Creates a streak counter.</summary>
        public TapStreak(int target = 5, float gap = 0.8f) { Target = target < 1 ? 1 : target; Gap = gap; }
        /// <summary>Registers a tap at a time in seconds; true exactly when the streak completes.</summary>
        public bool Tap(float time)
        {
            if (time - last > Gap || time < last) Count = 0;
            last = time; Count++;
            if (Count < Target) return false;
            Count = 0; last = float.NegativeInfinity; return true;
        }
        /// <summary>Clears the current streak.</summary>
        public void Reset() { Count = 0; last = float.NegativeInfinity; }
    }
}
