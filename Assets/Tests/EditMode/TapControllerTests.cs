using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArrowEscape.Tests
{
    /// <summary>Drives injected world input without a camera or physical input device.</summary>
    public class TapControllerTests
    {
        private GameObject host;
        private Board board;
        private BoardView view;
        private TapController controller;
        private ArrowView first;
        private Vector2 center;
        private Color normalColor;
        private Vector3 normalScale;
        private int tapped;
        private Arrow lastTap;

        /// <summary>Creates a two-arrow display, receiver, and tap counter.</summary>
        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Tap controller test");
            view = host.AddComponent<BoardView>();
            controller = host.AddComponent<TapController>();
            board = new Board(4, 3);
            board.TryAddArrow(1, 0, 0, Direction.Right, out _);
            board.TryAddArrow(2, 1, 0, Direction.Up, out _);
            view.Show(board);
            controller.Bind(view);
            view.TryGetArrowView(1, out first);
            center = view.CellToWorld(0, 0);
            normalColor = first.SpriteRenderer.color;
            normalScale = first.transform.localScale;
            tapped = 0;
            lastTap = null;
            controller.ArrowTapped += arrow => { tapped++; lastTap = arrow; };
        }

        /// <summary>Destroys all scene and runtime sprite objects.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            SpriteFactory.ClearCache();
        }

        /// <summary>One press/release emits one event without a per-tap log.</summary>
        [Test]
        public void PressReleaseReportsOnce()
        {
            controller.PointerDown(1, center);
            controller.PointerUp(1, center);
            controller.PointerUp(1, center);
            Assert.That(tapped, Is.EqualTo(1));
            Assert.That(lastTap, Is.SameAs(board.GetArrowById(1)));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Pressing changes only tint and scale, then restores both exactly.</summary>
        [Test]
        public void HighlightRestoresExactOriginalValues()
        {
            Vector3 position = first.transform.position;
            Quaternion rotation = first.transform.rotation;
            controller.PointerDown(1, center);
            Assert.That(first.SpriteRenderer.color, Is.EqualTo(ViewTheme.HighlightColor));
            Assert.That(first.transform.localScale, Is.EqualTo(normalScale * ViewTheme.HighlightScaleMultiplier));
            Assert.That(first.transform.position, Is.EqualTo(position));
            Assert.That(first.transform.rotation, Is.EqualTo(rotation));
            controller.PointerUp(1, center);
            Assert.That(first.SpriteRenderer.color, Is.EqualTo(normalColor));
            Assert.That(first.transform.localScale, Is.EqualTo(normalScale));
        }

        /// <summary>Empty-space presses create no highlight, event, or log.</summary>
        [Test]
        public void EmptySpaceDoesNothing()
        {
            Vector2 empty = view.CellToWorld(3, 2);
            controller.PointerDown(1, empty);
            controller.PointerUp(1, empty);
            Assert.That(tapped, Is.Zero);
            Assert.That(first.SpriteRenderer.color, Is.EqualTo(normalColor));
            Assert.That(first.transform.localScale, Is.EqualTo(normalScale));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Dragging away dims the arrow, and releasing cancels and restores it.</summary>
        [Test]
        public void DragAwayCancelsAndClearsHighlight()
        {
            controller.PointerDown(1, center);
            controller.PointerMove(1, center + Vector2.up);
            Assert.That(first.SpriteRenderer.color, Is.EqualTo(ViewTheme.DraggedAwayColor));
            controller.PointerUp(1, center + Vector2.up);
            Assert.That(tapped, Is.Zero);
            Assert.That(first.SpriteRenderer.color, Is.EqualTo(normalColor));
            Assert.That(first.transform.localScale, Is.EqualTo(normalScale));
        }

        /// <summary>Returning inside restores the warm tint and permits a tap.</summary>
        [Test]
        public void DragBackRestoresStillOverTint()
        {
            controller.PointerDown(1, center);
            controller.PointerMove(1, center + Vector2.up);
            controller.PointerMove(1, center + Vector2.up * 0.5f);
            Assert.That(first.SpriteRenderer.color, Is.EqualTo(ViewTheme.HighlightColor));
            controller.PointerUp(1, center + Vector2.up * 0.5f);
            Assert.That(tapped, Is.EqualTo(1));
        }

        /// <summary>New levels cancel old presses even when an identifier is reused.</summary>
        [Test]
        public void LoadingBoardResetsPressAndHighlights()
        {
            controller.PointerDown(1, center);
            var next = new Board(2, 2);
            next.TryAddArrow(1, 1, 1, Direction.Left, out _);
            view.Show(next);
            controller.PointerUp(1, view.CellToWorld(1, 1));
            Assert.That(tapped, Is.Zero);
            Assert.That(view.TryGetArrowView(1, out ArrowView replacement), Is.True);
            Assert.That(replacement.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
            Assert.That(replacement.transform.localScale, Is.EqualTo(Vector3.one * ViewTheme.ArrowFillRatio));
        }

        /// <summary>Other pointer ids cannot steal, dim, or finish a press.</summary>
        [Test]
        public void SecondPointerIsIgnored()
        {
            controller.PointerDown(1, center);
            controller.PointerDown(2, view.CellToWorld(1, 0));
            controller.PointerMove(2, center + Vector2.up * 5);
            controller.PointerUp(2, center);
            Assert.That(tapped, Is.Zero);
            Assert.That(first.SpriteRenderer.color, Is.EqualTo(ViewTheme.HighlightColor));
            controller.PointerUp(1, center);
            Assert.That(lastTap.Id, Is.EqualTo(1));
        }

        /// <summary>Reporting a tap changes neither the core board nor arrow positions.</summary>
        [Test]
        public void TapNeverMutatesBoardOrMovesArrows()
        {
            Vector3 before = first.transform.position;
            controller.PointerDown(1, center);
            controller.PointerMove(1, center + Vector2.right * 0.7f);
            controller.PointerUp(1, center + Vector2.right * 0.7f);
            Assert.That(board.Arrows.Count, Is.EqualTo(2));
            Assert.That(first.transform.position, Is.EqualTo(before));
            Assert.That(lastTap, Is.SameAs(board.GetArrowById(1)));
        }

        /// <summary>Cancellation restores the highlight without reporting a tap.</summary>
        [Test]
        public void CancelRestoresHighlight()
        {
            controller.PointerDown(1, center);
            controller.PointerCancel(1);
            controller.PointerUp(1, center);
            Assert.That(tapped, Is.Zero);
            Assert.That(first.SpriteRenderer.color, Is.EqualTo(normalColor));
            Assert.That(first.transform.localScale, Is.EqualTo(normalScale));
        }

        /// <summary>Missing bindings and cleared boards accept no input.</summary>
        [Test]
        public void MissingBoardOrBindingDoesNothing()
        {
            view.Clear();
            controller.PointerDown(1, center);
            controller.PointerUp(1, center);
            controller.Bind(null);
            controller.PointerDown(1, center);
            controller.PointerMove(1, center);
            controller.PointerUp(1, center);
            Assert.That(tapped, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
