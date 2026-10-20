using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks repeatable local generators, their bounds and distribution.</summary>
    public class DeterministicRandomTests
    {
        /// <summary>Equal seeds repeat while different seeds diverge.</summary>
        [TestCase(0)][TestCase(1)][TestCase(-47)] public void Seeds(int seed)
        {
            var a = new DeterministicRandom(seed); var b = new DeterministicRandom(seed); var c = new DeterministicRandom(seed + 1); bool different = false;
            for (int i = 0; i < 1000; i++) { float x = a.NextFloat(); Assert.That(x,Is.EqualTo(b.NextFloat())); different |= x != c.NextFloat(); Assert.That(x,Is.GreaterThanOrEqualTo(0).And.LessThan(1)); }
            Assert.That(different,Is.True);
        }
        /// <summary>The range and mean are healthy over ten thousand values.</summary>
        [Test] public void RangeAndMean()
        {
            var r = new DeterministicRandom(713); double sum = 0;
            for (int i = 0; i < 10000; i++) { sum += r.NextFloat(); Assert.That(r.Range(-3,7),Is.InRange(-3,7)); }
            Assert.That(sum / 10000,Is.InRange(.45,.55));
            var zero = default(DeterministicRandom); Assert.That(zero.NextFloat(),Is.GreaterThan(0));
        }
    }
}
