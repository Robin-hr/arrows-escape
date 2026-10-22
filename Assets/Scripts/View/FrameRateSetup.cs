using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Runs the game at a steady 60 fps. Phones default to 30 fps unless a target is set; vSync stays off so the
    /// target rate (not the display's refresh) paces frames, and the device is kept awake during play.
    /// </summary>
    public static class FrameRateSetup
    {
        /// <summary>Frames per second the game targets on every platform.</summary>
        public const int TargetFps = 60;

        /// <summary>Applied once before the first scene loads.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFps;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
