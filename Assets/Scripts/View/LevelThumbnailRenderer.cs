using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.Data;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Lazy CPU thumbnails with a two-render frame budget and per-screen lifetime.</summary>
    public sealed class LevelThumbnailRenderer : IDisposable
    {
        private readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
        private readonly List<string> visible=new List<string>();
        /// <summary>Number of newly rendered textures during the most recent tick.</summary>
        public int LastRenderCount { get; private set; }
        /// <summary>Starts the visible request queue for this frame.</summary>
        public void BeginFrame()=>visible.Clear();
        /// <summary>Queues only visible level ids; duplicate requests do not consume budget.</summary>
        public void Request(string id){if(!string.IsNullOrEmpty(id)&&!visible.Contains(id))visible.Add(id);}
        /// <summary>Returns a surviving cached image, without loading or rendering.</summary>
        public Texture2D GetCached(string id)=>id!=null&&cache.TryGetValue(id,out Texture2D texture)?texture:null;
        /// <summary>Renders at most two requested missing images. A test loader can supply boards.</summary>
        public void Tick(Func<string,Board> load=null)
        {
            LastRenderCount=0;
            for(int i=0;i<visible.Count&&LastRenderCount<2;i++)
            {
                string id=visible[i];if(GetCached(id)!=null)continue;
                Board board=load!=null?load(id):LevelRepository.LoadFromResources("Levels/"+id).Board;
                if(board==null)continue;cache[id]=Render(board);LastRenderCount++;
            }
        }
        /// <summary>Creates a 128-square paper thumbnail with transparent letterboxing.</summary>
        public static Texture2D Render(Board board)
        {
            if(board==null)throw new ArgumentNullException(nameof(board));
            var pixels=new Color32[128*128];float scale=120f/Mathf.Max(board.Width,board.Height);
            float left=(128-board.Width*scale)/2,bottom=(128-board.Height*scale)/2;
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                if(x+.5f>=left&&x+.5f<128-left&&y+.5f>=bottom&&y+.5f<128-bottom)pixels[y*128+x]=ViewTheme.Background;
            foreach(Arrow arrow in board.Arrows)
            {
                Vector2 direction=new Vector2(arrow.Direction.GetDx(),arrow.Direction.GetDy());
                Vector2 head=Point(arrow.X,arrow.Y,left,bottom,scale),tip=head;
                if(arrow.Length==1){tip=head+direction*.35f*scale;Line(pixels,head-direction*.35f*scale,tip-direction*.25f*scale,.13f*scale);}
                else for(int i=1;i<arrow.Length;i++)
                {
                    Cell a=arrow.Cells[i-1],b=arrow.Cells[i];Vector2 end=Point(b.X,b.Y,left,bottom,scale);
                    if(i==arrow.Length-1)end-=direction*.3f*scale;
                    Line(pixels,Point(a.X,a.Y,left,bottom,scale),end,.13f*scale);
                }
                Vector2 side=new Vector2(-direction.y,direction.x)*.31f*scale;
                Triangle(pixels,tip,tip-direction*.42f*scale+side,tip-direction*.42f*scale-side);
            }
            var texture=new Texture2D(128,128,TextureFormat.RGBA32,false){name="Level thumbnail",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
            texture.SetPixels32(pixels);texture.Apply();return texture;
        }
        private static Vector2 Point(int x,int y,float left,float bottom,float scale)=>new Vector2(left+(x+.5f)*scale,bottom+(y+.5f)*scale);
        private static void Line(Color32[] pixels,Vector2 a,Vector2 b,float radius)
        {
            Vector2 delta=b-a;float length=delta.sqrMagnitude;
            for(int y=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.y,b.y)-radius));y<Mathf.Min(128,Mathf.CeilToInt(Mathf.Max(a.y,b.y)+radius));y++)
            for(int x=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.x,b.x)-radius));x<Mathf.Min(128,Mathf.CeilToInt(Mathf.Max(a.x,b.x)+radius));x++)
            {Vector2 p=new Vector2(x+.5f,y+.5f);float t=length>0?Mathf.Clamp01(Vector2.Dot(p-a,delta)/length):0;if((p-a-t*delta).sqrMagnitude<=radius*radius)pixels[y*128+x]=ViewTheme.ArrowColor;}
        }
        private static float Cross(Vector2 a,Vector2 b,Vector2 p)=>(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
        private static void Triangle(Color32[] pixels,Vector2 a,Vector2 b,Vector2 c)
        {
            for(int y=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))));y<Mathf.Min(128,Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))));y++)
            for(int x=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))));x<Mathf.Min(128,Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))));x++)
            {var p=new Vector2(x+.5f,y+.5f);float u=Cross(a,b,p),v=Cross(b,c,p),w=Cross(c,a,p);if((u>=0&&v>=0&&w>=0)||(u<=0&&v<=0&&w<=0))pixels[y*128+x]=ViewTheme.ArrowColor;}
        }
        /// <summary>Releases all images when level select closes.</summary>
        public void Dispose(){foreach(var texture in cache.Values)if(texture!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(texture);else UnityEngine.Object.DestroyImmediate(texture);}cache.Clear();visible.Clear();}
    }
}
