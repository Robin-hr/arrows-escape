using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 16: solid per-level backgrounds, themes retired, every skin animated, no stray exit lines.</summary>
    public sealed class Step16Tests
    {
        [Test] public void EveryLevelHasItsOwnSolidColour()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Bootstrap.ShowLevel(0); Color first = Camera.main.backgroundColor;
                Assert.That(first, Is.EqualTo(SkinBackdrop.For("classic", 0, true)));
                scene.Bootstrap.ShowLevel(1); Color second = Camera.main.backgroundColor;
                Assert.That(second, Is.EqualTo(SkinBackdrop.For("classic", 1, true))); Assert.That(second, Is.Not.EqualTo(first));
                scene.Bootstrap.Inventory.Shop.AddCoins(1000); scene.Bootstrap.Inventory.TryBuy("inferno_streak"); scene.Bootstrap.Inventory.TryEquip("inferno_streak");
                Assert.That(scene.Board.IsLightBoard, Is.False, "glowing skins sit on deep colours");
                Assert.That(Camera.main.backgroundColor, Is.EqualTo(SkinBackdrop.For("inferno_streak", 1, false)));
                foreach (var backdrop in Object.FindObjectsByType<ThemeBackdrop>(FindObjectsSortMode.None)) Assert.That(backdrop.ParticleCount, Is.Zero, "no moving shapes behind the board");
                Assert.That(scene.Board.HasDarkTheme, Is.False);
            }
        }

        [Test] public void PalettesAreReadable()
        {
            foreach (Color c in LevelColors.Dark.Select(x => (Color)x)) { Assert.That(c.grayscale, Is.LessThan(.2f)); Assert.That(ArrowTint.Contrast(Color.white, c), Is.GreaterThan(10)); }
            foreach (Color c in LevelColors.Light.Select(x => (Color)x)) { Assert.That(c.grayscale, Is.GreaterThan(.85f)); Assert.That(ArrowTint.Contrast(ViewTheme.ArrowColor, c), Is.GreaterThan(10)); }
            Assert.That(LevelColors.For(-3, true), Is.EqualTo((Color)LevelColors.Light[7]), "negative keys wrap");
        }

        [Test] public void ThemesAreGoneFromShopAndSaves()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.OpenShop(); Assert.That(scene.Flow.Shop.ThemesButton.gameObject.activeSelf, Is.False);
                Assert.That(scene.Flow.Shop.AvatarsButton.gameObject.activeSelf, Is.True); Assert.That(scene.Flow.Shop.SoundsButton.gameObject.activeSelf, Is.True);
                Assert.That(ShopCatalog.Items.Count(i => i.Slot == ShopSlot.Theme), Is.EqualTo(1));
                scene.Flow.HandleBack(); scene.Flow.MainMenu.ProfileButton.onClick.Invoke(); StringAssert.Contains("Sound:", scene.Flow.Profile.EquippedText.text);
            }
        }

        [Test] public void SettingsLinkTermsAndPrivacy()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var settings = scene.Flow.SettingsScreen; Assert.That(settings.TermsButton, Is.Not.Null);
                StringAssert.Contains("Terms", settings.TermsButton.GetComponentInChildren<UnityEngine.UI.Text>().text);
                settings.TermsButton.onClick.Invoke(); Assert.That(settings.LastOpenedUrl, Is.EqualTo(AppInfo.TermsUrl));
                settings.PrivacyButton.onClick.Invoke(); Assert.That(settings.LastOpenedUrl, Is.EqualTo(AppInfo.PrivacyPolicyUrl));
                Assert.That(AppInfo.TermsUrl, Does.StartWith("https://")); Assert.That(AppInfo.TermsUrl, Is.Not.EqualTo(AppInfo.PrivacyPolicyUrl));
            }
        }

        [Test] public void EverySkinIsAnimatedInItsOwnColours()
        {
            foreach (var item in ShopCatalog.Items.Where(i => i.Slot == ShopSlot.Arrow && i.Id != "classic"))
            {
                var skin = ArrowSkinRegistry.Find(item.Id); Assert.That(skin, Is.Not.Null, item.Id); Assert.That(skin.IsProcedural, Is.True, item.Id);
                Assert.That(skin.Signature, Is.Not.Empty, item.Id);
            }
            var cyber = ArrowSkinRegistry.Find("cyber_pulsar"); Assert.That(cyber.Recolor, Is.True); Assert.That(ArrowSkinRegistry.Find("plasma_bolt").Recolor, Is.False);
            var block = new MaterialPropertyBlock(); cyber.ApplyRecolor(block); Assert.That(block.GetFloat("_Recolor"), Is.EqualTo(1)); Assert.That(block.GetColor("_RecolorA"), Is.EqualTo(cyber.Primary));
            Assert.That(ArrowSkinRegistry.All.Select(s => s.Id).Distinct().Count(), Is.EqualTo(19));
        }
    }
}
