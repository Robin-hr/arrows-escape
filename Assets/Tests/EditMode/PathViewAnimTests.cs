using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Real path rendering, instant gameplay, animation and mesh-wave integration.</summary>
    public class PathViewAnimTests
    {
        [Test] public void ExitAndReverseUndo()
        {
            using(var s=new AudioTestScene(PathTestBoards.Load()))
            {
                var board=s.Host.GetComponent<BoardView>();var animator=s.Host.GetComponent<ArrowAnimator>();board.TryGetArrowView(2,out var old);
                s.TapId(2);Assert.That(s.Game.Session.Board.GetArrowById(2),Is.Null);animator.Tick(1);Assert.That(old==null,Is.True);
                s.Game.Undo();board.TryGetArrowView(2,out var restored);Assert.That(restored.Path.Distance,Is.GreaterThan(0));animator.Tick(1);Assert.That(restored.Path.Distance,Is.Zero);Assert.That(restored.transform.position,Is.EqualTo(board.CellToWorld(4,0)));
            }
        }
        [Test] public void BlockAndLayersRestore()
        {
            using(var s=new AudioTestScene(PathTestBoards.Load()))
            {
                var board=s.Host.GetComponent<BoardView>();board.TryGetArrowView(1,out var view);Vector3 pos=view.transform.position;Color color=view.SpriteRenderer.color;
                s.TapId(1);s.Host.GetComponent<ArrowAnimator>().Tick(1);Assert.That(view.Path.Distance,Is.Zero);
                for(int i=0;i<100;i++){view.SetHint(Color.green,1.18f);view.SetHighlight(true);view.SetFlash(Color.red,1.15f);view.ClearFlash();view.SetHighlight(false);view.ClearHint();}
                Assert.That(view.transform.position,Is.EqualTo(pos));Assert.That(view.SpriteRenderer.color,Is.EqualTo(color));Assert.That(view.transform.localScale,Is.EqualTo(Vector3.one));
                Assert.That(view.GetComponent<MeshRenderer>().sharedMaterial,Is.SameAs(view.SpriteRenderer.sharedMaterial));
            }
        }
        [Test] public void BodyTapAndMidpoint()
        {
            using(var s=new AudioTestScene(PathTestBoards.Load()))
            {
                var board=s.Host.GetComponent<BoardView>();var taps=s.Host.GetComponent<TapController>();int id=-1;taps.ArrowTapped+=a=>id=a.Id;
                Vector2 p=board.CellToWorld(1,0);taps.PointerDown(1,p);taps.PointerUp(1,p);Assert.That(id,Is.EqualTo(1));
                p=board.CellToWorld(.5f,1);taps.PointerDown(1,p);taps.PointerUp(1,p);Assert.That(id,Is.EqualTo(3));
            }
        }
        [Test] public void PickingUnderZoomAndReset()
        {
            var cameraObject=new GameObject("Path pick camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
            try
            {
                using(var s=new AudioTestScene(PathTestBoards.Load()))
                {
                    var view=s.Host.GetComponent<BoardView>();var taps=s.Host.GetComponent<TapController>();int tapped=-1;taps.ArrowTapped+=a=>tapped=a.Id;
                    foreach(float size in new[]{2f,4f,8f})
                    {
                        camera.orthographicSize=size;camera.transform.position=new Vector3(.25f,-.5f,-10);
                        Vector3 screen=camera.WorldToScreenPoint(view.CellToWorld(1,0));Vector3 world=camera.ScreenToWorldPoint(screen);
                        taps.PointerDown(1,world);taps.PointerUp(1,world);Assert.That(tapped,Is.EqualTo(1));
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(cameraObject);}
        }
        [Test] public void GuideEndsAtBodyAndGridModeSurvives()
        {
            using(var s=new AudioTestScene(PathTestBoards.Load()))
            {
                var board=s.Host.GetComponent<BoardView>();var guide=s.Host.AddComponent<GuideController>();s.Game.BindGuide(guide);s.TapId(3);
                Assert.That(guide.LineEndWorld,Is.EqualTo((Vector2)board.CellToWorld(0,.35f)));
                while(!s.Game.Session.Board.IsCleared()){var hint=HintFinder.Find(s.Game.Session.Board);s.Tap(hint.Arrow);}
                Assert.That(board.UsesPaths,Is.True);Assert.That(board.PathDots,Is.Not.Null);s.Game.RestartLevel();Assert.That(board.UsesPaths,Is.True);Assert.That(board.PathDots,Is.Not.Null);
            }
        }
        [Test] public void DotWaveRestoresAllAndReusesBuffers()
        {
            using(var s=new AudioTestScene(PathTestBoards.Load()))
            {
                var grid=s.Host.GetComponent<BoardView>().PathDots;var original=(Vector3[])grid.Vertices.Clone();var vertices=grid.Vertices;grid.StartWave(0,0);grid.Tick(.1f);
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;i++)grid.Tick(.01f);long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
                Assert.That(allocated,Is.Zero);Assert.That(grid.Vertices,Is.SameAs(vertices));grid.Tick(10);Assert.That(grid.IsActive,Is.False);Assert.That(grid.Vertices,Is.EqualTo(original));foreach(Color c in grid.Colors)Assert.That(c,Is.EqualTo(ViewTheme.CellDot));
                grid.StartWave(0,0);s.Game.SetPaused(true);var paused=(Vector3[])grid.Vertices.Clone();grid.AdvanceFrame(.2f);Assert.That(grid.Vertices,Is.EqualTo(paused));grid.Tick(.2f);Assert.That(grid.Vertices,Is.Not.EqualTo(paused));
                s.Host.GetComponent<BoardView>().SetVisible(false);Assert.That(grid.IsActive,Is.False);
            }
        }
        [Test] public void EveryDotReceivesWave()
        {
            using(var s=new AudioTestScene(PathTestBoards.Load()))
            {
                var grid=s.Host.GetComponent<BoardView>().PathDots;var rest=(Vector3[])grid.Vertices.Clone();bool[] changed=new bool[20];grid.StartWave(0,0);
                for(int step=0;step<200;step++){grid.Tick(.01f);for(int i=0;i<20;i++)if(grid.Vertices[i*13+1]!=rest[i*13+1])changed[i]=true;}
                Assert.That(changed,Is.All.True);Assert.That(grid.Vertices,Is.EqualTo(rest));
            }
        }
    }
}
