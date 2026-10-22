#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ArrowEscape.Core;
using ArrowEscape.Data;
using UnityEngine;
using Unity.Profiling;
namespace ArrowEscape.View
{
    /// <summary>Development profiler harness, activated only by the explicit Editor benchmark command.</summary>
    public sealed class SkinBenchmarkRunner : MonoBehaviour
    {
        [Serializable] private sealed class Measurement{public string Skin;public bool Light;public double TickMs,MainThreadMs;public long DrawCalls,GcBytes;}
        [Serializable] private sealed class Report{public int Width=1080,Height=1920,Arrows=100,SamplesPerPhase=100;public bool TickRecorderValid,MainRecorderValid,DrawRecorderValid;public Measurement[] Measurements;}
        private static readonly ProfilerMarker marker=new ProfilerMarker(ProfilerCategory.Scripts,"ArrowSkins.Tick");
        private ProfilerRecorder ticks,main,draw,gc;
        private readonly List<Measurement> rows=new List<Measurement>(21);
        private readonly long[] tickSamples=new long[100],mainSamples=new long[100],drawSamples=new long[100],gcSamples=new long[100];
        private GameBootstrap bootstrap;
        private BoardView board;
        private SkinDirector director;
        private Camera camera;
        private RenderTexture target;
        private bool busyMode;
        private readonly ArrowView[] moving=new ArrowView[12];
        private int phase=-1,frame=-60,sample;
        private int previousVsync;
        private float previousRate;
        /// <summary>Intercepts only this explicit diagnostic launch; all stores are memory-only.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Begin()
        {
            if(!UnityEditor.SessionState.GetBool("ArrowSkinBenchmark",false))return;
            UnityEditor.SessionState.SetBool("ArrowSkinBenchmark",false);
            var cameraHost=new GameObject("Skin profiler camera",typeof(Camera));cameraHost.tag="MainCamera";
            var fake=new GameObject("Skin profiler game").AddComponent<GameBootstrap>();fake.Initialize(new MemoryProgressStore(),new MemorySettingsStore(),new MemoryInventoryStore());fake.enabled=false;
            var host=new GameObject("Skin profiler (memory only)");host.AddComponent<SkinBenchmarkRunner>();
        }
        private void Start()
        {
            busyMode=Array.IndexOf(Environment.GetCommandLineArgs(),"-arrowSkinBusy")>=0;Application.runInBackground=true;previousVsync=QualitySettings.vSyncCount;previousRate=Application.targetFrameRate;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            camera=Camera.main;if(camera==null){var go=new GameObject("Profiler camera");camera=go.AddComponent<Camera>();go.tag="MainCamera";}
            var existing=FindFirstObjectByType<GameBootstrap>();bootstrap=existing!=null?existing:new GameObject("Profiler game").AddComponent<GameBootstrap>();
            if(!bootstrap.IsInitialized)bootstrap.Initialize(new MemoryProgressStore(),new MemorySettingsStore(),new MemoryInventoryStore());
            board=bootstrap.GetComponent<BoardView>();director=bootstrap.GetComponent<SkinDirector>()??bootstrap.gameObject.AddComponent<SkinDirector>();director.Bind(board,bootstrap.GetComponent<GameController>());director.enabled=false;director.SetLodOverride(false);
            var model=new Board(20,20);for(int i=0;i<100;i++){int x=i%10*2,y=i/10*2;model.TryAddArrow(new Arrow(i+1,new[]{new Cell(x,y),new Cell(x+1,y),new Cell(x+1,y+1)},Direction.Up),out _);}
            bootstrap.GetComponent<GameController>().StartLevel(model);board.SetVisible(true);for(int i=0;i<moving.Length;i++)board.TryGetArrowView(i+1,out moving[i]);
            target=new RenderTexture(1080,1920,16);target.Create();camera.targetTexture=target;camera.orthographic=true;camera.orthographicSize=20;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;
            ticks=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"ArrowSkins.Tick",128);main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",128);draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",128);gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",128);
        }
        private void Update()
        {
            frame++;
            // Initial menu Start order is irrelevant: the diagnostic disables only runtime UI/cameras.
            if(frame<0)return;
            if(frame==0){foreach(var other in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(other!=camera)other.enabled=false;foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.gameObject.SetActive(false);board.SetVisible(true);bootstrap.GetComponent<GameController>().SetPaused(false);SetPhase();}
            if(frame>24&&sample<100)
            {tickSamples[sample]=ticks.LastValue;mainSamples[sample]=main.LastValue;drawSamples[sample]=draw.LastValue;gcSamples[sample]=gc.LastValue;sample++;}
            if(busyMode)for(int i=0;i<moving.Length;i++)moving[i].Path.SetDistance(moving[i].Path.ExitDistance*.30f*Mathf.Clamp01(frame/124f));
            using(marker.Auto())director.Tick(Time.unscaledDeltaTime);
            if(sample==100){rows.Add(new Measurement{Skin=phase==0?"classic":ArrowSkinRegistry.All[(phase-1)/2].Id,Light=phase==0||(phase-1)%2==0,TickMs=Median(tickSamples)*.000001,MainThreadMs=Median(mainSamples)*.000001,DrawCalls=Median(drawSamples),GcBytes=Median(gcSamples)});frame=-1;sample=0;if(phase==20)Finish();}
        }
        private void SetPhase()
        {
            director.Clear();for(int i=0;i<moving.Length;i++)moving[i].Path.SetDistance(0);
            phase++;bool light=phase==0||(phase-1)%2==0;board.SetArrowSkin(phase==0?"classic":ArrowSkinRegistry.All[(phase-1)/2].Id,light);camera.backgroundColor=light?ViewTheme.Background:new Color32(14,17,22,255);director.SetLodOverride(false);if(busyMode&&phase>0)for(int i=0;i<moving.Length;i++)director.PlayFlight(moving[i]);
        }
        private static long Median(long[] values){Array.Sort(values);return values[values.Length/2];}
        private void Finish()
        {
            Directory.CreateDirectory("Logs/SkinShots");var report=new Report{TickRecorderValid=ticks.Valid,MainRecorderValid=main.Valid,DrawRecorderValid=draw.Valid,Measurements=rows.ToArray()};File.WriteAllText(busyMode?"Logs/SkinShots/benchmark-flight.json":"Logs/SkinShots/benchmark.json",JsonUtility.ToJson(report,true));
            enabled=false;ticks.Dispose();main.Dispose();draw.Dispose();gc.Dispose();QualitySettings.vSyncCount=previousVsync;Application.targetFrameRate=(int)previousRate;
            if(Environment.GetCommandLineArgs().Length>0&&Array.IndexOf(Environment.GetCommandLineArgs(),"-arrowSkinBenchmark")>=0)UnityEditor.EditorApplication.Exit(0);else UnityEditor.EditorApplication.isPlaying=false;
        }
        private void OnDestroy(){if(target!=null){target.Release();DestroyImmediate(target);}}
    }
}
#endif
