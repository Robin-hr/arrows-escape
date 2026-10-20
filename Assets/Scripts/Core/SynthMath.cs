using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Deterministic mono synthesis. Rendering allocates only during clip preparation.</summary>
    public static class SynthMath
    {
        /// <summary>Renders a deterministic 1200-to-600 Hz icy glide with a gentle second harmonic.</summary>
        public static float[] RenderSlide(float durationSeconds,int sampleRate)
        {
            int count=SampleCount(durationSeconds,sampleRate);var samples=new float[count];double rate=Math.Log(2)/durationSeconds;
            for(int i=0;i<count;i++)
            {
                double time=(double)i/sampleRate;
                double phase=2*Math.PI*1200*(1-Math.Exp(-rate*time))/rate;
                samples[i]=(float)((Math.Sin(phase)+.2*Math.Sin(2*phase))*Math.Exp(-5*time/durationSeconds)*Edge(i,count,sampleRate));
            }
            Normalize(samples);return samples;
        }
        /// <summary>Renders a decaying fundamental with gentle second and third harmonics.</summary>
        public static float[] RenderNote(float frequencyHz, float durationSeconds, int sampleRate)
        {
            int count = SampleCount(durationSeconds, sampleRate);
            if (!Finite(frequencyHz) || frequencyHz <= 0 || frequencyHz >= sampleRate / 2.0)
                throw new ArgumentOutOfRangeException(nameof(frequencyHz));
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                double time = (double)i / sampleRate;
                double phase = 2 * Math.PI * frequencyHz * time;
                double tone = Math.Sin(phase) + 0.35 * Math.Sin(2 * phase) + 0.12 * Math.Sin(3 * phase);
                samples[i] = (float)(tone * Math.Exp(-5 * time / durationSeconds) * Edge(i, count, sampleRate));
            }
            Normalize(samples);
            return samples;
        }
        /// <summary>Renders a fast-decaying pitch drop from 140 Hz toward 70 Hz.</summary>
        public static float[] RenderThud(float durationSeconds, int sampleRate)
        {
            int count = SampleCount(durationSeconds, sampleRate);
            var samples = new float[count];
            double rate = Math.Log(2) / durationSeconds;
            for (int i = 0; i < count; i++)
            {
                double time = (double)i / sampleRate;
                // Integrate the exponential frequency so phase stays continuous.
                double phase = 2 * Math.PI * 140 * (1 - Math.Exp(-rate * time)) / rate;
                samples[i] = (float)(Math.Sin(phase) * Math.Exp(-9 * time / durationSeconds) * Edge(i, count, sampleRate));
            }
            Normalize(samples);
            return samples;
        }
        /// <summary>Mixes notes onto a fixed timeline, truncating notes beyond its boundaries.</summary>
        public static float[] RenderMix(IReadOnlyList<NoteSpec> notes, float totalSeconds, int sampleRate)
        {
            if (notes == null) throw new ArgumentNullException(nameof(notes));
            var mix = new float[SampleCount(totalSeconds, sampleRate)];
            for (int n = 0; n < notes.Count; n++)
            {
                NoteSpec note = notes[n];
                if (!Finite(note.StartSeconds)) continue;
                double offset = Math.Round((double)note.StartSeconds * sampleRate);
                if (offset >= mix.Length || offset <= int.MinValue) continue;
                float[] samples = RenderNote(note.FrequencyHz, note.DurationSeconds, sampleRate);
                long start = (long)offset;
                for (int i = (int)Math.Max(0, -start); i < samples.Length && start + i < mix.Length; i++)
                    mix[(int)(start + i)] += samples[i];
            }
            Normalize(mix);
            return mix;
        }
        private static int SampleCount(float duration, int rate)
        {
            if (rate < 8000) throw new ArgumentOutOfRangeException(nameof(rate));
            double count = Math.Round((double)duration * rate);
            if (!Finite(duration) || duration <= 0 || count > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(duration));
            return (int)count;
        }
        private static double Edge(int i, int count, int rate) =>
            Math.Min(1, i / (rate * 0.005)) * Math.Min(1, (count - 1 - i) / (rate * 0.01));
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void Normalize(float[] samples)
        {
            if (samples.Length == 0) return;
            float peak = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                if (!Finite(samples[i])) samples[i] = 0;
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            if (peak > 0)
                for (int i = 0; i < samples.Length; i++) samples[i] = samples[i] / peak * SoundDesign.PeakLevel;
            samples[samples.Length - 1] = 0;
        }
    }
}
