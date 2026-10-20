using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Advances animations directly with Tick, without frames, coroutines, or real time.</summary>
    public class ArrowAnimatorTests
    {
        private GameObject host;
        private BoardView boardView;
        private ArrowAnimator animator;
        private ArrowView tapped, blocker;
        private Vector3 position, blockerScale;
        private Color blockerColor;

        /// <summary>Creates two views and a reusable animator.</summary>
        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Animator test");
            boardView = host.AddComponent<BoardView>();
            animator = host.AddComponent<ArrowAnimator>();
            CreateViews(Direction.Up);
        }

        private void CreateViews(Direction direction)
        {
            var board = new Board(4, 4);
            board.TryAddArrow(1, 1, 1, direction, out _);
            board.TryAddArrow(2, 1, 3, Direction.Up, out _);
            boardView.Show(board);
            boardView.TryGetArrowView(1, out tapped);
            boardView.TryGetArrowView(2, out blocker);
            position = tapped.transform.position;
            blockerScale = blocker.transform.localScale;
            blockerColor = blocker.SpriteRenderer.color;
        }

        /// <summary>Stops animations before destroying their owners and cached sprites.</summary>
        [TearDown]
        public void TearDown()
        {
            animator.StopAll();
            Object.DestroyImmediate(host);
            SpriteFactory.ClearCache();
        }

        /// <summary>Midway slides travel partway along the correct axis and finish by destruction.</summary>
        [TestCase(Direction.Up)]
        [TestCase(Direction.Down)]
        [TestCase(Direction.Left)]
        [TestCase(Direction.Right)]
        public void SlideDirectionMidpointAndCompletion(Direction direction)
        {
            CreateViews(direction);
            animator.PlaySlideOut(tapped, direction, 4, 4, 1, 1);
            Assert.That(animator.HasActiveAnimations, Is.True);
            Assert.That(tapped.SpriteRenderer.sortingOrder, Is.EqualTo(ViewTheme.SlidingSortingOrder));
            animator.Tick(ViewTheme.SlideDuration / 2);
            Assert.That(tapped != null, Is.True);
            Vector2 unit = new Vector2(direction.GetDx(), direction.GetDy());
            float distance = AnimationMath.SlideDistance(4, 4, 1, 1, direction);
            Assert.That(Vector2.Distance(tapped.Offset, unit * (distance * 0.25f)), Is.LessThan(0.00001f));
            Assert.That(tapped.transform.position, Is.Not.EqualTo(position));
            animator.Tick(ViewTheme.SlideDuration / 2);
            Assert.That(tapped == null, Is.True);
            Assert.That(animator.HasActiveAnimations, Is.False);
        }

        /// <summary>The final slide portion fades out before the object is destroyed.</summary>
        [Test]
        public void SlideFadesNearEnd()
        {
            animator.PlaySlideOut(tapped, Direction.Up, 4, 4, 1, 1);
            animator.Tick(ViewTheme.SlideDuration * 0.85f);
            Assert.That(tapped.Alpha, Is.GreaterThan(0).And.LessThan(1));
        }

        /// <summary>Bump and blocker pulse finish independently and restore exact values.</summary>
        [Test]
        public void BlockedRestoresBothViewsExactly()
        {
            animator.PlayBlocked(tapped, Direction.Up, blocker);
            animator.Tick(ViewTheme.BumpDuration * 0.35f);
            Assert.That(tapped.Offset.y, Is.GreaterThan(0));
            Assert.That(tapped.SpriteRenderer.color, Is.EqualTo(ViewTheme.BlockedTint));
            Assert.That(blocker.SpriteRenderer.color, Is.EqualTo(ViewTheme.BlockerTint));
            Assert.That(blocker.transform.localScale.x, Is.GreaterThan(blockerScale.x));
            animator.Tick(ViewTheme.BumpDuration);
            Assert.That(tapped.Offset, Is.EqualTo(Vector2.zero));
            Assert.That(tapped.transform.position, Is.EqualTo(position));
            Assert.That(tapped.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
            animator.Tick(ViewTheme.FlashDuration);
            Assert.That(blocker.transform.localScale, Is.EqualTo(blockerScale));
            Assert.That(blocker.SpriteRenderer.color, Is.EqualTo(blockerColor));
            Assert.That(animator.HasActiveAnimations, Is.False);
        }

        /// <summary>A new bump starts exactly at the base cell without accumulated offsets.</summary>
        [Test]
        public void RetappingRestartsWithoutDrift()
        {
            for (int i = 0; i < 100; i++)
            {
                animator.PlayBlocked(tapped, Direction.Up, blocker);
                Assert.That(tapped.transform.position, Is.EqualTo(position));
                animator.Tick(0.08f);
            }
            animator.Tick(1);
            Assert.That(tapped.transform.position, Is.EqualTo(position));
            Assert.That(blocker.transform.localScale, Is.EqualTo(blockerScale));
            Assert.That(blocker.SpriteRenderer.color, Is.EqualTo(blockerColor));
        }

        /// <summary>StopAll clears feedback and cancels released slide objects.</summary>
        [Test]
        public void StopAllCancelsEverything()
        {
            animator.PlayBlocked(tapped, Direction.Up, blocker);
            animator.Tick(0.1f);
            animator.StopAll();
            Assert.That(animator.HasActiveAnimations, Is.False);
            Assert.That(tapped.transform.position, Is.EqualTo(position));
            Assert.That(tapped.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
            Assert.That(blocker.transform.localScale, Is.EqualTo(blockerScale));
            animator.PlaySlideOut(tapped, Direction.Up, 4, 4, 1, 1);
            animator.StopAll();
            Assert.That(tapped == null, Is.True);
            Assert.DoesNotThrow(() => animator.Tick(1));
        }

        /// <summary>Destroyed views and their other animation layers are safely pruned.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public void DestroyedViewsAreSafe(bool slide)
        {
            if (slide) animator.PlaySlideOut(tapped, Direction.Up, 4, 4, 1, 1);
            else animator.PlayBlocked(tapped, Direction.Up, blocker);
            Object.DestroyImmediate(tapped.gameObject);
            Assert.DoesNotThrow(() => animator.Tick(1));
            Assert.That(animator.HasActiveAnimations, Is.False);
        }

        /// <summary>A flash finishing reveals a still-active press highlight.</summary>
        [Test]
        public void FlashCompletionPreservesHighlightLayer()
        {
            animator.PlayBlocked(tapped, Direction.Up, blocker);
            blocker.SetHighlight(true);
            animator.Tick(1);
            Assert.That(blocker.SpriteRenderer.color, Is.EqualTo(ViewTheme.HighlightColor));
            Assert.That(blocker.transform.localScale, Is.EqualTo(blockerScale * ViewTheme.HighlightScaleMultiplier));
        }

        /// <summary>A former blocker can exit while its flash is still active, with no stale pulse.</summary>
        [Test]
        public void SlideInterruptsFlashCleanly()
        {
            animator.PlayBlocked(tapped, Direction.Up, blocker);
            animator.Tick(0.1f);
            animator.PlaySlideOut(blocker, Direction.Up, 4, 4, 1, 3);
            Assert.That(blocker.transform.localScale, Is.EqualTo(blockerScale));
            Assert.That(blocker.SpriteRenderer.color, Is.EqualTo(blockerColor));
            animator.Tick(1);
            Assert.That(blocker == null, Is.True);
            Assert.That(animator.HasActiveAnimations, Is.False);
        }
    }
}
