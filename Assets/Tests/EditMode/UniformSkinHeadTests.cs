using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Every skin wears the classic head (same sprite, size and placement); only the body differs (Step 14I).</summary>
    public sealed class UniformSkinHeadTests
    {
        private static readonly string[] ids = ArrowSkinRegistry.All.Select(s => s.Id).ToArray();
        private static readonly string[] skinHeads = { "Elemental SDF head", "Inferno SDF flame head", "Reference head", "Reference tail", "Frozen crystal fletching" };

        [TestCaseSource(nameof(ids))] public void SkinUsesClassicHeadAndNoOwnCaps(string id)
        {
            using (var s = new EconomyTestScene())
            {
                var board = new Board(8, 8);
                Assert.That(board.TryAddArrow(new Arrow(1, new[] { new Cell(1, 1), new Cell(2, 1), new Cell(3, 1), new Cell(3, 2) }, Direction.Up), out _), Is.True);
                s.Game.StartLevel(board);
                Assert.That(s.Board.TryGetArrowView(1, out ArrowView classic), Is.True);
                Vector3 classicScale = classic.Path.HeadArt.transform.lossyScale; Vector3 classicTip = classic.Path.HeadArt.transform.position;
                s.Board.SetArrowSkin(id, false);
                Assert.That(s.Board.TryGetArrowView(1, out ArrowView view), Is.True);
                var head = view.Skin.UniformHead;
                Assert.That(head, Is.Not.Null, id); Assert.That(head.enabled, Is.True, id);
                Assert.That(head.sprite, Is.SameAs(view.Path.HeadArt.sprite), id);
                Assert.That(Vector3.Distance(head.transform.lossyScale, classicScale), Is.LessThan(1e-4f), id);
                Assert.That(Vector2.Distance(head.transform.position, classicTip), Is.LessThan(1e-3f), id);
                Assert.That(view.Path.HeadArt.enabled, Is.False, "the classic ribbon is hidden under the skin");
                foreach (var renderer in view.GetComponentsInChildren<Renderer>())
                    if (skinHeads.Contains(renderer.name)) Assert.That(renderer.enabled, Is.False, id + " " + renderer.name);
            }
        }

        [Test] public void DarkSkinsGetALiftedHeadColour()
        {
            foreach (var skin in ArrowSkinRegistry.All)
            {
                Color c = SkinVisual.HeadColor(skin);
                Assert.That(c.r * .3f + c.g * .59f + c.b * .11f, Is.GreaterThan(.12f), skin.Id);
            }
        }
    }
}
