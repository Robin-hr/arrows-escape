using System;
namespace ArrowEscape.Core
{
    /// <summary>Highest stars already rewarded for a stable level id.</summary>
    [Serializable] public sealed class LevelRewardRecord
    {
        /// <summary>Stable filename id.</summary>
        public string id;
        /// <summary>Previously rewarded stars, zero through three.</summary>
        public int stars;
    }
}
