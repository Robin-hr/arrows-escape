using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Final attempt summary and replay/next actions, above the HUD.</summary>
    public sealed class LevelCompletePanel : MonoBehaviour
    {
        private RectTransform overlay, card;
        private RectTransform safeArea;
        private Vector2 lastSize;
        /// <summary>Whether the full-screen modal overlay is visible.</summary>
        public bool IsVisible => overlay != null && overlay.gameObject.activeSelf;
        /// <summary>Mistake summary.</summary>
        public UnityEngine.UI.Text MistakesText { get; private set; }
        /// <summary>Undo summary.</summary>
        public UnityEngine.UI.Text UndosText { get; private set; }
        /// <summary>Perfect-attempt label, visible only with zero mistakes and undos.</summary>
        public UnityEngine.UI.Text FlawlessText { get; private set; }
        /// <summary>The paired best attempt.</summary>
        public UnityEngine.UI.Text BestText { get; private set; }
        /// <summary>First-clear or strict-improvement label.</summary>
        public UnityEngine.UI.Text BestNoticeText { get; private set; }
        /// <summary>Final-list completion message.</summary>
        public UnityEngine.UI.Text AllCompleteText { get; private set; }
        /// <summary>Replay button.</summary>
        public UnityEngine.UI.Button ReplayButton { get; private set; }
        /// <summary>Next level button.</summary>
        public UnityEngine.UI.Button NextButton { get; private set; }

        /// <summary>Level selection action.</summary>
        public UnityEngine.UI.Button LevelsButton { get; private set; }
        /// <summary>Raised when level selection is requested from a visible completion.</summary>
        public event System.Action LevelsRequested;
        /// <summary>Builds the initially hidden overlay once.</summary>
        public void Build(RectTransform canvas, RectTransform safe)
        {
            if (overlay != null) return;
            safeArea = safe;
            overlay = UiFactory.Panel("LevelCompleteOverlay", canvas, ViewTheme.PanelDim, true);
            card = UiFactory.Panel("Card", overlay, ViewTheme.PanelCard);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(900, 900);
            card.anchoredPosition = Vector2.zero;
            UiFactory.Text("Title", card, "Level Complete", 60, new Vector2(850, 90), new Vector2(0, 350)).color = ViewTheme.UiAccent;
            MistakesText = UiFactory.Text("Mistakes", card, "", 44, new Vector2(850, 65), new Vector2(0, 240));
            UndosText = UiFactory.Text("Undos", card, "", 44, new Vector2(850, 65), new Vector2(0, 170));
            FlawlessText = UiFactory.Text("Flawless", card, "Flawless!", 44, new Vector2(850, 65), new Vector2(0, 90));
            BestText = UiFactory.Text("Best", card, "", 38, new Vector2(850, 65), new Vector2(0, 10));
            BestNoticeText = UiFactory.Text("BestNotice", card, "", 42, new Vector2(850, 65), new Vector2(0, -65));
            AllCompleteText = UiFactory.Text("AllComplete", card, "All levels complete!", 42, new Vector2(850, 65), new Vector2(0, -145));
            ReplayButton = UiFactory.Button("Replay", card, new Vector2(-270, -300));
            NextButton = UiFactory.Button("Next Level", card, new Vector2(0, -300));
            LevelsButton = UiFactory.Button("Levels", card, new Vector2(270, -300));
            foreach (var button in new[] { ReplayButton, NextButton, LevelsButton })
                ((RectTransform)button.transform).sizeDelta = new Vector2(240, 120);
            LevelsButton.onClick.AddListener(() => { if (IsVisible) LevelsRequested?.Invoke(); });
            BestText.gameObject.SetActive(false);
            BestNoticeText.gameObject.SetActive(false);
            AllCompleteText.gameObject.SetActive(false);
            Hide();
        }

        /// <summary>Shows final counters, with flawless only for an unaided perfect attempt.</summary>
        public void Show(int mistakes, int undos)
        {
            BestText.gameObject.SetActive(false);
            BestNoticeText.gameObject.SetActive(false);
            AllCompleteText.gameObject.SetActive(false);
            NextButton.gameObject.SetActive(true);
            NextButton.interactable = true;
            MistakesText.text = $"Mistakes {mistakes}";
            UndosText.text = $"Undos {undos}";
            FlawlessText.gameObject.SetActive(mistakes == 0 && undos == 0);
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
            UpdateLayout();
        }
        /// <summary>Shows persisted best and flow state without changing this attempt's flawless rules.</summary>
        public void Show(int mistakes, int undos, CompletionResult result, bool isLastLevel, bool allCompleted, bool canGoNext)
        {
            Show(mistakes, undos);
            if (result != null)
            {
                BestText.text = $"Best: {result.BestMistakes} mistakes" + (result.BestUndos > 0 ? $", {result.BestUndos} undos" : "");
                BestText.gameObject.SetActive(true);
                BestNoticeText.text = result.IsFirstClear ? "First clear!" : result.IsNewBest ? "New best!" : "";
                BestNoticeText.gameObject.SetActive(result.IsFirstClear || result.IsNewBest);
            }
            NextButton.gameObject.SetActive(!isLastLevel);
            NextButton.interactable = canGoNext;
            AllCompleteText.gameObject.SetActive(isLastLevel && allCompleted);
        }
        /// <summary>Hides the completion overlay.</summary>
        public void Hide() { if (overlay != null) overlay.gameObject.SetActive(false); }
        private void LateUpdate() { if (IsVisible) UpdateLayout(); }
        private void UpdateLayout()
        {
            Vector2 size = safeArea.rect.size;
            if (lastSize == size) return;
            lastSize = size;
            card.localScale = Vector3.one * Mathf.Min(1, size.x * 0.9f / 900, size.y * 0.9f / 900);
            card.position = safeArea.TransformPoint(safeArea.rect.center);
        }
    }
}
