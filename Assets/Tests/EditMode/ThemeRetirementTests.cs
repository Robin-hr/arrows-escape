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
            var state=new Inventory{Coins=100,EquippedThemeId="cosmic_nebula"};state.OwnedItemIds.AddRange(new[]{"mystic_wood","cosmic_nebula","grad_rose"});
            Assert.That(state.ApplyRetirements(),Is.True);
            Assert.That(state.Coins,Is.EqualTo(100+200+800));Assert.That(state.EquippedThemeId,Is.EqualTo("paper"));
            Assert.That(state.OwnedItemIds,Does.Not.Contain("mystic_wood"));Assert.That(state.OwnedItemIds,Does.Not.Contain("cosmic_nebula"));Assert.That(state.OwnedItemIds,Does.Contain("grad_rose"));
            state.OwnedItemIds.Add("cosmic_nebula");state.ApplyRetirements();Assert.That(state.Coins,Is.EqualTo(1100));
        }
        /// <summary>The catalog offers six gradients and three animated premium wallpapers; retired ids are gone.</summary>
        [Test] public void CatalogHasNewThemes()
        {
            foreach(string id in new[]{"mystic_wood","frozen_cavern","pharaohs_tomb","volcanic_underworld","neon_cyberpunk","cosmic_nebula"}){Assert.That(ShopCatalog.Find(id),Is.Null);Assert.That(ShopCatalog.RetiredItemIds,Does.Contain(id));}
            int gradients=0,premium=0;foreach(var item in ShopCatalog.Items){if(item.Slot!=ShopSlot.Theme)continue;if(item.Category=="Gradients")gradients++;if(item.Category=="Live Wallpapers")premium++;}
            Assert.That(gradients,Is.EqualTo(6));Assert.That(premium,Is.EqualTo(3));
            for(int i=0;i<ArrowEscape.View.ThemeDesign.Count;i++)Assert.That(ArrowEscape.View.ThemeDesign.IsAnimated(i),Is.EqualTo(i>=6));
        }
    }
}
