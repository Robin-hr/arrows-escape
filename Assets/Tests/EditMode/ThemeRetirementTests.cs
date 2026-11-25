using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14J replaces the scenic themes with gradient and geometric wallpapers and refunds owners once.</summary>
    public sealed class ThemeRetirementTests
    {
        /// <summary>Owned scenic themes are refunded at their price, removed, and the equipped theme falls back to Paper.</summary>
        [Test] public void ScenicThemesRefundOnce()
        {
            var state=new Inventory{Coins=100,EquippedThemeId="cosmic_nebula"};state.OwnedItemIds.AddRange(new[]{"mystic_wood","cosmic_nebula","classic"});
            Assert.That(state.ApplyRetirements(),Is.True);
            Assert.That(state.Coins,Is.EqualTo(100+200+800));Assert.That(state.EquippedThemeId,Is.EqualTo("paper"));
            Assert.That(state.OwnedItemIds,Does.Not.Contain("mystic_wood"));Assert.That(state.OwnedItemIds,Does.Not.Contain("cosmic_nebula"));Assert.That(state.OwnedItemIds,Does.Contain("classic"));
            state.OwnedItemIds.Add("cosmic_nebula");state.ApplyRetirements();Assert.That(state.Coins,Is.EqualTo(1100));
        }
        /// <summary>Step 16: every background theme is retired; owners get their coins back once and boards use solid level colours.</summary>
        [Test] public void BackgroundThemesRefundOnce()
        {
            var state=new Inventory{Coins=10,EquippedThemeId="geo_neon"};state.OwnedItemIds.AddRange(new[]{"grad_rose","geo_neon"});
            Assert.That(state.ApplyRetirements(),Is.True);Assert.That(state.Coins,Is.EqualTo(10+400+1200));Assert.That(state.EquippedThemeId,Is.EqualTo("paper"));
            Assert.That(state.OwnedItemIds,Does.Not.Contain("grad_rose"));state.OwnedItemIds.Add("grad_rose");state.ApplyRetirements();Assert.That(state.Coins,Is.EqualTo(1610),"never refunded twice");
            foreach(var item in ShopCatalog.Items)if(item.Slot==ShopSlot.Theme)Assert.That(item.Id,Is.EqualTo("paper"));
        }
    }
}
