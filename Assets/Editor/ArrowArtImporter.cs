using System;
using System.IO;
using System.Collections.Generic;
using ArrowEscape.View;
using UnityEditor;
using UnityEngine;
namespace ArrowEscape.Editor
{
    /// <summary>Deterministic reference extraction. Never writes to ArtSource.</summary>
    public static class ArrowArtImporter
    {
        /// <summary>Ids in source reading order (including one supplementary serpent).</summary>
        public static readonly string[] SheetA={"crimson_relic","vortex_harpoon","thunderbolt_dart","jade_serpent","sonic_boom","chronos_needle","abyss_fang","chromium_spike","stealth_phantom","emerald_viper","celestial_quill","phoenix_wing"};
        /// <summary>Sheet B includes icon references; other ten skins remain unimplemented.</summary>
        public static readonly string[] SheetB={"frostbite","inferno_streak","void_whisper","plasma_bolt","obsidian_shard","cyber_pulsar","spore_needle","bone_carver","infinity_core"};
        /// <summary>Imports all crops and refreshes the explicit runtime resource asset.</summary>
        [MenuItem("Tools/Arrow Escape/Import Arrow Art")]
        public static void Import()
        {
            var art=new List<ArrowSkinArt>(21);var contact=new List<Texture2D>(21);var ids=new List<string>(21);
            Texture2D a=Read("Assets/ArtSource/arrows_sheet_a.png"),b=Read("Assets/ArtSource/arrows_sheet_b.png");
            try
            {
                for(int i=0;i<SheetA.Length;i++)
                {if(SheetA[i]=="thunderbolt_dart"||ArrowSkinRegistry.Find(SheetA[i])?.IsProcedural==true)continue;int c=i%4,r=i/4;int[] x={60,410,750,1085};int[] y={92,305,530};Extract(a,SheetA[i],new RectInt(x[c],y[r],c==3?290:305,r==0?134:r==2?162:150),false,null,art,contact,ids);}
                for(int i=0;i<SheetB.Length;i++)
                {if(SheetB[i]=="inferno_streak"||SheetB[i]=="frostbite"||SheetB[i]=="plasma_bolt"||SheetB[i]=="void_whisper"||ArrowSkinRegistry.Find(SheetB[i])?.IsProcedural==true)continue;int c=i%3,r=i/3;int[] x={195,660,1090};int[] y={94,322,552};int[] iconX={52,525,960};Texture2D icon=Cut(b,new RectInt(iconX[c],y[r]+2,126,130));Extract(b,SheetB[i],new RectInt(x[c],y[r],c==0?315:c==2?285:290,132),i==2,icon,art,contact,ids);UnityEngine.Object.DestroyImmediate(icon);}
                AssetDatabase.Refresh();
                var resources=AssetDatabase.LoadAssetAtPath<ArrowSkinResources>("Assets/Resources/ArrowSkinResources.asset");
                if(resources==null){resources=ScriptableObject.CreateInstance<ArrowSkinResources>();AssetDatabase.CreateAsset(resources,"Assets/Resources/ArrowSkinResources.asset");}
                resources.Ribbon=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/ArrowSkinRibbon.shader");resources.Glow=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/ArrowSkinGlow.shader");resources.Art=art.ToArray();EditorUtility.SetDirty(resources);AssetDatabase.SaveAssets();
                InfernoSetup.Configure();
                ArrowArtContact.Write(contact,ids,"Logs/ArtImport/contact.png");
            }
            finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);foreach(var t in contact)UnityEngine.Object.DestroyImmediate(t);}
        }
        /// <summary>Reads a source independently of its importer, preserving original bytes and settings.</summary>
        public static Texture2D Read(string path)
        {var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!t.LoadImage(File.ReadAllBytes(path))){UnityEngine.Object.DestroyImmediate(t);throw new InvalidDataException(path);}return t;}
        private static void Extract(Texture2D source,string id,RectInt region,bool flip,Texture2D icon,List<ArrowSkinArt> art,List<Texture2D> contact,List<string> ids)
        {
            string folder="Assets/Art/Arrows/"+id;Directory.CreateDirectory(folder);
            string replacement="Assets/ArtSource/Arrows/"+id+".png";
            Texture2D full=CreateFull(source,region,flip,replacement,id=="cyber_pulsar"?.55f:0);Save(full,folder+"/full.png",false);
            int head=Mathf.Max(8,Mathf.RoundToInt(full.width*.32f)),tail=Mathf.Max(8,Mathf.RoundToInt(full.width*.24f));
            Texture2D h=Part(full,full.width-head,head),t=Part(full,0,tail),s=Shaft(full);
            Save(h,folder+"/head.png",false);Save(t,folder+"/tail.png",false);Save(s,folder+"/shaft.png",true);
            if(icon!=null)Save(icon,folder+"/icon.png",false);
            var entry=new ArrowSkinArt{Id=id,Full=AssetDatabase.LoadAssetAtPath<Sprite>(folder+"/full.png"),Head=AssetDatabase.LoadAssetAtPath<Sprite>(folder+"/head.png"),Tail=AssetDatabase.LoadAssetAtPath<Sprite>(folder+"/tail.png"),Shaft=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/shaft.png"),Icon=icon==null?null:AssetDatabase.LoadAssetAtPath<Sprite>(folder+"/icon.png")};
            art.Add(entry);contact.Add(full);ids.Add(id);UnityEngine.Object.DestroyImmediate(h);UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(s);
        }
        /// <summary>Override precedence is explicit and testable with a temporary transparent source.</summary>
        public static Texture2D CreateFull(Texture2D sheet,RectInt region,bool flip,string overridePath,float minimumBrightness=0)
        {return PadTransparent(File.Exists(overridePath)?Read(overridePath):RemoveBackground(sheet,region,flip,minimumBrightness),6);}
        private static Color Median(List<Color> samples)
        {samples.Sort((a,b)=>a.grayscale.CompareTo(b.grayscale));return samples[samples.Count/2];}
        /// <summary>Border-estimated matting with soft un-premultiplied glows and exact detected bounds.</summary>
        public static Texture2D RemoveBackground(Texture2D source,RectInt region,bool flip,float minimumBrightness=0)
        {
            var pixels=new Color[region.width*region.height];var samples=new List<Color>();
            for(int x=0;x<region.width;x++){samples.Add(source.GetPixel(region.x+x,source.height-1-region.y));samples.Add(source.GetPixel(region.x+x,source.height-region.y-region.height));}
            Color bg=Median(samples);var top=new Color[region.width];var bottom=new Color[region.width];
            for(int x=0;x<region.width;x++)
            {
                samples.Clear();for(int dx=-6;dx<=6;dx++)samples.Add(source.GetPixel(region.x+Mathf.Clamp(x+dx,0,region.width-1),source.height-1-region.y));top[x]=Median(samples);
                samples.Clear();for(int dx=-6;dx<=6;dx++)samples.Add(source.GetPixel(region.x+Mathf.Clamp(x+dx,0,region.width-1),source.height-region.y-region.height));bottom[x]=Median(samples);
            }
            int minX=region.width,minY=region.height,maxX=-1,maxY=-1;
            for(int y=0;y<region.height;y++)for(int x=0;x<region.width;x++)
            {
                Color c=source.GetPixel(region.x+x,source.height-1-(region.y+y));bg=Color.Lerp(top[x],bottom[x],y/(float)(region.height-1));
                if(Mathf.Max(c.r,Mathf.Max(c.g,c.b))<minimumBrightness){pixels[y*region.width+x]=Color.clear;continue;}
                float difference=Mathf.Max(Mathf.Abs(c.r-bg.r),Mathf.Max(Mathf.Abs(c.g-bg.g),Mathf.Abs(c.b-bg.b)));
                float alpha=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.23f,difference));
                // Recover saturated emission over the estimated dark matte; retain dark crystal silhouettes.
                Color straight=alpha>.001f?new Color(Mathf.Clamp01((c.r-bg.r*(1-alpha))/alpha),Mathf.Clamp01((c.g-bg.g*(1-alpha))/alpha),Mathf.Clamp01((c.b-bg.b*(1-alpha))/alpha),alpha):Color.clear;
                pixels[y*region.width+x]=straight;if(alpha>.06f){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            }
            if(maxX<minX)throw new InvalidDataException("No arrow detected in "+region);
            var output=new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);
            for(int y=0;y<output.height;y++)for(int x=0;x<output.width;x++)output.SetPixel(flip?output.width-1-x:x,output.height-1-y,pixels[(minY+y)*region.width+minX+x]);output.Apply();return output;
        }
        private static Texture2D PadTransparent(Texture2D source,int padding)
        {
            var result=new Texture2D(source.width+padding*2,source.height+padding*2,TextureFormat.RGBA32,false);var blank=new Color[result.width*result.height];result.SetPixels(blank);result.SetPixels(padding,padding,source.width,source.height,source.GetPixels());result.Apply();UnityEngine.Object.DestroyImmediate(source);return result;
        }
        private static Texture2D Cut(Texture2D source,RectInt region)
        {var t=new Texture2D(region.width,region.height,TextureFormat.RGBA32,false);for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)t.SetPixel(x,t.height-1-y,source.GetPixel(region.x+x,source.height-1-region.y-y));t.Apply();return t;}
        private static Texture2D Part(Texture2D source,int start,int width)
        {var t=new Texture2D(width,source.height,TextureFormat.RGBA32,false);t.SetPixels(source.GetPixels(start,0,width,source.height));for(int y=0;y<t.height;y++){t.SetPixel(0,y,Color.clear);t.SetPixel(t.width-1,y,Color.clear);}t.Apply();return t;}
        private static Texture2D Shaft(Texture2D source)
        {
            int width=32,height=Mathf.Max(12,source.height/3);var t=new Texture2D(width,height,TextureFormat.RGBA32,false);
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {float u=x/(float)(width-1);Color left=source.GetPixelBilinear(.4f+u*.14f,(source.height-height)*.5f/source.height+y/(float)source.height),right=source.GetPixelBilinear(.54f-u*.14f,(source.height-height)*.5f/source.height+y/(float)source.height);t.SetPixel(x,y,Color.Lerp(left,right,Mathf.Abs(u-.5f)*2));}
            // Force matching edges, including alpha, to make the strip tile without a seam.
            for(int y=0;y<height;y++){Color edge=(t.GetPixel(0,y)+t.GetPixel(width-1,y))*.5f;t.SetPixel(0,y,edge);t.SetPixel(width-1,y,edge);}t.Apply();return t;
        }
        private static void Save(Texture2D texture,string path,bool strip)
        {
            File.WriteAllBytes(path,texture.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=strip?TextureImporterType.Default:TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=!strip;importer.mipmapEnabled=false;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=strip?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.isReadable=true;importer.spritePixelsPerUnit=100;importer.SaveAndReimport();
        }
    }
}
