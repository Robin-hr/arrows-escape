using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Verifies view replacement and cleanup across restoration.</summary>
    public class BoardViewUndoTests
    {
        private GameObject host; private BoardView view; private Board board;
        /// <summary>Creates a runtime display.</summary>
        [SetUp] public void SetUp() { host = new GameObject(); view = host.AddComponent<BoardView>(); board = new Board(3, 3); board.TryAddArrow(1, 1, 1, Direction.Left, out _); view.Show(board); }
        /// <summary>Removes all runtime objects.</summary>
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); SpriteFactory.ClearCache(); }
        /// <summary>Added arrows have correct placement and orientation.</summary>
        [Test] public void Add()
        { board.TryAddArrow(2, 2, 2, Direction.Right, out Arrow a); ArrowView added = view.AddArrowView(a); Assert.That(added.transform.position, Is.EqualTo(view.CellToWorld(2, 2))); Assert.That(Quaternion.Angle(added.transform.rotation, Quaternion.Euler(0, 0, -90)), Is.LessThan(0.001)); Assert.That(view.TryGetArrowView(2, out ArrowView found), Is.True); Assert.That(found, Is.SameAs(added)); }
        /// <summary>Replacing an id leaves one fresh normal view.</summary>
        [Test] public void Replace()
        { view.TryGetArrowView(1, out ArrowView old); old.SetHighlight(true); ArrowView fresh = view.AddArrowView(board.Arrows[0]); Assert.That(old == null, Is.True); Assert.That(host.GetComponentsInChildren<ArrowView>().Length, Is.EqualTo(1)); Assert.That(fresh.Offset, Is.EqualTo(Vector2.zero)); Assert.That(fresh.Alpha, Is.EqualTo(1)); }
        /// <summary>Discard returns true only for an actual released view.</summary>
        [Test] public void Discard()
        { view.ReleaseArrowView(1, out ArrowView old); Assert.That(view.DiscardReleasedView(1), Is.True); Assert.That(old == null, Is.True); Assert.That(view.DiscardReleasedView(1), Is.False); }
        /// <summary>Clear and replacement show also remove released and restored children.</summary>
        [TestCase(false)][TestCase(true)] public void Cleanup(bool show)
        { view.ReleaseArrowView(1, out ArrowView released); board.TryAddArrow(2, 2, 2, Direction.Up, out Arrow a); ArrowView restored = view.AddArrowView(a); if (show) view.Show(new Board(1, 1)); else view.Clear(); Assert.That(released == null, Is.True); Assert.That(restored == null, Is.True); Assert.That(host.GetComponentsInChildren<ArrowView>().Length, Is.Zero); }
    }
}
