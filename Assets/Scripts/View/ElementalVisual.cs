using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>One parameterized procedural renderer for ice, contained plasma and shadow smoke.</summary>
    public sealed class ElementalVisual : MonoBehaviour
    {
        private readonly MeshRenderer[] renderers=new MeshRenderer[4];
        private MaterialPropertyBlock properties;
        private Mesh quad,bodyMesh;
        private RareRibbonSurface rareSurface;
        private RareCapsSurface rareCaps;
        private readonly MeshFilter[] filters=new MeshFilter[4];
        private MeshRenderer tail;
        private PathRibbon ribbon;
        private Vector2[] tags;
        private float lastDistance=float.NaN;
        private int seed;
        private float impact,flightTime;
        private bool flying,returning,visible=true;
        /// <summary>Visual anticipation never holds up board logic.</summary>
        public bool IsFlying=>flying;
        /// <summary>Elapsed visual time bounds an edge burst to the same signature window.</summary>
        public float FlightElapsed=>flightTime;
        /// <summary>Starts a fresh visual signature, never a gameplay move.</summary>
        public void BeginFlight(){flying=true;returning=false;flightTime=0;}
        /// <summary>Reverse travel ends at the exact zero-distance rest state.</summary>
        public void BeginReturn(){flying=returning=true;flightTime=0;}
        /// <summary>Restores idle after the moving window finishes.</summary>
        public void EndFlight(){flying=returning=false;flightTime=0;}
        /// <summary>Small name-defining blocked bite, surge or dark pulse.</summary>
        public void Blocked(){impact=.2f;}
        /// <summary>Prepared timers only; explicit ticks remain usable while paused.</summary>
        public void Tick(float dt){impact=Mathf.Max(0,impact-dt);if(flying)flightTime+=dt;}
        /// <summary>Reuses the existing ribbon; only the independent head quad is owned here.</summary>
        public void Initialize(Mesh mesh,PathRibbon path,int arrowId,bool rareRecipe=false)
        {
            if(rareRecipe){rareSurface=new RareRibbonSurface(path);rareCaps=new RareCapsSurface();}
            seed=ArrowEscape.Core.SkinEffectRules.Lightning(arrowId,0,0);ribbon=path;if(!rareRecipe){tags=new Vector2[path.Vertices.Length];bodyMesh=new Mesh{name="Elemental seam-free ribbon",hideFlags=HideFlags.HideAndDontSave};bodyMesh.MarkDynamic();UpdateGeometry(0);bodyMesh.triangles=path.Triangles;bodyMesh.bounds=mesh.bounds;}
            properties=new MaterialPropertyBlock();quad=new Mesh{name="Elemental head field",hideFlags=HideFlags.HideAndDontSave};
            quad.vertices=new[]{new Vector3(-1.5f,-.85f),new Vector3(.25f,-.85f),new Vector3(.25f,.85f),new Vector3(-1.5f,.85f)};
            quad.uv=new[]{new Vector2(-1.5f,-.85f),new Vector2(.25f,-.85f),new Vector2(.25f,.85f),new Vector2(-1.5f,.85f)};quad.triangles=new[]{0,1,2,0,2,3};
            for(int i=0;i<4;i++){var go=new GameObject(i==1?"Elemental SDF head":"Elemental depth "+i);go.transform.SetParent(transform,false);filters[i]=go.AddComponent<MeshFilter>();filters[i].sharedMesh=i==1?quad:bodyMesh;renderers[i]=go.AddComponent<MeshRenderer>();}
            var feathers=new GameObject("Frozen crystal fletching");feathers.transform.SetParent(transform,false);feathers.AddComponent<MeshFilter>().sharedMesh=quad;tail=feathers.AddComponent<MeshRenderer>();tail.enabled=false;
        }
        /// <summary>Hides all recipe layers when equipment changes.</summary>
        public void SetVisible(bool value){if(visible==value)return;visible=value;for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=value;if(tail!=null&&!value)tail.enabled=false;}
        /// <summary>One appearance writer for every procedural layer and state overlay.</summary>
        public void Apply(ArrowSkin skin,Vector2 head,Vector2 direction,float distance,float length,float clock,bool light,bool lod,bool feedback,Color tint,float alpha,int sorting,float translation=0)
        {
            bool rebuilt=skin.IsRare;if(rebuilt)rareSurface.Build(distance);else UpdateGeometry(distance);
            for(int i=0;i<4;i++)if(i!=1)filters[i].sharedMesh=rebuilt?rareSurface.Mesh:bodyMesh;
            if(returning&&distance<=0)EndFlight();
            properties.Clear();properties.SetFloat("_Clock",clock);properties.SetFloat("_Seed",seed);properties.SetFloat("_WindowStart",distance+translation);properties.SetFloat("_BodyLength",length);properties.SetFloat("_Lod",lod?1:0);
            properties.SetFloat("_State",feedback?1:0);properties.SetColor("_StateTint",tint);properties.SetFloat("_Opacity",alpha);
            properties.SetFloat("_Blocked",impact/.2f);properties.SetFloat("_Flying",flying?1:0);properties.SetFloat("_SignatureTime",flightTime);
            renderers[1].transform.localPosition=head;renderers[1].transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            renderers[1].transform.localScale=Vector3.one;
            if(rebuilt){Vector2 tailPoint=ribbon.Point(distance),tailDirection=(ribbon.Point(distance+.01f)-tailPoint).normalized;rareCaps.Build(head,direction,tailPoint,tailDirection,length>=1);filters[1].sharedMesh=rareCaps.Mesh;renderers[1].transform.localPosition=Vector3.zero;renderers[1].transform.localRotation=Quaternion.identity;}else filters[1].sharedMesh=quad;

            if(rebuilt)renderers[2].transform.localPosition=new Vector3(0,0,.015f);else renderers[2].transform.localPosition=Vector3.zero;
            for(int i=0;i<4;i++){renderers[i].enabled=rebuilt?i<2||i==2&&(!lod||flying||feedback):i<2||!lod||flying||feedback;renderers[i].sharedMaterial=skin.IsRare?RareMaterials.Get(skin.Index,i,light):ElementalMaterials.Get(skin.Index,i,light);renderers[i].SetPropertyBlock(properties);renderers[i].sortingOrder=sorting;}
            tail.enabled=skin.Index==0&&!lod;if(tail.enabled){Vector2 p=ribbon.Point(distance),d=(ribbon.Point(distance+.01f)-p).normalized;tail.transform.localPosition=p+d*.7f;tail.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);tail.transform.localScale=new Vector3(.7f,.65f,1);properties.SetFloat("_Tail",1);tail.sharedMaterial=ElementalMaterials.Get(0,1,light);tail.SetPropertyBlock(properties);tail.sortingOrder=sorting;}
        }
        private void UpdateGeometry(float distance)
        {
            if(distance==lastDistance)return;lastDistance=distance;
            for(int i=0;i<tags.Length;i++){tags[i]=ribbon.SkinTags[i];if(tags[i].x!=1)continue;float u=distance+ribbon.UV[i].x;Vector2 before=(ribbon.Point(u)-ribbon.Point(Mathf.Max(0,u-.05f))).normalized,after=(ribbon.Point(u+.05f)-ribbon.Point(u)).normalized;if(u<.001f||Vector2.Dot(before,after)>.995f)tags[i].y=1;}
            bodyMesh.vertices=ribbon.Vertices;bodyMesh.uv=ribbon.UV;bodyMesh.uv2=ribbon.Edges;bodyMesh.uv3=tags;
        }
        private void OnDestroy(){rareSurface?.Dispose();rareCaps?.Dispose();if(quad!=null){if(Application.isPlaying)Destroy(quad);else DestroyImmediate(quad);}if(bodyMesh!=null){if(Application.isPlaying)Destroy(bodyMesh);else DestroyImmediate(bodyMesh);}}
    }
}
