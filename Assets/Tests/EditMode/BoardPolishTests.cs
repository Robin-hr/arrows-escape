using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Curved arrow bends, painted head art, shape-only dots, centred framing and the level-start transition (Step 14G).</summary>
    public sealed class BoardPolishTests
    {
        private static Board PathBoard()
        {
            var board = new Board(6, 6);
            board.TryAddArrow(new Arrow(1, new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(2, 1) }, Direction.Up), out _);
            board.TryAddArrow(new Arrow(2, new[] { new Cell(5, 5), new Cell(4, 5), new Cell(4, 4), new Cell(3, 4) }, Direction.Left), out _);
            return board;
        }

        [Test] public void BendsBecomeCurvesAndTheHeadCellStaysExact()
        {
            var points = new[] { new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(2, 5) };
            var curved = CurvedTrack.Build(points, 2, out float head);
            Assert.That(curved.Length, Is.EqualTo(CurvedTrack.BendSegments + 4));
            Assert.That(curved[0], Is.EqualTo(points[0])); Assert.That(curved[curved.Length - 1], Is.EqualTo(points[3]));
            Assert.That(head, Is.LessThan(4f).And.GreaterThan(3.5f), "the bend cuts the corner");
            var ribbon = new PathRibbon(curved, head); ribbon.Build(0);
            Assert.That(Vector2.Distance(ribbon.Point(head), points[2]), Is.LessThan(1e-4f));
            for (int i = 1; i < curved.Length; i++) Assert.That(Vector2.Distance(curved[i - 1], curved[i]), Is.GreaterThan(0));
        }

        [Test] public void StraightArrowsAreUnchanged()
        {
            var points = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(6, 0) };
            var curved = CurvedTrack.Build(points, 2, out float head);
            Assert.That(curved, Is.EqualTo(points)); Assert.That(head, Is.EqualTo(2f));
        }

        [Test] public void DotsOnlyWhereArrowsAreAndCameraFramesTheShape()
        {
            using (var s = new EconomyTestScene())
            {
                var board = new Board(8, 8);
                Assert.That(board.TryAddArrow(new Arrow(1, new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(2, 1) }, Direction.Up), out _), Is.True);
                s.Game.StartLevel(board); s.Board.GetComponent<CameraFitter>().Fit(8, 8);
                var dots = s.Board.PathDots; Assert.That(dots, Is.Not.Null);
                Vector3[] v = dots.Vertices;
                float Radius(int x, int y) { int i = (y * 8 + x) * 13; return (v[i + 1] - v[i]).magnitude; }
                Assert.That(Radius(0, 0), Is.GreaterThan(0.01f)); Assert.That(Radius(2, 1), Is.GreaterThan(0.01f));
                Assert.That(Radius(7, 7), Is.EqualTo(0f).Within(1e-6f)); Assert.That(Radius(5, 3), Is.EqualTo(0f).Within(1e-6f));
                Bounds content = s.Board.ContentBounds;
                Assert.That(content.size.x, Is.EqualTo(3)); Assert.That(content.size.y, Is.EqualTo(2));
                Vector3 expected = (s.Board.CellToWorld(0, 0) + s.Board.CellToWorld(2, 1)) * 0.5f;
                Assert.That(Vector2.Distance(content.center, expected), Is.LessThan(1e-4f));
                if (Screen.width > 0 && Screen.height > 0) Assert.That(Camera.main.transform.position.x, Is.LessThan(-1f), "camera moves to the shape, not the empty grid centre");
            }
        }

        [Test] public void ClassicArrowsUsePaintedHeadArt()
        {
            using (var s = new EconomyTestScene())
            {
                s.Game.StartLevel(PathBoard());
                Assert.That(s.Board.TryGetArrowView(1, out ArrowView view), Is.True);
                Assert.That(view.Path.HeadArt, Is.Not.Null); Assert.That(view.Path.HeadArt.enabled, Is.True);
                Assert.That(Resources.Load<Texture2D>("Arrows/arrow_body"), Is.Not.Null);
            }
        }

        [Test] public void IntroLandsEveryArrowThenFinishes()
        {
            using (var s = new EconomyTestScene())
            {
                s.Game.StartLevel(PathBoard());
                var intro = s.Board.gameObject.AddComponent<BoardIntro>();
                intro.Play(0.2f);
                Assert.That(intro.IsPlaying, Is.True);
                s.Board.TryGetArrowView(1, out ArrowView view);
                Assert.That(view.Alpha, Is.Zero); Assert.That(view.Offset.y, Is.GreaterThan(0));
                for (int i = 0; i < 40 && intro.IsPlaying; i++) intro.Tick(0.05f);
                Assert.That(intro.IsPlaying, Is.False); Assert.That(view.Alpha, Is.EqualTo(1)); Assert.That(view.Offset, Is.EqualTo(Vector2.zero));
            }
        }
    }
}
