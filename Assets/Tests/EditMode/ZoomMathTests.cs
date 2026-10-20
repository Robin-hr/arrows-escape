using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Pure zoom limits, pivot preservation and screen-distance cancellation.</summary>
    public class ZoomMathTests
    {
        [Test] public void RangeAndFallback(){Assert.That(ZoomMath.ClampSize(100,10,1000,160),Is.EqualTo(10));Assert.That(ZoomMath.ClampSize(0,10,1000,0),Is.EqualTo(1000/(2*.45f*160)).Within(.0001));}
        [Test] public void PivotInvariant(){float c=2,p=5,old=10,next=4;float result=ZoomMath.PivotCenter(c,p,old,next);Assert.That((p-result)/next,Is.EqualTo((p-c)/old).Within(.00001));}
        [Test] public void PanClamp(){Assert.That(ZoomMath.ClampCenter(100,12,4),Is.EqualTo(8));Assert.That(ZoomMath.ClampCenter(-100,12,4),Is.EqualTo(-8));}
        [Test] public void DragThreshold(){Assert.That(ZoomMath.CancelsTap(12,0),Is.False);Assert.That(ZoomMath.CancelsTap(12.01f,0),Is.True);}
    }
}
