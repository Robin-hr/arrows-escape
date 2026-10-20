using ArrowEscape.Core;

namespace ArrowEscape.View
{
    /// <summary>Hardware boundary; tests record cues without constructing Android objects.</summary>
    public interface IHapticsPlayer
    {
        /// <summary>Whether this session can vibrate.</summary>
        bool IsSupported { get; }
        /// <summary>Plays a prepared cue.</summary>
        void Play(HapticCue cue);
        /// <summary>Cancels vibration immediately.</summary>
        void Cancel();
    }
}
