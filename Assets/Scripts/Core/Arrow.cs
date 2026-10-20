using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>An immutable arrow occupying an ordered path from tail to head.</summary>
    public sealed class Arrow
    {
        /// <summary>Immutable ordered cells from tail to head.</summary>
        public IReadOnlyList<Cell> Cells { get; }
        /// <summary>Number of occupied cells.</summary>
        public int Length => Cells.Count;
        /// <summary>Creates a validated path, copying the supplied cells.</summary>
        public Arrow(int id, IReadOnlyList<Cell> cells, Direction direction, ArrowKind kind = ArrowKind.Normal)
        {
            if(cells==null || cells.Count==0) throw new ArgumentException("A path needs at least one cell.", nameof(cells));
            if(direction<Direction.Up || direction>Direction.Right || kind<ArrowKind.Normal || kind>ArrowKind.Ice)
                throw new ArgumentException("Invalid direction or kind.");
            if(kind==ArrowKind.Ice && cells.Count>1) throw new ArgumentException("Ice must occupy one cell.");
            var copy=new Cell[cells.Count]; var seen=new HashSet<Cell>();
            for(int i=0;i<copy.Length;i++)
            {
                copy[i]=cells[i];
                if(!seen.Add(copy[i])) throw new ArgumentException("A path cannot repeat a cell.");
                if(i>0 && Math.Abs((long)copy[i].X-copy[i-1].X)+Math.Abs((long)copy[i].Y-copy[i-1].Y)!=1)
                    throw new ArgumentException("Path cells must be orthogonal neighbours.");
            }
            Cell head=copy[copy.Length-1];
            if(copy.Length>1 && ((long)head.X-copy[copy.Length-2].X!=direction.GetDx() || (long)head.Y-copy[copy.Length-2].Y!=direction.GetDy()))
                throw new ArgumentException("Direction must match the last segment.");
            Id=id; X=head.X; Y=head.Y; Direction=direction; Kind=kind; Cells=Array.AsReadOnly(copy);
        }
        /// <summary>The arrow's unique board identifier.</summary>
        public int Id { get; }
        /// <summary>The column, increasing to the right.</summary>
        public int X { get; }
        /// <summary>The row, increasing upward.</summary>
        public int Y { get; }
        /// <summary>The direction this arrow faces.</summary>
        public Direction Direction { get; }
        /// <summary>Normal exits only; Ice can also advance toward a blocker.</summary>
        public ArrowKind Kind { get; }

        /// <summary>Creates an arrow with a fixed position and direction.</summary>
        public Arrow(int id, int x, int y, Direction direction) : this(id,x,y,direction,ArrowKind.Normal) { }

        /// <summary>Creates an immutable arrow with explicit rules.</summary>
        public Arrow(int id, int x, int y, Direction direction, ArrowKind kind)
        {
            Cells = Array.AsReadOnly(new[] { new Cell(x,y) });
            Id = id;
            X = x;
            Y = y;
            Direction = direction;
            Kind = kind;
        }
    }
}
