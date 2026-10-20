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
    }
}
