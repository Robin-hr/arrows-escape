using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Preference controls; changing a preference never plays sound or haptics.</summary>
    public sealed class SettingsView : MonoBehaviour
    {
        private SettingsModel model;
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
        /// <summary>Creates the preference controls and wires stable callbacks.</summary>
        public void Build(Transform parent, SettingsModel value, Action<bool> sound, Action<bool> haptics, Action reset, Action back)
        {
            Root = UiFactory.ScreenRoot("Settings", parent, ViewTheme.PanelDim, out var safe);
            var column = UiFactory.Column(safe, 760, 1150);
            column.gameObject.AddComponent<UnityEngine.UI.Image>().color = ViewTheme.PanelCard;
            UiFactory.Title("Settings", column, 420);
            SoundButton = UiFactory.MenuButton("Sound: On", column, 220);
            HapticsButton = UiFactory.MenuButton("Haptics: On", column, 40);
            ResetButton = UiFactory.MenuButton("Reset Progress", column, -140);
            BackButton = UiFactory.MenuButton("Back", column, -320);
            UiFactory.Text("Version", column, "Version " + Application.version, 30, new Vector2(700, 60), new Vector2(0, -480));
            model = value; model.Changed += Refresh;
            SoundButton.onClick.AddListener(() => sound(!model.SoundEnabled));
            HapticsButton.onClick.AddListener(() => haptics(!model.HapticsEnabled));
            ResetButton.onClick.AddListener(() => reset()); BackButton.onClick.AddListener(() => back());
            Refresh();
        }
        /// <summary>Pulls current labels and toggle colors.</summary>
        public void Refresh()
        {
            UiFactory.SetToggle(SoundButton, model.SoundEnabled ? "Sound: On" : "Sound: Off", model.SoundEnabled);
            UiFactory.SetToggle(HapticsButton, model.HapticsEnabled ? "Haptics: On" : "Haptics: Off", model.HapticsEnabled);
        }
        private void OnDestroy() { if (model != null) model.Changed -= Refresh; }
    }
}
