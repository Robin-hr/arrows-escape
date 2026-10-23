using System;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.UI;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14K: Journey levels and lives, gift box, profile name, premium badge animation, snake loader.</summary>
    public sealed class Step14KTests
    {
        private static readonly long T0 = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc).Ticks;

        [Test] public void LivesSpendAndRefillOverTime()
        {
            var inv = new Inventory();
            Assert.That(JourneyLifeRules.Update(inv, T0), Is.EqualTo(5));
            for (int i = 0; i < 5; i++) Assert.That(JourneyLifeRules.TrySpend(inv, T0), Is.True);
            Assert.That(JourneyLifeRules.TrySpend(inv, T0), Is.False);
            Assert.That(JourneyLifeRules.SecondsToNext(inv, T0), Is.EqualTo(JourneyLifeRules.RefillMinutes * 60));
            long later = T0 + JourneyLifeRules.RefillTicks * 2 + TimeSpan.TicksPerMinute;
            Assert.That(JourneyLifeRules.Update(inv, later), Is.EqualTo(2), "two lives refilled (also while closed)");
            Assert.That(JourneyLifeRules.SecondsToNext(inv, later), Is.EqualTo((JourneyLifeRules.RefillMinutes - 1) * 60));
            Assert.That(JourneyLifeRules.Update(inv, T0 + JourneyLifeRules.RefillTicks * 50), Is.EqualTo(5), "never above five");
            JourneyLifeRules.Add(inv, 3, T0); Assert.That(inv.JourneyLives, Is.EqualTo(5));
            var copy = JsonUtility.FromJson<Inventory>(JsonUtility.ToJson(inv)); Assert.That(copy.JourneyLives, Is.EqualTo(5));
            Assert.That(JsonUtility.FromJson<Inventory>("{\"Coins\":5}").JourneyLives, Is.EqualTo(5), "old saves start full");
        }

        [Test] public void JourneyCatalogIsTwoHundredHardSilhouettes()
        {
            var paths = LevelRepository.GetJourneyResourcePaths();
            Assert.That(paths.Count, Is.EqualTo(200));
            string[] geometric = { "square", "rectangle", "triangle", "circle", "ellipse", "oval", "hexagon", "pentagon", "octagon", "rhombus", "kite_", "trapezium", "parallelogram", "semicircle", "heptagon", "nonagon", "decagon" };
            foreach (string path in paths)
            {
                var load = LevelRepository.LoadFromResources(path); Assert.That(load.Success, Is.True, path);
                Assert.That(load.Board.Arrows.Count, Is.InRange(55, 70), path);
                Assert.That(geometric.Any(g => path.ToLowerInvariant().Contains("_" + g)), Is.False, path);
                foreach (Arrow a in load.Board.Arrows) Assert.That(a.Length, Is.InRange(4, 10), path);
            }
            foreach (int i in new[] { 0, 99, 199 }) Assert.That(LevelSolvability.IsSolvable(LevelRepository.LoadFromResources(paths[i]).Board), Is.True, paths[i]);
        }

        [Test] public void JourneyCostsLivesAndGivesAGift()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow; long now = T0; flow.UtcNow = () => now;
                flow.MainMenu.JourneyButton.onClick.Invoke();
                Assert.That(flow.Journey.IsVisible, Is.True); Assert.That(flow.Journey.CurrentIndex, Is.Zero);
                Assert.That(flow.Journey.LivesText.text, Is.EqualTo("5"));
                flow.Journey.PlayButton.onClick.Invoke();
                Assert.That(flow.Machine.Current, Is.EqualTo(ScreenState.Playing)); Assert.That(scene.Bootstrap.IsJourneyLevel, Is.True);
                Assert.That(scene.Bootstrap.Inventory.State.JourneyLives, Is.EqualTo(4)); Assert.That(scene.Inventory.Saved.JourneyLives, Is.EqualTo(4));
                Assert.That(scene.Hud.Hud.LevelPositionText.text, Is.EqualTo("Journey 1 / 200"));
                Assert.That(scene.Game.Session.Board.Arrows.Count, Is.InRange(55, 70));
                Assert.That(flow.Daily.Hearts, Is.EqualTo(3), "three hearts inside the level too");
                var state = scene.Bootstrap.Inventory.State; int coins = state.Coins, hints = state.Hints, undos = state.Undos, owned = state.OwnedItemIds.Count;
                scene.Clear();
                Assert.That(flow.Gift.IsOpen, Is.True); Assert.That(flow.Gift.Reward, Is.Not.Null);
                bool gained = state.Hints > hints || state.Undos > undos || state.OwnedItemIds.Count > owned || state.Coins >= coins + 50;
                Assert.That(gained, Is.True, "the gift is granted");
                flow.Gift.OpenButton.onClick.Invoke(); Assert.That(flow.Gift.IsRevealed, Is.True); Assert.That(flow.Gift.ResultText.text, Does.StartWith("YOU GOT"));
                flow.Gift.CollectButton.onClick.Invoke(); Assert.That(flow.Gift.IsOpen, Is.False);
                Assert.That(scene.Progress.Saved.levels.Any(r => r.id.StartsWith("journey_001")), Is.True);
                scene.Hud.CompletePanel.LevelsButton.onClick.Invoke();
                Assert.That(flow.Journey.IsVisible, Is.True); Assert.That(flow.Journey.CurrentIndex, Is.EqualTo(1));
                state.JourneyLives = 0; state.JourneyRefillTicks = now;
                flow.Journey.PlayButton.onClick.Invoke();
                Assert.That(flow.JourneyLives.IsOpen, Is.True); Assert.That(flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu));
                Assert.That(flow.JourneyLives.TimerText.text, Is.EqualTo("NEXT LIFE IN 15:00"));
                flow.JourneyLives.AdButton.onClick.Invoke(); Assert.That(state.JourneyLives, Is.EqualTo(1));
                flow.JourneyLives.CloseButton.onClick.Invoke();
                flow.Journey.PlayButton.onClick.Invoke(); Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.EqualTo(1)); Assert.That(state.JourneyLives, Is.Zero);
            }
        }

        [Test] public void ProfileNameIsEditedAndShown()
        {
            Assert.That(PlayerNameRules.Clean("  Rob<in>  the  great!!!12345678 "), Is.EqualTo("Robin the great1"));
            Assert.That(PlayerNameRules.Display(""), Is.EqualTo(PlayerNameRules.DefaultName));
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow;
                Assert.That(flow.MainMenu.PlayerNameText.text, Is.EqualTo(PlayerNameRules.DefaultName));
                Assert.That(flow.MainMenu.PlayerNameText.font, Is.SameAs(BrandArt.NameFont));
                flow.OpenProfile(); flow.Profile.EditNameButton.onClick.Invoke();
                Assert.That(flow.NameEditor.IsOpen, Is.True);
                flow.NameEditor.Field.text = "Robin"; flow.NameEditor.SaveButton.onClick.Invoke();
                Assert.That(flow.NameEditor.IsOpen, Is.False); Assert.That(scene.Inventory.Saved.PlayerName, Is.EqualTo("Robin"));
                Assert.That(flow.Profile.NameText.text, Is.EqualTo("Robin")); Assert.That(flow.MainMenu.PlayerNameText.text, Is.EqualTo("Robin"));
                Assert.That(scene.Bootstrap.RankingSnapshot().Name, Is.EqualTo("Robin"));
                flow.HandleBack(); flow.MainMenu.JourneyButton.onClick.Invoke(); Assert.That(flow.Journey.NameText.text, Is.EqualTo("Robin"));
            }
        }

        [Test] public void OnlyPremiumBadgesAnimateOnEveryPage()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow; var pulse = flow.MainMenu.AvatarMotion;
                pulse.Tick(0.5f); Assert.That(pulse.AvatarAnimated || pulse.FrameAnimated, Is.False, "free avatar and frame stay still");
                Assert.That(flow.MainMenu.Avatar.FrameImage.rectTransform.localScale, Is.EqualTo(Vector3.one));
                var inv = scene.Bootstrap.Inventory; inv.Shop.AddCoins(20000);
                string avatar = ShopCatalog.Items.First(i => i.Slot == ShopSlot.Avatar && i.Category == "Premium").Id;
                string frame = ShopCatalog.Items.First(i => i.Slot == ShopSlot.Frame && i.Category == "Mythic Frames").Id;
                inv.TryBuy(avatar); inv.TryEquip(avatar);
                pulse.Tick(0.3f); Assert.That(pulse.AvatarAnimated, Is.True); Assert.That(pulse.FrameAnimated, Is.False);
                inv.TryBuy(frame); inv.TryEquip(frame);
                pulse.Tick(0.3f); var before = flow.MainMenu.Avatar.FrameImage.rectTransform.localScale; pulse.Tick(0.3f);
                Assert.That(pulse.FrameAnimated, Is.True); Assert.That(flow.MainMenu.Avatar.FrameImage.rectTransform.localScale, Is.Not.EqualTo(before));
                flow.OpenProfile(); var profilePulse = flow.Profile.Avatar.GetComponent<AvatarPulse>();
                Assert.That(profilePulse, Is.Not.Null); profilePulse.Tick(0.2f); Assert.That(profilePulse.AvatarAnimated && profilePulse.FrameAnimated, Is.True);
                Assert.That(flow.Journey.Avatar.GetComponent<AvatarPulse>(), Is.Not.Null);
                Assert.That(flow.Ranking.MeRow.Avatar.GetComponent<AvatarPulse>(), Is.Not.Null);
            }
        }

        [Test] public void LoadingScreensUseTheSnakeArrowLoader()
        {
            using (var scene = new EconomyTestScene(withUi: true))
                Assert.That(scene.Flow.Loading.Loader, Is.InstanceOf<SnakeLoaderGraphic>());
        }
    }
}
