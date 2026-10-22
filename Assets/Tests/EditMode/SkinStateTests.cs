using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Every skin returns to its exact rest layers after repeated overlapping feedback.</summary>
    public sealed class SkinStateTests
    {
        /// <summary>Rest geometry, state adapter and shader overlay recover without cumulative changes.</summary>
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
        public void HundredCycles(int index)
        {
            var host=new GameObject("skin states");try
            {
                var board=host.AddComponent<BoardView>();var model=new Board(6,5);model.TryAddArrow(new Arrow(1,new[]{new Cell(0,0),new Cell(1,0),new Cell(1,1)},Direction.Up),out _);board.Show(model);board.SetArrowSkin(ArrowSkinRegistry.All[index].Id);board.TryGetArrowView(1,out var view);var position=view.transform.position;var scale=view.transform.localScale;var color=view.SpriteRenderer.color;var vertices=view.Path.RibbonMesh.vertices;
                for(int i=0;i<100;i++){view.SetHint(Color.green,1.18f);view.SetHighlight(true);view.SetFlash(Color.red,1.15f);view.ClearFlash();view.SetHighlight(false);view.ClearHint();}
                Assert.That(view.transform.position,Is.EqualTo(position));Assert.That(view.transform.localScale,Is.EqualTo(scale));Assert.That(view.SpriteRenderer.color,Is.EqualTo(color));Assert.That(view.Path.RibbonMesh.vertices,Is.EqualTo(vertices));
                var block=new MaterialPropertyBlock();foreach(var renderer in view.Skin.GetComponentsInChildren<MeshRenderer>()){renderer.GetPropertyBlock(block);Assert.That(block.GetFloat("_State"),Is.Zero);}
            }finally{Object.DestroyImmediate(host);}
        }
    }
}
