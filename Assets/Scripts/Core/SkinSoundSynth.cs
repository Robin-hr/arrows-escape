using System;
namespace ArrowEscape.Core
{
    /// <summary>Ten deterministic quiet textures layered underneath the unchanged pentatonic note.</summary>
    public static class SkinSoundSynth
    {
        /// <summary>Renders an icy chime, fire, laser, whisper, crackle, glass, metal, bell, hiss or gem.</summary>
        public static float[] Render(int index,bool blocked,int rate=44100)
        {
            if(index<0||index>9||rate<8000)throw new ArgumentOutOfRangeException();
            if(index==1)return InfernoSound.Render(blocked,rate);
            float seconds=blocked?.09f:.28f;var data=new float[(int)Math.Round(seconds*rate)];double phase=0;uint noise=0x9e3779b9u+(uint)index;
            for(int i=0;i<data.Length;i++)
            {
                double t=i/(double)rate,p=t/seconds,frequency=350+index*75;
                noise^=noise<<13;noise^=noise>>17;noise^=noise<<5;double n=(noise/(double)uint.MaxValue)*2-1;
                if(index==2)frequency=t<.12?320+t*1600:1600*Math.Exp(-(t-.12)*16);else if(index==3)frequency=85;else if(index==9)frequency=210;
                phase+=frequency/rate;double tone=Math.Sin(phase*Math.PI*2)+.22*Math.Sin(phase*Math.PI*2*2.71);
                double value;
                switch(index){case 1:value=n*.65+tone*.12;break;case 3:value=n*.16+tone*.06;break;case 8:value=n*.65;break;default:value=tone*.5+n*(index==0?.07:.015);break;}
                if(index>=5)
                {
                    if(index==5)value=.36*Math.Sin(phase*Math.PI*2)+.18*Math.Sin(phase*Math.PI*2*2.63)+n*.06*Math.Exp(-t*45);
                    else if(index==6)value=.30*Math.Sin(phase*Math.PI*2)+.18*Math.Sin(phase*Math.PI*2*2.51)+.10*Math.Sin(phase*Math.PI*2*4.17);
                    else if(index==7)value=tone*.30+n*.025*(.5+.5*Math.Sin(t*180));
                    else if(index==8)value=n*(.18+.48*Math.Exp(-Math.Pow((t-.11)*18,2)))+tone*.025;
                    else value=tone*.38+Math.Sin(t*65*Math.PI*2)*(.27*Math.Exp(-t*28)+.14*Math.Exp(-Math.Pow((t-.12)*32,2)));
                }
                if(index==0)value+=n*.16*Math.Exp(-Math.Pow((t-.1)*75,2));
                if(index==2)value*=t<.12?.28:.9;
                double envelope=Math.Min(1,t/.005)*Math.Exp(-t*(blocked?32:13))*Math.Min(1,(1-p)*30);data[i]=(float)(value*envelope*.65);
            }
            data[data.Length-1]=0;return data;
        }
    }
}
