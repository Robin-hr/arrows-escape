using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>"Out of hints/undos" offer: watch an ad for one, or buy one for coins.</summary>
    public sealed class BoostOfferView : MonoBehaviour
    {
        private Action watch, buy;
        private UnityEngine.UI.Image icon;
        /// <summary>Overlay root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>True while open.</summary>
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>Which helper is offered.</summary>
        public BoostKind Kind { get; private set; }
        /// <summary>Heading.</summary>
        public UnityEngine.UI.Text TitleText { get; private set; }
        /// <summary>Watch-ad action.</summary>
        public UnityEngine.UI.Button AdButton { get; private set; }
        /// <summary>Buy-for-coins action.</summary>
        public UnityEngine.UI.Button BuyButton { get; private set; }
        /// <summary>Closes the offer.</summary>
        public UnityEngine.UI.Button CloseButton { get; private set; }
        /// <summary>Builds the hidden offer.</summary>
        public void Build(Transform parent)
        {
            Root = UiFactory.ScreenRoot("BoostOffer", parent, ViewTheme.PanelDim, out var safe);
            var card = UiFactory.Column(safe, 860, 820);
            NeonKit.Panel(card.gameObject.AddComponent<UnityEngine.UI.Image>(), NeonKit.Cyan);
            TitleText = BrandArt.Text("Title", card, "OUT OF HINTS", 54, new Vector2(760, 90), new Vector2(0, 310), Color.white);
            icon = NeonKit.Icon("Icon", card, "icon_hint", 170, new Vector2(0, 150));
            BrandArt.Text("Note", card, "Get one more to keep going", 30, new Vector2(760, 60), new Vector2(0, 30), BrandArt.Lavender);
            AdButton = UiFactory.Button("WATCH AD  +1", card, new Vector2(0, -90), NeonButtonStyle.Primary);
            ((RectTransform)AdButton.transform).sizeDelta = new Vector2(620, 140); NeonKit.WithIcon(AdButton, "Daily/icon_ad", 70);
            BuyButton = UiFactory.Button("BUY 1 · " + EconomyConfig.BoostPrice, card, new Vector2(0, -250));
            ((RectTransform)BuyButton.transform).sizeDelta = new Vector2(620, 140); NeonKit.WithIcon(BuyButton, "icon_coin", 66);
            CloseButton = NeonKit.IconButton("Close", card, "Daily/icon_close", new Vector2(370, 350), 96, 52);
            AdButton.onClick.AddListener(() => { if (!IsOpen) return; var a = watch; Hide(); a?.Invoke(); });
            BuyButton.onClick.AddListener(() => { if (!IsOpen) return; var b = buy; Hide(); b?.Invoke(); });
            CloseButton.onClick.AddListener(Hide);
            Root.gameObject.SetActive(false);
        }
        /// <summary>Opens the offer.</summary>
        public void Show(BoostKind kind, int coins, Action onWatch, Action onBuy)
        {
            Kind = kind; watch = onWatch; buy = onBuy;
            TitleText.text = kind == BoostKind.Hint ? "OUT OF HINTS" : "OUT OF UNDOS";
            icon.sprite = BrandArt.SpriteOf(kind == BoostKind.Hint ? "icon_hint" : "icon_undo");
            bool afford = coins >= EconomyConfig.BoostPrice;
            BuyButton.interactable = afford;
            BuyButton.GetComponentInChildren<UnityEngine.UI.Text>().text = afford ? "BUY 1 · " + EconomyConfig.BoostPrice : "NEED " + (EconomyConfig.BoostPrice - coins) + " MORE";
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }
        /// <summary>Closes without choosing.</summary>
        public void Hide() { watch = buy = null; if (Root != null) Root.gameObject.SetActive(false); }
    }
}
