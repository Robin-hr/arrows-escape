using System.Reflection;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace ArrowEscape.Tests
{
    /// <summary>Independent receipts, write ordering and crash recovery with fake stores.</summary>
    public sealed class RewardFlowTests
    {
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void AwardOnceBeforeProgressAndReplay()
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.ShowLevel(0);int progress=scene.Progress.Saves;scene.Inventory.BeforeSave=()=>Assert.That(scene.Progress.Saves,Is.EqualTo(progress));scene.Clear();scene.Inventory.BeforeSave=null;
                Assert.That(scene.Inventory.Saved.Coins,Is.EqualTo(40));Assert.That(scene.Inventory.Saves,Is.EqualTo(1));Assert.That(scene.Progress.Saves,Is.EqualTo(progress+1));
                typeof(ArrowEscape.View.GameBootstrap).GetMethod("OnCleared",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scene.Bootstrap,null);Assert.That(scene.Inventory.Saved.Coins,Is.EqualTo(40));Assert.That(scene.Inventory.Saves,Is.EqualTo(1));
                scene.Bootstrap.RestartLevel();scene.Clear();Assert.That(scene.Inventory.Saved.Coins,Is.EqualTo(45));
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void RestartIssuesFreshIdAndSandboxPaysNothing()
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.ShowLevel(0);int first=scene.Bootstrap.CurrentAttemptId;scene.Game.RestartLevel();Assert.That(scene.Bootstrap.CurrentAttemptId,Is.GreaterThan(first));
                scene.Bootstrap.TryStartSandboxLevel("TestLevels/path_001");scene.Clear();Assert.That(scene.Inventory.Saves,Is.Zero);Assert.That(scene.Bootstrap.LastCoinReward,Is.Zero);Assert.That(scene.Bootstrap.Inventory.State.Coins,Is.Zero);
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void CrashAfterInventoryBeforeProgress()
        {
            var inventory=new FakeInventoryStore();var progress=new FakeProgressStore();int paid;
            using(var scene=new EconomyTestScene(inventory:inventory,progress:progress))
            {
                scene.Bootstrap.ShowLevel(0);paid=scene.Bootstrap.CurrentAttemptId;progress.FailSave=true;
                LogAssert.Expect(LogType.Warning,"ArrowEscape progress could not be saved. Play continues with in-memory progress.");scene.Clear();
                Assert.That(inventory.Saved.Coins,Is.EqualTo(40));Assert.That(progress.Saved.levels,Is.Empty);
            }
            progress.FailSave=false;
            using(var restarted=new EconomyTestScene(inventory:inventory,progress:progress))
            {
                Assert.That(restarted.Bootstrap.Inventory.AwardLevelClear("level_001",3,paid),Is.Zero);Assert.That(restarted.Bootstrap.Inventory.State.Coins,Is.EqualTo(40));
                restarted.Bootstrap.ShowLevel(0);Assert.That(restarted.Bootstrap.CurrentAttemptId,Is.GreaterThan(paid));restarted.Clear();Assert.That(inventory.Saved.Coins,Is.EqualTo(45));
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void RewardsImproveFromOneToThreeStars()
        {using(var scene=new EconomyTestScene()){scene.Bootstrap.ShowLevel(0);scene.Clear(10);Assert.That(scene.Bootstrap.LastCoinReward,Is.EqualTo(20));scene.Bootstrap.RestartLevel();scene.Clear();Assert.That(scene.Bootstrap.LastCoinReward,Is.EqualTo(25));Assert.That(scene.Inventory.Saved.Coins,Is.EqualTo(45));}}
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void FailedInventorySaveDoesNotCommitProgress()
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.ShowLevel(0);scene.Inventory.FailSave=true;int before=scene.Progress.Saves;
                LogAssert.Expect(LogType.Warning,"ArrowEscape inventory could not be saved. State remains in memory.");scene.Clear();
                Assert.That(scene.Progress.Saves,Is.EqualTo(before));Assert.That(scene.Bootstrap.Progress.Tracker.IsCompleted("level_001"),Is.True);Assert.That(scene.Bootstrap.Inventory.State.Coins,Is.EqualTo(40));
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void CoinsPresentAfterStarsAndRespectSettings()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                var sounds=new RecordingSoundPlayer();var motors=new RecordingHapticsPlayer();var settings=scene.Bootstrap.Settings.Model;
                var audio=scene.Game.gameObject.AddComponent<ArrowEscape.View.AudioDirector>();audio.Initialize(scene.Game,settings,sounds);
                var haptic=scene.Game.gameObject.AddComponent<ArrowEscape.View.HapticsDirector>();haptic.Initialize(scene.Game,settings,motors,()=>0);
                scene.Game.gameObject.AddComponent<ArrowEscape.View.EconomyFeedback>().Bind(scene.Game,audio,haptic);
                scene.Bootstrap.ShowLevel(0);scene.Clear();sounds.Events.Clear();motors.Cues.Clear();var panel=scene.Hud.CompletePanel;
                panel.Tick(.74f);Assert.That(panel.CoinsText.gameObject.activeInHierarchy,Is.False);panel.Tick(.02f);Assert.That(panel.CoinsText.gameObject.activeInHierarchy,Is.True);panel.Tick(.6f);Assert.That(panel.CoinsText.text,Is.EqualTo("+40"));
                Assert.That(sounds.Events.FindAll(e=>e.Cue==SoundCue.Coin).Count,Is.EqualTo(1));Assert.That(motors.Cues.FindAll(c=>c==HapticCue.Coin).Count,Is.EqualTo(1));
                scene.Bootstrap.RestartLevel();settings.SoundEnabled=false;settings.HapticsEnabled=false;scene.Clear();sounds.Events.Clear();motors.Cues.Clear();panel.Tick(2);Assert.That(sounds.Events,Is.Empty);Assert.That(motors.Cues,Is.Empty);
            }
        }
    }
}
