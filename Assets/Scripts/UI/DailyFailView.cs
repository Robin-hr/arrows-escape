using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Shown when all three hearts are lost: restart from the beginning or watch an ad to continue.</summary>
    public sealed class DailyFailView : MonoBehaviour
    {
        /// <summary>Overlay root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>Restarts the puzzle with three hearts.</summary>
        public UnityEngine.UI.Button RestartButton { get; private set; }
        /// <summary>Watches an ad and continues with one heart.</summary>
        public UnityEngine.UI.Button AdButton { get; private set; }
        /// <summary>Leaves to the home page.</summary>
        public UnityEngine.UI.Button HomeButton { get; private set; }
        /// <summary>Builds the hidden panel.</summary>
        public void Build(Transform parent)
        {
            Root = UiFactory.ScreenRoot("DailyFail", parent, ViewTheme.PanelDim, out var safe);
            var card = UiFactory.Column(safe, 880, 960);
            NeonKit.Panel(card.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Magenta);
            BrandArt.Text("Title", card, "OUT OF HEARTS", 60, new Vector2(820, 100), new Vector2(0, 370), Color.white);
            for (int i = 0; i < 3; i++) BrandArt.Image("Heart" + i, card, "Daily/icon_heart_empty", new Vector2(110, 110), new Vector2((i - 1) * 130, 220));
            BrandArt.Text("Note", card, "Three mistakes used up your hearts.\nRestart the puzzle, or watch an ad to keep going.", 30, new Vector2(800, 120), new Vector2(0, 70), BrandArt.Lavender);
            AdButton = UiFactory.Button("WATCH AD · +1 HEART", card, new Vector2(0, -90), NeonButtonStyle.Primary);
            ((RectTransform)AdButton.transform).sizeDelta = new Vector2(700, 140); NeonKit.WithIcon(AdButton, "Daily/icon_ad", 70);
            RestartButton = UiFactory.Button("RESTART PUZZLE", card, new Vector2(0, -250));
            ((RectTransform)RestartButton.transform).sizeDelta = new Vector2(700, 140); NeonKit.WithIcon(RestartButton, "icon_restart", 62);
            HomeButton = UiFactory.Button("HOME", card, new Vector2(0, -390));
            ((RectTransform)HomeButton.transform).sizeDelta = new Vector2(420, 110); NeonKit.WithIcon(HomeButton, "icon_home", 54);
            Root.gameObject.SetActive(false);
        }
        /// <summary>Opens the panel.</summary>
        public void Show() { Root.gameObject.SetActive(true); Root.SetAsLastSibling(); }
        /// <summary>Closes the panel.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); }
    }
}
