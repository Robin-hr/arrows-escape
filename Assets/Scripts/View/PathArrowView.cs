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
        public void SetClassicVisible(bool visible){if(renderer!=null)renderer.enabled=visible;}
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
            ribbon=new PathRibbon(points,arrow.Length-1);ExitDistance=extension;
            mesh=new Mesh{name="Runtime path ribbon",hideFlags=HideFlags.HideAndDontSave};mesh.MarkDynamic();
            colors=new Color[ribbon.Vertices.Length];
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            var properties=new MaterialPropertyBlock();properties.SetTexture("_MainTex",Texture2D.whiteTexture);renderer.SetPropertyBlock(properties);
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
            ribbon.Build(distance);if(!dedicatedSkinGeometry){for(int i=0;i<colors.Length;i++)colors[i]=vertexTint;mesh.vertices=ribbon.Vertices;mesh.uv=ribbon.UV;mesh.uv2=ribbon.Edges;mesh.uv3=ribbon.SkinTags;mesh.colors=colors;}renderer.sortingOrder=order;GeometryChanged?.Invoke();
        }
        private void OnDestroy(){if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
