using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Converts pulses to Android's alternating silence/on waveform without allocations.</summary>
    public static class HapticWaveform
    {
        /// <summary>One initial zero and alternating pulse/gap entries, with no trailing gap.</summary>
        public static int RequiredLength(int pulseCount)
        { if (pulseCount < 0 || pulseCount > int.MaxValue / 2) throw new ArgumentOutOfRangeException(nameof(pulseCount)); return 2 * pulseCount; }
        /// <summary>Writes timing/amplitude entries into reusable buffers.</summary>
        public static int Build(IReadOnlyList<HapticPulse> pulses, long[] timings, int[] amplitudes)
        {
            if (pulses == null) throw new ArgumentNullException(nameof(pulses));
            if (timings == null) throw new ArgumentNullException(nameof(timings));
            if (amplitudes == null) throw new ArgumentNullException(nameof(amplitudes));
            int length = RequiredLength(pulses.Count);
            if (timings.Length < length || amplitudes.Length < length) throw new ArgumentException("Waveform buffers are too small.");
            if (length == 0) return 0;
            timings[0] = 0; amplitudes[0] = 0;
            for (int i = 0; i < pulses.Count; i++) {
                int index = 2 * i + 1; timings[index] = pulses[i].DurationMs; amplitudes[index] = pulses[i].Amplitude;
                if (i < pulses.Count - 1) { timings[index + 1] = pulses[i].GapAfterMs; amplitudes[index + 1] = 0; }
            }
            return length;
        }
        /// <summary>Sums on-times and internal gaps, ignoring the final gap.</summary>
        public static long TotalMilliseconds(IReadOnlyList<HapticPulse> pulses)
        {
            if (pulses == null) throw new ArgumentNullException(nameof(pulses));
            long total = 0;
            for (int i = 0; i < pulses.Count; i++) total += pulses[i].DurationMs + (i < pulses.Count - 1 ? (long)pulses[i].GapAfterMs : 0);
            return total;
        }
    }
}
