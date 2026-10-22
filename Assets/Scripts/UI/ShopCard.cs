using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>One reusable neon cosmetic card with a visible-only preview request.</summary>
    public sealed class ShopCard : MonoBehaviour
    {
        private UnityEngine.UI.Text state;
        private UnityEngine.UI.Image coin,check;
        private AvatarBadge badge;
        /// <summary>Catalog item shown by this card.</summary>
        public ShopItem Item { get; private set; }
        /// <summary>Card rectangle.</summary>
        public RectTransform Rect=>(RectTransform)transform;
        /// <summary>Cached preview display.</summary>
        public UnityEngine.UI.RawImage Preview { get; private set; }
        /// <summary>Avatar and frame cards show static portrait art instead of rendered previews.</summary>
        public bool IsPortrait=>Item!=null&&(Item.Slot==ShopSlot.Avatar||Item.Slot==ShopSlot.Frame);
        /// <summary>The portrait preview for avatar and frame cards, otherwise null.</summary>
        public AvatarBadge Badge=>badge;
        /// <summary>Current state text.</summary>
        public string StateText=>state.text;
        /// <summary>Creates the fixed card hierarchy once.</summary>
        public void Build(ShopItem item,UnityEngine.Events.UnityAction click)
        {
            Item=item;var frame=GetComponent<UnityEngine.UI.Image>();Color tint=UiStyle.Category(item.Category);
            var rim=NeonKit.Panel(frame,tint);
            var face=UiFactory.Rect("Face",transform,Vector2.zero,Vector2.zero);UiFactory.Stretch(face);face.offsetMin=Vector2.one*14;face.offsetMax=-Vector2.one*14;
            var button=GetComponent<UnityEngine.UI.Button>();button.onClick.AddListener(click);button.colors=WhiteColors();
            var viewport=UiFactory.Panel("PreviewArea",face,new Color32(8,7,34,255));
            viewport.anchorMin=new Vector2(.06f,.36f);viewport.anchorMax=new Vector2(.94f,.95f);viewport.offsetMin=viewport.offsetMax=Vector2.zero;
            UiStyle.SurfaceFor(viewport.GetComponent<UnityEngine.UI.Image>(),false,true);viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=true;
            Preview=UiFactory.Rect("Preview",viewport,Vector2.zero,Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            Preview.raycastTarget=false;Preview.rectTransform.anchorMin=Vector2.zero;Preview.rectTransform.anchorMax=Vector2.one;Preview.rectTransform.offsetMin=Preview.rectTransform.offsetMax=Vector2.zero;
            var aspect=Preview.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            if(IsPortrait)
            {
                Preview.enabled=false;
                badge=AvatarBadge.Create("Portrait",Preview.transform,10,Vector2.zero);var b=(RectTransform)badge.transform;b.anchorMin=new Vector2(.04f,.04f);b.anchorMax=new Vector2(.96f,.96f);b.offsetMin=b.offsetMax=Vector2.zero;
                if(item.Slot==ShopSlot.Avatar){badge.Set(item.Id,null);var a=badge.AvatarImage.rectTransform;a.anchorMin=new Vector2(.08f,.08f);a.anchorMax=new Vector2(.92f,.92f);}
                else badge.Set(Inventory.DefaultAvatarId,item.Id);
            }
            var resource=IsPortrait?null:ArrowSkinResources.Load();var art=resource==null?null:resource.Find(item.Id);
            // Plain colour previews are drawn for the paper board; give them a paper well so dark inks stay visible.
            if(item.Slot==ShopSlot.Arrow&&ArrowSkinRegistry.Find(item.Id)==null)viewport.GetComponent<UnityEngine.UI.Image>().color=ViewTheme.Background;
            if(!IsPortrait&&(ArrowSkinRegistry.Find(item.Id)?.IsProcedural==true||(ArrowSkinRegistry.Find(item.Id)!=null&&art!=null&&art.Full!=null)))
            {
                var display=UiFactory.Panel("Dark display case",face,new Color32(18,22,28,255));display.GetComponent<UnityEngine.UI.Image>().sprite=SkinCardPulse.CaseSprite;display.GetComponent<UnityEngine.UI.Image>().color=Color.white;display.SetSiblingIndex(0);display.anchorMin=new Vector2(.06f,.36f);display.anchorMax=new Vector2(.94f,.95f);display.offsetMin=display.offsetMax=Vector2.zero;
                aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=ArrowSkinRegistry.Find(item.Id)?.IsProcedural==true?640f/360:art.Full.rect.width/art.Full.rect.height;
                if(ArrowSkinRegistry.Find(item.Id)?.IsProcedural!=true){Preview.texture=art.Full.texture;Preview.gameObject.AddComponent<SkinCardPulse>().Bind(Preview);}
            }
            var name=UiFactory.Text("Name",face,item.Name,UiStyle.Body,Vector2.zero,Vector2.zero);Anchors(name.rectTransform,.19f,.35f);name.resizeTextForBestFit=true;name.resizeTextMinSize=22;name.resizeTextMaxSize=UiStyle.Body;name.color=NeonKit.Text;
            state=UiFactory.Text("State",face,"",UiStyle.Caption,Vector2.zero,Vector2.zero);Anchors(state.rectTransform,.02f,.18f);state.color=UiStyle.Muted;
            coin=UiStyle.Icon("PriceCoin",face,UiSpriteShape.Coin,Vector2.zero,30,UiStyle.Gold);coin.rectTransform.anchorMin=coin.rectTransform.anchorMax=new Vector2(.28f,.1f);
            check=UiStyle.Icon("EquippedCheck",face,UiSpriteShape.Check,Vector2.zero,28,new Color32(125,255,176,255));check.rectTransform.anchorMin=check.rectTransform.anchorMax=new Vector2(.22f,.1f);
            if(item.Category=="Legendary"||item.Category=="Mythic Frames")gameObject.AddComponent<LegendaryShimmer>().Bind(rim,tint);
        }
        /// <summary>Frame cards preview the frame around the player's current avatar.</summary>
        public void SetBadgeAvatar(string avatarId)
        {
            if(badge==null||Item.Slot!=ShopSlot.Frame||badge.AvatarId==avatarId)return;badge.Set(avatarId,Item.Id);
        }
        private static UnityEngine.UI.ColorBlock WhiteColors()
        {var colors=UnityEngine.UI.ColorBlock.defaultColorBlock;colors.normalColor=colors.selectedColor=Color.white;colors.highlightedColor=new Color(.96f,.98f,1f);colors.pressedColor=new Color(.8f,.82f,.95f);return colors;}
        private static void Anchors(RectTransform rect,float bottom,float top){rect.anchorMin=new Vector2(.03f,bottom);rect.anchorMax=new Vector2(.97f,top);rect.offsetMin=rect.offsetMax=Vector2.zero;}
        /// <summary>Refreshes purchase state outside the frame path.</summary>
        public void Refresh(ShopService shop)
        {
            state.text=!Item.IsAvailable?"Coming soon":shop.IsEquipped(Item.Id)?"Equipped":shop.IsOwned(Item.Id)?"Owned":Item.Price.ToString();
            state.color=shop.IsEquipped(Item.Id)?new Color32(125,255,176,255):shop.IsOwned(Item.Id)?NeonKit.Text:new Color32(255,214,92,255);
            coin.gameObject.SetActive(Item.IsAvailable&&!shop.IsOwned(Item.Id));check.gameObject.SetActive(shop.IsEquipped(Item.Id));
        }
    }
}
