using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Verifies selective animation cancellation does not destroy restored views.</summary>
    public class ArrowAnimatorCancelTests
    {
        private GameObject host; private ArrowAnimator animator; private BoardView view;
        /// <summary>Creates two independent views.</summary>
        [SetUp] public void SetUp() { host = new GameObject(); view = host.AddComponent<BoardView>(); animator = host.AddComponent<ArrowAnimator>(); var board = new Board(2, 2); board.TryAddArrow(1, 0, 0, Direction.Up, out _); board.TryAddArrow(2, 1, 0, Direction.Up, out _); view.Show(board); }
        /// <summary>Removes runtime objects.</summary>
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); SpriteFactory.ClearCache(); }
        /// <summary>A cancelled slide remains alive and stationary through later ticks.</summary>
        [Test] public void CancelSlide() { view.TryGetArrowView(1, out ArrowView a); animator.PlaySlideOut(a, Direction.Up, 2, 2, 0, 0); animator.Tick(0.1f); animator.Cancel(a); Vector3 position = a.transform.position; animator.Tick(10); Assert.That(a != null, Is.True); Assert.That(a.transform.position, Is.EqualTo(position)); Assert.That(animator.HasActiveAnimations, Is.False); }
        /// <summary>Null and destroyed references are safe.</summary>
        [Test] public void Missing() { Assert.DoesNotThrow(() => animator.Cancel(null)); view.TryGetArrowView(1, out ArrowView a); animator.PlaySlideOut(a, Direction.Up, 2, 2, 0, 0); Object.DestroyImmediate(a.gameObject); Assert.DoesNotThrow(() => animator.Cancel(a)); Assert.That(animator.HasActiveAnimations, Is.False); }
        /// <summary>Other slides continue to completion.</summary>
        [Test] public void Independent() { view.TryGetArrowView(1, out ArrowView a); view.TryGetArrowView(2, out ArrowView b); animator.PlaySlideOut(a, Direction.Up, 2, 2, 0, 0); animator.PlaySlideOut(b, Direction.Up, 2, 2, 1, 0); animator.Cancel(a); animator.Tick(1); Assert.That(a != null, Is.True); Assert.That(b == null, Is.True); }
    }
}
