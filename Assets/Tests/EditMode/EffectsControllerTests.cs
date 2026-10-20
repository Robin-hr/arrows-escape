using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks composed effects, restoration and null-safe unbound behavior.</summary>
    public class EffectsControllerTests
    {
        /// <summary>Exit and completion start the expected timelines; clear restores borrowed values.</summary>
        [Test]public void ExitAndComplete()
        {using(var s=new EffectsTestScene(new Board(6,6))){s.Effects.PlayExit(new Arrow(7,2,2,Direction.Up),6,6,1);Assert.That(s.Effects.Streaks.ActiveCount,Is.EqualTo(1));Assert.That(s.Effects.Particles.ActiveCount,Is.EqualTo(10));Assert.That(s.Effects.HasActiveEffects,Is.True);s.Effects.Tick(2);Assert.That(s.Effects.HasActiveEffects,Is.False);s.Effects.PlayComplete(6,6,2,2);Assert.That(s.Effects.DotWave.IsActive&&s.Effects.CameraPulse.IsActive,Is.True);Assert.That(s.Effects.Particles.ActiveCount,Is.EqualTo(40));s.Effects.Tick(.2f);s.Effects.ClearAll();Assert.That(s.Effects.HasActiveEffects,Is.False);Assert.That(s.Camera.backgroundColor,Is.EqualTo(ViewTheme.Background));s.View.TryGetCellDot(2,2,out var dot);Assert.That(dot.color,Is.EqualTo(ViewTheme.CellDot));Assert.That(dot.transform.localScale,Is.EqualTo(Vector3.one*ViewTheme.CellDotSize));}}
        /// <summary>Large boards still celebrate, skipping only the cell-dot wave.</summary>
        [Test]public void LargeBoard(){using(var s=new EffectsTestScene(new Board(40,40))){s.Effects.PlayComplete(40,40,0,0);Assert.That(s.Effects.DotWave.IsActive,Is.False);Assert.That(s.Effects.CameraPulse.IsActive,Is.True);Assert.That(s.Effects.Particles.ActiveCount,Is.EqualTo(40));s.View.Show(new Board(4,4));Assert.That(s.Effects.HasActiveEffects,Is.False);Assert.That(s.Host.transform.Find("Particles"),Is.Not.Null);}}
        /// <summary>No binding means no effects, no errors and no state changes.</summary>
        [Test]public void Unbound(){var host=new GameObject("Unbound");try{var e=host.AddComponent<EffectsController>();Assert.DoesNotThrow(()=>e.PlayExit(new Arrow(1,0,0,Direction.Up),4,4,1));Assert.DoesNotThrow(()=>e.PlayComplete(4,4,0,0));Assert.DoesNotThrow(()=>e.Tick(1));Assert.DoesNotThrow(e.ClearAll);Assert.DoesNotThrow(e.ClearStreaks);Assert.That(e.HasActiveEffects,Is.False);}finally{Object.DestroyImmediate(host);}}
        /// <summary>Rebinding uses the new coordinate origin without creating additional pool containers.</summary>
        [Test]public void Rebind()
        {using(var s=new EffectsTestScene()){var host=new GameObject("Other board");try{var view=host.AddComponent<BoardView>();view.Show(new Board(8,3));s.Effects.Bind(view);s.Effects.PlayExit(new Arrow(99,7,2,Direction.Up),8,3,1);s.Effects.Streaks.TryGetHeadWorld(99,out var head);Assert.That(head,Is.EqualTo((Vector2)view.CellToWorld(7,2)));Assert.That(s.Host.transform.childCount,Is.EqualTo(2));}finally{Object.DestroyImmediate(host);}}}
        /// <summary>Warmed active Tick paths allocate no managed memory, including dot and camera pulses.</summary>
        [Test]public void TickAllocatesNothing()
        {using(var s=new EffectsTestScene(new Board(6,6))){s.Effects.PlayExit(new Arrow(1,2,2,Direction.Up),6,6,1);s.Effects.PlayComplete(6,6,2,2);s.Effects.Tick(.001f);long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<500;i++)s.Effects.Tick(.0001f);long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(allocated,Is.Zero,"Active effect Tick paths must reuse their buffers.");}}
    }
}
