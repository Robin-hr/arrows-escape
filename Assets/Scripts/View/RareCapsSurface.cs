using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Batches the shaded head and feather fletching into one alpha-blended draw.</summary>
    public sealed class RareCapsSurface
    {
        private readonly Vector3[] vertices=new Vector3[8];
        private readonly Vector2[] uv=new Vector2[8],directions=new Vector2[8],tags=new Vector2[8];
        private Vector2 previousHead,previousTail,previousDirection,previousTailDirection;
        private bool prepared,tailVisible;
        /// <summary>One owned mesh with two independently positioned fields.</summary>
        public Mesh Mesh {get;}
        /// <summary>Prepares both quads and their head/tail tags once.</summary>
        public RareCapsSurface()
        {
            Mesh=new Mesh{name="Batched rare head and feathers",hideFlags=HideFlags.HideAndDontSave};Mesh.MarkDynamic();
            for(int i=0;i<2;i++){int v=i*4;uv[v]=new Vector2(-1.5f,-.85f);uv[v+1]=new Vector2(.25f,-.85f);uv[v+2]=new Vector2(.25f,.85f);uv[v+3]=new Vector2(-1.5f,.85f);for(int j=0;j<4;j++)tags[v+j]=new Vector2(i==0?0:-1,-2);}
            Mesh.vertices=vertices;Mesh.uv=uv;Mesh.uv3=tags;Mesh.triangles=new[]{0,1,2,0,2,3,4,5,6,4,6,7};
        }
        /// <summary>Updates prepared vertices only when the moving window changes.</summary>
        public void Build(Vector2 head,Vector2 direction,Vector2 tail,Vector2 tailDirection,bool showTail)
        {
            if(prepared&&head==previousHead&&tail==previousTail&&direction==previousDirection&&tailDirection==previousTailDirection&&showTail==tailVisible)return;
            prepared=true;previousHead=head;previousTail=tail;previousDirection=direction;previousTailDirection=tailDirection;tailVisible=showTail;
            for(int i=0;i<8;i++){Vector2 d=i<4?direction:tailDirection,normal=new Vector2(-d.y,d.x),center=i<4?head:tail+tailDirection*.7f;vertices[i]=i>=4&&!showTail?Vector3.zero:(Vector3)(center+d*uv[i].x+normal*uv[i].y);directions[i]=d;}
            Mesh.vertices=vertices;Mesh.uv2=directions;Mesh.RecalculateBounds();var bounds=Mesh.bounds;bounds.Expand(2);Mesh.bounds=bounds;
        }
        /// <summary>Disposes the single owned buffer mesh.</summary>
        public void Dispose(){if(Mesh==null)return;if(Application.isPlaying)Object.Destroy(Mesh);else Object.DestroyImmediate(Mesh);}
    }
}
