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
            var cells = new HashSet<(int, int)>();
            if (data.arrows != null)
                for (int i = 0; i < data.arrows.Count; i++)
                    ValidateArrow(data.arrows[i], i, data, ids, cells, errors);
            if (errors.Count > 0) return LevelLoadResult.Fail(errors, data.name);

            var board = new Board(data.width, data.height);
            foreach (ArrowData arrow in data.arrows)
            {
                TryDirection(arrow.direction, out Direction direction);
                board.TryAddArrow(arrow.id, arrow.x, arrow.y, direction, out _);
            }
            return LevelLoadResult.Ok(board, data.name);
        }

        private static void ValidateArrow(ArrowData arrow, int index, LevelData data,
            HashSet<int> ids, HashSet<(int, int)> cells, List<string> errors)
        {
            if (arrow == null)
            {
                errors.Add($"Arrow entry at index {index} is null.");
                return;
            }
            string label = $"Arrow {arrow.id} at ({arrow.x}, {arrow.y})";
            if (!TryDirection(arrow.direction, out _))
                errors.Add($"{label}: unknown direction '{arrow.direction ?? "null"}'; use Up, Down, Left, or Right.");
            if (arrow.x < 0 || arrow.x >= data.width || arrow.y < 0 || arrow.y >= data.height)
                errors.Add($"{label}: out of bounds for {data.width} x {data.height}.");
            if (!cells.Add((arrow.x, arrow.y)))
                errors.Add($"{label}: another arrow occupies the same cell.");
            if (!ids.Add(arrow.id)) errors.Add($"{label}: duplicate arrow id {arrow.id}.");
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
