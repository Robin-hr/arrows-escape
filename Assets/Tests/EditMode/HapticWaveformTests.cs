using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks Android waveform layout without any JNI calls.</summary>
    public class HapticWaveformTests
    {
        /// <summary>Buffers contain two entries per pulse.</summary>
        [TestCase(0,0)][TestCase(1,2)][TestCase(3,6)]public void Length(int count,int expected)=>Assert.That(HapticWaveform.RequiredLength(count),Is.EqualTo(expected));
        /// <summary>Completion timing alternates on-times and internal silent gaps.</summary>
        [Test]public void Complete()
        {var t=new long[6];var a=new int[6];Assert.That(HapticWaveform.Build(HapticDesign.GetPattern(HapticCue.LevelComplete),t,a),Is.EqualTo(6));Assert.That(t,Is.EqualTo(new long[]{0,20,60,20,60,45}));Assert.That(a,Is.EqualTo(new[]{0,90,0,130,0,220}));Assert.That(HapticWaveform.TotalMilliseconds(HapticDesign.GetPattern(HapticCue.LevelComplete)),Is.EqualTo(205));}
        /// <summary>The final gap is excluded and empty input writes nothing.</summary>
        [Test]public void SingleAndEmpty()
        {var p=new[]{new HapticPulse(12,70,99)};var t=new long[2];var a=new int[2];HapticWaveform.Build(p,t,a);Assert.That(t,Is.EqualTo(new long[]{0,12}));Assert.That(a,Is.EqualTo(new[]{0,70}));Assert.That(HapticWaveform.TotalMilliseconds(p),Is.EqualTo(12));Assert.That(HapticWaveform.Build(Array.Empty<HapticPulse>(),Array.Empty<long>(),Array.Empty<int>()),Is.Zero);}
        /// <summary>Invalid buffers and null inputs report useful errors.</summary>
        [Test]public void Invalid()
        {var p=HapticDesign.GetPattern(HapticCue.Exit);Assert.Throws<ArgumentNullException>(()=>HapticWaveform.Build(null,new long[2],new int[2]));Assert.Throws<ArgumentNullException>(()=>HapticWaveform.Build(p,null,new int[2]));Assert.Throws<ArgumentNullException>(()=>HapticWaveform.Build(p,new long[2],null));Assert.Throws<ArgumentException>(()=>HapticWaveform.Build(p,new long[1],new int[2]));Assert.Throws<ArgumentException>(()=>HapticWaveform.Build(p,new long[2],new int[1]));Assert.Throws<ArgumentNullException>(()=>HapticWaveform.TotalMilliseconds(null));}
    }
}
