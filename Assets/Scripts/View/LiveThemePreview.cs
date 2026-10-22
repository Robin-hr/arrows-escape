using ArrowEscape.Core;
using UnityEngine;
using UnityEngine.Rendering;
namespace ArrowEscape.View
{
    /// <summary>Isolated live mini-render shared by shop themes and the menu background interface.</summary>
    public sealed class LiveThemePreview : MonoBehaviour
    {
        private Camera camera;
        private Vector2 origin;
        private ArrowView sample;
        private float clock;
        /// <summary>Disable when an owning menu drives explicit ticks.</summary>
        public bool AutoTick {get;set;}=true;
        private ThemeBackdrop backdrop;
        private RenderPipeline.StandardRequest request;
        /// <summary>Owned live target fills the complete preview rectangle.</summary>
        public RenderTexture Texture {get;private set;}
        /// <summary>Creates reusable layers at a private preview location.</summary>
        public void Initialize(Vector2 origin,int width=512,int height=320)
        {
            this.origin=origin;
            Texture=new RenderTexture(width,height,16){name="Live theme preview",hideFlags=HideFlags.HideAndDontSave};Texture.Create();request=new RenderPipeline.StandardRequest{destination=Texture};
            var go=new GameObject("Theme preview camera");go.transform.SetParent(transform,false);camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=new Vector3(origin.x,origin.y,-10);camera.cullingMask=1<<28;camera.clearFlags=CameraClearFlags.SolidColor;camera.targetTexture=Texture;camera.depth=-30;
            backdrop=gameObject.AddComponent<ThemeBackdrop>();backdrop.Bind(camera);backdrop.enabled=false;SetLayers(transform);
        }
        private static void SetLayers(Transform node){node.gameObject.layer=28;for(int i=0;i<node.childCount;i++)SetLayers(node.GetChild(i));}
        /// <summary>Shows the selected live theme without changing the player's equipment.</summary>
        public void Show(string id){backdrop.SetTheme(id);camera.backgroundColor=ThemeDesign.Background(id);}
        /// <summary>Shows a bent sample with the player's equipped arrow, without modifying gameplay.</summary>
        public void ShowSample(string arrowId)
        {
            if(sample!=null)DestroyImmediate(sample.gameObject);backdrop.SetPreviewBoard(new Bounds(origin,new Vector3(7,4,0)));
            var model=new Arrow(1,new[]{new Cell(0,0),new Cell(1,0),new Cell(2,0),new Cell(2,1),new Cell(3,1),new Cell(4,1)},Direction.Right);
            var go=new GameObject("Theme detail sample");go.transform.SetParent(transform,false);sample=go.AddComponent<ArrowView>();sample.Initialize(model,null);sample.InitializePath(7,4,SkinMaterials.Glow(true));sample.SetBaseWorldPosition(origin+new Vector2(2,1));sample.BaseColor=arrowId=="classic"?new Color32(233,238,242,255):ArrowTint.For(arrowId,ThemeDesign.Background(backdrop.ThemeId));sample.SetSkin(arrowId,false);SetLayers(go.transform);
        }
        /// <summary>Explicit deterministic preview frame for tests and captures.</summary>
        public void Tick(float dt){backdrop.Tick(dt);clock+=dt;if(sample!=null&&sample.Skin!=null){sample.Skin.SetClock(clock);sample.Skin.Elemental?.Tick(dt);sample.Skin.Inferno?.Tick(dt);sample.Skin.Rare?.Tick(dt);}}
        /// <summary>Renders the current state in Edit Mode.</summary>
        public void RenderNow(){if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();}
        private void Update(){if(Application.isPlaying&&AutoTick)Tick(Time.unscaledDeltaTime);}
        private void OnDestroy(){if(Texture==null)return;camera.targetTexture=null;Texture.Release();if(Application.isPlaying)Destroy(Texture);else DestroyImmediate(Texture);}
    }
}
