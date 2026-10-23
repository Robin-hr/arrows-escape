#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ArrowEscape.Core;
using ArrowEscape.Data;
using Unity.Profiling;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Opt-in Editor diagnostic, with no real save access or effect on player builds.</summary>
    public sealed class RareThemeBenchmarkRunner : MonoBehaviour
    {
        [Serializable] private sealed class Row{public string Workload;public bool Light;public double MainMs,TickMs,GpuMs,P95MainMs;public long DrawCalls,GcBytes,PeakParticles;}
        [Serializable] private sealed class Report{public int Width=1080,Height=1920,Arrows=100;public bool GpuFeatureEnabled,GpuSamplesAvailable,TickValid,MainValid,DrawValid;public Row[] Rows;}
        private readonly long[] mainSamples=new long[120],tickSamples=new long[120],drawSamples=new long[120],gcSamples=new long[120];
        private readonly double[] gpuSamples=new double[120];
        private readonly FrameTiming[] timing=new FrameTiming[1];
        private readonly List<Row> rows=new List<Row>(67);
        private readonly string[] themes={"paper","grad_midnight","grad_aurora","grad_sunset","geo_crystal","geo_neon","geo_gold"};
        private ThemeBackdrop backdrop;
        private string phaseName;private bool flightPhase;
        private readonly ArrowView[] moving=new ArrowView[12];
        private static readonly ProfilerMarker marker=new ProfilerMarker(ProfilerCategory.Scripts,"RareTheme.Tick");
        private ProfilerRecorder main,tick,draw,gc;
        private BoardView board;
        private SkinDirector director;
        private Camera camera;
        private RenderTexture target;
        private int phase=-1,frame=-60,sample,previousVsync,previousRate;
        private long peakParticles;
        private bool hasGpu;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Begin()
        {
            if(!UnityEditor.SessionState.GetBool("RareThemeBenchmark",false))return;UnityEditor.SessionState.SetBool("RareThemeBenchmark",false);
            var cameraHost=new GameObject("Inferno benchmark camera",typeof(Camera));cameraHost.tag="MainCamera";var boot=new GameObject("Inferno benchmark memory game").AddComponent<GameBootstrap>();boot.Initialize(new MemoryProgressStore(),new MemorySettingsStore(),new MemoryInventoryStore());boot.enabled=false;boot.gameObject.AddComponent<RareThemeBenchmarkRunner>();
        }
        private void Start()
        {
            previousVsync=QualitySettings.vSyncCount;previousRate=Application.targetFrameRate;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;Application.runInBackground=true;
            board=GetComponent<BoardView>();director=gameObject.AddComponent<SkinDirector>();director.Bind(board,GetComponent<GameController>());director.enabled=false;director.SetLodOverride(false);camera=Camera.main;
            var model=new Board(40,20);for(int i=0;i<100;i++){int x=i%10*4,y=i/10*2;model.TryAddArrow(new Arrow(i+1,new[]{new Cell(x,y),new Cell(x+1,y),new Cell(x+2,y),new Cell(x+3,y),new Cell(x+3,y+1),new Cell(x+2,y+1),new Cell(x+1,y+1),new Cell(x,y+1)},Direction.Left),out _);}GetComponent<GameController>().StartLevel(model);for(int i=0;i<12;i++)board.TryGetArrowView(i+1,out moving[i]);
            target=new RenderTexture(1080,1920,16);target.Create();camera.targetTexture=target;camera.orthographic=true;camera.orthographicSize=45;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;
            main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",128);tick=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"RareTheme.Tick",128);draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",128);gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",128);
        }
        private void Update()
        {
            frame++;if(frame<0)return;
            if(frame==0){if(phase<0){foreach(var other in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(other!=camera)other.enabled=false;foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.gameObject.SetActive(false);board.SetVisible(true);GetComponent<GameController>().SetPaused(false);}StartPhase();}
            FrameTimingManager.CaptureFrameTimings();
            if(frame>30&&sample<120)
            {mainSamples[sample]=main.LastValue;tickSamples[sample]=tick.LastValue;drawSamples[sample]=draw.LastValue;gcSamples[sample]=gc.LastValue;if(FrameTimingManager.GetLatestTimings(1,timing)>0){gpuSamples[sample]=timing[0].gpuFrameTime;if(timing[0].gpuFrameTime>0)hasGpu=true;}sample++;}
            if(flightPhase)
            {
                float t=(frame%30)/29f;for(int i=0;i<12;i++){moving[i].Path.SetDistance(moving[i].Path.ExitDistance*.3f*t*t);if(frame%30==0)director.PlayFlight(moving[i]);}
            }
            using(marker.Auto()){director.Tick(Time.unscaledDeltaTime);backdrop?.Tick(Time.unscaledDeltaTime);}peakParticles=Math.Max(peakParticles,SkinParticlePool.GlobalLiveCount+ElementalSignatures.ActiveGlassFragments);
            if(sample==120){rows.Add(new Row{Workload=phaseName,Light=phase==0,MainMs=Median(mainSamples)*.000001,TickMs=Median(tickSamples)*.000001,DrawCalls=Median(drawSamples),GcBytes=Median(gcSamples),GpuMs=GpuMedian(),P95MainMs=mainSamples[114]*.000001,PeakParticles=peakParticles});frame=-1;sample=0;if(phase==66)Finish();}
        }
        private void StartPhase()
        {
            phase++;peakParticles=0;director.Clear();
            for(int i=0;i<12;i++)moving[i].Path.SetDistance(0);
            string skin="classic",theme="paper";flightPhase=false;
            if(phase>0&&phase<=6)theme=themes[phase];
            if(phase>=7){int n=phase-7;skin=ArrowSkinRegistry.All[4+n/12].Id;theme=themes[1+(n%12)/2];flightPhase=n%2==1;}
            phaseName=skin+" / "+theme+(flightPhase?" exit":" rest");
            board.SetTheme(theme);board.SetNormalArrowColor(ThemeDesign.ClassicInk(theme));board.SetArrowSkin(skin);backdrop=GetComponent<ThemeBackdrop>();if(backdrop!=null)backdrop.enabled=false;
            camera.backgroundColor=ThemeDesign.Background(theme);director.SetLodOverride(false);
            if(flightPhase)for(int i=0;i<12;i++)director.PlayFlight(moving[i]);
        }
        private static long Median(long[] data){Array.Sort(data);return data[60];}
        private double GpuMedian(){Array.Sort(gpuSamples);return gpuSamples[60];}
        private void Finish()
        {
            Directory.CreateDirectory("Logs/SkinShots");File.WriteAllText("Logs/SkinShots/rare-theme-benchmark.json",JsonUtility.ToJson(new Report{GpuFeatureEnabled=FrameTimingManager.IsFeatureEnabled(),GpuSamplesAvailable=hasGpu,TickValid=tick.Valid,MainValid=main.Valid,DrawValid=draw.Valid,Rows=rows.ToArray()},true));enabled=false;main.Dispose();tick.Dispose();draw.Dispose();gc.Dispose();QualitySettings.vSyncCount=previousVsync;Application.targetFrameRate=previousRate;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-rareThemeBenchmark")>=0)UnityEditor.EditorApplication.Exit(0);else UnityEditor.EditorApplication.isPlaying=false;
        }
        private void OnDestroy(){if(target!=null){if(camera!=null)camera.targetTexture=null;target.Release();DestroyImmediate(target);}}
    }
}
#endif
