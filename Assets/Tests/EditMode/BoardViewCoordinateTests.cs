using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Checks continuous cell coordinates independently of input devices.</summary>
    public class BoardViewCoordinateTests
    {
        private GameObject host;

        /// <summary>Destroys generated scene objects and sprite resources.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            SpriteFactory.ClearCache();
        }

        private BoardView Create(int width, int height)
        {
            host = new GameObject("Coordinate test");
            BoardView view = host.AddComponent<BoardView>();
            view.Show(new Board(width, height));
            return view;
        }

        /// <summary>Odd, even, and rectangular board cell centers round-trip exactly.</summary>
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(3, 8)]
        [TestCase(12, 5)]
        public void CellCentersRoundTrip(int width, int height)
        {
            BoardView view = Create(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Vector2 cell = view.WorldToCell(view.CellToWorld(x, y));
                    Assert.That(cell.x, Is.EqualTo(x).Within(0.0001f));
                    Assert.That(cell.y, Is.EqualTo(y).Within(0.0001f));
                }
        }

        /// <summary>Half a world unit is half a cell from the integer center.</summary>
        [Test]
        public void HalfCellOffsetRemainsContinuous()
        {
            BoardView view = Create(4, 4);
            Vector2 cell = view.WorldToCell(view.CellToWorld(1, 2) + Vector3.right * 0.5f);
            Assert.That(cell.x, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(cell.y, Is.EqualTo(2f).Within(0.0001f));
        }
    }
}
