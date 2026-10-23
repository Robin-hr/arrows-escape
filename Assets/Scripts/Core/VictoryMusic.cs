using System;
namespace ArrowEscape.Core
{
    /// <summary>
    /// Procedural victory fanfare (about 3.6 s): a brassy rising pickup, a held C-major chord with bass and a
    /// sparkling arpeggio on top, finished with a short room echo. Pure and deterministic.
    /// </summary>
    public static class VictoryMusic
    {
        /// <summary>Length of the fanfare in seconds.</summary>
        public const float Seconds = 3.6f;
        private static float Hz(int semitonesFromA4) => (float)(440.0 * Math.Pow(2, semitonesFromA4 / 12.0));

        /// <summary>Renders mono samples in [-0.8, 0.8].</summary>
        public static float[] Render(int sampleRate)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            int count = (int)Math.Round(Seconds * sampleRate); var mix = new float[count];
            // Pickup: G4 C5 E5 (triplet), then G5 - the fanfare call.
            Brass(mix, sampleRate, Hz(-2), 0.00f, 0.16f, 0.55f);
            Brass(mix, sampleRate, Hz(3), 0.13f, 0.16f, 0.55f);
            Brass(mix, sampleRate, Hz(7), 0.26f, 0.16f, 0.55f);
            Brass(mix, sampleRate, Hz(10), 0.39f, 0.42f, 0.65f);
            // Second phrase: E5 G5 then the big C6 landing.
            Brass(mix, sampleRate, Hz(7), 0.86f, 0.14f, 0.5f);
            Brass(mix, sampleRate, Hz(10), 0.98f, 0.14f, 0.55f);
            Brass(mix, sampleRate, Hz(15), 1.12f, 1.6f, 0.8f);
            // Held chord C4 E4 G4 C5 and bass C3 under the landing.
            foreach (int n in new[] { -9, -5, -2, 3 }) Brass(mix, sampleRate, Hz(n), 1.12f, 1.9f, 0.32f);
            Bass(mix, sampleRate, Hz(-21), 1.12f, 1.9f, 0.6f);
            Bass(mix, sampleRate, Hz(-26), 0.0f, 0.8f, 0.35f);
            // Sparkle arpeggio C6 E6 G6 C7 over the held chord.
            int[] sparkle = { 15, 19, 22, 27, 22, 27, 31 };
            for (int i = 0; i < sparkle.Length; i++) Bell(mix, sampleRate, Hz(sparkle[i]), 1.25f + i * 0.09f, 0.9f, 0.22f);
            Echo(mix, sampleRate, 0.19f, 0.28f);
            Normalize(mix, 0.8f);
            return mix;
        }

        // Saw-like tone with a soft attack and a closing brightness, roughly a synth brass.
        private static void Brass(float[] mix, int rate, float hz, float start, float length, float gain)
        {
            int s = (int)(start * rate), n = (int)(length * rate);
            for (int i = 0; i < n && s + i < mix.Length; i++)
            {
                double t = (double)i / rate, phase = 2 * Math.PI * hz * t;
                double vib = 1 + 0.004 * Math.Sin(2 * Math.PI * 5.5 * t) * Math.Min(1, t * 3);
                double bright = 0.55 + 0.45 * Math.Exp(-t * 3);
                double tone = Math.Sin(phase * vib) + bright * (0.5 * Math.Sin(2 * phase * vib) + 0.33 * Math.Sin(3 * phase * vib) + 0.2 * Math.Sin(4 * phase * vib) + 0.12 * Math.Sin(5 * phase * vib));
                double env = Math.Min(1, t / 0.025) * Math.Min(1, (length - t) / 0.08) * (0.85 + 0.15 * Math.Exp(-t * 6));
                mix[s + i] += (float)(tone * env * gain * 0.35);
            }
        }

        private static void Bass(float[] mix, int rate, float hz, float start, float length, float gain)
        {
            int s = (int)(start * rate), n = (int)(length * rate);
            for (int i = 0; i < n && s + i < mix.Length; i++)
            {
                double t = (double)i / rate, phase = 2 * Math.PI * hz * t;
                double env = Math.Min(1, t / 0.01) * Math.Exp(-t * 1.2) * Math.Min(1, (length - t) / 0.1);
                mix[s + i] += (float)((Math.Sin(phase) + 0.3 * Math.Sin(2 * phase)) * env * gain * 0.45);
            }
        }

        private static void Bell(float[] mix, int rate, float hz, float start, float length, float gain)
        {
            int s = (int)(start * rate), n = (int)(length * rate);
            for (int i = 0; i < n && s + i < mix.Length; i++)
            {
                double t = (double)i / rate, phase = 2 * Math.PI * hz * t;
                double tone = Math.Sin(phase) + 0.4 * Math.Sin(2.76 * phase) + 0.2 * Math.Sin(5.4 * phase);
                mix[s + i] += (float)(tone * Math.Exp(-t * 6) * Math.Min(1, t / 0.003) * gain * 0.3);
            }
        }

        private static void Echo(float[] mix, int rate, float delay, float feedback)
        {
            int d = (int)(delay * rate);
            for (int i = d; i < mix.Length; i++) mix[i] += mix[i - d] * feedback;
        }

        private static void Normalize(float[] mix, float peak)
        {
            float max = 0; foreach (float v in mix) max = Math.Max(max, Math.Abs(v));
            if (max <= 0) return; float k = peak / max; for (int i = 0; i < mix.Length; i++) mix[i] *= k;
            int fade = Math.Min(mix.Length, 2000); for (int i = 0; i < fade; i++) mix[mix.Length - 1 - i] *= i / (float)fade;
        }
    }
}
