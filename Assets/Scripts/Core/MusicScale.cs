using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Pure C-major pentatonic pitch and ping-pong math.</summary>
    public static class MusicScale
    {
        // Computed read-only data avoids a mutable static array or cache.
        /// <summary>Read-only semitone offsets within one octave: C, D, E, G, A.</summary>
        public static IReadOnlyList<int> Offsets => Array.AsReadOnly(new[] { 0, 2, 4, 7, 9 });
        /// <summary>Five notes per octave, plus the final root.</summary>
        public static int NoteCount(int octaves)
        {
            if (octaves < 1 || octaves > (int.MaxValue - 1) / 5) throw new ArgumentOutOfRangeException(nameof(octaves));
            return 5 * octaves + 1;
        }
        /// <summary>Distance above the root in semitones.</summary>
        public static int SemitonesFor(int noteIndex)
        {
            if (noteIndex < 0 || noteIndex / 5 > (int.MaxValue - 9) / 12) throw new ArgumentOutOfRangeException(nameof(noteIndex));
            int offset = noteIndex % 5;
            return 12 * (noteIndex / 5) + (offset < 3 ? offset * 2 : offset == 3 ? 7 : 9);
        }
        /// <summary>Equal-tempered frequency for a note index.</summary>
        public static float FrequencyHz(int noteIndex, float rootHz)
        {
            if (!(rootHz > 0) || float.IsInfinity(rootHz)) throw new ArgumentOutOfRangeException(nameof(rootHz));
            return (float)(rootHz * Math.Pow(2, SemitonesFor(noteIndex) / 12.0));
        }
        /// <summary>Climbs to the highest note then descends, without repeating endpoints.</summary>
        public static int PingPongIndex(int step, int noteCount)
        {
            if (noteCount < 1) throw new ArgumentOutOfRangeException(nameof(noteCount));
            if (noteCount == 1) return 0;
            long period = 2L * (noteCount - 1);
            long position = Math.Max(0, step) % period;
            return (int)(position < noteCount ? position : period - position);
        }
    }
}
