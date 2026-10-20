namespace ArrowEscape.Core
{
    /// <summary>Pure inset geometry for an arrow and a blocker strictly ahead of it.</summary>
    public static class GuideGeometry
    {
        /// <summary>Inset matching a sprite that fills seventy percent of a cell.</summary>
        public const float DefaultInset = 0.35f;
        /// <summary>Rejects invalid pairs or insets without throwing.</summary>
        public static bool TryComputeSegment(Arrow tapped, Arrow blocker, float inset, out GuideSegment segment)
        {
            segment = default;
            if (tapped == null || blocker == null || !(inset > 0 && inset < 0.5f) ||
                tapped.Direction < Direction.Up || tapped.Direction > Direction.Right) return false;
            int dx = tapped.Direction.GetDx(), dy = tapped.Direction.GetDy();
            long offsetX = (long)blocker.X - tapped.X, offsetY = (long)blocker.Y - tapped.Y;
            long distance = offsetX * dx + offsetY * dy;
            if (distance < 1 || (dx == 0 ? offsetX != 0 : offsetY != 0)) return false;
            segment = new GuideSegment(tapped.X + dx * inset, tapped.Y + dy * inset,
                blocker.X - dx * inset, blocker.Y - dy * inset, distance - 2 * inset, dx != 0);
            return true;
        }
    }
}
