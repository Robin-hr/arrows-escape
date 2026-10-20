using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Slide synthesis and event planning do not advance the exit melody.</summary>
    public class IceSoundTests
    {
        private static double Energy(float[] a,double hz)
        {
            double real=0,imaginary=0;
            for(int i=0;i<a.Length;i++){double phase=2*Math.PI*hz*i/44100;real+=a[i]*Math.Cos(phase);imaginary+=a[i]*Math.Sin(phase);}
            return real*real+imaginary*imaginary;
        }
        /// <summary>Checks samples with isolated ice-arrow state.</summary>
        [Test]public void Samples()
        {
            var a=SynthMath.RenderSlide(.16f,44100);Assert.That(a.Length,Is.EqualTo(7056));Assert.That(a[a.Length-1],Is.Zero);
            Assert.That(a,Is.EqualTo(SynthMath.RenderSlide(.16f,44100)));float peak=0;
            foreach(float sample in a){Assert.That(float.IsNaN(sample)||float.IsInfinity(sample),Is.False);peak=Math.Max(peak,Math.Abs(sample));}
            Assert.That(peak,Is.InRange(.75f,.85f));Assert.That(Energy(a,900),Is.GreaterThan(Energy(a,4000)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>SynthMath.RenderSlide(0,44100));Assert.Throws<ArgumentOutOfRangeException>(()=>SynthMath.RenderSlide(.16f,4000));
            Assert.That(SoundPlanner.ForSlide().Cue,Is.EqualTo(SoundCue.Slide));Assert.That(SoundPlanner.ForSlide().ScaleStep,Is.EqualTo(-1));
            Assert.That(SoundPlanner.ForSlideUndo().Volume,Is.LessThan(SoundPlanner.ForSlide().Volume));
        }
        /// <summary>Checks clips with isolated ice-arrow state.</summary>
        [Test]public void Clips()
        {
            using(var audio=new ProceduralAudio())
            {
                var first=audio.GetClip(SoundPlanner.ForSlide());Assert.That(first.samples,Is.EqualTo(7056));Assert.That(audio.GetClip(SoundPlanner.ForSlideUndo()),Is.SameAs(first));
                UnityEngine.Object.DestroyImmediate(first);Assert.That(audio.GetClip(SoundPlanner.ForSlide()),Is.Not.SameAs(first));
            }
        }
        /// <summary>Checks events and silence with isolated ice-arrow state.</summary>
        [Test]public void EventsAndSilence()
        {
            using(var s=new AudioTestScene(IceTestBoards.Cycle()))
            {
                s.TapId(1);Assert.That(s.Player.Events.Count,Is.EqualTo(1));Assert.That(s.Player.Events[0],Is.EqualTo(SoundPlanner.ForSlide()));
                s.Game.Undo();Assert.That(s.Player.Events[1],Is.EqualTo(SoundPlanner.ForSlideUndo()));s.TapId(1);s.TapId(4);
                Assert.That(s.Player.Events[3].Cue,Is.EqualTo(SoundCue.Exit));Assert.That(s.Player.Events[3].ScaleStep,Is.Zero);
                s.Game.Undo();s.Game.Undo();s.Player.Events.Clear();s.Game.SetPaused(true);s.TapId(1);Assert.That(s.Player.Events,Is.Empty);
                s.Game.SetPaused(false);s.Settings.SoundEnabled=false;s.TapId(1);s.Game.Undo();Assert.That(s.Player.Events,Is.Empty);
            }
        }
    }
}
