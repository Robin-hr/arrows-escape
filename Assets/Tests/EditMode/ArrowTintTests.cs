using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Equipment tint on both arrow renderers, with exact layer restoration and Ice preservation.</summary>
    public sealed class ArrowTintTests
    {
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void EveryReleasedArrowMeetsContrast(){foreach(var item in ShopCatalog.Items)if(item.Slot==ShopSlot.Arrow)Assert.That(ArrowTint.Contrast(ArrowTint.For(item.Id,ViewTheme.Background),ViewTheme.Background),Is.GreaterThanOrEqualTo(3));Assert.That(ArrowTint.Contrast(ArrowTint.EnsureContrast(Color.black,Color.black),Color.black),Is.GreaterThanOrEqualTo(3));}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void SpritesPathsIceAndClassic()
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.Inventory.Shop.AddCoins(150);scene.Bootstrap.Inventory.TryBuy("frostbite");scene.Bootstrap.Inventory.TryEquip("frostbite");
                var board=new Board(6,6);board.TryAddArrow(new Arrow(1,new[]{new Cell(0,0),new Cell(1,0),new Cell(1,1)},Direction.Up),out _);board.TryAddArrow(2,3,3,Direction.Right,out _);board.TryAddArrow(3,5,5,Direction.Up,ArrowKind.Ice,out _);scene.Game.StartLevel(board);
                Color expected=ArrowTint.For("frostbite",ViewTheme.Background);
                scene.Board.TryGetArrowView(1,out var path);scene.Board.TryGetArrowView(2,out var sprite);scene.Board.TryGetArrowView(3,out var ice);
                Assert.That(path.BaseColor,Is.EqualTo(expected));Assert.That(sprite.BaseColor,Is.EqualTo(expected));Assert.That(ice.BaseColor,Is.EqualTo(ViewTheme.IceArrowColor));
                foreach(var view in new[]{path,sprite})
                {
                    for(int i=0;i<100;i++){view.SetHighlight(true);view.SetHint(Color.green,1.2f);view.SetFlash(Color.red,1.1f);view.ClearFlash();view.SetHighlight(false);view.ClearHint();}
                    Assert.That(view.BaseColor,Is.EqualTo(expected));Assert.That(view.SpriteRenderer.color,Is.EqualTo(expected));
                }
                Color vertex=QualitySettings.activeColorSpace==ColorSpace.Linear?expected.linear:expected;Assert.That(path.Path.GetComponent<MeshFilter>().sharedMesh.colors[0],Is.EqualTo(vertex));
                scene.Bootstrap.Inventory.TryEquip("classic");Assert.That(path.BaseColor,Is.EqualTo(ViewTheme.ArrowColor));Assert.That(sprite.BaseColor,Is.EqualTo(ViewTheme.ArrowColor));Assert.That(ice.BaseColor,Is.EqualTo(ViewTheme.IceArrowColor));
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void UndoRestoredViewKeepsEquipment()
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.Inventory.Shop.AddCoins(150);scene.Bootstrap.Inventory.TryBuy("plasma_bolt");scene.Bootstrap.Inventory.TryEquip("plasma_bolt");scene.Bootstrap.ShowLevel(0);
                scene.Tap(3,0);scene.Game.Undo();scene.Board.TryGetArrowView(4,out var restored);Assert.That(restored.BaseColor,Is.EqualTo(ArrowTint.For("plasma_bolt",ViewTheme.Background)));
            }
        }
    }
}
