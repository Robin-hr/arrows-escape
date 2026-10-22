using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Dimmed neon pause overlay with no animation or global clock changes.</summary>
    public sealed class PauseView : MonoBehaviour
    {
        /// <summary>Full-screen blocking root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Resume action.</summary>
        public UnityEngine.UI.Button ResumeButton { get; private set; }
        /// <summary>Restart action.</summary>
        public UnityEngine.UI.Button RestartButton { get; private set; }
        /// <summary>Settings action.</summary>
        public UnityEngine.UI.Button SettingsButton { get; private set; }
        /// <summary>Discard and leave action.</summary>
        public UnityEngine.UI.Button MainMenuButton { get; private set; }
        /// <summary>Creates a responsive pause card.</summary>
        public void Build(Transform parent)
        {
            Root = UiFactory.ScreenRoot("Pause", parent, ViewTheme.PanelDim, out var safe);
            var column = UiFactory.Column(safe, 820, 1040);
            NeonKit.Panel(column.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Cyan);
            UiFactory.Title("Paused", column, 395);
            ResumeButton = UiFactory.MenuButton("Resume", column, 225); NeonKit.Style(ResumeButton, NeonButtonStyle.Primary); NeonKit.WithIcon(ResumeButton, "icon_play", 76);
            RestartButton = UiFactory.MenuButton("Restart", column, 55); NeonKit.WithIcon(RestartButton, "icon_restart", 72);
            SettingsButton = UiFactory.MenuButton("Settings", column, -115); NeonKit.WithIcon(SettingsButton, "icon_settings", 72);
            MainMenuButton = UiFactory.MenuButton("Main Menu", column, -285); NeonKit.WithIcon(MainMenuButton, "icon_home", 72);
        }
    }
}
