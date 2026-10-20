using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
using UnityEngine.EventSystems;
namespace ArrowEscape.UI
{
    /// <summary>Small press feedback with no per-frame allocations or temporary animation objects.</summary>
    public sealed class ButtonFeel : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        private Vector3 original=Vector3.one;
        private bool pressed,returning;
        private float elapsed;
        private UnityEngine.UI.Button button;
        private void Awake(){original=transform.localScale;button=GetComponent<UnityEngine.UI.Button>();}
        /// <summary>Presses to 95 percent and requests a setting-gated soft click.</summary>
        public void OnPointerDown(PointerEventData data)
        {
            if(button==null||!button.interactable||data.button!=PointerEventData.InputButton.Left)return;
            pressed=true;returning=false;transform.localScale=original*.95f;
            var game=FindFirstObjectByType<GameController>();if(game!=null)game.RequestUiFeedback(UiFeedbackCue.Button);
        }
        /// <summary>Releases gently.</summary>
        public void OnPointerUp(PointerEventData data)=>Release();
        /// <summary>Cancels a pressed scale when leaving the control.</summary>
        public void OnPointerExit(PointerEventData data)=>Release();
        private void Release(){if(!pressed)return;pressed=false;returning=true;elapsed=0;}
        /// <summary>Explicit animation driver.</summary>
        public void Tick(float dt){if(!returning||!(dt>0)||float.IsInfinity(dt))return;elapsed+=dt;float t=Mathf.Clamp01(elapsed/.12f);transform.localScale=original*Mathf.Lerp(.95f,1,1-Mathf.Pow(1-t,3));if(t==1)returning=false;}
        private void Update(){if(Application.isPlaying)Tick(Time.unscaledDeltaTime);}
        private void OnDisable(){pressed=returning=false;transform.localScale=original;}
    }
}
