using System;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Single modal confirmation above every other screen.</summary>
    public sealed class ConfirmDialog : MonoBehaviour
    {
        private Action confirm;
        private UnityEngine.UI.Text title, message;
        /// <summary>Blocking screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Whether a confirmation is currently open.</summary>
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>Cancel action.</summary>
        public UnityEngine.UI.Button CancelButton { get; private set; }
        /// <summary>Confirm action.</summary>
        public UnityEngine.UI.Button ConfirmButton { get; private set; }
        /// <summary>Builds an initially hidden responsive dialog.</summary>
        public void Build(Transform parent)
        {
            Root = UiFactory.ScreenRoot("ConfirmDialog", parent, ViewTheme.PanelDim, out var safe);
            var card = UiFactory.Column(safe, 900, 650);
            card.gameObject.AddComponent<UnityEngine.UI.Image>().color = ViewTheme.PanelCard;
            title = UiFactory.Title("", card, 220);
            message = UiFactory.Text("Message", card, "", 40, new Vector2(820, 210), new Vector2(0, 40));
            CancelButton = UiFactory.Button("Cancel", card, new Vector2(-170, -200));
            ConfirmButton = UiFactory.Button("Confirm", card, new Vector2(170, -200));
            CancelButton.onClick.AddListener(Hide); ConfirmButton.onClick.AddListener(Confirm);
            Hide();
        }
        /// <summary>Shows one confirmation; the callback is consumed before invocation.</summary>
        public void Show(string heading, string text, string confirmLabel, Action onConfirm)
        {
            title.text = heading; message.text = text; confirm = onConfirm;
            ConfirmButton.GetComponentInChildren<UnityEngine.UI.Text>().text = confirmLabel;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }
        /// <summary>Closes without confirming.</summary>
        public void Hide() { confirm = null; if (Root != null) Root.gameObject.SetActive(false); }
        private void Confirm() { if (!IsOpen) return; Action action = confirm; Hide(); action?.Invoke(); }
    }
}
