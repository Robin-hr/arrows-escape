using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Slide feedback is bounded, independently throttled and purely observational.</summary>
    public class IceHapticsEffectsTests
    {
        /// <summary>Checks pattern and throttle with isolated ice-arrow state.</summary>
        [Test]public void PatternAndThrottle()
        {
            var pattern=HapticDesign.GetPattern(HapticCue.Slide);Assert.That(pattern.Count,Is.EqualTo(1));Assert.That(pattern[0].DurationMs,Is.EqualTo(10));Assert.That(pattern[0].Amplitude,Is.EqualTo(55));
            Assert.That(HapticDesign.MinIntervalSeconds(HapticCue.Slide),Is.EqualTo(.05f));var throttle=new HapticThrottle();
            Assert.That(throttle.TryAccept(HapticCue.Slide,0),Is.True);Assert.That(throttle.TryAccept(HapticCue.Slide,.02f),Is.False);Assert.That(throttle.TryAccept(HapticCue.Exit,.02f),Is.True);Assert.That(throttle.TryAccept(HapticCue.Slide,.051f),Is.True);
        }
        /// <summary>Checks director and settings with isolated ice-arrow state.</summary>
        [Test]public void DirectorAndSettings()
        {
            using(var s=new AudioTestScene(IceTestBoards.Cycle()))
            {
                var player=new RecordingHapticsPlayer();var director=s.Host.AddComponent<HapticsDirector>();director.Initialize(s.Game,s.Settings,player,()=>0);
                s.TapId(1);s.Game.Undo();Assert.That(player.Cues,Is.EqualTo(new[]{HapticCue.Slide,HapticCue.Undo}));
                s.Settings.HapticsEnabled=false;player.Cues.Clear();s.TapId(1);s.Game.Undo();Assert.That(player.Cues,Is.Empty);
            }
        }
        /// <summary>Checks burst and pool cap with isolated ice-arrow state.</summary>
        [Test]public void BurstAndPoolCap()
        {
            using(var s=new EffectsTestScene(IceTestBoards.Cycle()))
            {
                s.Gameplay.TapId(1);Assert.That(s.Effects.Particles.ActiveCount,Is.EqualTo(ViewTheme.IceBurstCount));
                for(int i=0;i<100;i++)s.Effects.PlaySlideStop(s.Gameplay.Game.Session.Board.GetArrowById(1));
                Assert.That(s.Effects.Particles.ActiveCount,Is.LessThanOrEqualTo(ViewTheme.ParticleCapacity));Assert.That(s.Effects.Streaks.ActiveCount,Is.Zero);
            }
        }
        /// <summary>Checks state independent with isolated ice-arrow state.</summary>
        [Test]public void StateIndependent()
        {
            using(var s=new EffectsTestScene(IceTestBoards.Cycle()))
            {
                var plain=new GameSession(IceTestBoards.Cycle());
                foreach(int id in new[]{1,1,4,3})
                {
                    s.Gameplay.TapId(id);plain.TryMove(plain.Board.GetArrowById(id));
                    Assert.That(IceTestBoards.Key(s.Gameplay.Game.Session.Board),Is.EqualTo(IceTestBoards.Key(plain.Board)));
                    Assert.That(s.Gameplay.Game.MoveCount,Is.EqualTo(plain.MoveCount));Assert.That(s.Gameplay.Game.MistakeCount,Is.EqualTo(plain.MistakeCount));Assert.That(s.Gameplay.Game.CanUndo,Is.EqualTo(plain.CanUndo));
                }
                while(plain.CanUndo)
                {
                    plain.TryUndo();s.Gameplay.Game.Undo();Assert.That(IceTestBoards.Key(s.Gameplay.Game.Session.Board),Is.EqualTo(IceTestBoards.Key(plain.Board)));Assert.That(s.Gameplay.Game.UndoCount,Is.EqualTo(plain.UndoCount));
                }
            }
        }
    }
}
