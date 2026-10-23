using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>One pooled level stop on the Journey road: glass sphere (gold hexagon on milestones), number, stars or lock.</summary>
    public sealed class JourneyNode : MonoBehaviour
    {
        /// <summary>Normal node diameter.</summary>
        public const float Size = 150, MilestoneSize = 184;
        private UnityEngine.UI.Image[] stars;
        private UnityEngine.UI.Image lockIcon;

        /// <summary>Level index shown (-1 while unused).</summary>
        public int Index { get; private set; } = -1;
        /// <summary>Displayed state.</summary>
        public LevelTileState State { get; private set; }
        /// <summary>Tap target.</summary>
        public UnityEngine.UI.Button Button { get; private set; }
        /// <summary>Sphere art.</summary>
        public UnityEngine.UI.Image Ball { get; private set; }
        /// <summary>Level number.</summary>
        public UnityEngine.UI.Text Number { get; private set; }
        /// <summary>Clicks even while locked (developer triple tap).</summary>
        public LockedTileTap LockedTap { get; private set; }
        /// <summary>Node rectangle.</summary>
        public RectTransform Rect { get; private set; }

        /// <summary>Creates an unused node under the map content.</summary>
        public static JourneyNode Create(Transform parent, int slot)
        {
            var rect = UiFactory.Rect("Node" + slot, parent, new Vector2(Size, Size), Vector2.zero);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0); rect.pivot = new Vector2(0.5f, 0.5f);
            var node = rect.gameObject.AddComponent<JourneyNode>(); node.Rect = rect;
            node.Ball = BrandArt.Image("Ball", rect, "Journey/jn_locked", new Vector2(Size, Size), Vector2.zero);
            node.Ball.raycastTarget = true; UiFactory.Stretch(node.Ball.rectTransform);
            node.Button = node.Ball.gameObject.AddComponent<UnityEngine.UI.Button>(); node.Button.targetGraphic = node.Ball;
            var colors = node.Button.colors; colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white; colors.disabledColor = Color.white; colors.pressedColor = new Color(0.8f, 0.82f, 0.95f); node.Button.colors = colors;
            node.Ball.gameObject.AddComponent<ButtonFeel>();
            node.LockedTap = node.Ball.gameObject.AddComponent<LockedTileTap>();
            node.Number = BrandArt.Text("Number", rect, "1", 40, new Vector2(140, 60), new Vector2(0, 2), Color.white);
            node.lockIcon = BrandArt.Image("Lock", rect, "icon_lock", new Vector2(40, 40), new Vector2(0, -34));
            node.stars = new UnityEngine.UI.Image[3];
            for (int i = 0; i < 3; i++)
            {
                node.stars[i] = BrandArt.Image("Star" + i, rect, "icon_star", new Vector2(40, 40), new Vector2((i - 1) * 40, -Size * 0.5f - (i == 1 ? 14 : 6)));
            }
            rect.gameObject.SetActive(false);
            return node;
        }

        /// <summary>Shows a level; returns quickly when nothing changed.</summary>
        public void Bind(LevelTile tile, bool current)
        {
            if (tile == null) { Index = -1; if (gameObject.activeSelf) gameObject.SetActive(false); return; }
            Index = tile.Index; State = tile.State;
            bool milestone = JourneyLayout.IsMilestone(tile.Index);
            float size = milestone ? MilestoneSize : Size;
            Rect.sizeDelta = new Vector2(size, size);
            Rect.anchoredPosition = new Vector2(JourneyLayout.X(tile.Index), JourneyLayout.Y(tile.Index));
            string art = milestone ? "Journey/jn_boss" : tile.State == LevelTileState.Completed ? "Journey/jn_done" : tile.State == LevelTileState.Unlocked ? "Journey/jn_open" : "Journey/jn_locked";
            Ball.sprite = BrandArt.SpriteOf(art);
            Ball.color = milestone && tile.State == LevelTileState.Locked ? new Color(0.55f, 0.52f, 0.7f, 1) : Color.white;
            Button.interactable = tile.State != LevelTileState.Locked;
            Number.text = tile.Number.ToString();
            Number.fontSize = tile.Number >= 100 ? 34 : 40;
            Number.color = tile.State == LevelTileState.Locked ? new Color32(176, 170, 220, 255) : Color.white;
            Number.rectTransform.anchoredPosition = new Vector2(0, tile.State == LevelTileState.Locked ? 14 : 2);
            lockIcon.gameObject.SetActive(tile.State == LevelTileState.Locked);
            for (int i = 0; i < 3; i++)
            {
                bool on = tile.State == LevelTileState.Completed;
                if (stars[i].gameObject.activeSelf != on) stars[i].gameObject.SetActive(on);
                if (on) stars[i].color = i < tile.BestStars ? Color.white : new Color(0.25f, 0.22f, 0.4f, 0.9f);
                stars[i].rectTransform.anchoredPosition = new Vector2((i - 1) * 40, -size * 0.5f + 2 - (i == 1 ? 10 : 2));
            }
            transform.localScale = current ? Vector3.one * 1.08f : Vector3.one;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }
    }
}
