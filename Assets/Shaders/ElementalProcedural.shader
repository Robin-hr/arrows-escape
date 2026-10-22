Shader "ArrowEscape/Procedural Elemental"
{
 Properties {
  _Noise("Generated seamless noise",2D)="gray"{} _Glow("Generated halo",2D)="white"{}
  _Skin("Crystal / energy / smoke",Float)=0 _Mode("Body / head / halo / atmosphere / signature",Float)=0
  _DstBlend("Destination blend",Float)=1 _LightBoard("Paper rim",Float)=0 _Clock("Explicit clock",Float)=-1
  _BodyLength("Distance in cells",Float)=5 _WindowStart("Travelled window",Float)=0 _Seed("Deterministic wisp seed",Float)=0 _Tail("Crystal fletching",Float)=0 _Lod("Dense mode",Float)=0 _State("Feedback",Float)=0
  _StateTint("Feedback tint",Color)=(1,1,1,1) _Opacity("Opacity",Float)=1 _Blocked("Impact",Float)=0
  _Flying("Flight",Float)=0 _SignatureTime("Signature seconds",Float)=0 _Age("Residue age",Float)=0 _Reveal("Travelled distance",Float)=1000
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Pass {
   Tags {"LightMode"="Universal2D"} Cull Off ZWrite Off Blend SrcAlpha [_DstBlend]
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _Noise,_Glow;
   float _Skin,_Mode,_LightBoard,_Clock,_ArrowSkinTime,_BodyLength,_Lod,_State,_Opacity,_Blocked,_Flying,_SignatureTime,_Age,_Reveal,_Tail,_WindowStart,_Seed;
   float4 _StateTint;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 edge:TEXCOORD1;float2 tags:TEXCOORD2;};
   struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 tags:TEXCOORD1;float2 radial:TEXCOORD2;};
   float clock(){return _Clock<0?_ArrowSkinTime:_Clock;}
   vary vert(app v) {
    vary o;
    // Separate speeds and silhouettes for core, halo and atmosphere, with fixed vertex buffers.
    if(_Mode>1.5&&_Mode<3.5){float expand=_Mode<2.5?.22:.45;v.vertex.xy+=v.edge*expand;}
    o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.tags=v.tags;o.radial=v.edge;return o;
   }
   float3 working(float3 c) {
    #ifndef UNITY_COLORSPACE_GAMMA
    return GammaToLinearSpace(c);
    #else
    return c;
    #endif
   }
   float segment(float2 p,float2 a,float2 b){float2 d=b-a;return length(p-a-d*saturate(dot(p-a,d)/dot(d,d)));}
   // Six authored samples form a curved predator's fang, never an image-derived mask.
   float fangCurve(float2 p,float2 a,float2 control,float2 b){float nearest=10;float2 previous=a;for(int k=1;k<=6;k++){float q=k/6.0;float2 curvePoint=(1-q)*(1-q)*a+2*(1-q)*q*control+q*q*b;nearest=min(nearest,segment(p,previous,curvePoint));previous=curvePoint;}return nearest;}
   float crystal(float2 p){float x=-p.x;float width=x*.43;return max(abs(p.y)-width,max(x-1.1,(x-.72)*.7-abs(p.y)));}
   float chevron(float2 p){p.y=abs(p.y);return max(abs(p.y+p.x*.44)-(.006+.065*saturate(-p.x)),max(-p.x-1.05,p.x));}
   float dart(float2 p){float x=saturate(-p.x/1.15);return abs(p.y)-.42*sin(x*3.14159)*pow(max(.001,x),.35);}
   float4 frag(vary i):SV_Target {
    float t=clock(),n=tex2D(_Noise,float2(i.uv.x*.55-t*.22,i.uv.y*.9+t*.08)).r;
    float n2=_Lod>.5?.5:tex2D(_Noise,float2(i.uv.x*1.75+t*.34,i.uv.y*2.1-t*.12+n*.2)).r;
    float v=abs(i.uv.y*2-1),a=1;float3 c=1;
    if(_Mode>4.5){float2 p=(i.uv-.5)*2;float d=length(p);if(_Skin<.5){float crack=min(abs(p.y-p.x*.7),min(abs(p.y+p.x*.8),abs(p.x+.15*sin(p.y*9))));return float4(working(float3(.7,.94,1)),(1-smoothstep(.01,.04,crack))*(1-smoothstep(.4,1,d))*(1-smoothstep(0,1,_Age)));}float rim=exp(-abs(d-.72)*28);return float4(working(lerp(float3(.015,.005,.025),float3(.54,.36,1),rim)),(1-smoothstep(.75,1,d))*(1-smoothstep(.55,1,_Age)));}
    if(_Mode>3.5){clip(_Reveal-i.uv.x);
     // Shared exact-path residue: a cooling scorch, creeping fern, ion line or inward smoke.
     float age=_Age,fade=1-smoothstep(.25,1,age);float edge=abs(i.uv.y*2-1);
     if(_Skin<.5){float branch=abs(frac(i.uv.x*2)*2-1);float spine=exp(-branch*70);float twig=abs(edge-frac(branch*3)*.24-branch*.55);a=max(spine,1-smoothstep(.015,.045,twig))*smoothstep(edge,edge+.15,age*2)*fade;c=float3(.65,.9,1);}
     else if(_Skin<1.5){c=lerp(float3(1,.86,.27),float3(.35,.025,.006),smoothstep(0,.9,age));a=pow(saturate(1-edge),2)*fade;float shimmer=exp(-abs(edge-.62-sin(i.uv.x*4+t*8)*.12)*12)*.065*fade;a=max(a,shimmer);}
     else if(_Skin<2.5){c=float3(.35,1,1);float ring=abs(frac(i.uv.x*1.3-age*3)-.5);a=(pow(saturate(1-edge),6)+(1-smoothstep(.01,.04,ring))*.25)*fade;}
     else{c=float3(.27,.12,.47);a=pow(saturate(1-edge),2)*(.10+n*.25)*fade;}
     return float4(working(c),a*_Opacity);
    }
    if(_Mode>.5&&_Mode<1.5){
     float2 p=i.uv;float d=_Skin<.5?crystal(p):_Skin<2.5?chevron(p):dart(p);
     if(_Skin>2.5&&_Flying>.5)clip(.45-_WindowStart-p.x);
     if(_Tail>.5){float x=-p.x;float width=(.24+.12*sin(x*16))*sin(saturate(x/1.2)*3.14159);d=max(abs(p.y)-width,max(-x,x-1.2));}
     float aa=max(fwidth(d),.002),inside=1-smoothstep(-aa,aa,d),rim=1-smoothstep(.005,.035,abs(d));
     if(_Skin<.5){
      float facet=saturate(.5+.32*sin(p.x*13+abs(p.y)*19)+.18*sin(p.x*27-abs(p.y)*23));c=lerp(float3(.247,.663,.839),float3(.75,.94,1),facet);c=lerp(c,1,rim*.9);float sweep=pow(saturate(1-abs(frac(-p.x*.24-t/2.5)-.5)*12),6);c+=sweep*.4;
      // Two authored mirrored fangs grow, snap and retreat, independent of the logical exit.
      float bt=_Blocked>0?(1-_Blocked)*.2:_SignatureTime;float bite=max(_Flying,_Blocked)*(1-smoothstep(.2,.3,bt))*smoothstep(0,.1,bt);float jaw=.03+.36*(1-smoothstep(.1,.18,bt));
      if(bite>0){float2 fangPoint=float2(p.x,abs(p.y));float fang=fangCurve(fangPoint,float2(-.65,.22),float2(-.35,.62),float2(-.06,jaw))-.035;inside=max(inside,(1-smoothstep(0,.015,fang))*bite);}
     }else if(_Skin<2.5){c=lerp(float3(0,.94,1),float3(.91,1,1),smoothstep(.075,.015,-d));float charge=_Flying*(1-smoothstep(.12,.18,_SignatureTime));c+=_Flying*exp(-pow((_SignatureTime-.14)*24,2))*.5;float ring=abs(length(float2(p.x+.15,p.y))-(.8-.7*saturate(_SignatureTime/.12)))-.025;inside=max(inside,(1-smoothstep(0,.02,ring))*charge);}
     else{float layer=.5+.5*sin(p.x*12+abs(p.y)*18+n2*4-t*.8);c=lerp(float3(.102,.043,.18),float3(.357,.169,.604),layer*.5);c=lerp(c,float3(.54,.36,1),rim*(.78+.22*sin(t*2.094395))*.85);}
     c=lerp(c,float3(.12,.07,.09),rim*_LightBoard*.7);c=lerp(c,_StateTint.rgb,_State*rim);c=lerp(c,1,_Blocked*.55);
     return float4(working(c),inside*_Opacity*(1-smoothstep(-.006,.008,p.x))*(smoothstep(-1.2,-1.1,p.x)));
    }
    clip(1.5-i.tags.x);clip(.5-i.tags.y);float u=saturate(i.uv.x/max(.01,_BodyLength));if(i.tags.x>.5)v=max(v,length(i.radial));
    if(_Skin>2.5&&_Flying>.5)clip(_BodyLength+.45-_WindowStart-i.uv.x);
    if(_Mode>1.5&&_Mode<2.5){float3 halo=_Skin<.5?float3(.435,.784,1):_Skin<2.5?float3(0,.94,1):float3(.32,.17,.52);a=pow(saturate(1-v),2)*(.10+.06*sin(t*1.8+n))*smoothstep(0,.2,u);return float4(working(halo),a*_Opacity);}
    if(_Mode>2.5){
     if(_Skin<.5){float cell=floor(i.uv.x*2.86),f=frac(i.uv.x*2.86);float jitter=tex2D(_Noise,float2(cell*.13,.25)).r;float side=frac(cell*.5)<.25?1:-1;float reach=(.24+jitter*.6)*(i.uv.y*2-1>0?side>0?1:.55:side<0?1:.55);float crystalEdge=abs(f-(.25+jitter*.5))-.18*saturate(1-v/max(.01,reach));a=(1-smoothstep(-.005,.012,crystalEdge))*(1-smoothstep(reach-.03,reach,v))*.8;c=lerp(float3(.48,.77,.92),1,1-v);float phase=frac(t/4);float grow=smoothstep(0,.4,phase)*(1-smoothstep(.6,1,phase));float axis=abs(i.uv.x-_BodyLength*.24);float twig=abs(v-axis*.85-frac(axis*9)*.12);float fern=(1-smoothstep(.012,.035,twig))*(1-smoothstep(.22,.3,axis))*smoothstep(v,v+.12,grow)*.42;a=max(a,fern);}
     else if(_Skin<2.5){float strand=.3+.18*sin(i.uv.x*5+floor(t*7)*1.9+_Seed)+.09*sin(i.uv.x*17+floor(t*9)+_Seed*.31);float d=abs(v-strand);a=(1-smoothstep(.015,.055,d))*.6;c=float3(.62,1,1);}
     else{float w=sin(i.uv.x*2.5-t*.6+n*5)*.18;float smoke=tex2D(_Noise,float2(i.uv.x*.3-t*.07,v*.7+w+n2*.2)).r;a=smoothstep(.2,.8,smoke)*pow(saturate(1-v),1.4)*.42;c=lerp(float3(.72,.69,.78),float3(.27,.12,.45),u);}
     return float4(working(c),a*_Opacity*smoothstep(0,.18,u));
    }
    if(_Skin<.5){float crust=saturate((n-.25)*1.6+n2*.25);float width=.36+.55*crust;a=1-smoothstep(width-.06,width,v);c=lerp(float3(.29,.251,.22),lerp(float3(.56,.83,.94),float3(.875,.965,1),n2),smoothstep(.35,.8,crust));float cracks=pow(saturate(1-abs(n2-.5)*35),3);c=lerp(c,1,cracks*.7);float sweep=pow(saturate(1-abs(u-smoothstep(0,1,frac(t/2.5)))*12),4);c+=sweep*.35;}
    else if(_Skin<2.5){float core=exp(-v*v*14);c=lerp(float3(0,.94,1),float3(.91,1,1),core);c*=(.9+.1*sin(t*4.18879)+.05*n2)*(.98+.02*sin(t*62.83185+i.uv.x*12))*(1-.35*_Flying*(1-smoothstep(.1,.14,_SignatureTime)));a=(1-smoothstep(.65,1,v))*smoothstep(0,.22,u);}
    else{float width=.4+(n-.5)*.35;float edge=v/max(.1,width);a=(1-smoothstep(.4,1,edge))*(.12+.48*u)*smoothstep(0,.12,u);c=lerp(float3(.37,.31,.45),float3(.102,.043,.18),u);c=lerp(c,float3(.36,.20,.58),smoothstep(.35,.95,edge)*.5);if(_Lod>.5){c=lerp(float3(.50,.38,.73),float3(.60,.48,.85),v*v);a=(1-smoothstep(.55,1,v))*.9*smoothstep(0,.08,u);}}
    c=lerp(c,float3(.12,.07,.09),_LightBoard*smoothstep(.65,.95,v)*.75);c=lerp(c,_StateTint.rgb,_State*smoothstep(.45,.8,v));return float4(working(c),a*_Opacity);
   }
   ENDCG
  }
 }
}



