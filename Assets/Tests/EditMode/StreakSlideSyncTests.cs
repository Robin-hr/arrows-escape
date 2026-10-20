using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Compares independent streak math against a real sliding ArrowView.</summary>
    public class StreakSlideSyncTests
    {
        /// <summary>Uneven deltas stay synchronized in every direction on a non-square board.</summary>
        [TestCase(Direction.Up)][TestCase(Direction.Down)][TestCase(Direction.Left)][TestCase(Direction.Right)]public void InSync(Direction direction)
        {
            var board=new Board(3,7);board.TryAddArrow(1,1,3,direction,out var arrow);
            using(var s=new EffectsTestScene(board)) {
                s.View.ReleaseArrowView(1,out var view);var animator=s.Gameplay.Host.GetComponent<ArrowAnimator>();animator.PlaySlideOut(view,direction,3,7,1,3);
                s.Effects.PlayExit(arrow,3,7,1);foreach(float dt in new[]{.013f,.027f,.041f,.019f,.051f,.067f}) {
                    animator.Tick(dt);s.Effects.Tick(dt);Assert.That(s.Effects.Streaks.TryGetHeadWorld(1,out Vector2 head),Is.True);Assert.That(Vector2.Distance(head,view.transform.position),Is.LessThan(.001));
                }
                animator.Tick(1);s.Effects.Tick(1);Assert.That(view==null,Is.True);Assert.That(s.Effects.Streaks.ActiveCount,Is.Zero);
            }
        }
    }
}
