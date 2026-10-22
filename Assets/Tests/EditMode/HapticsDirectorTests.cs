using System.Reflection;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace ArrowEscape.Tests
{
    /// <summary>Drives haptic gameplay events with controlled preferences and a fake clock.</summary>
    public class HapticsDirectorTests
    {
        private AudioTestScene scene;private HapticsDirector director;private RecordingHapticsPlayer player;private float now;
        /// <summary>Injects fake hardware and time into real gameplay.</summary>
        [SetUp]public void Setup(){scene=new AudioTestScene();player=new RecordingHapticsPlayer();director=scene.Host.AddComponent<HapticsDirector>();now=0;director.Initialize(scene.Game,scene.Settings,player,()=>now);}
        /// <summary>Destroys subscribed directors and all runtime resources.</summary>
        [TearDown]public void Cleanup()=>scene.Dispose();
        /// <summary>Exit, blocked, undo, and completion have their own cues; hints have none.</summary>
        [Test]public void CueEvents()
        {scene.Game.StartLevel(LevelRepository.LoadFromResources("TestLevels/legacy_level_001").Board);scene.Game.ShowHint();Assert.That(player.Cues,Is.Empty);scene.TapId(1);scene.TapId(4);scene.Game.Undo();Assert.That(player.Cues,Is.EqualTo(new[]{HapticCue.Blocked,HapticCue.Exit,HapticCue.Undo}));scene.Game.StartLevel(AudioTestScene.FreeBoard(1));player.Cues.Clear();scene.TapId(1);Assert.That(player.Cues,Is.EqualTo(new[]{HapticCue.Exit,HapticCue.LevelComplete}));}
        /// <summary>Live preferences silence/cancel immediately and only haptics-on previews.</summary>
        [Test]public void Settings()
        {int cancels=player.Cancels;scene.Settings.HapticsEnabled=false;Assert.That(player.Cancels,Is.EqualTo(cancels+1));scene.TapId(1);Assert.That(player.Cues,Is.Empty);scene.Settings.SoundEnabled=false;Assert.That(player.Cancels,Is.EqualTo(cancels+1));scene.Settings.HapticsEnabled=true;Assert.That(player.Cues,Is.EqualTo(new[]{HapticCue.Preview}));scene.Settings.SoundEnabled=true;Assert.That(player.Cues.Count,Is.EqualTo(1));Assert.That(player.Cancels,Is.EqualTo(cancels+1));}
        /// <summary>Unsupported platforms are silent no-ops.</summary>
        [Test]public void Unsupported(){player.IsSupported=false;Assert.DoesNotThrow(()=>scene.TapId(1));scene.Settings.HapticsEnabled=false;scene.Settings.HapticsEnabled=true;Assert.That(player.Cues,Is.Empty);}
        /// <summary>Rapid exits share one throttle; blocked taps have an independent window.</summary>
        [Test]public void Throttled()
        {scene.Game.StartLevel(AudioTestScene.FreeBoard(22));for(int i=1;i<=20;i++){scene.TapId(i);now+=.005f;}Assert.That(player.Cues.Count,Is.InRange(2,3));int count=player.Cues.Count;now+=1;scene.TapId(21);Assert.That(player.Cues.Count,Is.EqualTo(count+1));scene.Game.StartLevel(LevelRepository.LoadFromResources("TestLevels/legacy_level_001").Board);scene.TapId(4);scene.TapId(1);scene.TapId(1);Assert.That(player.Cues[player.Cues.Count-1],Is.EqualTo(HapticCue.Blocked));}
        /// <summary>Level start and application pause cancel; paused taps do nothing.</summary>
        [Test]public void PauseAndLevelStart()
        {int before=player.Cancels;scene.Game.RestartLevel();Assert.That(player.Cancels,Is.EqualTo(before+1));typeof(HapticsDirector).GetMethod("OnApplicationPause",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(director,new object[]{true});Assert.That(player.Cancels,Is.EqualTo(before+2));scene.Game.SetPaused(true);scene.TapId(1);Assert.That(player.Cues,Is.Empty);}
        /// <summary>Destroy unsubscribes and cancels; later settings/game events are harmless.</summary>
        [Test]public void DestroyUnsubscribes(){int before=player.Cancels;Object.DestroyImmediate(director);Assert.That(player.Cancels,Is.EqualTo(before+1));scene.TapId(1);scene.Settings.HapticsEnabled=false;scene.Settings.HapticsEnabled=true;Assert.That(player.Cues,Is.Empty);Assert.That(player.Cancels,Is.EqualTo(before+1));}
        /// <summary>A null settings model means always enabled.</summary>
        [Test]public void NullSettings(){director.Initialize(scene.Game,null,player,()=>now);scene.Settings.HapticsEnabled=false;scene.TapId(1);Assert.That(player.Cues,Is.EqualTo(new[]{HapticCue.Exit}));}
        /// <summary>A missing controller warns once and disables the component.</summary>
        [Test]public void MissingGame(){var host=new GameObject("Missing haptic game");try{var d=host.AddComponent<HapticsDirector>();LogAssert.Expect(LogType.Warning,"ArrowEscape haptics: no GameController found; haptics disabled.");d.Initialize(null,null,player,()=>now);d.Initialize(null,null,player,()=>now);Assert.That(d.enabled,Is.False);}finally{Object.DestroyImmediate(host);}}
    }
}
