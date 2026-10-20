using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Dimmed pause overlay with no animation or global clock changes.</summary>
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
            var column = UiFactory.Column(safe, 760, 1120);
            var image = column.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = ViewTheme.PanelCard;
            UiFactory.Title("Paused", column, 420);
            ResumeButton = UiFactory.MenuButton("Resume", column, 220);
            RestartButton = UiFactory.MenuButton("Restart", column, 40);
            SettingsButton = UiFactory.MenuButton("Settings", column, -140);
            MainMenuButton = UiFactory.MenuButton("Main Menu", column, -320);
        }
    }
}
