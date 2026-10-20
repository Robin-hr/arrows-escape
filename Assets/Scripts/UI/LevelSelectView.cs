using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Reusable scroll-grid tiles; large catalogs remain navigable by wheel or touch drag.</summary>
    public sealed class LevelSelectView : MonoBehaviour
    {
        private readonly List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();
        private IReadOnlyList<LevelTile> tiles;
        private Action<int> start;
        private UnityEngine.UI.GridLayoutGroup grid;
        private Vector2 lastSize = new Vector2(-1, -1);
        /// <summary>Screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Back action.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>Vertical grid scrolling.</summary>
        public UnityEngine.UI.ScrollRect Scroll { get; private set; }
        /// <summary>Current tile buttons, in level order.</summary>
        public IReadOnlyList<UnityEngine.UI.Button> TileButtons => buttons;
        /// <summary>Builds the header, viewport, mask, and grid.</summary>
        public void Build(Transform parent, Action<int> onStart, Action onBack)
        {
            start = onStart;
            Root = UiFactory.ScreenRoot("LevelSelect", parent, ViewTheme.Background, out var safe);
            var header = UiFactory.Rect("Header", safe, new Vector2(900, 140), Vector2.zero);
            header.anchorMin = header.anchorMax = header.pivot = new Vector2(0.5f, 1);
            header.anchoredPosition = new Vector2(0, -20);
            header.gameObject.AddComponent<MenuFit>().Configure(header, safe, new Vector2(1000, 170));
            UiFactory.Title("Levels", header, 0);
            BackButton = UiFactory.Button("Back", header, new Vector2(-310, 0));
            ((RectTransform)BackButton.transform).sizeDelta = new Vector2(220, 120);
            BackButton.onClick.AddListener(() => onBack());
            Scroll = UiFactory.ScrollGrid(safe); grid = Scroll.content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        }
        /// <summary>Reuses all tile objects when count is unchanged; callbacks validate the current tile state.</summary>
        public void Refresh(IReadOnlyList<LevelTile> value)
        {
            tiles = value ?? Array.Empty<LevelTile>();
            while (buttons.Count > tiles.Count)
            { int i = buttons.Count - 1; DestroyImmediate(buttons[i].gameObject); buttons.RemoveAt(i); }
            while (buttons.Count < tiles.Count)
            {
                int index = buttons.Count;
                var button = UiFactory.Button("Level " + (index + 1), Scroll.content, Vector2.zero);
                button.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 32;
                button.onClick.AddListener(() => { if (index < tiles.Count && tiles[index].State != LevelTileState.Locked) start(tiles[index].Index); });
                buttons.Add(button);
            }
            for (int i = 0; i < tiles.Count; i++)
            {
                LevelTile tile = tiles[i]; var button = buttons[i];
                button.interactable = tile.State != LevelTileState.Locked;
                string detail = tile.State == LevelTileState.Locked ? "Locked [X]" : tile.State == LevelTileState.Completed ?
                    (tile.BestMistakes == 0 && tile.BestUndos == 0 ? "[OK] Flawless" : "[OK] " + tile.BestMistakes + " mistakes") : "Play";
                button.GetComponentInChildren<UnityEngine.UI.Text>().text = tile.Number + "\n" + detail;
                var colors = button.colors; colors.normalColor = tile.IsResume ? ViewTheme.ButtonPressed : ViewTheme.ButtonNormal; button.colors = colors;
            }
            Canvas.ForceUpdateCanvases(); UpdateLayout(); RevealResume();
        }
        /// <summary>Scrolls the continue tile into the viewport after layout is calculated.</summary>
        public void RevealResume()
        {
            if (tiles == null || !Root.gameObject.activeInHierarchy) return;
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < tiles.Count; i++) if (tiles[i].IsResume)
            {
                float height = Scroll.content.rect.height - Scroll.viewport.rect.height;
                float row = i / grid.constraintCount;
                float y = row * (grid.cellSize.y + grid.spacing.y);
                Scroll.verticalNormalizedPosition = height > 0 ? 1 - Mathf.Clamp01(y / height) : 1;
                break;
            }
        }
        private void LateUpdate() => UpdateLayout();
        private void UpdateLayout()
        {
            if (Scroll == null) return;
            Vector2 size = Scroll.viewport.rect.size; if (size == lastSize || size.x <= 0) return; lastSize = size;
            int columns = size.x >= 1340 && size.x > size.y ? 6 : 4;
            grid.constraintCount = columns;
            float cell = Mathf.Max(200, (size.x - 20 - (columns - 1) * 20) / columns);
            grid.cellSize = new Vector2(cell, cell);
        }
    }
}
