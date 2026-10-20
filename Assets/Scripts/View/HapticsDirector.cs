using System;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Observes gameplay/settings and throttles independent prepared haptic cues.</summary>
    [ExecuteAlways]
    public sealed class HapticsDirector : MonoBehaviour
    {
        private GameController controller;
        private SettingsModel settings;
        private IHapticsPlayer player;
        private Func<float> clock;
        private readonly HapticThrottle throttle = new HapticThrottle();
        private bool initialized, previousEnabled, warned, ownsPlayer;
        private bool HapticsOn => settings == null || settings.HapticsEnabled;
        /// <summary>Injects hardware and time; null settings means on, null clock uses unscaled time.</summary>
        public void Initialize(GameController game, SettingsModel model, IHapticsPlayer value, Func<float> time)
        {
            Unsubscribe(); ReleasePlayer(); controller = game; settings = model; player = value ?? HapticsPlayerFactory.Create();
            ownsPlayer = value == null; clock = time ?? ReadClock; initialized = true; previousEnabled = HapticsOn; throttle.Reset();
            if (controller == null) { Missing(); return; }
            enabled = true; controller.ArrowExited += OnExit; controller.MoveBlocked += OnBlocked; controller.ArrowRestored += OnUndo;
            controller.BoardCleared += OnComplete; controller.LevelStarted += OnLevelStarted; if (settings != null) settings.Changed += OnSettingsChanged;
            controller.StarPresented += OnStar;
            controller.ArrowSlid+=OnSlide;controller.SlideReverted+=OnUndo;
            if (!HapticsOn) player.Cancel();
        }
        private static float ReadClock() => Time.unscaledTime;
        private void Start()
        {
            if (initialized) return; GameController game = FindFirstObjectByType<GameController>();
            if (game == null) { Missing(); return; }
            GameBootstrap bootstrap = FindFirstObjectByType<GameBootstrap>(); if (bootstrap != null) bootstrap.EnsureInitialized();
            Initialize(game,bootstrap == null ? null : bootstrap.Settings.Model,null,null);
        }
        private void Missing() { if (!warned) { warned = true; Debug.LogWarning("ArrowEscape haptics: no GameController found; haptics disabled.",this); } enabled = false; }
        private void Play(HapticCue cue) { if (HapticsOn && player != null && player.IsSupported && throttle.TryAccept(cue,clock())) player.Play(cue); }
        /// <summary>Plays a light presentation cue through the live haptics preference.</summary>
        public void PlayUi(UiFeedbackCue cue)=>Play(cue==UiFeedbackCue.Coin?HapticCue.Coin:cue==UiFeedbackCue.Purchase?HapticCue.Purchase:HapticCue.Button);
        private void OnExit(Arrow arrow) => Play(HapticCue.Exit);
        private void OnBlocked(Arrow arrow, Arrow blocker) => Play(HapticCue.Blocked);
        private void OnUndo(Arrow arrow) => Play(HapticCue.Undo);
        private void OnSlide(Arrow before,Arrow after)=>Play(HapticCue.Slide);
        private void OnStar(int index) { if (!controller.IsPaused) Play(HapticCue.Star); }
        private void OnComplete() => Play(HapticCue.LevelComplete);
        private void OnLevelStarted() { player?.Cancel(); throttle.Reset(); }
        private void OnSettingsChanged()
        {
            bool now = HapticsOn; if (now == previousEnabled) return; previousEnabled = now;
            if (!now) player?.Cancel(); else Play(HapticCue.Preview);
        }
        private void OnApplicationPause(bool paused) { if (paused) player?.Cancel(); }
        /// <summary>Logs support, preference, and cue lengths once when explicitly requested.</summary>
        [ContextMenu("Test: Log Haptics Status")]
        public void LogHapticsStatus()
        {
            Debug.Log("Haptics supported: " + (player != null && player.IsSupported) + "; enabled: " + HapticsOn +
                "; Exit " + HapticWaveform.TotalMilliseconds(HapticDesign.GetPattern(HapticCue.Exit)) + " ms; Blocked " + HapticWaveform.TotalMilliseconds(HapticDesign.GetPattern(HapticCue.Blocked)) +
                " ms; Undo " + HapticWaveform.TotalMilliseconds(HapticDesign.GetPattern(HapticCue.Undo)) + " ms; LevelComplete " + HapticWaveform.TotalMilliseconds(HapticDesign.GetPattern(HapticCue.LevelComplete)) +
                " ms; Preview " + HapticWaveform.TotalMilliseconds(HapticDesign.GetPattern(HapticCue.Preview)) + " ms.",this);
        }
        private void Unsubscribe()
        {
            if (controller != null) controller.StarPresented -= OnStar;
            if (controller != null) { controller.ArrowExited -= OnExit; controller.MoveBlocked -= OnBlocked; controller.ArrowRestored -= OnUndo; controller.BoardCleared -= OnComplete; controller.LevelStarted -= OnLevelStarted; }
            if (settings != null) settings.Changed -= OnSettingsChanged;
            if(controller!=null){controller.ArrowSlid-=OnSlide;controller.SlideReverted-=OnUndo;}
        }
        private void ReleasePlayer() { player?.Cancel(); if (ownsPlayer && player is IDisposable disposable) disposable.Dispose(); player = null; ownsPlayer = false; }
        private void OnDestroy() { Unsubscribe(); ReleasePlayer(); }
    }
}
