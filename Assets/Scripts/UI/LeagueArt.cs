using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Names, colours and emblem art for the trophy leagues.</summary>
    public static class LeagueArt
    {
        /// <summary>Display name, e.g. "GOLD".</summary>
        public static string Name(League league) => league.ToString().ToUpperInvariant();
        /// <summary>Brand art path of the shield emblem.</summary>
        public static string Emblem(League league) => "Leagues/league_" + league.ToString().ToLowerInvariant();
        /// <summary>Accent colour used for the league name and progress bar.</summary>
        public static Color Tint(League league)
        {
            switch (league)
            {
                case League.Bronze: return new Color32(222, 146, 92, 255);
                case League.Silver: return new Color32(206, 216, 240, 255);
                case League.Gold: return new Color32(255, 204, 72, 255);
                case League.Platinum: return new Color32(130, 236, 214, 255);
                case League.Diamond: return new Color32(86, 196, 255, 255);
                default: return new Color32(214, 140, 255, 255);
            }
        }
        /// <summary>"1,234" with invariant grouping.</summary>
        public static string Count(int value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
