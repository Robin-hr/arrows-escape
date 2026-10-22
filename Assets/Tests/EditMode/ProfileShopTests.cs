using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.UI;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Avatars, frames, the developer coin grant and the neon restyle of every screen (Step 14F).</summary>
    public sealed class ProfileShopTests
    {
        [Test] public void AvatarsAndFramesAreFreeByDefaultAndEquipPerSlot()
        {
            var shop = new ShopService(new Inventory { Coins = 1000 });
            Assert.That(shop.IsEquipped(Inventory.DefaultAvatarId), Is.True); Assert.That(shop.IsEquipped(Inventory.DefaultFrameId), Is.True);
            Assert.That(shop.TryBuy("avatar_fox"), Is.True); Assert.That(shop.TryEquip("avatar_fox"), Is.True);
            Assert.That(shop.Inventory.EquippedAvatarId, Is.EqualTo("avatar_fox")); Assert.That(shop.Inventory.EquippedArrowId, Is.EqualTo("classic"));
            Assert.That(shop.IsEquipped(Inventory.DefaultAvatarId), Is.False); Assert.That(shop.IsEquipped(Inventory.DefaultFrameId), Is.True);
            Assert.That(shop.TryEquip(ShopSlot.Frame, "avatar_fox"), Is.False);
            Assert.That(shop.TryBuy("frame_gold"), Is.True); Assert.That(shop.TryEquip(ShopSlot.Frame, "frame_gold"), Is.True);
            Assert.That(shop.Inventory.EquippedFrameId, Is.EqualTo("frame_gold")); Assert.That(shop.Inventory.Coins, Is.EqualTo(400));
        }

        [Test] public void SanitizeRepairsAvatarAndFrameSlots()
        {
            var inventory = new Inventory { EquippedAvatarId = "avatar_dragon", EquippedFrameId = "frame_bronze_x" };
            inventory.OwnedItemIds.Remove(Inventory.DefaultAvatarId); inventory.OwnedItemIds.Remove(Inventory.DefaultFrameId);
            inventory.Sanitize();
            Assert.That(inventory.EquippedAvatarId, Is.EqualTo(Inventory.DefaultAvatarId)); Assert.That(inventory.EquippedFrameId, Is.EqualTo(Inventory.DefaultFrameId));
            Assert.That(inventory.OwnedItemIds, Does.Contain(Inventory.DefaultAvatarId).And.Contain(Inventory.DefaultFrameId));
        }

        [Test] public void EveryAvatarAndFrameHasArt()
        {
            foreach (var item in ShopCatalog.Items.Where(i => i.Slot == ShopSlot.Avatar)) Assert.That(AvatarBadge.AvatarTexture(item.Id), Is.Not.Null, item.Id);
            foreach (var item in ShopCatalog.Items.Where(i => i.Slot == ShopSlot.Frame)) Assert.That(AvatarBadge.FrameTexture(item.Id), Is.Not.Null, item.Id);
        }

        [Test] public void TapStreakNeedsFiveQuickTaps()
        {
            var streak = new TapStreak(5, 0.8f);
            for (int i = 0; i < 4; i++) Assert.That(streak.Tap(i * 0.3f), Is.False);
            Assert.That(streak.Tap(1.2f), Is.True); Assert.That(streak.Count, Is.Zero);
            for (int i = 0; i < 4; i++) streak.Tap(10 + i * 0.3f);
            Assert.That(streak.Tap(13f), Is.False, "a long pause starts over");
        }

        [Test] public void FiveCoinTapsGrantDeveloperCoinsOnce()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                float now = 0; scene.Flow.Clock = () => now;
                scene.Flow.MainMenu.CoinsButton.onClick.Invoke();
                Assert.That(scene.Flow.Shop.IsVisible, Is.True, "first tap still opens the shop");
                for (int i = 0; i < 4; i++) { now += 0.25f; scene.Flow.Shop.CoinsButton.onClick.Invoke(); }
                Assert.That(scene.Bootstrap.Inventory.State.Coins, Is.EqualTo(EconomyConfig.DeveloperGrant));
                Assert.That(scene.Inventory.Saved.Coins, Is.EqualTo(EconomyConfig.DeveloperGrant));
                Assert.That(scene.Flow.Toast.IsVisible, Is.True); Assert.That(scene.Flow.Toast.Message.text, Does.Contain("50,000"));
                now += 0.25f; scene.Flow.Shop.CoinsButton.onClick.Invoke();
                Assert.That(scene.Bootstrap.Inventory.State.Coins, Is.EqualTo(EconomyConfig.DeveloperGrant));
                scene.Flow.Toast.Tick(3); Assert.That(scene.Flow.Toast.IsVisible, Is.False);
            }
        }

        [Test] public void ShopHasFourTabsWithAvatarAndFrameCards()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.OpenShop(); var shop = scene.Flow.Shop;
                Assert.That(shop.Tab, Is.EqualTo(ShopSlot.Arrow));
                shop.AvatarsButton.onClick.Invoke();
                Assert.That(shop.Cards.Count, Is.EqualTo(12)); Assert.That(shop.Cards.All(c => c.IsPortrait && c.Badge != null), Is.True);
                Assert.That(shop.Cards[0].StateText, Is.EqualTo("Equipped")); Assert.That(shop.Cards[1].StateText, Is.EqualTo("100"));
                shop.FramesButton.onClick.Invoke();
                Assert.That(shop.Cards.Count, Is.EqualTo(10)); Assert.That(shop.Cards[0].Badge.FrameId, Is.EqualTo("frame_bronze"));
                for (int i = 0; i < 5; i++) shop.Tick(0.016f);
                Assert.That(shop.Cards[1].Badge.FrameImage.texture, Is.Not.Null);
            }
        }

        [Test] public void BuyingAvatarUpdatesHomeBadgeAndProfile()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Bootstrap.Inventory.Shop.AddCoins(1000);
                scene.Flow.OpenShop(); var shop = scene.Flow.Shop; shop.AvatarsButton.onClick.Invoke();
                shop.OpenDetail(1); Assert.That(shop.Detail.HasStaticPreview, Is.True); Assert.That(shop.Detail.ActionText, Is.EqualTo("Buy - 100"));
                shop.Detail.MainButton.onClick.Invoke(); scene.Flow.Confirm.ConfirmButton.onClick.Invoke();
                Assert.That(shop.Detail.ActionText, Is.EqualTo("Equip")); shop.Detail.MainButton.onClick.Invoke();
                Assert.That(scene.Inventory.Saved.EquippedAvatarId, Is.EqualTo("avatar_fox"));
                Assert.That(scene.Flow.MainMenu.Avatar.AvatarId, Is.EqualTo("avatar_fox"));
                scene.Flow.HandleBack(); scene.Flow.HandleBack();
                scene.Flow.MainMenu.ProfileButton.onClick.Invoke();
                var profile = scene.Flow.Profile;
                Assert.That(profile.Avatar.AvatarId, Is.EqualTo("avatar_fox")); Assert.That(profile.AvatarsText.text, Is.EqualTo("2 / 12")); Assert.That(profile.FramesText.text, Is.EqualTo("1 / 10"));
                Assert.That(profile.EquippedText.text, Does.Contain("Swift Fox"));
            }
        }

        [Test] public void ProfileAvatarOpensAvatarShopAndBackReturnsToProfile()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.MainMenu.ProfileButton.onClick.Invoke(); scene.Flow.Profile.AvatarButton.onClick.Invoke();
                Assert.That(scene.Flow.Shop.IsVisible, Is.True); Assert.That(scene.Flow.Shop.Tab, Is.EqualTo(ShopSlot.Avatar));
                Assert.That(scene.Flow.HandleBack(), Is.True);
                Assert.That(scene.Flow.Shop.IsVisible, Is.False); Assert.That(scene.Flow.Profile.IsVisible, Is.True);
                Assert.That(scene.Flow.MainMenu.Root.gameObject.activeSelf, Is.False);
                scene.Flow.HandleBack(); Assert.That(scene.Flow.MainMenu.Root.gameObject.activeSelf, Is.True);
            }
        }

        [Test] public void ScreensUseNeonKit()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow;
                Assert.That(flow.SettingsScreen.SoundButton.transform.Find("Rim"), Is.Not.Null);
                Assert.That(flow.SettingsScreen.SoundButton.GetComponentInChildren<UnityEngine.UI.Text>().font, Is.SameAs(BrandArt.Display));
                Assert.That(flow.PauseScreen.ResumeButton.transform.Find("Icon"), Is.Not.Null);
                Assert.That(flow.Levels.Root.Find("NeonBackdrop"), Is.Not.Null);
                flow.MainMenu.SettingsButton.onClick.Invoke(); Assert.That(flow.SettingsScreen.BackdropVisible, Is.True);
                flow.HandleBack(); flow.MainMenu.PlayButton.onClick.Invoke(); scene.Hud.Hud.PauseButton.onClick.Invoke(); flow.PauseScreen.SettingsButton.onClick.Invoke();
                Assert.That(flow.SettingsScreen.BackdropVisible, Is.False, "settings over a paused board only dims it");
                Assert.That(scene.Hud.Hud.HintButton.transform.Find("Icon"), Is.Not.Null);
                Assert.That(scene.Hud.Hud.LevelText.color, Is.EqualTo(HudView.TextColor));
            }
        }

        [Test] public void LevelTilesShowNumberStateAndLock()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.MainMenu.LevelsButton.onClick.Invoke(); var levels = scene.Flow.Levels;
                var first = levels.TileButtons[0].GetComponentInChildren<UnityEngine.UI.Text>().text;
                Assert.That(first, Does.Contain(">1<")); Assert.That(first, Does.Contain("Play"));
                Assert.That(levels.TileButtons[1].transform.Find("Lock").gameObject.activeSelf, Is.True);
                Assert.That(levels.TileButtons[0].transform.Find("Lock").gameObject.activeSelf, Is.False);
            }
        }
    }
}
