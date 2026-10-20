using System;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>One detail sheet reused across all items; confirmation is a separate modal.</summary>
    public sealed class ShopDetailSheet : MonoBehaviour
    {
        private InventoryService inventory;
        private ConfirmDialog confirm;
        private GameController game;
        private UnityEngine.UI.Text nameText,category,description,mainLabel;
        /// <summary>Full-screen blocking root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Whether a detail sheet is open.</summary>
        public bool IsVisible=>Root!=null&&Root.gameObject.activeSelf;
        /// <summary>The selected item.</summary>
        public ShopItem Item { get; private set; }
        /// <summary>Large cached preview.</summary>
        public UnityEngine.UI.RawImage Preview { get; private set; }
        /// <summary>The single purchase/equip action.</summary>
        public UnityEngine.UI.Button MainButton { get; private set; }
        /// <summary>Back action closes only the sheet.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>Observable button label for tests.</summary>
        public string ActionText=>mainLabel.text;
        /// <summary>Raised after a successful purchase or equipment change.</summary>
        public event Action Changed;
        /// <summary>Creates the initially hidden detail sheet.</summary>
        public void Build(Transform parent,ConfirmDialog dialog,GameController controller)
        {
            confirm=dialog;game=controller;
            Root=UiFactory.ScreenRoot("ShopDetail",parent,ViewTheme.PanelDim,out var safe);
            var card=UiFactory.Column(safe,900,1300);var surface=card.gameObject.AddComponent<UnityEngine.UI.Image>();surface.color=UiStyle.Surface;UiStyle.SurfaceFor(surface,true,true);
            BackButton=UiFactory.Button("",card,new Vector2(-340,550));((RectTransform)BackButton.transform).sizeDelta=new Vector2(112,112);UiStyle.Icon("Back",BackButton.transform,UiSpriteShape.Back,Vector2.zero,48,Color.white);BackButton.onClick.AddListener(Hide);
            nameText=UiFactory.Text("Name",card,"",56,new Vector2(820,110),new Vector2(0,425));nameText.fontStyle=FontStyle.Bold;
            category=UiFactory.Text("Category",card,"",UiStyle.Body,new Vector2(820,64),new Vector2(0,330));category.color=UiStyle.Muted;
            Preview=UiFactory.Rect("LargePreview",card,new Vector2(400,320),new Vector2(0,110)).gameObject.AddComponent<UnityEngine.UI.RawImage>();Preview.raycastTarget=false;
            description=UiFactory.Text("Description",card,"",UiStyle.Body,new Vector2(780,180),new Vector2(0,-175));
            var note=UiFactory.Text("PreviewNote",card,"Color preview - full effects coming later",UiStyle.Caption,new Vector2(780,70),new Vector2(0,-315));note.color=UiStyle.Muted;
            MainButton=UiFactory.Button("",card,new Vector2(0,-475));((RectTransform)MainButton.transform).sizeDelta=new Vector2(700,128);mainLabel=MainButton.GetComponentInChildren<UnityEngine.UI.Text>();MainButton.onClick.AddListener(OnMain);Hide();
        }
        /// <summary>Shows catalog text; preview rendering remains lazy.</summary>
        public void Show(ShopItem item,InventoryService service)
        {Item=item;inventory=service;nameText.text=item.Name;category.text=item.Category;description.text=item.Description;Preview.texture=null;Root.gameObject.SetActive(true);Root.SetAsLastSibling();Refresh();}
        /// <summary>Reflects exact ownership and affordability state.</summary>
        public void Refresh()
        {
            if(Item==null||inventory==null)return;var shop=inventory.Shop;
            string label=!Item.IsAvailable?"Coming soon":shop.IsEquipped(Item.Id)?"Equipped":shop.IsOwned(Item.Id)?"Equip":shop.CanAfford(Item.Price)?"Buy - "+Item.Price:"Need "+(Item.Price-inventory.State.Coins)+" more";
            mainLabel.text=label;MainButton.interactable=Item.IsAvailable&&!shop.IsEquipped(Item.Id)&&(shop.IsOwned(Item.Id)||shop.CanAfford(Item.Price));
        }
        private void OnMain()
        {
            if(!MainButton.interactable||Item==null)return;
            if(inventory.Shop.IsOwned(Item.Id)){if(inventory.TryEquip(Item.Id)){Changed?.Invoke();Refresh();}return;}
            confirm.Show("Confirm purchase","Buy "+Item.Name+" for "+Item.Price+" coins?","Buy - "+Item.Price,Buy);
        }
        private void Buy()
        {if(Item==null||!inventory.TryBuy(Item.Id))return;game.RequestUiFeedback(UiFeedbackCue.Purchase);Changed?.Invoke();Refresh();}
        /// <summary>Closes the sheet before leaving its shop.</summary>
        public void Hide(){if(Root!=null)Root.gameObject.SetActive(false);}
    }
}
