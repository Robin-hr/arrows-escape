using System;
using System.Collections.Generic;
using System.IO;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEditor;
using UnityEngine;
namespace ArrowEscape.Editor
{
    /// <summary>Explicit shape authoring and review-before-write pack workflow.</summary>
    public sealed class ShapeGeneratorPanel : IDisposable
    {
        private ShapeKind shape=ShapeKind.Hexagon;
        private int width=18,height=16,seed=12345,min=4,max=10,turn=45,coverage=900,free=300,attempts=30;
        private bool targetEnabled;
        private DifficultyTier tier=DifficultyTier.Medium;
        private string folder=LevelExportPaths.DefaultFolder,notice="",lastExport;
        private ShapeGenerationResult result;
        private Texture2D preview;
        private readonly List<ShapePackItem> pack=new List<ShapePackItem>();
        private string summary="";
        /// <summary>Draws controls; generation happens only after an explicit button press.</summary>
        public void Draw()
        {
            shape=(ShapeKind)EditorGUILayout.EnumPopup("Shape",shape);
            if(GUILayout.Button("Use minimum / default aspect")){Cell size=ShapeMask.DefaultSize(shape,ShapeMask.MinimumSize(shape));width=size.X;height=size.Y;min=4;max=10;}
            width=EditorGUILayout.IntField("Requested width",width);height=EditorGUILayout.IntField("Requested height",height);
            seed=EditorGUILayout.IntField("Seed",seed);min=EditorGUILayout.IntField("Minimum length",min);max=EditorGUILayout.IntField("Maximum length",max);
            turn=EditorGUILayout.IntField("Turn bias %",turn);coverage=EditorGUILayout.IntField("Coverage permille",coverage);free=EditorGUILayout.IntField("Maximum free permille",free);attempts=EditorGUILayout.IntField("Attempts",attempts);
            targetEnabled=EditorGUILayout.Toggle("Target tier",targetEnabled);if(targetEnabled)tier=(DifficultyTier)EditorGUILayout.EnumPopup("Tier",tier);
            folder=EditorGUILayout.TextField("Export folder",folder);
            EditorGUILayout.BeginHorizontal();if(GUILayout.Button("Generate"))Run(Generate);if(GUILayout.Button("Previous seed"))Run(()=>{seed=checked(seed-1);Generate();});if(GUILayout.Button("Next seed"))Run(()=>{seed=checked(seed+1);Generate();});EditorGUILayout.EndHorizontal();
            using(new EditorGUI.DisabledScope(result==null||!result.Success))if(GUILayout.Button("Export"))Run(Export);
            using(new EditorGUI.DisabledScope(!Application.isPlaying||lastExport==null||!lastExport.StartsWith("Assets/Resources/",StringComparison.Ordinal)))if(GUILayout.Button("Play in Sandbox"))Run(Play);
            if(preview!=null)GUILayout.Label(preview,GUILayout.Width(256),GUILayout.Height(256));
            if(result!=null&&result.Success)EditorGUILayout.HelpBox($"Coverage {result.CoveragePermille}/1000; tier matched {result.TierMatched}\n"+LevelGenerationReport.Analysis(result.Analysis,result.Rating),MessageType.Info);
            if(GUILayout.Button("Build Shape Pack: prepare summary"))Run(Prepare);
            if(summary.Length>0)EditorGUILayout.TextArea(summary);
            using(new EditorGUI.DisabledScope(pack.Count!=57||pack.Exists(p=>p.Result==null)))if(GUILayout.Button("Confirm and write reviewed pack"))Run(WritePack);
            if(notice.Length>0)EditorGUILayout.HelpBox(notice,MessageType.Info);
        }
        private void Generate()
        {
            result=ShapeLevelGenerator.Generate(new ShapeLevelSettings(shape,width,height,min,max,turn,coverage,free,attempts,targetEnabled?(DifficultyTier?)tier:null),seed);lastExport=null;
            if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);preview=result.Success?LevelThumbnailRenderer.Render(result.Board):null;notice=result.Success?"Generated in memory.":result.FailureReason;
        }
        private void Export()
        {
            string error=LevelExportPaths.Validate(folder);if(error!=null)throw new ArgumentException(error);
            var ancestor=new DirectoryInfo(Path.GetFullPath(folder));while(ancestor!=null){if(ancestor.Exists&&(ancestor.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked folders are refused.");ancestor=ancestor.Parent;}
            Directory.CreateDirectory(folder);lastExport=LevelExportPaths.GetUniquePath(folder,LevelExportPaths.BuildFileName(result.Board.Width,result.Board.Height,result.Rating.Tier,result.Seed));
            using(var stream=new FileStream(lastExport,FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(stream))writer.Write(JsonUtility.ToJson(LevelExport.ToLevelData(result.Board,shape+" "+seed),true));
            lastExport=lastExport.Replace('\\','/');AssetDatabase.Refresh();notice="Exported: "+lastExport;
        }
        private void Play(){var bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>(FindObjectsInactive.Include);string path=lastExport.Substring("Assets/Resources/".Length);path=path.Substring(0,path.Length-5);notice=bootstrap!=null&&bootstrap.TryStartSandboxLevel(path)?"Sandbox opened; progress is unchanged.":"Open Play mode with GameBootstrap first.";}
        private void Prepare()
        {
            pack.Clear();try{foreach(var entry in ShapePackPlan.CreateRequests()){if(EditorUtility.DisplayCancelableProgressBar("Prepare shape pack",entry.Id,pack.Count/57f))break;pack.Add(ShapePackBuilder.Prepare(entry));}}finally{EditorUtility.ClearProgressBar();}
            if(pack.Count==57){var ordered=ShapePackOrdering.Order(pack);pack.Clear();pack.AddRange(ordered);}
            summary=ShapePackBuilder.Summary(pack);notice="Review the summary. No level files have been written.";
        }
        private void WritePack()
        {
            if(!EditorUtility.DisplayDialog("Write shape pack?", "Write these 57 reviewed levels to Assets/Resources/Levels? Existing files will never be overwritten.","Write pack","Cancel"))return;
            ShapePackBuilder.WriteNew("Assets/Resources/Levels",pack);AssetDatabase.Refresh();notice="Pack written. Original levels untouched.";
        }
        private void Run(Action action){try{action();}catch(Exception error){notice=error.Message;}}
        /// <summary>Releases the unsaved preview texture.</summary>
        public void Dispose(){if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
    }
}
