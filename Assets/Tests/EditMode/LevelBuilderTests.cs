using System.Collections.Generic;
using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks validation separately from parsing and solvability.</summary>
    public class LevelBuilderTests
    {
        private static LevelData ValidData() => new LevelData
        {
            name = "Example", width = 4, height = 3,
            arrows = new List<ArrowData>
            {
                new ArrowData { id = 1, x = 0, y = 0, direction = "Up" },
                new ArrowData { id = 2, x = 2, y = 1, direction = "Right" }
            }
        };

        private static void AssertFailure(LevelLoadResult result, string message)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Board, Is.Null);
            Assert.That(result.Errors.Count, Is.GreaterThan(0));
            Assert.That(string.Join(" ", result.Errors), Does.Contain(message));
        }

        /// <summary>Valid data retains dimensions, arrows, and name.</summary>
        [Test]
        public void ValidDataBuildsBoard()
        {
            LevelLoadResult result = LevelBuilder.Build(ValidData());
            Assert.That(result.Success, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.LevelName, Is.EqualTo("Example"));
            Assert.That(result.Board.Width, Is.EqualTo(4));
            Assert.That(result.Board.Height, Is.EqualTo(3));
            Assert.That(result.Board.Arrows.Count, Is.EqualTo(2));
            Assert.That(result.Board.GetArrowAt(0, 0).Id, Is.EqualTo(1));
            Assert.That(result.Board.GetArrowAt(2, 1).Id, Is.EqualTo(2));
        }

        /// <summary>Direction names ignore letter case.</summary>
        [TestCase("up", Direction.Up)]
        [TestCase("UP", Direction.Up)]
        [TestCase("Up", Direction.Up)]
        [TestCase("dOwN", Direction.Down)]
        [TestCase("LEFT", Direction.Left)]
        [TestCase("right", Direction.Right)]
        public void DirectionsIgnoreCase(string text, Direction expected)
        {
            LevelData data = ValidData();
            data.arrows[0].direction = text;
            LevelLoadResult result = LevelBuilder.Build(data);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Board.GetArrowById(1).Direction, Is.EqualTo(expected));
        }

        /// <summary>Missing data produces an error rather than an exception.</summary>
        [Test]
        public void NullDataFails() => AssertFailure(LevelBuilder.Build(null), "null");

        /// <summary>Both dimensions must stay within supported bounds.</summary>
        [TestCase(0, 3, "Width")]
        [TestCase(4, 0, "Height")]
        [TestCase(65, 3, "Width")]
        [TestCase(4, 65, "Height")]
        [TestCase(-1, 3, "Width")]
        [TestCase(4, -1, "Height")]
        public void InvalidDimensionsFail(int width, int height, string message)
        {
            LevelData data = ValidData();
            data.width = width;
            data.height = height;
            AssertFailure(LevelBuilder.Build(data), message);
        }

        /// <summary>Null and empty lists are invalid levels.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public void MissingArrowsFail(bool useNull)
        {
            LevelData data = ValidData();
            data.arrows = useNull ? null : new List<ArrowData>();
            AssertFailure(LevelBuilder.Build(data), "arrows list");
        }

        /// <summary>Only the four direction names are allowed, including no numeric aliases.</summary>
        [TestCase("Diagonal")]
        [TestCase("0")]
        [TestCase("")]
        [TestCase(null)]
        public void UnknownDirectionFails(string direction)
        {
            LevelData data = ValidData();
            data.arrows[0].direction = direction;
            AssertFailure(LevelBuilder.Build(data), "Arrow 1 at (0, 0): unknown direction");
        }

        /// <summary>Negative and oversized coordinates are reported with arrow details.</summary>
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(4, 0)]
        [TestCase(0, 3)]
        public void OutOfBoundsFails(int x, int y)
        {
            LevelData data = ValidData();
            data.arrows[0].x = x;
            data.arrows[0].y = y;
            AssertFailure(LevelBuilder.Build(data), $"Arrow 1 at ({x}, {y}): out of bounds");
        }

        /// <summary>Two arrows cannot share a cell.</summary>
        [Test]
        public void SameCellFails()
        {
            LevelData data = ValidData();
            data.arrows[1].x = 0;
            data.arrows[1].y = 0;
            AssertFailure(LevelBuilder.Build(data), "Arrow 2 at (0, 0): another arrow occupies the same cell");
        }

        /// <summary>Identifiers must be unique even when positions differ.</summary>
        [Test]
        public void DuplicateIdFails()
        {
            LevelData data = ValidData();
            data.arrows[1].id = 1;
            AssertFailure(LevelBuilder.Build(data), "duplicate arrow id 1");
        }

        /// <summary>Null entries are invalid rather than causing a crash.</summary>
        [Test]
        public void NullArrowEntryFails()
        {
            LevelData data = ValidData();
            data.arrows.Add(null);
            AssertFailure(LevelBuilder.Build(data), "index 2 is null");
        }

        /// <summary>Independent errors are collected even after invalid dimensions.</summary>
        [Test]
        public void MultipleProblemsAreAllReported()
        {
            var data = new LevelData
            {
                width = 0, height = 65,
                arrows = new List<ArrowData>
                {
                    new ArrowData { id = 7, x = -1, y = 0, direction = "bad" },
                    new ArrowData { id = 7, x = -1, y = 0, direction = null }, null
                }
            };
            LevelLoadResult result = LevelBuilder.Build(data);
            AssertFailure(result, "Width");
            Assert.That(result.Errors.Count, Is.EqualTo(9));
            string messages = string.Join(" ", result.Errors);
            foreach (string expected in new[] { "Height", "unknown direction", "out of bounds",
                "same cell", "duplicate arrow id", "index 2 is null" })
                Assert.That(messages, Does.Contain(expected));
        }

        /// <summary>Building a valid board does not reject an unsolvable puzzle.</summary>
        [Test]
        public void BuilderDoesNotCheckSolvability()
        {
            LevelData data = ValidData();
            data.arrows[0].direction = "Right";
            data.arrows[1].y = 0;
            data.arrows[1].direction = "Left";
            LevelLoadResult result = LevelBuilder.Build(data);
            Assert.That(result.Success, Is.True);
            Assert.That(LevelSolvability.IsSolvable(result.Board), Is.False);
        }

        /// <summary>Failure factories always supply a message.</summary>
        [Test]
        public void FailFactoryGuaranteesError() => AssertFailure(LevelLoadResult.Fail(null), "could not be loaded");
    }
}
