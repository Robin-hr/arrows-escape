using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Premium frame animation on every page that shows the player's badge (Step 14M: frame only). A premium
    /// (Magic or Mythic) frame gently breathes and sways; there are no extra rings, glows or sparkles, the avatar
    /// itself stays still, and ordinary frames do not move. It reads the badge's current frame every frame, so
    /// equipping a different frame switches the animation at once.
    /// </summary>
    public sealed class AvatarPulse : MonoBehaviour
    {
        private AvatarBadge badge;
        private float clock;
        private string frameId;

        /// <summary>Seconds animated so far.</summary>
        public float Clock => clock;
        /// <summary>The animated badge.</summary>
        public AvatarBadge Badge => badge;
        /// <summary>Avatars never animate (Step 14M): only the frame does.</summary>
        public bool AvatarAnimated => false;
        /// <summary>True when the equipped frame is premium (it animates).</summary>
        public bool FrameAnimated { get; private set; }

        /// <summary>Adds the frame animation to a badge (once).</summary>
        public static AvatarPulse Attach(AvatarBadge target)
        {
            var existing = target.GetComponent<AvatarPulse>(); if (existing != null) return existing;
            var pulse = target.gameObject.AddComponent<AvatarPulse>(); pulse.badge = target;
            NeonKit.Isolate(target.gameObject);
            pulse.Tick(0);
            return pulse;
        }

        private void Refresh()
        {
            if (frameId == badge.FrameId) return;
            frameId = badge.FrameId;
            FrameAnimated = PremiumCosmetics.IsPremiumFrame(frameId);
            if (!FrameAnimated) { badge.FrameImage.rectTransform.localScale = Vector3.one; badge.FrameImage.rectTransform.localRotation = Quaternion.identity; }
        }

        /// <summary>Explicit animation driver; allocation-free.</summary>
        public void Tick(float dt)
        {
            if (badge == null || !(dt >= 0) || float.IsInfinity(dt)) return;
            Refresh();
            if (!FrameAnimated) return;
            clock += dt;
            var frame = badge.FrameImage.rectTransform;
            frame.localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(clock * 2.4f));
            frame.localRotation = Quaternion.Euler(0, 0, 3.5f * Mathf.Sin(clock * 1.3f));
        }

        private void Update() { if (Application.isPlaying) Tick(Time.unscaledDeltaTime); }
    }
}
