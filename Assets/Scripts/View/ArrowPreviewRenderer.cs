using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Lazy two-preview frame budget with prepared textures and ribbon geometry; no frame allocations.</summary>
    public sealed class ArrowPreviewRenderer : IDisposable
    {
        private readonly Texture2D[] textures;
        private readonly bool[] rendered;
        private readonly Color[] primaryColors,secondaryColors;
        private readonly List<int> requests=new List<int>(32);
        private readonly Color32[] pixels=new Color32[128*128];
        private readonly PathRibbon ribbon;
        private readonly Vector2[] vertices;
        /// <summary>Renders during the latest tick, never above two.</summary>
        public int LastRenderCount { get; private set; }
        /// <summary>Preallocates native texture capacity; drawing still waits for visible requests.</summary>
        public ArrowPreviewRenderer()
        {
            int count=ShopCatalog.Items.Count;textures=new Texture2D[count];rendered=new bool[count];primaryColors=new Color[count];secondaryColors=new Color[count];
            for(int i=0;i<count;i++)
            {
                textures[i]=NewTexture();var item=ShopCatalog.Items[i];ColorUtility.TryParseHtmlString(item.PrimaryColor,out primaryColors[i]);ColorUtility.TryParseHtmlString(item.SecondaryColor,out secondaryColors[i]);
                if(item.Slot==ShopSlot.Arrow){primaryColors[i]=ArrowTint.For(item.Id,ViewTheme.Background);secondaryColors[i]=ArrowTint.EnsureContrast(secondaryColors[i],ViewTheme.Background);}
            }
            ribbon=new PathRibbon(new[]{new Vector2(-2,-1),new Vector2(0,-1),new Vector2(0,1),new Vector2(2,1)},6);ribbon.Build(0);
            vertices=new Vector2[ribbon.Vertices.Length];for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector2(64,64)+(Vector2)ribbon.Vertices[i]*24;
        }
        private static Texture2D NewTexture()=>new Texture2D(128,128,TextureFormat.RGBA32,false){name="Cosmetic preview",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
        /// <summary>Starts a visible-only request list.</summary>
        public void BeginFrame()=>requests.Clear();
        /// <summary>Queues a known item once.</summary>
        public void Request(string id){int i=Index(id);if(i>=0&&!requests.Contains(i))requests.Add(i);}
        /// <summary>Returns only a fully rendered cached preview.</summary>
        public Texture2D GetCached(string id){int i=Index(id);return i>=0&&rendered[i]&&textures[i]!=null?textures[i]:null;}
        /// <summary>Renders at most two visible requests using reused buffers.</summary>
        public void Tick()
        {
            LastRenderCount=0;
            for(int j=0;j<requests.Count&&LastRenderCount<2;j++)
            {
                int i=requests[j];if(rendered[i]&&textures[i]!=null)continue;
                if(textures[i]==null)textures[i]=NewTexture();
                Render(i);textures[i].SetPixels32(pixels);textures[i].Apply(false,false);rendered[i]=true;LastRenderCount++;
            }
        }
        private void Render(int index)
        {
            var item=ShopCatalog.Items[index];Color primary=primaryColors[index],secondary=secondaryColors[index];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)pixels[y*128+x]=item.Slot==ShopSlot.Theme?Color.Lerp(primary,secondary,y/127f):new Color(0,0,0,0);
            if(item.Slot==ShopSlot.Theme)return;
            for(int t=0;t<ribbon.Triangles.Length;t+=3)
            {
                Vector2 a=vertices[ribbon.Triangles[t]],b=vertices[ribbon.Triangles[t+1]],c=vertices[ribbon.Triangles[t+2]];
                float orientation=Cross(a,b,c);if(Mathf.Abs(orientation)<.00001f)continue;
                for(int y=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))));y<Mathf.Min(128,Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))));y++)
                for(int x=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))));x<Mathf.Min(128,Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))));x++)
                {Vector2 point=new Vector2(x+.5f,y+.5f);float u=Cross(a,b,point),v=Cross(b,c,point),w=Cross(c,a,point);if(orientation>0?u>=0&&v>=0&&w>=0:u<=0&&v<=0&&w<=0)pixels[y*128+x]=Color.Lerp(primary,secondary,x/127f);}
            }
        }
        private static float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        private static int Index(string id){for(int i=0;i<ShopCatalog.Items.Count;i++)if(ShopCatalog.Items[i].Id==id)return i;return -1;}
        /// <summary>Releases every texture when the shop closes.</summary>
        public void Dispose(){for(int i=0;i<textures.Length;i++){if(textures[i]!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(textures[i]);else UnityEngine.Object.DestroyImmediate(textures[i]);}textures[i]=null;rendered[i]=false;}requests.Clear();}
    }
}
