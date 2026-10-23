using System;

namespace ArrowEscape.Core
{
    /// <summary>Maps post-move progress to deterministic, allocation-free sound requests.</summary>
    public static class SoundPlanner
    {
        /// <summary>Ice slide gain, independent of melody progress.</summary>
        public const float SlideVolume=.5f;
        /// <summary>Softer gain when undoing a slide.</summary>
        public const float SlideUndoVolume=.35f;
        /// <summary>Plans an ice slide without selecting a musical scale step.</summary>
        public static SoundEvent ForSlide()=>new SoundEvent(SoundCue.Slide,-1,SlideVolume);
        /// <summary>Reuses the glide clip more softly for slide undo.</summary>
        public static SoundEvent ForSlideUndo()=>new SoundEvent(SoundCue.Slide,-1,SlideUndoVolume);
        // These gains leave headroom for several overlapping voices.
        /// <summary>Quiet rising pentatonic chimes for earned stars.</summary>
        public static SoundEvent ForStar(int index) => new SoundEvent(SoundCue.Star, 5 + Math.Max(0, Math.Min(2, index)) * 2, .3f);
        /// <summary>Exit gain.</summary>
        public const float ExitVolume = 0.6f;
        /// <summary>Blocked-tap gain.</summary>
        public const float BlockedVolume = 0.55f;
        /// <summary>Undo gain, softer than the original exit.</summary>
        public const float UndoVolume = 0.4f;
        /// <summary>Hint chime gain.</summary>
        public const float HintVolume = 0.4f;
        /// <summary>Completion arpeggio gain.</summary>
        public const float CompleteVolume = 0.7f;
        /// <summary>Sound-on preview gain.</summary>
        public const float PreviewVolume = 0.5f;
        /// <summary>Soft presentation cues using cached procedural clips.</summary>
        public static SoundEvent ForUi(UiFeedbackCue cue)=>cue==UiFeedbackCue.Coin ? new SoundEvent(SoundCue.Coin,8,.28f) :
            cue==UiFeedbackCue.Purchase ? new SoundEvent(SoundCue.Purchase,10,.6f) : new SoundEvent(SoundCue.Button,-1,.12f);
        private static int Step(int value) => MusicScale.PingPongIndex(value, MusicScale.NoteCount(SoundDesign.OctaveCount));
        /// <summary>Plans the note for an exit using the updated move count.</summary>
        public static SoundEvent ForExit(int moveCount) => new SoundEvent(SoundCue.Exit, Step(Math.Max(moveCount, 1) - 1), ExitVolume);
        /// <summary>Replays the reversed exit's note using the updated move count.</summary>
        public static SoundEvent ForUndo(int moveCountAfterUndo) => new SoundEvent(SoundCue.Undo, Step(Math.Max(moveCountAfterUndo, 0)), UndoVolume);
        /// <summary>Plans a low thud.</summary>
        public static SoundEvent ForBlocked() => new SoundEvent(SoundCue.Blocked, -1, BlockedVolume);
        /// <summary>Plans the sad trombone played when the hearts run out (Step 14M).</summary>
        public static SoundEvent ForLose() => new SoundEvent(SoundCue.Lose, -1, 0.7f);
        /// <summary>Plans a soft high chime.</summary>
        public static SoundEvent ForHint() => new SoundEvent(SoundCue.Hint, SoundDesign.HintScaleStep, HintVolume);
        /// <summary>Plans the pre-rendered completion arpeggio.</summary>
        public static SoundEvent ForLevelComplete() => new SoundEvent(SoundCue.LevelComplete, -1, CompleteVolume);
        /// <summary>Plans the Sound-on preview.</summary>
        public static SoundEvent ForPreview() => new SoundEvent(SoundCue.Preview, SoundDesign.PreviewScaleStep, PreviewVolume);
    }
}
