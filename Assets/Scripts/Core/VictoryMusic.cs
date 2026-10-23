using System;

namespace ArrowEscape.Core
{
    /// <summary>
    /// Level-complete fanfare (Step 14M): a short triumphant brass fanfare in C major. A timpani roll swells, a brass
    /// triplet pickup leads into a rising call, and the final C lands on a cymbal crash with a full brass and string
    /// chord, timpani hits and a bright shimmer. Pure, deterministic synthesis.
    /// </summary>
    public static class VictoryMusic
    {
        /// <summary>Length in seconds.</summary>
        public const float Seconds = 4.2f;

        private static float Hz(int midi) => 440f * (float)Math.Pow(2, (midi - 69) / 12.0);

        /// <summary>Renders the fanfare (mono, peak 0.85).</summary>
        public static float[] Render(int rate)
        {
            int n = (int)(Seconds * rate); var s = new float[n];
            uint seed = 777u;
            float Noise() { seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; return (seed & 0xFFFF) / 32768f - 1f; }
            // Timpani roll (crescendo) into the first note, then hits on the big notes.
            for (int k = 0; k < 14; k++) Timpani(s, rate, k * 0.042f, Hz(43), 0.10f + 0.03f * k, Noise);
            Timpani(s, rate, 1.00f, Hz(36), 0.8f, Noise); Timpani(s, rate, 1.75f, Hz(36), 1.0f, Noise); Timpani(s, rate, 1.75f, Hz(43), 0.6f, Noise);
            Timpani(s, rate, 3.30f, Hz(36), 0.9f, Noise);
            // Brass fanfare: ta-ta-ta TAAA, ta-ta TAAAAA (C major).
            Brass(s, rate, 0.56f, 0.13f, 67, 0.5f); Brass(s, rate, 0.72f, 0.13f, 67, 0.5f); Brass(s, rate, 0.88f, 0.13f, 67, 0.55f);
            Brass(s, rate, 1.00f, 0.42f, 72, 0.75f); Brass(s, rate, 1.00f, 0.42f, 64, 0.45f);
            Brass(s, rate, 1.45f, 0.13f, 76, 0.6f); Brass(s, rate, 1.60f, 0.13f, 79, 0.65f);
            // The landing: high C with the full chord underneath.
            Brass(s, rate, 1.75f, 2.1f, 84, 0.8f); Brass(s, rate, 1.75f, 2.1f, 79, 0.55f); Brass(s, rate, 1.75f, 2.1f, 76, 0.55f);
            Brass(s, rate, 1.75f, 2.1f, 72, 0.6f); Brass(s, rate, 1.75f, 2.1f, 48, 0.55f); Brass(s, rate, 1.75f, 2.1f, 36, 0.45f);
            foreach (int m in new[] { 60, 64, 67, 72, 76 }) Strings(s, rate, 1.75f, 2.3f, m, 0.16f);
            Cymbal(s, rate, 1.75f, 1.0f, Noise); Cymbal(s, rate, 3.30f, 0.45f, Noise);
            // Bright sparkle arpeggio on the held chord.
            int[] sparkle = { 84, 88, 91, 96, 91, 96, 100 };
            for (int k = 0; k < sparkle.Length; k++) Bell(s, rate, 1.95f + k * 0.11f, Hz(sparkle[k]), 0.12f);
            float peak = 0; foreach (float v in s) peak = Math.Max(peak, Math.Abs(v));
            if (peak > 1e-6f) { float g = 0.85f / peak; for (int i = 0; i < n; i++) s[i] *= g; }
            int fade = rate / 10; for (int i = 0; i < fade; i++) s[n - 1 - i] *= i / (float)fade;
            return s;
        }

        private const int TableSize = 2048;

        // One cycle of a harmonic mix (amplitude of harmonic h = weight(h)), band-limited below Nyquist.
        private static float[] Table(float hz, int rate, Func<int, double> weight, int harmonics)
        {
            var table = new float[TableSize];
            for (int h = 1; h <= harmonics; h++)
            {
                if (hz * h > rate * 0.45) break;
                double a = weight(h);
                for (int k = 0; k < TableSize; k++) table[k] += (float)(a * Math.Sin(2 * Math.PI * h * k / TableSize));
            }
            return table;
        }

