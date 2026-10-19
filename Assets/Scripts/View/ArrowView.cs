using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Displays one immutable core arrow without handling input or game rules.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ArrowView : MonoBehaviour
    {
        /// <summary>The core arrow represented by this view.</summary>
        public Arrow Arrow { get; private set; }
        /// <summary>The renderer using Unity's default unlit sprite material.</summary>
        public SpriteRenderer SpriteRenderer { get; private set; }

        /// <summary>Assigns the arrow, sprite, shared default sprite material, tint, and direction.</summary>
        public void Initialize(Arrow arrow, Material spriteMaterial)
        {
            Arrow = arrow;
            SpriteRenderer = GetComponent<SpriteRenderer>();
            SpriteRenderer.sprite = SpriteFactory.GetArrowSprite();
            SpriteRenderer.color = ViewTheme.ArrowColor;
            SpriteRenderer.sortingOrder = ViewTheme.ArrowSortingOrder;
            // URP initially assigns a lit material; the board supplies Sprites/Default instead.
            SpriteRenderer.sharedMaterial = spriteMaterial;
            transform.rotation = Quaternion.Euler(0, 0, ArrowRotation.GetRotationZ(arrow.Direction));
            transform.localScale = Vector3.one * ViewTheme.ArrowFillRatio;
        }
    }
}
