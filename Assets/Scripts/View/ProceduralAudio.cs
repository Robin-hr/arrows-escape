using System;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Owns generated, mono, non-streamed clips; no imported sound files.</summary>
    public sealed class ProceduralAudio : IDisposable
    {
        private readonly AudioClip[] notes = new AudioClip[MusicScale.NoteCount(SoundDesign.OctaveCount)];
        private readonly bool[] unavailable = new bool[MusicScale.NoteCount(SoundDesign.OctaveCount) + 3];
        private AudioClip thud, jingle,slide;
        private readonly AudioClip[] uiClips=new AudioClip[3];
        private readonly bool[] uiUnavailable=new bool[3];
        private readonly AudioClip[] stars = new AudioClip[3];
        private readonly bool[] starUnavailable = new bool[3];
        /// <summary>Returns a cached clip, recreating destroyed clips; failures return null quietly.</summary>
        public AudioClip GetClip(SoundEvent sound)
        {
            try
            {
                switch (sound.Cue)
                {
                    case SoundCue.Coin: case SoundCue.Purchase: case SoundCue.Button:
                        int ui=(int)sound.Cue-(int)SoundCue.Coin;
                        if(uiUnavailable[ui])return null;
                        if(uiClips[ui]==null)uiClips[ui]=Create("UI "+ui,ui==2?SynthMath.RenderThud(.04f,SoundDesign.SampleRate):
                            SynthMath.RenderNote(MusicScale.FrequencyHz(ui==0?8:10,SoundDesign.RootHz),ui==0?.18f:.3f,SoundDesign.SampleRate));
                        uiUnavailable[ui]=uiClips[ui]==null;return uiClips[ui];
                    case SoundCue.Star:
                        int star = Math.Max(0, Math.Min(2, (sound.ScaleStep - 5) / 2));
                        if (starUnavailable[star]) return null;
                        if (stars[star] == null) stars[star] = Create("Star " + star,
                            SynthMath.RenderNote(MusicScale.FrequencyHz(5 + star * 2, SoundDesign.RootHz), .28f, SoundDesign.SampleRate));
                        starUnavailable[star] = stars[star] == null;
                        return stars[star];
                    case SoundCue.Slide:
                        if(unavailable[notes.Length+2])return null;
                        if(slide==null)slide=Create("Ice slide",SynthMath.RenderSlide(SoundDesign.SlideSeconds,SoundDesign.SampleRate));
                        unavailable[notes.Length+2]=slide==null;return slide;
                    case SoundCue.Exit: case SoundCue.Undo: case SoundCue.Hint: case SoundCue.Preview:
                        int step = Math.Max(0, Math.Min(notes.Length - 1, sound.ScaleStep));
                        if (unavailable[step]) return null;
                        if (notes[step] == null)
                            notes[step] = Create("Pentatonic " + step, SynthMath.RenderNote(
                                MusicScale.FrequencyHz(step, SoundDesign.RootHz), SoundDesign.NoteSeconds, SoundDesign.SampleRate));
                        unavailable[step] = notes[step] == null;
                        return notes[step];
                    case SoundCue.Blocked:
                        if (unavailable[notes.Length]) return null;
                        if (thud == null) thud = Create("Blocked", SynthMath.RenderThud(SoundDesign.ThudSeconds, SoundDesign.SampleRate));
                        unavailable[notes.Length] = thud == null;
                        return thud;
                    case SoundCue.LevelComplete:
                        if (unavailable[notes.Length + 1]) return null;
                        if (jingle == null) jingle = Create("Level complete", SynthMath.RenderMix(
                            SoundDesign.CreateJingle(), SoundDesign.JingleTotalSeconds, SoundDesign.SampleRate));
                        unavailable[notes.Length + 1] = jingle == null;
                        return jingle;
                    default: return null;
                }
            }
            catch (Exception) { return null; }
        }
        /// <summary>Prepares all clips during setup, keeping synthesis off the gameplay path.</summary>
        public void Prepare()
        {
            for (int i = 0; i < notes.Length; i++) GetClip(new SoundEvent(SoundCue.Exit, i, 0));
            GetClip(SoundPlanner.ForBlocked());
            GetClip(SoundPlanner.ForLevelComplete());
            GetClip(SoundPlanner.ForSlide());
            for (int i = 0; i < 3; i++) {GetClip(SoundPlanner.ForStar(i));GetClip(SoundPlanner.ForUi((UiFeedbackCue)i));}
        }
        private static AudioClip Create(string name, float[] samples)
        {
            AudioClip clip = null;
            try
            {
                clip = AudioClip.Create(name, samples.Length, 1, SoundDesign.SampleRate, false);
                if (clip == null) return null;
                clip.hideFlags = HideFlags.HideAndDontSave;
                if (clip.SetData(samples, 0)) return clip;
            }
            catch (Exception) { }
            DestroyClip(clip);
            return null;
        }
        /// <summary>Destroys all generated clips; later requests can safely recreate them.</summary>
        public void Dispose()
        {
            for (int i = 0; i < notes.Length; i++) { DestroyClip(notes[i]); notes[i] = null; }
            for (int i = 0; i < 3; i++) { DestroyClip(stars[i]); stars[i] = null; starUnavailable[i] = false; }
            for(int i=0;i<3;i++){DestroyClip(uiClips[i]);uiClips[i]=null;uiUnavailable[i]=false;}
            Array.Clear(unavailable, 0, unavailable.Length);
            DestroyClip(thud); DestroyClip(jingle); thud = null; jingle = null;
            DestroyClip(slide);slide=null;
        }
        private static void DestroyClip(AudioClip clip)
        {
            if (clip == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(clip);
            else UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
