namespace ArrowEscape.Tests
{
    /// <summary>Fixed resources for pre-pack flow fixtures; the runtime catalog is never replaced globally.</summary>
    public static class OriginalTestCatalog
    {
        /// <summary>A fresh ordered set of the three original resources.</summary>
        public static string[] LegacyPaths => new[]{"TestLevels/legacy_level_001","TestLevels/legacy_level_002","TestLevels/legacy_level_003"};
        /// <summary>Stable ids used by legacy flow expectations.</summary>
        public static string[] Paths => new[]{"Levels/level_001","Levels/level_002","Levels/level_003"};
    }
}
