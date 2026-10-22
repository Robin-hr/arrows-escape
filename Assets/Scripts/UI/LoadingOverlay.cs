using System;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Full-screen loading layer above every other canvas. It only covers the screen and blocks input for a fixed time;
    /// the game state underneath is already final, so nothing depends on the animation finishing.
    /// </summary>
    public sealed class LoadingOverlay : MonoBehaviour
    {
        /// <summary>App start, before the home page.</summary>
        public const float StartupSeconds = 3f;
        /// <summary>Every new level start.</summary>
        public const float LevelSeconds = 1f;
        /// <summary>Opening the profile page.</summary>
        public const float ProfileSeconds = 2f;
        private const float FadeIn = 0.12f, FadeOut = 0.25f;

        private CanvasGroup group;
        private UnityEngine.UI.RawImage background;
        private UnityEngine.UI.Image dim;
        private UnityEngine.UI.RawImage logo;
        private RectTransform loaderRect, captionRect, loaderPanel;
        private const float PanelScale = 1.13f;
        private string[] captionFrames = new string[4];
        private int captionFrame = -1;
        private float elapsed, duration;
        private Action onDone;
        private Vector2 lastSize;

        /// <summary>Overlay root (its own canvas, sorted above HUD and menus).</summary>
        public RectTransform Root { get; private set; }
        /// <summary>The snake-arrow puzzle element.</summary>
        public SnakeLoaderGraphic Loader { get; private set; }
        /// <summary>Caption under the loader.</summary>
        public UnityEngine.UI.Text Caption { get; private set; }
        /// <summary>True while covering the screen (including its fade-out).</summary>
        public bool IsVisible { get; private set; }
        /// <summary>Which loading screen is (or was last) shown.</summary>
        public LoadingKind Kind { get; private set; }
        /// <summary>Seconds left before the overlay starts fading out.</summary>
        public float Remaining => IsVisible ? Mathf.Max(0, duration - elapsed) : 0;

        /// <summary>Fixed display time for each kind.</summary>
        public static float DurationFor(LoadingKind kind) =>
            kind == LoadingKind.Startup ? StartupSeconds : kind == LoadingKind.Level ? LevelSeconds : ProfileSeconds;

        /// <summary>Loader edge length in canvas reference units: large at startup, medium for levels, small for profile.</summary>
        public static float LoaderSizeFor(LoadingKind kind) =>
            kind == LoadingKind.Startup ? 520f : kind == LoadingKind.Level ? 400f : 280f;

        /// <summary>Builds the hidden overlay under a canvas.</summary>
        public void Build(Transform parent)
        {
            if (Root != null) return;
            Root = UiFactory.Rect("LoadingOverlay", parent, Vector2.zero, Vector2.zero); UiFactory.Stretch(Root);
            var canvas = Root.gameObject.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 300;
            Root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            group = Root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = true; group.interactable = true;

            dim = UiFactory.Rect("Dim", Root, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
            UiFactory.Stretch(dim.rectTransform); dim.color = new Color(0.02f, 0.024f, 0.1f, 0.94f); dim.raycastTarget = true;
            background = BrandArt.Backdrop("StartupBackground", Root, "loading_background"); background.raycastTarget = true;
            dim.transform.SetAsFirstSibling();

            logo = UiFactory.Rect("Logo", Root, new Vector2(1000, 491), Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            logo.texture = BrandArt.Texture("arrows_escape_logo"); logo.raycastTarget = false;
            logo.rectTransform.anchorMin = logo.rectTransform.anchorMax = new Vector2(0.5f, 0.69f);
            if (logo.texture == null) logo.color = new Color(1, 1, 1, 0);

            // The puzzle sits on the same chamfered neon panel as every other card in the game.
            loaderPanel = UiFactory.Rect("LoaderPanel", Root, new Vector2(580, 580), Vector2.zero);
            NeonKit.Panel(loaderPanel.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Cyan).raycastTarget = false;
            loaderPanel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            loaderRect = UiFactory.Rect("SnakeLoader", Root, new Vector2(520, 520), Vector2.zero);
            Loader = loaderRect.gameObject.AddComponent<SnakeLoaderGraphic>(); Loader.DrawTile = false;
            Caption = BrandArt.Text("Caption", Root, "LOADING", 40, new Vector2(900, 70), Vector2.zero, BrandArt.Lavender);
            captionRect = Caption.rectTransform;
            Root.gameObject.SetActive(false);
        }

        /// <summary>Shows a loading screen for its fixed duration, then fades out and calls onDone once.</summary>
        public void Show(LoadingKind kind, string caption = null, Action done = null)
        {
            if (Root == null) return;
            Kind = kind; duration = DurationFor(kind); elapsed = 0; onDone = done; IsVisible = true;
            bool startup = kind == LoadingKind.Startup;
            background.gameObject.SetActive(startup); logo.gameObject.SetActive(startup); dim.gameObject.SetActive(!startup);
            float size = LoaderSizeFor(kind);
            loaderRect.sizeDelta = new Vector2(size, size);
            loaderRect.anchorMin = loaderRect.anchorMax = new Vector2(0.5f, startup ? 0.33f : 0.52f);
            loaderPanel.sizeDelta = loaderRect.sizeDelta * PanelScale; loaderPanel.anchorMin = loaderPanel.anchorMax = loaderRect.anchorMin;
            captionRect.anchorMin = captionRect.anchorMax = new Vector2(0.5f, startup ? 0.15f : 0.52f);
            captionRect.anchoredPosition = startup ? Vector2.zero : new Vector2(0, -size * 0.5f - 70);
            Caption.fontSize = startup ? 40 : kind == LoadingKind.Level ? 46 : 34;
            SetCaption(string.IsNullOrEmpty(caption) ? "LOADING" : caption, animatedDots: startup || kind == LoadingKind.Profile);
            Loader.Speed = startup ? 1.4f : kind == LoadingKind.Level ? 2.4f : 1.8f;
            Loader.Restart(startup ? 0f : 0.3f);
            group.alpha = startup ? 1f : 0f;
            lastSize = Vector2.zero;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            Layout();
        }

        /// <summary>Removes the overlay at once (no callback).</summary>
        public void HideImmediate()
        {
            IsVisible = false; onDone = null;
            if (Root != null) Root.gameObject.SetActive(false);
        }

        /// <summary>Advances fades, the caption dots and the timer; the puzzle animates itself.</summary>
        public void Tick(float dt)
        {
            if (!IsVisible || !(dt > 0) || float.IsInfinity(dt)) return;
            elapsed += dt;
            Layout();
            if (Kind != LoadingKind.Startup && elapsed < FadeIn) group.alpha = elapsed / FadeIn;
            else if (elapsed < duration) group.alpha = 1f;
            else
            {
                float t = (elapsed - duration) / FadeOut;
                group.alpha = Mathf.Clamp01(1f - t);
                if (t >= 1f) { Finish(); return; }
            }
            int frame = (int)(elapsed * 3f) % 4;
            if (frame != captionFrame) { captionFrame = frame; Caption.text = captionFrames[frame]; }
        }

        private void Finish()
        {
            IsVisible = false; Root.gameObject.SetActive(false);
            Action done = onDone; onDone = null; done?.Invoke();
        }

        private void SetCaption(string text, bool animatedDots)
        {
            for (int i = 0; i < 4; i++) captionFrames[i] = animatedDots ? text + new string('.', i) : text;
            captionFrame = -1; Caption.text = captionFrames[0];
        }

        // Keeps the logo and loader inside narrow or landscape screens; only runs when the size changes.
        private void Layout()
        {
            Vector2 size = Root.rect.size;
            if (size == lastSize || size.x <= 0 || size.y <= 0) return;
            lastSize = size;
            BrandArt.Cover(background);
            float logoWidth = Mathf.Min(1000f, size.x * 0.9f, size.y * 0.42f * 2.04f);
            logo.rectTransform.sizeDelta = new Vector2(logoWidth, logoWidth / 2.04f);
            float loader = Mathf.Min(LoaderSizeFor(Kind), size.x * 0.8f, size.y * (Kind == LoadingKind.Startup ? 0.3f : 0.45f));
            loaderRect.sizeDelta = new Vector2(loader, loader); loaderPanel.sizeDelta = loaderRect.sizeDelta * PanelScale;
            if (Kind != LoadingKind.Startup) captionRect.anchoredPosition = new Vector2(0, -loader * 0.5f - 70);
        }

        // Clamp: a level load or app resume can produce one long frame; the screen must still be seen for its full time.
        private void Update() { if (Application.isPlaying) Tick(Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep)); }

        /// <summary>Longest frame step counted toward a loading screen's time.</summary>
        public const float MaxFrameStep = 0.1f;
    }
}
