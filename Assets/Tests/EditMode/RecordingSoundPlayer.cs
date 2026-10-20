using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.View;

namespace ArrowEscape.Tests
{
    /// <summary>Records sound requests without creating clips or using an audio device.</summary>
    public sealed class RecordingSoundPlayer : ISoundPlayer
    {
        /// <summary>All requests, in event order.</summary>
        public readonly List<SoundEvent> Events = new List<SoundEvent>();
        /// <summary>Number of immediate stop requests.</summary>
        public int Stops;
        /// <summary>Records a value copy of the sound.</summary>
        public void Play(SoundEvent soundEvent) => Events.Add(soundEvent);
        /// <summary>Counts stops.</summary>
        public void StopAll() => Stops++;
    }
}
