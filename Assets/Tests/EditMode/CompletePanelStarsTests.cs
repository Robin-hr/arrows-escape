using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14A regression coverage with isolated state and no player-save access.</summary>
    public sealed class CompletePanelStarsTests
    {
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [TestCase(0,3)] [TestCase(3,2)] [TestCase(10,1)]
        public void EarnedSlotsAndOneCuePerStar(int wasted, int expected)
        {
            using(var scene=new ProgressTestScene(withUi:true))
            {
                var settings=new SettingsModel();var sound=new RecordingSoundPlayer();var haptics=new RecordingHapticsPlayer();
                var audio=scene.Game.gameObject.AddComponent<AudioDirector>();audio.Initialize(scene.Game,settings,sound);
                var motor=scene.Game.gameObject.AddComponent<HapticsDirector>();motor.Initialize(scene.Game,settings,haptics,()=>0);
                scene.Bootstrap.ShowLevel(0);for(int i=0;i<wasted;i++)scene.Tap(0,0);scene.Clear();
                var panel=scene.Hud.CompletePanel;Assert.That(panel.Stars.Count,Is.EqualTo(expected));
                Assert.That(panel.MovesText.text,Is.EqualTo((8+wasted)+" / 8 moves"));
                sound.Events.Clear();haptics.Cues.Clear();
                panel.Tick(.01f);Assert.That(sound.Events.Count,Is.EqualTo(1));
                panel.Tick(.23f);Assert.That(sound.Events.Count,Is.EqualTo(1));
                panel.Tick(1);panel.Tick(10);
                Assert.That(sound.Events.Count,Is.EqualTo(expected));Assert.That(haptics.Cues.Count,Is.EqualTo(expected));
                for(int i=0;i<expected;i++)
                {Assert.That(sound.Events[i].Cue,Is.EqualTo(SoundCue.Star));Assert.That(sound.Events[i].ScaleStep,Is.EqualTo(5+i*2));Assert.That(haptics.Cues[i],Is.EqualTo(HapticCue.Star));}
                for(int i=0;i<3;i++)
                {Assert.That(panel.Stars.Slot(i).color,Is.EqualTo(i<expected?ViewTheme.StarGold:ViewTheme.StarGrey));Assert.That(panel.Stars.Slot(i).transform.localScale,Is.EqualTo(Vector3.one));}
            }
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void SettingsAndPauseSuppressFeedback()
        {
            using(var scene=new ProgressTestScene(withUi:true))
            {
                var settings=new SettingsModel();var sound=new RecordingSoundPlayer();var haptics=new RecordingHapticsPlayer();
                scene.Game.gameObject.AddComponent<AudioDirector>().Initialize(scene.Game,settings,sound);
                scene.Game.gameObject.AddComponent<HapticsDirector>().Initialize(scene.Game,settings,haptics,()=>0);
                scene.Bootstrap.ShowLevel(0);scene.Clear();sound.Events.Clear();haptics.Cues.Clear();var panel=scene.Hud.CompletePanel;
                scene.Game.SetPaused(true);panel.Tick(1);Assert.That(sound.Events,Is.Empty);Assert.That(haptics.Cues,Is.Empty);
                scene.Game.SetPaused(false);settings.SoundEnabled=false;settings.HapticsEnabled=false;panel.Tick(1);
                Assert.That(sound.Events,Is.Empty);Assert.That(haptics.Cues,Is.Empty);Assert.That(panel.Stars.Slot(0).transform.localScale,Is.EqualTo(Vector3.one));
            }
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void NewBestAndPracticeLabels()
        {
            using(var scene=new ProgressTestScene(withUi:true))
            {
                scene.Bootstrap.ShowLevel(0);for(int i=0;i<10;i++)scene.Tap(0,0);scene.Clear();
                var panel=scene.Hud.CompletePanel;Assert.That(panel.BestNoticeText.text,Is.EqualTo("First clear!"));
                scene.Bootstrap.RestartLevel();scene.Clear();Assert.That(panel.BestNoticeText.text,Is.EqualTo("New best!"));
                scene.Bootstrap.RestartLevel();scene.Clear();Assert.That(panel.BestNoticeText.gameObject.activeSelf,Is.False);
                scene.Bootstrap.TryStartSandboxLevel("TestLevels/path_001");scene.Clear();Assert.That(panel.BestNoticeText.text,Is.EqualTo("Practice - not saved"));
            }
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void RebindingDoesNotReplayStarFeedback()
        {
            using(var scene=new ProgressTestScene(withUi:true))
            {
                scene.Bootstrap.ShowLevel(0);scene.Clear();int count=0;scene.Game.StarPresented+=_=>count++;
                scene.Hud.CompletePanel.Tick(1);scene.Hud.Bind(scene.Game,scene.Bootstrap);scene.Hud.CompletePanel.Tick(1);
                Assert.That(count,Is.EqualTo(3));
            }
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void TickWithoutSubscribersAllocatesNothing()
        {
            var host = new GameObject("Star animation", typeof(RectTransform));
            try
            {
                var root = ArrowEscape.UI.UiFactory.Rect("Stars",host.transform,new Vector2(120,40),Vector2.zero);
                var stars = root.gameObject.AddComponent<ArrowEscape.UI.StarDisplay>();stars.Build(host.transform,30,40);
                stars.Pop(3);stars.Tick(.001f);
                long before=System.GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<100;i++)stars.Tick(.001f);
                Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                stars.Tick(1);Assert.That(stars.Slot(2).transform.localScale,Is.EqualTo(Vector3.one));
            }
            finally { Object.DestroyImmediate(host); }
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void ProceduralStarsAreCachedAndDistinct()
        {
            Sprite filled=SpriteFactory.GetStarSprite(),outline=SpriteFactory.GetStarSprite(true);
            Assert.That(filled,Is.Not.SameAs(outline));Assert.That(SpriteFactory.GetStarSprite(),Is.SameAs(filled));
            Assert.That(filled.texture.GetPixel(32,32).a,Is.EqualTo(1));Assert.That(outline.texture.GetPixel(32,32).a,Is.Zero);
            Assert.That(filled.texture.GetPixel(0,0).a,Is.Zero);
            Object.DestroyImmediate(filled);Assert.That(SpriteFactory.GetStarSprite(),Is.Not.Null);
        }
    }
}
