using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.UI;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14J: hearts on every level, victory celebration, Journey map, glass home and animated avatar.</summary>
    public sealed class Step14JTests
    {
        /// <summary>Every level has three hearts; at zero the player watches an ad or buys a heart for 500 coins.</summary>
        [Test] public void EveryLevelHasThreeHearts()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow; var daily = flow.Daily;
                flow.MainMenu.PlayButton.onClick.Invoke();
                Assert.That(flow.Machine.Current, Is.EqualTo(ScreenState.Playing)); Assert.That(scene.Bootstrap.IsDailyLevel, Is.False);
                Assert.That(daily.Hearts, Is.EqualTo(3)); Assert.That(scene.Hud.Hud.Hearts[0].gameObject.activeInHierarchy, Is.True);
                var board = scene.Game.Session.Board; var blocked = board.Arrows.First(a => !board.CanExit(a));
                for (int i = 0; i < 3; i++) scene.Tap(blocked.X, blocked.Y);
                Assert.That(daily.Hearts, Is.Zero); Assert.That(daily.Fail.IsOpen, Is.True);
                Assert.That(daily.Fail.BuyButton.interactable, Is.False, "not enough coins yet");
                daily.Fail.AdButton.onClick.Invoke(); Assert.That(daily.Hearts, Is.EqualTo(1)); Assert.That(daily.Fail.IsOpen, Is.False);
                scene.Bootstrap.Inventory.Shop.AddCoins(600);
                scene.Tap(blocked.X, blocked.Y); Assert.That(daily.Fail.IsOpen, Is.True); Assert.That(daily.Fail.BuyButton.interactable, Is.True);
                daily.Fail.BuyButton.onClick.Invoke();
                Assert.That(daily.Hearts, Is.EqualTo(1)); Assert.That(daily.Fail.IsOpen, Is.False);
                Assert.That(scene.Bootstrap.Inventory.State.Coins, Is.EqualTo(100)); Assert.That(scene.Inventory.Saved.Coins, Is.EqualTo(100));
                scene.Tap(blocked.X, blocked.Y); daily.Fail.RestartButton.onClick.Invoke();
                Assert.That(daily.Hearts, Is.EqualTo(3)); Assert.That(scene.Game.MistakeCount, Is.Zero);
                scene.Tap(blocked.X, blocked.Y); scene.Tap(blocked.X, blocked.Y); scene.Tap(blocked.X, blocked.Y);
                daily.Fail.HomeButton.onClick.Invoke(); Assert.That(flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu)); Assert.That(daily.Fail.IsOpen, Is.False);
            }
        }

        /// <summary>A clear starts the full-screen confetti and the card pop; the victory fanfare replaces the jingle.</summary>
        [Test] public void VictoryCelebration()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.MainMenu.PlayButton.onClick.Invoke(); scene.Clear();
                var panel = scene.Hud.CompletePanel; Assert.That(panel.IsVisible, Is.True);
                Assert.That(panel.Confetti.IsPlaying, Is.True);
                var rect = panel.Confetti.rectTransform; Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero)); Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
                panel.Confetti.Tick(0.5f); Assert.That(panel.Confetti.IsPlaying, Is.True);
                for (int i = 0; i < 400 && panel.Confetti.IsPlaying; i++) panel.Confetti.Tick(0.05f);
                Assert.That(panel.Confetti.IsPlaying, Is.False);
                float[] fanfare = VictoryMusic.Render(SoundDesign.SampleRate);
                Assert.That(fanfare.Length, Is.EqualTo((int)(VictoryMusic.Seconds * SoundDesign.SampleRate)).Within(2));
                float peak = 0; foreach (float v in fanfare) peak = Mathf.Max(peak, Mathf.Abs(v));
                Assert.That(peak, Is.LessThanOrEqualTo(1f).And.GreaterThan(0.5f));
            }
        }

        /// <summary>JOURNEY sits under PLAY; the map scrolls to the current level and starts unlocked levels only.</summary>
        [Test] public void JourneyMap()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow; var menu = flow.MainMenu;
                var play = (RectTransform)menu.PlayButton.transform; var journey = (RectTransform)menu.JourneyButton.transform;
                Assert.That(journey.anchoredPosition.y, Is.LessThan(play.anchoredPosition.y - play.sizeDelta.y * 0.5f));
                Assert.That(journey.anchoredPosition.y - journey.sizeDelta.y * 0.5f, Is.GreaterThan(((RectTransform)menu.LevelsButton.transform).anchoredPosition.y + MainMenuView.TileSize * 0.5f));
                menu.JourneyButton.onClick.Invoke();
                Assert.That(flow.Journey.IsVisible, Is.True); Assert.That(menu.Root.gameObject.activeSelf, Is.False);
                Assert.That(flow.Journey.CurrentIndex, Is.EqualTo(scene.Bootstrap.ContinueIndex));
                var shown = flow.Journey.Nodes.Where(n => n.gameObject.activeSelf).ToList();
                Assert.That(shown.Count, Is.GreaterThan(0)); Assert.That(shown.Any(n => n.Index == flow.Journey.CurrentIndex), Is.True);
                var locked = shown.FirstOrDefault(n => n.State == LevelTileState.Locked);
                if (locked != null) { locked.Button.onClick.Invoke(); Assert.That(flow.Journey.IsVisible, Is.True, "locked levels do not start"); }
                Assert.That(flow.HandleBack(), Is.True); Assert.That(flow.Journey.IsVisible, Is.False); Assert.That(menu.Root.gameObject.activeSelf, Is.True);
                menu.JourneyButton.onClick.Invoke();
                flow.Journey.PlayButton.onClick.Invoke();
                Assert.That(flow.Machine.Current, Is.EqualTo(ScreenState.Playing)); Assert.That(flow.Journey.IsVisible, Is.False);
                Assert.That(scene.Bootstrap.CurrentLevelIndex, Is.EqualTo(scene.Bootstrap.ContinueIndex));
            }
        }

        /// <summary>The road map winds left and right and keeps every level reachable on one path.</summary>
        [Test] public void JourneyLayoutWinds()
        {
            float min = 0, max = 0;
            for (int i = 0; i < 360; i++) { min = Mathf.Min(min, JourneyLayout.X(i)); max = Mathf.Max(max, JourneyLayout.X(i)); Assert.That(JourneyLayout.Y(i + 1), Is.GreaterThan(JourneyLayout.Y(i))); }
            Assert.That(min, Is.LessThan(-200)); Assert.That(max, Is.GreaterThan(200)); Assert.That(max, Is.LessThanOrEqualTo(JourneyLayout.Amplitude));
            JourneyLayout.VisibleRange(JourneyLayout.Y(100), JourneyLayout.Y(108), 360, out int first, out int last);
            Assert.That(first, Is.LessThanOrEqualTo(100)); Assert.That(last, Is.GreaterThanOrEqualTo(108)); Assert.That(last - first, Is.LessThan(16));
            Assert.That(JourneyLayout.IsMilestone(9), Is.True); Assert.That(JourneyLayout.IsMilestone(10), Is.False);
            Assert.That(JourneyLayout.WorldName(1), Is.Not.Empty);
        }

        /// <summary>Home elements wear the 3D glass finish; the avatar animates on the home page only and survives re-equipping.</summary>
        [Test] public void GlassHomeAndAnimatedAvatar()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var menu = scene.Flow.MainMenu;
                foreach (var button in new[] { menu.LevelsButton, menu.ShopButton, menu.RankingButton, menu.SettingsButton, menu.DailyButton, menu.RewardsButton, menu.CoinsButton, menu.StarsButton, menu.JourneyButton })
                {
                    var image = (UnityEngine.UI.Image)button.targetGraphic;
                    Assert.That(image.sprite, Is.Not.Null, button.name); Assert.That(image.sprite.name, Does.StartWith("Glass/glass_"), button.name);
                    Assert.That(button.transform.Find(GlassKit.GlossName), Is.Not.Null, button.name);
                }
                var pulse = menu.AvatarMotion; Assert.That(pulse, Is.Not.Null); Assert.That(pulse.Badge, Is.SameAs(menu.Avatar));
                var frame = menu.Avatar.FrameImage.rectTransform;
                menu.Tick(0.4f); var before = frame.localScale; var spin = pulse.Ring.localEulerAngles.z;
                menu.Tick(0.3f); Assert.That(frame.localScale, Is.Not.EqualTo(before)); Assert.That(pulse.Ring.localEulerAngles.z, Is.Not.EqualTo(spin));
                scene.Bootstrap.Inventory.Shop.AddCoins(5000);
                string avatar = ShopCatalog.Items.First(i => i.Slot == ShopSlot.Avatar && i.Price > 0).Id;
                scene.Bootstrap.Inventory.TryBuy(avatar); scene.Bootstrap.Inventory.TryEquip(avatar);
                Assert.That(menu.Avatar.AvatarId, Is.EqualTo(avatar));
                float clock = pulse.Clock; menu.Tick(0.2f); Assert.That(pulse.Clock, Is.GreaterThan(clock), "still animating after the avatar changed");
                scene.Flow.OpenProfile(); Assert.That(scene.Flow.Profile.Root.GetComponentsInChildren<AvatarPulse>(true), Is.Empty, "home page only");
                clock = pulse.Clock; menu.Tick(0.2f); Assert.That(pulse.Clock, Is.EqualTo(clock), "frozen while the home page is hidden");
            }
        }

        /// <summary>Screens fade and settle in Play mode; Edit Mode and hidden screens stay settled.</summary>
        [Test] public void ScreenTransitionsSettle()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var appear = scene.Flow.MainMenu.Root.GetComponent<ScreenAppear>(); Assert.That(appear, Is.Not.Null);
                Assert.That(appear.IsPlaying, Is.False); Assert.That(appear.Group.alpha, Is.EqualTo(1));
                appear.Play(); Assert.That(appear.Group.alpha, Is.Zero);
                appear.Tick(ScreenAppear.Duration * 0.5f); Assert.That(appear.Group.alpha, Is.GreaterThan(0.5f).And.LessThan(1));
                appear.Tick(1); Assert.That(appear.Group.alpha, Is.EqualTo(1)); Assert.That(appear.IsPlaying, Is.False);
            }
        }
    }
}
