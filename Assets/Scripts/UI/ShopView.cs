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
        /// <summary>Coin balance label.</summary>
        public UnityEngine.UI.Text CoinsText { get; private set; }
        /// <summary>The single reused detail sheet.</summary>
        public ShopDetailSheet Detail { get; private set; }
        /// <summary>Visible-tab cards for tests.</summary>
        public IReadOnlyList<ShopCard> Cards=>cards;
        /// <summary>Notifies menu flow that the shop was closed.</summary>
        public event Action Closed;
        /// <summary>Creates runtime hierarchy without loading previews.</summary>
        public void Build(Transform parent,ConfirmDialog confirm,GameController controller)
        {
            game=controller;Root=UiFactory.ScreenRoot("Shop",parent,UiStyle.Background,out var safe);
            var header=UiFactory.Rect("Header",safe,new Vector2(940,220),Vector2.zero);header.anchorMin=header.anchorMax=header.pivot=new Vector2(.5f,1);header.anchoredPosition=new Vector2(0,-16);
            header.gameObject.AddComponent<MenuFit>().Configure(header,safe,new Vector2(1000,240));
            UiFactory.Text("Title",header,"Shop",UiStyle.Heading,new Vector2(350,100),new Vector2(0,-50)).fontStyle=FontStyle.Bold;
            BackButton=UiFactory.Button("",header,new Vector2(-400,-50));((RectTransform)BackButton.transform).sizeDelta=new Vector2(112,100);UiStyle.Icon("Back",BackButton.transform,UiSpriteShape.Back,Vector2.zero,46,Color.white);BackButton.onClick.AddListener(HandleBackAction);
            CoinsText=UiFactory.Text("Coins",header,"",UiStyle.Body,new Vector2(220,80),new Vector2(330,-50));UiStyle.Icon("Coin",header,UiSpriteShape.Coin,new Vector2(210,-50),42,UiStyle.Gold);
            counter=CoinsText.gameObject.AddComponent<CoinCounter>();counter.Bind(CoinsText);
            ArrowsButton=UiFactory.Button("Arrows",header,new Vector2(-175,-165));ThemesButton=UiFactory.Button("Themes",header,new Vector2(175,-165));
            ((RectTransform)ArrowsButton.transform).sizeDelta=((RectTransform)ThemesButton.transform).sizeDelta=new Vector2(320,96);
            ArrowsButton.onClick.AddListener(ShowArrows);ThemesButton.onClick.AddListener(ShowThemes);
            for(int i=0;i<header.childCount;i++){var child=(RectTransform)header.GetChild(i);child.anchorMin=child.anchorMax=new Vector2(.5f,1);}
            scroll=BuildScroll(safe);
            Detail=gameObject.AddComponent<ShopDetailSheet>();Detail.Build(parent,confirm,game);Detail.Changed+=Refresh;
            Root.gameObject.SetActive(false);
        }
        private static UnityEngine.UI.ScrollRect BuildScroll(RectTransform safe)
        {
            var root=UiFactory.Rect("ShopScroll",safe,Vector2.zero,Vector2.zero);UiFactory.Stretch(root);root.offsetMin=new Vector2(24,24);root.offsetMax=new Vector2(-24,-270);
            var view=root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();var viewport=UiFactory.Panel("Viewport",root,UiStyle.Background,true);viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
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
        public void Show()
        {
            if(inventory==null)return;Root.gameObject.SetActive(true);Root.SetAsLastSibling();
            renderer?.Dispose();renderer=new ArrowPreviewRenderer();tab=ShopSlot.Arrow;Rebuild();counter.SetValue(inventory.State.Coins,false);
        }
        private void ShowArrows(){tab=ShopSlot.Arrow;Rebuild();}
        private void ShowThemes(){tab=ShopSlot.Theme;Rebuild();}
        private void Rebuild()
        {
            Detail.Hide();for(int i=scroll.content.childCount-1;i>=0;i--)DestroyImmediate(scroll.content.GetChild(i).gameObject);
            cards.Clear();sections.Clear();grids.Clear();sectionCounts.Clear();string previous=null;Transform gridParent=null;int section=-1;
            for(int i=0;i<ShopCatalog.Items.Count;i++)
            {
                ShopItem item=ShopCatalog.Items[i];if(item.Slot!=tab)continue;
                if(previous!=item.Category)
                {
                    previous=item.Category;section++;
                    var block=UiFactory.Rect(item.Category,scroll.content,new Vector2(900,400),Vector2.zero);sections.Add(block);sectionCounts.Add(0);
                    var label=UiFactory.Text("Section",block,item.Category,UiStyle.Section,new Vector2(900,72),Vector2.zero);label.alignment=TextAnchor.MiddleLeft;
                    label.rectTransform.anchorMin=new Vector2(0,1);label.rectTransform.anchorMax=new Vector2(1,1);label.rectTransform.pivot=new Vector2(.5f,1);label.rectTransform.sizeDelta=new Vector2(0,72);
                    var grid=UiFactory.Rect("Cards",block,Vector2.zero,Vector2.zero);UiFactory.Stretch(grid);grid.offsetMax=new Vector2(0,-80);
                    var layout=grid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();layout.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;layout.spacing=new Vector2(24,24);grids.Add(layout);gridParent=grid;
                }
                int index=cards.Count;var root=UiFactory.Rect(item.Id,gridParent,new Vector2(360,360),Vector2.zero);root.gameObject.AddComponent<UnityEngine.UI.Image>();root.gameObject.AddComponent<UnityEngine.UI.Button>();root.gameObject.AddComponent<ButtonFeel>();
                var card=root.gameObject.AddComponent<ShopCard>();card.Build(item,()=>OpenDetail(index));card.Refresh(inventory.Shop);cards.Add(card);sectionCounts[section]++;
            }
            lastSize=Vector2.zero;Canvas.ForceUpdateCanvases();UpdateLayout();scroll.verticalNormalizedPosition=1;
        }
        /// <summary>Opens the chosen card's detail sheet.</summary>
        public void OpenDetail(int index){if(index>=0&&index<cards.Count)Detail.Show(cards[index].Item,inventory);}
        private void OnCoins(int amount){counter.SetValue(amount);Refresh();}
        private void OnEquipped(ShopSlot slot,string id)=>Refresh();
        /// <summary>Refreshes states without recreating the card hierarchy.</summary>
        public void Refresh(){for(int i=0;i<cards.Count;i++)cards[i].Refresh(inventory.Shop);Detail.Refresh();}
        private void HandleBackAction()=>HandleBack();
        /// <summary>Closes the detail first, then the shop; callers consume confirmation modals first.</summary>
        public bool HandleBack(){if(!IsVisible)return false;if(Detail.IsVisible){Detail.Hide();return true;}Close();return true;}
        /// <summary>Closes and destroys all cached previews.</summary>
        public void Close()
        {
            Detail.Hide();Root.gameObject.SetActive(false);renderer?.Dispose();renderer=null;
            for(int i=0;i<cards.Count;i++)cards[i].Preview.texture=null;Closed?.Invoke();
        }
        private void UpdateLayout()
        {
            Vector2 size=scroll.viewport.rect.size;if(size==lastSize||size.x<=0)return;lastSize=size;
            int columns=size.x<1300?2:size.x<1900?3:4;float width=(size.x-(columns-1)*24)/columns;
            for(int i=0;i<sections.Count;i++){grids[i].constraintCount=columns;grids[i].cellSize=new Vector2(width,360);sections[i].sizeDelta=new Vector2(0,80+((sectionCounts[i]+columns-1)/columns)*384);}
        }
        /// <summary>Processes only visible preview requests, capped at two renders per frame.</summary>
        public void Tick(float dt)
        {
            if(!IsVisible||renderer==null)return;UpdateLayout();renderer.BeginFrame();
            scroll.viewport.GetWorldCorners(viewportCorners);
            for(int i=0;i<cards.Count;i++)
            {
                cards[i].Rect.GetWorldCorners(corners);
                if(corners[2].y>=viewportCorners[0].y&&corners[0].y<=viewportCorners[2].y)renderer.Request(cards[i].Item.Id);
            }
            if(Detail.IsVisible)renderer.Request(Detail.Item.Id);
            renderer.Tick();
            for(int i=0;i<cards.Count;i++){var texture=renderer.GetCached(cards[i].Item.Id);if(cards[i].Preview.texture!=texture)cards[i].Preview.texture=texture;}
            if(Detail.IsVisible)Detail.Preview.texture=renderer.GetCached(Detail.Item.Id);
        }
        private void Update(){if(Application.isPlaying)Tick(Time.unscaledDeltaTime);}
        private void OnDestroy(){renderer?.Dispose();if(inventory!=null){inventory.Shop.CoinsChanged-=OnCoins;inventory.Shop.EquippedChanged-=OnEquipped;}}
    }
}
