using System;

namespace ArrowEscape.Core
{
    /// <summary>
    /// Shop tap sounds (Step 14L): the instrument that plays the exit melody when an arrow flies off. Index 0 is the
    /// original Classic Chime; 1..10 are the ten shop sounds. Every instrument still follows the move-count pentatonic
    /// melody, only the timbre changes. Pure, deterministic synthesis (no engine types).
    /// </summary>
    public static class TapSoundSynth
    {
        /// <summary>Shop ids in instrument order.</summary>
        public static readonly string[] Ids =
        {
            "sound_classic", "sound_marimba", "sound_kalimba", "sound_musicbox", "sound_glassbell", "sound_synthpluck",
            "sound_retrocoin", "sound_bubblepop", "sound_woodblock", "sound_laserzap", "sound_spacewhoosh",
        };

        /// <summary>Number of instruments, including Classic.</summary>
        public static int Count => Ids.Length;

        /// <summary>Free default tap sound.</summary>
        public const string DefaultId = "sound_classic";

        /// <summary>Instrument index of a shop id (0 for unknown ids).</summary>
        public static int IndexOf(string id) { int i = Array.IndexOf(Ids, id); return i < 0 ? 0 : i; }

        /// <summary>Renders one note of an instrument (peaks matched to the Classic Chime; buzzy waves quieter).</summary>
        public static float[] Render(int instrument, float hz, int rate)
        {
            if (instrument <= 0) return SynthMath.RenderNote(hz, SoundDesign.NoteSeconds, rate);
            float seconds = instrument == 4 ? 1.1f : instrument == 10 ? 0.7f : instrument == 9 ? 0.32f : 0.5f;
            int n = (int)(seconds * rate); var s = new float[n];
            uint noise = 22222u + (uint)instrument * 977u;
            float Noise() { noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5; return (noise & 0xFFFF) / 32768f - 1f; }
            double phase = 0, phase2 = 0; float lp = 0;
            // Karplus-Strong buffer for the synth pluck.
            int period = Math.Max(2, (int)(rate / Math.Max(40f, hz))); var ks = new float[period];
            for (int i = 0; i < period; i++) ks[i] = Noise();
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)rate; float v = 0;
                double attack = Math.Min(1.0, t / 0.004);
                switch (instrument)
                {
                    case 1: // marimba: warm fundamental + fast wooden 4th partial
                        v = (float)(attack * (Math.Sin(2 * Math.PI * hz * t) * Math.Exp(-t * 7) + 0.45 * Math.Sin(2 * Math.PI * hz * 4 * t) * Math.Exp(-t * 30)));
                        break;
                    case 2: // kalimba: bright tine with a metallic 3rd overtone
                        v = (float)(attack * (Math.Sin(2 * Math.PI * hz * t) * Math.Exp(-t * 5) + 0.35 * Math.Sin(2 * Math.PI * hz * 5.4 * t) * Math.Exp(-t * 18) + 0.15 * Math.Sin(2 * Math.PI * hz * 2 * t) * Math.Exp(-t * 9)));
                        break;
                    case 3: // music box: high, tinkly comb tooth
                        v = (float)(attack * (Math.Sin(2 * Math.PI * hz * 2 * t) * Math.Exp(-t * 4.5) + 0.3 * Math.Sin(2 * Math.PI * hz * 6 * t) * Math.Exp(-t * 12)));
                        break;
                    case 4: // glass bell: inharmonic partials with a long shimmer
                        v = (float)(attack * (Math.Sin(2 * Math.PI * hz * t) * Math.Exp(-t * 2.6) + 0.5 * Math.Sin(2 * Math.PI * hz * 2.76 * t) * Math.Exp(-t * 4) + 0.25 * Math.Sin(2 * Math.PI * hz * 5.4 * t) * Math.Exp(-t * 7)) * (1 + 0.05 * Math.Sin(2 * Math.PI * 6 * t)));
                        break;
                    case 5: // synth pluck: plucked string (Karplus-Strong)
                    {
                        int k = i % period; float next = ks[(k + 1) % period];
                        float outv = ks[k]; ks[k] = 0.497f * (outv + next);
                        v = outv * (float)Math.Exp(-t * 3);
                        break;
                    }
                    case 6: // retro coin: 8-bit square, jumps up a fifth
                    {
                        double f = t < 0.06 ? hz : hz * 1.5; phase += f / rate;
                        v = (float)((phase % 1 < 0.5 ? 0.5 : -0.5) * Math.Exp(-t * 6));
                        break;
                    }
                    case 7: // bubble pop: sine blip gliding upwards
                    {
                        double f = hz * (1 + 1.6 * (1 - Math.Exp(-t * 25))); phase += f / rate;
                        v = (float)(Math.Sin(2 * Math.PI * phase) * Math.Exp(-t * 14) * attack);
                        break;
                    }
                    case 8: // wood block: knock of filtered noise plus a hollow resonance
                        lp += 0.35f * (Noise() - lp);
                        v = (float)(lp * Math.Exp(-t * 90) * 0.9 + Math.Sin(2 * Math.PI * hz * 1.5 * t) * Math.Exp(-t * 22) * attack);
                        break;
                    case 9: // laser zap: sawtooth diving from high to the note
                    {
                        double f = hz * (1 + 3 * Math.Exp(-t * 28)); phase += f / rate;
                        v = (float)((2 * (phase % 1) - 1) * 0.55 * Math.Exp(-t * 9));
                        break;
                    }
                    default: // 10 space whoosh: airy noise swell under a soft rising tone
                    {
                        lp += 0.08f * (Noise() - lp);
                        double env = Math.Sin(Math.PI * Math.Min(1, t / seconds)); double f = hz * (0.75 + 0.5 * t / seconds); phase2 += f / rate;
                        v = (float)((lp * 1.6 + 0.6 * Math.Sin(2 * Math.PI * phase2)) * env);
                        break;
                    }
                }
                s[i] = v;
            }
            float peak = 0; for (int i = 0; i < n; i++) peak = Math.Max(peak, Math.Abs(s[i]));
            float target = instrument == 6 ? 0.5f : instrument == 9 ? 0.55f : instrument == 10 ? 0.7f : 0.8f;
            if (peak > 1e-6f) { float g = target / peak; for (int i = 0; i < n; i++) s[i] *= g; }
            // short fade-out so no click at the end
            int fade = Math.Min(n, rate / 100); for (int i = 0; i < fade; i++) s[n - 1 - i] *= i / (float)fade;
            return s;
        }
    }
}
