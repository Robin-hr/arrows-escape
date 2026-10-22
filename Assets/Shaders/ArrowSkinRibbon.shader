Shader "ArrowEscape/Skin Ribbon"
{
 Properties
 {
  _MainTex("Imported seamless shaft",2D)="white"{}
  _Primary("Body",Color)=(1,1,1,1) _Secondary("Accent",Color)=(1,1,1,1)
  _SkinIndex("Recipe",Float)=0 _Period("Loop seconds",Float)=2.5
  _LightBoard("Paper outline",Float)=1 _State("State overlay",Float)=0
  _StateTint("State tint",Color)=(1,1,1,1) _Opacity("Opacity",Float)=1
  _Flight("Flight progress",Float)=0 _Lod("Simplify patterns",Float)=0
  _Seed("Deterministic arrow seed",Float)=0 _SkinClock("Preview clock (-1 uses global)",Float)=-1
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Pass
  {
   Tags {"LightMode"="Universal2D"}
   Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 2.0
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   float4 _Primary,_Secondary,_StateTint;
   float _SkinIndex,_Period,_LightBoard,_State,_Opacity,_Flight,_Lod,_Seed,_SkinClock,_ArrowSkinTime;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 edge:TEXCOORD1;float2 tags:TEXCOORD2;};
   struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 tags:TEXCOORD1;};
   float clock(){return _SkinClock<0?_ArrowSkinTime:_SkinClock;}
   float noise(float2 p){return frac(sin(dot(floor(p),float2(127.1,311.7))+_Seed)*43758.5453);}
   vary vert(app v)
   {
    vary o;float t=clock();
    // Flight-only serpent wave. The logical ribbon and its 0.26-cell silhouette stay unchanged at rest.
    if(_SkinIndex>7.5&&_SkinIndex<8.5)v.vertex.xy+=v.edge.xy*sin(v.uv.x*8-t*14)*.055*_Flight;
    o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.tags=v.tags;return o;
   }
   float4 frag(vary i):SV_Target
   {
    clip(1.5-i.tags.x);clip(.5-i.tags.y);
    float u=i.uv.x,v=i.uv.y,t=clock(),edge=abs(v*2-1),core=pow(saturate(1-edge),3);
    float3 c=lerp(_Primary.rgb,_Secondary.rgb,core*.65);float glint=pow(saturate(1-abs(frac(u*.12-t/_Period)*2-1)*9),3);
    float detail=1-_Lod;float alpha=_SkinIndex<.5?lerp(.84,.94,_LightBoard):1;
    if(_Lod>.5){c=lerp(c,float3(.055,.065,.08),smoothstep(.78,1,edge)*_LightBoard*.9);c=lerp(c,_StateTint.rgb,_State*smoothstep(.38,.8,edge));return float4(c,_Opacity);}
    if(_SkinIndex<.5)
    {float cracks=step(.89,noise(float2(u*24,v*15)))*detail;c=lerp(c,float3(.15,.48,.7),cracks*.55);c+=glint*.65;float spike=step(.72,frac(u/.35))*step(.65,edge);c+=spike*.25;}
    else if(_SkinIndex<1.5)
    {float flame=noise(float2(u*18-t*16,v*6));c=lerp(_Primary.rgb,_Secondary.rgb,saturate(core+flame*.5));c=lerp(c,float3(1,.97,.85),_Flight*_Flight*.7);c*=.8+.2*sin(t*19+u*8);}
    else if(_SkinIndex<2.5)
    {float strands=pow(saturate(1-abs(v-(.5+.28*sin(u*8-t*9)))*16),3);float second=pow(saturate(1-abs(v-(.5-.28*sin(u*8-t*9)))*16),3);c=lerp(_Primary.rgb,_Secondary.rgb,max(strands,second)*detail);c+=core*.8;c*=.85+.15*sin(t*6.283185/1.5);}
    else if(_SkinIndex<3.5)
    {float mist=sin(u*6+t*2+sin(v*8-t))*.5+.5;c=lerp(_Primary.rgb,_Secondary.rgb,pow(edge,3)*(.4+.4*mist));alpha=1-_Flight*saturate(1-frac(u*.11-t*.1));}
    else if(_SkinIndex<5.5)
    {float facet=step(.5,frac(u*2+v*2))*.06;c=_Primary.rgb+facet+_Secondary.rgb*pow(edge,5)*.7+glint*.8;}
    else if(_SkinIndex<6.5)
    {float3 rainbow=.5+.5*cos(float3(0,2.094,4.189)+u*2-t*6.283185/5);c=lerp(_Primary.rgb,rainbow,.25+.3*_Flight);c+=pow(saturate(sin(v*18+u-t)),8)*.55;}
    else if(_SkinIndex<7.5)
    {float sparkle=step(.97,noise(float2(u*13,v*6)))*pow(saturate(sin(t*3+u*7)),8);c=lerp(_Primary.rgb,_Secondary.rgb,core)+sparkle*detail;}
    else if(_SkinIndex<8.5)
    {float scales=step(.4,frac(u*5+floor(v*6)*.5))*step(.45,frac(v*6));c=lerp(_Primary.rgb,_Secondary.rgb,scales*.7*detail);float strand=pow(saturate(1-abs(v-.5-sin(u*6-t*2)*.25)*17),3);c+=float3(.2,.85,.16)*strand*.7;}
    else
    {float band=step(.87,frac(u/1.2));float phase=frac(t/1.6);float beat=exp(-pow((phase-.15)*18,2))+exp(-pow((phase-.34)*20,2))*.65;c=lerp(_Primary.rgb,float3(.83,.65,.29),band);c+=_Secondary.rgb*beat*.3+glint*.3;}
    float4 strip=tex2D(_MainTex,float2(u*.7,v));c=lerp(c,strip.rgb,strip.a*.22*(1-_Lod));
    // A thin dark edge on paper keeps pale and emissive recipes legible.
    c=lerp(c,float3(.055,.065,.08),smoothstep(.78,1,edge)*_LightBoard*.9);
    // State outline is independent of the skin's base colors and returns exactly when cleared.
    c=lerp(c,_StateTint.rgb, _State*smoothstep(.38,.8,edge));
    return float4(c,alpha*_Opacity);
   }
   ENDCG
  }
 }
}
