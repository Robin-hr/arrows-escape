using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Pure duration and residual-offset contracts.</summary>
    public class AnimationMathIceTests
    {
        /// <summary>Checks durations with isolated ice-arrow state.</summary>
        [Test] public void Durations()
        {
            float previous=.1f;
            for(int cells=-2;cells<40;cells++)
            {
                float value=AnimationMath.IceSlideDuration(cells,.1f,.05f,.4f);
                Assert.That(value,Is.InRange(.1f,.4f));Assert.That(value,Is.GreaterThanOrEqualTo(previous));previous=value;
            }
            Assert.That(AnimationMath.IceSlideDuration(2,.1f,.05f,.4f),Is.EqualTo(.2f));
        }
        [TestCase(-1,0,1)][TestCase(0,-1,1)][TestCase(1,0,.5f)][TestCase(0,0,-1)]
        public void Invalid(float start,float per,float max)=>Assert.Throws<ArgumentOutOfRangeException>(()=>AnimationMath.IceSlideDuration(1,start,per,max));
        /// <summary>Checks offset with isolated ice-arrow state.</summary>
        [Test] public void Offset()
        {
            Assert.That(AnimationMath.SlideOffsetFactor(0),Is.EqualTo(1));Assert.That(AnimationMath.SlideOffsetFactor(1),Is.Zero);
            float previous=1;
            for(int i=-5;i<=105;i++){float value=AnimationMath.SlideOffsetFactor(i/100f);Assert.That(value,Is.InRange(0,previous));previous=value;}
        }
    }
}
