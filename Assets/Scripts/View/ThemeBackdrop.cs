using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Three GPU-animated parallax layers and a soft board panel, with no CPU particle simulation.</summary>
    public sealed class ThemeBackdrop : MonoBehaviour
    {
        private static readonly ThemeBackdrop[] instances=new ThemeBackdrop[32];
        private int registrySlot=-1;
        private readonly MeshRenderer[] layers=new MeshRenderer[3];
        private readonly Material[] materials=new Material[3];
        private MaterialPropertyBlock properties;
        private Camera camera;
        private Mesh quad;
        private MeshRenderer panel;
        private Material panelMaterial;
        private BoardView board;
        private GameController game;
        private float clock;
        private bool sampleBoard;private Bounds sampleBounds;
        /// <summary>Optional fixed board panel for a shop detail miniature.</summary>
        public void SetPreviewBoard(Bounds bounds){sampleBoard=true;sampleBounds=bounds;if(panelMaterial!=null)panelMaterial.SetFloat("_PreviewDots",1);Tick(0); }
        private int selected=-2;
        /// <summary>Equipped theme identity; Paper leaves the original presentation untouched.</summary>
        public string ThemeId {get;private set;}="paper";
        /// <summary>Number of preallocated analytical front-layer motes.</summary>
        public int ParticleCount=>selected<0||!BudgetVisible?0:Mathf.Min(selected==4?141:selected==5?3:117,ParticleAllowance());
        /// <summary>Three parallax draws plus one readability panel.</summary>
        public int DrawCount=>selected<0?0:4;
        /// <summary>Explicit theme clock for pause and allocation tests.</summary>
        public float Clock=>clock;
        private bool BudgetVisible=>gameObject.activeInHierarchy&&camera!=null&&camera.gameObject.activeInHierarchy&&(board==null||board.IsVisible);
        private int ParticleAllowance()
        {if(registrySlot<0)return 0;int active=0;for(int i=0;i<instances.Length;i++)if(instances[i]!=null&&instances[i].selected>=0&&instances[i].BudgetVisible)active++;return ThemeDesign.ParticleLimit/Mathf.Max(1,active);}
        /// <summary>Binds a camera and optionally a real play board; preview callers own their camera.</summary>
        public void Bind(Camera target,BoardView view=null,GameController controller=null)
        {
            camera=target;board=view;game=controller;if(properties==null)properties=new MaterialPropertyBlock();
            if(quad!=null)return;for(int i=0;i<instances.Length;i++)if(instances[i]==null){instances[i]=this;registrySlot=i;break;}
            quad=new Mesh{name="Theme unit quad",hideFlags=HideFlags.HideAndDontSave};quad.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,1,2,0,2,3};
            for(int i=0;i<3;i++)layers[i]=CreateLayer("Theme parallax "+i,-24+i);
            panel=CreateLayer("Board readability panel",-1);
        }
        private MeshRenderer CreateLayer(string name,int order)
        {var go=new GameObject(name);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=quad;var renderer=go.AddComponent<MeshRenderer>();renderer.sortingOrder=order;renderer.enabled=false;return renderer;}
        /// <summary>Changes only the visual recipe; no settings or progress data are written.</summary>
        public void SetTheme(string id)
        {
            ThemeId=ThemeDesign.Index(id)<0?"paper":id;int next=ThemeDesign.Index(ThemeId);if(next==selected)return;selected=next;clock=0;
            for(int i=0;i<3;i++){DestroyOwned(materials[i]);materials[i]=next<0?null:RareMaterials.Theme(next,i);layers[i].sharedMaterial=materials[i];layers[i].enabled=next>=0;}
            if(panelMaterial==null&&next>=0){panelMaterial=RareMaterials.Theme(next,0);panelMaterial?.SetFloat("_Mode",14);panel.sharedMaterial=panelMaterial;}
            panel.enabled=next>=0&&(board!=null||sampleBoard);Tick(0);
        }
        /// <summary>Advances prepared shader properties; no per-frame arrays, meshes, materials or particles are allocated.</summary>
        public void Tick(float dt)
        {
            if(selected<0||camera==null||!(dt>=0)||float.IsInfinity(dt))return;clock+=dt;
            float height=camera.orthographicSize*2,width=height*camera.aspect;Vector3 center=camera.transform.position;center.z=0;
            properties.SetFloat("_MoteBudget",ParticleAllowance());properties.SetFloat("_Clock",clock);properties.SetFloat("_Aspect",camera.aspect);
            for(int i=0;i<3;i++){layers[i].transform.position=center;layers[i].transform.localScale=new Vector3(width,height,1);layers[i].SetPropertyBlock(properties);}
            if(sampleBoard){panel.transform.position=sampleBounds.center;panel.transform.localScale=sampleBounds.size+new Vector3(.55f,.55f,1);panel.enabled=true;}
            if(board!=null){panel.transform.position=board.WorldBounds.center;panel.transform.localScale=board.WorldBounds.size+new Vector3(.55f,.55f,1);panel.enabled=board.IsVisible;}
        }
        /// <summary>Gameplay pause freezes motion; hidden boards suppress their backdrop.</summary>
        public void AdvanceFrame(float dt)
        {
            bool visible=board==null||board.IsVisible;
            for(int i=0;i<3;i++)if(layers[i]!=null)layers[i].enabled=selected>=0&&visible;
            if(panel!=null)panel.enabled=selected>=0&&(board!=null||sampleBoard)&&visible;
            if(visible&&(game==null||!game.IsPaused))Tick(dt);
        }
        private void Update(){if(Application.isPlaying)AdvanceFrame(Time.deltaTime);}
        private static void DestroyOwned(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy(){if(registrySlot>=0&&instances[registrySlot]==this)instances[registrySlot]=null;for(int i=0;i<3;i++)DestroyOwned(materials[i]);DestroyOwned(panelMaterial);DestroyOwned(quad);}
    }
}
