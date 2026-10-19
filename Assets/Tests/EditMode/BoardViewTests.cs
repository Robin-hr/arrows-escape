using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Checks generated objects and sprites without editing scenes or prefabs.</summary>
    public class BoardViewTests
    {
        private GameObject host;
        private BoardView view;

        /// <summary>Creates a fresh view for each test.</summary>
        [SetUp]
        public void SetUp()
        {
            host = new GameObject("BoardView test");
            view = host.AddComponent<BoardView>();
        }

        /// <summary>Destroys all test objects and generated sprite resources immediately.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            SpriteFactory.ClearCache();
        }

        private static Board MakeBoard()
        {
            var board = new Board(4, 4);
            board.TryAddArrow(1, 0, 0, Direction.Up, out _);
            board.TryAddArrow(2, 1, 0, Direction.Left, out _);
            board.TryAddArrow(3, 2, 0, Direction.Down, out _);
            board.TryAddArrow(4, 3, 0, Direction.Right, out _);
            return board;
        }

        /// <summary>Odd and even grids have their center at world zero.</summary>
        [TestCase(3, 3, 0, 0, -1f, -1f)]
        [TestCase(3, 3, 2, 2, 1f, 1f)]
        [TestCase(4, 4, 0, 0, -1.5f, -1.5f)]
        [TestCase(4, 4, 3, 3, 1.5f, 1.5f)]
        [TestCase(3, 8, 0, 0, -1f, -3.5f)]
        public void CellToWorldCentersGrid(int width, int height, int x, int y, float worldX, float worldY)
        {
            view.Show(new Board(width, height));
            Assert.That(view.CellToWorld(x, y), Is.EqualTo(new Vector3(worldX, worldY, 0)));
        }

        /// <summary>One view per arrow is placed at the corresponding cell.</summary>
        [Test]
        public void ShowCreatesExactlyOneViewPerArrow()
        {
            Board board = MakeBoard();
            view.Show(board);
            ArrowView[] arrows = host.GetComponentsInChildren<ArrowView>();
            Assert.That(arrows.Length, Is.EqualTo(board.Arrows.Count));
            Assert.That(host.transform.childCount, Is.EqualTo(1));
            Assert.That(host.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(16 + 4));
            foreach (ArrowView arrow in arrows)
            {
                Assert.That(arrow.Arrow, Is.SameAs(board.GetArrowById(arrow.Arrow.Id)));
                Assert.That(arrow.transform.position, Is.EqualTo(view.CellToWorld(arrow.Arrow.X, arrow.Arrow.Y)));
            }
        }

        /// <summary>Replacing the display destroys the previous hierarchy in Edit Mode.</summary>
        [Test]
        public void ShowTwiceDoesNotLeaveOldObjects()
        {
            view.Show(MakeBoard());
            ArrowView oldView = host.GetComponentInChildren<ArrowView>();
            var replacement = new Board(2, 2);
            replacement.TryAddArrow(9, 1, 1, Direction.Down, out _);
            view.Show(replacement);
            Assert.That(oldView == null, Is.True);
            Assert.That(host.transform.childCount, Is.EqualTo(1));
            Assert.That(host.GetComponentsInChildren<ArrowView>(true).Length, Is.EqualTo(1));
            Assert.That(host.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(5));
            Assert.That(view.TryGetArrowView(1, out _), Is.False);
        }

        /// <summary>Views can be looked up by identifier.</summary>
        [Test]
        public void TryGetArrowViewFindsOnlyKnownIds()
        {
            view.Show(MakeBoard());
            Assert.That(view.TryGetArrowView(2, out ArrowView found), Is.True);
            Assert.That(found.Arrow.Id, Is.EqualTo(2));
            Assert.That(view.TryGetArrowView(99, out ArrowView missing), Is.False);
            Assert.That(missing, Is.Null);
        }

        /// <summary>Each sprite uses the rotation of its arrow's direction.</summary>
        [Test]
        public void EveryArrowUsesCorrectRotation()
        {
            view.Show(MakeBoard());
            foreach (ArrowView arrow in host.GetComponentsInChildren<ArrowView>())
            {
                float expected = ArrowRotation.GetRotationZ(arrow.Arrow.Direction);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(arrow.transform.eulerAngles.z, expected)), Is.LessThan(0.001f));
                Assert.That(arrow.SpriteRenderer.sharedMaterial.shader.name, Is.EqualTo("Sprites/Default"));
            }
        }

        /// <summary>Clear removes the root, arrows, dots, and lookup entries.</summary>
        [Test]
        public void ClearRemovesEverything()
        {
            view.Show(MakeBoard());
            view.Clear();
            view.Clear();
            Assert.That(host.transform.childCount, Is.Zero);
            Assert.That(host.GetComponentsInChildren<ArrowView>(true), Is.Empty);
            Assert.That(view.TryGetArrowView(1, out _), Is.False);
        }

        /// <summary>Large boards display arrows without thousands of cell objects.</summary>
        [TestCase(33, 2)]
        [TestCase(2, 33)]
        public void LargeBoardsSkipCellDots(int width, int height)
        {
            var board = new Board(width, height);
            board.TryAddArrow(1, 0, 0, Direction.Up, out _);
            view.Show(board);
            Assert.That(host.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(1));
        }

        /// <summary>Generated sprites are cached, one unit wide, and excluded from saving.</summary>
        [Test]
        public void SpriteCacheHasExpectedProperties()
        {
            Sprite arrow = SpriteFactory.GetArrowSprite();
            Assert.That(SpriteFactory.GetArrowSprite(), Is.SameAs(arrow));
            Assert.That(SpriteFactory.GetCellSprite(), Is.SameAs(SpriteFactory.GetCellSprite()));
            Assert.That(arrow.texture.width, Is.EqualTo(128));
            Assert.That(arrow.texture.height, Is.EqualTo(128));
            Assert.That(arrow.bounds.size.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(arrow.bounds.size.y, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(arrow.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
            Assert.That(arrow.texture.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
        }

        /// <summary>A destroyed cache entry is recreated on demand.</summary>
        [Test]
        public void SpriteCacheRecoversAfterDestruction()
        {
            Sprite original = SpriteFactory.GetArrowSprite();
            Object.DestroyImmediate(original.texture);
            Sprite recreated = SpriteFactory.GetArrowSprite();
            Assert.That(recreated != null && recreated.texture != null, Is.True);
            Assert.That(original == null, Is.True);
        }
    }
}
