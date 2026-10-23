using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Smooth screen transition (Step 14J): whenever a screen root is shown in Play mode its content fades in and
    /// settles from 96 % scale, and a translucent dim backdrop fades up with it, over <see cref="Duration"/> seconds.
    /// Opaque page backgrounds appear at once so no black flash shows between screens. Edit Mode stays instant.
    /// </summary>
    public sealed class ScreenAppear : MonoBehaviour
    {
        /// <summary>Transition length in seconds.</summary>
        public const float Duration = 0.24f;
        private RectTransform content;
        private CanvasGroup group;
        private UnityEngine.UI.Image backdrop;
        private float backdropAlpha = 1, elapsed = Duration;

        /// <summary>True while the transition runs.</summary>
        public bool IsPlaying => elapsed < Duration;
        /// <summary>Content alpha group.</summary>
        public CanvasGroup Group => group;

        /// <summary>Attaches to a screen root and its safe-area content.</summary>
        public static ScreenAppear Attach(RectTransform root, RectTransform safe)
        {
            var appear = root.gameObject.AddComponent<ScreenAppear>();
            appear.content = safe; appear.group = safe.gameObject.AddComponent<CanvasGroup>();
            appear.backdrop = root.GetComponent<UnityEngine.UI.Image>();
            if (appear.backdrop != null) appear.backdropAlpha = appear.backdrop.color.a;
            return appear;
        }

        private void OnEnable() { if (Application.isPlaying) Play(); else Finish(); }
        private void OnDisable() => Finish();

        /// <summary>Starts the transition from the beginning.</summary>
        public void Play() { elapsed = 0; Apply(0); }

        /// <summary>Jumps to the settled state.</summary>
        public void Finish() { elapsed = Duration; Apply(1); }

        /// <summary>Explicit driver; allocation-free.</summary>
        public void Tick(float dt)
        {
            if (!IsPlaying || !(dt >= 0) || float.IsInfinity(dt)) return;
            elapsed = Mathf.Min(Duration, elapsed + dt);
            Apply(Motion.Out(elapsed / Duration));
        }

        private void Apply(float k)
        {
            if (group == null) return;
            group.alpha = k;
            content.localScale = Vector3.one * Mathf.LerpUnclamped(0.96f, 1f, k);
            if (backdrop != null && backdropAlpha < 0.95f) { Color c = backdrop.color; c.a = backdropAlpha * k; backdrop.color = c; }
        }

        // Dt is clamped so a long first frame (scene load, GC) still shows a few frames of motion.
        private void Update() { if (Application.isPlaying) Tick(Mathf.Min(Time.unscaledDeltaTime, 1f / 30f)); }
    }
}
