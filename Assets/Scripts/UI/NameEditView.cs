using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Profile name editor: a text field (opens the phone keyboard), a live blackletter preview, SAVE and CANCEL.</summary>
    public sealed class NameEditView : MonoBehaviour
    {
        private Action<string> save;
        /// <summary>Overlay root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>The name field.</summary>
        public UnityEngine.UI.InputField Field { get; private set; }
        /// <summary>Preview in the profile-name font.</summary>
        public UnityEngine.UI.Text Preview { get; private set; }
        /// <summary>Saves the name.</summary>
        public UnityEngine.UI.Button SaveButton { get; private set; }
        /// <summary>Closes without saving.</summary>
        public UnityEngine.UI.Button CancelButton { get; private set; }

        /// <summary>Builds the hidden editor.</summary>
        public void Build(Transform parent, Action<string> onSave)
        {
            save = onSave;
            Root = UiFactory.ScreenRoot("NameEdit", parent, ViewTheme.PanelDim, out var safe);
            var card = UiFactory.Column(safe, 900, 760);
            NeonKit.Panel(card.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Cyan);
            BrandArt.Text("Title", card, "YOUR NAME", 54, new Vector2(820, 90), new Vector2(0, 285), Color.white);
            Preview = BrandArt.NameText("Preview", card, 72, new Vector2(820, 110), new Vector2(0, 165));

            var box = UiFactory.Rect("NameField", card, new Vector2(760, 120), new Vector2(0, 30)).gameObject.AddComponent<UnityEngine.UI.Image>();
            GlassKit.Apply(box, GlassKit.Kind.Pill, new Color32(70, 60, 200, 255)); box.raycastTarget = true;
            var text = UiFactory.Text("Text", box.transform, "", 44, new Vector2(680, 90), new Vector2(0, 4));
            text.font = BrandArt.Display; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.supportRichText = false;
            var hint = UiFactory.Text("Placeholder", box.transform, "TYPE A NAME", 36, new Vector2(680, 90), new Vector2(0, 4));
            hint.font = BrandArt.Display; hint.color = new Color(1, 1, 1, 0.4f); hint.alignment = TextAnchor.MiddleCenter;
            Field = box.gameObject.AddComponent<UnityEngine.UI.InputField>();
            Field.textComponent = text; Field.placeholder = hint; Field.targetGraphic = box;
            Field.characterLimit = PlayerNameRules.MaxLength; Field.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
            Field.contentType = UnityEngine.UI.InputField.ContentType.Standard; Field.caretColor = Color.white; Field.customCaretColor = true;
            Field.selectionColor = new Color(0.22f, 0.88f, 1f, 0.45f);
            Field.onValueChanged.AddListener(v => Preview.text = PlayerNameRules.Display(v));
            BrandArt.Text("Rule", card, "Up to 16 letters, numbers, spaces and . - _", 22, new Vector2(820, 40), new Vector2(0, -60), BrandArt.Lavender);

            SaveButton = UiFactory.Button("SAVE", card, new Vector2(195, -220), NeonButtonStyle.Primary);
            ((RectTransform)SaveButton.transform).sizeDelta = new Vector2(360, 130);
            SaveButton.onClick.AddListener(Save);
            CancelButton = UiFactory.Button("CANCEL", card, new Vector2(-195, -220));
            ((RectTransform)CancelButton.transform).sizeDelta = new Vector2(360, 130);
            CancelButton.onClick.AddListener(Hide);
            Root.gameObject.SetActive(false);
        }

        /// <summary>Opens with the current name.</summary>
        public void Show(string current)
        {
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            Field.text = PlayerNameRules.Clean(current); Preview.text = PlayerNameRules.Display(current);
            if (Application.isPlaying) Field.ActivateInputField();
        }

        /// <summary>Saves the cleaned name and closes.</summary>
        public void Save() { string name = PlayerNameRules.Clean(Field.text); Hide(); save?.Invoke(name); }

        /// <summary>Closes the editor.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }
    }
}
