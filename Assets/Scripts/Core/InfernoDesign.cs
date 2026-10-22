using System;
namespace ArrowEscape.Core
{
    /// <summary>Procedural fire timing and an authored symmetric flame silhouette, independent of reference pixels.</summary>
    public static class InfernoDesign
    {
        /// <summary>Three swept-back tongues on each half of the head.</summary>
        public const int TonguesPerSide = 3;
        /// <summary>Breathing loop duration in seconds.</summary>
        public const float BreathSeconds = 1.2f;
        private static readonly float[] xs={1.1f,.89f,.71f,.61f,.75f,.67f,.47f,.29f,.20f,.34f,.49f,.33f,.14f,0,.10f,.22f,.13f,0};
        private static readonly float[] ys={0,.17f,.25f,.26f,.12f,.17f,.32f,.35f,.34f,.27f,.18f,.23f,.43f,.45f,.37f,.23f,.12f,0};
        private static readonly float[] smoothX,smoothY;
        static InfernoDesign()
        {
            int count=(xs.Length-1)*4+1;smoothX=new float[count];smoothY=new float[count];
            for(int i=0;i<count;i++){int segment=Math.Min(xs.Length-2,i/4);float t=(i-segment*4)/4f;smoothX[i]=Curve(xs,segment,t);smoothY[i]=Math.Max(0,Curve(ys,segment,t));}
        }
        private static float Curve(float[] values,int i,float t)
        {float a=values[Math.Max(0,i-1)],b=values[i],c=values[i+1],d=values[Math.Min(values.Length-1,i+2)];return .5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);}
        /// <summary>Number of vertices in the upper half, including the central baseline.</summary>
        public static int OutlineCount=>xs.Length;
        /// <summary>Authored upper outline x coordinate, in cells.</summary>
        public static float OutlineX(int index)=>xs[index];
        /// <summary>Authored upper outline y coordinate, in cells.</summary>
        public static float OutlineY(int index)=>ys[index];
        /// <summary>Distance to the mirrored polygon boundary; negative means inside.</summary>
        public static float HeadDistance(float x,float y)
        {
            y=Math.Abs(y);float distance=float.MaxValue;bool inside=false;
            for(int i=0,j=smoothX.Length-1;i<smoothX.Length;j=i++)
            {
                float dx=smoothX[i]-smoothX[j],dy=smoothY[i]-smoothY[j],den=dx*dx+dy*dy;
                float t=den>0?Math.Max(0,Math.Min(1,((x-smoothX[j])*dx+(y-smoothY[j])*dy)/den)):0;
                float ex=x-smoothX[j]-dx*t,ey=y-smoothY[j]-dy*t;if(i!=0)distance=Math.Min(distance,ex*ex+ey*ey);
                if((smoothY[i]>y)!=(smoothY[j]>y)&&x<(smoothX[j]-smoothX[i])*(y-smoothY[i])/(smoothY[j]-smoothY[i])+smoothX[i])inside=!inside;
            }
            return (float)Math.Sqrt(distance)*(inside?-1:1);
        }
        /// <summary>Quadratic travel has speed proportional to elapsed progress.</summary>
        public static float Temperature(float elapsedProgress)=>float.IsNaN(elapsedProgress)?0:Math.Max(0,Math.Min(1,elapsedProgress));
        /// <summary>Temperature color red channel, always in range.</summary>
        public static float HeatRed(float temperature)=>1-(1-Temperature(temperature))*(1-224/255f);
        /// <summary>Temperature color green channel, red-orange to white-hot.</summary>
        public static float HeatGreen(float temperature)=>64/255f+(1-64/255f)*Temperature(temperature);
        /// <summary>Temperature color blue channel, red-orange to white-hot.</summary>
        public static float HeatBlue(float temperature)=>26/255f+(1-26/255f)*Temperature(temperature);
    }
}
