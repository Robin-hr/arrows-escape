using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Displays progress above the board and actions below it, inside the safe area.</summary>
    public sealed class HudView : MonoBehaviour
    {
        private RectTransform top, bottom;
        private RectTransform undoRect, hintRect, restartRect;
        private Vector2 lastSize;
        private string lastName;
        private int lastLevelIndex = -2, lastLevelCount = -1;
        private int lastMoves = -1, lastTotal = -1, lastMistakes = -1;
        /// <summary>Level name label.</summary>
        public UnityEngine.UI.Text LevelText { get; private set; }
        /// <summary>One-based level position, separate from the JSON level name.</summary>
        public UnityEngine.UI.Text LevelPositionText { get; private set; }
        /// <summary>Current exit progress label.</summary>
        public UnityEngine.UI.Text MovesText { get; private set; }
        /// <summary>Blocked tap count label.</summary>
        public UnityEngine.UI.Text MistakesText { get; private set; }
        /// <summary>Pause action in the safe-area top bar.</summary>
        public UnityEngine.UI.Button PauseButton { get; private set; }
        /// <summary>Unlimited undo control.</summary>
        public UnityEngine.UI.Button UndoButton { get; private set; }
        /// <summary>Free suggestion control between Undo and Restart.</summary>
        public UnityEngine.UI.Button HintButton { get; private set; }
        /// <summary>Restart attempt control.</summary>
        public UnityEngine.UI.Button RestartButton { get; private set; }

        /// <summary>Creates the HUD once under a safe-area rectangle.</summary>
        public void Build(RectTransform safeArea)
        {
            if (top != null) return;
            top = UiFactory.Rect("TopBar", safeArea, new Vector2(900, 150), Vector2.zero);
            top.anchorMin = top.anchorMax = top.pivot = new Vector2(0.5f, 1);
            bottom = UiFactory.Rect("BottomBar", safeArea, new Vector2(1000, 120), Vector2.zero);
            bottom.anchorMin = bottom.anchorMax = bottom.pivot = new Vector2(0.5f, 0);
            LevelText = UiFactory.Text("LevelName", top, "", ViewTheme.HudFontSize, new Vector2(320, 65), new Vector2(-260, 40));
            LevelPositionText = UiFactory.Text("LevelPosition", top, "", ViewTheme.HudFontSize, new Vector2(300, 65), new Vector2(100, 40));
            MovesText = UiFactory.Text("Moves", top, "", ViewTheme.HudFontSize, new Vector2(440, 65), new Vector2(-225, -30));
            MistakesText = UiFactory.Text("Mistakes", top, "", ViewTheme.HudFontSize, new Vector2(300, 65), new Vector2(140, -30));
            float step = ViewTheme.ButtonWidth + ViewTheme.UiSpacing;
            UndoButton = UiFactory.Button("Undo", bottom, new Vector2(-step, 0));
            HintButton = UiFactory.Button("Hint", bottom, Vector2.zero);
            RestartButton = UiFactory.Button("Restart", bottom, new Vector2(step, 0));
            undoRect = (RectTransform)UndoButton.transform;
            hintRect = (RectTransform)HintButton.transform;
            restartRect = (RectTransform)RestartButton.transform;
            PauseButton = UiFactory.Button("Pause", top, new Vector2(380, 0));
            ((RectTransform)PauseButton.transform).sizeDelta = new Vector2(120, 120);
            PauseButton.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 28;
            SetUndoState(false);
            SetHintState(false);
            UpdateLayout();
        }

        /// <summary>Updates only labels whose values changed.</summary>
        public void SetState(string levelName, int moves, int total, int mistakes, bool canUndo)
        {
            if (LevelText == null) return;
            if (lastName != levelName) { lastName = levelName; LevelText.text = levelName; }
            if (lastMoves != moves || lastTotal != total)
            { lastMoves = moves; lastTotal = total; MovesText.text = $"Moves {moves} / {total}"; }
            if (lastMistakes != mistakes) { lastMistakes = mistakes; MistakesText.text = $"Mistakes {mistakes}"; }
            SetUndoState(canUndo);
        }

        /// <summary>Sets undo availability and the standard disabled button tint.</summary>
        public void SetUndoState(bool canUndo)
        {
            if (UndoButton != null) UndoButton.interactable = canUndo;
        }

        /// <summary>Sets hint availability without changing the board.</summary>
        public void SetHintState(bool canHint) { if (HintButton != null) HintButton.interactable = canHint; }

        /// <summary>Shows one-based progress through the resource list, or hides it before initialization.</summary>
        public void SetLevelPosition(int index, int count)
        {
            if (LevelPositionText == null || (lastLevelIndex == index && lastLevelCount == count)) return;
            lastLevelIndex = index; lastLevelCount = count;
            LevelPositionText.text = index >= 0 && count > 0 ? $"Level {index + 1} / {count}" : "";
        }

        private void LateUpdate() => UpdateLayout();
        private void UpdateLayout()
        {
            if (top == null) return;
            Vector2 size = ((RectTransform)top.parent).rect.size;
            if (size == lastSize || size.x <= 0 || size.y <= 0) return;
            lastSize = size;
            // Keep every visible pixel within the camera's reserved top/bottom bands.
            float topScale = UiFit.ComputeScale(900, 150, size.x * 0.9f, size.y * 0.13f);
            float minimumBar = 3 * 200 + 2 * ViewTheme.UiSpacing;
            float barWidth = Mathf.Max(minimumBar, Mathf.Min(1000, size.x * 0.94f));
            bottom.sizeDelta = new Vector2(barWidth, ViewTheme.ButtonHeight);
            float buttonScale = UiFit.ComputeScale(3 * ViewTheme.ButtonWidth, ViewTheme.ButtonHeight,
                barWidth - 2 * ViewTheme.UiSpacing, ViewTheme.ButtonHeight);
            float width = Mathf.Max(200, ViewTheme.ButtonWidth * buttonScale);
            undoRect.sizeDelta = hintRect.sizeDelta = restartRect.sizeDelta = new Vector2(width, ViewTheme.ButtonHeight);
            undoRect.anchoredPosition = new Vector2(-width - ViewTheme.UiSpacing, 0);
            hintRect.anchoredPosition = Vector2.zero;
            restartRect.anchoredPosition = new Vector2(width + ViewTheme.UiSpacing, 0);
            float bottomScale = UiFit.ComputeScale(barWidth, ViewTheme.ButtonHeight, size.x * 0.9f, size.y * 0.14f);
            top.localScale = Vector3.one * topScale;
            bottom.localScale = Vector3.one * bottomScale;
            top.anchoredPosition = new Vector2(0, -size.y * 0.01f);
            bottom.anchoredPosition = new Vector2(0, size.y * 0.01f);
        }
    }
}
