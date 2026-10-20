using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>One cheap static dot mesh, rebuilt only when its screen rectangle changes.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuDotGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear();raycastTarget=false;Rect r=rectTransform.rect;int index=0;
            for(float y=r.yMin+32;y<r.yMax;y+=64)for(float x=r.xMin+32;x<r.xMax;x+=64)
            {
                helper.AddVert(new Vector3(x,y),color,Vector2.zero);
                for(int k=0;k<8;k++){float a=k*Mathf.PI/4;helper.AddVert(new Vector3(x+Mathf.Cos(a)*1.5f,y+Mathf.Sin(a)*1.5f),color,Vector2.zero);}
                for(int k=0;k<8;k++)helper.AddTriangle(index,index+1+k,index+1+(k+1)%8);index+=9;
            }
        }
    }
}
