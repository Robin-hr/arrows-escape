using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Paper main menu with a compact action column, economy header and prepared motion.</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        private PaperMenuBackground background;
        private ThemeMenuBackground themeBackground;
        private bool hasTheme;
        private MenuEntrance entrance;
        private CoinCounter coins;
        /// <summary>Menu screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Continue action.</summary>
        public UnityEngine.UI.Button PlayButton { get; private set; }
        /// <summary>Level selection action.</summary>
        public UnityEngine.UI.Button LevelsButton { get; private set; }
        /// <summary>Cosmetic shop action.</summary>
        public UnityEngine.UI.Button ShopButton { get; private set; }
        /// <summary>Preferences action.</summary>
        public UnityEngine.UI.Button SettingsButton { get; private set; }
        /// <summary>Retained platform quit API, hidden from the four-action menu.</summary>
        public UnityEngine.UI.Button QuitButton { get; private set; }
        /// <summary>Existing completion summary.</summary>
        public UnityEngine.UI.Text CompletedText { get; private set; }
        /// <summary>Retained continue subtitle API; the visible target is in the Play button.</summary>
        public UnityEngine.UI.Text ContinueText { get; private set; }
        /// <summary>Coin header opens the shop.</summary>
        public UnityEngine.UI.Button CoinsButton { get; private set; }
        /// <summary>Visible coin balance.</summary>
        public UnityEngine.UI.Text CoinsText { get; private set; }
        /// <summary>Best stars out of the catalog's available stars.</summary>
        public UnityEngine.UI.Text StarsText { get; private set; }
        /// <summary>Background replacement seam for Step 14E.</summary>
        public IMenuBackground Background=>hasTheme?(IMenuBackground)themeBackground:background;
        /// <summary>Builds the runtime screen once.</summary>
        public void Build(Transform parent)
        {
            if(Root!=null)return;
            Root=UiFactory.ScreenRoot("MainMenu",parent,UiStyle.Background,out var safe);
            var bg=UiFactory.Rect("MenuBackground",Root,Vector2.zero,Vector2.zero);UiFactory.Stretch(bg);bg.SetAsFirstSibling();
            background=bg.gameObject.AddComponent<PaperMenuBackground>();background.Build(bg);themeBackground=bg.gameObject.AddComponent<ThemeMenuBackground>();themeBackground.Build(bg);
            var header=UiFactory.Rect("Header",safe,new Vector2(900,112),Vector2.zero);
            header.anchorMin=header.anchorMax=header.pivot=new Vector2(.5f,1);header.anchoredPosition=new Vector2(0,-16);
            header.gameObject.AddComponent<MenuFit>().Configure(header,safe,new Vector2(960,150));
            StarsText=UiFactory.Text("TotalStars",header,"",UiStyle.Body,new Vector2(280,80),new Vector2(-270,-50));
            StarsText.alignment=TextAnchor.MiddleLeft;
            var star=UiFactory.Rect("StarIcon",header,new Vector2(44,44),new Vector2(-438,-50)).gameObject.AddComponent<UnityEngine.UI.Image>();star.sprite=SpriteFactory.GetStarSprite();star.color=UiStyle.Gold;star.raycastTarget=false;
            CoinsButton=UiFactory.Button("0",header,new Vector2(300,-50));((RectTransform)CoinsButton.transform).sizeDelta=new Vector2(280,88);
            CoinsText=CoinsButton.GetComponentInChildren<UnityEngine.UI.Text>();CoinsText.fontSize=UiStyle.Body;
            UiStyle.Icon("Coin",CoinsButton.transform,UiSpriteShape.Coin,new Vector2(-88,0),44,UiStyle.Gold);
            CoinsText.rectTransform.offsetMin=new Vector2(46,0);
            coins=CoinsButton.gameObject.AddComponent<CoinCounter>();coins.Bind(CoinsText);
            for(int i=0;i<header.childCount;i++){var child=(RectTransform)header.GetChild(i);child.anchorMin=child.anchorMax=new Vector2(.5f,1);}
            var area=UiFactory.Rect("MenuContent",safe,Vector2.zero,Vector2.zero);UiFactory.Stretch(area);area.offsetMax=new Vector2(0,-180);
            var column=UiFactory.Column(area,760,1100);
            var title=UiFactory.Text("Title",column,"ARROWS ESCAPE",72,new Vector2(740,120),new Vector2(0,420));title.fontStyle=FontStyle.Bold;
            var underline=UiFactory.Rect("TitlePath",column,new Vector2(210,96),new Vector2(0,316));
            underline.gameObject.AddComponent<PathPreviewGraphic>().Configure(UiStyle.Ink,UiStyle.Muted);
            PlayButton=Action("Play",column,160);LevelsButton=Action("Levels",column,-8);ShopButton=Action("Shop",column,-168);SettingsButton=Action("Settings",column,-328);
            UiStyle.Icon("Grid",LevelsButton.transform,UiSpriteShape.Grid,new Vector2(-190,0),38,Color.white);
            UiStyle.Icon("Cart",ShopButton.transform,UiSpriteShape.Cart,new Vector2(-190,0),42,Color.white);
            UiStyle.Icon("Gear",SettingsButton.transform,UiSpriteShape.Gear,new Vector2(-190,0),42,Color.white);
            CompletedText=UiFactory.Text("Completed",column,"",UiStyle.Caption,new Vector2(700,64),new Vector2(0,-455));CompletedText.color=UiStyle.Muted;
            ContinueText=UiFactory.Text("ContinueLevel",column,"",UiStyle.Caption,new Vector2(500,48),Vector2.zero);ContinueText.gameObject.SetActive(false);
            QuitButton=UiFactory.MenuButton("Quit",column,-520);QuitButton.gameObject.SetActive(false);
            entrance=column.gameObject.AddComponent<MenuEntrance>();entrance.Configure(new[]{title.rectTransform,underline,(RectTransform)PlayButton.transform,(RectTransform)LevelsButton.transform,(RectTransform)ShopButton.transform,(RectTransform)SettingsButton.transform,CompletedText.rectTransform},underline);
        }
        private static UnityEngine.UI.Button Action(string label,Transform parent,float y)
        {var button=UiFactory.MenuButton(label,parent,y);((RectTransform)button.transform).sizeDelta=new Vector2(520,128);button.GetComponentInChildren<UnityEngine.UI.Text>().fontSize=UiStyle.Section;return button;}
        /// <summary>Refreshes progress on entering, preserving the existing ContinueIndex navigation.</summary>
        public void Refresh(bool hasProgress,int completed,int total,int continueIndex)
        {
            PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text=hasProgress?"Continue - Level "+(continueIndex+1):"Play";
            PlayButton.interactable=total>0;ContinueText.text=hasProgress&&total>0?"Level "+(continueIndex+1):"";
            CompletedText.text=$"Completed {completed} / {total}";
            entrance.Restart();background.Restart();
        }
        /// <summary>Applies the equipped live theme while retaining Paper's exact presentation.</summary>
        public void SetTheme(string id)
        {themeBackground.Show(id);bool dark=ThemeDesign.Index(id)>=0;hasTheme=dark;Color ink=dark?Color.white:UiStyle.Ink;StarsText.color=ink;CompletedText.color=dark?new Color(.8f,.85f,.88f):UiStyle.Muted;var texts=Root.GetComponentsInChildren<UnityEngine.UI.Text>(true);for(int i=0;i<texts.Length;i++)if(texts[i].name=="Title")texts[i].color=ink;}
        /// <summary>Updates the safe-area economy counters without restarting entrance motion.</summary>
        public void SetEconomy(int balance,int totalStars,int maximumStars,bool animate=true)
        {if(coins.Value!=balance||!animate)coins.SetValue(balance,animate);string text=totalStars+" / "+maximumStars;if(StarsText.text!=text)StarsText.text=text;}
        /// <summary>Explicit steady-state animation driver for tests and profiling.</summary>
        public void Tick(float dt){if(Root==null||!Root.gameObject.activeInHierarchy)return;background.Tick(dt);themeBackground.Tick(dt);entrance.Tick(dt);}
        private void Update(){if(Application.isPlaying)Tick(Time.unscaledDeltaTime);}
    }
}
