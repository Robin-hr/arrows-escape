using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Real tap events, logical replacement, and bounded stuck detection.</summary>
    public class GameControllerIceTests
    {
        private AudioTestScene scene;
        /// <summary>Checks cleanup with isolated ice-arrow state.</summary>
        [TearDown] public void Cleanup()=>scene?.Dispose();
        private void Tap(int id)
        {
            var arrow=scene.Game.Session.Board.GetArrowById(id);Vector2 p=scene.Host.GetComponent<BoardView>().CellToWorld(arrow.X,arrow.Y);
            var taps=scene.Host.GetComponent<TapController>();taps.PointerDown(1,p);taps.PointerUp(1,p);
        }
        /// <summary>Checks slide events and undo with isolated ice-arrow state.</summary>
        [Test] public void SlideEventsAndUndo()
        {
            scene=new AudioTestScene(IceTestBoards.Cycle());var game=scene.Game;var board=game.Session.Board;var views=scene.Host.GetComponent<BoardView>();
            var guide=scene.Host.AddComponent<GuideController>();game.BindGuide(guide);game.ShowHint();guide.ShowBlockerLine(board.GetArrowById(2),board.GetArrowById(3));
            int slides=0,reverts=0,moves=0,mistakes=0,undos=0;Arrow before=board.GetArrowById(1);
            game.ArrowSlid+=(a,b)=>{slides++;Assert.That(a,Is.SameAs(before));Assert.That(b,Is.SameAs(board.GetArrowById(1)));};
            game.SlideReverted+=a=>{reverts++;Assert.That(a.X,Is.Zero);};
            game.MoveCountChanged+=()=>moves++;game.MistakeCountChanged+=n=>mistakes++;game.UndoStateChanged+=()=>undos++;
            views.TryGetArrowView(1,out var original);Vector3 current=original.transform.position;Tap(1);
            Assert.That(slides,Is.EqualTo(1));Assert.That(moves+mistakes,Is.Zero);Assert.That(undos,Is.EqualTo(1));Assert.That(game.MoveCount,Is.Zero);
            Assert.That(original.transform.position,Is.EqualTo(current));Assert.That(original.Arrow.X,Is.EqualTo(2));
            Assert.That(guide.HasActiveHint||guide.HasActiveLine,Is.False);scene.Host.GetComponent<ArrowAnimator>().Tick(1);
            Assert.That(original.CurrentWorldPosition,Is.EqualTo((Vector2)views.CellToWorld(2,0)));
            game.ShowHint();game.Undo();Assert.That(reverts,Is.EqualTo(1));Assert.That(guide.HasActiveHint,Is.False);scene.Host.GetComponent<ArrowAnimator>().Tick(1);
            Assert.That(original.CurrentWorldPosition,Is.EqualTo((Vector2)views.CellToWorld(0,0)));
            Assert.That(scene.Host.GetComponentsInChildren<ArrowView>().Length,Is.EqualTo(4));views.TryGetBoard(out var displayed);Assert.That(displayed,Is.SameAs(board));
        }
        /// <summary>Checks rapid tap and pause with isolated ice-arrow state.</summary>
        [Test] public void RapidTapAndPause()
        {
            scene=new AudioTestScene(IceTestBoards.Cycle());int blocked=0;scene.Game.MoveBlocked+=(a,b)=>blocked++;
            scene.Game.SetPaused(true);Tap(1);Assert.That(scene.Game.Session.Board.GetArrowById(1).X,Is.Zero);
            scene.Game.SetPaused(false);Tap(1);Tap(1);Assert.That(scene.Game.MistakeCount,Is.EqualTo(1));Assert.That(blocked,Is.EqualTo(1));
        }
        /// <summary>Checks cycle clear with isolated ice-arrow state.</summary>
        [Test] public void CycleClear()
        {
            scene=new AudioTestScene(IceTestBoards.Cycle());int clears=0;scene.Game.BoardCleared+=()=>clears++;
            foreach(int id in new[]{1,4,3,2,1})scene.TapId(id);
            Assert.That(clears,Is.EqualTo(1));Assert.That(scene.Game.MoveCount,Is.EqualTo(4));Assert.That(scene.Game.MistakeCount,Is.Zero);
        }
        /// <summary>Checks stuck and recovery with isolated ice-arrow state.</summary>
        [Test] public void StuckAndRecovery()
        {
            scene=new AudioTestScene(IceTestBoards.Trap());int becameStuck=0,becameFree=0;
            scene.Game.StuckChanged+=v=>{if(v)becameStuck++;else becameFree++;};
            scene.TapId(2);Assert.That(scene.Game.IsStuck,Is.False);scene.TapId(1);
            Assert.That(scene.Game.IsStuck,Is.True);Assert.That(becameStuck,Is.EqualTo(1));Assert.That(scene.Game.CanHint,Is.False);Assert.That(scene.Game.ShowHint(),Is.False);
            scene.Game.Undo();Assert.That(scene.Game.IsStuck,Is.False);Assert.That(becameFree,Is.EqualTo(1));scene.TapId(1);Assert.That(scene.Game.IsStuck,Is.True);
            scene.Game.RestartLevel();Assert.That(scene.Game.IsStuck,Is.False);Assert.That(scene.Game.StuckCheckCount,Is.EqualTo(1));
            scene.Game.StartLevel(AudioTestScene.FreeBoard(3));scene.TapId(1);scene.Game.Undo();Assert.That(scene.Game.StuckCheckCount,Is.Zero);
        }
    }
}
