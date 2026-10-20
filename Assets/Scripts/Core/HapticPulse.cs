using System;

namespace ArrowEscape.Core
{
    /// <summary>One immutable vibration pulse and its following silent gap.</summary>
    public readonly struct HapticPulse
    {
        /// <summary>Positive on-time in milliseconds.</summary>
        public int DurationMs { get; }
        /// <summary>Strength from 1 through 255.</summary>
        public int Amplitude { get; }
        /// <summary>Nonnegative gap; the final pulse's gap is ignored.</summary>
        public int GapAfterMs { get; }
        /// <summary>Validates and stores one pulse.</summary>
        public HapticPulse(int durationMs, int amplitude, int gapAfterMs = 0)
        {
            if (durationMs <= 0) throw new ArgumentOutOfRangeException(nameof(durationMs));
            if (amplitude < 1 || amplitude > 255) throw new ArgumentOutOfRangeException(nameof(amplitude));
            if (gapAfterMs < 0) throw new ArgumentOutOfRangeException(nameof(gapAfterMs));
            DurationMs = durationMs; Amplitude = amplitude; GapAfterMs = gapAfterMs;
        }
    }
}
