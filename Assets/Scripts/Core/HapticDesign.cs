using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Phone-tunable haptic patterns. Real motors differ: tune these on a physical phone.</summary>
    public static class HapticDesign
    {
        private static readonly IReadOnlyList<HapticPulse> Exit = Array.AsReadOnly(new[] { new HapticPulse(12,70) });
        private static readonly IReadOnlyList<HapticPulse> Blocked = Array.AsReadOnly(new[] { new HapticPulse(40,200) });
        private static readonly IReadOnlyList<HapticPulse> Undo = Array.AsReadOnly(new[] { new HapticPulse(8,45) });
        private static readonly IReadOnlyList<HapticPulse> Complete = Array.AsReadOnly(new[] { new HapticPulse(20,90,60),new HapticPulse(20,130,60),new HapticPulse(45,220) });
        private static readonly IReadOnlyList<HapticPulse> Preview = Array.AsReadOnly(new[] { new HapticPulse(15,90) });
        private static readonly IReadOnlyList<HapticPulse> Slide = Array.AsReadOnly(new[] { new HapticPulse(10,55) });
        private static readonly IReadOnlyList<HapticPulse> Star = Array.AsReadOnly(new[] { new HapticPulse(10,50) });
        private static readonly IReadOnlyList<HapticPulse> Purchase = Array.AsReadOnly(new[] { new HapticPulse(20,100) });
        private static readonly IReadOnlyList<HapticPulse> Button = Array.AsReadOnly(new[] { new HapticPulse(6,40) });
        /// <summary>Returns an immutable, cached pattern.</summary>
        public static IReadOnlyList<HapticPulse> GetPattern(HapticCue cue)
        {
            switch (cue) {
                case HapticCue.Exit: return Exit; case HapticCue.Blocked: return Blocked; case HapticCue.Undo: return Undo;
                case HapticCue.LevelComplete: return Complete; case HapticCue.Preview: return Preview;
                case HapticCue.Slide: return Slide;
                case HapticCue.Star: case HapticCue.Coin: return Star;
                case HapticCue.Purchase: return Purchase; case HapticCue.Button: return Button;
                default: throw new ArgumentOutOfRangeException(nameof(cue));
            }
        }
        /// <summary>Minimum interval between accepted requests of this cue.</summary>
        public static float MinIntervalSeconds(HapticCue cue)
        {
            switch (cue) {
                case HapticCue.Exit: case HapticCue.Undo: return .04f;
                case HapticCue.Blocked: return .08f; case HapticCue.LevelComplete: return .5f; case HapticCue.Preview: return .2f;
                case HapticCue.Slide: return .05f;
                case HapticCue.Purchase: return .1f; case HapticCue.Button: return .05f;
                case HapticCue.Coin: case HapticCue.Star: return 0; // Presentation emits each star exactly once.
                default: throw new ArgumentOutOfRangeException(nameof(cue));
            }
        }
    }
}
