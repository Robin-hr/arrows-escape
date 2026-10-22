using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>A short neon message that fades out by itself (unscaled time, no coroutines).</summary>
    public sealed class ToastView : MonoBehaviour
    {
        private const float Duration = 2.2f, Fade = 0.35f;
        private CanvasGroup group;
        private float remaining;
        /// <summary>Toast rectangle.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Message text.</summary>
        public UnityEngine.UI.Text Message { get; private set; }
        /// <summary>Whether the toast is on screen.</summary>
        public bool IsVisible => Root != null && Root.gameObject.activeSelf;

        /// <summary>Builds the hidden toast near the top of the screen.</summary>
        public void Build(Transform parent)
        {
            var pill = BrandArt.Image("Toast", parent, "ui_pill", new Vector2(820, 170), Vector2.zero, NeonKit.PillBorder);
            Root = pill.rectTransform; Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.78f);
            if (pill.sprite == null) pill.color = new Color(0.06f, 0.04f, 0.23f, 0.95f);
            group = pill.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            NeonKit.Icon("Coin", Root, "icon_coin", 92, new Vector2(-320, 0));
            Message = BrandArt.Text("Message", Root, "", 38, new Vector2(620, 140), new Vector2(50, 0), new Color32(255, 220, 110, 255));
            Root.gameObject.SetActive(false);
        }

        /// <summary>Shows a message for about two seconds.</summary>
        public void Show(string text)
        {
            if (Root == null) return;
            Message.text = text; remaining = Duration; group.alpha = 1;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }

        /// <summary>Advances the fade; hides when done.</summary>
        public void Tick(float dt)
        {
            if (!IsVisible || !(dt > 0)) return;
            remaining -= dt;
            group.alpha = Mathf.Clamp01(remaining / Fade);
            if (remaining <= 0) Root.gameObject.SetActive(false);
        }

        private void Update() { if (Application.isPlaying) Tick(Mathf.Min(Time.unscaledDeltaTime, 0.1f)); }
    }
}
