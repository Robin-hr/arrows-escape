using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14L: name style, streak ranks, 3D gift, 3D menu icons, menu music and shop tap sounds.</summary>
    public sealed class Step14LTests
    {
        /// <summary>The profile name is centred, orange and in the Titan One game font.</summary>
        [Test] public void NameIsCentredOrangeTitanOne()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow; flow.OpenProfile();
                var name = flow.Profile.NameText;
                Assert.That(name.rectTransform.anchoredPosition.x, Is.EqualTo(0).Within(0.01f)); Assert.That(name.alignment, Is.EqualTo(TextAnchor.MiddleCenter));
                Assert.That(name.color, Is.EqualTo(BrandArt.NameOrange)); Assert.That(flow.MainMenu.PlayerNameText.color, Is.EqualTo(BrandArt.NameOrange));
                Assert.That(BrandArt.NameFont.name, Does.Contain("Titan"));
            }
        }

        /// <summary>Streak badges are ranked Rookie (100 days) to Elite (1,000 days).</summary>
        [Test] public void StreakBadgeRanks()
        {
            Assert.That(DailyStreakRules.BadgeName(100), Is.EqualTo("ROOKIE")); Assert.That(DailyStreakRules.BadgeName(1000), Is.EqualTo("ELITE"));
            Assert.That(DailyStreakRules.Badges.Select(DailyStreakRules.BadgeName).Distinct().Count(), Is.EqualTo(10));
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var badge = scene.Flow.DailyPage.Badges[0];
                Assert.That(badge.transform.Find("Name").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("ROOKIE"));
            }
        }

        /// <summary>The gift is a 3D box: it turns while closed and opens with confetti and the reward.</summary>
        [Test] public void GiftBoxIs3D()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var gift = scene.Flow.Gift; gift.Show(new DailyReward(DailyRewardKind.Coins, 100), null);
                Assert.That(BrandArt.Texture("Gift/gift3d_atlas"), Is.Not.Null);
                int first = gift.Frame; for (int i = 0; i < 20; i++) gift.Tick(0.05f);
                Assert.That(gift.Frame, Is.Not.EqualTo(first), "the closed box turns"); Assert.That(gift.Frame, Is.LessThan(GiftBoxView.SpinFrames));
                gift.OpenButton.onClick.Invoke();
                Assert.That(gift.IsRevealed, Is.True); Assert.That(gift.Frame, Is.EqualTo(GiftBoxView.SpinFrames + GiftBoxView.OpenFrames - 1), "ends on the open box");
                Assert.That(gift.ResultText.text, Does.StartWith("YOU GOT"));
            }
        }

        /// <summary>Menu icons are the 3D set and PLAY and JOURNEY share one size.</summary>
        [Test] public void MenuUses3DIconsAndEqualButtons()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var menu = scene.Flow.MainMenu;
                Assert.That(((RectTransform)menu.PlayButton.transform).sizeDelta, Is.EqualTo(((RectTransform)menu.JourneyButton.transform).sizeDelta));
                foreach (var button in new[] { menu.LevelsButton, menu.ShopButton, menu.RankingButton, menu.SettingsButton, menu.DailyButton, menu.RewardsButton, menu.JourneyButton })
                {
                    var icon = button.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>();
                    Assert.That(icon.sprite, Is.Not.Null, button.name); Assert.That(icon.sprite.name, Does.StartWith("Icons3D/"), button.name);
                }
                Assert.That(menu.PlayButton.transform.Find("PlayIcon").GetComponent<UnityEngine.UI.Image>().sprite.name, Is.EqualTo("Icons3D/i3_play"));
            }
        }

        /// <summary>Menu music loops seamlessly and is wanted on the menus only.</summary>
        [Test] public void MenuMusicOnMenusOnly()
        {
            float[] music = MenuMusic.Render(SoundDesign.SampleRate);
            Assert.That(music.Length, Is.EqualTo((int)(MenuMusic.Seconds * SoundDesign.SampleRate)));
            float peak = 0; foreach (float v in music) { Assert.That(float.IsNaN(v), Is.False); peak = Mathf.Max(peak, Mathf.Abs(v)); }
            Assert.That(peak, Is.InRange(0.3f, 0.7f));
            Assert.That(Mathf.Abs(music[0] - music[music.Length - 1]), Is.LessThan(0.08f), "no click at the loop point");
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow; Assert.That(flow.Music.Wanted, Is.True);
                flow.MainMenu.PlayButton.onClick.Invoke(); Assert.That(flow.Music.Wanted, Is.False, "silent over the board");
                flow.HandleBack(); flow.PauseScreen.MainMenuButton.onClick.Invoke(); Assert.That(flow.Music.Wanted, Is.True);
                flow.OpenShop(); Assert.That(flow.Music.Wanted, Is.True);
                scene.Bootstrap.Settings.Model.SoundEnabled = false; Assert.That(flow.Music.Audible, Is.False);
            }
        }

        /// <summary>The shop sells ten named tap sounds; equipping one changes the exit melody's instrument.</summary>
        [Test] public void ShopTapSounds()
        {
            var sounds = ShopCatalog.Items.Where(i => i.Slot == ShopSlot.Sound).ToArray();
            Assert.That(sounds.Length, Is.EqualTo(13)); Assert.That(sounds.Count(i => i.Price > 0), Is.EqualTo(12));
            Assert.That(sounds.Select(i => i.Name).Distinct().Count(), Is.EqualTo(13));
            Assert.That(sounds.Select(i => i.Id), Is.EqualTo(TapSoundSynth.Ids));
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var flow = scene.Flow; flow.OpenShop(); flow.Shop.SoundsButton.onClick.Invoke();
                Assert.That(flow.Shop.Cards.Count, Is.EqualTo(13)); Assert.That(flow.Shop.Cards.All(c => c.IsSound && c.Preview.texture != null), Is.True);
                Assert.That(scene.Bootstrap.Inventory.State.EquippedSoundId, Is.EqualTo(TapSoundSynth.DefaultId));
                scene.Bootstrap.Inventory.Shop.AddCoins(1000);
                Assert.That(scene.Bootstrap.Inventory.TryBuy("sound_kalimba"), Is.True); Assert.That(scene.Bootstrap.Inventory.TryEquip("sound_kalimba"), Is.True);
                Assert.That(scene.Inventory.Saved.EquippedSoundId, Is.EqualTo("sound_kalimba"));
                flow.Shop.OpenDetail(3); Assert.That(flow.Shop.Detail.SampleButton.gameObject.activeSelf, Is.True);
            }
            var audio = new ProceduralAudio();
            try
            {
                audio.TapInstrument = TapSoundSynth.IndexOf("sound_kalimba");
                var clip = audio.GetClip(new SoundEvent(SoundCue.Exit, 2, 0.6f)); Assert.That(clip, Is.Not.Null); Assert.That(clip.name, Is.EqualTo("Tap 2 2"));
                var hint = audio.GetClip(new SoundEvent(SoundCue.Hint, 2, 0.4f)); Assert.That(hint.name, Does.StartWith("Pentatonic"), "hints keep the classic chime");
                audio.TapInstrument = 0; Assert.That(audio.GetClip(new SoundEvent(SoundCue.Exit, 2, 0.6f)).name, Does.StartWith("Pentatonic"));
            }
            finally { audio.Dispose(); }
            for (int i = 1; i < TapSoundSynth.Count; i++)
            {
                var a = TapSoundSynth.Render(i, 440f, SoundDesign.SampleRate); float peak = 0; foreach (float v in a) peak = Mathf.Max(peak, Mathf.Abs(v));
                Assert.That(peak, Is.InRange(0.4f, 0.85f), TapSoundSynth.Ids[i]);
            }
        }
    }
}
