using ArrowEscape.Core;

namespace ArrowEscape.View
{
    /// <summary>Playback boundary; tests replace Unity audio with a recording fake.</summary>
    public interface ISoundPlayer
    {
        /// <summary>Plays one prepared cue.</summary>
        void Play(SoundEvent soundEvent);
        /// <summary>Immediately stops every active voice.</summary>
        void StopAll();
    }
}
