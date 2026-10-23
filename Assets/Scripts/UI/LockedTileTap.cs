using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace ArrowEscape.UI
{
    /// <summary>Reports clicks on a level tile even while its Button is non-interactable (locked tiles).</summary>
    public sealed class LockedTileTap : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>Raised on every click.</summary>
        public event Action Tapped;
        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke();
        /// <summary>Simulates a click (tests).</summary>
        public void Tap() => Tapped?.Invoke();
    }
}
