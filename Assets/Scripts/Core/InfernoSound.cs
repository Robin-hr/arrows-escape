using System;
namespace ArrowEscape.Core
{
    /// <summary>A seeded, filtered fire rush with sparse crackle underneath the musical note.</summary>
    public static class InfernoSound
    {
        /// <summary>Produces a finite deterministic sample buffer before gameplay.</summary>
        public static float[] Render(bool blocked,int rate)
        {
            float seconds=blocked?.12f:.48f;var data=new float[(int)Math.Round(seconds*rate)];uint seed=7177;double low=0;
            for(int i=0;i<data.Length;i++)
            {
                seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;double n=seed/(double)uint.MaxValue*2-1,t=i/(double)rate,p=t/seconds;
                low+=.045*(n-low);double crack=Math.Abs(n)>.996?n*.25:0;
                double envelope=Math.Min(1,t/.015)*Math.Pow(1-p,blocked?2:1.5);data[i]=(float)((low*2.8+crack)*envelope*.65);
            }
            data[data.Length-1]=0;return data;
        }
    }
}
