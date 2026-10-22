using ArrowEscape.Core;
using UnityEngine;
using UnityEngine.Rendering;
namespace ArrowEscape.View
{
    /// <summary>One isolated, reusable render-texture board for the selected shop skin.</summary>
    public sealed class LiveSkinPreview : MonoBehaviour
    {
        private Camera camera;
        private RenderPipeline.StandardRequest request;
        private ArrowView arrow;
        private SkinParticlePool particles;
        private ElementalSignatures signatures;
        /// <summary>Explicit signature starts for gallery verification.</summary>
        public ElementalSignatures Signatures=>signatures;
        private AudioDirector audio;
        private float clock,cycle,flight,emit,ambient;
        private bool flying,dark,waitingForReplay;
        private bool edgeFlared;
        private Bounds boardBounds;
        private string id;
        private Vector2 flightStart;
        private Vector2 origin=new Vector2(18000,18000);
        /// <summary>Prepared visual arrow for gallery captures and animation tests.</summary>
        public ArrowView View=>arrow;
        /// <summary>Detail previews replay; cards remain at rest.</summary>
        public bool AutoReplay {get;set;}=true;
        /// <summary>Live camera target, owned by this component.</summary>
        public RenderTexture Texture { get; private set; }
        /// <summary>Selected implemented recipe.</summary>
        public ArrowSkin Skin=>ArrowSkinRegistry.Find(id);
        /// <summary>Whether preview is on the reference dark board.</summary>
        public bool IsDark=>dark;
        /// <summary>Prepares a small dot board and one bent arrow; no player session or stores are touched.</summary>
        public void Initialize(AudioDirector director)=>Initialize(director,new Vector2(18000,18000));
        /// <summary>Explicit isolated capture location prevents overlap with an open shop preview.</summary>
        public void Initialize(AudioDirector director,Vector2 worldOrigin)
        {
            origin=worldOrigin;
            audio=director;Texture=new RenderTexture(640,360,16){name="Live arrow skin preview",hideFlags=HideFlags.HideAndDontSave};Texture.Create();request=new RenderPipeline.StandardRequest{destination=Texture};
            var go=new GameObject("Skin preview camera");go.transform.SetParent(transform,false);camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.4f;camera.transform.position=new Vector3(origin.x+.8f,origin.y,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.targetTexture=Texture;camera.depth=-20;camera.cullingMask=1<<29;
            particles=new SkinParticlePool(transform);signatures=new ElementalSignatures(transform);
            var material=SkinMaterials.Art(true);
            for(int y=-1;y<=1;y++)for(int x=-3;x<=3;x++){var dot=new GameObject("Preview dot");dot.layer=29;dot.transform.SetParent(transform,false);dot.transform.position=origin+new Vector2(x,y);dot.transform.localScale=Vector3.one*.06f;var renderer=dot.AddComponent<SpriteRenderer>();renderer.sprite=SpriteFactory.GetCellSprite();renderer.sharedMaterial=material;renderer.color=ViewTheme.CellDot;}
            SetDark(false);
        }
        /// <summary>Replaces preview art, never the real board or equipment.</summary>
        public void Show(string skinId)
        {
            var model=new Arrow(1,new[]{new Cell(0,0),new Cell(1,0),new Cell(1,1),new Cell(2,1),new Cell(3,1),new Cell(3,2),new Cell(4,2)},Direction.Right);
            ShowArrow(skinId,model,7,4,origin+new Vector2(2,1));
        }
        /// <summary>Reuses the same renderer for straight cards, the hero and bent boards.</summary>
        public void ShowArrow(string skinId,Arrow model,int width,int height,Vector2 headWorld)
        {
            id=skinId;cycle=flight=clock=0;flying=waitingForReplay=false;particles.Clear();signatures?.Clear();
            boardBounds=new Bounds(headWorld-new Vector2(model.X-(width-1)*.5f,model.Y-(height-1)*.5f),new Vector3(width,height,0));
            if(arrow!=null)DestroyImmediate(arrow.gameObject);
            var go=new GameObject("Bent preview arrow");go.transform.SetParent(transform,false);
            arrow=go.AddComponent<ArrowView>();arrow.Initialize(model,null);arrow.InitializePath(width,height,SkinMaterials.Glow(true));arrow.SetBaseWorldPosition(headWorld);arrow.SetSkin(id,!dark);SetLayers(transform);if(dark)camera.backgroundColor=Skin?.DarkBoardColor??new Color32(14,17,22,255);gameObject.SetActive(true);
        }
        /// <summary>Creates a six-cell right-facing procedural card without any image asset.</summary>
        public void ShowStraight(string skinId)
        {ShowArrow(skinId,new Arrow(1,new[]{new Cell(0,0),new Cell(1,0),new Cell(2,0),new Cell(3,0),new Cell(4,0),new Cell(5,0)},Direction.Right),9,3,origin+new Vector2(2.5f,0));SetFraming(origin,2.5f);}
        /// <summary>Capture-only camera framing, independent of gameplay camera fitting.</summary>
        public void SetFraming(Vector2 center,float halfHeight){camera.transform.position=new Vector3(center.x,center.y,-10);camera.orthographicSize=halfHeight;}
        /// <summary>Sets an exact local capture background.</summary>
        public void SetCaptureBackground(Color color){camera.backgroundColor=color;}
        /// <summary>Recreates a requested capture target outside animation paths.</summary>
        public void SetResolution(int width,int height){camera.targetTexture=null;Texture.Release();DestroyImmediate(Texture);Texture=new RenderTexture(width,height,16){hideFlags=HideFlags.HideAndDontSave};Texture.Create();camera.targetTexture=Texture;request.destination=Texture;}
        private static void SetLayers(Transform node){node.gameObject.layer=29;for(int i=0;i<node.childCount;i++)SetLayers(node.GetChild(i));}
        /// <summary>Switches light/dark locally, never changing saved theme or the game camera.</summary>
        public void SetDark(bool value)
        {dark=value;if(camera!=null)camera.backgroundColor=value?(Skin?.DarkBoardColor??new Color32(14,17,22,255)):ViewTheme.Background;if(arrow!=null)arrow.SetSkin(id,!value);}
        /// <summary>Restarts flight on demand and plays setting-gated texture under a preview note.</summary>
        public void Fly()
        {if(arrow==null||Skin==null)return;arrow.gameObject.SetActive(true);waitingForReplay=edgeFlared=false;flight=emit=0;flying=true;cycle=0;flightStart=Skin.PrefersDarkBoard?arrow.Skin.TailWorld:arrow.Skin.HeadWorld;arrow.Path.SetDistance(0);arrow.Skin.Inferno?.BeginFlight();arrow.Skin.Elemental?.BeginFlight();arrow.Skin.Rare?.BeginFlight();signatures?.Begin(arrow,Skin.Index==2?.4f:.8f);arrow.Skin.SetFlight(0);audio?.PlaySkinPreview(Skin.Index);particles.Emit(Skin,arrow.Skin.HeadWorld,Vector2.right,Skin.PrefersDarkBoard?64:22,!dark);}
        /// <summary>Advances shader clock, pooled trail and three-second automatic replay without allocations.</summary>
        public void Tick(float dt)
        {
            if(!(dt>=0)||float.IsInfinity(dt)||arrow==null||!gameObject.activeInHierarchy)return;
            clock+=dt;cycle+=dt;arrow.Skin.Inferno?.Tick(dt);arrow.Skin.Elemental?.Tick(dt);arrow.Skin.Rare?.Tick(dt);signatures?.Tick(dt);arrow.Skin.SetClock(clock);particles.Tick(dt);
            if(!flying){ambient+=dt;if(ambient>=.4f&&!waitingForReplay&&!arrow.Skin.IsLod){ambient=0;particles.Emit(Skin,Skin.PrefersDarkBoard?arrow.Skin.TailWorld:arrow.Skin.HeadWorld,Skin.PrefersDarkBoard?arrow.Skin.DirectionWorld:Vector2.down,2,!dark);}if(AutoReplay&&cycle>=3)Fly();return;}
            flight+=dt;float t=Mathf.Clamp01(flight/(Skin.Index==2?.4f:.8f));float motion=t;if(Skin.IsProcedural&&Skin.Index!=1){float duration=Skin.Index==2?.4f:.8f;float delay=(Skin.Index==2?.12f:(Skin.Index>=5&&Skin.Index<=9)?.24f:.1f)/duration;motion=Mathf.Clamp01((t-delay)/(1-delay));}arrow.Path.SetDistance(arrow.Path.ExitDistance*motion*motion);arrow.Skin.SetFlight(t);if(!Skin.IsElemental||Skin.Index==1)particles.Beam(0,Skin,flightStart,arrow.Skin.HeadWorld,!dark);emit+=dt;
            if(emit>=.025f){emit=0;particles.Emit(Skin,arrow.Skin.HeadWorld,Vector2.right,5,!dark);}
            if((Skin.Index==0||Skin.Index==5)&&!edgeFlared&&!boardBounds.Contains(arrow.Skin.HeadWorld)){edgeFlared=true;Vector2 point=boardBounds.ClosestPoint(arrow.Skin.HeadWorld);signatures.Edge(arrow,point);particles.Emit(Skin,point,arrow.Skin.DirectionWorld,22,!dark);}
            if(t>=1){flying=false;arrow.Path.SetDistance(0);arrow.Skin.Inferno?.EndFlight();arrow.Skin.Elemental?.EndFlight();arrow.Skin.Rare?.EndFlight();arrow.Skin.SetFlight(0);if(Skin.IsProcedural){waitingForReplay=true;arrow.gameObject.SetActive(false);}}
        }
        /// <summary>Forces preview detail without changing any game LOD policy.</summary>
        public void SetLod(bool value){if(arrow!=null&&arrow.Skin!=null)arrow.Skin.SetLod(value);}
        private int phase=-1;
        /// <summary>Card previews render at 30 Hz on alternating frames (see <see cref="PreviewThrottle"/>).</summary>
        public bool HalfRate{get;set;}
        private void Update()
        {
            if(!Application.isPlaying)return;Tick(Time.unscaledDeltaTime);
            if(camera==null)return;
            if(!HalfRate){if(!camera.enabled)camera.enabled=true;return;}
            if(phase<0)phase=PreviewThrottle.NextPhase();if(camera.enabled)camera.enabled=false;
            if(PreviewThrottle.ShouldRender(phase))RenderNow();
        }
        /// <summary>Explicit render for Editor comparisons; runtime uses the enabled target camera.</summary>
        public void RenderNow(){if(camera==null)return;if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();}
        private void OnDisable(){particles?.Clear();}
        private void OnDestroy(){particles?.Dispose();signatures?.Dispose();if(Texture!=null){if(camera!=null)camera.targetTexture=null;Texture.Release();if(Application.isPlaying)Destroy(Texture);else DestroyImmediate(Texture);}}
    }
}

