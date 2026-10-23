namespace ArrowEscape.Core
{
    /// <summary>Integer economy tuning; no purchases use real money.</summary>
    public static class EconomyConfig
    {
        /// <summary>Base reward for a level's first inventory-recorded clear.</summary>
        public const int FirstClearCoins = 10;
        /// <summary>Reward for each previously unrewarded star.</summary>
        public const int CoinsPerNewStar = 10;
        /// <summary>Base reward for each genuine replay clear.</summary>
        public const int ReplayClearCoins = 5;
        /// <summary>Wallet ceiling.</summary>
        public const int MaxCoins = 9999999;
        /// <summary>Developer grant for five rapid taps on the coin counter.</summary>
        public const int DeveloperGrant = 50000;
        /// <summary>Hints every new player starts with.</summary>
        public const int StartingHints = 3;
        /// <summary>Undos every new player starts with.</summary>
        public const int StartingUndos = 3;
        /// <summary>Coin price of one extra hint or undo.</summary>
        public const int BoostPrice = 300;
        /// <summary>Most hints or undos a player can hold.</summary>
        public const int MaxBoosts = 999;
        /// <summary>Hearts per attempt (every level and the daily puzzle).</summary>
        public const int DailyHearts = 3;
        /// <summary>Coin price of one extra heart when all hearts are lost.</summary>
        public const int HeartPrice = 500;
        /// <summary>Set false before a store release to disable the developer options (coin grant, open locked levels).</summary>
        public const bool DeveloperGrantEnabled = true;
    }
}
