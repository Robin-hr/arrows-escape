using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Validates level data and builds boards without checking solvability.</summary>
    public static class LevelBuilder
    {
        /// <summary>Collects all data errors or returns a new board; invalid data never throws.</summary>
        public static LevelLoadResult Build(LevelData data)
        {
            if (data == null) return LevelLoadResult.Fail(new[] { "Level data is null." });
            var errors = new List<string>();
            if (data.width < 1 || data.width > 64)
                errors.Add($"Width must be between 1 and 64; received {data.width}.");
            if (data.height < 1 || data.height > 64)
                errors.Add($"Height must be between 1 and 64; received {data.height}.");
            if (data.arrows == null || data.arrows.Count == 0)
                errors.Add("The arrows list must not be null or empty.");

            var ids = new HashSet<int>();
            var cells = new Dictionary<Cell,int>();
            if (data.arrows != null)
                for (int i = 0; i < data.arrows.Count; i++)
                    ValidateArrow(data.arrows[i], i, data, ids, cells, errors);
            if (errors.Count > 0) return LevelLoadResult.Fail(errors, data.name);

            var board = new Board(data.width, data.height);
            foreach (ArrowData arrow in data.arrows)
            {
                TryDirection(arrow.direction, out Direction direction);
                TryKind(arrow.type,out ArrowKind kind);
                board.TryAddArrow(new Arrow(arrow.id, ReadCells(arrow), direction, kind), out _);
            }
            return LevelLoadResult.Ok(board, data.name);
        }

        private static void ValidateArrow(ArrowData arrow, int index, LevelData data,
            HashSet<int> ids, Dictionary<Cell,int> cells, List<string> errors)
        {
            if (arrow == null)
            {
                errors.Add($"Arrow entry at index {index} is null.");
                return;
            }
            string label = $"Arrow {arrow.id} at ({arrow.x}, {arrow.y})";
            if(!TryKind(arrow.type,out _))errors.Add($"{label}: unknown type '{arrow.type}'; use Normal or Ice.");
            if (!TryDirection(arrow.direction, out _))
                errors.Add($"{label}: unknown direction '{arrow.direction ?? "null"}'; use Up, Down, Left, or Right.");
            if (arrow.x < 0 || arrow.x >= data.width || arrow.y < 0 || arrow.y >= data.height)
                errors.Add($"{label}: out of bounds for {data.width} x {data.height}.");
            ValidateCells(arrow, data, cells, errors);
            if (!ids.Add(arrow.id)) errors.Add($"{label}: duplicate arrow id {arrow.id}.");
        }

        private static Cell[] ReadCells(ArrowData arrow)
        {
            if(arrow.cells==null || arrow.cells.Length==0)return new[]{new Cell(arrow.x,arrow.y)};
            var result=new Cell[arrow.cells.Length/2];
            for(int i=0;i<result.Length;i++)result[i]=new Cell(arrow.cells[2*i],arrow.cells[2*i+1]);
            return result;
        }
        private static void ValidateCells(ArrowData arrow,LevelData data,Dictionary<Cell,int> occupied,List<string> errors)
        {
            string label=$"Arrow {arrow.id}";
            if(arrow.cells==null || arrow.cells.Length==0)
            {
                var cell=new Cell(arrow.x,arrow.y);
                if(occupied.TryGetValue(cell,out int owner))
                    errors.Add($"Arrow {arrow.id} at ({arrow.x}, {arrow.y}): another arrow occupies the same cell (arrow {owner}).");
                else occupied.Add(cell,arrow.id);
                return;
            }
            if(arrow.cells!=null && arrow.cells.Length%2!=0)errors.Add($"{label}: cells has odd length.");
            Cell[] path=ReadCells(arrow);var seen=new HashSet<Cell>();
            for(int i=0;i<path.Length;i++)
            {
                Cell cell=path[i];
                if(cell.X<0||cell.X>=data.width||cell.Y<0||cell.Y>=data.height)errors.Add($"{label}: cell ({cell.X}, {cell.Y}) out of bounds.");
                if(!seen.Add(cell))errors.Add($"{label}: repeated cell ({cell.X}, {cell.Y}).");
                if(occupied.TryGetValue(cell,out int owner) && owner!=arrow.id)errors.Add($"{label}: another arrow {owner} occupies the same cell ({cell.X}, {cell.Y}).");
                else occupied[cell]=arrow.id;
                if(i>0 && Math.Abs((long)cell.X-path[i-1].X)+Math.Abs((long)cell.Y-path[i-1].Y)!=1)
                    errors.Add($"{label}: non-adjacent step at ({cell.X}, {cell.Y}).");
            }
            if(path.Length==0)return;
            Cell head=path[path.Length-1];
            if(head.X!=arrow.x||head.Y!=arrow.y)errors.Add($"{label}: x/y must equal the head.");
            if(path.Length>1 && TryKind(arrow.type,out ArrowKind kind) && kind==ArrowKind.Ice)errors.Add($"{label}: ice must have length 1.");
            if(!TryDirection(arrow.direction,out Direction direction))return;
            int dx=direction.GetDx(),dy=direction.GetDy();
            if(path.Length>1 && ((long)head.X-path[path.Length-2].X!=dx || (long)head.Y-path[path.Length-2].Y!=dy))
                errors.Add($"{label}: direction does not match the last segment.");
            for(int i=0;i<path.Length-1;i++)
            {
                long ahead=dx!=0?(path[i].Y==head.Y?((long)path[i].X-head.X)*dx:0):(path[i].X==head.X?((long)path[i].Y-head.Y)*dy:0);
                if(ahead>0){errors.Add($"Arrow {arrow.id} can never exit: its own body is in front of its head.");break;}
            }
        }

        private static bool TryKind(string text,out ArrowKind kind)
        {
            kind=ArrowKind.Normal;if(string.IsNullOrEmpty(text)||string.Equals(text,"normal",StringComparison.OrdinalIgnoreCase))return true;
            kind=ArrowKind.Ice;return string.Equals(text,"ice",StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryDirection(string text, out Direction direction)
        {
            // Compare names explicitly so numeric enum strings such as "0" are rejected.
            direction = Direction.Up;
            if (string.Equals(text, "Up", StringComparison.OrdinalIgnoreCase)) return true;
            direction = Direction.Down;
            if (string.Equals(text, "Down", StringComparison.OrdinalIgnoreCase)) return true;
            direction = Direction.Left;
            if (string.Equals(text, "Left", StringComparison.OrdinalIgnoreCase)) return true;
            direction = Direction.Right;
            return string.Equals(text, "Right", StringComparison.OrdinalIgnoreCase);
        }
    }
}
