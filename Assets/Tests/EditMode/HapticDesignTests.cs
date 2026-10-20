using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks exact cue patterns and immutable access.</summary>
    public class HapticDesignTests
    {
        /// <summary>Single pulse cues use their specified duration, strength and throttle.</summary>
        [TestCase(HapticCue.Exit,12,70,.04f)][TestCase(HapticCue.Blocked,40,200,.08f)][TestCase(HapticCue.Undo,8,45,.04f)][TestCase(HapticCue.Preview,15,90,.2f)]
        public void Singles(HapticCue cue,int ms,int amplitude,float interval)
        {var p=HapticDesign.GetPattern(cue);Assert.That(p.Count,Is.EqualTo(1));Assert.That(p[0].DurationMs,Is.EqualTo(ms));Assert.That(p[0].Amplitude,Is.EqualTo(amplitude));Assert.That(p[0].GapAfterMs,Is.Zero);Assert.That(HapticDesign.MinIntervalSeconds(cue),Is.EqualTo(interval));}
        /// <summary>Completion rises across three pulses and all patterns are immutable and valid.</summary>
        [Test] public void CompleteAndValidity()
        {
            var complete=HapticDesign.GetPattern(HapticCue.LevelComplete);Assert.That(complete.Count,Is.EqualTo(3));int[] duration={20,20,45},amplitude={90,130,220},gaps={60,60,0};
            for(int i=0;i<3;i++){Assert.That(complete[i].DurationMs,Is.EqualTo(duration[i]));Assert.That(complete[i].Amplitude,Is.EqualTo(amplitude[i]));Assert.That(complete[i].GapAfterMs,Is.EqualTo(gaps[i]));}
            Assert.That(HapticDesign.MinIntervalSeconds(HapticCue.LevelComplete),Is.EqualTo(.5f));
            for(int i=0;i<5;i++){var p=HapticDesign.GetPattern((HapticCue)i);Assert.That(p.Count,Is.GreaterThan(0));foreach(var pulse in p){Assert.That(pulse.DurationMs,Is.GreaterThan(0));Assert.That(pulse.Amplitude,Is.InRange(1,255));Assert.That(pulse.GapAfterMs,Is.GreaterThanOrEqualTo(0));}Assert.Throws<NotSupportedException>(()=>((IList<HapticPulse>)p)[0]=new HapticPulse(1,1));}
            Assert.That(HapticDesign.GetPattern(HapticCue.Blocked)[0].Amplitude,Is.GreaterThan(HapticDesign.GetPattern(HapticCue.Exit)[0].Amplitude));Assert.That(HapticDesign.GetPattern(HapticCue.Exit)[0].Amplitude,Is.GreaterThan(HapticDesign.GetPattern(HapticCue.Undo)[0].Amplitude));
            Assert.Throws<ArgumentOutOfRangeException>(()=>HapticDesign.GetPattern((HapticCue)90));Assert.Throws<ArgumentOutOfRangeException>(()=>HapticDesign.MinIntervalSeconds((HapticCue)90));
        }
        /// <summary>Pulse invariants reject invalid values.</summary>
        [Test] public void InvalidPulse(){Assert.Throws<ArgumentOutOfRangeException>(()=>new HapticPulse(0,1));Assert.Throws<ArgumentOutOfRangeException>(()=>new HapticPulse(1,0));Assert.Throws<ArgumentOutOfRangeException>(()=>new HapticPulse(1,256));Assert.Throws<ArgumentOutOfRangeException>(()=>new HapticPulse(1,1,-1));}
    }
}