        private static float Read(float[] table, double phase) { double x = (phase - Math.Floor(phase)) * TableSize; int a = (int)x; float f = (float)(x - a); return table[a % TableSize] * (1 - f) + table[(a + 1) % TableSize] * f; }

        // Brass: a bright "blat" at the attack settling to a rounder tone (cross-fade of two wavetables) with vibrato.
        private static void Brass(float[] s, int rate, float start, float length, int midi, float gain)
        {
            float hz = Hz(midi); int begin = (int)(start * rate), count = (int)((length + 0.25f) * rate);
            float[] bright = Table(hz, rate, h => 1.0 / h, 10), mellow = Table(hz, rate, h => Math.Pow(0.45, h - 1) / h, 10);
            double phase = 0;
            for (int i = 0; i < count && begin + i < s.Length; i++)
            {
                double t = i / (double)rate;
                double env = Math.Min(1, t / 0.03) * (t < length ? 1 - 0.15 * Math.Min(1, t / 0.4) : 0.85 * Math.Exp(-(t - length) * 12));
                double w = Math.Min(1, t / 0.05) * (0.45 + 0.55 * Math.Exp(-t * 5));
                phase += hz * (1 + 0.004 * Math.Sin(2 * Math.PI * 5.2 * t) * Math.Min(1, t / 0.25)) / rate;
                double v = Read(mellow, phase) * (1 - w) + Read(bright, phase) * w;
                s[begin + i] += (float)(v * env * gain * 0.4);
            }
        }

        private static void Strings(float[] s, int rate, float start, float length, int midi, float gain)
        {
            float hz = Hz(midi); int begin = (int)(start * rate), count = (int)((length + 0.4f) * rate);
            float[] table = Table(hz, rate, h => 1.0 / (h * 1.6), 4);
            double p1 = 0, p2 = 0.3, p3 = 0.6;
            for (int i = 0; i < count && begin + i < s.Length; i++)
            {
                double t = i / (double)rate, env = Math.Min(1, t / 0.25) * (t < length ? 1 : Math.Exp(-(t - length) * 6));
                p1 += hz / rate; p2 += hz * 1.003 / rate; p3 += hz * 0.997 / rate;
                s[begin + i] += (float)((Read(table, p1) + Read(table, p2) + Read(table, p3)) * env * gain * 0.3);
            }
        }

        private static void Timpani(float[] s, int rate, float start, float hz, float gain, Func<float> noise)
        {
            int begin = (int)(start * rate), count = (int)(0.9f * rate);
            for (int i = 0; i < count && begin + i < s.Length; i++)
            {
                double t = i / (double)rate, f = hz * (1 + 0.15 * Math.Exp(-t * 30));
                double v = Math.Sin(2 * Math.PI * f * t) * Math.Exp(-t * 4.5) + 0.4 * Math.Sin(2 * Math.PI * f * 1.5 * t) * Math.Exp(-t * 8) + noise() * Math.Exp(-t * 60) * 0.25;
                s[begin + i] += (float)(v * gain * 0.5);
            }
        }

        private static void Cymbal(float[] s, int rate, float start, float gain, Func<float> noise)
        {
            int begin = (int)(start * rate), count = (int)(2.2f * rate); float prev = 0;
            for (int i = 0; i < count && begin + i < s.Length; i++)
            {
                double t = i / (double)rate; float x = noise(); float hp = x - prev; prev = x;   // bright, hissy
                s[begin + i] += (float)(hp * Math.Exp(-t * 2.8) * gain * 0.12 * Math.Min(1, t / 0.002));
            }
        }

        private static void Bell(float[] s, int rate, float start, float hz, float gain)
        {
            int begin = (int)(start * rate), count = (int)(0.9f * rate);
            for (int i = 0; i < count && begin + i < s.Length; i++)
            {
                double t = i / (double)rate;
                double v = Math.Sin(2 * Math.PI * hz * t) * Math.Exp(-t * 5) + 0.4 * Math.Sin(2 * Math.PI * hz * 2.76 * t) * Math.Exp(-t * 9);
                s[begin + i] += (float)(v * gain * Math.Min(1, t / 0.003));
            }
        }
    }
}
