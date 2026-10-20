using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks independent accepted-time windows and clock resets.</summary>
    public class HapticThrottleTests
    {
        /// <summary>Rejected calls do not extend the window and the boundary is accepted.</summary>
        [Test]public void Window(){var t=new HapticThrottle();Assert.That(t.TryAccept(HapticCue.Exit,0),Is.True);Assert.That(t.TryAccept(HapticCue.Exit,.02f),Is.False);Assert.That(t.TryAccept(HapticCue.Exit,.04f),Is.True);Assert.That(t.TryAccept(HapticCue.Exit,.09f),Is.True);}
        /// <summary>Each cue has its own clock; backwards time re-anchors it.</summary>
        [Test]public void IndependentAndReset(){var t=new HapticThrottle();Assert.That(t.TryAccept(HapticCue.Exit,10),Is.True);Assert.That(t.TryAccept(HapticCue.Blocked,10),Is.True);Assert.That(t.TryAccept(HapticCue.Exit,1),Is.True);Assert.That(t.TryAccept(HapticCue.Exit,1.01f),Is.False);t.Reset();Assert.That(t.TryAccept(HapticCue.Exit,1.01f),Is.True);}
    }
}
