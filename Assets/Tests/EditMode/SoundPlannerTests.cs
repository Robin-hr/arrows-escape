using ArrowEscape.Core;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Verifies that only post-move progress chooses the melody note.</summary>
    public class SoundPlannerTests
    {
        /// <summary>Exit progression climbs, turns, repeats, and clamps.</summary>
        [TestCase(1,0)][TestCase(11,10)][TestCase(12,9)][TestCase(21,0)][TestCase(0,0)][TestCase(-4,0)]
        public void ExitSteps(int count, int step)
        { var e = SoundPlanner.ForExit(count); Assert.That(e.Cue, Is.EqualTo(SoundCue.Exit)); Assert.That(e.ScaleStep, Is.EqualTo(step)); }
        /// <summary>Large progress never escapes the note bank.</summary>
        [Test] public void LargeExit() => Assert.That(SoundPlanner.ForExit(100000).ScaleStep, Is.InRange(0,10));
        /// <summary>Undo replays exactly the exit it reversed, more softly.</summary>
        [Test] public void RoundTrip()
        {
            for (int k = 0; k <= 40; k++) Assert.That(SoundPlanner.ForUndo(k).ScaleStep, Is.EqualTo(SoundPlanner.ForExit(k + 1).ScaleStep));
            Assert.That(SoundPlanner.ForUndo(-1).ScaleStep, Is.Zero);
            Assert.That(SoundPlanner.ForUndo(1).Volume, Is.LessThan(SoundPlanner.ForExit(2).Volume));
            Assert.That(SoundPlanner.ForUndo(1).Cue, Is.EqualTo(SoundCue.Undo));
        }
        /// <summary>Every cue has its documented step and gain.</summary>
        [Test] public void CueTable()
        {
            SoundEvent[] events = { SoundPlanner.ForExit(1), SoundPlanner.ForBlocked(), SoundPlanner.ForUndo(0), SoundPlanner.ForHint(), SoundPlanner.ForLevelComplete(), SoundPlanner.ForPreview() };
            SoundCue[] cues = { SoundCue.Exit, SoundCue.Blocked, SoundCue.Undo, SoundCue.Hint, SoundCue.LevelComplete, SoundCue.Preview };
            int[] steps = { 0,-1,0,8,-1,7 }; float[] volumes = { .6f,.55f,.4f,.4f,.7f,.5f };
            for (int i = 0; i < events.Length; i++)
            {
                Assert.That(events[i].Cue, Is.EqualTo(cues[i])); Assert.That(events[i].ScaleStep, Is.EqualTo(steps[i]));
                Assert.That(events[i].Volume, Is.EqualTo(volumes[i])); Assert.That(events[i].Volume, Is.GreaterThan(0).And.LessThanOrEqualTo(1));
            }
            Assert.That(SoundPlanner.ExitVolume, Is.LessThanOrEqualTo(.7f));
        }
        /// <summary>Value equality is deterministic and includes every field.</summary>
        [Test] public void Equality()
        {
            var a = SoundPlanner.ForExit(9); var b = SoundPlanner.ForExit(9);
            Assert.That(a == b, Is.True); Assert.That(a.Equals((object)b), Is.True); Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a != SoundPlanner.ForExit(8), Is.True); Assert.That(a.Equals(null), Is.False);
            Assert.That(new SoundEvent(SoundCue.Exit, 8, -1).Volume, Is.Zero);
            Assert.That(new SoundEvent(SoundCue.Exit, 8, 4).Volume, Is.EqualTo(1));
        }
    }
}
