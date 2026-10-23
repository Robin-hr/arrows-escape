namespace ArrowEscape.Core
{
    /// <summary>One daily calendar reward.</summary>
    public sealed class DailyReward
    {
        /// <summary>Kind of reward.</summary>
        public DailyRewardKind Kind { get; }
        /// <summary>Coins or boost count.</summary>
        public int Amount { get; }
        /// <summary>Shop item id for Item rewards.</summary>
        public string ItemId { get; }
        /// <summary>Creates a reward.</summary>
        public DailyReward(DailyRewardKind kind, int amount, string itemId = null) { Kind = kind; Amount = amount; ItemId = itemId; }
        /// <summary>Short label, e.g. "300 COINS", "1 HINT", item name.</summary>
        public string Label
        {
            get
            {
                switch (Kind)
                {
                    case DailyRewardKind.Coins: return Amount + " COINS";
                    case DailyRewardKind.Hint: return Amount + (Amount == 1 ? " HINT" : " HINTS");
                    case DailyRewardKind.Undo: return Amount + (Amount == 1 ? " UNDO" : " UNDOS");
                    default: return ShopCatalog.Find(ItemId)?.Name?.ToUpperInvariant() ?? "MYSTERY";
                }
            }
        }
    }
}
