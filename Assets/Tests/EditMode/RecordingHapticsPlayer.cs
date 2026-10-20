using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.View;
namespace ArrowEscape.Tests
{
    /// <summary>Fake haptics hardware; never constructs Android Java objects.</summary>
    public sealed class RecordingHapticsPlayer : IHapticsPlayer
    {
        /// <summary>Simulated hardware capability.</summary>
        public bool IsSupported { get; set; } = true;
        /// <summary>Accepted vibration cues in event order.</summary>
        public readonly List<HapticCue> Cues=new List<HapticCue>();
        /// <summary>Number of cancellation requests.</summary>
        public int Cancels;
        /// <summary>Records a cue.</summary>
        public void Play(HapticCue cue)=>Cues.Add(cue);
        /// <summary>Records cancellation.</summary>
        public void Cancel()=>Cancels++;
    }
}
