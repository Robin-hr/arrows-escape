using System.IO;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace ArrowEscape.Editor
{
    /// <summary>Reproducible gallery views. The reference is opened only to compose the requested comparison.</summary>
    public static class InfernoCapture
    {
        /// <summary>First recorded visual review round.</summary>
        public static void RoundOne()=>Capture(1);
        /// <summary>Second recorded visual review round.</summary>
        public static void RoundTwo()=>Capture(2);
        /// <summary>Third recorded visual review round.</summary>
        public static void RoundThree()=>Capture(3);
        /// <summary>Final palette and spark-spray visual review.</summary>
        public static void RoundFour()=>Capture(4);
        /// <summary>Captures the current procedural skin without modifying any player data.</summary>
        [MenuItem("Tools/Arrow Escape/Skin Gallery - Capture Inferno")]
        public static void CaptureFinal()=>Capture(0);
        private static void Capture(int round)
        {
            Directory.CreateDirectory("Logs/SkinShots");string folder=round>0?"Logs/SkinShots/InfernoRound"+round:"Logs/SkinShots";Directory.CreateDirectory(folder);
            var origin=new Vector2(30000,30000);var host=new GameObject("Inferno gallery capture"){hideFlags=HideFlags.HideAndDontSave};var preview=host.AddComponent<LiveSkinPreview>();preview.Initialize(null,origin);preview.AutoReplay=false;preview.SetDark(true);preview.SetResolution(1920,1080);preview.ShowStraight("inferno_streak");preview.SetFraming(origin+new Vector2(-.6f,0),2.35f);preview.SetCaptureBackground(Color.black);
            for(int i=0;i<host.transform.childCount;i++)if(host.transform.GetChild(i).name=="Preview dot")host.transform.GetChild(i).gameObject.SetActive(false);
            try
            {
                for(int i=0;i<120;i++)preview.Tick(1/60f);preview.RenderNow();Save(preview.Texture,folder+"/inferno_hero.png");
                preview.Fly();for(int i=0;i<28;i++)preview.Tick(1/60f);preview.SetFraming(origin+new Vector2(preview.View.Path.Distance-.6f,0),2.35f);preview.RenderNow();Save(preview.Texture,folder+"/inferno_flight.png");
                Compare(folder+"/inferno_flight.png",folder+"/inferno_compare.png");Compare(folder+"/inferno_hero.png",folder+"/inferno_compare_idle.png");
                if(round>0)foreach(string name in new[]{"inferno_hero.png","inferno_flight.png","inferno_compare.png","inferno_compare_idle.png"})File.Copy(folder+"/"+name,"Logs/SkinShots/"+name,true);
            }
            finally{Object.DestroyImmediate(host);}
            CaptureBoard(false);CaptureBoard(true);
        }
        private static void CaptureBoard(bool dense)
        {
            var host=new GameObject("Inferno board capture"){hideFlags=HideFlags.HideAndDontSave};var game=host.AddComponent<GameController>();var board=host.GetComponent<BoardView>();var model=new Board(dense?24:9,dense?24:5);
            if(dense){int id=1;for(int y=0;y<24;y++)for(int x=0;x<24;x+=8){var cells=new Cell[8];for(int i=0;i<8;i++)cells[i]=new Cell(x+i,y);model.TryAddArrow(new Arrow(id++,cells,Direction.Right),out _);}}
            else{model.TryAddArrow(new Arrow(1,new[]{new Cell(0,1),new Cell(1,1),new Cell(2,1),new Cell(2,2),new Cell(3,2),new Cell(4,2),new Cell(4,3),new Cell(5,3)},Direction.Right),out _);model.TryAddArrow(2,7,3,Direction.Up,out _);}
            game.StartLevel(model);board.SetArrowSkin("inferno_streak");var director=host.GetComponent<SkinDirector>();director.SetLodOverride(dense);director.Tick(1.25f);
            var cameraHost=new GameObject("Inferno capture camera");var camera=cameraHost.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=dense?23:9;camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color32(13,15,18,255);camera.clearFlags=CameraClearFlags.SolidColor;var target=new RenderTexture(1080,1920,16);target.Create();camera.targetTexture=target;
            try
            {
                Render(camera,target);Save(target,dense?"Logs/SkinShots/inferno_dense.png":"Logs/SkinShots/inferno_bent.png");
                if(!dense){var guide=host.AddComponent<GuideController>();game.BindGuide(guide);Vector2 point=board.CellToWorld(5,3);var taps=host.GetComponent<TapController>();taps.PointerDown(1,point);taps.PointerUp(1,point);host.GetComponent<ArrowAnimator>().Tick(.05f);director.Tick(.05f);Render(camera,target);Save(target,"Logs/SkinShots/inferno_blocked.png");}
            }
            finally{camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(cameraHost);Object.DestroyImmediate(host);}
        }
        private static void Render(Camera camera,RenderTexture target){if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});else camera.Render();}
        private static void Save(RenderTexture target,string path){var image=SkinGalleryWindow.ReadTarget(target);File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);}
        private static void Compare(string render,string output)
        {
            var target=ArrowArtImporter.Read("Assets/ArtSource/Skins/inferno_streak/reference.png");var image=ArrowArtImporter.Read(render);var comparison=new Texture2D(1920,2160,TextureFormat.RGBA32,false);comparison.SetPixels32(0,1080,1920,1080,target.GetPixels32());comparison.SetPixels32(0,0,1920,1080,image.GetPixels32());comparison.Apply();File.WriteAllBytes(output,comparison.EncodeToPNG());Object.DestroyImmediate(comparison);Object.DestroyImmediate(image);Object.DestroyImmediate(target);
        }
    }
}
