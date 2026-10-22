Shader "ArrowEscape/Procedural Inferno"
{
 Properties
 {
  _Noise("Generated seamless fire noise",2D)="gray"{} _Spark("Generated spark streak",2D)="white"{}
  _HeadDistance("Generated authored SDF",2D)="white"{} _Dot("Generated round spark",2D)="white"{} _Glow("Generated soft halo",2D)="white"{} _Round("Round spark",Float)=0 _TrailLength("Lingering streak cells",Float)=5
  _Mode("Body / head / halo / spark",Float)=0 _LightBoard("Light board rim",Float)=0
  _DstBlend("Destination blend",Float)=1 _Clock("Explicit clock",Float)=-1
  _Temperature("Speed temperature",Range(0,1))=0 _Flying("Flight active",Float)=0 _Blocked("Impact flare",Float)=0
  _WindowStart("Moving window",Float)=0 _BodyLength("Body cells",Float)=5 _Lod("Simple noise",Float)=0
  _State("Feedback overlay",Float)=0 _StateTint("Feedback tint",Color)=(1,1,1,1) _Opacity("Opacity",Float)=1
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
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _Noise,_Spark,_HeadDistance,_Dot,_Glow;
   float _Round,_TrailLength;
   float _Mode,_LightBoard,_Clock,_ArrowSkinTime,_Temperature,_Flying,_Blocked,_WindowStart,_BodyLength,_Lod,_State,_Opacity;
   float4 _StateTint;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 edge:TEXCOORD1;float2 tags:TEXCOORD2;float4 color:COLOR;};
   struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 tags:TEXCOORD1;float4 color:COLOR;float2 radial:TEXCOORD2;};
   float clock(){return _Clock<0?_ArrowSkinTime:_Clock;}
   vary vert(app v)
   {
    vary o;if(_Mode>1.5&&_Mode<2.5)v.vertex.xy+=v.edge*(.28+.15*_Flying*_Temperature);
    o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.tags=v.tags;o.color=v.color;o.radial=v.edge;return o;
   }
   // The floating-point field is generated from authored smooth curves at startup.
   // It never contains any reference-image pixels and remains sharp under bilinear sampling.
   float shape(float2 pnt){return tex2D(_HeadDistance,float2((pnt.x+.6)/2.1,(pnt.y+.8)/1.6)).r;}
   float3 working(float3 c)
   {
    #ifndef UNITY_COLORSPACE_GAMMA
    return GammaToLinearSpace(c);
    #else
    return c;
    #endif
   }
   float3 heat(float edge)
   {
    float3 white=float3(1,1,1),pale=float3(1,.945,.69),gold=float3(1,.761,.29),orange=float3(1,.478,.102),red=float3(.784,.22,.039);
    float3 c=lerp(white,pale,smoothstep(.30,.63,edge));c=lerp(c,gold,smoothstep(.61,.79,edge));c=lerp(c,orange,smoothstep(.78,.92,edge));c=lerp(c,red,smoothstep(.92,1,edge));
    float3 temperature=lerp(float3(.878,.251,.102),float3(1,1,1),_Temperature);
    return lerp(c,c*temperature,_Flying*(1-_Temperature)*.90);
   }
   float4 frag(vary i):SV_Target
   {
    float t=clock(),breath=.94+.06*sin(t*6.283185/1.2),alpha=1;float3 c;
    if(_Mode>3.5){float a=saturate(i.uv.x/max(.01,_TrailLength)),v=abs(i.uv.y*2-1);float fade=smoothstep(0,.22,a)*(1-smoothstep(.82,1,a));return float4(working(float3(1,.72,.32)),pow(saturate(1-v),3)*fade*.32*_Opacity);}
    if(_Mode>2.5){float a=lerp(tex2D(_Spark,i.uv).a,tex2D(_Dot,i.uv).a,_Round);return float4(i.color.rgb*1.2,a*i.color.a);}
    if(_Mode>.5&&_Mode<1.5)
    {
     float2 p=i.uv;float n=.5;if(_Lod<.5)n=tex2D(_Noise,float2(p.x*.9+t*.32,abs(p.y)*1.4+t*.17)).r;
     float flutter=(n-.5)*.026*(1-_Lod*.65);p.y=abs(p.y);p.y+=flutter*sin(t*62.83185+p.x*8)*smoothstep(0,.10,p.y);p.x+=_Blocked*p.y*.15;
     float d=shape(p),aa=max(fwidth(d),.0025),surface=1-smoothstep(-aa,aa,d);
     float edge=saturate(1+d/.16);c=heat(edge);
     float rim=smoothstep(-.018,0,d);c=lerp(c,float3(.353,.102,.024),rim*_LightBoard*.8);
     c=lerp(c,_StateTint.rgb,_State*smoothstep(-.055,-.012,d));c=lerp(c,float3(1,1,.90),_Blocked*.8);
     float halo=exp(-max(0,d)*12)*(1-surface)*.22*(1-_LightBoard*.5)*smoothstep(.3,.15,d);c=lerp(float3(1,.30,.015),c,surface);alpha=surface+halo;
     return float4(working(c)*breath,alpha*_Opacity);
    }
    clip(1.5-i.tags.x);clip(.5-i.tags.y);
    float along=saturate(i.uv.x/max(.01,_BodyLength)),v=abs(i.uv.y*2-1);if(i.tags.x>.5)v=max(v,length(i.radial));
    float noise=.5;if(_Lod<.5)noise=tex2D(_Noise,float2((i.uv.x+t*2.5)*.38,i.uv.y*.8-t*.19)).r;
    if(_Mode>1.5){float glow=tex2D(_Glow,float2(.5,.5+v*.5)).a*(.06+.12*along)*smoothstep(0,.20,along);return float4(working(float3(1,.416,0)),glow*breath*_Opacity);}
    float width=lerp(.35,1,along)*(1+(noise-.5)*.60*(1-_Lod*.75));float edge=v/width;
    alpha=(1-smoothstep(.88,1,edge))*smoothstep(0,.25,along)*(1-smoothstep(max(0,_BodyLength-1.05),max(.01,_BodyLength-.57),i.uv.x));
    c=heat(pow(saturate(edge),1.5));c=lerp(c,float3(.353,.102,.024),smoothstep(.86,1,edge)*_LightBoard*.8);
    c=lerp(c,_StateTint.rgb,_State*smoothstep(.55,.85,edge));c*=breath*(.97+.03*sin(t*62.83185+i.uv.x*3));
    return float4(working(c),alpha*_Opacity);
   }
   ENDCG
  }
 }
}
