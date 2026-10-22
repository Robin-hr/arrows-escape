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
            var viewport=UiFactory.Panel("PreviewArea",face,new Color32(18,22,28,255));
            viewport.anchorMin=new Vector2(.06f,.38f);viewport.anchorMax=new Vector2(.94f,.94f);viewport.offsetMin=viewport.offsetMax=Vector2.zero;
            UiStyle.SurfaceFor(viewport.GetComponent<UnityEngine.UI.Image>(),false,true);viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=true;
            Preview=UiFactory.Rect("Preview",viewport,Vector2.zero,Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            Preview.raycastTarget=false;Preview.rectTransform.anchorMin=Vector2.zero;Preview.rectTransform.anchorMax=Vector2.one;Preview.rectTransform.offsetMin=Preview.rectTransform.offsetMax=Vector2.zero;
            var aspect=Preview.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            var resource=ArrowSkinResources.Load();var art=resource==null?null:resource.Find(item.Id);
            if(ArrowSkinRegistry.Find(item.Id)?.IsProcedural==true||(ArrowSkinRegistry.Find(item.Id)!=null&&art!=null&&art.Full!=null))
            {
                var display=UiFactory.Panel("Dark display case",face,new Color32(18,22,28,255));display.GetComponent<UnityEngine.UI.Image>().sprite=SkinCardPulse.CaseSprite;display.GetComponent<UnityEngine.UI.Image>().color=Color.white;display.SetSiblingIndex(0);display.anchorMin=new Vector2(.06f,.36f);display.anchorMax=new Vector2(.94f,.94f);display.offsetMin=display.offsetMax=Vector2.zero;
                aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=ArrowSkinRegistry.Find(item.Id)?.IsProcedural==true?640f/360:art.Full.rect.width/art.Full.rect.height;
                if(ArrowSkinRegistry.Find(item.Id)?.IsProcedural!=true){Preview.texture=art.Full.texture;Preview.gameObject.AddComponent<SkinCardPulse>().Bind(Preview);}
            }
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
