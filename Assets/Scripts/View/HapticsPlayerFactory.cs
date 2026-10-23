using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Picks the haptics adapter for the platform: Android motor, iOS system vibration, silent elsewhere.</summary>
    public static class HapticsPlayerFactory
    {
        /// <summary>Never constructs device objects in the Editor or on desktop.</summary>
        public static IHapticsPlayer Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidHapticsPlayer();
#elif UNITY_IOS && !UNITY_EDITOR
            return new IosHapticsPlayer();
#else
            return new NullHapticsPlayer();
#endif
        }
    }
}
