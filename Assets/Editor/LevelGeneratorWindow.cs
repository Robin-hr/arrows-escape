using System;
using System.IO;
using System.Text;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEditor;
using UnityEngine;
namespace ArrowEscape.Editor
{
    /// <summary>Explicit, synchronous Editor-only level generation and safe JSON export.</summary>
    public sealed class LevelGeneratorWindow : EditorWindow
    {
        [SerializeField]private int width=6,height=6,arrowCount=14,iceCount=0,seed=12345,batchCount=10;
        [SerializeField]private bool targetEnabled,requireSlides=true;
        [SerializeField]private DifficultyTier targetTier=DifficultyTier.Medium;
        [SerializeField]private int minimumFree=1,maxAttempts=40,blockBias=3;
        [SerializeField]private string folder=LevelExportPaths.DefaultFolder;
        private GenerationResult result;
        private string report="",notice="",lastExport;
        private Vector2 scroll; private int tab; private ShapeGeneratorPanel shapes;
        private GUIStyle gridStyle;
        private Font monoFont;
        /// <summary>Opens the generator without generating or writing anything.</summary>
        [MenuItem("Tools/Arrow Escape/Level Generator")]
        public static void Open()=>GetWindow<LevelGeneratorWindow>("Level Generator");
        private GeneratorSettings Settings()=>new GeneratorSettings(width,height,arrowCount,iceCount,targetEnabled?(DifficultyTier?)targetTier:null,requireSlides,minimumFree,maxAttempts,blockBias);
        private void OnGUI()
        {
            EnsureStyle();tab=GUILayout.Toolbar(tab,new[]{"General","Shapes"});scroll=EditorGUILayout.BeginScrollView(scroll);
            if(tab==1){if(shapes==null)shapes=new ShapeGeneratorPanel();shapes.Draw();EditorGUILayout.EndScrollView();return;}
            EditorGUILayout.LabelField("Backward Level Generator",EditorStyles.boldLabel);
            width=EditorGUILayout.IntField("Width (3–10)",width);height=EditorGUILayout.IntField("Height (3–10)",height);
            arrowCount=EditorGUILayout.IntField("Arrow count",arrowCount);iceCount=EditorGUILayout.IntField("Ice count",iceCount);
            targetEnabled=EditorGUILayout.Toggle("Target a tier",targetEnabled);
            if(targetEnabled)targetTier=(DifficultyTier)EditorGUILayout.EnumPopup("Target tier",targetTier);
            requireSlides=EditorGUILayout.Toggle("Require slides",requireSlides);
            minimumFree=EditorGUILayout.IntField("Minimum free exits",minimumFree);maxAttempts=EditorGUILayout.IntField("Max attempts",maxAttempts);
            blockBias=EditorGUILayout.IntField("Block bias",blockBias);seed=EditorGUILayout.IntField("Seed",seed);batchCount=EditorGUILayout.IntField("Batch count",batchCount);
            folder=EditorGUILayout.TextField("Export folder",folder);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Generate"))Run(GenerateNow);
            if(GUILayout.Button("Previous seed"))Run(()=>{seed=checked(seed-1);GenerateNow();});
            if(GUILayout.Button("Next seed"))Run(()=>{seed=checked(seed+1);GenerateNow();});
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            using(new EditorGUI.DisabledScope(result==null||!result.Success))
                if(GUILayout.Button("Export"))Run(()=>{lastExport=Export(result,folder);notice="Exported: "+lastExport;});
            if(GUILayout.Button("Batch Export"))Run(Batch);
            using(new EditorGUI.DisabledScope(!CanPlayExport()))
                if(GUILayout.Button("Play in Sandbox"))Run(PlayExport);
            EditorGUILayout.EndHorizontal();
            if(!string.IsNullOrEmpty(notice))EditorGUILayout.HelpBox(notice,MessageType.Info);
            if(!string.IsNullOrEmpty(report))EditorGUILayout.SelectableLabel(report,gridStyle,GUILayout.MinHeight(650));
            EditorGUILayout.EndScrollView();
        }
        private void GenerateNow()
        {
            result=LevelGenerator.Generate(Settings(),seed);lastExport=null;
            report=result.Success?LevelGenerationReport.Grid(result.Board)+"\n"+
                LevelGenerationReport.Analysis(result.Analysis,result.Rating)+$"\nTierMatched {result.TierMatched}; attempts {result.AttemptsUsed}; seed {result.Seed}\n"+
                "Intended: "+LevelGenerationReport.Moves(result.IntendedSolution)+"\nSolver: "+LevelGenerationReport.Moves(result.Analysis.SolverPath):result.FailureReason;
            notice=result.Success?"Generated in memory. Export writes a new file.":"Generation failed: "+result.FailureReason;
        }
        private static string Export(GenerationResult generated,string destination)
        {
            string error=LevelExportPaths.Validate(destination);if(error!=null)throw new ArgumentException(error);
            destination=destination.Replace('\\','/').TrimEnd('/');
            RefuseLinkedFolders(destination);
            Directory.CreateDirectory(destination);
            string path=LevelExportPaths.GetUniquePath(destination,LevelExportPaths.BuildFileName(generated.Board.Width,generated.Board.Height,generated.Rating.Tier,generated.Seed));
            string json=JsonUtility.ToJson(LevelExport.ToLevelData(generated.Board,"Generated "+generated.Seed),true);
            using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write))
            using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(json);
            AssetDatabase.Refresh();return path;
        }
        private static void RefuseLinkedFolders(string destination)
        {
            // Conservative: refuse symlink/junction ancestors so an Assets-looking path cannot escape.
            var directory=new DirectoryInfo(Path.GetFullPath(destination));
            string assetRoot=Path.GetFullPath("Assets");
            while(directory!=null&&directory.FullName.Length>=assetRoot.Length)
            {
                if(directory.Exists&&(directory.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("Export through linked folders is refused.");
                directory=directory.Parent;
            }
        }
        private void Batch()
        {
            var settings=Settings();var errors=settings.Validate();if(errors.Count>0)throw new ArgumentException(string.Join(" ",errors));
            if(batchCount<1||batchCount>1000||(long)seed+batchCount-1>int.MaxValue)throw new ArgumentException("Batch count must be 1..1000 and seeds must fit an integer.");
            string error=LevelExportPaths.Validate(folder);if(error!=null)throw new ArgumentException(error);
            int exported=0,failed=0;var tiers=new int[4];
            try
            {
                for(int i=0;i<batchCount;i++)
                {
                    if(EditorUtility.DisplayCancelableProgressBar("Generating levels",$"Seed {seed+i}",(float)i/batchCount))break;
                    GenerationResult generated=LevelGenerator.Generate(settings,seed+i);
                    if(!generated.Success){failed++;continue;}
                    lastExport=Export(generated,folder);exported++;tiers[(int)generated.Rating.Tier]++;
                }
            }
            finally{EditorUtility.ClearProgressBar();}
            notice=$"Batch: exported {exported}, failed {failed}. Easy {tiers[0]}, Medium {tiers[1]}, Hard {tiers[2]}, Expert {tiers[3]}.";
        }
        private bool CanPlayExport()=>Application.isPlaying&&!string.IsNullOrEmpty(lastExport)&&lastExport.StartsWith("Assets/Resources/",StringComparison.Ordinal);
        private void PlayExport()
        {
            var bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>(FindObjectsInactive.Include);
            if(bootstrap==null){notice="No GameBootstrap exists. Start Play and open a level first.";return;}
            string resource=lastExport.Substring("Assets/Resources/".Length);resource=resource.Substring(0,resource.Length-".json".Length);
            notice=bootstrap.TryStartSandboxLevel(resource)?"Playing exported level in sandbox; progress is not saved.":"Could not load the exported resource.";
        }
        private void Run(Action action){try{action();}catch(Exception exception){notice=exception.Message;}}
        private void EnsureStyle()
        {
            if(gridStyle!=null)return;
            monoFont=Font.CreateDynamicFontFromOSFont(new[]{"Consolas","Menlo","DejaVu Sans Mono","Courier New"},14);
            if(monoFont!=null)monoFont.hideFlags=HideFlags.HideAndDontSave;
            gridStyle=new GUIStyle(EditorStyles.textArea){font=monoFont,wordWrap=true};
        }
        private void OnDisable(){shapes?.Dispose();shapes=null;if(monoFont!=null)DestroyImmediate(monoFont);gridStyle=null;}
    }
}


