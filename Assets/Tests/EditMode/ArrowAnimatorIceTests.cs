using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Tick-driven ice motion is continuous and ends exactly at the new base.</summary>
    public class ArrowAnimatorIceTests
    {
        private AudioTestScene scene;private ArrowView view;private ArrowAnimator animator;
        /// <summary>Checks setup with isolated ice-arrow state.</summary>
        [SetUp]public void Setup(){scene=new AudioTestScene(IceTestBoards.Cycle());scene.Host.GetComponent<BoardView>().TryGetArrowView(1,out view);animator=scene.Host.GetComponent<ArrowAnimator>();}
        /// <summary>Checks cleanup with isolated ice-arrow state.</summary>
        [TearDown]public void Cleanup()=>scene.Dispose();
        /// <summary>Checks tick and finish with isolated ice-arrow state.</summary>
        [Test]public void TickAndFinish()
        {
            view.SetOffset(Vector2.left*2);animator.PlayIceSlide(view,.2f);Assert.That(view.Offset.x,Is.EqualTo(-2));Assert.That(animator.HasActiveAnimations,Is.True);
            animator.Tick(.1f);Assert.That(view.Offset.x,Is.InRange(-1.99f,-.001f));animator.Tick(.1f);Assert.That(view.Offset,Is.EqualTo(Vector2.zero));Assert.That(animator.HasActiveAnimations,Is.False);
        }
        /// <summary>Checks interrupt continuous with isolated ice-arrow state.</summary>
        [Test]public void InterruptContinuous()
        {
            view.SetOffset(Vector2.left*2);animator.PlayIceSlide(view,.2f);animator.Tick(.08f);Vector2 current=view.CurrentWorldPosition;
            view.Rebase(new Arrow(1,3,0,Direction.Right,ArrowKind.Ice),view.BaseWorldPosition+Vector2.right,true);
            animator.PlayIceSlide(view,.3f);Assert.That(Vector2.Distance(current,view.CurrentWorldPosition),Is.LessThan(.001f));animator.Tick(.3f);Assert.That(view.Offset,Is.EqualTo(Vector2.zero));
        }
        /// <summary>Checks exit continuous with isolated ice-arrow state.</summary>
        [TestCase(0f)][TestCase(-1.3f)]public void ExitContinuous(float offset)
        {
            view.SetOffset(Vector2.right*offset);Vector2 current=view.CurrentWorldPosition;animator.PlaySlideOut(view,Direction.Right,4,3,0,0);
            Assert.That(view.CurrentWorldPosition,Is.EqualTo(current));animator.Tick(.1f);Assert.That(view.CurrentWorldPosition.x,Is.GreaterThan(current.x));
        }
        /// <summary>Checks cancel and destroyed with isolated ice-arrow state.</summary>
        [Test]public void CancelAndDestroyed()
        {
            view.SetOffset(Vector2.left);animator.PlayIceSlide(view,.2f);animator.Cancel(view);Vector2 offset=view.Offset;animator.Tick(1);Assert.That(view.Offset,Is.EqualTo(offset));Assert.That(animator.HasActiveAnimations,Is.False);
            animator.PlayIceSlide(view,.2f);Object.DestroyImmediate(view.gameObject);Assert.DoesNotThrow(()=>animator.Tick(1));Assert.That(animator.HasActiveAnimations,Is.False);
        }
    }
}
