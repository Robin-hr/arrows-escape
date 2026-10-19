using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>A grid of arrows with no rendering or input dependencies.</summary>
    public sealed class Board
    {
        private readonly List<Arrow> arrows = new List<Arrow>();

        /// <summary>The number of columns.</summary>
        public int Width { get; }
        /// <summary>The number of rows.</summary>
        public int Height { get; }
        /// <summary>Current arrows in insertion order; callers cannot edit this list.</summary>
        public IReadOnlyList<Arrow> Arrows { get; }

        /// <summary>Creates an empty board with positive dimensions.</summary>
        public Board(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            Arrows = arrows.AsReadOnly();
        }

        /// <summary>Adds an arrow if the cell, identifier, and direction are valid.</summary>
        public bool TryAddArrow(int id, int x, int y, Direction direction, out Arrow arrow)
        {
            arrow = null;
            if (!InBounds(x, y) || GetArrowAt(x, y) != null || GetArrowById(id) != null)
                return false;
            if (direction < Direction.Up || direction > Direction.Right) return false;
            arrow = new Arrow(id, x, y, direction);
            arrows.Add(arrow);
            return true;
        }

        /// <summary>Checks whether a cell lies inside the board.</summary>
        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        /// <summary>Returns the arrow at a cell, or null if empty or outside the board.</summary>
        public Arrow GetArrowAt(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return arrows.Find(arrow => arrow.X == x && arrow.Y == y);
        }

        /// <summary>Returns the arrow with this identifier, or null.</summary>
        public Arrow GetArrowById(int id) => arrows.Find(arrow => arrow.Id == id);

        /// <summary>Checks whether a board member has a clear path to the edge.</summary>
        public bool CanExit(Arrow arrow) => Contains(arrow) && GetBlocker(arrow) == null;

        /// <summary>Returns the nearest blocker, or null for a clear path or nonmember.</summary>
        public Arrow GetBlocker(Arrow arrow)
        {
            if (!Contains(arrow)) return null;
            int dx = arrow.Direction.GetDx();
            int dy = arrow.Direction.GetDy();
            // Start one cell ahead: the arrow never blocks itself.
            int x = arrow.X + dx;
            int y = arrow.Y + dy;
            while (InBounds(x, y))
            {
                Arrow blocker = GetArrowAt(x, y);
                if (blocker != null) return blocker;
                x += dx;
                y += dy;
            }
            return null;
        }

        /// <summary>Removes a board member without checking its exit path.</summary>
        public bool Remove(Arrow arrow) => arrows.Remove(arrow);

        /// <summary>Checks whether no arrows remain.</summary>
        public bool IsCleared() => arrows.Count == 0;

        /// <summary>Copies the board and creates a separate instance of every arrow.</summary>
        public Board Clone()
        {
            var clone = new Board(Width, Height);
            foreach (Arrow arrow in arrows)
                clone.TryAddArrow(arrow.Id, arrow.X, arrow.Y, arrow.Direction, out _);
            return clone;
        }

        private bool Contains(Arrow arrow) => arrow != null && arrows.Contains(arrow);
    }
}
