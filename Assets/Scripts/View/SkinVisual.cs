using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Optional mesh/art presentation. The owner keeps its classic state adapter and sole Apply writer.</summary>
    public sealed class SkinVisual : MonoBehaviour
    {
        private ArrowView owner;
        private Transform visuals;
        private MeshRenderer body,halo;
        private SpriteRenderer head,tail;
        private Mesh singleMesh;
        private PathRibbon single;
        private InfernoVisual inferno;
        private ElementalVisual elemental;
        private ElementalVisual rare;
        /// <summary>Shared procedural Rare renderer; no extracted art or reference pixels.</summary>
        public ElementalVisual Rare=>Skin!=null&&Skin.IsRare?rare:null;
        /// <summary>Shared procedural crystal, energy or smoke layer.</summary>
        public ElementalVisual Elemental=>Skin!=null&&Skin.IsElemental&&Skin.Index!=1?elemental:null;
        /// <summary>Procedural fire presentation, absent on every other skin.</summary>
        public InfernoVisual Inferno=>Skin!=null&&Skin.Index==1?inferno:null;
        private MaterialPropertyBlock bodyProperties,haloProperties,headProperties,tailProperties;
        private float flight,opacity=1,clock=-1;
        private bool activeState,light,lod;
        private Color stateTint;
        private int order;
        /// <summary>Current recipe; null leaves Classic or tint fallback intact.</summary>
        public ArrowSkin Skin { get; private set; }
        /// <summary>Whether the light-board alpha blend is selected.</summary>
        public bool IsLight=>light;
        /// <summary>Dense-board simplified presentation.</summary>
        public bool IsLod=>lod;
        /// <summary>Visible head in world space, including snake travel.</summary>
        public Vector2 HeadWorld=>visuals==null?owner.CurrentWorldPosition:(Vector2)visuals.TransformPoint(HeadLocal());
        /// <summary>Current tail position, including reverse travel.</summary>
        public Vector2 TailWorld=>visuals==null?owner.CurrentWorldPosition:(Vector2)visuals.TransformPoint(TailLocal());
        /// <summary>Forward head tangent in world space.</summary>
        public Vector2 DirectionWorld=>visuals==null?Vector2.up:(Vector2)visuals.TransformDirection(HeadDirection());
        /// <summary>Attaches once and reuses all child renderers on equipment changes.</summary>
        public void Configure(ArrowView view,ArrowSkin skin,bool lightBoard)
        {
            owner=view;Skin=skin;light=lightBoard;owner.Path?.SetDedicatedSkinGeometry(skin?.IsRare==true);
            if(skin==null){if(visuals!=null)visuals.gameObject.SetActive(false);if(owner.Path!=null)owner.Path.SetClassicVisible(true);else owner.SpriteRenderer.enabled=true;return;}
            if(visuals==null)Create();
            if(skin.Index==1&&inferno==null){inferno=visuals.gameObject.AddComponent<InfernoVisual>();inferno.Initialize(owner.Path!=null?owner.Path.RibbonMesh:singleMesh,owner.Path!=null?owner.Path.Ribbon:single);}
            if(inferno!=null)inferno.enabled=skin.Index==1;
            if(skin.IsElemental&&skin.Index!=1&&elemental==null){elemental=visuals.gameObject.AddComponent<ElementalVisual>();elemental.Initialize(owner.Path!=null?owner.Path.RibbonMesh:singleMesh,owner.Path!=null?owner.Path.Ribbon:single,owner.Arrow.Id);}
            if(skin.IsRare&&rare==null){rare=visuals.gameObject.AddComponent<ElementalVisual>();rare.Initialize(owner.Path!=null?owner.Path.RibbonMesh:singleMesh,owner.Path!=null?owner.Path.Ribbon:single,owner.Arrow.Id,true);}
            visuals.gameObject.SetActive(true);if(owner.Path!=null)owner.Path.SetClassicVisible(false);else owner.SpriteRenderer.enabled=false;
            body.sharedMaterial=SkinMaterials.Ribbon(skin,light);halo.sharedMaterial=SkinMaterials.Glow(light);head.sharedMaterial=tail.sharedMaterial=SkinMaterials.Art(light);
            var resources=ArrowSkinResources.Load();var art=resources==null?null:resources.Find(skin.Id);
            head.sprite=art?.Head;tail.sprite=art?.Tail;
            RefreshArt(head,headProperties);RefreshArt(tail,tailProperties);Apply();
        }
        private void Create()
        {
            bodyProperties=new MaterialPropertyBlock();haloProperties=new MaterialPropertyBlock();headProperties=new MaterialPropertyBlock();tailProperties=new MaterialPropertyBlock();
            visuals=new GameObject("Arrow skin presentation").transform;visuals.SetParent(transform,false);
            if(owner.Path==null)visuals.localScale=Vector3.one/ViewTheme.ArrowFillRatio;
            Mesh mesh;
            if(owner.Path!=null){mesh=owner.Path.RibbonMesh;owner.Path.GeometryChanged+=OnGeometry;}
            else
            {
                single=new PathRibbon(new[]{new Vector2(0,-.42f),new Vector2(0,.42f),new Vector2(0,2)},.84f);single.Build(0);
                singleMesh=new Mesh{name="Skin single-cell ribbon",hideFlags=HideFlags.HideAndDontSave};singleMesh.vertices=single.Vertices;singleMesh.triangles=single.Triangles;singleMesh.uv=single.UV;singleMesh.uv2=single.Edges;singleMesh.uv3=single.SkinTags;var colors=new Color[single.Vertices.Length];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;singleMesh.colors=colors;mesh=singleMesh;
            }
            halo=MeshChild("Soft halo",mesh);body=MeshChild("Patterned body",mesh);
            head=SpriteChild("Reference head");tail=SpriteChild("Reference tail");
        }
        private MeshRenderer MeshChild(string name,Mesh mesh)
        {var go=new GameObject(name);go.transform.SetParent(visuals,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;return go.AddComponent<MeshRenderer>();}
        private SpriteRenderer SpriteChild(string name)
        {var go=new GameObject(name);go.transform.SetParent(visuals,false);return go.AddComponent<SpriteRenderer>();}
        /// <summary>Receives the owner's feedback layer without changing its base tint or geometry.</summary>
        public void SetAppearance(Color tint,bool feedback,float alpha,int sortingOrder)
        {stateTint=tint;activeState=feedback;opacity=alpha;order=sortingOrder;Apply();}
        /// <summary>Flight state only; the existing animator owns positional movement.</summary>
        public void SetFlight(float progress){flight=Mathf.Clamp01(progress);Apply();}
        /// <summary>Disables optional halos and tail art at physically dense cell sizes.</summary>
        public void SetLod(bool value){if(lod==value)return;lod=value;Apply();}
        /// <summary>Explicit preview clock; negative uses the board's shared paused clock.</summary>
        public void SetClock(float seconds){clock=seconds;Apply();}
        private Vector2 HeadLocal()=>owner.Path!=null?owner.Path.Ribbon.Point(owner.Path.Distance+owner.Path.Ribbon.BodyLength):new Vector2(0,.42f);
        private Vector2 TailLocal()=>owner.Path!=null?owner.Path.Ribbon.Point(owner.Path.Distance):new Vector2(0,-.42f);
        private Vector2 HeadDirection()
        {if(owner.Path==null)return Vector2.up;var r=owner.Path.Ribbon;float d=owner.Path.Distance+r.BodyLength;return (r.Point(d)-r.Point(Mathf.Max(0,d-.01f))).normalized;}
        private Vector2 TailDirection()
        {if(owner.Path==null)return Vector2.up;var r=owner.Path.Ribbon;float d=owner.Path.Distance;return (r.Point(d+.01f)-r.Point(d)).normalized;}
        private static void RefreshArt(SpriteRenderer renderer,MaterialPropertyBlock block)
        {block.Clear();if(renderer.sprite!=null)block.SetTexture("_MainTex",renderer.sprite.texture);block.SetFloat("_Art",1);block.SetColor("_Tint",Color.white);renderer.SetPropertyBlock(block);}
        private void OnGeometry(){if(Skin!=null)Apply();}
        private void Apply()
        {
            if(Skin==null||body==null)return;
            bool procedural=Skin.IsProcedural;body.enabled=halo.enabled=head.enabled=tail.enabled=!procedural;
            if(inferno!=null)inferno.SetVisible(Skin.Index==1);
            if(elemental!=null)elemental.SetVisible(Skin.IsElemental&&Skin.Index!=1);
            if(rare!=null)rare.SetVisible(Skin.IsRare);
            if(Skin.IsRare){rare.Apply(Skin,HeadLocal(),HeadDirection(),owner.Path!=null?owner.Path.Distance:0,owner.Path!=null?owner.Path.Ribbon.BodyLength:.84f,clock,light,lod,activeState,stateTint,opacity,order,owner.Path==null?owner.Offset.magnitude:0);return;}
            if(procedural&&Skin.Index!=1){elemental.Apply(Skin,HeadLocal(),HeadDirection(),owner.Path!=null?owner.Path.Distance:0,owner.Path!=null?owner.Path.Ribbon.BodyLength:.84f,clock,light,lod,activeState,stateTint,opacity,order,owner.Path==null?owner.Offset.magnitude:0);return;}
            if(procedural){inferno.Apply(HeadLocal(),HeadDirection(),owner.Path!=null?owner.Path.Distance:0,owner.Path!=null?owner.Path.Ribbon.BodyLength:.84f,owner.Path!=null&&inferno.IsFlying?Mathf.Max(flight,Mathf.Sqrt(Mathf.Clamp01(owner.Path.Distance/owner.Path.ExitDistance))):flight,clock,light,lod,activeState,stateTint,opacity,order);return;}
            bodyProperties.SetFloat("_State",activeState?1:0);bodyProperties.SetColor("_StateTint",stateTint);bodyProperties.SetFloat("_Opacity",opacity);bodyProperties.SetFloat("_Flight",flight);bodyProperties.SetFloat("_Lod",lod?1:0);bodyProperties.SetFloat("_Seed",SkinEffectRules.Lightning(owner.Arrow.Id,0,0));bodyProperties.SetFloat("_SkinClock",clock);body.SetPropertyBlock(bodyProperties);body.sortingOrder=order;
            haloProperties.SetFloat("_Seed",SkinEffectRules.Lightning(owner.Arrow.Id,0,0));haloProperties.SetFloat("_Shape",Skin.Index);haloProperties.SetFloat("_BodyLength",owner.Path!=null?owner.Path.Ribbon.BodyLength:.84f);haloProperties.SetColor("_Tint",Color.Lerp(Skin.Primary,Skin.Secondary,.4f));haloProperties.SetFloat("_Expansion",activeState?.085f:.11f);haloProperties.SetFloat("_Opacity",opacity*(light?.20f:.30f));haloProperties.SetFloat("_Pulse",Skin.Period);haloProperties.SetFloat("_State",activeState?1:0);haloProperties.SetColor("_StateTint",stateTint);haloProperties.SetFloat("_SkinClock",clock);halo.SetPropertyBlock(haloProperties);halo.sortingOrder=order;halo.enabled=!lod||activeState||flight>0;
            ApplyCap(head,headProperties,HeadLocal(),HeadDirection(),1.05f);
            ApplyCap(tail,tailProperties,TailLocal()+TailDirection()*.18f,TailDirection(),.60f);tail.enabled=(!lod||flight>0)&&tail.sprite!=null;
        }
        private void ApplyCap(SpriteRenderer renderer,MaterialPropertyBlock block,Vector2 point,Vector2 direction,float width)
        {
            if(renderer.sprite==null){renderer.enabled=false;return;}renderer.enabled=true;
            // Place the art's right edge at the logical head; both sheets are normalized to right-facing.
            Vector2 cap=point-direction*(width*.5f);renderer.transform.localPosition=new Vector3(cap.x,cap.y,-.012f);renderer.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);renderer.transform.localScale=Vector3.one*(width/renderer.sprite.bounds.size.x);
            block.SetFloat("_Flight",flight);block.SetFloat("_Shape",Skin.Index);block.SetFloat("_Opacity",opacity);block.SetFloat("_State",activeState?1:0);block.SetColor("_StateTint",stateTint);block.SetFloat("_SkinClock",clock);renderer.SetPropertyBlock(block);renderer.sortingOrder=order;
        }
        private void OnDestroy()
        {if(owner!=null&&owner.Path!=null)owner.Path.GeometryChanged-=OnGeometry;if(singleMesh!=null){if(Application.isPlaying)Destroy(singleMesh);else DestroyImmediate(singleMesh);}}
    }
}
