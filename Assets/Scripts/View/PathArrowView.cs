using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Mesh appearance for a path, composed by its owning ArrowView layers.</summary>
    public sealed class PathArrowView : MonoBehaviour
    {
        private Mesh mesh;
        private MeshRenderer renderer;
        private Color[] colors;
        private PathRibbon ribbon;
        /// <summary>Shared geometry for optional skins; classic keeps its renderer unchanged.</summary>
        public Mesh RibbonMesh=>mesh;
        /// <summary>Distance-based track, also used by art caps and flight particles.</summary>
        public PathRibbon Ribbon=>ribbon;
        /// <summary>Notifies dependent visual layers after the moving ribbon has updated.</summary>
        public event System.Action GeometryChanged;
        /// <summary>Hides only the classic presentation while a skin is attached.</summary>
        public void SetClassicVisible(bool visible){classicVisible=visible;if(renderer!=null)renderer.enabled=visible;if(headArt!=null)headArt.enabled=visible&&headSprite!=null;}
        private bool classicVisible=true;
        private SpriteRenderer headArt;
        private static Texture2D bodyTexture;
        private static Sprite headSprite;
        private static bool artLoaded;
        /// <summary>Painted 2D arrow art (Resources/Arrows): a shaded body strip and a separate rounded head.</summary>
        private static void LoadArt()
        {
            if(artLoaded)return;artLoaded=true;
            bodyTexture=Resources.Load<Texture2D>("Arrows/arrow_body");
            var head=Resources.Load<Texture2D>("Arrows/arrow_head");
            if(head!=null){headSprite=Sprite.Create(head,new Rect(0,0,head.width,head.height),new Vector2(1,.5f),head.height/HeadArtWidth,0,SpriteMeshType.FullRect);headSprite.name="Arrow head art";headSprite.hideFlags=HideFlags.HideAndDontSave;}
        }
        /// <summary>World width of the painted head (its height in the texture).</summary>
        public const float HeadArtWidth=.70f;
        /// <summary>The classic painted head, or null when the art is missing.</summary>
        public SpriteRenderer HeadArt=>headArt;
        private float distance;
        private Color tint;
        private int order;
        private bool dedicatedSkinGeometry;
        /// <summary>Rare materials own their mesh; the invisible classic GPU upload can be skipped.</summary>
        public void SetDedicatedSkinGeometry(bool value){dedicatedSkinGeometry=value;}
        /// <summary>Distance required to carry the tail beyond the board.</summary>
        public float ExitDistance { get; private set; }
        /// <summary>Current travel along the path.</summary>
        public float Distance => distance;
        /// <summary>Creates a local track and reuses the board sprite material.</summary>
        public void Initialize(Arrow arrow,int width,int height,Material material)
        {
            var points=new Vector2[arrow.Length+1];
            for(int i=0;i<arrow.Length;i++)points[i]=new Vector2(arrow.Cells[i].X-arrow.X,arrow.Cells[i].Y-arrow.Y);
            float extension=AnimationMath.SlideDistance(width,height,arrow.X,arrow.Y,arrow.Direction,arrow.Length+1);
            points[arrow.Length]=new Vector2(arrow.Direction.GetDx(),arrow.Direction.GetDy())*extension;
            // Rounded bends; the body length is the curved distance from the tail to the head cell.
            var curved=CurvedTrack.Build(points,arrow.Length-1,out float bodyLength);
            ribbon=new PathRibbon(curved,bodyLength);ExitDistance=extension;
            mesh=new Mesh{name="Runtime path ribbon",hideFlags=HideFlags.HideAndDontSave};mesh.MarkDynamic();
            colors=new Color[ribbon.Vertices.Length];
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            LoadArt();
            var properties=new MaterialPropertyBlock();properties.SetTexture("_MainTex",bodyTexture!=null?bodyTexture:Texture2D.whiteTexture);renderer.SetPropertyBlock(properties);
            if(headSprite!=null)
            {
                var headObject=new GameObject("Arrow head art");headObject.transform.SetParent(transform,false);
                headArt=headObject.AddComponent<SpriteRenderer>();headArt.sprite=headSprite;headArt.sharedMaterial=material;
            }
            mesh.vertices=ribbon.Vertices;mesh.triangles=ribbon.Triangles;
            mesh.bounds=new Bounds(Vector3.zero,Vector3.one*(width+height+arrow.Length)*4);
        }
        /// <summary>Updates the independent motion layer.</summary>
        public void SetDistance(float value){distance=Mathf.Clamp(value,0,ExitDistance);Apply();}
        /// <summary>Receives composed tint and ordering from ArrowView.</summary>
        public void SetAppearance(Color value,int sortingOrder){tint=value;order=sortingOrder;Apply();}
        private void Apply()
        {
            if(mesh==null)return;
            // Mesh vertex colors need the conversion SpriteRenderer normally supplies.
            Color vertexTint=QualitySettings.activeColorSpace==ColorSpace.Linear?tint.linear:tint;
            ribbon.Build(distance);
            if(!dedicatedSkinGeometry)
            {
                for(int i=0;i<colors.Length;i++)colors[i]=vertexTint;
                // The painted head sprite replaces the ribbon's flat triangle.
                if(headArt!=null){int h=colors.Length-3;Color clear=vertexTint;clear.a=0;colors[h]=colors[h+1]=colors[h+2]=clear;}
                mesh.vertices=ribbon.Vertices;mesh.uv=ribbon.UV;mesh.uv2=ribbon.Edges;mesh.uv3=ribbon.SkinTags;mesh.colors=colors;
            }
            if(headArt!=null)
            {
                Vector3[] v=ribbon.Vertices;int h=v.Length-3;Vector2 tip=v[h],back=(v[h+1]+v[h+2])*.5f,dir=tip-back;
                headArt.transform.localPosition=new Vector3(tip.x,tip.y,-.001f);
                headArt.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg);
                headArt.color=tint;headArt.sortingOrder=order;headArt.enabled=classicVisible;
            }
            renderer.sortingOrder=order;GeometryChanged?.Invoke();
        }
        private void OnDestroy(){if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
