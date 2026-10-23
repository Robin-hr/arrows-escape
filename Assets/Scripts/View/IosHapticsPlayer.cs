#if UNITY_IOS
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>
    /// iOS haptics without a native plugin: the system vibration for the strong cues only (blocked move and level
    /// complete). Light cues stay silent because iOS offers no amplitude control through Unity's API.
    /// </summary>
    public sealed class IosHapticsPlayer : IHapticsPlayer
    {
        /// <summary>Every iPhone has a vibration motor.</summary>
        public bool IsSupported => SystemInfo.supportsVibration;
        /// <summary>Strong cues vibrate; the rest are skipped.</summary>
        public void Play(HapticCue cue) { if (IsSupported && (cue == HapticCue.Blocked || cue == HapticCue.LevelComplete)) Handheld.Vibrate(); }
        /// <summary>System vibrations cannot be cancelled.</summary>
        public void Cancel() { }
    }
}
#endif
