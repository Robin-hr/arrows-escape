using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace ArrowEscape.Tests
{
    /// <summary>Exercises observation, pause, menus and state independence through real taps.</summary>
    public class EffectsDirectorTests
    {
        /// <summary>Blocked taps start no effects; exits do; undo clears only streaks.</summary>
        [Test]public void ExitBlockedUndo()
        {using(var s=new EffectsTestScene(LevelRepository.LoadFromResources("TestLevels/legacy_level_001").Board)){s.Gameplay.TapId(1);Assert.That(s.Effects.HasActiveEffects,Is.False);s.Gameplay.TapId(4);Assert.That(s.Effects.Streaks.ActiveCount,Is.EqualTo(1));Assert.That(s.Effects.Particles.ActiveCount,Is.EqualTo(10));s.Gameplay.Game.Undo();Assert.That(s.Effects.Streaks.ActiveCount,Is.Zero);Assert.That(s.Effects.Particles.ActiveCount,Is.EqualTo(10));s.Gameplay.Game.RestartLevel();Assert.That(s.Effects.HasActiveEffects,Is.False);s.Gameplay.TapId(4);s.Gameplay.Game.StartLevel(AudioTestScene.FreeBoard(4));Assert.That(s.Effects.HasActiveEffects,Is.False);}}
        /// <summary>The final cell is the origin and the celebration is not replayed by ignored taps.</summary>
        [Test]public void ClearOriginOnce()
        {using(var s=new EffectsTestScene(AudioTestScene.FreeBoard(3))){s.Gameplay.TapId(1);s.Gameplay.TapId(2);s.Gameplay.TapId(3);Assert.That(s.Effects.Particles.ActiveCount,Is.EqualTo(70));s.Effects.Tick(.05f);s.View.TryGetCellDot(2,0,out var last);s.View.TryGetCellDot(0,0,out var first);Assert.That(last.transform.localScale.x,Is.GreaterThan(ViewTheme.CellDotSize));Assert.That(first.transform.localScale,Is.EqualTo(Vector3.one*ViewTheme.CellDotSize));s.Gameplay.Game.Undo();Assert.That(s.Effects.Particles.ActiveCount,Is.EqualTo(70));}}
        /// <summary>AdvanceFrame is the Update path: pause freezes it, hidden boards clear even while paused.</summary>
        [Test]public void PauseAndHidden()
        {using(var s=new EffectsTestScene()){s.Gameplay.TapId(1);s.Effects.Streaks.TryGetHeadWorld(1,out var before);s.Gameplay.Game.SetPaused(true);s.Director.AdvanceFrame(.2f);s.Effects.Streaks.TryGetHeadWorld(1,out var frozen);Assert.That(frozen,Is.EqualTo(before));s.Gameplay.Game.SetPaused(false);s.Director.AdvanceFrame(.1f);s.Effects.Streaks.TryGetHeadWorld(1,out var moved);Assert.That(moved,Is.Not.EqualTo(before));s.Gameplay.Game.SetPaused(true);s.View.SetVisible(false);s.Director.AdvanceFrame(0);Assert.That(s.Effects.HasActiveEffects,Is.False);}}
        private static string Script(bool effects)
        {
            using(var s=new EffectsTestScene(LevelRepository.LoadFromResources("TestLevels/legacy_level_001").Board)) {
                if(!effects)Object.DestroyImmediate(s.Host);
                s.Gameplay.TapId(1);s.Gameplay.TapId(4);s.Gameplay.TapId(3);s.Gameplay.Game.Undo();s.Gameplay.Game.ShowHint();s.Gameplay.TapId(3);
                var game=s.Gameplay.Game;string result=game.MistakeCount+"/"+game.MoveCount+"/"+game.UndoCount+"/"+game.CanUndo;
                foreach(var a in game.Session.Board.Arrows){s.View.TryGetArrowView(a.Id,out var v);result+=";"+a.Id+","+a.X+","+a.Y+","+a.Direction+","+v.transform.position;}
                var undo=game.Session.TryUndo();return result+";history="+undo.Outcome+","+undo.Restored.Id;
            }
        }
        /// <summary>Counts, arrow order, positions and history are identical without the effects objects.</summary>
        [Test]public void PureVisuals()=>Assert.That(Script(true),Is.EqualTo(Script(false)));
        /// <summary>Destroying the observing director removes subscriptions.</summary>
        [Test]public void DestroyUnsubscribes(){using(var s=new EffectsTestScene()){Object.DestroyImmediate(s.Director);s.Gameplay.TapId(1);Assert.That(s.Effects.HasActiveEffects,Is.False);}}
        /// <summary>Every sample respects caps and restores every dot and the background after settling.</summary>
        [TestCase(1)][TestCase(2)][TestCase(3)]public void Samples(int index)
        {
            var board=LevelRepository.LoadFromResources("TestLevels/legacy_level_00"+index).Board;
            using(var s=new EffectsTestScene(board)){
                while(!s.Gameplay.Game.IsCleared){s.Gameplay.Tap(HintFinder.Find(board).Arrow);Assert.That(s.Effects.Particles.ActiveCount,Is.LessThanOrEqualTo(96));Assert.That(s.Effects.Streaks.ActiveCount,Is.LessThanOrEqualTo(8));}
                Assert.That(s.Host.transform.Find("Particles").childCount,Is.LessThanOrEqualTo(96));Assert.That(s.Host.transform.Find("Streaks").childCount,Is.LessThanOrEqualTo(8));s.Director.AdvanceFrame(3);Assert.That(s.Effects.HasActiveEffects,Is.False);Assert.That(s.Camera.backgroundColor,Is.EqualTo(ViewTheme.Background));
                for(int y=0;y<board.Height;y++)for(int x=0;x<board.Width;x++){s.View.TryGetCellDot(x,y,out var dot);Assert.That(dot.color,Is.EqualTo(ViewTheme.CellDot));Assert.That(dot.transform.localScale,Is.EqualTo(Vector3.one*ViewTheme.CellDotSize));}
            }
        }
        /// <summary>A missing game warns only once and disables the observer.</summary>
        [Test]public void MissingGame()
        {var host=new GameObject("Missing effects game");try{var d=host.AddComponent<EffectsDirector>();LogAssert.Expect(LogType.Warning,"ArrowEscape effects: no GameController or BoardView found; effects disabled.");d.Initialize(null,null,null);d.Initialize(null,null,null);Assert.That(d.enabled,Is.False);}finally{Object.DestroyImmediate(host);}}
        /// <summary>Test-created bootstraps remain free of new runtime-only components.</summary>
        [Test]public void BootstrapHasNoEffects(){var host=new GameObject("Test bootstrap");try{host.AddComponent<GameBootstrap>();Assert.That(host.GetComponent<EffectsController>(),Is.Null);Assert.That(host.GetComponent<EffectsDirector>(),Is.Null);Assert.That(host.GetComponent<HapticsDirector>(),Is.Null);}finally{Object.DestroyImmediate(host);}}
    }
}
