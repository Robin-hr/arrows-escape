namespace ArrowEscape.Core
{
    /// <summary>Finds the nearest arrow without changing the board.</summary>
    public static class TapResolver
    {
        /// <summary>Resolves within the inclusive radius; exact distance ties use the lower id.</summary>
        public static Arrow Resolve(Board board, float cellX, float cellY, float selectRadius)
        {
            if (board == null || float.IsNaN(selectRadius) || float.IsInfinity(selectRadius) || selectRadius <= 0)
                return null;
            Arrow nearest = null;
            double bestDistance = (double)selectRadius * selectRadius;
            foreach (Arrow arrow in board.Arrows)
            {
                // Double precision avoids overflow for points far outside the board.
                double dx = (double)cellX - arrow.X;
                double dy = (double)cellY - arrow.Y;
                double distance = dx * dx + dy * dy;
                if (distance < bestDistance || (distance == bestDistance &&
                    (nearest == null || arrow.Id < nearest.Id)))
                {
                    nearest = arrow;
                    bestDistance = distance;
                }
            }
            return nearest;
        }
    }
}
