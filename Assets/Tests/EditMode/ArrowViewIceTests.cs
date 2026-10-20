using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Ice tint layers and immutable-position rebasing.</summary>
    public class ArrowViewIceTests
    {
        private AudioTestScene scene;private ArrowView view;
        /// <summary>Checks setup with isolated ice-arrow state.</summary>
        [SetUp] public void Setup(){scene=new AudioTestScene(IceTestBoards.Cycle());scene.Host.GetComponent<BoardView>().TryGetArrowView(1,out view);}
        /// <summary>Checks cleanup with isolated ice-arrow state.</summary>
        [TearDown] public void Cleanup()=>scene.Dispose();
        /// <summary>Checks layers restore ice with isolated ice-arrow state.</summary>
        [Test] public void LayersRestoreIce()
        {
            Vector3 position=view.transform.position,scale=view.transform.localScale;Color color=view.SpriteRenderer.color;
            Assert.That(color,Is.EqualTo(ViewTheme.IceArrowColor));
            for(int i=0;i<100;i++)
            {
                view.SetHint(Color.green,1.2f);view.SetHighlight(true);view.SetFlash(Color.red,1.1f);
                view.ClearFlash();view.SetHighlight(false);view.ClearHint();
                Assert.That(view.transform.position,Is.EqualTo(position));Assert.That(view.transform.localScale,Is.EqualTo(scale));Assert.That(view.SpriteRenderer.color,Is.EqualTo(color));
            }
            view.SetFlash(Color.red,2);view.ResetVisual();Assert.That(view.SpriteRenderer.color,Is.EqualTo(color));
        }
        /// <summary>Checks rebase continuity with isolated ice-arrow state.</summary>
        [Test] public void RebaseContinuity()
        {
            Vector2 current=view.CurrentWorldPosition;var moved=new Arrow(1,2,0,Direction.Right,ArrowKind.Ice);
            view.Rebase(moved,view.BaseWorldPosition+Vector2.right*2,true);
            Assert.That(view.Arrow,Is.SameAs(moved));Assert.That(view.CurrentWorldPosition,Is.EqualTo(current));
            Assert.That(view.Offset,Is.EqualTo(Vector2.left*2));
            view.Rebase(moved,Vector2.one,false);Assert.That(view.Offset,Is.EqualTo(Vector2.zero));Assert.That(view.CurrentWorldPosition,Is.EqualTo(Vector2.one));
        }
    }
}
