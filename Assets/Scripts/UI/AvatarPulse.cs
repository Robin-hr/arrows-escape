using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Life for the player's profile badge on every page that shows it, for premium cosmetics only:
    /// a premium avatar breathes inside a pulsing glow with orbiting sparkles; a premium frame sways and breathes
    /// with a rotating neon comet ring behind it. Ordinary avatars and frames stay still. It reads the badge's
    /// current ids every frame, so equipping a different avatar or frame switches the animation at once.
    /// </summary>
    public sealed class AvatarPulse : MonoBehaviour
    {
        private const int Sparkles = 3;
        private static readonly Color GlowA = new Color32(56, 225, 255, 150), GlowB = new Color32(255, 95, 210, 150);
        private AvatarBadge badge;
        private RectTransform ring, glow;
        private UnityEngine.UI.Image glowImage, ringImage;
        private readonly RectTransform[] sparkles = new RectTransform[Sparkles];
        private readonly UnityEngine.UI.Image[] sparkleImages = new UnityEngine.UI.Image[Sparkles];
        private float size, clock;
        private string avatarId, frameId;

        /// <summary>Seconds animated so far.</summary>
        public float Clock => clock;
        /// <summary>The rotating ring behind the frame.</summary>
        public RectTransform Ring => ring;
        /// <summary>The animated badge.</summary>
        public AvatarBadge Badge => badge;
        /// <summary>True when the equipped avatar is premium (it animates).</summary>
        public bool AvatarAnimated { get; private set; }
        /// <summary>True when the equipped frame is premium (it animates).</summary>
        public bool FrameAnimated { get; private set; }

        /// <summary>Adds the animation layers to a badge (once).</summary>
        public static AvatarPulse Attach(AvatarBadge target)
        {
            var existing = target.GetComponent<AvatarPulse>(); if (existing != null) return existing;
            var pulse = target.gameObject.AddComponent<AvatarPulse>(); pulse.badge = target;
            var rect = (RectTransform)target.transform; pulse.size = rect.sizeDelta.x;
            pulse.glowImage = BrandArt.Image("PulseGlow", rect, "Glass/avatar_glow", Vector2.one * pulse.size * 1.7f, Vector2.zero);
            pulse.glow = pulse.glowImage.rectTransform; pulse.glow.SetAsFirstSibling();
            pulse.ringImage = BrandArt.Image("PulseRing", rect, "Glass/avatar_ring", Vector2.one * pulse.size * 1.26f, Vector2.zero);
            pulse.ring = pulse.ringImage.rectTransform; pulse.ring.SetSiblingIndex(1);
            for (int i = 0; i < Sparkles; i++)
            {
                pulse.sparkleImages[i] = BrandArt.Image("Sparkle" + i, rect, "Glass/avatar_glow", Vector2.one * pulse.size * 0.19f, Vector2.zero);
                pulse.sparkles[i] = pulse.sparkleImages[i].rectTransform;
            }
            NeonKit.Isolate(target.gameObject);
            pulse.Tick(0);
            return pulse;
        }

        private void Refresh()
        {
            if (avatarId == badge.AvatarId && frameId == badge.FrameId) return;
            avatarId = badge.AvatarId; frameId = badge.FrameId;
            AvatarAnimated = PremiumCosmetics.IsPremiumAvatar(avatarId); FrameAnimated = PremiumCosmetics.IsPremiumFrame(frameId);
            glow.gameObject.SetActive(AvatarAnimated); ring.gameObject.SetActive(FrameAnimated);
            for (int i = 0; i < Sparkles; i++) sparkles[i].gameObject.SetActive(AvatarAnimated);
            if (!AvatarAnimated) badge.AvatarImage.rectTransform.localScale = Vector3.one;
            if (!FrameAnimated) { badge.FrameImage.rectTransform.localScale = Vector3.one; badge.FrameImage.rectTransform.localRotation = Quaternion.identity; }
        }

        /// <summary>Explicit animation driver; allocation-free.</summary>
        public void Tick(float dt)
        {
            if (badge == null || !(dt >= 0) || float.IsInfinity(dt)) return;
            Refresh();
            if (!AvatarAnimated && !FrameAnimated) return;
            clock += dt;
            float t = clock, breathe = Mathf.Sin(t * 2.4f), shift = 0.5f + 0.5f * Mathf.Sin(t * 0.9f);
            if (FrameAnimated)
            {
                ring.localRotation = Quaternion.Euler(0, 0, -t * 85f);
                var frame = badge.FrameImage.rectTransform;
                frame.localScale = Vector3.one * (1f + 0.035f * breathe);
                frame.localRotation = Quaternion.Euler(0, 0, 3.5f * Mathf.Sin(t * 1.3f));
            }
            if (AvatarAnimated)
            {
                glow.localScale = Vector3.one * (1f + 0.07f * breathe);
                Color g = Color.Lerp(GlowA, GlowB, shift); g.a = 0.42f + 0.22f * (0.5f + 0.5f * breathe); glowImage.color = g;
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

        private void Update() { if (Application.isPlaying) Tick(Time.unscaledDeltaTime); }
    }
}
