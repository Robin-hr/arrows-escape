using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Only Android receives a hardware adapter; iOS needs a future native plugin.</summary>
    public static class HapticsPlayerFactory
    {
        /// <summary>Never constructs Android objects in the Editor or on desktop/iOS.</summary>
        public static IHapticsPlayer Create() => Application.platform == RuntimePlatform.Android ?
            (IHapticsPlayer)new AndroidHapticsPlayer() : new NullHapticsPlayer();
    }
}
