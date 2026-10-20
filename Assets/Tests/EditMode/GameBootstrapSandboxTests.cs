using System.Text.RegularExpressions;
using ArrowEscape.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace ArrowEscape.Tests
{
    /// <summary>Sandbox attempts never write progression and replay the sandbox resource.</summary>
    public class GameBootstrapSandboxTests
    {
        /// <summary>Checks isolation and completion with isolated ice-arrow state.</summary>
        [Test]public void IsolationAndCompletion()
        {
            using(var s=new ProgressTestScene(withUi:true))
            {
                s.Bootstrap.StartInitialLevel();int saves=s.Store.Saves;string data=JsonUtility.ToJson(s.Bootstrap.Progress.Tracker.Data);
                int resume=s.Bootstrap.ContinueIndex;bool progress=s.Bootstrap.HasAnyProgress;
                Assert.That(s.Bootstrap.TryStartSandboxLevel("TestLevels/ice_001"),Is.True);Assert.That(s.Game.LevelName,Is.EqualTo("Ice 1"));
                CompletionResult completed=null;s.Bootstrap.LevelCompleted+=r=>completed=r;
                foreach(int id in new[]{1,4,3,2,1})
                {
                    var a=s.Game.Session.Board.GetArrowById(id);
                    if(s.Game.Session.Board.Arrows.Count==1)LogAssert.Expect(LogType.Log,"Level cleared. Mistakes: 0");
                    s.Tap(a.X,a.Y);
                }
                Assert.That(completed.Completions,Is.Zero);Assert.That(completed.IsNewBest||completed.IsFirstClear,Is.False);
                Assert.That(s.Hud.CompletePanel.gameObject.activeSelf,Is.True);
                s.Bootstrap.NextLevel();Assert.That(s.Bootstrap.IsSandboxLevel,Is.True);Assert.That(s.Bootstrap.CanGoNext,Is.False);Assert.That(s.Bootstrap.IsLastLevel,Is.True);
                s.Bootstrap.RestartLevel();Assert.That(s.Game.Session.Board.Arrows.Count,Is.EqualTo(4));s.Game.RestartLevel();Assert.That(s.Game.LevelName,Is.EqualTo("Ice 1"));
                Assert.That(s.Store.Saves,Is.EqualTo(saves));Assert.That(JsonUtility.ToJson(s.Bootstrap.Progress.Tracker.Data),Is.EqualTo(data));
                Assert.That(s.Bootstrap.ContinueIndex,Is.EqualTo(resume));Assert.That(s.Bootstrap.HasAnyProgress,Is.EqualTo(progress));
                Assert.That(s.Bootstrap.TryStartLevel(0),Is.True);Assert.That(s.Bootstrap.IsSandboxLevel,Is.False);s.Clear();Assert.That(s.Bootstrap.Progress.Tracker.IsCompleted("level_001"),Is.True);
            }
        }
        /// <summary>Checks missing and leaving with isolated ice-arrow state.</summary>
        [Test]public void MissingAndLeaving()
        {
            using(var s=new ProgressTestScene())
            {
                s.Bootstrap.TryStartSandboxLevel("TestLevels/ice_001");LogAssert.Expect(LogType.Warning,new Regex(".*[Mm]issing.*|.*not.*found.*|.*does not.*",RegexOptions.Singleline));
                Assert.That(s.Bootstrap.TryStartSandboxLevel("TestLevels/missing"),Is.False);Assert.That(s.Bootstrap.IsSandboxLevel,Is.True);
                s.Bootstrap.PreviousLevel();Assert.That(s.Bootstrap.IsSandboxLevel,Is.False);
                s.Bootstrap.TryStartSandboxLevel("TestLevels/ice_002");s.Bootstrap.DebugJump(1);Assert.That(s.Bootstrap.IsSandboxLevel,Is.False);
                s.Bootstrap.TryStartSandboxLevel("TestLevels/ice_001");s.Bootstrap.TryStartLevel(-1);Assert.That(s.Bootstrap.IsSandboxLevel,Is.False);
            }
        }
    }
}
