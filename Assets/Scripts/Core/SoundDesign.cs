namespace ArrowEscape.Core
{
    /// <summary>Shared synthesis settings; no runtime state or audio assets.</summary>
    public static class SoundDesign
    {
        /// <summary>Short icy glide sound duration.</summary>
        public const float SlideSeconds=.16f;
        /// <summary>C4, the root of the melody.</summary>
        public const float RootHz = 261.6256f;
        /// <summary>Two pentatonic octaves plus the top C.</summary>
        public const int OctaveCount = 2;
        /// <summary>Mono PCM samples per second.</summary>
        public const int SampleRate = 44100;
        /// <summary>Length of an exit, undo, hint, or preview note.</summary>
        public const float NoteSeconds = 0.55f;
        /// <summary>Length of the low blocked-tap sound.</summary>
        public const float ThudSeconds = 0.18f;
        /// <summary>Normalized sample peak, leaving mixing headroom.</summary>
        public const float PeakLevel = 0.8f;
        /// <summary>Silence baked into the beginning of the completion clip.</summary>
        public const float JingleLeadSeconds = 0.15f;
        /// <summary>E5 previews the Sound setting.</summary>
        public const int PreviewScaleStep = 7;
        /// <summary>G5 marks a hint request.</summary>
        public const int HintScaleStep = 8;
        /// <summary>Lead, four gaps, and the final sustained note.</summary>
        public const float JingleTotalSeconds = JingleLeadSeconds + 4 * 0.11f + 0.9f;
        /// <summary>C5, E5, G5, A5, C6 with a short lead and overlapping tails.</summary>
        public static NoteSpec[] CreateJingle()
        {
            return new[] {
                Note(5, 0, 0.5f), Note(7, 1, 0.5f), Note(8, 2, 0.5f),
                Note(9, 3, 0.5f), Note(10, 4, 0.9f)
            };
        }
        private static NoteSpec Note(int step, int index, float seconds) =>
            new NoteSpec(MusicScale.FrequencyHz(step, RootHz), JingleLeadSeconds + index * 0.11f, seconds);
    }
}
