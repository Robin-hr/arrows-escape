using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Composes independent visual layers through one transform/render writer.</summary>
    public sealed class ArrowView : MonoBehaviour
    {
        /// <summary>Optional ribbon renderer; null for legacy one-cell sprites.</summary>
        public PathArrowView Path { get; private set; }
        /// <summary>Attaches the path mesh after ordinary initialization.</summary>
        public void InitializePath(int width,int height,Material material)
        {
            // Unity permits only one renderer per object. Keep the public sprite-state
            // adapter on a disabled child; the root renderer is the visible path mesh.
            var state=new GameObject("Path sprite state");state.transform.SetParent(transform,false);
            SpriteRenderer old=SpriteRenderer;
            SpriteRenderer=state.AddComponent<SpriteRenderer>();SpriteRenderer.sharedMaterial=material;SpriteRenderer.enabled=false;
            DestroyImmediate(old);
            Path=gameObject.AddComponent<PathArrowView>();Path.Initialize(Arrow,width,height,material);
            SpriteRenderer.enabled=false;transform.rotation=Quaternion.identity;originalScale=Vector3.one;Apply();
        }
        /// <summary>Optional skin visuals; null keeps the legacy classic renderer exactly.</summary>
        public SkinVisual Skin { get; private set; }
        /// <summary>Applies a recipe to Normal arrows only, leaving the legacy base state intact.</summary>
        public void SetSkin(string id,bool lightBoard)
        {
            if(Arrow==null||Arrow.Kind==ArrowKind.Ice)return;var recipe=ArrowSkinRegistry.Find(id);
            if(recipe!=null&&Skin==null)Skin=gameObject.AddComponent<SkinVisual>();
            if(Skin!=null){Skin.Configure(this,recipe,lightBoard);Apply();}
        }
        private Color originalColor;
        private Vector3 originalScale;
        private bool basePositionSet;
        private bool highlighted;
        private bool stillOver;
        private bool flashing;
        private Color flashTint;
        private float flashScale = 1;
        private bool hinted;
        private BoardView boardStyle;
        private Color hintTint;
        private float hintScale = 1;
        /// <summary>Whether the independent hint layer is active.</summary>
        public bool HasHint => hinted;
        /// <summary>Sets the hint layer without touching position, rotation, or other visual layers.</summary>
        public void SetHint(Color tint, float scaleMultiplier)
        { hinted = true; hintTint = tint; hintScale = Mathf.Max(0, scaleMultiplier); Apply(); }
        /// <summary>Clears only the hint layer, restoring exact underlying values.</summary>
        public void ClearHint() { hinted = false; hintScale = 1; Apply(); }
        private Vector2 offset;
        private float alpha = 1;
        private int sortingOrder;
        /// <summary>The core arrow represented by this view.</summary>
        public Arrow Arrow { get; private set; }
        /// <summary>The renderer using Unity's default unlit sprite material.</summary>
        public SpriteRenderer SpriteRenderer { get; private set; }
        /// <summary>The current logical cell center, initially set by BoardView and updated by Rebase.</summary>
        public Vector2 BaseWorldPosition { get; private set; }
        /// <summary>The current world-space animation offset from the cell center.</summary>
        public Vector2 Offset => offset;
        /// <summary>The opacity multiplier independent of tint layers.</summary>
        public float Alpha => alpha;
        /// <summary>Exact underlying tint restored when every temporary layer clears.</summary>
        public Color BaseColor { get=>originalColor; set { originalColor=value;Apply(); } }
        /// <summary>Current composed position, independent of the logical cell center.</summary>
        public Vector2 CurrentWorldPosition=>BaseWorldPosition+offset;
        /// <summary>Updates immutable arrow identity while optionally preserving visual continuity.</summary>
        public void Rebase(Arrow arrow,Vector2 newBaseWorldPosition,bool keepWorldPosition)
        {
            Vector2 current=CurrentWorldPosition;Arrow=arrow;BaseWorldPosition=newBaseWorldPosition;basePositionSet=true;
            offset=keepWorldPosition?current-newBaseWorldPosition:Vector2.zero;Apply();
        }

        /// <summary>Assigns the arrow, sprite, shared default sprite material, tint, and direction.</summary>
        public void Initialize(Arrow arrow, Material spriteMaterial)
        {
            Arrow = arrow;
            SpriteRenderer = GetComponent<SpriteRenderer>();
            if(SpriteRenderer==null)SpriteRenderer=gameObject.AddComponent<SpriteRenderer>();
            SpriteRenderer.sprite = arrow.Kind==ArrowKind.Ice?SpriteFactory.GetIceArrowSprite():SpriteFactory.GetArrowSprite();
            // URP initially assigns a lit material; the board supplies Sprites/Default instead.
            SpriteRenderer.sharedMaterial = spriteMaterial;
            if(arrow.Kind==ArrowKind.Ice)
            {
                // Reuse the existing unlit material; per-renderer texture data needs no new material.
                var properties=new MaterialPropertyBlock();properties.SetTexture("_MainTex",SpriteRenderer.sprite.texture);SpriteRenderer.SetPropertyBlock(properties);
            }
            transform.rotation = Quaternion.Euler(0, 0, ArrowRotation.GetRotationZ(arrow.Direction));
            originalColor = arrow.Kind==ArrowKind.Ice?ViewTheme.IceArrowColor:ViewTheme.ArrowColor;
            originalScale = Vector3.one * ViewTheme.ArrowFillRatio;
            BaseWorldPosition = transform.position;
            basePositionSet = false;
            ResetVisual();
        }

        /// <summary>Sets the fixed cell center once after initialization.</summary>
        public void SetBaseWorldPosition(Vector2 position)
        {
            if (basePositionSet) return;
            BaseWorldPosition = position;
            basePositionSet = true;
            Apply();
        }

        /// <summary>Sets highlight state without disturbing flash, offset, or alpha layers.</summary>
        public void SetHighlight(bool on, bool stillOver = true)
        {
            highlighted = on;
            this.stillOver = stillOver;
            Apply();
        }

        /// <summary>Sets an absolute offset; offsets never accumulate.</summary>
        public void SetOffset(Vector2 worldOffset) { offset = worldOffset; Apply(); }

        /// <summary>Sets opacity clamped to zero through one.</summary>
        public void SetAlpha(float value) { alpha = Mathf.Clamp01(value); Apply(); }

        /// <summary>Overrides tint and adds a pulse multiplier while preserving other layers.</summary>
        public void SetFlash(Color tint, float scaleMultiplier)
        {
            flashing = true;
            flashTint = tint;
            flashScale = Mathf.Max(0, scaleMultiplier);
            Apply();
        }

        /// <summary>Clears flash feedback, revealing any active highlight.</summary>
        public void ClearFlash() { flashing = false; flashScale = 1; Apply(); }

        /// <summary>Sets rendering order independently of tint and movement.</summary>
        public void SetSortingOrder(int value) { sortingOrder = value; Apply(); }

        /// <summary>Clears every layer, restoring the exact original appearance and cell center.</summary>
        public void ResetVisual()
        {
            offset = Vector2.zero;
            if(Path!=null)Path.SetDistance(0);
            alpha = 1;
            highlighted = flashing = hinted = false;
            hintScale = 1;
            stillOver = true;
            flashScale = 1;
            sortingOrder = ViewTheme.ArrowSortingOrder;
            Apply();
        }

        private void Apply()
        {
            if (SpriteRenderer == null) return;
            Color tint = flashing ? flashTint : highlighted ?
                (stillOver ? ViewTheme.HighlightColor : ViewTheme.DraggedAwayColor) : hinted ? hintTint : originalColor;
            if(flashing||highlighted||hinted){if(boardStyle==null)boardStyle=GetComponentInParent<BoardView>();if(boardStyle!=null&&boardStyle.HasDarkTheme)tint=ArrowTint.EnsureContrast(tint,ThemeDesign.Background(boardStyle.ThemeId));}
            tint.a *= alpha;
            float scale = (highlighted ? ViewTheme.HighlightScaleMultiplier : 1) * (flashing ? flashScale : 1) * (hinted ? hintScale : 1);
            transform.position = new Vector3(BaseWorldPosition.x + offset.x, BaseWorldPosition.y + offset.y, 0);
            transform.localScale = originalScale * scale;
            SpriteRenderer.color = tint;
            SpriteRenderer.sortingOrder = sortingOrder;
            if(Path!=null)Path.SetAppearance(tint,sortingOrder);
            if(Skin!=null)Skin.SetAppearance(tint,flashing||highlighted||hinted,alpha,sortingOrder);
        }
    }
}

