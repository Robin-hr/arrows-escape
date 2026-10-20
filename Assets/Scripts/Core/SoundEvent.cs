using System;

namespace ArrowEscape.Core
{
    /// <summary>An allocation-free request for a cue, scale step, and volume.</summary>
    public readonly struct SoundEvent : IEquatable<SoundEvent>
    {
        /// <summary>The sound to play.</summary>
        public SoundCue Cue { get; }
        /// <summary>Note index, or -1 for a non-tonal cue.</summary>
        public int ScaleStep { get; }
        /// <summary>Playback gain, clamped to 0..1.</summary>
        public float Volume { get; }
        /// <summary>Creates an immutable request; invalid gain becomes silence.</summary>
        public SoundEvent(SoundCue cue, int scaleStep, float volume)
        {
            Cue = cue;
            ScaleStep = scaleStep;
            Volume = float.IsNaN(volume) ? 0 : Math.Max(0, Math.Min(1, volume));
        }
        /// <summary>Compares all three values.</summary>
        public bool Equals(SoundEvent other) => Cue == other.Cue && ScaleStep == other.ScaleStep && Volume == other.Volume;
        /// <summary>Compares a boxed request.</summary>
        public override bool Equals(object obj) => obj is SoundEvent other && Equals(other);
        /// <summary>Hashes all three values.</summary>
        public override int GetHashCode() => ((int)Cue * 397 ^ ScaleStep) * 397 ^ Volume.GetHashCode();
        /// <summary>Value equality.</summary>
        public static bool operator ==(SoundEvent left, SoundEvent right) => left.Equals(right);
        /// <summary>Value inequality.</summary>
        public static bool operator !=(SoundEvent left, SoundEvent right) => !left.Equals(right);
    }
}
