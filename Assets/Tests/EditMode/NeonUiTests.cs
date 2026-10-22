using ArrowEscape.Core;
using ArrowEscape.UI;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Snake-arrow loader, loading screens, neon home page and profile page.</summary>
    public sealed class NeonUiTests
    {
        [Test] public void LoaderModelSolvesAndRepeats()
        {
            var model = new SnakeLoaderModel();
            Assert.That(model.SnakeCount, Is.EqualTo(11));
            Assert.That(model.Period, Is.GreaterThan(5f).And.LessThan(10f));
            Assert.That(model.StartedCount(0), Is.Zero);
            Assert.That(model.StartedCount(model.Period - 1.0f), Is.EqualTo(11));
            var xs = new float[model.MaxBodyPoints]; var ys = new float[model.MaxBodyPoints];
            model.Evaluate(0);
            for (int i = 0; i < model.SnakeCount; i++)
            {
                Assert.That(model.Opacity(i), Is.EqualTo(1f));
                Assert.That(model.GetBody(i, xs, ys), Is.GreaterThanOrEqualTo(2));
            }
            model.Evaluate(model.Period * 3 + 0.25f); float a = model.Opacity(0);
            model.Evaluate(0.25f); Assert.That(model.Opacity(0), Is.EqualTo(a), "the animation loops exactly");
        }

        [Test] public void LoaderFlashesTheBlocker()
        {
            var model = new SnakeLoaderModel(); bool flashed = false;
            for (float t = 0; t < model.Period; t += 0.05f) { model.Evaluate(t); if (model.ColorSlot(9) == SnakeLoaderModel.FlashColor) flashed = true; }
            Assert.That(flashed, Is.True);
        }

        [Test] public void LoaderEvaluateDoesNotAllocate()
        {
            var model = new SnakeLoaderModel(); var xs = new float[model.MaxBodyPoints]; var ys = new float[model.MaxBodyPoints];
            model.Evaluate(1); model.GetBody(0, xs, ys);
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 200; i++) { model.Evaluate(i * 0.04f); for (int s = 0; s < model.SnakeCount; s++) model.GetBody(s, xs, ys); }
            Assert.That(System.GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        [Test] public void DurationsAndSizes()
        {
            Assert.That(LoadingOverlay.DurationFor(LoadingKind.Startup), Is.EqualTo(3f));
            Assert.That(LoadingOverlay.DurationFor(LoadingKind.Level), Is.EqualTo(1f));
            Assert.That(LoadingOverlay.DurationFor(LoadingKind.Profile), Is.EqualTo(2f));
            Assert.That(LoadingOverlay.LoaderSizeFor(LoadingKind.Startup), Is.GreaterThan(LoadingOverlay.LoaderSizeFor(LoadingKind.Level)));
            Assert.That(LoadingOverlay.LoaderSizeFor(LoadingKind.Level), Is.GreaterThan(LoadingOverlay.LoaderSizeFor(LoadingKind.Profile)));
        }

        [Test] public void OverlayShowsForItsDurationThenCallsBackOnce()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var overlay = scene.Flow.Loading; int done = 0;
                overlay.Show(LoadingKind.Level, "LEVEL 1", () => done++);
                Assert.That(overlay.IsVisible, Is.True); Assert.That(overlay.Root.gameObject.activeSelf, Is.True);
                Assert.That(overlay.Caption.text, Is.EqualTo("LEVEL 1"));
                overlay.Tick(0.9f); Assert.That(overlay.IsVisible, Is.True);
                overlay.Tick(0.2f); overlay.Tick(0.2f);
                Assert.That(overlay.IsVisible, Is.False); Assert.That(done, Is.EqualTo(1));
                overlay.Tick(1f); Assert.That(done, Is.EqualTo(1));
            }
        }

        [Test] public void StartupAndProfileTimes()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var overlay = scene.Flow.Loading;
                overlay.Show(LoadingKind.Startup);
                overlay.Tick(2.9f); Assert.That(overlay.IsVisible, Is.True);
                overlay.Tick(0.5f); Assert.That(overlay.IsVisible, Is.False);
                overlay.Show(LoadingKind.Profile);
                overlay.Tick(1.9f); Assert.That(overlay.IsVisible, Is.True);
                overlay.Tick(0.5f); Assert.That(overlay.IsVisible, Is.False);
            }
        }

        [Test] public void LevelStartShowsLoaderAndBlocksBoardInput()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.LoadingScreensEnabled = true;
                var adapter = scene.Game.GetComponent<ArrowEscape.View.PointerInputAdapter>(); Vector2 point = new Vector2(-1000, -1000);
                scene.Flow.MainMenu.PlayButton.onClick.Invoke();
                Assert.That(scene.Flow.Machine.Current, Is.EqualTo(ScreenState.Playing));
                Assert.That(scene.Flow.Loading.IsVisible, Is.True); Assert.That(scene.Flow.Loading.Kind, Is.EqualTo(LoadingKind.Level));
                Assert.That(adapter.ShouldAcceptPointerDown(point), Is.False);
                scene.Flow.Loading.Tick(1.5f);
                Assert.That(scene.Flow.Loading.IsVisible, Is.False); Assert.That(adapter.ShouldAcceptPointerDown(point), Is.True);
            }
        }

        [Test] public void HomePageIsNeonAndKeepsNavigation()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var menu = scene.Flow.MainMenu;
                Assert.That(menu.Background, Is.TypeOf<NeonMenuBackground>());
                Assert.That(menu.PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("PLAY"));
                Assert.That(menu.PlayLevelText.text, Is.EqualTo("LEVEL 1"));
                menu.StarsButton.onClick.Invoke(); Assert.That(scene.Flow.Machine.Current, Is.EqualTo(ScreenState.LevelSelect));
            }
        }

        [Test] public void ProfileOpensWithStatsAndBackReturns()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Bootstrap.Inventory.Shop.AddCoins(120);
                scene.Flow.MainMenu.ProfileButton.onClick.Invoke();
                var profile = scene.Flow.Profile;
                Assert.That(profile.IsVisible, Is.True); Assert.That(scene.Flow.MainMenu.Root.gameObject.activeSelf, Is.False);
                Assert.That(profile.LevelsText.text, Is.EqualTo("0 / 3")); Assert.That(profile.StarsText.text, Is.EqualTo("0 / 9"));
                Assert.That(profile.CoinsText.text, Is.EqualTo("120")); Assert.That(profile.ArrowsText.text, Does.StartWith("1 / "));
                Assert.That(profile.EquippedText.text, Does.Contain("Classic"));
                Assert.That(scene.Flow.HandleBack(), Is.True);
                Assert.That(profile.IsVisible, Is.False); Assert.That(scene.Flow.MainMenu.Root.gameObject.activeSelf, Is.True);
            }
        }

        [Test] public void ProfileStatsCountPerfectLevels()
        {
            var tracker = new ProgressTracker();
            tracker.RecordCompletion("a", 0, 0, 5, 5); tracker.RecordCompletion("b", 0, 0, 9, 5);
            var stats = ProfileStats.Build(tracker, new[] { "a", "b", "c" }, new Inventory());
            Assert.That(stats.LevelsCompleted, Is.EqualTo(2)); Assert.That(stats.LevelCount, Is.EqualTo(3));
            Assert.That(stats.PerfectLevels, Is.EqualTo(1)); Assert.That(stats.MaxStars, Is.EqualTo(9));
            Assert.That(stats.EquippedTheme, Is.EqualTo("Paper"));
        }
    }
}
