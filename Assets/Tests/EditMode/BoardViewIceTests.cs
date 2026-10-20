using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Board views preserve kind, shared identity and one view per id.</summary>
    public class BoardViewIceTests
    {
        private AudioTestScene scene;
        /// <summary>Checks setup with isolated ice-arrow state.</summary>
        [SetUp]public void Setup()=>scene=new AudioTestScene(IceTestBoards.Cycle());
        /// <summary>Checks cleanup with isolated ice-arrow state.</summary>
        [TearDown]public void Cleanup()=>scene.Dispose();
        /// <summary>Checks kind sprites with isolated ice-arrow state.</summary>
        [Test]public void KindSprites()
        {
            var boardView=scene.Host.GetComponent<BoardView>();boardView.TryGetArrowView(1,out var ice);boardView.TryGetArrowView(2,out var normal);
            Assert.That(ice.SpriteRenderer.sprite,Is.SameAs(SpriteFactory.GetIceArrowSprite()));Assert.That(ice.BaseColor,Is.EqualTo(ViewTheme.IceArrowColor));
            Assert.That(normal.SpriteRenderer.sprite,Is.SameAs(SpriteFactory.GetArrowSprite()));Assert.That(normal.BaseColor,Is.EqualTo(ViewTheme.ArrowColor));
            var properties=new MaterialPropertyBlock();ice.SpriteRenderer.GetPropertyBlock(properties);Assert.That(properties.GetTexture("_MainTex"),Is.SameAs(ice.SpriteRenderer.sprite.texture));
            Assert.That(boardView.AddArrowView(ice.Arrow).BaseColor,Is.EqualTo(ViewTheme.IceArrowColor));
            Assert.That(scene.Host.GetComponentsInChildren<ArrowView>().Length,Is.EqualTo(4));
        }
        /// <summary>Checks rebase uses same view with isolated ice-arrow state.</summary>
        [Test]public void RebaseUsesSameView()
        {
            var board=scene.Game.Session.Board;var boardView=scene.Host.GetComponent<BoardView>();boardView.TryGetArrowView(1,out var original);
            Vector3 position=original.transform.position;board.TryMoveArrow(board.GetArrowById(1),2,0,out var moved);
            Assert.That(boardView.RebaseArrowView(moved,true,out var result),Is.True);Assert.That(result,Is.SameAs(original));Assert.That(result.Arrow,Is.SameAs(moved));Assert.That(result.transform.position,Is.EqualTo(position));
            Assert.That(result.BaseWorldPosition,Is.EqualTo((Vector2)boardView.CellToWorld(2,0)));
            Assert.That(boardView.RebaseArrowView(new Arrow(99,0,0,Direction.Up),true,out _),Is.False);
        }
    }
}
