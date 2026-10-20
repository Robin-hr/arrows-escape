using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Observes gameplay without modifying it; freezes effects during pause.</summary>
    [ExecuteAlways]
    public sealed class EffectsDirector : MonoBehaviour
    {
        private GameController controller;
        private BoardView boardView;
        private EffectsController effects;
        private Arrow lastExit;
        private bool initialized, warned, celebrated;
        /// <summary>Binds optional visual feedback to real gameplay events.</summary>
        public void Initialize(GameController game, BoardView view, EffectsController player)
        {
            Unsubscribe(); if (effects != null) effects.ClearAll();
            controller = game; boardView = view; effects = player; initialized = true; lastExit = null; celebrated = false;
            if (controller == null || boardView == null) { Missing(); return; }
            enabled = true; if (effects != null) effects.Bind(boardView);
            controller.ArrowExited += OnExit; controller.BoardCleared += OnComplete;
            controller.ArrowRestored += OnUndo; controller.LevelStarted += OnLevelStarted;
            controller.ArrowSlid+=OnSlide;controller.SlideReverted+=OnUndo;
        }
        private void Start()
        {
            if (initialized) return;
            GameController game = FindFirstObjectByType<GameController>(); BoardView view = game == null ? null : game.GetComponent<BoardView>();
            Initialize(game,view,GetComponent<EffectsController>());
        }
        private void Missing() { if (!warned) { warned = true; Debug.LogWarning("ArrowEscape effects: no GameController or BoardView found; effects disabled.",this); } enabled = false; }
        private void OnExit(Arrow arrow)
        {
            lastExit = arrow; if (effects == null || controller.Session == null) return; Board b = controller.Session.Board;
            effects.PlayExit(arrow,b.Width,b.Height,controller.MoveCount);
        }
        private void OnComplete()
        {
            if (celebrated || effects == null || controller.Session == null) return; celebrated = true; Board b = controller.Session.Board;
            effects.PlayComplete(b.Width,b.Height,lastExit == null ? (b.Width-1)/2 : lastExit.X,lastExit == null ? (b.Height-1)/2 : lastExit.Y);
        }
        private void OnUndo(Arrow arrow) { if (effects != null) effects.ClearStreaks(); }
        private void OnSlide(Arrow before,Arrow after){if(effects!=null)effects.PlaySlideStop(after);}
        private void OnLevelStarted() { if (effects != null) effects.ClearAll(); lastExit = null; celebrated = false; }
        /// <summary>Automatic-frame path: hidden boards clear once; paused boards freeze.</summary>
        public void AdvanceFrame(float deltaTime)
        {
            if (effects == null || boardView == null || controller == null) return;
            if (!boardView.IsVisible) { if (effects.HasActiveEffects) effects.ClearAll(); return; }
            if (!controller.IsPaused) effects.Tick(deltaTime);
        }
        private void Update() { if (Application.isPlaying) AdvanceFrame(Time.deltaTime); }
        private void Unsubscribe()
        {
            if (controller == null) return;
            controller.ArrowExited -= OnExit; controller.BoardCleared -= OnComplete;
            controller.ArrowRestored -= OnUndo; controller.LevelStarted -= OnLevelStarted;
            controller.ArrowSlid-=OnSlide;controller.SlideReverted-=OnUndo;
        }
        private void OnDestroy() { Unsubscribe(); if (effects != null) effects.ClearAll(); }
    }
}
