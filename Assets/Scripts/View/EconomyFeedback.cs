using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Runtime composition for presentation cues, sharing the existing audio and haptic players.</summary>
    public sealed class EconomyFeedback : MonoBehaviour
    {
        private GameController game;
        private AudioDirector audio;
        private HapticsDirector haptics;
        /// <summary>Injects existing players; tests can bind recording directors without devices.</summary>
        public void Bind(GameController controller,AudioDirector sound,HapticsDirector motor)
        {if(game!=null)game.UiFeedbackRequested-=OnCue;game=controller;audio=sound;haptics=motor;if(game!=null)game.UiFeedbackRequested+=OnCue;}
        private void Start()=>Bind(GetComponent<GameController>(),GetComponent<AudioDirector>(),GetComponent<HapticsDirector>());
        private void OnCue(UiFeedbackCue cue){if(audio!=null)audio.PlayUi(cue);if(haptics!=null)haptics.PlayUi(cue);}
        private void OnDestroy(){if(game!=null)game.UiFeedbackRequested-=OnCue;}
    }
}
