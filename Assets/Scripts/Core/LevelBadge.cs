namespace ArrowEscape.Core
{
    /// <summary>Step 15: the difficulty label shown above the board (Normal, Hard, Super Hard).</summary>
    public enum LevelBadgeKind { Normal, Hard, SuperHard }

    /// <summary>Step 15: the difficulty rhythm shared by the level generator and the HUD badge.</summary>
    public static class LevelBadge
    {
        /// <summary>Catalog levels: a Super Hard peak every tenth level from 70, Hard levels in between, a breather after each peak.</summary>
        public static LevelBadgeKind ForCatalog(int number)
        {
            if (number <= 60) return number >= 20 && number % 10 == 0 ? LevelBadgeKind.Hard : LevelBadgeKind.Normal;
            int p = (number - 61) % 10;
            return p == 9 ? LevelBadgeKind.SuperHard : p == 4 || p == 7 ? LevelBadgeKind.Hard : LevelBadgeKind.Normal;
        }

        /// <summary>Journey levels: every tenth is Super Hard, every fifth Hard.</summary>
        public static LevelBadgeKind ForJourney(int number) => number % 10 == 0 ? LevelBadgeKind.SuperHard : number % 5 == 0 ? LevelBadgeKind.Hard : LevelBadgeKind.Normal;

        /// <summary>Upper-case label text.</summary>
        public static string Label(LevelBadgeKind kind) => kind == LevelBadgeKind.SuperHard ? "SUPER HARD" : kind == LevelBadgeKind.Hard ? "HARD" : "NORMAL";

        /// <summary>Multiplier on the size of the board for each label.</summary>
        public static int SizePermille(LevelBadgeKind kind) => kind == LevelBadgeKind.SuperHard ? 1350 : kind == LevelBadgeKind.Hard ? 1150 : 1000;
    }
}
