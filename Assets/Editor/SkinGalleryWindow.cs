using System;
using System.IO;
using System.Collections.Generic;
using ArrowEscape.View;
using UnityEditor;
using UnityEngine;
namespace ArrowEscape.Editor
{
    /// <summary>Play-mode art review with live light/dark and flight controls, plus reproducible captures.</summary>
    public sealed class SkinGalleryWindow : EditorWindow
    {
        private SkinGalleryBoard board;
        private bool dark=true,lod;
        private double previous;
        /// <summary>Opens the requested Editor gallery, without modifying the current level.</summary>
        [MenuItem("Tools/Arrow Escape/Skin Gallery")]
        public static void Open()=>GetWindow<SkinGalleryWindow>("Skin Gallery");
        private void OnEnable(){EditorApplication.update+=Advance;previous=EditorApplication.timeSinceStartup;}
        private void OnDisable(){EditorApplication.update-=Advance;ClosePreview();}
        private void OnGUI()
        {
            if(!EditorApplication.isPlaying){EditorGUILayout.HelpBox("Enter Play mode to review skins. The gallery never changes inventory or progress.",MessageType.Info);return;}
            EnsurePreview();EditorGUILayout.LabelField("Ten implemented skins — same renderer as gameplay",EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();EditorGUILayout.BeginVertical(GUILayout.Width(240));
            bool next=GUILayout.Toggle(dark,"Dark board","Button");if(next!=dark){dark=next;board.SetDark(dark);}
            bool nextLod=GUILayout.Toggle(lod,"Force LOD","Button");if(nextLod!=lod){lod=nextLod;board.SetLod(lod);}
            for(int i=0;i<ArrowSkinRegistry.All.Count;i++)if(GUILayout.Button("Fly "+ArrowSkinRegistry.All[i].Id))board.Fly(i);
            if(GUILayout.Button("Save rest / flight / compare"))CaptureAll();if(GUILayout.Button("Capture procedural Inferno"))InfernoCapture.CaptureFinal();EditorGUILayout.EndVertical();
            Rect image=GUILayoutUtility.GetRect(100,100,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));GUI.DrawTexture(image,board.Texture,ScaleMode.ScaleToFit,false);EditorGUILayout.EndHorizontal();
        }
        private void EnsurePreview()
        {
            if(board!=null)return;var game=FindFirstObjectByType<ArrowEscape.View.GameController>();board=new SkinGalleryBoard(game==null?null:game.GetComponent<AudioDirector>());board.SetDark(dark);board.SetLod(lod);
        }
        private void Advance()
        {
            double now=EditorApplication.timeSinceStartup;float dt=Mathf.Min(.05f,(float)(now-previous));previous=now;
            if(!EditorApplication.isPlaying){ClosePreview();return;}board?.Tick(dt);Repaint();
        }
        private void ClosePreview(){board?.Dispose();board=null;}
        /// <summary>Batch-callable capture using explicit shader clocks, including a side-by-side reference sheet.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory("Logs/SkinShots");var host=new GameObject("Skin capture (isolated)"){hideFlags=HideFlags.HideAndDontSave};var live=host.AddComponent<LiveSkinPreview>();live.Initialize(null,new Vector2(30000,30000));live.SetDark(true);
            var images=new List<Texture2D>(20);var labels=new List<string>(20);var resource=ArrowSkinResources.Load();
            try
            {
                foreach(var skin in ArrowSkinRegistry.All)
                {
                    live.Show(skin.Id);live.SetDark(true);live.Tick(1.25f);live.RenderNow();Texture2D rest=ReadTarget(live.Texture);File.WriteAllBytes("Logs/SkinShots/"+skin.Id+"_rest.png",rest.EncodeToPNG());
                    var art=resource.Find(skin.Id);images.Add(art!=null?art.Full.texture:rest);labels.Add(skin.Id+(art!=null?"_REF":"_PROCEDURAL"));images.Add(rest);labels.Add(skin.Id+"_GAME");
                    live.Fly();live.Tick(.4f);live.RenderNow();Texture2D flight=ReadTarget(live.Texture);File.WriteAllBytes("Logs/SkinShots/"+skin.Id+"_flight.png",flight.EncodeToPNG());DestroyImmediate(flight);
                }
                ArrowArtContact.Write(images,labels,"Logs/SkinShots/compare.png",2);
            }
            finally{for(int i=1;i<images.Count;i+=2)DestroyImmediate(images[i]);DestroyImmediate(host);}
        }
        /// <summary>Reads a native target only for explicit screenshots, never during animation.</summary>
        public static Texture2D ReadTarget(RenderTexture target)
        {var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=previous;return image;}
    }
}
