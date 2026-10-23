using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Translates gameplay and live preferences into sound requests.</summary>
    [ExecuteAlways]
    public sealed class AudioDirector : MonoBehaviour
    {
        private GameController controller;
        private SettingsModel settings;
        private ISoundPlayer player;
        private bool initialized, previousSound, warningLogged, testingScale;
        private int testStep;
        private float testElapsed;
        private bool SoundOn => settings == null || settings.SoundEnabled;

        /// <summary>Injects dependencies without disk or an audio device; null settings means always on.</summary>
        public void Initialize(GameController game, SettingsModel model, ISoundPlayer soundPlayer)
        {
            Unsubscribe();
            if (PlayerExists()) player.StopAll();
            controller = game; settings = model;
            player = soundPlayer ?? GetComponent<UnitySoundPlayer>() ?? gameObject.AddComponent<UnitySoundPlayer>();
            initialized = true; testingScale = false; previousSound = SoundOn;
            if (equippedTap > 0 && player is UnitySoundPlayer tapPlayer && tapPlayer != null) tapPlayer.TapInstrument = equippedTap;
            EnsureAudioListener();
            if (controller == null) { MissingController(); return; }
            enabled = true;
            controller.ArrowExited += OnExit;
            controller.MoveBlocked += OnBlocked;
            controller.ArrowRestored += OnUndo;
            controller.HintShown += OnHint;
            controller.BoardCleared += OnComplete;
            controller.LevelStarted += OnLevelStarted;
            controller.StarPresented += OnStar;
            controller.ArrowSlid+=OnSlide;controller.SlideReverted+=OnSlideUndo;
            if (settings != null) settings.Changed += OnSettingsChanged;
            if (!SoundOn) Stop();
        }
        private void Start()
        {
            if (initialized) return;
            EnsureAudioListener();
            GameController game = FindFirstObjectByType<GameController>();
            if (game == null) { MissingController(); return; }
            GameBootstrap bootstrap = FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null) bootstrap.EnsureInitialized();
            Initialize(game, bootstrap == null ? null : bootstrap.Settings.Model, null);
        }
        private void MissingController()
        {
            if (!warningLogged) { warningLogged = true; Debug.LogWarning("ArrowEscape audio: no GameController found; audio disabled.", this); }
            enabled = false;
        }
        /// <summary>Ensures one enabled listener, reusing an existing listener before creating one.</summary>
        public static void EnsureAudioListener()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            AudioListener chosen = null;
            for (int i = 0; i < listeners.Length; i++)
            {
                if (chosen == null) chosen = listeners[i];
                else listeners[i].enabled = false;
            }
            if (chosen != null) { chosen.enabled = true; return; }
            Camera camera = Camera.main;
            // No static or persistent host: the listener shares this runtime's lifetime.
            if (camera != null)
            {
                chosen = camera.GetComponent<AudioListener>();
                if (chosen == null) chosen = camera.gameObject.AddComponent<AudioListener>();
            }
            else
            {
                AudioDirector director = FindFirstObjectByType<AudioDirector>();
                if (director != null)
                {
                    chosen = director.GetComponent<AudioListener>();
                    if (chosen == null) chosen = director.gameObject.AddComponent<AudioListener>();
                }
            }
            if (chosen != null) chosen.enabled = true;
        }
        private bool PlayerExists() => player != null && (!(player is Object unityObject) || unityObject != null);
        private void Play(SoundEvent sound) { if (SoundOn && PlayerExists()) player.Play(sound); }
        private void Stop() { testingScale = false; if (PlayerExists()) player.StopAll(); }
        /// <summary>Sad trombone when the hearts run out (Step 14M).</summary>
        public void PlayLose() => Play(SoundPlanner.ForLose());
        /// <summary>Plays economic UI through the same setting gate and six voices.</summary>
        public void PlayUi(UiFeedbackCue cue)=>Play(SoundPlanner.ForUi(cue));
        /// <summary>Quiet skin texture under the original signature cue; pause and live sound setting are respected.</summary>
        public void PlaySkin(int index,bool blocked=false)
        {if((controller==null||!controller.IsPaused)&&index>=0&&index<10)Play(new SoundEvent(blocked?SoundCue.SkinBlocked:SoundCue.SkinExit,index,(blocked?.12f:.18f)*(index==3?.35f:1)));}
        private int equippedTap, previewTap = -1, previewStep;
        private float previewElapsed;
        private static readonly int[] samplePhrase = { 0, 2, 4, 5, 7, 9 };
        /// <summary>Instrument of the running (or last) tap-sound sample, -1 before any.</summary>
        public int PreviewInstrument { get; private set; } = -1;
        /// <summary>Equipped tap instrument (Step 14L): the exit/undo melody plays with it.</summary>
        public void SetTapSound(int instrument)
        {
            equippedTap = instrument;
            if (previewTap < 0 && player is UnitySoundPlayer unity && unity != null) unity.TapInstrument = instrument;
        }
        /// <summary>Plays a six-note sample phrase with a tap sound, then restores the equipped one.</summary>
        public void PreviewTapSound(int instrument)
        {
            PreviewInstrument = instrument; previewTap = instrument; previewStep = 0; previewElapsed = 0.2f;
            if (player is UnitySoundPlayer unity && unity != null) unity.TapInstrument = instrument;
            if (!Application.isPlaying) { previewTap = -1; SetTapSound(equippedTap); }
        }
        private void TickPreview(float dt)
        {
            if (previewTap < 0) return;
            previewElapsed += dt;
            while (previewElapsed >= 0.2f && previewTap >= 0)
            {
                previewElapsed -= 0.2f;
                if (previewStep < samplePhrase.Length) Play(new SoundEvent(SoundCue.Exit, samplePhrase[previewStep++], SoundPlanner.ExitVolume));
                else { previewTap = -1; SetTapSound(equippedTap); }
            }
        }
        /// <summary>Shop/gallery demonstration never advances the real move-count melody.</summary>
        public void PlaySkinPreview(int index){if(index<0||index>=10)return;Play(SoundPlanner.ForPreview());Play(new SoundEvent(SoundCue.SkinExit,index,index==3?.063f:.18f));}
        private void OnExit(Arrow arrow) => Play(SoundPlanner.ForExit(controller.MoveCount));
        private void OnBlocked(Arrow arrow, Arrow blocker) => Play(SoundPlanner.ForBlocked());
        private void OnUndo(Arrow arrow) => Play(SoundPlanner.ForUndo(controller.MoveCount));
        private void OnHint(Arrow arrow) => Play(SoundPlanner.ForHint());
        private void OnStar(int index) { if (!controller.IsPaused) Play(SoundPlanner.ForStar(index)); }
        private void OnComplete() => Play(SoundPlanner.ForLevelComplete());
        private void OnLevelStarted() => Stop();
        private void OnSlide(Arrow before,Arrow after)=>Play(SoundPlanner.ForSlide());
        private void OnSlideUndo(Arrow arrow)=>Play(SoundPlanner.ForSlideUndo());
        private void OnSettingsChanged()
        {
            bool now = SoundOn;
            if (now == previousSound) return;
            previousSound = now;
            if (!now) Stop(); else Play(SoundPlanner.ForPreview());
        }
        /// <summary>Starts the scale demonstration; the first note follows after 0.25 seconds.</summary>
        [ContextMenu("Test: Play Scale")]
        public void TestPlayScale()
        {
            testingScale = SoundOn && PlayerExists(); testStep = 0; testElapsed = 0;
        }
        /// <summary>Plays one blocked-tap test sound, respecting Sound.</summary>
        [ContextMenu("Test: Play Blocked")]
        public void TestPlayBlocked() => Play(SoundPlanner.ForBlocked());
        /// <summary>Plays the completion test arpeggio, respecting Sound.</summary>
        [ContextMenu("Test: Play Level Complete")]
        public void TestPlayLevelComplete() => Play(SoundPlanner.ForLevelComplete());
        /// <summary>Advances the preallocated scale demonstration without coroutines.</summary>
        public void Tick(float deltaTime)
        {
            if (!testingScale || !(deltaTime > 0) || float.IsInfinity(deltaTime)) return;
            if (!SoundOn) { testingScale = false; return; }
            testElapsed += deltaTime;
            while (testElapsed + 0.000001f >= 0.25f && testingScale)
            {
                testElapsed -= 0.25f;
                Play(new SoundEvent(SoundCue.Exit, testStep++, SoundPlanner.ExitVolume));
                if (testStep == MusicScale.NoteCount(SoundDesign.OctaveCount)) testingScale = false;
            }
        }
        private void Update() { if (Application.isPlaying) { Tick(Time.deltaTime); TickPreview(Time.unscaledDeltaTime); } }
        private void OnApplicationPause(bool paused) { if (paused) Stop(); }
        private void Unsubscribe()
        {
            if (controller != null) controller.StarPresented -= OnStar;
            if (controller != null)
            {
                controller.ArrowExited -= OnExit; controller.MoveBlocked -= OnBlocked;
                controller.ArrowRestored -= OnUndo; controller.HintShown -= OnHint;
                controller.BoardCleared -= OnComplete; controller.LevelStarted -= OnLevelStarted;
                controller.ArrowSlid-=OnSlide;controller.SlideReverted-=OnSlideUndo;
            }
            if (settings != null) settings.Changed -= OnSettingsChanged;
        }
        private void OnDestroy() { Unsubscribe(); Stop(); }
    }
}
