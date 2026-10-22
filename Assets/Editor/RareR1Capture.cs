using System.IO;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace ArrowEscape.Editor
{
    /// <summary>Reproducible reference-only reviews; never reads any player store.</summary>
    public static class RareR1Capture
    {
        /// <summary>Captures the explicitly selected skin, leaving the next recipe untouched.</summary>
        public static void Run()
        {
            var args=System.Environment.GetCommandLineArgs();string id="emerald_viper";int round=1;
            for(int i=0;i<args.Length-1;i++){if(args[i]=="-rareSkin")id=args[i+1];if(args[i]=="-rareRound")round=int.Parse(args[i+1]);}
            Capture(id,round);
        }
        /// <summary>Final regression captures after all five recipes have passed their individual reviews.</summary>
        public static void FinalSet(){foreach(string id in new[]{"emerald_viper","celestial_quill","crimson_relic","chromium_spike","obsidian_shard"})Capture(id,0);}
        private static void Capture(string selected,int round)
        {
            string folder="Logs/SkinShots/RareR1";Directory.CreateDirectory(folder);
            var sheet=new Texture2D(2400,360,TextureFormat.RGBA32,false);
            for(int index=4;index<9;index++)
            {
                string id=ArrowSkinRegistry.All[index].Id;if(id!=selected)continue;var origin=new Vector2(30000,30000);var host=new GameObject("Elemental review");var preview=host.AddComponent<LiveSkinPreview>();preview.Initialize(null,origin);preview.AutoReplay=false;preview.SetDark(true);preview.SetResolution(1920,1080);preview.ShowStraight(id);preview.SetFraming(origin+new Vector2(index==1?-.6f:-.2f,0),index==1?2.35f:1.85f);preview.SetCaptureBackground(Color.black);
                for(int i=0;i<host.transform.childCount;i++)if(host.transform.GetChild(i).name=="Preview dot")host.transform.GetChild(i).gameObject.SetActive(false);
                try
                {
                    for(int i=0;i<120;i++)preview.Tick(1/60f);preview.RenderNow();Save(preview.Texture,folder+"/"+id+"_hero.png");
                    preview.SetCaptureBackground(preview.Skin.DarkBoardColor);preview.SetResolution(480,360);preview.SetFraming(origin,2.4f);preview.RenderNow();var tile=SkinGalleryWindow.ReadTarget(preview.Texture);sheet.SetPixels32((index-4)*480,0,480,360,tile.GetPixels32());Object.DestroyImmediate(tile);
                    preview.SetResolution(1920,1080);preview.SetCaptureBackground(Color.black);preview.Fly();for(int i=0;i<30;i++)preview.Tick(1/60f);preview.SetFraming(origin+new Vector2(preview.View.Path.Distance+(index==1?-.6f:-.2f),0),index==1?2.35f:1.85f);preview.RenderNow();Save(preview.Texture,folder+"/"+id+"_flight.png");
                    Compare(id,folder+"/"+id+"_hero.png",folder+"/"+id+"_compare.png");
                    if(id=="celestial_quill")preview.Show(id);else preview.ShowStraight(id);preview.SetResolution(480,270);preview.SetFraming(origin+new Vector2(id=="obsidian_shard"?2:1,id=="celestial_quill"?1:0),3.3f);preview.SetCaptureBackground(preview.Skin.DarkBoardColor);preview.Fly();
                    var strip=new Texture2D(3840,270,TextureFormat.RGBA32,false);float[] times=id=="obsidian_shard"?new[]{0,.07f,.14f,.22f,.36f,.50f,.66f,.84f}:new[]{0,.07f,.14f,.22f,.32f,.44f,.58f,.78f};float elapsed=0;
                    for(int frame=0;frame<8;frame++){while(elapsed<times[frame]-.00001f){float step=Mathf.Min(1/60f,times[frame]-elapsed);preview.Tick(step);elapsed+=step;}preview.RenderNow();var image=SkinGalleryWindow.ReadTarget(preview.Texture);strip.SetPixels32(frame*480,0,480,270,image.GetPixels32());Object.DestroyImmediate(image);}strip.Apply();File.WriteAllBytes(folder+"/"+id+"_signature.png",strip.EncodeToPNG());Object.DestroyImmediate(strip);
                    if(round>0)File.Copy(folder+"/"+id+"_compare.png",folder+"/"+id+"_round"+round+"_compare.png",true);
                }
                finally{Object.DestroyImmediate(host);}
                CaptureBoard(id,false);CaptureBoard(id,true);CaptureBoard(id,false,"frozen_cavern");CaptureBoard(id,false,"volcanic_underworld");
            }
            sheet.Apply();File.WriteAllBytes(folder+"/"+selected+"_tile.png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);
        }
        private static void CaptureBoard(string id,bool dense,string theme=null)
        {
            var host=new GameObject("Inferno board capture"){hideFlags=HideFlags.HideAndDontSave};var game=host.AddComponent<GameController>();var board=host.GetComponent<BoardView>();var model=new Board(dense?24:9,dense?24:5);
            if(dense){int arrowId=1;for(int y=0;y<24;y++)for(int x=0;x<24;x+=8){var cells=new Cell[8];for(int i=0;i<8;i++)cells[i]=new Cell(x+i,y);model.TryAddArrow(new Arrow(arrowId++,cells,Direction.Right),out _);}}
            else{model.TryAddArrow(new Arrow(1,new[]{new Cell(0,1),new Cell(1,1),new Cell(2,1),new Cell(2,2),new Cell(3,2),new Cell(4,2),new Cell(4,3),new Cell(5,3)},Direction.Right),out _);model.TryAddArrow(2,7,3,Direction.Up,out _);}
            game.StartLevel(model);board.SetArrowSkin(id);var director=host.GetComponent<SkinDirector>();director.SetLodOverride(dense);director.Tick(1.25f);
            var cameraHost=new GameObject("Inferno capture camera");var camera=cameraHost.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=dense?23:9;camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=ArrowSkinRegistry.Find(id).DarkBoardColor;camera.clearFlags=CameraClearFlags.SolidColor;var target=new RenderTexture(1080,1920,16);target.Create();camera.targetTexture=target;
            if(theme!=null){var backdrop=host.GetComponent<ThemeBackdrop>();if(backdrop==null)backdrop=host.AddComponent<ThemeBackdrop>();backdrop.Bind(camera,board,game);board.SetTheme(theme);backdrop.Tick(2);}
            try
            {
                Render(camera,target);Save(target,"Logs/SkinShots/RareR1/"+id+(theme!=null?(theme=="frozen_cavern"?"_theme_a.png":"_theme_b.png"):dense?"_dense.png":"_bent.png"));
                if(!dense&&theme==null){var guide=host.AddComponent<GuideController>();game.BindGuide(guide);Vector2 point=board.CellToWorld(5,3);var taps=host.GetComponent<TapController>();taps.PointerDown(1,point);taps.PointerUp(1,point);host.GetComponent<ArrowAnimator>().Tick(.05f);director.Tick(.05f);Render(camera,target);Save(target,"Logs/SkinShots/RareR1/"+id+"_blocked.png");}
            }
            finally{camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(cameraHost);Object.DestroyImmediate(host);}
        }
        private static void Render(Camera camera,RenderTexture target){if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});else camera.Render();}
        private static void Save(RenderTexture target,string path){var image=SkinGalleryWindow.ReadTarget(target);File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);}
        private static void Compare(string id,string render,string output)
        {
            var target=ArrowArtImporter.Read("Assets/ArtSource/Skins/"+id+"/reference.png");var image=ArrowArtImporter.Read(render);var comparison=new Texture2D(1920,2160,TextureFormat.RGBA32,false);comparison.SetPixels32(0,1080,1920,1080,target.GetPixels32());comparison.SetPixels32(0,0,1920,1080,image.GetPixels32());comparison.Apply();File.WriteAllBytes(output,comparison.EncodeToPNG());Object.DestroyImmediate(comparison);Object.DestroyImmediate(image);Object.DestroyImmediate(target);
        }
    }
}

