namespace ArrowEscape.Core
{
    /// <summary>One note placed on a synthesized timeline.</summary>
    public readonly struct NoteSpec
    {
        /// <summary>Oscillator frequency.</summary>
        public float FrequencyHz { get; }
        /// <summary>Offset from the beginning of the clip.</summary>
        public float StartSeconds { get; }
        /// <summary>Length of this note.</summary>
        public float DurationSeconds { get; }
        /// <summary>Describes a note without rendering samples.</summary>
        public NoteSpec(float frequencyHz, float startSeconds, float durationSeconds)
        { FrequencyHz = frequencyHz; StartSeconds = startSeconds; DurationSeconds = durationSeconds; }
    }
}
