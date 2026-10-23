using System.IO;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.UI;
using ArrowEscape.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace ArrowEscape.Editor
{
    /// <summary>Memory-only visual checks of the six equipped themes and the rebuilt catalog.</summary>
    public static class ThemeCapture
    {
        /// <summary>Captures phone portraits of level seventeen, with Classic and a mixed skin comparison.</summary>
        [MenuItem("Tools/Arrow Escape/Capture Theme Readability")]
        public static void Capture()
        {
            Directory.CreateDirectory("Logs/ThemeShots");Directory.CreateDirectory("Logs/LevelShots");
            foreach(string id in new[]{"grad_midnight","grad_aurora","grad_sunset","grad_lagoon","grad_emerald","grad_rose","geo_crystal","geo_neon","geo_gold"}){CaptureOne(id,false);CaptureOne(id,true);}
            var grid=new Texture2D(512,384,TextureFormat.RGBA32,false);var paths=LevelRepository.GetLevelResourcePaths();
            for(int i=0;i<12;i++){var tile=LevelThumbnailRenderer.Render(LevelRepository.LoadFromResources(paths[i]).Board);grid.SetPixels32((i%4)*128,(2-i/4)*128,128,128,tile.GetPixels32());Object.DestroyImmediate(tile);}grid.Apply();File.WriteAllBytes("Logs/LevelShots/levels_01_12.png",grid.EncodeToPNG());Object.DestroyImmediate(grid);
        }
        private static void CaptureOne(string id,bool mixed)
        {
            var old=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);var tags=old.Select(c=>c.tag).ToArray();foreach(var previous in old)previous.tag="Untagged";
            var cameraHost=new GameObject("Theme capture camera");cameraHost.tag="MainCamera";var camera=cameraHost.AddComponent<Camera>();var target=new RenderTexture(1080,1920,16);target.Create();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;
            var host=new GameObject("Theme memory board");var game=host.AddComponent<GameController>();var view=host.GetComponent<BoardView>();string path=LevelRepository.GetLevelResourcePaths()[16];var loaded=LevelRepository.LoadFromResources(path);game.StartLevel(loaded.Board,loaded.LevelName);view.SetTheme(id);view.SetNormalArrowColor(ThemeDesign.ClassicInk(id));view.SetArrowSkin("classic");
            var fit=ScreenFit.Compute(loaded.Board.Width,loaded.Board.Height,1080f/1920,.08f,.08f,.15f,.14f);camera.orthographic=true;camera.orthographicSize=fit.OrthographicSize;camera.transform.position=new Vector3(fit.CameraX,fit.CameraY,-10);camera.backgroundColor=ThemeDesign.Background(id);
            var hudHost=new GameObject("Theme capture HUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));var canvas=hudHost.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=90;var scaler=hudHost.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var safe=UiFactory.Rect("Capture safe area",hudHost.transform,Vector2.zero,Vector2.zero);UiFactory.Stretch(safe);var hud=hudHost.AddComponent<HudView>();hud.Build(safe);hud.SetBoardContrast(true,true);hud.SetState(loaded.LevelName,0,game.Par,0,false);hud.SetStarState(0,game.Par,true);hud.SetLevelPosition(16,60);
            try
            {
                if(mixed){var director=host.AddComponent<SkinDirector>();director.Bind(view,game);foreach(var arrow in loaded.Board.Arrows)if(view.TryGetArrowView(arrow.Id,out var visual))visual.SetSkin(arrow.Id%2==0?"inferno_streak":"crimson_relic",false);director.SetLodOverride(true);director.Tick(2);}
                host.GetComponent<ThemeBackdrop>().Tick(2);Canvas.ForceUpdateCanvases();if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});else camera.Render();var image=SkinGalleryWindow.ReadTarget(target);File.WriteAllBytes("Logs/ThemeShots/"+id+(mixed?"_skins.png":"_level.png"),image.EncodeToPNG());Object.DestroyImmediate(image);
            }
            finally{Object.DestroyImmediate(hudHost);Object.DestroyImmediate(host);camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(cameraHost);for(int i=0;i<old.Length;i++)if(old[i]!=null)old[i].tag=tags[i];}
        }
    }
}
