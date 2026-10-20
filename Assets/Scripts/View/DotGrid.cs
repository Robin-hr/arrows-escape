using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>One reusable mesh of dots with an optional completion wave.</summary>
    [ExecuteAlways]
    public sealed class DotGrid : MonoBehaviour
    {
        private Mesh mesh;
        private Vector3[] rest,vertices;
        private Color[] colors,renderColors;
        private Vector3[] centers;
        private float[] delays;
        private int width,height,lastX,lastY;
        private float elapsed,total;
        private BoardView boardView;
        private GameController game;
        /// <summary>Whether a wave is updating the buffers.</summary>
        public bool IsActive { get; private set; }
        /// <summary>Persistent vertices for allocation and restoration tests.</summary>
        public Vector3[] Vertices => vertices;
        /// <summary>Persistent colors for tests.</summary>
        public Color[] Colors => colors;
        /// <summary>Builds circular dots once and subscribes through existing gameplay events.</summary>
        public void Initialize(BoardView view,Board board,Material material,GameController controller)
        {
            boardView=view;game=controller;width=board.Width;height=board.Height;
            int count=width*height;centers=new Vector3[count];delays=new float[count];
            rest=new Vector3[count*13];vertices=new Vector3[rest.Length];colors=new Color[rest.Length];renderColors=new Color[rest.Length];var triangles=new int[count*36];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int i=y*width+x,v=i*13;centers[i]=view.CellToWorld(x,y);rest[v]=centers[i];
                for(int j=0;j<12;j++)
                {
                    float a=j*Mathf.PI/6;rest[v+j+1]=centers[i]+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.06f;
                    int t=i*36+j*3;triangles[t]=v;triangles[t+1]=v+j+1;triangles[t+2]=v+1+(j+1)%12;
                }
            }
            mesh=new Mesh{name="Runtime dot grid",hideFlags=HideFlags.HideAndDontSave};mesh.MarkDynamic();
            if(rest.Length>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.sortingOrder=ViewTheme.CellSortingOrder;
            var properties=new MaterialPropertyBlock();properties.SetTexture("_MainTex",Texture2D.whiteTexture);renderer.SetPropertyBlock(properties);
            mesh.vertices=rest;mesh.triangles=triangles;mesh.bounds=new Bounds(Vector3.zero,new Vector3(width+1,height+1,1));ResetWave();
            if(game!=null){game.ArrowExited+=OnExit;game.BoardCleared+=OnCleared;}
        }
        private void OnExit(Arrow arrow){lastX=arrow.X;lastY=arrow.Y;}
        private void OnCleared()=>StartWave(lastX,lastY);
        /// <summary>Uses the same radial delays and duration as the legacy dot wave.</summary>
        public void StartWave(int x,int y)
        {
            ResetWave();for(int row=0;row<height;row++)for(int col=0;col<width;col++)delays[row*width+col]=EffectMath.DotWaveDelay(col,row,x,y,ViewTheme.DotWaveSecondsPerCell);
            total=EffectMath.DotWaveTotalSeconds(width,height,x,y,ViewTheme.DotWaveSecondsPerCell,ViewTheme.DotWaveDuration);IsActive=true;
        }
        /// <summary>Advances the wave using existing buffers; explicit ticks ignore pause.</summary>
        public void Tick(float dt)
        {
            if(!IsActive||!(dt>=0)||float.IsInfinity(dt))return;
            elapsed+=dt;if(elapsed>=total){ResetWave();return;}
            for(int i=0;i<centers.Length;i++)
            {
                float amount=EffectMath.PulseAmount(EffectMath.DotWaveLocalTime(elapsed,delays[i],ViewTheme.DotWaveDuration));
                float scale=1+(ViewTheme.DotWavePeakScale-1)*amount;Color color=Color.Lerp(ViewTheme.CellDot,ViewTheme.DotWaveColor,amount);
                Color vertexColor=QualitySettings.activeColorSpace==ColorSpace.Linear?color.linear:color;
                for(int j=0;j<13;j++){int v=i*13+j;vertices[v]=centers[i]+(rest[v]-centers[i])*scale;colors[v]=color;renderColors[v]=vertexColor;}
            }
            mesh.vertices=vertices;mesh.colors=renderColors;
        }
        /// <summary>Restores exact rest buffers and stops mesh updates.</summary>
        public void ResetWave()
        {
            IsActive=false;elapsed=0;if(mesh==null)return;
            // Keep logical colors exact while sending linear vertex colors to the shader.
            Color vertexColor=QualitySettings.activeColorSpace==ColorSpace.Linear?ViewTheme.CellDot.linear:ViewTheme.CellDot;
            for(int i=0;i<vertices.Length;i++){vertices[i]=rest[i];colors[i]=ViewTheme.CellDot;renderColors[i]=vertexColor;}
            mesh.vertices=vertices;mesh.colors=renderColors;
        }
        /// <summary>Honors hidden-board and gameplay pause flags.</summary>
        public void AdvanceFrame(float dt)
        {
            if(boardView==null||!boardView.IsVisible){if(IsActive)ResetWave();return;}
            if(game==null||!game.IsPaused)Tick(dt);
        }
        private void Update()=>AdvanceFrame(Time.deltaTime);
        private void OnDisable(){if(IsActive)ResetWave();}
        private void OnDestroy()
        {
            if(game!=null){game.ArrowExited-=OnExit;game.BoardCleared-=OnCleared;}
            if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
        }
    }
}
