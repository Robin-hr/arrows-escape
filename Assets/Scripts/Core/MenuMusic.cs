using System;

namespace ArrowEscape.Core
{
    /// <summary>
    /// Menu background music (Step 14L): a calm, seamlessly looping 8-bar synthwave tune (soft pad chords, round bass,
    /// a twinkling arpeggio and a simple pentatonic melody) at 100 BPM. Played on the menus, never on the board.
    /// Pure synthesis; note tails wrap around so the loop has no seam.
    /// </summary>
    public static class MenuMusic
    {
        /// <summary>Tempo.</summary>
        public const float Bpm = 100f;
        /// <summary>Bars in the loop.</summary>
        public const int Bars = 8;
        /// <summary>Loop length in seconds.</summary>
        public static float Seconds => Bars * 4 * 60f / Bpm;

        // Chord roots per bar (A minor - F - C - G, twice) as MIDI notes; chord = root, third, fifth.
        private static readonly int[] roots = { 57, 53, 48, 55, 57, 53, 48, 55 };
        private static readonly bool[] minor = { true, false, false, false, true, false, false, false };
        // Melody: (bar, beat in eighths, midi, length in eighths); A-minor pentatonic.
        private static readonly int[,] melody =
        {
            {0,0,76,3},{0,3,74,1},{0,4,72,4}, {1,0,69,2},{1,2,72,2},{1,4,74,4}, {2,0,76,2},{2,2,79,2},{2,4,76,4}, {3,0,74,6},
            {4,0,72,3},{4,3,74,1},{4,4,76,4}, {5,0,79,2},{5,2,81,2},{5,4,79,4}, {6,0,76,2},{6,2,74,2},{6,4,72,2},{6,6,74,2}, {7,0,69,8},
        };

        private static float Hz(int midi) => 440f * (float)Math.Pow(2, (midi - 69) / 12.0);

        /// <summary>Renders the loop (mono, peak about 0.6).</summary>
        public static float[] Render(int rate)
        {
            float beat = 60f / Bpm, eighth = beat / 2; int n = (int)(Seconds * rate); var s = new float[n];
            for (int bar = 0; bar < Bars; bar++)
            {
                float barStart = bar * 4 * beat; int root = roots[bar];
                int[] chord = { root, root + (minor[bar] ? 3 : 4), root + 7 };
                // Pad: three soft detuned sines per chord note, swelling over the bar.
                foreach (int m in chord) Add(s, rate, barStart, 4 * beat, Hz(m), 0.055f, Voice.Pad);
                // Bass: root on beats 1 and 3, an octave lower.
                Add(s, rate, barStart, beat * 1.8f, Hz(root - 12), 0.16f, Voice.Bass);
                Add(s, rate, barStart + 2 * beat, beat * 1.8f, Hz(root - 12), 0.13f, Voice.Bass);
                // Arpeggio: eighth notes up and down the chord, an octave up.
                int[] arp = { chord[0], chord[1], chord[2], chord[1] + 12, chord[2], chord[1], chord[0] + 12, chord[2] };
                for (int k = 0; k < 8; k++) Add(s, rate, barStart + k * eighth, eighth * 1.6f, Hz(arp[k] + 12), 0.035f, Voice.Pluck);
            }
            for (int i = 0; i < melody.GetLength(0); i++)
                Add(s, rate, melody[i, 0] * 4 * beat + melody[i, 1] * eighth, melody[i, 3] * eighth, Hz(melody[i, 2]), 0.075f, Voice.Lead);
            float peak = 0; foreach (float v in s) peak = Math.Max(peak, Math.Abs(v));
            if (peak > 1e-6f) { float g = 0.6f / peak; for (int i = 0; i < n; i++) s[i] *= g; }
            return s;
        }

        private enum Voice { Pad, Bass, Pluck, Lead }

        // Adds one note; samples past the loop end wrap to the start (seamless loop).
        private static void Add(float[] s, int rate, float start, float length, float hz, float gain, Voice voice)
        {
            int n = s.Length, begin = (int)(start * rate), count = (int)((length + (voice == Voice.Pad ? 0.6f : 0.35f)) * rate);
            for (int i = 0; i < count; i++)
            {
                double t = i / (double)rate; double env;
                switch (voice)
                {
                    case Voice.Pad: env = Math.Min(1, t / 0.5) * (t < length ? 1 : Math.Exp(-(t - length) * 6)); break;
                    case Voice.Bass: env = Math.Min(1, t / 0.01) * Math.Exp(-t * 2.2) * (t < length ? 1 : Math.Exp(-(t - length) * 20)); break;
                    case Voice.Pluck: env = Math.Min(1, t / 0.003) * Math.Exp(-t * 9); break;
                    default: env = Math.Min(1, t / 0.02) * Math.Exp(-t * 1.6) * (t < length ? 1 : Math.Exp(-(t - length) * 14)); break;
                }
                double w = 2 * Math.PI * hz * t, v;
                switch (voice)
                {
                    case Voice.Pad: v = Math.Sin(w) + 0.6 * Math.Sin(w * 1.004) + 0.4 * Math.Sin(w * 0.996) + 0.15 * Math.Sin(2 * w); break;
                    case Voice.Bass: v = Math.Sin(w) + 0.3 * Math.Sin(2 * w); break;
                    case Voice.Pluck: v = Math.Sin(w) + 0.25 * Math.Sin(3 * w); break;
                    default: v = Math.Sin(w + 0.3 * Math.Sin(2 * Math.PI * 5.5 * t)) + 0.2 * Math.Sin(2 * w); break;
                }
                int idx = (begin + i) % n;
                s[idx] += (float)(v * env * gain);
            }
        }
    }
}
