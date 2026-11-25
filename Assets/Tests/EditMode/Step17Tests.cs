using System;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 17: paper HUD, backgrounds that follow the arrow style, night board, special offers and reminders.</summary>
    public sealed class Step17Tests
    {
        private static readonly string[] AllSkins = new[] { "classic" }.Concat(ArrowSkinRegistry.All.Select(s => s.Id)).ToArray();

        [Test] public void EveryArrowStyleHasItsOwnContrastingBackground()
        {
            foreach (string id in AllSkins)
            {
                bool light = SkinBackdrop.LightBoard(id, false);
                Assert.That(light, Is.EqualTo(SkinBackdrop.DarkArrowSkins.Contains(id)), id + ": dark arrows sit on a light board, bright arrows on a dark one");
                Color face = SkinBackdrop.ArrowFace(id, light);
                for (int key = 0; key < 8; key++)
                {
                    Color bg = SkinBackdrop.For(id, key, light);
                    Assert.That(SkinBackdrop.Contrast(face, bg), Is.GreaterThanOrEqualTo(4.5f), $"{id} level {key}: arrows must stand out");
                    if (light) Assert.That(SkinBackdrop.Luminance(bg), Is.GreaterThan(.7f), id); else Assert.That(SkinBackdrop.Luminance(bg), Is.LessThan(.03f), id);
                }
                Assert.That(SkinBackdrop.For(id, 1, light), Is.Not.EqualTo(SkinBackdrop.For(id, 0, light)), id + ": levels get different shades");
                if (SkinBackdrop.NightChangesBoard(id))
                    for (int key = 0; key < 8; key++) Assert.That(SkinBackdrop.Contrast(SkinBackdrop.ArrowFace(id, false), SkinBackdrop.For(id, key, false)), Is.GreaterThanOrEqualTo(4.5f), id + " night");
            }
            Assert.That(SkinBackdrop.For("inferno_streak", 0, false), Is.Not.EqualTo(SkinBackdrop.For("frostbite", 0, false)), "each skin has its own colour family");
            Assert.That(SkinBackdrop.For("crimson_relic", 0, true), Is.Not.EqualTo(SkinBackdrop.For("void_whisper", 0, true)));
        }

        [Test] public void NightModeTurnsClassicInkLightOnADarkBoard()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Bootstrap.ShowLevel(0);
                Assert.That(scene.Board.IsLightBoard, Is.True); Assert.That(scene.Hud.Hud.IsNight, Is.False);
                scene.Hud.ToggleNight();
                Assert.That(scene.Board.NightMode, Is.True); Assert.That(scene.Board.IsLightBoard, Is.False);
                Assert.That(Camera.main.backgroundColor, Is.EqualTo(SkinBackdrop.For("classic", 0, false)));
                Assert.That(scene.Bootstrap.Settings.Model.NightMode, Is.True, "saved");
                scene.Board.TryGetBoard(out Board b); scene.Board.TryGetArrowView(b.Arrows[0].Id, out ArrowView view);
                Assert.That(view.BaseColor, Is.EqualTo(SkinBackdrop.NightInk));
                Assert.That(scene.Hud.Hud.IsNight, Is.True, "banners follow the board");
                scene.Hud.ToggleNight(); Assert.That(scene.Board.IsLightBoard, Is.True); Assert.That(view.BaseColor, Is.EqualTo(ViewTheme.ArrowColor));
                scene.Bootstrap.Inventory.Shop.AddCoins(2000); scene.Bootstrap.Inventory.TryBuy("frostbite"); scene.Bootstrap.Inventory.TryEquip("frostbite");
                Assert.That(scene.Board.IsLightBoard, Is.False);
                scene.Hud.ToggleNight(); Assert.That(scene.Board.IsLightBoard, Is.False, "glowing arrows always stay on a dark board");
                StringAssert.Contains("dark board", scene.Hud.Hud.NoteText.text);
            }
        }

        [Test] public void PaperHudMatchesTheReferenceLayout()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.MainMenu.PlayButton.onClick.Invoke(); scene.Bootstrap.ShowLevel(1);
                var hud = scene.Hud.Hud;
                Assert.That(hud.TitleText.text, Is.EqualTo("LEVEL 2"));
                StringAssert.StartsWith("Level 2 / ", hud.LevelPositionText.text);
                Assert.That(hud.ArrowsLeftText.text, Is.EqualTo(scene.Game.Session.Board.Arrows.Count.ToString()));
                Assert.That(hud.CoinsText.text, Is.EqualTo(scene.Bootstrap.Inventory.State.Coins.ToString()));
                foreach (var b in new[] { hud.UndoButton, hud.HintButton, hud.ToolsButton }) Assert.That(b.transform.parent.name, Is.EqualTo("BottomBar"));
                foreach (var b in new[] { hud.PauseButton, hud.NightButton }) Assert.That(b.transform.parent.name, Is.EqualTo("TopBar"));
                Assert.That(hud.Hearts.Length, Is.EqualTo(3)); Assert.That(hud.Hearts[0].sprite.name, Is.EqualTo("Hud/hud_heart"));
                Assert.That(hud.ToolsOpen, Is.False); hud.ToolsButton.onClick.Invoke(); Assert.That(hud.ToolsOpen, Is.True);
                Assert.That(hud.RestartButton.transform.parent, Is.EqualTo(hud.ToolsTray));
                Vector2 center = new Vector2(Screen.width * .5f, Screen.height * .5f);
                Assert.That(scene.Hud.IsBlocked(center), Is.True, "an open tray is modal"); Assert.That(hud.ToolsOpen, Is.False, "and closes on an outside tap");
                Assert.That(scene.Hud.IsBlocked(center), Is.False);
                scene.Tap(0, 0); Assert.That(hud.ArrowsLeftText.text, Is.EqualTo(scene.Game.Session.Board.Arrows.Count.ToString()));
            }
        }

        [Test] public void FreeToolMarksEveryArrowThatCanLeave()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.MainMenu.PlayButton.onClick.Invoke(); scene.Bootstrap.ShowLevel(1);
                Board b = scene.Game.Session.Board; int free = b.Arrows.Count(a => b.CanExit(a));
                int hints = scene.Bootstrap.Inventory.State.Hints;
                Assert.That(scene.Hud.Reveal(), Is.EqualTo(free));
                Assert.That(scene.Bootstrap.Inventory.State.Hints, Is.EqualTo(hints - 1), "costs one hint");
                var reveal = scene.Game.GetComponent<FreeArrowReveal>(); Assert.That(reveal.MarkedCount, Is.EqualTo(free));
                StringAssert.Contains("can leave right now", scene.Hud.Hud.NoteText.text);
                reveal.Tick(FreeArrowReveal.Seconds + .1f); Assert.That(reveal.MarkedCount, Is.Zero);
                scene.Hud.Reveal(); scene.Game.RestartLevel(); Assert.That(reveal.MarkedCount, Is.Zero, "a new attempt clears the marks");
            }
        }

        [Test] public void SpecialOfferCalendar()
        {
            var sat = OfferCalendar.ActiveOn(new DateTime(2026, 11, 28)); Assert.That(sat.Title, Is.EqualTo("WEEKEND SALE")); Assert.That(sat.DiscountPercent, Is.EqualTo(25));
            Assert.That(OfferCalendar.ActiveOn(new DateTime(2026, 11, 29)).Id, Is.EqualTo(sat.Id));
            Assert.That(OfferCalendar.ActiveOn(new DateTime(2026, 11, 25)), Is.Null, "no sale on a weekday");
            var xmas = OfferCalendar.ActiveOn(new DateTime(2026, 12, 25)); Assert.That(xmas.Title, Is.EqualTo("CHRISTMAS SALE")); Assert.That(xmas.GiftCoins, Is.EqualTo(300));
            Assert.That(OfferCalendar.ActiveOn(new DateTime(2026, 12, 27)), Is.Null, "a festival takes over its whole weekend");
            Assert.That(OfferCalendar.ActiveOn(new DateTime(2027, 1, 1)).Title, Is.EqualTo("NEW YEAR SALE"));
            Assert.That(OfferCalendar.ActiveOn(new DateTime(2027, 10, 29)).Title, Is.EqualTo("DIWALI SALE"));
            Assert.That(OfferCalendar.ActiveOn(new DateTime(2027, 1, 15)).Title, Is.EqualTo("PONGAL SALE"));
            var next = OfferCalendar.Upcoming(new DateTime(2026, 11, 25), 14);
            Assert.That(next.Select(o => o.Start), Is.EqualTo(new[] { new DateTime(2026, 11, 28), new DateTime(2026, 12, 5) }));
            Assert.That(OfferCalendar.Discounted(150, 25), Is.EqualTo(115)); Assert.That(OfferCalendar.Discounted(0, 50), Is.EqualTo(0)); Assert.That(OfferCalendar.Discounted(10, 90), Is.EqualTo(5));
        }

        [Test] public void ShopHonoursTheOfferAndItsGiftOnce()
        {
            var shop = new ShopService(); shop.AddCoins(5000);
            var item = ShopCatalog.Find("frostbite"); shop.ActiveOffer = OfferCalendar.ActiveOn(new DateTime(2026, 12, 25));
            int price = shop.PriceOf(item); Assert.That(price, Is.LessThan(item.Price)); Assert.That(price, Is.EqualTo(OfferCalendar.Discounted(item.Price, 40)));
            Assert.That(shop.TryBuy("frostbite"), Is.True); Assert.That(shop.Inventory.Coins, Is.EqualTo(5000 - price));
            Assert.That(shop.CanClaimOfferGift, Is.True); Assert.That(shop.ClaimOfferGift(), Is.EqualTo(300)); Assert.That(shop.ClaimOfferGift(), Is.Zero, "once per offer");
            shop.ActiveOffer = null; Assert.That(shop.PriceOf(item), Is.EqualTo(item.Price)); Assert.That(shop.CanClaimOfferGift, Is.False);
            var json = JsonUtility.ToJson(shop.Inventory); var back = JsonUtility.FromJson<Inventory>(json); Assert.That(back.ClaimedOfferIds, Does.Contain("christmas_20261224"));
            var old = JsonUtility.FromJson<Inventory>("{\"version\":1,\"Coins\":5}"); old.Sanitize(); Assert.That(old.ClaimedOfferIds, Is.Empty);
        }

        [Test] public void ReminderPlan()
        {
            var c = new NotificationContext { Now = new DateTime(2026, 11, 25, 8, 0, 0), Streak = 3, JourneyLives = 2, SecondsToFullLives = 2700, RewardWaiting = true };
            var plan = NotificationPlanner.Plan(c);
            Assert.That(plan.Count, Is.LessThanOrEqualTo(NotificationPlanner.MaxNotifications));
            Assert.That(plan.Select(p => p.Id).Distinct().Count(), Is.EqualTo(plan.Count), "unique ids");
            Assert.That(plan.Select(p => p.Fire), Is.Ordered);
            Assert.That(plan.All(p => p.Fire > c.Now), Is.True, "nothing in the past");
            var first = plan.First(); Assert.That(first.Fire, Is.EqualTo(new DateTime(2026, 11, 25, 8, 45, 0)), "lives full in 45 minutes"); Assert.That(first.Channel, Is.EqualTo(NotificationPlanner.RemindersChannel));
            var daily = plan.Where(p => p.Channel == NotificationPlanner.DailyChannel && p.Title == "Daily Puzzle ready").ToList();
            Assert.That(daily.Count, Is.EqualTo(7)); Assert.That(daily[0].Fire, Is.EqualTo(new DateTime(2026, 11, 25, 9, 0, 0))); StringAssert.Contains("free reward", daily[0].Body);
            Assert.That(plan.Any(p => p.Fire == new DateTime(2026, 11, 25, 20, 0, 0) && p.Title.Contains("3-day streak")), Is.True);
            var offers = plan.Where(p => p.Channel == NotificationPlanner.OffersChannel).ToList();
            Assert.That(offers.Select(o => o.Fire), Does.Contain(new DateTime(2026, 11, 28, 10, 0, 0))); StringAssert.Contains("WEEKEND SALE", offers[0].Title);
            Assert.That(plan.Any(p => p.Fire == new DateTime(2026, 11, 28, 18, 30, 0) && p.Title == "The arrows miss you"), Is.True);
            c.Now = new DateTime(2026, 11, 25, 10, 0, 0); c.DailySolvedToday = true; c.JourneyLives = 5; c.SecondsToFullLives = 0;
            plan = NotificationPlanner.Plan(c);
            Assert.That(plan.Any(p => p.Fire.Date == c.Now.Date && p.Channel == NotificationPlanner.DailyChannel), Is.False, "solved today: no daily or streak reminder today");
            Assert.That(plan.Any(p => p.Title == "Journey lives are full"), Is.False);
            var sat = NotificationPlanner.Plan(new NotificationContext { Now = new DateTime(2026, 11, 28, 12, 0, 0) });
            Assert.That(sat.Any(p => p.Title.StartsWith("Last chance") && p.Fire == new DateTime(2026, 11, 29, 18, 0, 0)), Is.True);
        }

        [Test] public void SoftPromptAskOnceThenScheduleAndToggle()
        {
            var previous = NotificationRegistry.Scheduler; var fake = new MemoryNotificationScheduler(); NotificationRegistry.Scheduler = fake;
            try
            {
                using (var scene = new EconomyTestScene(withUi: true))
                {
                    var director = scene.Flow.Notifications; director.Clock = () => new DateTime(2026, 11, 25, 8, 0, 0);
                    Assert.That(director.MaybeAsk(1), Is.False, "not before two cleared levels");
                    Assert.That(director.MaybeAsk(2), Is.True); Assert.That(scene.Flow.Confirm.IsOpen, Is.True);
                    scene.Flow.Confirm.ConfirmButton.onClick.Invoke();
                    Assert.That(fake.PermissionRequests, Is.EqualTo(1));
                    Assert.That(scene.Bootstrap.Settings.Model.NotificationsEnabled && scene.Bootstrap.Settings.Model.NotificationsAsked, Is.True);
                    Assert.That(fake.Scheduled.Count, Is.GreaterThan(5)); Assert.That(director.MaybeAsk(5), Is.False, "asked only once");
                    Assert.That(scene.Flow.SettingsScreen.RemindersOn, Is.True);
                    scene.Flow.SettingsScreen.RemindersButton.onClick.Invoke();
                    Assert.That(scene.Bootstrap.Settings.Model.NotificationsEnabled, Is.False); Assert.That(fake.Scheduled, Is.Empty, "turning reminders off cancels them");
                    StringAssert.Contains("Off", scene.Flow.SettingsScreen.RemindersButton.GetComponentInChildren<UnityEngine.UI.Text>().text);
                    fake.GrantOnRequest = false; fake.Permission = NotificationPermission.Unknown;
                    scene.Flow.SettingsScreen.RemindersButton.onClick.Invoke();
                    Assert.That(scene.Bootstrap.Settings.Model.NotificationsEnabled, Is.False, "a refused system prompt keeps them off");
                }
            }
            finally { NotificationRegistry.Scheduler = previous; }
        }

        [Test] public void NotNowKeepsRemindersOff()
        {
            var previous = NotificationRegistry.Scheduler; var fake = new MemoryNotificationScheduler(); NotificationRegistry.Scheduler = fake;
            try
            {
                using (var scene = new EconomyTestScene(withUi: true))
                {
                    scene.Flow.Notifications.MaybeAsk(3); scene.Flow.Confirm.CancelButton.onClick.Invoke();
                    Assert.That(fake.PermissionRequests, Is.Zero); Assert.That(scene.Bootstrap.Settings.Model.NotificationsAsked, Is.True);
                    Assert.That(scene.Bootstrap.Settings.Model.NotificationsEnabled, Is.False); Assert.That(scene.Flow.Notifications.MaybeAsk(9), Is.False);
                }
            }
            finally { NotificationRegistry.Scheduler = previous; }
        }
    }
}
