using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks pure effect curves against their endpoints and existing slide math.</summary>
    public class EffectMathTests
    {
        /// <summary>Progress, distance, fade and size have the expected endpoints and monotonicity.</summary>
        [Test] public void Curves()
        {
            Assert.That(EffectMath.Progress(-1,1),Is.Zero);Assert.That(EffectMath.Progress(2,1),Is.EqualTo(1));Assert.That(EffectMath.Progress(0,0),Is.EqualTo(1));
            Assert.That(EffectMath.ParticleDistance(5,0),Is.Zero);Assert.That(EffectMath.ParticleDistance(5,1),Is.EqualTo(5));
            Assert.That(EffectMath.ParticleAlpha(0),Is.EqualTo(1));Assert.That(EffectMath.ParticleAlpha(1),Is.Zero);Assert.That(EffectMath.ParticleSize(.2f,.1f,.5f),Is.EqualTo(.15f).Within(.000001));
            float distance=0,alpha=1;for(int i=0;i<=100;i++){float t=i/100f;float d=EffectMath.ParticleDistance(5,t),a=EffectMath.ParticleAlpha(t);Assert.That(d,Is.GreaterThanOrEqualTo(distance));Assert.That(a,Is.LessThanOrEqualTo(alpha));distance=d;alpha=a;}
        }
        /// <summary>Streak geometry uses exactly the same curves as slides.</summary>
        [TestCase(Direction.Up)][TestCase(Direction.Down)][TestCase(Direction.Left)][TestCase(Direction.Right)]
        public void Streak(Direction d)
        {
            foreach(float t in new[]{0f,.5f,1f})Assert.That(EffectMath.StreakHeadDistance(3,7,1,3,d,t),Is.EqualTo(AnimationMath.SlideDistance(3,7,1,3,d)*AnimationMath.EaseInQuad(t)));
            Assert.That(EffectMath.StreakTailDistance(1,2.5f),Is.Zero);Assert.That(EffectMath.StreakTailDistance(5,2.5f),Is.EqualTo(2.5f));Assert.Throws<ArgumentOutOfRangeException>(()=>EffectMath.StreakTailDistance(1,-1));
            Assert.That(EffectMath.StreakAlpha(.4f,.35f,.5f),Is.EqualTo(.35f));Assert.That(EffectMath.StreakAlpha(1,.35f,.5f),Is.Zero);
        }
        /// <summary>Wave propagation is radial and the total includes the farthest corner.</summary>
        [Test] public void Wave()
        {
            Assert.That(EffectMath.DotWaveDelay(2,2,2,2,.07f),Is.Zero);Assert.That(EffectMath.DotWaveDelay(0,0,2,2,.07f),Is.EqualTo(EffectMath.DotWaveDelay(4,4,2,2,.07f)));
            Assert.That(EffectMath.DotWaveDelay(5,0,0,0,.07f),Is.GreaterThan(EffectMath.DotWaveDelay(1,0,0,0,.07f)));
            Assert.That(EffectMath.DotWaveLocalTime(.1f,.2f,.5f),Is.Zero);Assert.That(EffectMath.DotWaveLocalTime(1,.2f,.5f),Is.EqualTo(1));
            float last=0;for(int i=0;i<=100;i++){float v=EffectMath.DotWaveLocalTime(i/100f,.2f,.5f);Assert.That(v,Is.GreaterThanOrEqualTo(last));last=v;}
            Assert.That(EffectMath.DotWaveTotalSeconds(4,5,0,0,.07f,.5f),Is.EqualTo(.85f).Within(.000001));
        }
        /// <summary>The pulse occurs once, has a peak of one, and ends at exact zero.</summary>
        [Test] public void Pulse()
        {Assert.That(EffectMath.PulseAmount(0),Is.Zero);Assert.That(EffectMath.PulseAmount(1),Is.Zero);Assert.That(EffectMath.PulseAmount(.4f),Is.EqualTo(1));for(int i=0;i<=100;i++)Assert.That(EffectMath.PulseAmount(i/100f),Is.InRange(0,1));}
    }
}
