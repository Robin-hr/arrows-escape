using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>One board-wide ambient scheduler and bounded trail pool; never changes puzzle state.</summary>
    public sealed class SkinDirector : MonoBehaviour
    {
        private BoardView board;
        private GameController game;
        private AudioDirector audio;
        private Camera camera;
        private SkinParticlePool pool;
        private ElementalSignatures signatures;
        private DotAbsorption absorption;
        /// <summary>Prepared visual signature pool, independent of puzzle actions.</summary>
        public ElementalSignatures Signatures=>signatures;
        /// <summary>Re-captures exact dot colors after equipment changes.</summary>
        public void ClearDimming(){absorption?.Clear();absorption=null;}
        /// <summary>Captures the freshly styled dot colors.</summary>
        public void RefreshDimming(){absorption=board==null?null:new DotAbsorption(board);absorption?.Refresh(board.ArrowSkinId=="void_whisper",true);}
        private ArrowView[] views=System.Array.Empty<ArrowView>();
        private SeededRandom random;
        private float ambientTime,clock;
        private bool? lodOverride;
        private bool lod,hiddenCleared;
        private struct Trail {public ArrowView View;public float Time,Duration,EmitTime;public Vector2 Start;public bool EdgeFlared;}
        /// <summary>Development light fallback for Inferno; null uses its dark preference.</summary>
        public bool? InfernoLightOverride {get;private set;}
        /// <summary>Unequipping restores the normal Paper preference.</summary>
        public void ResetInfernoOverride(){InfernoLightOverride=null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            dark=false;
#endif
        }
        private readonly Trail[] trails=new Trail[12];
        private int nextTrail;
        /// <summary>Total ambient starts, for budget tests and profiling.</summary>
        public int AmbientStarts { get; private set; }
        /// <summary>Last deterministic ambient arrow id.</summary>
        public int LastAmbientId { get; private set; }=-1;
        /// <summary>Native particle total is capped structurally at 600.</summary>
        public int LiveParticles=>pool==null?0:pool.LiveCount;
        /// <summary>Fixed native emitter count.</summary>
        public int PoolCount=>pool==null?0:pool.PoolCount;
        /// <summary>Current dense-board mode.</summary>
        public bool IsLod=>lod;
        /// <summary>Shared GPU clock, frozen with gameplay pause.</summary>
        public float Clock=>clock;
        /// <summary>Binds once; cached board views refresh only on board replacement.</summary>
        public void Bind(BoardView view,GameController controller)
        {
            Unsubscribe();board=view;game=controller;audio=GetComponent<AudioDirector>();camera=Camera.main;
            if(pool==null)pool=new SkinParticlePool(transform);if(signatures==null)signatures=new ElementalSignatures(transform);random=new SeededRandom(7177);
            if(board!=null)board.BoardChanged+=Refresh;
            if(game!=null){game.ArrowExited+=OnExit;game.MoveBlocked+=OnBlocked;game.ArrowRestored+=OnUndo;game.LevelStarted+=Reset;game.BoardCleared+=OnCleared;}
            Refresh();
        }
        private void Refresh()
        {
            if(board==null)return;views=board.GetComponentsInChildren<ArrowView>();System.Array.Sort(views,(a,b)=>a.Arrow.Id.CompareTo(b.Arrow.Id));Clear();RefreshDimming();
        }
        private void OnCleared(){signatures?.Clear();absorption?.Clear();}
        private void Reset(){ambientTime=clock=0;AmbientStarts=0;LastAmbientId=-1;random=new SeededRandom(7177);Refresh();}
        /// <summary>Allows the gallery to force LOD, or null to restore physical-size detection.</summary>
        public void SetLodOverride(bool? value){lodOverride=value;ApplyLod();}
        private void ApplyLod()
        {
            bool value=lodOverride??(camera!=null&&Screen.height/(2*camera.orthographicSize*(Screen.dpi>0?Screen.dpi:160))<.30f);
            lod=value;if(board!=null&&board.TryGetBoard(out var current))for(int i=0;i<current.Arrows.Count;i++)if(board.TryGetArrowView(current.Arrows[i].Id,out var view)&&view!=null&&view.Skin!=null)view.Skin.SetLod(value);
        }
        private void OnExit(Arrow arrow)
        {if(audio==null)audio=GetComponent<AudioDirector>();if(board!=null&&board.TryGetReleasedView(arrow.Id,out var view)){PlayFlight(view);absorption?.Refresh(board.ArrowSkinId=="void_whisper");if(view.Skin!=null&&view.Skin.Skin!=null)audio?.PlaySkin(view.Skin.Skin.Index);}}
        private void OnBlocked(Arrow arrow,Arrow blocker)
        {if(audio==null)audio=GetComponent<AudioDirector>();if(board!=null&&board.TryGetArrowView(arrow.Id,out var view)&&view.Skin!=null&&view.Skin.Skin!=null){var skin=view.Skin;skin.Inferno?.Blocked();skin.Elemental?.Blocked();skin.Rare?.Blocked();if(skin.Skin.Index==3)absorption?.Pulse();if(board.TryGetBoard(out var model)){var cell=model.GetBlockingCell(arrow);if(cell.HasValue)signatures?.Blocked(view,board.CellToWorld(cell.Value.X,cell.Value.Y));}pool.Emit(skin.Skin,skin.HeadWorld,new Vector2(arrow.Direction.GetDx(),arrow.Direction.GetDy()),18,skin.IsLight);audio?.PlaySkin(skin.Skin.Index,true);}}
        private void OnUndo(Arrow arrow)
        {Refresh();ClearTrails();if(board!=null&&board.TryGetArrowView(arrow.Id,out var view)&&view.Skin!=null&&view.Skin.Skin!=null){view.Skin.Elemental?.BeginReturn();view.Skin.Rare?.BeginReturn();signatures?.ReturnRift(view);view.Skin.Inferno?.BeginReturn(view.Path!=null?view.Path.Distance:1);pool.Emit(view.Skin.Skin,view.Skin.HeadWorld,view.Skin.DirectionWorld,15,view.Skin.IsLight,true);}}
        /// <summary>Starts only visual flight on a released or gallery arrow.</summary>
        public void PlayFlight(ArrowView view)
        {
            if(view==null||view.Skin==null||view.Skin.Skin==null)return;int i=nextTrail++%trails.Length;
            trails[i]=new Trail{View=view,Start=view.Skin.Inferno!=null?view.Skin.TailWorld:view.Skin.HeadWorld,Duration=(view.Path!=null?AnimationMath.PathExitDuration(view.Path.ExitDistance):ViewTheme.SlideDuration)*(view.Skin.Skin.Index==2?.5f:1)};
            float travel=0;if(view.Path==null&&board!=null&&board.TryGetBoard(out var model))travel=AnimationMath.SlideDistance(model.Width,model.Height,view.Arrow.X,view.Arrow.Y,view.Arrow.Direction);
            view.Skin.Inferno?.BeginFlight();view.Skin.Elemental?.BeginFlight();view.Skin.Rare?.BeginFlight();signatures?.Begin(view,trails[i].Duration,travel);view.Skin.SetFlight(0);pool.Emit(view.Skin.Skin,view.Skin.HeadWorld,view.Skin.Inferno!=null?view.Skin.DirectionWorld:Vector2.up,view.Skin.Inferno!=null?64:24,view.Skin.IsLight);
        }
        /// <summary>Gallery/test playback through the same live sound preference gate.</summary>
        public void PlayFlavor(ArrowSkin skin,bool blocked=false){if(skin!=null)audio?.PlaySkin(skin.Index,blocked);}
        /// <summary>Advances prepared effects and one shared GPU clock, never allocating.</summary>
        public void Tick(float dt)
        {
            if(!(dt>=0)||float.IsInfinity(dt)||pool==null)return;clock+=dt;Shader.SetGlobalFloat("_ArrowSkinTime",clock);ApplyLod();pool.Tick(dt);signatures?.Tick(dt);absorption?.Tick(dt);
            for(int n=0;n<views.Length;n++)if(views[n]!=null&&views[n].Skin!=null){views[n].Skin.Inferno?.Tick(dt);views[n].Skin.Elemental?.Tick(dt);views[n].Skin.Rare?.Tick(dt);}
            for(int i=0;i<trails.Length;i++)
            {
                var entry=trails[i];if(entry.View==null||entry.View.Skin==null||entry.View.Skin.Skin==null)continue;
                entry.Time+=dt;var skin=entry.View.Skin;skin.SetFlight(Mathf.Clamp01(entry.Time/entry.Duration));entry.EmitTime+=dt;if(!skin.Skin.IsElemental||skin.Skin.Index==1)pool.Beam(i,skin.Skin,entry.Start,skin.HeadWorld,skin.IsLight);
                if(entry.EmitTime>=.025f){entry.EmitTime=0;pool.Emit(skin.Skin,skin.HeadWorld,new Vector2(entry.View.Arrow.Direction.GetDx(),entry.View.Arrow.Direction.GetDy()),skin.Inferno!=null?4:skin.Skin.Index==2?8:5,skin.IsLight);}
                if(skin.Skin.IsProcedural&&!entry.EdgeFlared&&board!=null&&!board.WorldBounds.Contains(skin.HeadWorld)){pool.Emit(skin.Skin,skin.HeadWorld,skin.DirectionWorld,22,skin.IsLight);if(game==null||!game.IsCleared){var bounds=board.WorldBounds;signatures?.Edge(entry.View,new Vector2(Mathf.Clamp(skin.HeadWorld.x,bounds.min.x,bounds.max.x),Mathf.Clamp(skin.HeadWorld.y,bounds.min.y,bounds.max.y)));}entry.EdgeFlared=true;}
                if(entry.Time>=entry.Duration){skin.Inferno?.EndFlight();skin.Elemental?.EndFlight();skin.Rare?.EndFlight();skin.SetFlight(0);entry.View=null;}trails[i]=entry;
            }
            if(lod||views.Length==0)return;ambientTime+=dt;
            if(ambientTime<.4f)return;ambientTime%=.4f;int start=random.NextInt(views.Length);
            if(board.ArrowSkinId=="inferno_streak")
            {for(int n=0;n<views.Length;n++){var v=views[n];if(v==null||v.Skin?.Inferno==null||!board.TryGetArrowView(v.Arrow.Id,out var current)||current!=v)continue;pool.Emit(v.Skin.Skin,v.Skin.TailWorld,v.Skin.DirectionWorld,2,v.Skin.IsLight);if(random.NextInt(12)==0)pool.Emit(v.Skin.Skin,v.Skin.HeadWorld,v.Skin.DirectionWorld,1,v.Skin.IsLight);}AmbientStarts++;return;}
            for(int n=0;n<views.Length;n++)
            {var view=views[(start+n)%views.Length];if(view==null||view.Skin==null||view.Skin.Skin==null||!view.gameObject.activeInHierarchy||!board.TryGetArrowView(view.Arrow.Id,out var current)||current!=view)continue;pool.Emit(view.Skin.Skin,view.Skin.HeadWorld,Vector2.down,3,view.Skin.IsLight);AmbientStarts++;LastAmbientId=view.Arrow.Id;break;}
        }
        /// <summary>Automatic frames respect pause and hidden boards; explicit Tick remains testable.</summary>
        public void AdvanceFrame(float dt)
        {if(board==null||!board.IsVisible){if(!hiddenCleared){Clear();hiddenCleared=true;}return;}hiddenCleared=false;if(game==null||!game.IsPaused)Tick(dt);}
        private void Update(){if(Application.isPlaying)AdvanceFrame(Time.deltaTime);}
        /// <summary>Clears lingering effects without deleting pooled objects.</summary>
        public void Clear(){pool?.Clear();signatures?.Clear();absorption?.Clear();ClearTrails();for(int i=0;i<views.Length;i++)if(views[i]!=null&&views[i].Skin!=null)views[i].Skin.Inferno?.EndFlight();}
        private void ClearTrails(){for(int i=0;i<trails.Length;i++){if(trails[i].View!=null&&trails[i].View.Skin!=null){trails[i].View.Skin.Inferno?.EndFlight();trails[i].View.Skin.Elemental?.EndFlight();trails[i].View.Skin.Rare?.EndFlight();trails[i].View.Skin.SetFlight(0); }trails[i]=default;}}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool dark;
        /// <summary>Unsaved development comparison background.</summary>
        public bool IsDarkBoard=>dark;
        /// <summary>Development-only paper/dark comparison; never saved as a theme.</summary>
        [ContextMenu("Debug: Dark board")]
        public void DebugDarkBoard(){SetDarkBoard(board!=null&&ArrowSkinRegistry.Find(board.ArrowSkinId)?.PrefersDarkBoard==true?board.IsLightBoard:!dark);}
        /// <summary>Gallery light/dark control.</summary>
        public void SetDarkBoard(bool value)
        {dark=value;if(board!=null&&ArrowSkinRegistry.Find(board.ArrowSkinId)?.PrefersDarkBoard==true)InfernoLightOverride=!value;if(camera==null)camera=Camera.main;if(camera!=null)camera.backgroundColor=value?new Color32(14,17,22,255):ViewTheme.Background;if(board!=null)board.SetArrowSkin(board.ArrowSkinId,!value);}
#endif
        private void Unsubscribe()
        {if(board!=null)board.BoardChanged-=Refresh;if(game!=null){game.ArrowExited-=OnExit;game.MoveBlocked-=OnBlocked;game.ArrowRestored-=OnUndo;game.LevelStarted-=Reset;game.BoardCleared-=OnCleared;}}
        private void OnDestroy(){Unsubscribe();Clear();pool?.Dispose();signatures?.Dispose();}
    }
}
