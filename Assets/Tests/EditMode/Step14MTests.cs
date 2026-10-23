using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14M: victory/lose/purchase/wrong sounds, Journey restart cost, Home on the complete card, frame-only animation.</summary>
    public sealed class Step14MTests
    {
        private static float Peak(float[] samples) { float p = 0; foreach (float v in samples) { Assert.That(float.IsNaN(v), Is.False); p = Mathf.Max(p, Mathf.Abs(v)); } return p; }

        /// <summary>All new sound effects are healthy, deterministic and sized as designed.</summary>
        [Test] public void NewSoundsAreHealthy()
        {
            int rate = SoundDesign.SampleRate;
            Assert.That(VictoryMusic.Render(rate).Length, Is.EqualTo((int)(VictoryMusic.Seconds * rate))); Assert.That(Peak(VictoryMusic.Render(rate)), Is.EqualTo(0.85f).Within(0.01f));
            Assert.That(GameSounds.LoseMusic(rate).Length, Is.EqualTo((int)(GameSounds.LoseSeconds * rate))); Assert.That(Peak(GameSounds.LoseMusic(rate)), Is.InRange(0.5f, 0.85f));
            Assert.That(Peak(GameSounds.CashRegister(rate)), Is.InRange(0.5f, 0.8f)); Assert.That(Peak(GameSounds.WrongBuzz(rate)), Is.InRange(0.4f, 0.65f));
            Assert.That(GameSounds.WrongBuzz(rate), Is.EqualTo(GameSounds.WrongBuzz(rate)));
            Assert.That(SoundPlanner.ForLose().Cue, Is.EqualTo(SoundCue.Lose));
            foreach (string id in new[] { "sound_memeboom", "sound_pipeclang" })
            {
                var item = ShopCatalog.Find(id); Assert.That(item, Is.Not.Null); Assert.That(item.Category, Is.EqualTo("Premium Sounds")); Assert.That(item.Price, Is.GreaterThanOrEqualTo(1000));
                Assert.That(Peak(TapSoundSynth.Render(TapSoundSynth.IndexOf(id), 330f, rate)), Is.InRange(0.5f, 0.85f));
            }
        }

        /// <summary>Restarting a Journey level from the HUD costs one of the five Journey lives.</summary>
        [Test] public void JourneyRestartCostsALife()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow; long now = 638650000000000000L; flow.UtcNow = () => now;
                flow.MainMenu.JourneyButton.onClick.Invoke(); flow.Journey.PlayButton.onClick.Invoke();
                Assert.That(scene.Bootstrap.Inventory.State.JourneyLives, Is.EqualTo(4));
                scene.Hud.Hud.RestartButton.onClick.Invoke();
                Assert.That(scene.Bootstrap.Inventory.State.JourneyLives, Is.EqualTo(3)); Assert.That(scene.Inventory.Saved.JourneyLives, Is.EqualTo(3));
                scene.Bootstrap.Inventory.State.JourneyLives = 0;
                scene.Hud.Hud.RestartButton.onClick.Invoke();
                Assert.That(flow.JourneyLives.IsOpen, Is.True, "no lives: the restart is refused and the lives panel opens");
            }
        }

        /// <summary>The level-complete card has a Home button that returns to the home page.</summary>
        [Test] public void CompleteCardHasHome()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.MainMenu.PlayButton.onClick.Invoke(); scene.Clear();
                var panel = scene.Hud.CompletePanel; Assert.That(panel.IsVisible, Is.True); Assert.That(panel.HomeButton.gameObject.activeInHierarchy, Is.True);
                panel.HomeButton.onClick.Invoke();
                Assert.That(scene.Flow.Machine.Current, Is.EqualTo(ScreenState.MainMenu)); Assert.That(scene.Flow.MainMenu.Root.gameObject.activeSelf, Is.True);
            }
        }

        /// <summary>Only premium frames animate: no ring light, glow or sparkles are added to the badge.</summary>
        [Test] public void OnlyTheFrameAnimates()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var badge = scene.Flow.MainMenu.Avatar;
                foreach (string name in new[] { "PulseRing", "PulseGlow", "Sparkle0" }) Assert.That(badge.transform.Find(name), Is.Null, name);
                var menu = scene.Flow.MainMenu;
                Assert.That(((UnityEngine.UI.Image)menu.DailyButton.targetGraphic).color, Is.EqualTo(GlassKit.Teal));
                Assert.That(((UnityEngine.UI.Image)menu.RewardsButton.targetGraphic).color, Is.EqualTo(GlassKit.Blue));
                Assert.That(menu.ShopButton.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>().sprite.name, Is.EqualTo("Icons3D/i3_shop"));
            }
        }
    }
}
