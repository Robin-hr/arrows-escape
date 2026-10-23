using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Home-page-only life for the profile badge: a rotating neon comet ring and breathing glow behind it, the frame
    /// gently breathing and swaying, the avatar pulsing just out of phase, and three sparkles orbiting the frame.
    /// It animates the badge's existing layers, so it keeps working after the player equips another avatar or frame.
    /// </summary>
    public sealed class AvatarPulse : MonoBehaviour
    {
        private const int Sparkles = 3;
        private static readonly Color GlowA = new Color32(56, 225, 255, 150), GlowB = new Color32(255, 95, 210, 150);
        private AvatarBadge badge;
        private RectTransform ring, glow;
        private UnityEngine.UI.Image glowImage;
        private readonly RectTransform[] sparkles = new RectTransform[Sparkles];
        private readonly UnityEngine.UI.Image[] sparkleImages = new UnityEngine.UI.Image[Sparkles];
        private float size, clock;

        /// <summary>Seconds animated so far.</summary>
        public float Clock => clock;
        /// <summary>The rotating ring behind the frame.</summary>
        public RectTransform Ring => ring;
        /// <summary>The animated badge.</summary>
        public AvatarBadge Badge => badge;

        /// <summary>Adds the animation layers to a badge (once).</summary>
        public static AvatarPulse Attach(AvatarBadge target)
        {
            var existing = target.GetComponent<AvatarPulse>(); if (existing != null) return existing;
            var pulse = target.gameObject.AddComponent<AvatarPulse>(); pulse.badge = target;
            var rect = (RectTransform)target.transform; pulse.size = rect.sizeDelta.x;
            pulse.glowImage = BrandArt.Image("PulseGlow", rect, "Glass/avatar_glow", Vector2.one * pulse.size * 1.7f, Vector2.zero);
            pulse.glow = pulse.glowImage.rectTransform; pulse.glow.SetAsFirstSibling();
            pulse.ring = BrandArt.Image("PulseRing", rect, "Glass/avatar_ring", Vector2.one * pulse.size * 1.26f, Vector2.zero).rectTransform;
            pulse.ring.SetSiblingIndex(1);
            for (int i = 0; i < Sparkles; i++)
            {
                pulse.sparkleImages[i] = BrandArt.Image("Sparkle" + i, rect, "Glass/avatar_glow", Vector2.one * 26, Vector2.zero);
                pulse.sparkles[i] = pulse.sparkleImages[i].rectTransform;
            }
            NeonKit.Isolate(target.gameObject);
            pulse.Tick(0);
            return pulse;
        }

        /// <summary>Explicit animation driver; allocation-free.</summary>
        public void Tick(float dt)
        {
            if (badge == null || !(dt >= 0) || float.IsInfinity(dt)) return;
            clock += dt;
            float t = clock, breathe = Mathf.Sin(t * 2.4f), shift = 0.5f + 0.5f * Mathf.Sin(t * 0.9f);
            ring.localRotation = Quaternion.Euler(0, 0, -t * 85f);
            glow.localScale = Vector3.one * (1f + 0.07f * breathe);
            Color g = Color.Lerp(GlowA, GlowB, shift); g.a = 0.42f + 0.22f * (0.5f + 0.5f * breathe); glowImage.color = g;
            var frame = badge.FrameImage.rectTransform;
            frame.localScale = Vector3.one * (1f + 0.035f * breathe);
            frame.localRotation = Quaternion.Euler(0, 0, 3.5f * Mathf.Sin(t * 1.3f));
            badge.AvatarImage.rectTransform.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(t * 2.4f + 1.2f));
            float radius = size * 0.6f;
            for (int i = 0; i < Sparkles; i++)
            {
                float a = t * 1.5f + i * (Mathf.PI * 2f / Sparkles);
                sparkles[i].anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                float twinkle = 0.5f + 0.5f * Mathf.Sin(t * 5f + i * 2.1f);
                sparkles[i].localScale = Vector3.one * (0.6f + 0.7f * twinkle);
                sparkleImages[i].color = new Color(1, 1, 1, 0.45f + 0.55f * twinkle);
            }
        }
    }
}
