using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Fixed pool of exact-track residues and rifts, separate from instantly applied puzzle moves.</summary>
    public sealed class ElementalSignatures
    {
        private const int Capacity=12,Sections=256;
        private static readonly Slot[] fragmentReservations=new Slot[50];
        /// <summary>Analytical edge fragments count against the same six-hundred-particle budget.</summary>
        public static int ActiveGlassFragments
        {get{int count=0;for(int i=0;i<fragmentReservations.Length;i++){var s=fragmentReservations[i];if(s==null)continue;if(s.Rift==null){s.Reserved=false;fragmentReservations[i]=null;}else count+=12;}return count;}}
        private sealed class Slot
        {
            internal Mesh Mesh,StarsMesh;internal MeshRenderer Renderer,Rift,Stars;
            internal readonly Vector3[] StarVertices=new Vector3[48];
            internal readonly Vector2[] StarUV=new Vector2[48];
            internal ArrowView View;
            internal Vector2 InitialTail;
            internal float BodyLength;
            internal readonly Vector3[] Vertices=new Vector3[Sections*4];
            internal readonly Vector2[] UV=new Vector2[Sections*4];
            internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();
            internal float Time,Duration,Lead;internal ArrowSkin Skin;internal bool Active,Returning,Impact,Reserved;
        }
        private readonly Slot[] slots=new Slot[Capacity];
        private int next;
        /// <summary>Total starts; one per outgoing arrow, never retriggered by a frame.</summary>
        public int Starts {get;private set;}
        /// <summary>Edge bites, counted separately from exit signatures.</summary>
        public int EdgeStarts {get;private set;}
        /// <summary>Latest baked track's original tail, for exact-travel regression checks.</summary>
        public Vector2 LastTrackStart {get;private set;}
        /// <summary>Latest baked track's final head position, including the complete exit distance.</summary>
        public Vector2 LastTrackEnd {get;private set;}
        /// <summary>Whether any residue is still visible.</summary>
        public bool HasActive {get{for(int i=0;i<slots.Length;i++)if(slots[i].Active&&!slots[i].Returning)return true;return false;}}
        /// <summary>The reverse rift is an arrow-return effect, independent of board residues.</summary>
        public bool HasReturnRift {get{for(int i=0;i<slots.Length;i++)if(slots[i].Active&&slots[i].Returning)return true;return false;}}
        /// <summary>Creates all meshes, buffers and renderers outside the play path.</summary>
        public ElementalSignatures(Transform parent)
        {
            // Edge marks may start inside Tick: prepare every possible pass before animation.
            for(int skin=0;skin<4;skin++)for(int mode=4;mode<6;mode++){ElementalMaterials.Get(skin,mode,false);ElementalMaterials.Get(skin,mode,true);}
            for(int skin=5;skin<=9;skin++)for(int mode=4;mode<8;mode++){RareMaterials.Get(skin,mode,false);RareMaterials.Get(skin,mode,true);}
            var indices=new int[Sections*6];for(int j=0;j<Sections;j++){int v=j*4,t=j*6;indices[t]=v;indices[t+1]=v+1;indices[t+2]=v+2;indices[t+3]=v;indices[t+4]=v+2;indices[t+5]=v+3;}
            for(int i=0;i<slots.Length;i++)
            {
                var go=new GameObject("Pooled Elemental signature "+i);go.transform.SetParent(parent,false);var s=new Slot();slots[i]=s;
                s.Mesh=new Mesh{name="Exact travelled path residue",hideFlags=HideFlags.HideAndDontSave};s.Mesh.MarkDynamic();s.Mesh.vertices=s.Vertices;s.Mesh.triangles=indices;go.AddComponent<MeshFilter>().sharedMesh=s.Mesh;
                s.Renderer=go.AddComponent<MeshRenderer>();s.Renderer.sortingOrder=ViewTheme.EffectsSortingOrder;s.Renderer.enabled=false;
                var portal=new GameObject("Closing shadow rift");portal.transform.SetParent(go.transform,false);portal.AddComponent<MeshFilter>().sharedMesh=RiftQuad; s.Rift=portal.AddComponent<MeshRenderer>();s.Rift.sortingOrder=ViewTheme.EffectsSortingOrder;s.Rift.enabled=false;
                var stars=new GameObject("Path-corner constellation");stars.transform.SetParent(go.transform,false);s.StarsMesh=new Mesh{name="Constellation markers",hideFlags=HideFlags.HideAndDontSave};s.StarsMesh.MarkDynamic();s.StarsMesh.vertices=s.StarVertices;var starIndices=new int[72];for(int k=0;k<12;k++){int v=k*4,t=k*6;starIndices[t]=v;starIndices[t+1]=v+1;starIndices[t+2]=v+2;starIndices[t+3]=v;starIndices[t+4]=v+2;starIndices[t+5]=v+3;}s.StarsMesh.triangles=starIndices;stars.AddComponent<MeshFilter>().sharedMesh=s.StarsMesh;s.Stars=stars.AddComponent<MeshRenderer>();s.Stars.sortingOrder=ViewTheme.EffectsSortingOrder;s.Stars.enabled=false;
            }
        }
        /// <summary>Bakes the outgoing body and its forward extension, preserving every bend.</summary>
        public void Begin(ArrowView view,float motionSeconds=-1,float travelCells=-1)
        {
            if(view?.Skin?.Skin?.IsProcedural!=true)return;Starts++;var s=slots[next++%slots.Length];Release(s);s.Skin=view.Skin.Skin;s.View=view;s.Time=0;s.Duration=s.Skin.Index==0?1.2f:s.Skin.Index==2?.3f:.8f;s.Lead=s.Skin.Index==2?Mathf.Max(0,motionSeconds):0;s.Returning=false;s.Active=true;s.Impact=false;
            s.Stars.enabled=false;s.Rift.enabled=s.Skin.Index==3||s.Skin.Index==6||s.Skin.Index==7||s.Skin.Index==8||s.Skin.Index==9;s.Rift.transform.position=view.Skin.HeadWorld+view.Skin.DirectionWorld*.45f;s.Rift.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(view.Skin.DirectionWorld.y,view.Skin.DirectionWorld.x)*Mathf.Rad2Deg);s.Rift.sharedMaterial=Pass(s.Skin,5,view.Skin.IsLight);
            if(s.Skin.Index==9){s.Rift.transform.position=(view.Skin.HeadWorld+view.Skin.TailWorld)*.5f;s.Rift.sortingOrder=0;}else s.Rift.sortingOrder=ViewTheme.EffectsSortingOrder;
            var path=view.Path;s.InitialTail=view.Skin.TailWorld;s.BodyLength=Vector2.Distance(s.InitialTail,view.Skin.HeadWorld);
            if(s.Skin.Index==7)
            {
                int marker=0;for(int k=0;k<view.Arrow.Length&&marker<12;k++)
                {bool corner=k==0||k==view.Arrow.Length-1;if(!corner){var a=view.Arrow.Cells[k-1];var b=view.Arrow.Cells[k];var c=view.Arrow.Cells[k+1];corner=b.X-a.X!=c.X-b.X||b.Y-a.Y!=c.Y-b.Y;}if(!corner)continue;Vector2 center=path!=null?(Vector2)view.transform.TransformPoint(path.Ribbon.Point(k)):view.Skin.HeadWorld;int v=marker++*4;s.StarVertices[v]=center+new Vector2(-.35f,-.35f);s.StarVertices[v+1]=center+new Vector2(.35f,-.35f);s.StarVertices[v+2]=center+new Vector2(.35f,.35f);s.StarVertices[v+3]=center+new Vector2(-.35f,.35f);s.StarUV[v]=Vector2.zero;s.StarUV[v+1]=Vector2.right;s.StarUV[v+2]=Vector2.one;s.StarUV[v+3]=Vector2.up;}
                for(int k=marker*4;k<s.StarVertices.Length;k++)s.StarVertices[k]=Vector3.zero;s.StarsMesh.vertices=s.StarVertices;s.StarsMesh.uv=s.StarUV;s.StarsMesh.RecalculateBounds();s.Stars.sharedMaterial=RareMaterials.Get(7,7,view.Skin.IsLight);s.Stars.enabled=true;
            }
            float length=path!=null?path.Ribbon.TrackLength:Mathf.Max(0,travelCells)+s.BodyLength;
            LastTrackStart=s.InitialTail;LastTrackEnd=path!=null?(Vector2)view.transform.TransformPoint(path.Ribbon.Point(length)):s.InitialTail+view.Skin.DirectionWorld*length;
            int count=Mathf.Min(Sections,Mathf.CeilToInt(length*4));float width=s.Skin.Index==0?.7f:s.Skin.Index==3?.4f:s.Skin.Index==6?.45f:.14f;
            for(int j=0;j<Sections;j++)
            {
                float a=Mathf.Min(j,count)*length/count,b=Mathf.Min(j+1,count)*length/count;
                Vector2 p=path!=null?(Vector2)view.transform.TransformPoint(path.Ribbon.Point(a)):view.Skin.TailWorld+view.Skin.DirectionWorld*a;
                Vector2 q=path!=null?(Vector2)view.transform.TransformPoint(path.Ribbon.Point(b)):view.Skin.TailWorld+view.Skin.DirectionWorld*b;
                Vector2 normal=new Vector2(-(q-p).y,(q-p).x).normalized*width;
                int v=j*4;s.Vertices[v]=p-normal;s.Vertices[v+1]=p+normal;s.Vertices[v+2]=q+normal;s.Vertices[v+3]=q-normal;
                s.UV[v]=new Vector2(a,0);s.UV[v+1]=new Vector2(a,1);s.UV[v+2]=new Vector2(b,1);s.UV[v+3]=new Vector2(b,0);
            }
            s.Mesh.vertices=s.Vertices;s.Mesh.uv=s.UV;s.Mesh.RecalculateBounds();s.Renderer.sharedMaterial=Pass(s.Skin,4,view.Skin.IsLight);s.Renderer.enabled=true;Apply(s);
        }
        /// <summary>A smaller bite or ion arc reaches the actual blocking cell, never a whole-board ray.</summary>
        public void Blocked(ArrowView view,Vector2 blocker)
        {
            if(view?.Skin?.Skin?.IsProcedural!=true||view.Skin.Skin.Index==1)return;
            var s=slots[next++%slots.Length];Release(s);s.Skin=view.Skin.Skin;s.View=null;s.Time=s.Lead=0;s.Returning=false;s.Duration=.2f;s.Active=true;s.Impact=true;s.Stars.enabled=false;s.Rift.enabled=s.Skin.Index==9;if(s.Rift.enabled){s.Rift.sharedMaterial=Pass(s.Skin,5,view.Skin.IsLight);s.Rift.transform.position=view.Skin.HeadWorld;s.Rift.sortingOrder=0;}
            Vector2 start=view.Skin.HeadWorld,delta=blocker-start,side=new Vector2(-delta.y,delta.x).normalized;
            for(int j=0;j<Sections;j++)
            {
                float a=Mathf.Min(j,12)/12f,b=Mathf.Min(j+1,12)/12f;
                Vector2 p=start+delta*a+side*(a>0&&a<1?ArrowEscape.Core.SkinEffectRules.Lightning(view.Arrow.Id,j,0)*.00025f:0);
                Vector2 q=start+delta*b+side*(b>0&&b<1?ArrowEscape.Core.SkinEffectRules.Lightning(view.Arrow.Id,j+1,0)*.00025f:0);int v=j*4;Vector2 w=side*.035f;
                s.Vertices[v]=p-w;s.Vertices[v+1]=p+w;s.Vertices[v+2]=q+w;s.Vertices[v+3]=q-w;s.UV[v]=new Vector2(a,0);s.UV[v+1]=new Vector2(a,1);s.UV[v+2]=new Vector2(b,1);s.UV[v+3]=new Vector2(b,0);
            }
            s.Mesh.vertices=s.Vertices;s.Mesh.uv=s.UV;s.Mesh.RecalculateBounds();s.Renderer.sharedMaterial=Pass(s.Skin,4,view.Skin.IsLight);s.Renderer.enabled=true;Apply(s);
        }
        /// <summary>A shrinking crack at the exact board crossing, independent of the removed view.</summary>
        public void Edge(ArrowView view,Vector2 point)
        {
            if(view?.Skin?.Skin==null||(view.Skin.Skin.Index!=0&&view.Skin.Skin.Index!=5))return;if(view.Skin.Skin.Index==5&&view.Skin.Rare.FlightElapsed>=.9f)return;EdgeStarts++;var s=slots[next++%slots.Length];Release(s);s.Skin=view.Skin.Skin;s.View=null;s.Time=s.Lead=0;s.Returning=false;s.Duration=1;s.Active=true;s.Impact=false;s.Stars.enabled=false;s.Renderer.enabled=false;s.Rift.enabled=true;if(s.Skin.Index==5){s.Duration=Mathf.Min(.25f,.9f-view.Skin.Rare.FlightElapsed);if(!Reserve(s)){s.Active=false;s.Rift.enabled=false;return;}SkinParticlePool.EnforceBudget();}s.Rift.transform.position=point;s.Rift.transform.rotation=Quaternion.identity;s.Rift.sharedMaterial=Pass(s.Skin,5,view.Skin.IsLight);Apply(s);
        }
        /// <summary>Opens the same shadow rift in reverse while smoke gathers back into the restored arrow.</summary>
        public void ReturnRift(ArrowView view)
        {
            if(view?.Skin?.Skin?.Index!=3)return;var s=slots[next++%slots.Length];Release(s);s.Skin=view.Skin.Skin;s.View=null;s.Time=s.Lead=0;s.Returning=s.Active=true;s.Impact=false;s.Stars.enabled=false;s.Duration=view.Path!=null?ArrowEscape.Core.AnimationMath.PathExitDuration(view.Path.Distance):.35f;s.Renderer.enabled=false;s.Rift.enabled=true;
            s.Rift.transform.position=view.BaseWorldPosition+view.Skin.DirectionWorld*.45f;s.Rift.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(view.Skin.DirectionWorld.y,view.Skin.DirectionWorld.x)*Mathf.Rad2Deg);s.Rift.sharedMaterial=ElementalMaterials.Get(3,5,view.Skin.IsLight);Apply(s);
        }
        private static bool Reserve(Slot slot)
        {
            // Unity hosts can disappear without a caller disposing this plain C# pool.
            // Prune their reservations before budgeting another twelve-fragment field.
            if(ActiveGlassFragments>=600)return false;
            for(int i=0;i<fragmentReservations.Length;i++)if(fragmentReservations[i]==null){fragmentReservations[i]=slot;slot.Reserved=true;return true;}
            return false;
        }
        private static void Release(Slot slot){if(!slot.Reserved)return;slot.Reserved=false;for(int i=0;i<fragmentReservations.Length;i++)if(fragmentReservations[i]==slot){fragmentReservations[i]=null;return;}}
        private static Material Pass(ArrowSkin skin,int mode,bool light)=>skin.IsRare?RareMaterials.Get(skin.Index,mode,light):ElementalMaterials.Get(skin.Index,mode,light);
        private static Mesh riftQuad;
        private static Mesh RiftQuad
        {get{if(riftQuad!=null)return riftQuad;riftQuad=new Mesh{name="Procedural rift quad",hideFlags=HideFlags.HideAndDontSave};riftQuad.vertices=new[]{new Vector3(-1,-1),new Vector3(1,-1),new Vector3(1,1),new Vector3(-1,1)};riftQuad.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};riftQuad.triangles=new[]{0,1,2,0,2,3};return riftQuad;}}
        private static void Apply(Slot s)
        {float t=Mathf.Clamp01((s.Time-s.Lead)/s.Duration);s.Properties.SetFloat("_Age",s.Returning?.3f:t*t*(3-2*t));float reveal=1000;if(s.View!=null)reveal=s.View.Path!=null?s.View.Path.Distance+s.View.Path.Ribbon.BodyLength:Vector2.Distance(s.InitialTail,s.View.Skin.TailWorld)+s.BodyLength;s.Properties.SetFloat("_Reveal",reveal);s.Properties.SetFloat("_Opacity",1);s.Renderer.SetPropertyBlock(s.Properties);s.Rift.SetPropertyBlock(s.Properties);s.Stars.SetPropertyBlock(s.Properties);if((s.Skin.Index==6||s.Skin.Index==7||s.Skin.Index==8)&&s.View!=null)s.Rift.transform.position=s.View.Skin.HeadWorld;float open=s.Impact?Mathf.Sin(Mathf.SmoothStep(0,1,t)*Mathf.PI):s.Returning?Mathf.Sin(Mathf.SmoothStep(0,1,t)*Mathf.PI):Mathf.SmoothStep(0,1,Mathf.Clamp01(s.Time/.1f))*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((s.Time-.45f)/.35f)));s.Rift.transform.localScale=(s.Skin.Index==5||s.Skin.Index==6||s.Skin.Index==7||s.Skin.Index==8)?new Vector3(1.25f*open,.95f*open,1):s.Skin.Index==9?new Vector3(open*(s.Impact?.7f:2),open*(s.Impact?.7f:2),1):new Vector3(.18f*open,.75f*open,1);}
        /// <summary>Eased follow-through only; the signature action itself is at most 0.8 seconds.</summary>
        public void Tick(float dt)
        {for(int i=0;i<slots.Length;i++){var s=slots[i];if(!s.Active)continue;s.Time+=dt;if(s.Time>=s.Lead+s.Duration){Release(s);s.Active=false;s.Renderer.enabled=s.Rift.enabled=s.Stars.enabled=false;}else Apply(s);}}
        /// <summary>Immediately clears board-changing residues on replacement, undo and completion.</summary>
        public void Clear(){for(int i=0;i<slots.Length;i++){Release(slots[i]);slots[i].Active=false;if(slots[i].Renderer!=null)slots[i].Renderer.enabled=false;if(slots[i].Rift!=null)slots[i].Rift.enabled=false;if(slots[i].Stars!=null)slots[i].Stars.enabled=false;}}
        /// <summary>Releases only meshes owned by this pool.</summary>
        public void Dispose(){Clear();for(int i=0;i<slots.Length;i++)if(slots[i].Mesh!=null){if(Application.isPlaying){Object.Destroy(slots[i].Mesh);Object.Destroy(slots[i].StarsMesh);}else{Object.DestroyImmediate(slots[i].Mesh);Object.DestroyImmediate(slots[i].StarsMesh);}}}
    }
}
