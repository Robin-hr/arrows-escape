using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks propagation and exact restoration, including interruption and destroyed dots.</summary>
    public class DotWavePlayerTests
    {
        /// <summary>The near dot pulses first; far dots wait and all captured values restore exactly.</summary>
        [Test]public void PropagationAndRestore()
        {
            using(var s=new EffectsTestScene(new Board(6,6))) {
                s.View.TryGetCellDot(0,0,out var near);s.View.TryGetCellDot(5,5,out var far);Color color=new Color(.1f,.2f,.3f,.4f);Vector3 scale=new Vector3(.08f,.09f,1);near.color=color;near.transform.localScale=scale;
                var wave=new DotWavePlayer();Assert.That(wave.Start(s.View,6,6,0,0),Is.True);wave.Tick(0);Assert.That(near.color,Is.EqualTo(color));Color farColor=far.color;Vector3 farScale=far.transform.localScale;
                wave.Tick(.2f);Assert.That(near.transform.localScale.x,Is.GreaterThan(scale.x));Assert.That(far.color,Is.EqualTo(farColor));Assert.That(far.transform.localScale,Is.EqualTo(farScale));
                wave.Tick(.4f);Assert.That(far.transform.localScale.x,Is.GreaterThan(farScale.x));wave.Tick(2);Assert.That(wave.IsActive,Is.False);Assert.That(near.color,Is.EqualTo(color));Assert.That(near.transform.localScale,Is.EqualTo(scale));
                for(int y=0;y<6;y++)for(int x=0;x<6;x++){s.View.TryGetCellDot(x,y,out var dot);Assert.That(dot.color,Is.EqualTo(x==0&&y==0?color:ViewTheme.CellDot));Assert.That(dot.transform.localScale,Is.EqualTo(x==0&&y==0?scale:Vector3.one*ViewTheme.CellDotSize));}
            }
        }
        /// <summary>Restarting twenty times and clearing mid-wave never accumulates drift.</summary>
        [Test]public void RestartAndClear()
        {using(var s=new EffectsTestScene(new Board(4,4))){s.View.TryGetCellDot(1,1,out var dot);Color c=dot.color;Vector3 scale=dot.transform.localScale;var wave=new DotWavePlayer();for(int i=0;i<20;i++){wave.Start(s.View,4,4,1,1);wave.Tick(.2f);wave.Start(s.View,4,4,1,1);wave.Tick(.1f);wave.ClearAll();Assert.That(dot.color,Is.EqualTo(c));Assert.That(dot.transform.localScale,Is.EqualTo(scale));}wave.Start(s.View,4,4,0,0);s.View.Show(new Board(4,4));Assert.DoesNotThrow(()=>wave.Tick(.2f));Assert.DoesNotThrow(wave.ClearAll);}}
        /// <summary>Null boards and boards above the dot cap do not start waves.</summary>
        [Test]public void MissingDots(){var wave=new DotWavePlayer();Assert.That(wave.Start(null,4,4,0,0),Is.False);using(var s=new EffectsTestScene(new Board(33,33)))Assert.That(wave.Start(s.View,33,33,0,0),Is.False);Assert.That(wave.IsActive,Is.False);}
    }
}
