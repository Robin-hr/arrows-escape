using System;

namespace ArrowEscape.Core
{
    /// <summary>
    /// Step 14M sound effects (pure, deterministic synthesis):
    /// <see cref="LoseMusic"/> - a sad "wah wah wah waaah" trombone when the hearts run out;
    /// <see cref="CashRegister"/> - a "ka-ching" when something is bought;
    /// <see cref="WrongBuzz"/> - a two-beat "eh-eh" wrong-answer buzzer for a blocked (wrong) move.
    /// </summary>
    public static class GameSounds
    {
        /// <summary>Length of the lose jingle.</summary>
        public const float LoseSeconds = 3.0f;
        /// <summary>Length of the purchase sound.</summary>
        public const float CashSeconds = 0.95f;
        /// <summary>Length of the wrong-move buzzer.</summary>
        public const float BuzzSeconds = 0.42f;

        private static float Hz(int midi) => 440f * (float)Math.Pow(2, (midi - 69) / 12.0);

        /// <summary>Sad trombone: four falling notes, the last one long with a wobbling "wah".</summary>
        public static float[] LoseMusic(int rate)
        {
            int n = (int)(LoseSeconds * rate); var s = new float[n];
            int[] notes = { 58, 57, 56, 55 }; float[] starts = { 0.05f, 0.52f, 0.99f, 1.46f }, lengths = { 0.4f, 0.4f, 0.4f, 1.35f };
            for (int k = 0; k < 4; k++)
            {
                float hz = Hz(notes[k]); int begin = (int)(starts[k] * rate), count = (int)((lengths[k] + 0.12f) * rate); double phase = 0;
                for (int i = 0; i < count && begin + i < n; i++)
                {
                    double t = i / (double)rate, len = lengths[k];
                    double env = Math.Min(1, t / 0.04) * (t < len ? 1 : Math.Exp(-(t - len) * 25));
                    // "wah": a plunger mute opening and closing changes how bright the tone is.
                    double wah = k < 3 ? 0.25 + 0.75 * Math.Sin(Math.PI * Math.Min(1, t / len)) : 0.45 + 0.4 * Math.Sin(2 * Math.PI * 4.5 * t) * Math.Min(1, t / 0.3);
                    double vib = k == 3 ? 1 + 0.012 * Math.Sin(2 * Math.PI * 5 * t) * Math.Min(1, t / 0.3) : 1;
                    double slide = k == 3 ? 1 - 0.03 * Math.Max(0, t - 0.9) : 1;
                    phase += hz * vib * slide / rate;
                    double v = 0;
                    for (int h = 1; h <= 8; h++) v += Math.Sin(2 * Math.PI * h * phase) / h * Math.Pow(wah, h - 1);
                    s[begin + i] += (float)(v * env * 0.5);
                }
            }
            return Normalize(s, 0.8f);
        }

        /// <summary>Cash register: mechanical clunk and drawer rattle, then a bright bell "ching" and coin sparkle.</summary>
        public static float[] CashRegister(int rate)
        {
            int n = (int)(CashSeconds * rate); var s = new float[n]; uint seed = 4242u;
            float Noise() { seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; return (seed & 0xFFFF) / 32768f - 1f; }
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)rate; float x = Noise(); lp += 0.25f * (x - lp);
                double clunk = (Math.Sin(2 * Math.PI * 110 * t) * 0.8 + lp * 1.5) * Math.Exp(-t * 45);                       // "ka"
                double rattle = t > 0.04 && t < 0.2 ? lp * 0.6 * (0.5 + 0.5 * Math.Sin(2 * Math.PI * 38 * t)) * Math.Exp(-(t - 0.04) * 12) : 0;
                double bt = t - 0.14, ching = 0;                                                                              // "ching"
                if (bt > 0) ching = (Math.Sin(2 * Math.PI * 2093 * bt) + 0.7 * Math.Sin(2 * Math.PI * 3136 * bt) + 0.35 * Math.Sin(2 * Math.PI * 5274 * bt)) * Math.Exp(-bt * 5.5) * Math.Min(1, bt / 0.002);
                double ct = t - 0.2, coins = 0;
                if (ct > 0) for (int k = 0; k < 4; k++) { double c = ct - k * 0.07; if (c > 0) coins += Math.Sin(2 * Math.PI * (4200 + 600 * k) * c) * Math.Exp(-c * 30) * 0.25; }
                s[i] = (float)(clunk * 0.55 + rattle + ching * 0.5 + coins);
            }
            return Normalize(s, 0.75f);
        }

        /// <summary>Wrong-answer buzzer: two short, low, buzzy "eh-eh" tones.</summary>
        public static float[] WrongBuzz(int rate)
        {
            int n = (int)(BuzzSeconds * rate); var s = new float[n];
            for (int b = 0; b < 2; b++)
            {
                float start = b * 0.21f, length = 0.16f; int begin = (int)(start * rate), count = (int)(length * rate);
                double phase1 = 0, phase2 = 0;
                for (int i = 0; i < count && begin + i < n; i++)
                {
                    double t = i / (double)rate, env = Math.Min(1, t / 0.006) * Math.Min(1, (length - t) / 0.02);
                    phase1 += 155.0 / rate; phase2 += 162.0 / rate;   // slightly detuned pair: a harsh "wrong" buzz
                    double sq1 = phase1 % 1 < 0.5 ? 1 : -1, sq2 = phase2 % 1 < 0.5 ? 1 : -1;
                    s[begin + i] += (float)((sq1 + sq2) * 0.5 * env);
                }
            }
            // Soften the square edges a little (one-pole low-pass) so it buzzes without hurting.
            float y = 0; for (int i = 0; i < n; i++) { y += 0.35f * (s[i] - y); s[i] = y; }
            return Normalize(s, 0.6f);
        }

        private static float[] Normalize(float[] s, float target)
        {
            float peak = 0; foreach (float v in s) peak = Math.Max(peak, Math.Abs(v));
            if (peak > 1e-6f) { float g = target / peak; for (int i = 0; i < s.Length; i++) s[i] *= g; }
            int fade = Math.Min(s.Length, 200); for (int i = 0; i < fade; i++) s[s.Length - 1 - i] *= i / (float)fade;
            return s;
        }
    }
}
