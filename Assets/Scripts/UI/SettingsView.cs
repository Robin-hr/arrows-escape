using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Neon preference controls; changing a preference never plays sound or haptics.</summary>
    public sealed class SettingsView : MonoBehaviour
    {
        private SettingsModel model;
        private RectTransform backdrop;
        private UnityEngine.UI.Image soundIcon;
        /// <summary>Screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Sound preference toggle.</summary>
        public UnityEngine.UI.Button SoundButton { get; private set; }
        /// <summary>Haptics preference toggle.</summary>
        public UnityEngine.UI.Button HapticsButton { get; private set; }
        /// <summary>Confirmation request action.</summary>
        public UnityEngine.UI.Button ResetButton { get; private set; }
        /// <summary>Return action.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>Whether the full neon backdrop is shown (hidden when opened over a paused board).</summary>
        public bool BackdropVisible => backdrop != null && backdrop.gameObject.activeSelf;
        /// <summary>Creates the preference controls and wires stable callbacks.</summary>
        public void Build(Transform parent, SettingsModel value, Action<bool> sound, Action<bool> haptics, Action reset, Action back)
        {
            Root = UiFactory.ScreenRoot("Settings", parent, ViewTheme.PanelDim, out var safe);
            NeonKit.Backdrop(Root, 0.35f); backdrop = (RectTransform)Root.Find("NeonBackdrop");
            var column = UiFactory.Column(safe, 820, 1040);
            NeonKit.Panel(column.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Cyan);
            UiFactory.Title("Settings", column, 395);
            SoundButton = UiFactory.MenuButton("Sound: On", column, 225); soundIcon = NeonKit.WithIcon(SoundButton, "icon_sound_on", 72);
            HapticsButton = UiFactory.MenuButton("Haptics: On", column, 55); NeonKit.WithIcon(HapticsButton, "icon_haptics", 72);
            ResetButton = UiFactory.MenuButton("Reset Progress", column, -115); NeonKit.Style(ResetButton, NeonButtonStyle.Danger); NeonKit.WithIcon(ResetButton, "icon_reset", 72);
            BackButton = UiFactory.MenuButton("Back", column, -285); NeonKit.WithIcon(BackButton, "icon_back", 64);
            var version = UiFactory.Text("Version", column, "Version " + Application.version, 28, new Vector2(700, 50), new Vector2(0, -425));
            version.color = UiStyle.Muted;
            model = value; model.Changed += Refresh;
            SoundButton.onClick.AddListener(() => sound(!model.SoundEnabled));
            HapticsButton.onClick.AddListener(() => haptics(!model.HapticsEnabled));
            ResetButton.onClick.AddListener(() => reset()); BackButton.onClick.AddListener(() => back());
            Refresh();
        }
        /// <summary>Shows the neon page backdrop from the home page, or only a dim veil over a paused board.</summary>
        public void SetBackdrop(bool visible) { if (backdrop != null && backdrop.gameObject.activeSelf != visible) backdrop.gameObject.SetActive(visible); }
        /// <summary>Pulls current labels and toggle colors.</summary>
        public void Refresh()
        {
            UiFactory.SetToggle(SoundButton, model.SoundEnabled ? "Sound: On" : "Sound: Off", model.SoundEnabled);
            UiFactory.SetToggle(HapticsButton, model.HapticsEnabled ? "Haptics: On" : "Haptics: Off", model.HapticsEnabled);
            if (soundIcon != null) soundIcon.sprite = BrandArt.SpriteOf(model.SoundEnabled ? "icon_sound_on" : "icon_sound_off");
        }
        private void OnDestroy() { if (model != null) model.Changed -= Refresh; }
    }
}
