using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>UI ribbon using the same rounded mesh geometry as gameplay, without a new material.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PathPreviewGraphic : UnityEngine.UI.MaskableGraphic
    {
        private PathRibbon ribbon;
        private Color secondary;
        /// <summary>Builds immutable sample geometry once; equipment changes affect tint only.</summary>
        public void Configure(Color primary,Color accent)
        {
            color=primary;secondary=accent;
            if(ribbon==null){ribbon=new PathRibbon(new[]{new Vector2(-2,-1),new Vector2(0,-1),new Vector2(0,1),new Vector2(2,1)},6);ribbon.Build(0);}
            raycastTarget=false;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear();if(ribbon==null)return;
            float scale=Mathf.Min(rectTransform.rect.width/5,rectTransform.rect.height/3);
            Vector2 center=rectTransform.rect.center;
            for(int i=0;i<ribbon.Vertices.Length;i++)
            {Vector3 v=ribbon.Vertices[i];helper.AddVert(new Vector3(center.x+v.x*scale,center.y+v.y*scale,0),Color.Lerp(color,secondary,Mathf.Clamp01((v.x+2)/4)),Vector2.zero);}
            for(int i=0;i<ribbon.Triangles.Length;i+=3)helper.AddTriangle(ribbon.Triangles[i],ribbon.Triangles[i+1],ribbon.Triangles[i+2]);
        }
    }
}
