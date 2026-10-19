namespace ArrowEscape.Core
{
    /// <summary>Checks whether repeatedly exiting free arrows can clear a board.</summary>
    public static class LevelSolvability
    {
        /// <summary>Checks a clone, leaving the original untouched; null is unsolvable.</summary>
        public static bool IsSolvable(Board board)
        {
            if (board == null) return false;
            Board remaining = board.Clone();
            while (!remaining.IsCleared())
            {
                Arrow free = null;
                foreach (Arrow arrow in remaining.Arrows)
                    if (remaining.CanExit(arrow)) { free = arrow; break; }
                if (free == null) return false;
                // Removal only empties a cell, so no other arrow becomes harder to free.
                remaining.Remove(free);
            }
            return true;
        }

        /// <summary>Counts arrows that can exit now; null has zero free arrows.</summary>
        public static int CountFreeArrows(Board board)
        {
            if (board == null) return 0;
            int count = 0;
            foreach (Arrow arrow in board.Arrows)
                if (board.CanExit(arrow)) count++;
            return count;
        }
    }
}
