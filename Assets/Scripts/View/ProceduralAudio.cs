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
        private AudioClip thud, jingle,slide,lose;
        private readonly AudioClip[] skinClips=new AudioClip[20];
        private readonly bool[] skinUnavailable=new bool[20];
        private readonly AudioClip[] uiClips=new AudioClip[3];
        private readonly bool[] uiUnavailable=new bool[3];
        private readonly AudioClip[] stars = new AudioClip[3];
        private readonly AudioClip[,] tapNotes = new AudioClip[TapSoundSynth.Count, MusicScale.NoteCount(SoundDesign.OctaveCount)];
        /// <summary>Instrument used for exit and undo notes (0 = Classic Chime; Step 14L shop sounds).</summary>
        public int TapInstrument { get; set; }
        private readonly bool[] starUnavailable = new bool[3];
        /// <summary>Returns a cached clip, recreating destroyed clips; failures return null quietly.</summary>
        public AudioClip GetClip(SoundEvent sound)
        {
            try
            {
                switch (sound.Cue)
                {
                    case SoundCue.SkinExit:case SoundCue.SkinBlocked:
                        int skin=Math.Max(0,Math.Min(9,sound.ScaleStep))*2+(sound.Cue==SoundCue.SkinBlocked?1:0);
                        if(skinUnavailable[skin])return null;
                        if(skinClips[skin]==null)skinClips[skin]=Create("Skin texture "+skin,SkinSoundSynth.Render(skin/2,skin%2==1));skinUnavailable[skin]=skinClips[skin]==null;return skinClips[skin];
                    case SoundCue.Coin: case SoundCue.Purchase: case SoundCue.Button:
                        int ui=(int)sound.Cue-(int)SoundCue.Coin;
                        if(uiUnavailable[ui])return null;
                        if(uiClips[ui]==null)uiClips[ui]=Create("UI "+ui,ui==2?SynthMath.RenderThud(.04f,SoundDesign.SampleRate):ui==1?GameSounds.CashRegister(SoundDesign.SampleRate):
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
                        int instrument = Math.Max(0, Math.Min(TapSoundSynth.Count - 1, TapInstrument));
                        if (instrument > 0 && (sound.Cue == SoundCue.Exit || sound.Cue == SoundCue.Undo))
                        {
                            if (tapNotes[instrument, step] == null)
                                tapNotes[instrument, step] = Create("Tap " + instrument + " " + step, TapSoundSynth.Render(instrument, MusicScale.FrequencyHz(step, SoundDesign.RootHz), SoundDesign.SampleRate));
                            if (tapNotes[instrument, step] != null) return tapNotes[instrument, step];
                        }
                        if (unavailable[step]) return null;
                        if (notes[step] == null)
                            notes[step] = Create("Pentatonic " + step, SynthMath.RenderNote(
                                MusicScale.FrequencyHz(step, SoundDesign.RootHz), SoundDesign.NoteSeconds, SoundDesign.SampleRate));
                        unavailable[step] = notes[step] == null;
                        return notes[step];
                    case SoundCue.Blocked:
                        if (unavailable[notes.Length]) return null;
                        if (thud == null) thud = Create("Blocked", GameSounds.WrongBuzz(SoundDesign.SampleRate));
                        unavailable[notes.Length] = thud == null;
                        return thud;
                    case SoundCue.Lose:
                        if (lose == null) lose = Create("Lose", GameSounds.LoseMusic(SoundDesign.SampleRate));
                        return lose;
                    case SoundCue.LevelComplete:
                        if (unavailable[notes.Length + 1]) return null;
                        if (jingle == null) jingle = Create("Victory fanfare", VictoryMusic.Render(SoundDesign.SampleRate));
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
            for(int i=0;i<20;i++)GetClip(new SoundEvent(i%2==0?SoundCue.SkinExit:SoundCue.SkinBlocked,i/2,0));
            for (int i = 0; i < notes.Length; i++) GetClip(new SoundEvent(SoundCue.Exit, i, 0));
            GetClip(SoundPlanner.ForBlocked());
            GetClip(SoundPlanner.ForLevelComplete());
            GetClip(SoundPlanner.ForSlide());
            GetClip(SoundPlanner.ForLose());
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
            for(int i=0;i<skinClips.Length;i++){DestroyClip(skinClips[i]);skinClips[i]=null;skinUnavailable[i]=false;}
            Array.Clear(unavailable, 0, unavailable.Length);
            DestroyClip(thud); DestroyClip(jingle); thud = null; jingle = null;
            DestroyClip(slide);slide=null;DestroyClip(lose);lose=null;
            for (int a = 0; a < tapNotes.GetLength(0); a++) for (int b = 0; b < tapNotes.GetLength(1); b++) { DestroyClip(tapNotes[a, b]); tapNotes[a, b] = null; }
        }
        /// <summary>Generates every note of a tap instrument ahead of play (equip time), so taps never synthesise.</summary>
        public void PrepareTap(int instrument)
        {
            int keep = TapInstrument; TapInstrument = instrument;
            for (int i = 0; i < notes.Length; i++) GetClip(new SoundEvent(SoundCue.Exit, i, 0));
            TapInstrument = keep;
        }
        private static void DestroyClip(AudioClip clip)
        {
            if (clip == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(clip);
            else UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
