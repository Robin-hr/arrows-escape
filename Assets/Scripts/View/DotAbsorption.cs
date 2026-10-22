using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Independent, reversible dot-light layer. Captures exact colors before any dimming.</summary>
    public sealed class DotAbsorption
    {
        private readonly BoardView view;
        private readonly DotGrid grid;
        private readonly SpriteRenderer[] sprites;
        private readonly Color[] rest;
        private readonly float[] from,target,current;
        private readonly int width,height;
        private float elapsed=1;
        private float pulse;
        /// <summary>A short eased dark pulse, independent of permanent absorption targets.</summary>
        public void Pulse(){pulse=.2f;}
        /// <summary>Captures the currently configured board dot style outside Update.</summary>
        public DotAbsorption(BoardView board)
        {
            view=board;grid=board.PathDots;if(!board.TryGetBoard(out var model))return;width=model.Width;height=model.Height;
            int count=width*height;sprites=new SpriteRenderer[count];rest=new Color[count];from=new float[count];target=new float[count];current=new float[count];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){int i=y*width+x;board.TryGetCellDot(x,y,out sprites[i]);rest[i]=board.PathDots!=null?board.PathDots.Colors[i*13]:sprites[i]!=null?sprites[i].color:Color.clear;current[i]=from[i]=target[i]=1;}
        }
        /// <summary>Updates targets only after a board change, never by scanning arrows each frame.</summary>
        public void Refresh(bool enabled,bool immediate=false)
        {
            if(current==null||!view.TryGetBoard(out var board))return;elapsed=0;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int i=y*width+x;from[i]=current[i];float distance=3;
                if(enabled)for(int a=0;a<board.Arrows.Count;a++){var arrow=board.Arrows[a];if(arrow.Kind!=ArrowKind.Normal)continue;for(int c=0;c<arrow.Cells.Count;c++){var cell=arrow.Cells[c];float dx=x-cell.X,dy=y-cell.Y;distance=Mathf.Min(distance,Mathf.Sqrt(dx*dx+dy*dy));}}
                target[i]=1-.78f*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/1.5f)));
            }
            if(immediate)Tick(1);
        }
        /// <summary>Restores the captured colors bit-for-bit, including alpha.</summary>
        public void Clear(){if(current==null)return;pulse=0;for(int i=0;i<current.Length;i++)current[i]=from[i]=target[i]=1;elapsed=1;Apply();}
        /// <summary>Interpolates the light returning after an exit; reuses all color and target buffers.</summary>
        public void Tick(float dt)
        {if(current==null||(elapsed>=1&&pulse<=0))return;pulse=Mathf.Max(0,pulse-dt);elapsed=Mathf.Min(1,elapsed+dt/.35f);float t=elapsed*elapsed*(3-2*elapsed);float dark=1-.35f*Mathf.Sin(pulse/.2f*Mathf.PI);for(int i=0;i<current.Length;i++)current[i]=Mathf.Lerp(from[i],target[i],t)*dark;Apply();}
        private void Apply()
        {
            if(view==null)return;for(int i=0;i<current.Length;i++)if(sprites[i]!=null)sprites[i].color=current[i]==1?rest[i]:rest[i]*current[i];
            if(grid!=null)grid.ApplyAbsorption(rest,current);
        }
    }
}
