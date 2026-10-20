using ArrowEscape.Core;

namespace ArrowEscape.View
{
    /// <summary>Silent implementation for Editor, desktop and iOS.</summary>
    public sealed class NullHapticsPlayer : IHapticsPlayer
    {
        /// <summary>No vibration hardware is exposed by this implementation.</summary>
        public bool IsSupported => false;
        /// <summary>Intentionally does nothing.</summary>
        public void Play(HapticCue cue) { }
        /// <summary>Intentionally does nothing.</summary>
        public void Cancel() { }
    }
}
