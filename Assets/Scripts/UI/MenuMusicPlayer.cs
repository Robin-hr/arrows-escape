using System.Threading.Tasks;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>
    /// Loops <see cref="MenuMusic"/> on the menu pages (home, levels, shop, profile, ranking, daily, journey map)
    /// and fades it out while a board is on screen. Follows the Sound setting. The tune is synthesised on a worker
    /// thread at startup so the first frame never stalls; Edit Mode never creates the clip.
    /// </summary>
    public sealed class MenuMusicPlayer : MonoBehaviour
    {
        /// <summary>Full music volume.</summary>
        public const float Volume = 0.32f;
        private const float FadeSeconds = 0.7f;
        private AudioSource source;
        private SettingsModel settings;
        private Task<float[]> rendering;
        private AudioClip clip;
        private float level;

        /// <summary>True while the current screen wants music.</summary>
        public bool Wanted { get; private set; }
        /// <summary>Current fade level 0..1 (times <see cref="Volume"/>).</summary>
        public float Level => level;
        /// <summary>True when music would be audible now (wanted and sound on).</summary>
        public bool Audible => Wanted && (settings == null || settings.SoundEnabled);

        /// <summary>Binds the Sound setting and starts preparing the tune in Play mode.</summary>
        public void Bind(SettingsModel model)
        {
            settings = model;
            if (!Application.isPlaying || rendering != null || clip != null) return;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = true; source.spatialBlend = 0; source.volume = 0; source.priority = 64; source.bypassEffects = true;
            rendering = Task.Run(() => MenuMusic.Render(SoundDesign.SampleRate));
        }

        /// <summary>Menus call this on every screen change.</summary>
        public void SetWanted(bool wanted) => Wanted = wanted;

        /// <summary>Explicit fade driver.</summary>
        public void Tick(float dt)
        {
            if (!(dt >= 0) || float.IsInfinity(dt)) return;
            float target = Audible ? 1f : 0f;
            level = Mathf.MoveTowards(level, target, dt / FadeSeconds);
            if (source == null) return;
            if (clip == null && rendering != null && rendering.IsCompleted)
            {
                if (rendering.Status == TaskStatus.RanToCompletion)
                {
                    clip = AudioClip.Create("Menu music", rendering.Result.Length, 1, SoundDesign.SampleRate, false);
                    clip.SetData(rendering.Result, 0); clip.hideFlags = HideFlags.HideAndDontSave; source.clip = clip;
                }
                rendering = null;
            }
            if (clip == null) return;
            source.volume = level * Volume;
            if (level > 0 && !source.isPlaying) source.Play();
            else if (level <= 0 && source.isPlaying) source.Pause();
        }

        private void Update() { if (Application.isPlaying) Tick(Time.unscaledDeltaTime); }
        private void OnApplicationPause(bool paused) { if (paused && source != null) source.Pause(); }
        private void OnDestroy() { if (clip != null) Destroy(clip); }
    }
}
