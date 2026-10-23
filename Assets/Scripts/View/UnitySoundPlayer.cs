using System;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Six reusable 2D voices with deterministic round-robin stealing.</summary>
    [ExecuteAlways]
    public sealed class UnitySoundPlayer : MonoBehaviour, ISoundPlayer
    {
        /// <summary>Maximum number of simultaneous source objects.</summary>
        public const int VoiceCount = 6;
        private readonly AudioSource[] voices = new AudioSource[VoiceCount];
        private readonly bool[] busy = new bool[VoiceCount];
        private ProceduralAudio audio;
        private int lastIndex = -1;
        private bool prepared;
        private void Awake()
        {
            audio = new ProceduralAudio();
            for (int i = 0; i < voices.Length; i++)
            {
                var child = new GameObject("Audio Voice " + (i + 1));
                child.transform.SetParent(transform, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
                source.bypassEffects = true; source.priority = 128;
                voices[i] = source;
            }
            // Clip generation happens once here, before a player can tap.
            audio.Prepare();
            prepared = true;
        }
        /// <summary>Uses an idle source or steals one; unavailable audio is silently ignored.</summary>
        public void Play(SoundEvent soundEvent)
        {
            if (!prepared || audio == null) return;
            try
            {
                AudioClip clip = audio.GetClip(soundEvent);
                if (clip == null) return;
                for (int i = 0; i < voices.Length; i++) busy[i] = voices[i] == null || voices[i].isPlaying;
                int index = VoiceAllocator.Pick(busy, lastIndex);
                AudioSource voice = voices[index];
                if (voice == null) return;
                voice.Stop(); voice.clip = clip; voice.volume = soundEvent.Volume; voice.pitch = 1;
                lastIndex = index;
                // Edit Mode tests exercise mapping/pooling without requiring an output device.
                if (Application.isPlaying && voice.isActiveAndEnabled && AudioSettings.outputSampleRate > 0) voice.Play();
            }
            catch (Exception) { }
        }
        /// <summary>Exit/undo instrument (0 = Classic Chime, 1..10 = shop tap sounds); its notes are generated now.</summary>
        public int TapInstrument
        {
            get => audio != null ? audio.TapInstrument : 0;
            set { if (audio == null) return; if (value > 0) audio.PrepareTap(value); audio.TapInstrument = value; }
        }
        /// <summary>Stops all voices without disposing their reusable clips.</summary>
        public void StopAll()
        {
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null) voices[i].Stop();
        }
        private void OnDisable() => StopAll();
        private void OnDestroy() { StopAll(); audio?.Dispose(); }
    }
}
