using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>One reusable lingering laser/prism/mist streak, separate from all gameplay geometry.</summary>
    public sealed class SkinTrailRibbon
    {
        private readonly Mesh mesh;
        private readonly MeshRenderer renderer;
        private readonly Vector3[] vertices=new Vector3[4];
        private readonly Vector2[] uv=new Vector2[4];
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        private float remaining,duration;
        /// <summary>Creates buffers once; the host owns the renderer object.</summary>
        public SkinTrailRibbon(Transform parent,int index)
        {
            var go=new GameObject("Pooled skin ribbon "+index);go.transform.SetParent(parent,false);mesh=new Mesh{name="Pooled skin ribbon",hideFlags=HideFlags.HideAndDontSave};mesh.MarkDynamic();mesh.vertices=vertices;mesh.triangles=new[]{0,1,2,0,2,3};mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};go.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=go.AddComponent<MeshRenderer>();renderer.sortingOrder=ViewTheme.EffectsSortingOrder;renderer.enabled=false;
        }
        /// <summary>Refreshes a fading segment; recipes select prism, laser, smoke or saturated fire.</summary>
        public void Set(ArrowSkin skin,Vector2 start,Vector2 end,bool light)
        {
            if(skin.Index==1)start=Vector2.MoveTowards(end,start,6);
            Vector2 d=(end-start).normalized,n=new Vector2(-d.y,d.x)*(skin.Index==6?.27f:skin.Index==2?.12f:.16f);
            vertices[0]=start+n;vertices[1]=end+n;vertices[2]=end-n;vertices[3]=start-n;
            float length=Vector2.Distance(start,end);uv[0]=new Vector2(0,1);uv[1]=new Vector2(length,1);uv[2]=new Vector2(length,0);uv[3]=Vector2.zero;
            mesh.vertices=vertices;mesh.uv=uv;mesh.bounds=new Bounds((start+end)*.5f,new Vector3(Mathf.Abs(end.x-start.x)+2,Mathf.Abs(end.y-start.y)+2,2));renderer.sharedMaterial=skin.Index==1?InfernoMaterials.Get(4,light):SkinMaterials.Glow(light);
            properties.SetFloat("_TrailLength",length);properties.SetFloat("_Trail",1);properties.SetFloat("_Art",0);properties.SetFloat("_Shape",skin.Index);properties.SetColor("_Tint",Color.Lerp(skin.Primary,skin.Secondary,.25f));properties.SetFloat("_Opacity",light?.30f:.60f);renderer.SetPropertyBlock(properties);renderer.enabled=length>.01f;duration=skin.TrailSeconds;remaining=duration;
        }
        /// <summary>Fades in place after its source arrow has gone.</summary>
        public void Tick(float dt){if(remaining<=0)return;remaining=Mathf.Max(0,remaining-dt);properties.SetFloat("_Opacity",remaining/duration*.45f);renderer.SetPropertyBlock(properties);renderer.enabled=remaining>0;}
        /// <summary>Returns this segment to its disabled rest state.</summary>
        public void Clear(){remaining=0;if(renderer!=null)renderer.enabled=false;}
        /// <summary>Releases its native mesh at host disposal.</summary>
        public void Dispose(){if(mesh!=null){if(Application.isPlaying)Object.Destroy(mesh);else Object.DestroyImmediate(mesh);}}
    }
}
