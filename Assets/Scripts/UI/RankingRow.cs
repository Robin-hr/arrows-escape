using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>One neon ranking row: rank, avatar, name, league emblem, trophies and stars.</summary>
    public sealed class RankingRow : MonoBehaviour
    {
        private static readonly Color First = new Color32(255, 210, 74, 255), Second = new Color32(214, 224, 244, 255), Third = new Color32(226, 150, 92, 255);
        private UnityEngine.UI.Image panel;
        /// <summary>"#1".</summary>
        public UnityEngine.UI.Text RankText { get; private set; }
        /// <summary>Player name.</summary>
        public UnityEngine.UI.Text NameText { get; private set; }
        /// <summary>League name.</summary>
        public UnityEngine.UI.Text LeagueText { get; private set; }
        /// <summary>Trophy total.</summary>
        public UnityEngine.UI.Text TrophiesText { get; private set; }
        /// <summary>Star total.</summary>
        public UnityEngine.UI.Text StarsText { get; private set; }
        /// <summary>League shield.</summary>
        public UnityEngine.UI.Image Emblem { get; private set; }
        /// <summary>Avatar in its frame.</summary>
        public AvatarBadge Avatar { get; private set; }
        /// <summary>The entry shown.</summary>
        public LeaderboardEntry Entry { get; private set; }

        /// <summary>Creates a row; <paramref name="large"/> is the highlighted local-player card.</summary>
        public static RankingRow Create(string name, Transform parent, Vector2 size, Vector2 position, bool large)
        {
            var image = UiFactory.Rect(name, parent, size, position).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false; NeonKit.Panel(image, large ? NeonKit.Gold : NeonKit.Cyan);
            var row = image.gameObject.AddComponent<RankingRow>(); row.panel = image;
            float w = size.x, h = size.y, left = -w * 0.5f;
            row.RankText = BrandArt.Text("Rank", image.transform, "-", large ? 40 : 34, new Vector2(130, h * 0.6f), new Vector2(left + 80, 0), Color.white);
            float badge = h * 0.78f;
            row.Avatar = AvatarBadge.Create("Avatar", image.transform, badge, new Vector2(left + 150 + badge * 0.5f, 0));
            float textX = left + 165 + badge;
            row.NameText = BrandArt.Text("Name", image.transform, "", large ? 32 : 28, new Vector2(330, 46), new Vector2(textX + 165, h * 0.17f), Color.white);
            row.NameText.alignment = TextAnchor.MiddleLeft;
            row.Emblem = NeonKit.Icon("Emblem", image.transform, LeagueArt.Emblem(League.Bronze), 40, new Vector2(textX + 20, -h * 0.2f));
            row.LeagueText = BrandArt.Text("League", image.transform, "", 20, new Vector2(280, 36), new Vector2(textX + 185, -h * 0.2f), Color.white);
            row.LeagueText.alignment = TextAnchor.MiddleLeft;
            float right = w * 0.5f;
            NeonKit.Icon("TrophyIcon", image.transform, "icon_trophy", 44, new Vector2(right - 215, h * 0.17f));
            row.TrophiesText = BrandArt.Text("Trophies", image.transform, "0", 30, new Vector2(160, 44), new Vector2(right - 110, h * 0.17f), new Color32(255, 214, 92, 255));
            row.TrophiesText.alignment = TextAnchor.MiddleLeft;
            NeonKit.Icon("StarIcon", image.transform, "icon_star", 36, new Vector2(right - 215, -h * 0.2f));
            row.StarsText = BrandArt.Text("Stars", image.transform, "0", 24, new Vector2(160, 40), new Vector2(right - 110, -h * 0.2f), BrandArt.Lavender);
            row.StarsText.alignment = TextAnchor.MiddleLeft;
            return row;
        }

        /// <summary>Shows an entry; top three ranks are tinted gold, silver and bronze.</summary>
        public void Set(LeaderboardEntry entry)
        {
            if (entry == null) return;
            Entry = entry;
            RankText.text = entry.Rank > 0 ? "#" + LeagueArt.Count(entry.Rank) : "-";
            RankText.color = entry.Rank == 1 ? First : entry.Rank == 2 ? Second : entry.Rank == 3 ? Third : Color.white;
            NameText.text = entry.IsMe && entry.Name != "YOU" ? entry.Name + " (YOU)" : entry.Name;
            NameText.color = entry.IsMe ? (Color)new Color32(125, 255, 176, 255) : Color.white;
            League league = entry.League;
            Emblem.sprite = BrandArt.SpriteOf(LeagueArt.Emblem(league)); Emblem.color = Emblem.sprite != null ? Color.white : new Color(1, 1, 1, 0);
            LeagueText.text = LeagueArt.Name(league); LeagueText.color = LeagueArt.Tint(league);
            TrophiesText.text = LeagueArt.Count(entry.Trophies);
            StarsText.text = LeagueArt.Count(entry.Stars);
            Avatar.Set(string.IsNullOrEmpty(entry.AvatarId) ? Inventory.DefaultAvatarId : entry.AvatarId, entry.FrameId);
        }
    }
}
