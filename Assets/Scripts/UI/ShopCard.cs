using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>One reusable cosmetic card with a visible-only preview request.</summary>
    public sealed class ShopCard : MonoBehaviour
    {
        private UnityEngine.UI.Text state;
        private UnityEngine.UI.Image coin,check;
        /// <summary>Catalog item shown by this card.</summary>
        public ShopItem Item { get; private set; }
        /// <summary>Card rectangle.</summary>
        public RectTransform Rect=>(RectTransform)transform;
        /// <summary>Cached preview display.</summary>
        public UnityEngine.UI.RawImage Preview { get; private set; }
        /// <summary>Current state text.</summary>
        public string StateText=>state.text;
        /// <summary>Creates the fixed card hierarchy once.</summary>
        public void Build(ShopItem item,UnityEngine.Events.UnityAction click)
        {
            Item=item;var frame=GetComponent<UnityEngine.UI.Image>();frame.color=UiStyle.Category(item.Category);
            UiStyle.SurfaceFor(frame,false,true);
            var face=UiFactory.Panel("Face",transform,UiStyle.Surface);face.offsetMin=Vector2.one*6;face.offsetMax=-Vector2.one*6;
            var button=GetComponent<UnityEngine.UI.Button>();button.onClick.AddListener(click);button.colors=WhiteColors();
            Preview=UiFactory.Rect("Preview",face,Vector2.zero,Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            Preview.raycastTarget=false;Preview.rectTransform.anchorMin=new Vector2(.1f,.36f);Preview.rectTransform.anchorMax=new Vector2(.9f,.92f);Preview.rectTransform.offsetMin=Preview.rectTransform.offsetMax=Vector2.zero;
            var aspect=Preview.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.HeightControlsWidth;aspect.aspectRatio=1;
            var name=UiFactory.Text("Name",face,item.Name,UiStyle.Body,Vector2.zero,Vector2.zero);Anchors(name.rectTransform,.20f,.35f);name.resizeTextForBestFit=true;name.resizeTextMinSize=26;name.resizeTextMaxSize=UiStyle.Body;
            state=UiFactory.Text("State",face,"",UiStyle.Caption,Vector2.zero,Vector2.zero);Anchors(state.rectTransform,.02f,.19f);state.color=UiStyle.Muted;
            coin=UiStyle.Icon("PriceCoin",face,UiSpriteShape.Coin,Vector2.zero,28,UiStyle.Gold);coin.rectTransform.anchorMin=coin.rectTransform.anchorMax=new Vector2(.28f,.105f);
            check=UiStyle.Icon("EquippedCheck",face,UiSpriteShape.Check,Vector2.zero,26,UiStyle.Ink);check.rectTransform.anchorMin=check.rectTransform.anchorMax=new Vector2(.23f,.105f);
            if(item.Category=="Legendary")gameObject.AddComponent<LegendaryShimmer>().Bind(frame,UiStyle.Category(item.Category));
        }
        private static UnityEngine.UI.ColorBlock WhiteColors()
        {var colors=UnityEngine.UI.ColorBlock.defaultColorBlock;colors.normalColor=colors.selectedColor=Color.white;colors.highlightedColor=new Color(.96f,.98f,.98f);colors.pressedColor=new Color(.9f,.94f,.94f);return colors;}
        private static void Anchors(RectTransform rect,float bottom,float top){rect.anchorMin=new Vector2(.03f,bottom);rect.anchorMax=new Vector2(.97f,top);rect.offsetMin=rect.offsetMax=Vector2.zero;}
        /// <summary>Refreshes purchase state outside the frame path.</summary>
        public void Refresh(ShopService shop)
        {
            state.text=!Item.IsAvailable?"Coming soon":shop.IsEquipped(Item.Id)?"Equipped":shop.IsOwned(Item.Id)?"Owned":Item.Price.ToString();
            coin.gameObject.SetActive(Item.IsAvailable&&!shop.IsOwned(Item.Id));check.gameObject.SetActive(shop.IsEquipped(Item.Id));
        }
    }
}
