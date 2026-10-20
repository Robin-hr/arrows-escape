using System;
using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Tests the fixed voice pool's idle search and stealing order.</summary>
    public class VoiceAllocatorTests
    {
        /// <summary>Idle selection advances and wraps.</summary>
        [TestCase(-1,0)][TestCase(0,1)][TestCase(5,0)][TestCase(50,0)][TestCase(-2,0)]
        public void Idle(int last, int expected) => Assert.That(VoiceAllocator.Pick(new bool[6], last), Is.EqualTo(expected));
        /// <summary>The first idle voice after the previous one wins.</summary>
        [Test] public void SkipBusy() => Assert.That(VoiceAllocator.Pick(new[] { true,true,false,true,false,false }, 0), Is.EqualTo(2));
        /// <summary>Busy pools steal the next voice in round-robin order.</summary>
        [TestCase(-1,0)][TestCase(0,1)][TestCase(5,0)]
        public void Steal(int last, int expected) => Assert.That(VoiceAllocator.Pick(new[] { true,true,true,true,true,true }, last), Is.EqualTo(expected));
        /// <summary>Invalid pools report the documented errors.</summary>
        [Test] public void InvalidPool()
        {
            Assert.Throws<ArgumentNullException>(() => VoiceAllocator.Pick(null, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => VoiceAllocator.Pick(Array.Empty<bool>(), -1));
        }
    }
}
