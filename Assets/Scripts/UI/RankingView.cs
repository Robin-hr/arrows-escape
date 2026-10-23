using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Neon world ranking: the local player's card plus the global top list (trophies first, stars break ties).</summary>
    public sealed class RankingView : MonoBehaviour
    {
        /// <summary>Rows shown from the top of the world ranking.</summary>
        public const int TopCount = 50;
        private const float RowHeight = 128, RowGap = 12;
        private readonly List<RankingRow> rows = new List<RankingRow>();
        private Action back;
        private Func<PlayerSnapshot> snapshot;
        private RectTransform content;
        private int request;

        /// <summary>Screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while the page is open.</summary>
        public bool IsVisible => Root != null && Root.gameObject.activeSelf;
        /// <summary>Closes the page.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>Connecting / offline / error notice.</summary>
        public UnityEngine.UI.Text StatusText { get; private set; }
        /// <summary>The local player's row.</summary>
        public RankingRow MeRow { get; private set; }
        /// <summary>Pooled top rows (inactive beyond the fetched count).</summary>
        public IReadOnlyList<RankingRow> Rows => rows;
        /// <summary>Number of top rows currently shown.</summary>
        public int ShownCount { get; private set; }
        /// <summary>The last page shown.</summary>
        public LeaderboardPage Page { get; private set; }
        /// <summary>Scrolling list.</summary>
        public UnityEngine.UI.ScrollRect Scroll { get; private set; }

        /// <summary>Builds the hidden page.</summary>
        public void Build(Transform parent, Action onBack, Func<PlayerSnapshot> me)
        {
            if (Root != null) return;
            back = onBack; snapshot = me;
            Root = UiFactory.ScreenRoot("Ranking", parent, BrandArt.Space, out var safe);
            NeonKit.Backdrop(Root, 0.6f);

            var header = UiFactory.Rect("Header", safe, new Vector2(960, 150), Vector2.zero);
            header.anchorMin = header.anchorMax = header.pivot = new Vector2(0.5f, 1); header.anchoredPosition = new Vector2(0, -16);
            header.gameObject.AddComponent<MenuFit>().Configure(header, safe, new Vector2(1000, 170));
            BackButton = NeonKit.IconButton("Back", header, "icon_back", new Vector2(-420, -62), 112, 56);
            BackButton.onClick.AddListener(() => back?.Invoke());
            NeonKit.Icon("TitleIcon", header, "icon_ranking", 78, new Vector2(-250, -62));
            BrandArt.Text("Title", header, "WORLD RANKING", 54, new Vector2(620, 100), new Vector2(60, -62), Color.white);
            for (int i = 0; i < header.childCount; i++) { var child = (RectTransform)header.GetChild(i); child.anchorMin = child.anchorMax = new Vector2(0.5f, 1); }

            var area = UiFactory.Rect("RankingContent", safe, Vector2.zero, Vector2.zero); UiFactory.Stretch(area); area.offsetMax = new Vector2(0, -180);
            var column = UiFactory.Column(area, 960, 1640);
            BrandArt.Text("YouLabel", column, "YOUR RANK", 26, new Vector2(900, 40), new Vector2(0, 780), BrandArt.Cyan);
            MeRow = RankingRow.Create("Me", column, new Vector2(940, 150), new Vector2(0, 680), true);
            AvatarPulse.Attach(MeRow.Avatar);
            StatusText = BrandArt.Text("Status", column, "", 24, new Vector2(900, 70), new Vector2(0, 560), BrandArt.Lavender);
            StatusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            BrandArt.Text("TopLabel", column, "TOP " + TopCount, 26, new Vector2(900, 40), new Vector2(0, 500), BrandArt.Cyan);

            var view = UiFactory.Rect("ScrollView", column, new Vector2(960, 1260), new Vector2(0, -150));
            Scroll = view.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport = UiFactory.Rect("Viewport", view, Vector2.zero, Vector2.zero); UiFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var hit = viewport.gameObject.AddComponent<UnityEngine.UI.Image>(); hit.color = new Color(0, 0, 0, 0);
            content = UiFactory.Rect("Content", viewport, Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;
            Scroll.viewport = viewport; Scroll.content = content; Scroll.horizontal = false; Scroll.vertical = true;
            Scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic; Scroll.scrollSensitivity = 50;
            for (int i = 0; i < TopCount; i++)
            {
                var row = RankingRow.Create("Row" + (i + 1), content, new Vector2(920, RowHeight), Vector2.zero, false);
                var rect = (RectTransform)row.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1);
                rect.anchoredPosition = new Vector2(0, -i * (RowHeight + RowGap) - 6);
                row.gameObject.SetActive(false); rows.Add(row);
            }
            Root.gameObject.SetActive(false);
        }

        /// <summary>Opens the page and requests a fresh ranking; shows the local row immediately.</summary>
        public void Show()
        {
            if (Root == null) return;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            PlayerSnapshot me = snapshot?.Invoke();
            if (me != null) MeRow.Set(new LeaderboardEntry(0, "", "YOU", me.Trophies, me.Stars, me.AvatarId, me.FrameId, true));
            StatusText.text = "CONNECTING TO WORLD RANKING..."; StatusText.gameObject.SetActive(true);
            Load(me);
        }

        private async void Load(PlayerSnapshot me)
        {
            int id = ++request;
            LeaderboardPage page;
            try
            {
                var service = LeaderboardRegistry.Service;
                if (me != null) await service.SubmitAsync(me);
                page = await service.FetchAsync(me, TopCount);
            }
            catch (Exception e) { page = new LeaderboardPage(null, null, LeaderboardStatus.Error, "Ranking unavailable: " + e.Message); }
            if (id != request || this == null || Root == null) return;
            Apply(page);
        }

        /// <summary>Fills the rows from a fetched page.</summary>
        public void Apply(LeaderboardPage page)
        {
            if (page == null) return;
            Page = page;
            if (page.Me != null) MeRow.Set(page.Me);
            ShownCount = Mathf.Min(page.Top.Count, rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                bool on = i < ShownCount; if (rows[i].gameObject.activeSelf != on) rows[i].gameObject.SetActive(on);
                if (on) rows[i].Set(page.Top[i]);
            }
            content.sizeDelta = new Vector2(0, ShownCount * (RowHeight + RowGap) + 12);
            StatusText.text = page.Status == LeaderboardStatus.Online ? "" : page.Message;
            StatusText.gameObject.SetActive(page.Status != LeaderboardStatus.Online);
        }

        /// <summary>Hides the page.</summary>
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); request++; }
    }
}
