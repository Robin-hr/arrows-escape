using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Retirement is an atomic, one-time migration, independent of progress and attempt rewards.</summary>
    public sealed class ElementalRetirementTests
    {
        /// <summary>The reserved id cannot appear in the shop or implemented recipes.</summary>
        [Test] public void CatalogHasFourElementalsAndTwentyArrows()
        {
            Assert.That(ShopCatalog.Find("thunderbolt_dart"),Is.Null);Assert.That(ArrowEscape.View.ArrowSkinRegistry.Find("thunderbolt_dart"),Is.Null);
            Assert.That(ShopCatalog.RetiredItemIds,Does.Contain("thunderbolt_dart"));
            Assert.That(ShopCatalog.Items.Count(x=>x.Slot==ShopSlot.Arrow),Is.EqualTo(20));
            Assert.That(ShopCatalog.Items.Where(x=>x.Category=="Elemental & Magic").Select(x=>x.Id),Is.EqualTo(new[]{"frostbite","inferno_streak","plasma_bolt","void_whisper"}));
        }
        /// <summary>All other ownership, reward and receipt data survives both migration and reload.</summary>
        [Test] public void RefundOnceAndPreserveOtherData()
        {
            var state=new Inventory{Coins=314,EquippedArrowId="thunderbolt_dart",NextAttemptId=12,LastPaidAttemptId=10};state.OwnedItemIds.AddRange(new[]{"thunderbolt_dart","plasma_bolt","future_item"});state.RewardedStars.Add(new LevelRewardRecord{id="level_001",stars=3});
            var store=new FakeInventoryStore{Saved=state};var service=new InventoryService(store);service.Load();
            Assert.That(service.State.Coins,Is.EqualTo(464));Assert.That(service.State.EquippedArrowId,Is.EqualTo("classic"));Assert.That(service.State.OwnedItemIds,Does.Not.Contain("thunderbolt_dart"));Assert.That(service.State.OwnedItemIds,Does.Contain("future_item").And.Contain("plasma_bolt"));Assert.That(service.State.NextAttemptId,Is.EqualTo(12));Assert.That(service.State.LastPaidAttemptId,Is.EqualTo(10));Assert.That(service.State.RewardedStars.Single().stars,Is.EqualTo(3));Assert.That(store.Saves,Is.EqualTo(1));
            service.Load();Assert.That(service.State.Coins,Is.EqualTo(464));Assert.That(store.Saves,Is.EqualTo(1));Assert.That(service.State.RefundedRetiredItemIds,Is.EqualTo(new[]{"thunderbolt_dart"}));
        }
        /// <summary>Failed writes retain the original on disk; retry refunds the original exactly once.</summary>
        [Test] public void FailedMigrationDoesNotLoseOrDuplicateCoins()
        {
            var state=new Inventory{Coins=20};state.OwnedItemIds.Add("thunderbolt_dart");var store=new FakeInventoryStore{Saved=state,FailSave=true};var service=new InventoryService(store,_=>{});service.Load();Assert.That(service.LastSaveFailed,Is.True);Assert.That(store.Saved.Coins,Is.EqualTo(20));Assert.That(store.Saved.OwnedItemIds,Does.Contain("thunderbolt_dart"));store.FailSave=false;service.Load();Assert.That(store.Saved.Coins,Is.EqualTo(170));service.Load();Assert.That(service.State.Coins,Is.EqualTo(170));
        }
        /// <summary>A forged duplicate ownership entry cannot cause a second refund.</summary>
        [Test] public void RecordedRefundAndDuplicateOwnershipAreSafe()
        {
            var state=new Inventory{Coins=42};state.OwnedItemIds.AddRange(new[]{"thunderbolt_dart","thunderbolt_dart"});state.RefundedRetiredItemIds.Add("thunderbolt_dart");Assert.That(state.ApplyRetirements(),Is.True);Assert.That(state.Coins,Is.EqualTo(42));Assert.That(state.OwnedItemIds,Does.Not.Contain("thunderbolt_dart"));
        }
        /// <summary>Old payloads require no new version and default to an empty receipt list.</summary>
        [Test] public void LegacyInventoryAndCoinCap()
        {
            var state=JsonUtility.FromJson<Inventory>("{\"version\":1,\"Coins\":9999990,\"OwnedItemIds\":[\"classic\",\"paper\",\"thunderbolt_dart\"]}");Assert.That(state.ApplyRetirements(),Is.True);Assert.That(state.Coins,Is.EqualTo(EconomyConfig.MaxCoins));Assert.That(state.version,Is.EqualTo(1));Assert.That(JsonUtility.FromJson<Inventory>(JsonUtility.ToJson(state)).RefundedRetiredItemIds,Does.Contain("thunderbolt_dart"));
        }
        /// <summary>The wallet cap defers refund overflow instead of discarding paid currency.</summary>
        [Test] public void RefundOverflowSurvivesUntilSpending()
        {
            var state=new Inventory{Coins=EconomyConfig.MaxCoins};state.OwnedItemIds.Add("thunderbolt_dart");state.ApplyRetirements();Assert.That(state.PendingRetirementCoins,Is.EqualTo(150));state=JsonUtility.FromJson<Inventory>(JsonUtility.ToJson(state));var shop=new ShopService(state);Assert.That(shop.TryBuy("frostbite"),Is.True);Assert.That(state.Coins,Is.EqualTo(EconomyConfig.MaxCoins));Assert.That(state.PendingRetirementCoins,Is.Zero);Assert.That(state.ApplyRetirements(),Is.False);
        }
    }
}
