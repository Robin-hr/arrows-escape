using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks continuous cell centers against existing integer coordinates.</summary>
    public class BoardViewFloatCoordinateTests
    {
        /// <summary>Odd/even boards share exact integer centers and invert half-cell positions.</summary>
        [TestCase(3)][TestCase(4)] public void Coordinates(int size)
        {
            var host = new GameObject(); try { var view = host.AddComponent<BoardView>(); view.Show(new Board(size, size));
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                { Assert.That(view.CellToWorld((float)x, y), Is.EqualTo(view.CellToWorld(x, y))); var half = view.CellToWorld(x + 0.5f, y); Assert.That(half.x, Is.EqualTo(view.CellToWorld(x, y).x + 0.5f)); Assert.That(view.WorldToCell(half), Is.EqualTo(new Vector2(x + 0.5f, y))); }
            } finally { Object.DestroyImmediate(host); SpriteFactory.ClearCache(); }
        }
    }
}
