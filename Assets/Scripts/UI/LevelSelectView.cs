using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Reusable scroll-grid tiles; large catalogs remain navigable by wheel or touch drag.</summary>
    public sealed class LevelSelectView : MonoBehaviour
    {
        private readonly List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.RawImage> thumbnails=new List<UnityEngine.UI.RawImage>();
        private readonly LevelThumbnailRenderer renderer=new LevelThumbnailRenderer(new Color32(14,12,52,255),new Color32(120,236,255,255));
        private readonly List<UnityEngine.UI.Image> rims=new List<UnityEngine.UI.Image>();
        private readonly List<GameObject> locks=new List<GameObject>();
        private readonly Vector3[] corners=new Vector3[4],viewportCorners=new Vector3[4];
        private readonly List<StarDisplay> stars = new List<StarDisplay>();
        private bool wasVisible;

        private IReadOnlyList<LevelTile> tiles;
        private Action<int> start;
        private UnityEngine.UI.GridLayoutGroup grid;
        private Vector2 lastSize = new Vector2(-1, -1);
        /// <summary>Screen root.</summary>
        public RectTransform Root { get; private set; }
        /// <summary>Back action.</summary>
        public UnityEngine.UI.Button BackButton { get; private set; }
        /// <summary>Vertical grid scrolling.</summary>
        public UnityEngine.UI.ScrollRect Scroll { get; private set; }
        /// <summary>Current tile buttons, in level order.</summary>
        public IReadOnlyList<UnityEngine.UI.Button> TileButtons => buttons;
        /// <summary>Developer option: three quick taps on a locked tile open it for testing (null = off).</summary>
        public Action<int> DeveloperOpen { get; set; }
        /// <summary>Clock for the developer triple tap; tests may replace it.</summary>
        public Func<float> Clock { get; set; } = () => Time.realtimeSinceStartup;
        private readonly TapStreak lockedTaps = new TapStreak(3, 0.6f);
        private int lockedTile = -1;
        /// <summary>Counts a tap on a locked tile; the third quick tap on the same tile opens it.</summary>
        public void TapLocked(int index)
        {
            if (DeveloperOpen == null || tiles == null || index < 0 || index >= tiles.Count || tiles[index].State != LevelTileState.Locked) return;
            if (index != lockedTile) { lockedTaps.Reset(); lockedTile = index; }
            if (lockedTaps.Tap(Clock())) { lockedTile = -1; DeveloperOpen(tiles[index].Index); }
        }
        /// <summary>Builds the header, viewport, mask, and grid.</summary>
        public void Build(Transform parent, Action<int> onStart, Action onBack)
        {
            start = onStart;
            Root = UiFactory.ScreenRoot("LevelSelect", parent, BrandArt.Space, out var safe);
            NeonKit.Backdrop(Root, 0.5f);
            var header = UiFactory.Rect("Header", safe, new Vector2(900, 140), Vector2.zero);
            header.anchorMin = header.anchorMax = header.pivot = new Vector2(0.5f, 1);
            header.anchoredPosition = new Vector2(0, -20);
            header.gameObject.AddComponent<MenuFit>().Configure(header, safe, new Vector2(1000, 170));
            NeonKit.Title("LEVELS", header, Vector2.zero, 64);
            BackButton = NeonKit.IconButton("Back", header, "icon_back", new Vector2(-390, 0));
            BackButton.onClick.AddListener(() => onBack());
            Scroll = UiFactory.ScrollGrid(safe); grid = Scroll.content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        }
        /// <summary>Reuses all tile objects when count is unchanged; callbacks validate the current tile state.</summary>
        public void Refresh(IReadOnlyList<LevelTile> value)
        {
            tiles = value ?? Array.Empty<LevelTile>();
            while (buttons.Count > tiles.Count)
            { int i = buttons.Count - 1; DestroyImmediate(buttons[i].gameObject); buttons.RemoveAt(i); thumbnails.RemoveAt(i); stars.RemoveAt(i); rims.RemoveAt(i); locks.RemoveAt(i); }
            while (buttons.Count < tiles.Count)
            {
                int index = buttons.Count;
                var button = UiFactory.Button("Level " + (index + 1), Scroll.content, Vector2.zero);
                button.onClick.AddListener(() => { if (index < tiles.Count && tiles[index].State != LevelTileState.Locked) start(tiles[index].Index); });
                button.gameObject.AddComponent<LockedTileTap>().Tapped += () => { if (!button.interactable) TapLocked(index); };
                var label=button.GetComponentInChildren<UnityEngine.UI.Text>();
                label.resizeTextForBestFit=false;label.supportRichText=true;label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;label.lineSpacing=.82f;
                label.rectTransform.anchorMin=new Vector2(0,.07f);label.rectTransform.anchorMax=new Vector2(1,.33f);
                label.rectTransform.offsetMin=new Vector2(6,0);label.rectTransform.offsetMax=new Vector2(-6,0);label.fontSize=34;
                var well=UiFactory.Rect("ThumbWell",button.transform,Vector2.zero,Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
                well.raycastTarget=false;well.color=new Color32(10,8,40,230);well.rectTransform.anchorMin=new Vector2(.12f,.48f);well.rectTransform.anchorMax=new Vector2(.88f,.92f);well.rectTransform.offsetMin=well.rectTransform.offsetMax=Vector2.zero;
                var image=UiFactory.Rect("Thumbnail",button.transform,Vector2.zero,Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();
                image.raycastTarget=false;image.rectTransform.anchorMin=new Vector2(.12f,.48f);image.rectTransform.anchorMax=new Vector2(.88f,.92f);
                image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;image.enabled=false;var aspect=image.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.HeightControlsWidth;aspect.aspectRatio=1;
                var lockIcon=NeonKit.Icon("Lock",button.transform,"icon_lock",84,Vector2.zero);lockIcon.rectTransform.anchorMin=lockIcon.rectTransform.anchorMax=new Vector2(.5f,.70f);lockIcon.rectTransform.anchoredPosition=Vector2.zero;
                var starRoot = UiFactory.Rect("SavedStars", button.transform, new Vector2(110,28), Vector2.zero);
                var display = starRoot.gameObject.AddComponent<StarDisplay>(); display.Build(button.transform, 30, 36);
                starRoot.anchorMin = starRoot.anchorMax = new Vector2(.5f,.405f); starRoot.anchoredPosition = Vector2.zero;
                stars.Add(display);
                thumbnails.Add(image);buttons.Add(button);rims.Add(button.transform.Find("Rim").GetComponent<UnityEngine.UI.Image>());locks.Add(lockIcon.gameObject);
            }
            for (int i = 0; i < tiles.Count; i++)
            {
                LevelTile tile = tiles[i]; var button = buttons[i];
                bool locked = tile.State == LevelTileState.Locked, done = tile.State == LevelTileState.Completed;
                button.interactable = !locked;
                stars[i].SetCount(tile.BestStars); stars[i].gameObject.SetActive(done);
                string detail = locked ? "Locked" : done ? (tile.BestMistakes == 0 && tile.BestUndos == 0 ? "Flawless" : tile.BestMistakes + " mistakes") : "Play";
                button.GetComponentInChildren<UnityEngine.UI.Text>().text = "<size=32>" + tile.Number + "</size>  <size=16><color=#" + (done ? "FFD86B" : locked ? "8C86C0" : "8FF3FF") + ">" + detail + "</color></size>";
                thumbnails[i].color=new Color(1,1,1,locked?.25f:1);
                locks[i].SetActive(locked);
                NeonKit.Style(button, tile.IsResume && !locked ? NeonButtonStyle.Primary : NeonButtonStyle.Normal);
                rims[i].color = locked ? NeonKit.Dim : done ? NeonKit.Gold : tile.IsResume ? NeonKit.Bright : NeonKit.Cyan;
            }
            Canvas.ForceUpdateCanvases(); UpdateLayout(); RevealResume();
        }
        /// <summary>Scrolls the continue tile into the viewport after layout is calculated.</summary>
        public void RevealResume()
        {
            if (tiles == null || !Root.gameObject.activeInHierarchy) return;
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < tiles.Count; i++) if (tiles[i].IsResume)
            {
                float height = Scroll.content.rect.height - Scroll.viewport.rect.height;
                float row = i / grid.constraintCount;
                float y = row * (grid.cellSize.y + grid.spacing.y);
                Scroll.verticalNormalizedPosition = height > 0 ? 1 - Mathf.Clamp01(y / height) : 1;
                break;
            }
        }
        private void Update()
        {
            bool showing=Root!=null&&Root.gameObject.activeInHierarchy;
            if(!showing){if(wasVisible)ClearThumbnails();wasVisible=false;return;}wasVisible=true;
            if(tiles==null)return;renderer.BeginFrame();Scroll.viewport.GetWorldCorners(viewportCorners);
            for(int i=0;i<tiles.Count;i++)
            {
                thumbnails[i].rectTransform.GetWorldCorners(corners);
                if(corners[2].y>=viewportCorners[0].y&&corners[0].y<=viewportCorners[2].y&&corners[2].x>=viewportCorners[0].x&&corners[0].x<=viewportCorners[2].x)renderer.Request(tiles[i].Id);
            }
            renderer.Tick();for(int i=0;i<tiles.Count;i++){Texture2D texture=renderer.GetCached(tiles[i].Id);thumbnails[i].texture=texture;thumbnails[i].enabled=texture!=null;}
        }
        private void ClearThumbnails(){foreach(var image in thumbnails)if(image!=null){image.texture=null;image.enabled=false;}renderer.Dispose();}
        private void OnDisable()=>ClearThumbnails();
        private void OnDestroy(){ClearThumbnails();}
        private void LateUpdate() => UpdateLayout();
        private void UpdateLayout()
        {
            if (Scroll == null) return;
            Vector2 size = Scroll.viewport.rect.size; if (size == lastSize || size.x <= 0) return; lastSize = size;
            int columns = size.x >= 1340 && size.x > size.y ? 6 : 4;
            grid.constraintCount = columns;
            float cell = Mathf.Max(200, (size.x - 20 - (columns - 1) * 20) / columns);
            grid.cellSize = new Vector2(cell, cell);
        }
    }
}
