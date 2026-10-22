using System.Linq;
using ArrowEscape.Core;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Real cards, sheet, purchase confirmation, Back ordering and cached previews.</summary>
    public sealed class ShopUiTests
    {
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void CardStatesAndNeedMore()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                scene.Flow.MainMenu.ShopButton.onClick.Invoke();var shop=scene.Flow.Shop;
                Assert.That(shop.IsVisible,Is.True);Assert.That(shop.Cards[0].StateText,Is.EqualTo("Equipped"));Assert.That(shop.Cards[1].StateText,Is.EqualTo("150"));
                shop.OpenDetail(1);Assert.That(shop.Detail.ActionText,Is.EqualTo("Need 150 more"));Assert.That(shop.Detail.MainButton.interactable,Is.False);
                scene.Bootstrap.Inventory.Shop.AddCoins(300);Assert.That(shop.Detail.ActionText,Is.EqualTo("Buy - 150"));
                shop.ThemesButton.onClick.Invoke();Assert.That(shop.Cards[0].StateText,Is.EqualTo("Equipped"));shop.OpenDetail(1);Assert.That(shop.Detail.ActionText,Is.EqualTo("Buy - 200"));Assert.That(shop.Detail.MainButton.interactable,Is.True);
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void ConfirmBuyThenEquipAndPersist()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                scene.Bootstrap.Inventory.Shop.AddCoins(300);scene.Flow.OpenShop();var shop=scene.Flow.Shop;shop.OpenDetail(1);shop.Detail.MainButton.onClick.Invoke();
                Assert.That(scene.Flow.Confirm.IsOpen,Is.True);Assert.That(scene.Bootstrap.Inventory.State.Coins,Is.EqualTo(300));
                scene.Flow.Confirm.ConfirmButton.onClick.Invoke();Assert.That(scene.Bootstrap.Inventory.State.Coins,Is.EqualTo(150));Assert.That(shop.Detail.ActionText,Is.EqualTo("Equip"));Assert.That(shop.Cards[1].StateText,Is.EqualTo("Owned"));
                shop.Detail.MainButton.onClick.Invoke();Assert.That(shop.Detail.ActionText,Is.EqualTo("Equipped"));Assert.That(scene.Inventory.Saved.EquippedArrowId,Is.EqualTo("frostbite"));Assert.That(scene.Inventory.Saves,Is.EqualTo(2));
                var counter=shop.CoinsText.GetComponent<ArrowEscape.UI.CoinCounter>();counter.Tick(1);Assert.That(shop.CoinsText.text,Is.EqualTo("150"));
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void BackClosesConfirmThenSheetThenShop()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                scene.Bootstrap.Inventory.Shop.AddCoins(1000);scene.Flow.OpenShop();scene.Flow.Shop.OpenDetail(1);scene.Flow.Shop.Detail.MainButton.onClick.Invoke();
                scene.Flow.HandleBack();Assert.That(scene.Flow.Confirm.IsOpen,Is.False);Assert.That(scene.Flow.Shop.Detail.IsVisible,Is.True);
                scene.Flow.HandleBack();Assert.That(scene.Flow.Shop.Detail.IsVisible,Is.False);Assert.That(scene.Flow.Shop.IsVisible,Is.True);
                scene.Flow.HandleBack();Assert.That(scene.Flow.Shop.IsVisible,Is.False);Assert.That(scene.Flow.MainMenu.Root.gameObject.activeSelf,Is.True);
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void PreviewBudgetAndDestroyedCache()
        {
            using(var renderer=new ArrowEscape.View.ArrowPreviewRenderer())
            {
                renderer.BeginFrame();foreach(var item in ShopCatalog.Items)renderer.Request(item.Id);renderer.Tick();Assert.That(renderer.LastRenderCount,Is.EqualTo(2));
                var old=renderer.GetCached("classic");Assert.That(old.width,Is.EqualTo(128));Assert.That(old.height,Is.EqualTo(128));Object.DestroyImmediate(old);renderer.BeginFrame();renderer.Request("classic");renderer.Tick();Assert.That(renderer.GetCached("classic"),Is.Not.Null);
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void WarmMenuAndShopTickAllocateNothing()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                scene.Flow.MainMenu.Tick(.01f);long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<30;i++)scene.Flow.MainMenu.Tick(.001f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                scene.Flow.OpenShop();for(int i=0;i<30;i++)scene.Flow.Shop.Tick(.016f);
                before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<30;i++)scene.Flow.Shop.Tick(.001f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            }
        }
    }
}
