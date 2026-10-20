using System;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Pure economy arithmetic.</summary>
    public sealed class CoinRewardTests
    {
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [TestCase(1,20)] [TestCase(2,30)] [TestCase(3,40)]
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void FirstClear(int stars,int expected)=>Assert.That(CoinReward.Compute(false,0,stars),Is.EqualTo(expected));
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [TestCase(1,1,5)] [TestCase(2,2,5)] [TestCase(3,3,5)] [TestCase(1,3,25)] [TestCase(3,1,5)]
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void Replay(int previous,int stars,int expected)=>Assert.That(CoinReward.Compute(true,previous,stars),Is.EqualTo(expected));
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void Bounds(){Assert.Throws<ArgumentOutOfRangeException>(()=>CoinReward.Compute(false,-1,1));Assert.Throws<ArgumentOutOfRangeException>(()=>CoinReward.Compute(false,0,4));for(int a=0;a<=3;a++)for(int b=0;b<=3;b++)Assert.That(CoinReward.Compute(true,a,b),Is.GreaterThanOrEqualTo(0));}
    }
}
