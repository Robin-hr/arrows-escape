using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Checks exact restoration and precedence of independent visual layers.</summary>
    public class ArrowViewLayerTests
    {
        private GameObject host;
        private ArrowView view;
        private Vector3 position, scale;
        private Color color;

        /// <summary>Creates one arrow at a nonzero cell center.</summary>
        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Layer test");
            BoardView boardView = host.AddComponent<BoardView>();
            var board = new Board(4, 4);
            board.TryAddArrow(1, 1, 2, Direction.Left, out _);
            boardView.Show(board);
            boardView.TryGetArrowView(1, out view);
            position = view.transform.position;
            scale = view.transform.localScale;
            color = view.SpriteRenderer.color;
        }

        /// <summary>Removes generated objects and sprite resources.</summary>
        [TearDown]
        public void TearDown() { Object.DestroyImmediate(host); SpriteFactory.ClearCache(); }

        private void AssertOriginal()
        {
            Assert.That(view.transform.position, Is.EqualTo(position));
            Assert.That(view.transform.localScale, Is.EqualTo(scale));
            Assert.That(view.SpriteRenderer.color, Is.EqualTo(color));
        }

        /// <summary>Highlight clearing restores original values exactly.</summary>
        [Test]
        public void HighlightRestoresExactly()
        {
            view.SetHighlight(true);
            view.SetHighlight(false);
            AssertOriginal();
        }

        /// <summary>Flash color overrides highlight and clearing reveals each lower layer.</summary>
        [Test]
        public void FlashBeatsHighlight()
        {
            view.SetHighlight(true);
            view.SetFlash(ViewTheme.BlockerTint, 1.15f);
            Assert.That(view.SpriteRenderer.color, Is.EqualTo(ViewTheme.BlockerTint));
            view.SetHighlight(true, false);
            Assert.That(view.SpriteRenderer.color, Is.EqualTo(ViewTheme.BlockerTint));
            view.ClearFlash();
            Assert.That(view.SpriteRenderer.color, Is.EqualTo(ViewTheme.DraggedAwayColor));
            view.SetHighlight(false);
            AssertOriginal();
        }

        /// <summary>Offsets are absolute values relative to the fixed base, not additive moves.</summary>
        [Test]
        public void OffsetRestoresBaseExactly()
        {
            view.SetOffset(new Vector2(0.18f, 0.25f));
            Assert.That(view.transform.position, Is.EqualTo(position + new Vector3(0.18f, 0.25f, 0)));
            view.SetOffset(Vector2.zero);
            AssertOriginal();
        }

        /// <summary>Repeated interrupted layer changes never accumulate drift.</summary>
        [Test]
        public void RepeatingLayersCausesNoDrift()
        {
            Quaternion rotation = view.transform.rotation;
            for (int i = 0; i < 100; i++)
            {
                view.SetHighlight(true);
                view.SetFlash(ViewTheme.BlockedTint, 1.15f);
                view.SetOffset(new Vector2(0.123f, -0.18f));
                view.SetAlpha(0.4f);
                view.ClearFlash();
                view.SetHighlight(false);
                view.SetOffset(Vector2.zero);
                view.SetAlpha(1);
                AssertOriginal();
            }
            Assert.That(view.transform.rotation, Is.EqualTo(rotation));
        }

        /// <summary>Reset clears all visual state, including alpha and sorting order.</summary>
        [Test]
        public void ResetVisualRestoresEverything()
        {
            view.SetHighlight(true);
            view.SetFlash(ViewTheme.BlockedTint, 1.15f);
            view.SetOffset(Vector2.up);
            view.SetAlpha(0.2f);
            view.SetSortingOrder(99);
            view.ResetVisual();
            AssertOriginal();
            Assert.That(view.Offset, Is.EqualTo(Vector2.zero));
            Assert.That(view.Alpha, Is.EqualTo(1));
            Assert.That(view.SpriteRenderer.sortingOrder, Is.EqualTo(ViewTheme.ArrowSortingOrder));
        }

        /// <summary>Opacity remains independent when flash and highlight are changed.</summary>
        [Test]
        public void AlphaIsIndependentOfTint()
        {
            view.SetAlpha(0.5f);
            view.SetFlash(ViewTheme.BlockedTint, 1);
            Assert.That(view.SpriteRenderer.color.a, Is.EqualTo(0.5f));
            view.SetHighlight(true);
            view.ClearFlash();
            Assert.That(view.SpriteRenderer.color.a, Is.EqualTo(0.5f));
        }
    }
}
