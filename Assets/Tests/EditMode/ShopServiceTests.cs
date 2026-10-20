using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Ownership, prices, equipment and capped wallet rules.</summary>
    public sealed class ShopServiceTests
    {
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void Defaults(){var shop=new ShopService();Assert.That(shop.Inventory.Coins,Is.Zero);Assert.That(shop.IsOwned("classic")&&shop.IsOwned("paper"),Is.True);Assert.That(shop.IsEquipped("classic")&&shop.IsEquipped("paper"),Is.True);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void BuyAndEquipFireOnce()
        {
            var shop=new ShopService(new Inventory{Coins=300});int coins=0,buys=0,equips=0;shop.CoinsChanged+=_=>coins++;shop.ItemPurchased+=_=>buys++;shop.EquippedChanged+=(slot,id)=>{Assert.That(slot,Is.EqualTo(ShopSlot.Arrow));Assert.That(id,Is.EqualTo("plasma_bolt"));equips++;};
            Assert.That(shop.TryBuy("plasma_bolt"),Is.True);Assert.That(shop.Inventory.Coins,Is.EqualTo(150));Assert.That(shop.TryBuy("plasma_bolt"),Is.False);Assert.That(shop.TryEquip("plasma_bolt"),Is.True);Assert.That(shop.TryEquip("plasma_bolt"),Is.True);
            Assert.That(coins,Is.EqualTo(1));Assert.That(buys,Is.EqualTo(1));Assert.That(equips,Is.EqualTo(1));Assert.That(shop.Inventory.EquippedThemeId,Is.EqualTo("paper"));
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void FailuresNeverSpend(){var shop=new ShopService(new Inventory{Coins=149});Assert.That(shop.TryBuy("plasma_bolt"),Is.False);Assert.That(shop.TryBuy("unknown"),Is.False);Assert.That(shop.TryBuy("paper"),Is.False);Assert.That(shop.TryBuy("mystic_wood"),Is.False);Assert.That(shop.TryEquip("plasma_bolt"),Is.False);Assert.That(shop.TryEquip(ShopSlot.Theme,"classic"),Is.False);Assert.That(shop.Inventory.Coins,Is.EqualTo(149));}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void CapAndNegativeProtection(){var shop=new ShopService(new Inventory{Coins=EconomyConfig.MaxCoins});shop.AddCoins(int.MaxValue);Assert.That(shop.Inventory.Coins,Is.EqualTo(EconomyConfig.MaxCoins));Assert.That(shop.AwardLevelClear("a",3),Is.Zero);Assert.That(shop.Inventory.LastPaidAttemptId,Is.GreaterThan(0));Assert.Throws<ArgumentOutOfRangeException>(()=>shop.AddCoins(-1));Assert.That(shop.CanAfford(-1),Is.False);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void ReceiptAndFreshReplay(){var shop=new ShopService();int first=shop.BeginAttempt();Assert.That(first,Is.EqualTo(1));Assert.That(shop.AwardLevelClear("a",1,first),Is.EqualTo(20));Assert.That(shop.AwardLevelClear("a",3,first),Is.Zero);int replay=shop.BeginAttempt();Assert.That(replay,Is.GreaterThan(first));Assert.That(shop.AwardLevelClear("a",3,replay),Is.EqualTo(25));Assert.That(shop.Inventory.NextAttemptId,Is.GreaterThan(replay));}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void UnknownOwnershipIsRetainedButInactive(){var state=new Inventory();state.OwnedItemIds.Add("future_arrow");var shop=new ShopService(state);Assert.That(state.OwnedItemIds,Does.Contain("future_arrow"));Assert.That(shop.IsOwned("future_arrow"),Is.False);Assert.That(shop.TryEquip("future_arrow"),Is.False);}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void ReleasedCatalogPurchasesStayNonnegative(){var shop=new ShopService(new Inventory{Coins=10000});foreach(var item in ShopCatalog.Items)if(item.IsAvailable&&!shop.IsOwned(item.Id)){Assert.That(shop.TryBuy(item.Id),Is.True);Assert.That(shop.Inventory.Coins,Is.GreaterThanOrEqualTo(0));}}
    }
}
