using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Runtime main menu with a responsive fixed-size column.</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        /// <summary>The screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Start or continue action.</summary>
        public UnityEngine.UI.Button PlayButton { get; private set; }
        /// <summary>Level selection action.</summary>
        public UnityEngine.UI.Button LevelsButton { get; private set; }
        /// <summary>Preferences action.</summary>
        public UnityEngine.UI.Button SettingsButton { get; private set; }
        /// <summary>Platform quit action.</summary>
        public UnityEngine.UI.Button QuitButton { get; private set; }
        /// <summary>Completion total label.</summary>
        public UnityEngine.UI.Text CompletedText { get; private set; }
        /// <summary>Continue target subtitle.</summary>
        public UnityEngine.UI.Text ContinueText { get; private set; }
        /// <summary>Builds the menu once.</summary>
        public void Build(Transform parent)
        {
            Root = UiFactory.ScreenRoot("MainMenu", parent, ViewTheme.Background, out var safe);
            var column = UiFactory.Column(safe, 760, 1200);
            UiFactory.Title("Arrow Escape", column, 480);
            CompletedText = UiFactory.Text("Completed", column, "", 40, new Vector2(700, 70), new Vector2(0, 365));
            PlayButton = UiFactory.MenuButton("Play", column, 220);
            ContinueText = UiFactory.Text("ContinueLevel", column, "", 32, new Vector2(700, 60), new Vector2(0, 115));
            LevelsButton = UiFactory.MenuButton("Levels", column, -40);
            SettingsButton = UiFactory.MenuButton("Settings", column, -220);
            QuitButton = UiFactory.MenuButton("Quit", column, -400);
#if UNITY_IOS
            QuitButton.gameObject.SetActive(false);
#endif
        }
        /// <summary>Refreshes menu text only when entering or progress changes.</summary>
        public void Refresh(bool hasProgress, int completed, int total, int continueIndex)
        {
            PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text = hasProgress ? "Continue" : "Play";
            PlayButton.interactable = total > 0;
            ContinueText.text = hasProgress && total > 0 ? "Level " + (continueIndex + 1) : "";
            CompletedText.text = $"Completed {completed} / {total}";
        }
    }
}
