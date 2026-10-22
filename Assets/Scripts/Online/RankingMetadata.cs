using System;
namespace ArrowEscape.Online
{
    /// <summary>Small JSON payload stored with each ranking score (well under the 1 KB limit).</summary>
    [Serializable]
    public sealed class RankingMetadata
    {
        /// <summary>Equipped avatar id.</summary>
        public string avatar;
        /// <summary>Equipped frame id.</summary>
        public string frame;
        /// <summary>Total trophies.</summary>
        public int trophies;
        /// <summary>Total best stars.</summary>
        public int stars;
    }
}
