using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Creates a tidy runtime hierarchy for a centered board.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        private readonly Dictionary<int, ArrowView> arrowViews = new Dictionary<int, ArrowView>();
        private readonly Dictionary<int, ArrowView> releasedViews = new Dictionary<int, ArrowView>();
        private Transform root;
        /// <summary>Whether the board root should be visible, including after replacement.</summary>
        public bool IsVisible { get; private set; } = true;
        /// <summary>Sets display visibility without changing the board.</summary>
        public void SetVisible(bool visible) { IsVisible = visible; if (root != null) root.gameObject.SetActive(visible); }
        private Material arrowMaterial;
        private Material cellMaterial;
        private int width;
        private int height;
        private Board currentBoard;
        private string arrowSkinId="classic";
        private bool lightBoard=true;
        /// <summary>Equipped background; visual identity never replaces a progress key.</summary>
        public string ThemeId {get;private set;}="paper";
        /// <summary>Non-Paper themes take precedence over skin preferences and debug light fallback.</summary>
        public bool HasDarkTheme=>ThemeDesign.Index(ThemeId)>=0;
        /// <summary>Applies the equipped theme before refreshing skins and contrast.</summary>
        public void SetTheme(string id)
        {ThemeId=ThemeDesign.Index(id)<0?"paper":id;var backdrop=GetComponent<ThemeBackdrop>();if(backdrop==null&&HasDarkTheme){backdrop=gameObject.AddComponent<ThemeBackdrop>();backdrop.Bind(Camera.main,this,GetComponent<GameController>());}backdrop?.SetTheme(ThemeId);SetArrowSkin(arrowSkinId);ApplyDotStyle();GetComponent<CameraFitter>()?.ApplyBackground();}

        /// <summary>Current visual recipe id, independent of the saved inventory.</summary>
        public string ArrowSkinId=>arrowSkinId;
        /// <summary>Resolved board presentation mode; independent of saved Paper theme.</summary>
        public bool IsLightBoard=>lightBoard;
        /// <summary>Notifies HUD contrast when equipment changes presentation.</summary>
        public event Action SkinChanged;
        /// <summary>Applies only implemented skins, preserving fallback tint and Ice.</summary>
        public void SetArrowSkin(string id,bool light=true)
        {
            GetComponent<SkinDirector>()?.ClearDimming();
            bool wasFire=ArrowSkinRegistry.Find(arrowSkinId)?.PrefersDarkBoard==true;
            arrowSkinId=id;
            var director=GetComponent<SkinDirector>();
            if(ArrowSkinRegistry.Find(id)!=null&&director==null){director=gameObject.AddComponent<SkinDirector>();director.Bind(this,GetComponent<GameController>());}
            if(ArrowSkinRegistry.Find(id)?.PrefersDarkBoard==true)light=director?.InfernoLightOverride??false;
            else if(wasFire){light=true;director?.ResetInfernoOverride();}
            if(HasDarkTheme)light=false;
            lightBoard=light;
            foreach(var pair in arrowViews)if(pair.Value!=null)pair.Value.SetSkin(id,light);
            foreach(var pair in releasedViews)if(pair.Value!=null)pair.Value.SetSkin(id,light);
            if(wasFire||ArrowSkinRegistry.Find(id)?.PrefersDarkBoard==true){GetComponent<CameraFitter>()?.ApplyBackground();ApplyDotStyle();}
            director?.RefreshDimming();SkinChanged?.Invoke();
        }
        private void ApplyDotStyle()
        {
            bool fire=ArrowSkinRegistry.Find(arrowSkinId)?.PrefersDarkBoard==true&&!lightBoard;Color tint=HasDarkTheme?new Color(233f/255,238f/255,242f/255,.20f):ViewTheme.CellDot;if(fire&&!HasDarkTheme)tint.a=.18f;
            if(PathDots!=null)PathDots.SetRestColor(tint);
            else if(root!=null)for(int i=0;i<root.childCount;i++){var child=root.GetChild(i);if(child.name.StartsWith("Cell ",StringComparison.Ordinal)){var dot=child.GetComponent<SpriteRenderer>();if(dot!=null)dot.color=tint;}}
        }
        private Color normalArrowColor=ViewTheme.ArrowColor;
        /// <summary>Applies equipment to existing arrows and future undo-restored views.</summary>
        public void SetNormalArrowColor(Color color)
        {
            normalArrowColor=color;
            foreach(var pair in arrowViews)if(pair.Value!=null&&pair.Value.Arrow.Kind==ArrowKind.Normal)pair.Value.BaseColor=color;
        }

        /// <summary>Path-grid renderer fixed for the lifetime of this displayed attempt.</summary>
        public DotGrid PathDots { get; private set; }
        /// <summary>Board rectangle in world space.</summary>
        public Bounds WorldBounds => new Bounds(Vector3.zero,new Vector3(width,height,0));
        /// <summary>World bounds of the cells the level's arrows occupy at the start (the visible shape).</summary>
        public Bounds ContentBounds { get; private set; }
        private void MeasureContent(Board board)
        {
            int minX=int.MaxValue,minY=int.MaxValue,maxX=int.MinValue,maxY=int.MinValue;
            foreach(Arrow arrow in board.Arrows)for(int c=0;c<arrow.Length;c++){Cell cell=arrow.Cells[c];minX=Mathf.Min(minX,cell.X);minY=Mathf.Min(minY,cell.Y);maxX=Mathf.Max(maxX,cell.X);maxY=Mathf.Max(maxY,cell.Y);}
            if(minX>maxX){ContentBounds=WorldBounds;return;}
            Vector3 a=CellToWorld(minX,minY),b=CellToWorld(maxX,maxY);
            ContentBounds=new Bounds((a+b)*.5f,new Vector3(maxX-minX+1,maxY-minY+1,0));
        }
        /// <summary>Whether this attempt was loaded with paths.</summary>
        public bool UsesPaths { get; private set; }

        /// <summary>Whether this displayed board was small enough to draw cell dots.</summary>
        public bool HasCellDots => !UsesPaths && root != null && width > 0 && height > 0 &&
            width <= ViewTheme.DotBoardLimit && height <= ViewTheme.DotBoardLimit;

        /// <summary>Finds a cell dot for effects, safely rejecting missing or destroyed objects.</summary>
        public bool TryGetCellDot(int x, int y, out SpriteRenderer renderer)
        {
            renderer = null;
            if (!HasCellDots || x < 0 || y < 0 || x >= width || y >= height) return false;
            Transform dot = root.Find("Cell " + x + "," + y);
            if (dot == null) return false;
            renderer = dot.GetComponent<SpriteRenderer>();
            return renderer != null;
        }

        /// <summary>Raised after clearing or finishing a replacement board display.</summary>
        public event Action BoardChanged;

        /// <summary>Replaces the current display with the supplied board; null clears it.</summary>
        public void Show(Board board)
        {
            ClearDisplay(false);
            if (board == null) { BoardChanged?.Invoke(); return; }
            arrowMaterial = PrepareMaterial(arrowMaterial, SpriteFactory.GetArrowSprite());
            width = board.Width;
            height = board.Height;
            MeasureContent(board);
            root = new GameObject("Board").transform;
            root.SetParent(transform, false);
            if(GetComponent<BoardZoom>()==null)gameObject.AddComponent<BoardZoom>();
            UsesPaths=false;
            foreach(Arrow arrow in board.Arrows)if(arrow.Length>1){UsesPaths=true;break;}
            if(UsesPaths)
            {
                PathDots=CreateChild("Dot Grid").AddComponent<DotGrid>();
                PathDots.Initialize(this,board,arrowMaterial,GetComponent<GameController>());
            }
            else if (width <= ViewTheme.DotBoardLimit && height <= ViewTheme.DotBoardLimit) CreateDots();
            foreach (Arrow arrow in board.Arrows)
            {
                AddArrowView(arrow);
            }
            root.gameObject.SetActive(IsVisible);
            currentBoard = board;
            if(HasDarkTheme||ArrowSkinRegistry.Find(arrowSkinId)?.PrefersDarkBoard==true)ApplyDotStyle();
            BoardChanged?.Invoke();
        }

        /// <summary>Maps a cell center directly to y-up world space centered on the origin.</summary>
        public Vector3 CellToWorld(int x, int y) => CellToWorld((float)x, y);

        /// <summary>Maps continuous cell coordinates to the same centered y-up world space.</summary>
        public Vector3 CellToWorld(float x, float y) =>
            new Vector3(x - (width - 1) * 0.5f, y - (height - 1) * 0.5f, 0);

        /// <summary>Converts world space to continuous cell coordinates (integers are centers).</summary>
        public Vector2 WorldToCell(Vector3 world) =>
            new Vector2(world.x + (width - 1) * 0.5f, world.y + (height - 1) * 0.5f);

        /// <summary>Reads the displayed board; input consumers must query it without mutation.</summary>
        public bool TryGetBoard(out Board board)
        {
            board = currentBoard;
            return board != null;
        }

        /// <summary>Finds the current arrow view with this identifier.</summary>
        public bool TryGetArrowView(int id, out ArrowView view) => arrowViews.TryGetValue(id, out view);

        /// <summary>Removes a view from tap lookup while retaining it under the cleanup root.</summary>
        public bool ReleaseArrowView(int id, out ArrowView view)
        {
            if (!arrowViews.TryGetValue(id, out view)) return false;
            arrowViews.Remove(id);
            releasedViews[id] = view;
            return view != null;
        }

        /// <summary>Removes the generated display; runtime destruction finishes at frame end.</summary>
        public void Clear() => ClearDisplay(true);

        /// <summary>Creates a fresh normal view, replacing any registered view with this id.</summary>
        public ArrowView AddArrowView(Arrow arrow)
        {
            if (arrow == null || root == null) return null;
            if (arrowViews.TryGetValue(arrow.Id, out ArrowView old)) DestroyArrow(old);
            DiscardReleasedView(arrow.Id);
            ArrowView view = CreateChild($"Arrow {arrow.Id}").AddComponent<ArrowView>();
            view.Initialize(arrow, arrowMaterial);
            if(arrow.Length>1)view.InitializePath(width,height,arrowMaterial);
            view.BaseColor=arrow.Kind==ArrowKind.Ice?ViewTheme.IceArrowColor:normalArrowColor;
            view.SetBaseWorldPosition(CellToWorld(arrow.X, arrow.Y));
            view.ResetVisual();
            view.SetSkin(arrowSkinId,lightBoard);
            arrowViews[arrow.Id] = view;
            return view;
        }

        /// <summary>Finds a released view so its animation can be cancelled before disposal.</summary>
        public bool TryGetReleasedView(int id, out ArrowView view) => releasedViews.TryGetValue(id, out view) && view != null;
        /// <summary>Reuses the current view by id and updates its logical cell without a visual jump.</summary>
        public bool RebaseArrowView(Arrow moved,bool keepWorldPosition,out ArrowView view)
        {
            view=null;if(moved==null||!arrowViews.TryGetValue(moved.Id,out view)||view==null)return false;
            view.Rebase(moved,CellToWorld(moved.X,moved.Y),keepWorldPosition);return true;
        }

        /// <summary>Immediately retires a released view; it can never remain visible beside its replacement.</summary>
        public bool DiscardReleasedView(int id)
        {
            if (!releasedViews.TryGetValue(id, out ArrowView view)) return false;
            releasedViews.Remove(id);
            if (view == null) return false;
            DestroyArrow(view);
            return true;
        }

        private static void DestroyArrow(ArrowView view)
        {
            if (view == null) return;
            view.gameObject.SetActive(false);
            // Runtime-created views have no saved assets; immediate disposal prevents duplicate ids.
            DestroyImmediate(view.gameObject);
        }

        private void ClearDisplay(bool notify)
        {
            currentBoard = null;
            PathDots=null;UsesPaths=false;
            if (notify) BoardChanged?.Invoke();
            arrowViews.Clear();
            releasedViews.Clear();
            width = height = 0;
            if (root == null) return;
            root.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(root.gameObject);
            else DestroyImmediate(root.gameObject);
            root = null;
        }

        private void CreateDots()
        {
            cellMaterial = PrepareMaterial(cellMaterial, SpriteFactory.GetCellSprite());
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    SpriteRenderer dot = CreateChild($"Cell {x},{y}").AddComponent<SpriteRenderer>();
                    dot.transform.position = CellToWorld(x, y);
                    dot.sprite = SpriteFactory.GetCellSprite();
                    dot.color = ViewTheme.CellDot;
                    dot.sortingOrder = ViewTheme.CellSortingOrder;
                    dot.sharedMaterial = cellMaterial;
                    dot.transform.localScale = Vector3.one * ViewTheme.CellDotSize;
                }
        }

        private GameObject CreateChild(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root, false);
            return child;
        }

        private void OnDestroy()
        {
            DestroyMaterial(arrowMaterial);
            DestroyMaterial(cellMaterial);
        }

        private static Material PrepareMaterial(Material material, Sprite sprite)
        {
            if (material == null)
                material = new Material(Shader.Find("Sprites/Default"))
                {
                    name = sprite.name + " default sprites", hideFlags = HideFlags.HideAndDontSave
                };
            // Bind each texture explicitly: URP must not reuse the dot texture for arrows.
            // All arrows still share one material, and all dots share another.
            material.mainTexture = sprite.texture;
            return material;
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
