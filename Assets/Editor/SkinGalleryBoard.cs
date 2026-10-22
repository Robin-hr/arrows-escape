using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.Editor
{
    /// <summary>Ten simultaneous bent arrows, one per skin, isolated from the player level and saves.</summary>
    public sealed class SkinGalleryBoard
    {
        private readonly GameObject root;
        private readonly BoardView board;
        private readonly Camera camera;
        private readonly ArrowView[] arrows=new ArrowView[10];
        private readonly Vector2[] starts=new Vector2[10];
        private readonly float[] flights=new float[10];
        private readonly SkinParticlePool pool;
        private readonly ElementalSignatures signatures;
        private readonly Vector2 offset=new Vector2(26000,26000);
        private readonly AudioDirector audio;
        private bool dark=true,lod;
        private float clock,emit;
        /// <summary>Whole sample board, for the Editor window.</summary>
        public RenderTexture Texture {get;}
        /// <summary>Creates the gallery with no GameSession, inventory or progress interaction.</summary>
        public SkinGalleryBoard(AudioDirector sound)
        {
            audio=sound;root=new GameObject("Ten-skin gallery"){hideFlags=HideFlags.HideAndDontSave};board=root.AddComponent<BoardView>();
            var model=new Board(24,32);for(int i=0;i<ArrowSkinRegistry.All.Count;i++){int x=3+i%2*12,y=2+(4-i/2)*6;model.TryAddArrow(new Arrow(i+1,new[]{new Cell(x,y),new Cell(x+1,y),new Cell(x+1,y+1),new Cell(x+2,y+1),new Cell(x+3,y+1),new Cell(x+3,y+2),new Cell(x+4,y+2)},Direction.Right),out _);}
            board.Show(model);if(board.PathDots!=null)board.PathDots.transform.position=offset;
            for(int i=0;i<ArrowSkinRegistry.All.Count;i++){board.TryGetArrowView(i+1,out arrows[i]);arrows[i].Rebase(arrows[i].Arrow,arrows[i].BaseWorldPosition+offset,false);arrows[i].SetSkin(ArrowSkinRegistry.All[i].Id,false);}
            pool=new SkinParticlePool(root.transform);signatures=new ElementalSignatures(root.transform);SetLayers(root.transform);Texture=new RenderTexture(768,1056,16){name="Ten-skin sample board",hideFlags=HideFlags.HideAndDontSave};Texture.Create();var go=new GameObject("Gallery camera");go.transform.SetParent(root.transform,false);camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=17;camera.transform.position=new Vector3(offset.x,offset.y,-10);camera.targetTexture=Texture;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;SetDark(true);
        }
        private static void SetLayers(Transform node){node.gameObject.layer=29;for(int i=0;i<node.childCount;i++)SetLayers(node.GetChild(i));}
        /// <summary>Flies the selected visual out and back for comparison without touching the puzzle.</summary>
        public void Fly(int index){flights[index]=.0001f;starts[index]=arrows[index].Skin.HeadWorld;arrows[index].Skin.Inferno?.BeginFlight();arrows[index].Skin.Elemental?.BeginFlight();arrows[index].Skin.Rare?.BeginFlight();signatures.Begin(arrows[index],arrows[index].Skin.Skin.Index==2?.4f:.8f);audio?.PlaySkinPreview(ArrowSkinRegistry.All[index].Index);pool.Emit(ArrowSkinRegistry.All[index],arrows[index].Skin.HeadWorld,Vector2.right,20,!dark);}
        /// <summary>Changes the unsaved comparison background and halo blending.</summary>
        public void SetDark(bool value){dark=value;camera.backgroundColor=value?new Color32(14,17,22,255):ViewTheme.Background;for(int i=0;i<ArrowSkinRegistry.All.Count;i++)arrows[i].SetSkin(ArrowSkinRegistry.All[i].Id,!value);}
        /// <summary>Forces optional layer simplification.</summary>
        public void SetLod(bool value){lod=value;for(int i=0;i<ArrowSkinRegistry.All.Count;i++)arrows[i].Skin.SetLod(value);}
        /// <summary>Prepared timers and native particles only; no managed frame allocations.</summary>
        public void Tick(float dt)
        {
            clock+=dt;emit+=dt;pool.Tick(dt);signatures.Tick(dt);
            for(int i=0;i<ArrowSkinRegistry.All.Count;i++)
            {
                var view=arrows[i];view.Skin.Inferno?.Tick(dt);view.Skin.Elemental?.Tick(dt);view.Skin.Rare?.Tick(dt);view.Skin.SetClock(clock);if(flights[i]<=0)continue;flights[i]+=dt;float t=Mathf.Clamp01(flights[i]/(view.Skin.Skin.Index==2?.4f:.8f));view.Path.SetDistance(t*t*view.Path.ExitDistance);view.Skin.SetFlight(t);if(!view.Skin.Skin.IsElemental||view.Skin.Skin.Index==1)pool.Beam(i,ArrowSkinRegistry.All[i],starts[i],view.Skin.HeadWorld,!dark);
                if(emit>.035f)pool.Emit(ArrowSkinRegistry.All[i],view.Skin.HeadWorld,Vector2.right,5,!dark);
                if(t>=1){flights[i]=0;view.Skin.Inferno?.EndFlight();view.Skin.Elemental?.EndFlight();view.Skin.Rare?.EndFlight();view.Path.SetDistance(0);view.Skin.SetFlight(0);}
            }
            if(emit>.035f)emit=0;
        }
        /// <summary>Releases only the isolated gallery and its target.</summary>
        public void Dispose(){pool.Dispose();signatures.Dispose();Texture.Release();Object.DestroyImmediate(Texture);Object.DestroyImmediate(root);}
    }
}
