using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Responsive sectioned shop with two-per-frame, visible-only preview rendering.</summary>
    public sealed class ShopView : MonoBehaviour
    {
        private InventoryService inventory;
        private ArrowSkinResources skinResources;
        private readonly LiveSkinPreview[] elementalCards=new LiveSkinPreview[10];
        private readonly LiveThemePreview[] themeCards=new LiveThemePreview[ThemeDesign.Count];
        private readonly List<ShopCard> cards=new List<ShopCard>(28);
        private readonly List<RectTransform> sections=new List<RectTransform>(7);
        private readonly List<UnityEngine.UI.GridLayoutGroup> grids=new List<UnityEngine.UI.GridLayoutGroup>(7);
        private readonly List<int> sectionCounts=new List<int>(7);
        private readonly Vector3[] corners=new Vector3[4],viewportCorners=new Vector3[4];
        private ArrowPreviewRenderer renderer;
        private CoinCounter counter;
        private UnityEngine.UI.ScrollRect scroll;
        private Vector2 lastSize;
        private ShopSlot tab;
        private GameController game;
        /// <summary>Shop screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Whether shop or its child detail is visible.</summary>
        public bool IsVisible=>Root!=null&&Root.gameObject.activeSelf;
        /// <summary>Back control.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>Arrow tab.</summary>
        public UnityEngine.UI.Button ArrowsButton { get; private set; }
        /// <summary>Theme tab.</summary>
        public UnityEngine.UI.Button ThemesButton { get; private set; }
        /// <summary>Avatar tab.</summary>
        public UnityEngine.UI.Button AvatarsButton { get; private set; }
        /// <summary>Avatar frame tab.</summary>
        public UnityEngine.UI.Button FramesButton { get; private set; }
        /// <summary>Coin pill (also counts the five-tap developer grant).</summary>
        public UnityEngine.UI.Button CoinsButton { get; private set; }
        /// <summary>Coin balance label.</summary>
        public UnityEngine.UI.Text CoinsText { get; private set; }
        /// <summary>The tab currently shown.</summary>
        public ShopSlot Tab=>tab;
        /// <summary>The single reused detail sheet.</summary>
        public ShopDetailSheet Detail { get; private set; }
        /// <summary>Visible-tab cards for tests.</summary>
        public IReadOnlyList<ShopCard> Cards=>cards;
        /// <summary>Notifies menu flow that the shop was closed.</summary>
        public event Action Closed;
        /// <summary>Creates runtime hierarchy without loading previews.</summary>
        public void Build(Transform parent,ConfirmDialog confirm,GameController controller)
        {
            game=controller;skinResources=ArrowSkinResources.Load();Root=UiFactory.ScreenRoot("Shop",parent,BrandArt.Space,out var safe);
            NeonKit.Backdrop(Root,.6f);
            // Same header geometry as the home page, so the coin pill stays under the player's finger.
            var header=UiFactory.Rect("Header",safe,new Vector2(960,150),Vector2.zero);header.anchorMin=header.anchorMax=header.pivot=new Vector2(.5f,1);header.anchoredPosition=new Vector2(0,-16);
            header.gameObject.AddComponent<MenuFit>().Configure(header,safe,new Vector2(1000,170));
            BackButton=NeonKit.IconButton("Back",header,"icon_back",new Vector2(-420,-62));BackButton.onClick.AddListener(HandleBackAction);
            NeonKit.Title("SHOP",header,new Vector2(-90,-62),60,320);
            var coinPill=BrandArt.Image("Coins",header,"ui_pill",new Vector2(330,92),new Vector2(315,-62),NeonKit.PillBorder);coinPill.raycastTarget=true;
            if(coinPill.sprite==null)coinPill.color=new Color(.16f,.11f,.48f,.95f);
            CoinsButton=coinPill.gameObject.AddComponent<UnityEngine.UI.Button>();CoinsButton.targetGraphic=coinPill;coinPill.gameObject.AddComponent<ButtonFeel>();
            var colors=CoinsButton.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(.82f,.85f,.95f);CoinsButton.colors=colors;
            NeonKit.Icon("CoinIcon",coinPill.transform,"icon_coin",66,new Vector2(-118,0));
            CoinsText=BrandArt.Text("CoinsText",coinPill.transform,"0",36,new Vector2(170,60),new Vector2(10,0),Color.white);
            NeonKit.Icon("PlusBadge",coinPill.transform,"icon_plus",50,new Vector2(128,0));
            counter=CoinsText.gameObject.AddComponent<CoinCounter>();counter.Bind(CoinsText);
            for(int i=0;i<header.childCount;i++){var child=(RectTransform)header.GetChild(i);child.anchorMin=child.anchorMax=new Vector2(.5f,1);}
            var tabs=UiFactory.Rect("Tabs",safe,new Vector2(960,110),Vector2.zero);tabs.anchorMin=tabs.anchorMax=tabs.pivot=new Vector2(.5f,1);tabs.anchoredPosition=new Vector2(0,-182);
            tabs.gameObject.AddComponent<MenuFit>().Configure(tabs,safe,new Vector2(1000,120));
            ArrowsButton=MakeTab(tabs,"Arrows","icon_arrow",-360);ThemesButton=MakeTab(tabs,"Themes","icon_theme",-120);AvatarsButton=MakeTab(tabs,"Avatars","icon_avatar",120);FramesButton=MakeTab(tabs,"Frames","icon_frame",360);
            ArrowsButton.onClick.AddListener(ShowArrows);ThemesButton.onClick.AddListener(ShowThemes);AvatarsButton.onClick.AddListener(()=>ShowTab(ShopSlot.Avatar));FramesButton.onClick.AddListener(()=>ShowTab(ShopSlot.Frame));
            scroll=BuildScroll(safe);
            Detail=gameObject.AddComponent<ShopDetailSheet>();Detail.Build(parent,confirm,game);Detail.Changed+=Refresh;
            Root.gameObject.SetActive(false);
        }
        private static UnityEngine.UI.Button MakeTab(Transform parent,string label,string icon,float x)
        {
            var button=UiFactory.Button(label,parent,new Vector2(x,0));((RectTransform)button.transform).sizeDelta=new Vector2(232,104);
            NeonKit.WithIcon(button,icon,46);var text=button.GetComponentInChildren<UnityEngine.UI.Text>();text.resizeTextMaxSize=28;
            return button;
        }
        private void SetTabLooks()
        {
            NeonKit.Style(ArrowsButton,tab==ShopSlot.Arrow?NeonButtonStyle.Primary:NeonButtonStyle.Normal);NeonKit.Style(ThemesButton,tab==ShopSlot.Theme?NeonButtonStyle.Primary:NeonButtonStyle.Normal);
            NeonKit.Style(AvatarsButton,tab==ShopSlot.Avatar?NeonButtonStyle.Primary:NeonButtonStyle.Normal);NeonKit.Style(FramesButton,tab==ShopSlot.Frame?NeonButtonStyle.Primary:NeonButtonStyle.Normal);
        }
        private static UnityEngine.UI.ScrollRect BuildScroll(RectTransform safe)
        {
            var root=UiFactory.Rect("ShopScroll",safe,Vector2.zero,Vector2.zero);UiFactory.Stretch(root);root.offsetMin=new Vector2(24,24);root.offsetMax=new Vector2(-24,-310);
            var view=root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();var viewport=UiFactory.Panel("Viewport",root,new Color(0,0,0,0),true);viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var content=UiFactory.Rect("Content",viewport,Vector2.zero,Vector2.zero);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);
            var layout=content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=32;layout.childControlWidth=true;layout.childControlHeight=false;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            view.viewport=viewport;view.content=content;view.horizontal=false;view.scrollSensitivity=50;return view;
        }
        /// <summary>Rebinds a saved inventory after startup or a development reset.</summary>
        public void Bind(InventoryService service)
        {
            if(inventory!=null){inventory.Shop.CoinsChanged-=OnCoins;inventory.Shop.EquippedChanged-=OnEquipped;}
            inventory=service;inventory.Shop.CoinsChanged+=OnCoins;inventory.Shop.EquippedChanged+=OnEquipped;
            counter.SetValue(inventory.State.Coins,false);if(IsVisible)Rebuild();
        }
        /// <summary>Opens on the Arrow tab with a new screen-lifetime preview cache.</summary>
        public void Show()=>Show(ShopSlot.Arrow);
        /// <summary>Opens on a chosen tab (the profile page opens Avatars).</summary>
        public void Show(ShopSlot slot)
        {
            if(inventory==null)return;Root.gameObject.SetActive(true);Root.SetAsLastSibling();
            renderer?.Dispose();renderer=new ArrowPreviewRenderer();tab=slot;Rebuild();counter.SetValue(inventory.State.Coins,false);
        }
        private void ShowArrows()=>ShowTab(ShopSlot.Arrow);
        private void ShowThemes()=>ShowTab(ShopSlot.Theme);
        /// <summary>Switches the visible tab.</summary>
        public void ShowTab(ShopSlot slot){tab=slot;Rebuild();}
        private void Rebuild()
        {
            for(int n=0;n<elementalCards.Length;n++)if(elementalCards[n]!=null)elementalCards[n].gameObject.SetActive(false);
            for(int n=0;n<themeCards.Length;n++)if(themeCards[n]!=null)themeCards[n].gameObject.SetActive(false);
            Detail.Hide();for(int i=scroll.content.childCount-1;i>=0;i--)DestroyImmediate(scroll.content.GetChild(i).gameObject);
            cards.Clear();sections.Clear();grids.Clear();sectionCounts.Clear();string previous=null;Transform gridParent=null;int section=-1;
            for(int i=0;i<ShopCatalog.Items.Count;i++)
            {
                ShopItem item=ShopCatalog.Items[i];if(item.Slot!=tab)continue;
                if(previous!=item.Category)
                {
                    previous=item.Category;section++;
                    var block=UiFactory.Rect(item.Category,scroll.content,new Vector2(900,400),Vector2.zero);sections.Add(block);sectionCounts.Add(0);
                    var label=BrandArt.Text("Section",block,item.Category.ToUpperInvariant(),38,new Vector2(900,72),Vector2.zero,UiStyle.Category(item.Category));label.alignment=TextAnchor.MiddleLeft;
                    label.rectTransform.anchorMin=new Vector2(0,1);label.rectTransform.anchorMax=new Vector2(1,1);label.rectTransform.pivot=new Vector2(.5f,1);label.rectTransform.sizeDelta=new Vector2(0,72);
                    var grid=UiFactory.Rect("Cards",block,Vector2.zero,Vector2.zero);UiFactory.Stretch(grid);grid.offsetMax=new Vector2(0,-80);
                    var layout=grid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();layout.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;layout.spacing=new Vector2(24,24);grids.Add(layout);gridParent=grid;
                }
                int index=cards.Count;var root=UiFactory.Rect(item.Id,gridParent,new Vector2(360,360),Vector2.zero);root.gameObject.AddComponent<UnityEngine.UI.Image>();root.gameObject.AddComponent<UnityEngine.UI.Button>();root.gameObject.AddComponent<ButtonFeel>();
                var card=root.gameObject.AddComponent<ShopCard>();card.Build(item,()=>OpenDetail(index));card.Refresh(inventory.Shop);card.SetBadgeAvatar(inventory.State.EquippedAvatarId);cards.Add(card);sectionCounts[section]++;
            }
            SetTabLooks();PrepareInfernoCard();PrepareThemeCards();lastSize=Vector2.zero;Canvas.ForceUpdateCanvases();UpdateLayout();scroll.verticalNormalizedPosition=1;
        }
        private void PrepareInfernoCard()
        {
            for(int i=0;i<cards.Count;i++)if(ArrowSkinRegistry.Find(cards[i].Item.Id)?.IsProcedural==true)
            {
                int n=ArrowSkinRegistry.Find(cards[i].Item.Id).Index;
                var infernoCard=elementalCards[n];if(infernoCard==null){var go=new GameObject("Procedural Inferno card");go.transform.SetParent(transform,false);infernoCard=go.AddComponent<LiveSkinPreview>();infernoCard.Initialize(null,new Vector2(22000+n*1000,22000));infernoCard.AutoReplay=false;infernoCard.SetDark(true);infernoCard.ShowStraight(cards[i].Item.Id);infernoCard.SetResolution(400,225);infernoCard.HalfRate=true;elementalCards[n]=infernoCard;}
                cards[i].Preview.texture=infernoCard.Texture;
            }
        }
        private void PrepareThemeCards()
        {
            for(int i=0;i<cards.Count;i++){int n=ThemeDesign.Index(cards[i].Item.Id);if(n<0)continue;
                if(themeCards[n]==null){var go=new GameObject("Live theme card "+n);go.transform.SetParent(transform,false);var preview=go.AddComponent<LiveThemePreview>();preview.Initialize(new Vector2(35000+n*100,35000),320,200);preview.Show(cards[i].Item.Id);preview.HalfRate=true;themeCards[n]=preview;}
                cards[i].Preview.texture=themeCards[n].Texture;cards[i].Preview.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            }
        }
        /// <summary>Opens the chosen card's detail sheet.</summary>
        public void OpenDetail(int index){if(index>=0&&index<cards.Count)Detail.Show(cards[index].Item,inventory);}
        private void OnCoins(int amount){counter.SetValue(amount);Refresh();}
        private void OnEquipped(ShopSlot slot,string id)=>Refresh();
        /// <summary>Refreshes states without recreating the card hierarchy.</summary>
        public void Refresh(){for(int i=0;i<cards.Count;i++){cards[i].Refresh(inventory.Shop);cards[i].SetBadgeAvatar(inventory.State.EquippedAvatarId);}Detail.Refresh();}
        private void HandleBackAction()=>HandleBack();
        /// <summary>Closes the detail first, then the shop; callers consume confirmation modals first.</summary>
        public bool HandleBack(){if(!IsVisible)return false;if(Detail.IsVisible){Detail.Hide();return true;}Close();return true;}
        /// <summary>Closes and destroys all cached previews.</summary>
        public void Close()
        {
            for(int n=0;n<elementalCards.Length;n++)if(elementalCards[n]!=null){DestroyImmediate(elementalCards[n].gameObject);elementalCards[n]=null;}
            for(int n=0;n<themeCards.Length;n++)if(themeCards[n]!=null){DestroyImmediate(themeCards[n].gameObject);themeCards[n]=null;}
            Detail.Hide();Root.gameObject.SetActive(false);renderer?.Dispose();renderer=null;
            for(int i=0;i<cards.Count;i++)if(!cards[i].IsPortrait)cards[i].Preview.texture=null;Closed?.Invoke();
        }
        private void UpdateLayout()
        {
            Vector2 size=scroll.viewport.rect.size;if(size==lastSize||size.x<=0)return;lastSize=size;
            int columns=(size.x<1300?2:size.x<1900?3:4)+(tab==ShopSlot.Avatar||tab==ShopSlot.Frame?1:0);float width=(size.x-(columns-1)*24)/columns;
            for(int i=0;i<sections.Count;i++){grids[i].constraintCount=columns;grids[i].cellSize=new Vector2(width,360);sections[i].sizeDelta=new Vector2(0,80+((sectionCounts[i]+columns-1)/columns)*384);}
        }
        /// <summary>Processes only visible preview requests, capped at two renders per frame.</summary>
        public void Tick(float dt)
        {
            if(!IsVisible||renderer==null)return;UpdateLayout();renderer.BeginFrame();
            scroll.viewport.GetWorldCorners(viewportCorners);
            for(int i=0;i<cards.Count;i++)
            {
                if(cards[i].IsPortrait)continue;
                cards[i].Rect.GetWorldCorners(corners);
                int themeIndex=ThemeDesign.Index(cards[i].Item.Id);if(themeIndex>=0){bool shown=corners[2].y>=viewportCorners[0].y&&corners[0].y<=viewportCorners[2].y;themeCards[themeIndex].gameObject.SetActive(shown);continue;}
                if(ArrowSkinRegistry.Find(cards[i].Item.Id)?.IsProcedural==true)
                {
                    var infernoCard=elementalCards[ArrowSkinRegistry.Find(cards[i].Item.Id).Index];
                    bool shown=corners[2].y>=viewportCorners[0].y&&corners[0].y<=viewportCorners[2].y;
                    if(infernoCard!=null){infernoCard.gameObject.SetActive(shown);cards[i].Preview.texture=infernoCard.Texture;}continue;
                }
                if(corners[2].y>=viewportCorners[0].y&&corners[0].y<=viewportCorners[2].y)renderer.Request(cards[i].Item.Id);
            }
            if(Detail.IsVisible&&!Detail.HasStaticPreview&&ArrowSkinRegistry.Find(Detail.Item.Id)==null)renderer.Request(Detail.Item.Id);
            renderer.Tick();
            for(int i=0;i<cards.Count;i++){if(cards[i].IsPortrait||ArrowSkinRegistry.Find(cards[i].Item.Id)?.IsProcedural==true||ThemeDesign.Index(cards[i].Item.Id)>=0)continue;var art=skinResources?.Find(cards[i].Item.Id);var texture=ArrowSkinRegistry.Find(cards[i].Item.Id)!=null&&art?.Full!=null?art.Full.texture:renderer.GetCached(cards[i].Item.Id);if(cards[i].Preview.texture!=texture)cards[i].Preview.texture=texture;}
            if(Detail.IsVisible&&!Detail.HasLivePreview&&!Detail.HasStaticPreview)Detail.Preview.texture=renderer.GetCached(Detail.Item.Id);
        }
        private void Update(){if(Application.isPlaying)Tick(Time.unscaledDeltaTime);}
        private void OnDestroy(){renderer?.Dispose();if(inventory!=null){inventory.Shop.CoinsChanged-=OnCoins;inventory.Shop.EquippedChanged-=OnEquipped;}}
    }
}
