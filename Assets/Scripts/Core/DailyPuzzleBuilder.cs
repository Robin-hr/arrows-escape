using System;
namespace ArrowEscape.Core
{
    /// <summary>
    /// Builds the daily puzzle from the date alone, so every player gets the same board: one big (24 x 36) board
    /// whose outline also comes from the date. Pure and deterministic; safe to run on a worker thread.
    /// </summary>
    public static class DailyPuzzleBuilder
    {
        /// <summary>Board width in cells.</summary>
        public const int Width = 24;
        /// <summary>Board height in cells.</summary>
        public const int Height = 36;
        private static readonly ShapeKind[] Outlines = { ShapeKind.Rectangle, ShapeKind.Ellipse, ShapeKind.Hexagon, ShapeKind.Octagon, ShapeKind.Oval, ShapeKind.Rhombus, ShapeKind.Decagon };

        /// <summary>Stable seed for a calendar date key (yyyymmdd).</summary>
        public static int Seed(int dateKey) { unchecked { uint x = (uint)dateKey * 2654435761u; x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; return (int)(x & 0x7FFFFFFF); } }

        /// <summary>The outline used on a date.</summary>
        public static ShapeKind OutlineFor(int dateKey) => Outlines[Seed(dateKey) % Outlines.Length];

        /// <summary>Builds the board for a date key; never returns null.</summary>
        public static Board Build(int dateKey)
        {
            int seed = Seed(dateKey);
            ShapeKind first = OutlineFor(dateKey);
            foreach (ShapeKind shape in new[] { first, ShapeKind.Rectangle })
                for (int k = 0; k < 6; k++)
                {
                    var settings = new ShapeLevelSettings(shape, Width, Height, 4, 10, turnBias: 50, coverageTargetPermille: 750, maxFreeAtStartPermille: 500, maxAttempts: 10);
                    var result = ShapeLevelGenerator.Generate(settings, seed + k * 104729);
                    if (result.Success) return result.Board;
                }
            return Fallback();
        }

        // Straight rows of arrows that always solve; used only if generation keeps failing.
        private static Board Fallback()
        {
            var board = new Board(Width, Height); int id = 1;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x + 4 <= Width; x += 4)
                {
                    var cells = new Cell[4]; for (int i = 0; i < 4; i++) cells[i] = new Cell(x + i, y);
                    board.TryAddArrow(new Arrow(id++, cells, Direction.Right), out _);
                }
            return board;
        }
    }
}
