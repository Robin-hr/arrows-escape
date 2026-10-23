using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.UI;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Limited hints/undos, developer level opening, daily rewards and the daily puzzle (Step 14I).</summary>
    public sealed class DailyFeatureTests
    {
        private const int Today = 20261023;

        [Test] public void DailyBoardComesFromTheDateAlone()
        {
            Board a = DailyPuzzleBuilder.Build(Today), b = DailyPuzzleBuilder.Build(Today), c = DailyPuzzleBuilder.Build(Today + 1);
            Assert.That(a.Width, Is.EqualTo(DailyPuzzleBuilder.Width)); Assert.That(a.Height, Is.EqualTo(DailyPuzzleBuilder.Height));
            Assert.That(a.Arrows.Count, Is.GreaterThan(50), "a big board");
            Assert.That(a.Arrows.Select(x => x.X * 1000 + x.Y * 10 + (int)x.Direction), Is.EqualTo(b.Arrows.Select(x => x.X * 1000 + x.Y * 10 + (int)x.Direction)));
            Assert.That(c.Arrows.Select(x => x.X * 1000 + x.Y), Is.Not.EqualTo(a.Arrows.Select(x => x.X * 1000 + x.Y)));
            Assert.That(LevelSolvability.IsSolvable(a), Is.True); Assert.That(LevelSolvability.CountFreeArrows(a), Is.LessThan(a.Arrows.Count));
        }

        [Test] public void StreakGrowsOnConsecutiveDaysAndBadgesAtHundreds()
        {
            var inventory = new Inventory();
            Assert.That(DailyStreakRules.RecordSolve(inventory, Today), Is.EqualTo(1));
            Assert.That(DailyStreakRules.RecordSolve(inventory, Today), Is.EqualTo(1), "a second solve the same day");
            Assert.That(DailyStreakRules.RecordSolve(inventory, DailyCalendar.AddDays(Today, 1)), Is.EqualTo(2));
            Assert.That(DailyStreakRules.CurrentStreak(inventory, DailyCalendar.AddDays(Today, 2)), Is.EqualTo(2), "alive until the end of the next day");
            Assert.That(DailyStreakRules.CurrentStreak(inventory, DailyCalendar.AddDays(Today, 3)), Is.Zero, "a missed day breaks it");
            Assert.That(DailyStreakRules.RecordSolve(inventory, DailyCalendar.AddDays(Today, 3)), Is.EqualTo(1));
            Assert.That(inventory.DailyBestStreak, Is.EqualTo(2)); Assert.That(inventory.DailySolvedCount, Is.EqualTo(3));
            Assert.That(DailyStreakRules.Badges, Is.EqualTo(new[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 }));
            Assert.That(DailyStreakRules.EarnedBadges(99), Is.Zero); Assert.That(DailyStreakRules.EarnedBadges(1000), Is.EqualTo(10));
            Assert.That(DailyStreakRules.NextBadge(250), Is.EqualTo(300)); Assert.That(DailyStreakRules.NextBadge(1000), Is.EqualTo(-1));
            string share = DailyStreakRules.ShareText(Today, 140, 1, 2, 5);
            Assert.That(share, Does.Contain("23 Oct 2026").And.Contain("140 moves").And.Contain("1 mistake").And.Contain("5 day streak"));
            foreach (int badge in DailyStreakRules.Badges) Assert.That(BrandArt.Texture("Daily/badge_streak_" + badge), Is.Not.Null);
        }

        [Test] public void RewardCalendarGivesTheSevenDays()
        {
            var shop = new ShopService(new Inventory());
            int coins = shop.Inventory.Coins, hints = shop.Inventory.Hints, undos = shop.Inventory.Undos;
            Assert.That(DailyRewardRules.Claim(shop, Today).Kind, Is.EqualTo(DailyRewardKind.Coins)); Assert.That(shop.Inventory.Coins, Is.EqualTo(coins + 300));
            Assert.That(DailyRewardRules.Claim(shop, Today), Is.Null, "once per day");
            DailyRewardRules.Claim(shop, Today + 1); Assert.That(shop.Inventory.Undos, Is.EqualTo(undos + 1));
            DailyRewardRules.Claim(shop, Today + 2); Assert.That(shop.Inventory.Hints, Is.EqualTo(hints + 1));
            DailyRewardRules.Claim(shop, Today + 3); Assert.That(shop.Inventory.Coins, Is.EqualTo(coins + 800));
            DailyRewardRules.Claim(shop, Today + 4); DailyRewardRules.Claim(shop, Today + 5);
            Assert.That(shop.Inventory.Undos, Is.EqualTo(undos + 2)); Assert.That(shop.Inventory.Hints, Is.EqualTo(hints + 2));
            var pool = DailyRewardRules.MysteryPool(shop.Inventory);
            Assert.That(pool.Any(r => r.Kind == DailyRewardKind.Item && ShopCatalog.Find(r.ItemId).Slot == ShopSlot.Arrow), Is.True);
            Assert.That(pool.Any(r => r.Kind == DailyRewardKind.Item && ShopCatalog.Find(r.ItemId).Slot == ShopSlot.Frame), Is.True);
            Assert.That(pool.Any(r => r.Kind == DailyRewardKind.Item && ShopCatalog.Find(r.ItemId).Slot == ShopSlot.Avatar), Is.True);
            Assert.That(pool.Count(r => r.Kind == DailyRewardKind.Coins), Is.EqualTo(3));
            var mystery = DailyRewardRules.Claim(shop, Today + 6); Assert.That(mystery, Is.Not.Null);
            Assert.That(DailyRewardRules.NextDay(shop.Inventory), Is.Zero, "the cycle starts again");
        }

        [Test] public void HintsAndUndosAreLimitedWithAdAndCoinOffers()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var hud = scene.Hud.Hud; var state = scene.Bootstrap.Inventory.State;
                Assert.That(state.Hints, Is.EqualTo(3)); Assert.That(state.Undos, Is.EqualTo(3));
                scene.Flow.MainMenu.PlayButton.onClick.Invoke();
                Assert.That(hud.HintCountText.text, Is.EqualTo("3"));
                for (int i = 0; i < 3; i++) hud.HintButton.onClick.Invoke();
                Assert.That(state.Hints, Is.Zero); Assert.That(hud.HintCountText.text, Is.EqualTo("+"));
                Assert.That(scene.Inventory.Saved.Hints, Is.Zero, "uses are saved");
                hud.HintButton.onClick.Invoke();
                var offer = scene.Hud.Offer; Assert.That(offer.IsOpen, Is.True); Assert.That(offer.Kind, Is.EqualTo(BoostKind.Hint));
                Assert.That(offer.BuyButton.interactable, Is.False, "no coins yet");
                offer.AdButton.onClick.Invoke(); Assert.That(offer.IsOpen, Is.False); Assert.That(state.Hints, Is.EqualTo(1), "the ad gives one");
                scene.Bootstrap.Inventory.Shop.AddCoins(EconomyConfig.BoostPrice);
                scene.Hud.OpenOffer(BoostKind.Undo); Assert.That(offer.BuyButton.interactable, Is.True);
                offer.BuyButton.onClick.Invoke(); Assert.That(state.Undos, Is.EqualTo(4)); Assert.That(state.Coins, Is.Zero);
                var free = scene.Game.Session.Board.Arrows.First(a => scene.Game.Session.Board.CanExit(a));
                scene.Tap(free.X, free.Y); hud.UndoButton.onClick.Invoke();
                Assert.That(state.Undos, Is.EqualTo(3), "an undo that happens costs one");
            }
        }

        [Test] public void TripleTapOnALockedLevelOpensItForTesting()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                float now = 0; var levels = scene.Flow.Levels; levels.Clock = () => now;
                scene.Flow.MainMenu.LevelsButton.onClick.Invoke();
                Assert.That(levels.TileButtons[2].interactable, Is.False);
                var relay = levels.TileButtons[2].GetComponent<LockedTileTap>(); Assert.That(relay, Is.Not.Null);
                relay.Tap(); now += 0.2f; relay.Tap(); Assert.That(scene.Game.Session, Is.Null);
                now += 2f; relay.Tap(); Assert.That(scene.Game.Session, Is.Null, "a slow tap starts over");
                now += 0.2f; relay.Tap(); now += 0.2f; relay.Tap();
                Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.EqualTo(2)); Assert.That(scene.Flow.Machine.Current, Is.EqualTo(ScreenState.Playing));
                Assert.That(scene.Flow.Toast.Message.text, Does.Contain("LEVEL 3"));
            }
        }

        [Test] public void DailyRewardsPageClaimsAndRevealsDaySeven()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                int key = Today; scene.Flow.TodayKey = () => key; var flow = scene.Flow; var state = scene.Bootstrap.Inventory.State;
                flow.MainMenu.RewardsButton.onClick.Invoke(); Assert.That(flow.Rewards.IsVisible, Is.True);
                flow.Rewards.ClaimButton.onClick.Invoke(); Assert.That(state.Coins, Is.EqualTo(300)); Assert.That(scene.Inventory.Saved.Coins, Is.EqualTo(300));
                Assert.That(flow.Rewards.ClaimButton.interactable, Is.False); Assert.That(flow.Rewards.ResultText.text, Does.Contain("300 COINS"));
                for (int d = 1; d < 7; d++) { key = Today + d; flow.Rewards.Show(state, key); flow.Rewards.ClaimButton.onClick.Invoke(); }
                Assert.That(flow.Rewards.LastReward, Is.Not.Null); Assert.That(flow.Rewards.IsSpinning, Is.False);
                Assert.That(flow.Rewards.ResultText.text, Does.StartWith("YOU GOT"));
                Assert.That(flow.HandleBack(), Is.True); Assert.That(flow.MainMenu.Root.gameObject.activeSelf, Is.True);
            }
        }

        [Test] public void DailyPuzzleHeartsStreakAndShare()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.TodayKey = () => Today; var flow = scene.Flow; var daily = flow.Daily;
                flow.MainMenu.DailyButton.onClick.Invoke(); Assert.That(flow.DailyPage.IsVisible, Is.True);
                flow.DailyPage.PlayButton.onClick.Invoke();
                Assert.That(scene.Bootstrap.IsDailyLevel, Is.True); Assert.That(flow.Machine.Current, Is.EqualTo(ScreenState.Playing));
                Assert.That(scene.Hud.Hud.IsDaily, Is.True); Assert.That(daily.Hearts, Is.EqualTo(3));
                Assert.That(scene.Hud.Hud.LevelText.text, Is.EqualTo("DAILY PUZZLE"));
                var board = scene.Game.Session.Board; var blocked = board.Arrows.First(a => !board.CanExit(a));
                for (int i = 0; i < 3; i++) scene.Tap(blocked.X, blocked.Y);
                Assert.That(daily.Hearts, Is.Zero); Assert.That(daily.Fail.IsOpen, Is.True);
                daily.Fail.AdButton.onClick.Invoke(); Assert.That(daily.Hearts, Is.EqualTo(1)); Assert.That(daily.Fail.IsOpen, Is.False);
                scene.Tap(blocked.X, blocked.Y); Assert.That(daily.Fail.IsOpen, Is.True);
                daily.Fail.RestartButton.onClick.Invoke(); Assert.That(daily.Hearts, Is.EqualTo(3)); Assert.That(scene.Game.MistakeCount, Is.Zero);
                scene.Clear();
                Assert.That(daily.Result.IsOpen, Is.True); Assert.That(flow.Machine.Current, Is.EqualTo(ScreenState.LevelComplete));
                Assert.That(scene.Bootstrap.Inventory.State.DailyStreak, Is.EqualTo(1)); Assert.That(scene.Inventory.Saved.DailyLastSolvedKey, Is.EqualTo(Today));
                Assert.That(scene.Hud.CompletePanel.IsVisible, Is.False, "the normal level card stays hidden");
                daily.Result.ShareButton.onClick.Invoke();
                Assert.That(daily.LastShareText, Does.Contain("23 Oct 2026").And.Contain("moves").And.Contain("0 mistakes"));
                Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(daily.LastShareText));
                daily.Result.HomeButton.onClick.Invoke(); Assert.That(flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu));
                Assert.That(flow.MainMenu.StreakText.text, Is.EqualTo("1"));
                Assert.That(scene.Bootstrap.IsDailyLevel, Is.True, "the daily stays loaded until another level starts");
                flow.MainMenu.PlayButton.onClick.Invoke(); Assert.That(scene.Bootstrap.IsDailyLevel, Is.False); Assert.That(scene.Hud.Hud.IsDaily, Is.False);
            }
        }
    }
}
