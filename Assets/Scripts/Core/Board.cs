using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>A grid of arrows with no rendering or input dependencies.</summary>
    public sealed class Board
    {
        private readonly List<Arrow> arrows = new List<Arrow>();
        private readonly Dictionary<Cell,Arrow> occupancy = new Dictionary<Cell,Arrow>();

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
            => TryAddArrow(id,x,y,direction,ArrowKind.Normal,out arrow);

        /// <summary>Adds an explicit kind with the same cell and identifier failure rules.</summary>
        public bool TryAddArrow(int id, int x, int y, Direction direction, ArrowKind kind, out Arrow arrow)
        {
            arrow = null;
            if (!InBounds(x, y) || GetArrowAt(x, y) != null || GetArrowById(id) != null)
                return false;
            if (direction < Direction.Up || direction > Direction.Right) return false;
            if (kind < ArrowKind.Normal || kind > ArrowKind.Ice) return false;
            arrow = new Arrow(id, x, y, direction, kind);
            arrows.Add(arrow);
            occupancy.Add(new Cell(x,y),arrow);
            return true;
        }

        /// <summary>Adds a copied path atomically; invalid bounds, cells and ids return false.</summary>
        public bool TryAddArrow(Arrow source, out Arrow arrow)
        {
            arrow=null;
            if(source==null || GetArrowById(source.Id)!=null || source.Direction<Direction.Up || source.Direction>Direction.Right || source.Kind<ArrowKind.Normal || source.Kind>ArrowKind.Ice) return false;
            for(int i=0;i<source.Length;i++)
            { Cell cell=source.Cells[i]; if(!InBounds(cell.X,cell.Y)||occupancy.ContainsKey(cell))return false; }
            arrow=new Arrow(source.Id,source.Cells,source.Direction,source.Kind);
            arrows.Add(arrow);
            for(int i=0;i<arrow.Length;i++)occupancy.Add(arrow.Cells[i],arrow);
            return true;
        }

        /// <summary>Returns the first occupied ray cell, including the arrow's own body.</summary>
        public Cell? GetBlockingCell(Arrow arrow)
        {
            if(!Contains(arrow))return null;
            int dx=arrow.Direction.GetDx(),dy=arrow.Direction.GetDy();
            for(int x=arrow.X+dx,y=arrow.Y+dy;InBounds(x,y);x+=dx,y+=dy)
                if(GetArrowAt(x,y)!=null)return new Cell(x,y);
            return null;
        }

        /// <summary>Checks whether a cell lies inside the board.</summary>
        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        /// <summary>Returns the arrow at a cell, or null if empty or outside the board.</summary>
        public Arrow GetArrowAt(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return occupancy.TryGetValue(new Cell(x,y),out Arrow arrow) ? arrow : null;
        }

        /// <summary>Returns the arrow with this identifier, or null.</summary>
        public Arrow GetArrowById(int id) => arrows.Find(arrow => arrow.Id == id);

        /// <summary>Checks whether a board member has a clear path to the edge.</summary>
        public bool CanExit(Arrow arrow) => Contains(arrow) && GetBlocker(arrow) == null;

        /// <summary>Returns the nearest blocker, or null for a clear path or nonmember.</summary>
        public Arrow GetBlocker(Arrow arrow)
        {
            Cell? cell=GetBlockingCell(arrow);
            return cell.HasValue ? GetArrowAt(cell.Value.X,cell.Value.Y) : null;
        }

        /// <summary>Removes a board member without checking its exit path.</summary>
        public bool Remove(Arrow arrow)
        {
            if(!arrows.Remove(arrow))return false;
            for(int i=0;i<arrow.Length;i++)occupancy.Remove(arrow.Cells[i]);
            return true;
        }

        /// <summary>Checks whether no arrows remain.</summary>
        public bool IsCleared() => arrows.Count == 0;

        /// <summary>Whether any current arrow uses the ice rules.</summary>
        public bool HasIceArrows { get { foreach (Arrow arrow in arrows) if (arrow.Kind == ArrowKind.Ice) return true; return false; } }
        /// <summary>Counts consecutive empty cells immediately ahead, up to a blocker or edge.</summary>
        public int CountFreeCellsAhead(Arrow arrow)
        {
            if (!Contains(arrow)) return 0;
            int dx=arrow.Direction.GetDx(),dy=arrow.Direction.GetDy(),x=arrow.X+dx,y=arrow.Y+dy,count=0;
            while(InBounds(x,y) && GetArrowAt(x,y)==null) { count++; x+=dx; y+=dy; }
            return count;
        }
        /// <summary>Only a blocked Ice arrow with a gap can slide.</summary>
        public bool CanSlide(Arrow arrow) => Contains(arrow) && arrow.Kind==ArrowKind.Ice && !CanExit(arrow) && CountFreeCellsAhead(arrow)>0;
        /// <summary>Replaces the current instance at its existing list index; stale references fail.</summary>
        public bool TryMoveArrow(Arrow arrow, int newX, int newY, out Arrow moved)
        {
            moved=null;
            if(!Contains(arrow) || arrow.Length>1 || !InBounds(newX,newY) || GetArrowAt(newX,newY)!=null || (arrow.X==newX && arrow.Y==newY)) return false;
            int index=arrows.IndexOf(arrow);
            moved=new Arrow(arrow.Id,newX,newY,arrow.Direction,arrow.Kind); arrows[index]=moved; occupancy.Remove(arrow.Cells[0]); occupancy.Add(moved.Cells[0],moved); return true;
        }

        /// <summary>Copies the board and creates a separate instance of every arrow.</summary>
        public Board Clone()
        {
            var clone = new Board(Width, Height);
            foreach (Arrow arrow in arrows)
                clone.TryAddArrow(arrow, out _);
            return clone;
        }

        private bool Contains(Arrow arrow) => arrow != null && arrows.Contains(arrow);
    }
}
