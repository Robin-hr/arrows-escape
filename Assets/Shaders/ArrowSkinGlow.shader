Shader "ArrowEscape/Skin Halo and Art"
{
 Properties
 {
  _MainTex("Art or white halo",2D)="white"{} _Tint("Color",Color)=(1,1,1,1)
  _LightBoard("Outline art on paper",Float)=0 _DstBlend("Destination blend",Float)=1 _Expansion("Halo width",Float)=0
  _Flight("Flight progress",Float)=0 _Art("Art mode",Float)=0 _Opacity("Opacity",Float)=1 _State("Feedback",Float)=0
  _StateTint("Feedback color",Color)=(1,1,1,1) _Pulse("Pulse period",Float)=2.5 _Shape("Particle recipe",Float)=0 _Trail("Lingering beam",Float)=0 _BodyLength("Cell length",Float)=1 _Seed("Deterministic seed",Float)=0
  _SkinClock("Preview clock",Float)=-1
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Pass
  {
   Tags {"LightMode"="Universal2D"}
   Cull Off ZWrite Off Blend SrcAlpha [_DstBlend]
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _Tint,_StateTint,_MainTex_TexelSize;float _Expansion,_Art,_Opacity,_State,_Pulse,_SkinClock,_ArrowSkinTime,_Shape,_LightBoard,_Trail,_BodyLength,_Flight,_Seed;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 edge:TEXCOORD1;float2 tags:TEXCOORD2;fixed4 color:COLOR;};
   struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float2 tags:TEXCOORD1;float2 radial:TEXCOORD2;};
   vary vert(app v){vary o;v.vertex.xy+=v.edge*_Expansion;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.tags=v.tags;o.radial=v.edge;return o;}
   fixed4 frag(vary i):SV_Target
   {
    if(_Art<.5&&_Trail<.5){clip(1.5-i.tags.x);clip(.5-i.tags.y);}
    float time=_SkinClock<0?_ArrowSkinTime:_SkinClock;
    fixed4 art=tex2D(_MainTex,i.uv);float halo=pow(saturate(1-abs(i.uv.y*2-1)),1.7)*(.68+.17*sin(time*6.283185/max(.1,_Pulse)));
    if(i.tags.x>.5&&i.tags.x<1.5&&_Art<.5)halo=pow(saturate(1-length(i.radial)),1.7)*.7;
    if(_Art<.5&&_Trail<.5){
     if(_Shape<.5){float tooth=pow(saturate(1-abs(frac(i.uv.x/.35)*2-1)),2);tooth*=lerp(.65,1,step(.5,frac(floor(i.uv.x/.35)*.5)));halo*=.45+tooth*.8;}
     else if(_Shape>2.5&&_Shape<3.5)halo*=.45+.45*sin(i.uv.x*7+time*2+sin(i.uv.y*9-time));
     else if(_Shape>8.5){float burst=pow(saturate(sin(time*2.094)),16);float star=exp(-abs(i.uv.x-_BodyLength*.5)*8)*pow(saturate(1-abs(i.uv.y*2-1)),.3);halo+=star*burst;}
    }
    float4 c=lerp(float4(_Tint.rgb,halo*_Tint.a),art*_Tint,_Art);
    if(_Art>.5&&_Art<1.5){
     if(_Shape>.5&&_Shape<1.5)c.rgb=lerp(c.rgb,float3(1,.98,.88),_Flight*.6);
     if(_Shape>7.5&&_Shape<8.5){float eye=exp(-dot((i.uv-float2(.62,.62))*float2(32,48),(i.uv-float2(.62,.62))*float2(32,48)));c.rgb+=float3(.4,1,.1)*eye*step(.08,frac(time/4));}
     if(_Shape>5.5&&_Shape<6.5)c.rgb*=.88+.12*cos(float3(0,2.094,4.189)+time*1.2566+i.uv.x*6);
     float2 d=_MainTex_TexelSize.xy*1.7;float around=max(max(tex2D(_MainTex,i.uv+float2(d.x,0)).a,tex2D(_MainTex,i.uv-float2(d.x,0)).a),max(tex2D(_MainTex,i.uv+float2(0,d.y)).a,tex2D(_MainTex,i.uv-float2(0,d.y)).a));float stroke=saturate((around-art.a)*3);c.rgb=lerp(c.rgb,lerp(float3(.055,.065,.08),_StateTint.rgb,_State),stroke);c.a=max(c.a,stroke*max(_LightBoard,_State));}
    c.rgb=lerp(c.rgb,_StateTint.rgb,_State*.7);if(_Trail>.5){float3 tint=_Tint.rgb;if(_Shape>5.5&&_Shape<6.5)tint=.5+.5*cos(float3(0,2.094,4.189)+i.uv.y*7-time*2);if(_Shape>1.5&&_Shape<2.5)tint=lerp(float3(0,.8,1),float3(.9,1,1),pow(saturate(1-abs(i.uv.y*2-1)),6));c.rgb=tint;}
    if(_Art>1.5){float2 p=i.uv*2-1;float r=length(p);float star=saturate(1-min(abs(p.x),abs(p.y))*10)*saturate(1-r);float shard=saturate(1-abs(p.x)*2-abs(p.y));float mask=(_Shape<.5||_Shape>4.5&&_Shape<5.5)?shard:(_Shape>6.5&&_Shape<7.5)||_Shape>8.5?star:pow(saturate(1-r),2);c=float4(_Tint.rgb,mask*_Opacity);}
    c.a*=_Opacity;c*=lerp(float4(1,1,1,1),i.color,saturate(_Art));return c;
   }
   ENDCG
  }
 }
}
