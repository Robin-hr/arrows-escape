using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Theme-backed implementation of the existing menu presentation seam.</summary>
    public sealed class ThemeMenuBackground : MonoBehaviour,IMenuBackground
    {
        private LiveThemePreview preview;
        private UnityEngine.UI.RawImage image;
        /// <summary>Creates an isolated live backdrop behind menu content.</summary>
        public void Build(RectTransform parent)
        {
            image=UiFactory.Rect("EquippedTheme",parent,Vector2.zero,Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();UiFactory.Stretch(image.rectTransform);image.raycastTarget=false;
            var go=new GameObject("Menu theme renderer");go.transform.SetParent(transform,false);preview=go.AddComponent<LiveThemePreview>();preview.Initialize(new Vector2(42000,42000),540,960);preview.AutoTick=false;image.texture=preview.Texture;image.gameObject.SetActive(false);go.SetActive(false);
        }
        /// <summary>Paper preserves the exact prior dot-and-ribbon menu.</summary>
        public void Show(string id){bool shown=ThemeDesign.Index(id)>=0;preview.gameObject.SetActive(shown);image.gameObject.SetActive(shown);if(shown)preview.Show(id);}
        /// <summary>Theme presentation does not restart player state.</summary>
        public void Restart(){}
        /// <summary>Advances already prepared theme layers.</summary>
        public void Tick(float dt){if(preview!=null&&preview.gameObject.activeSelf)preview.Tick(dt);}
    }
}
