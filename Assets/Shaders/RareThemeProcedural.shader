Shader "ArrowEscape/Rare Skins and Themes"
{
 Properties {
 _Noise("Generated noise",2D)="gray"{} _Skin("Permanent recipe",Float)=5 _Mode("Body/head/halo/atmosphere/residue/sigil or theme",Float)=0
 _DstBlend("Blend",Float)=1 _LightBoard("Light rim",Float)=0 _Clock("Clock",Float)=-1 _BodyLength("Cells",Float)=5 _WindowStart("Distance",Float)=0
 _Lod("Dense mode",Float)=0 _State("State",Float)=0 _StateTint("Outline",Color)=(1,1,1,1) _Opacity("Opacity",Float)=1
 _Flying("Flying",Float)=0 _SignatureTime("Signature",Float)=0 _Blocked("Impact",Float)=0 _Age("Residue age",Float)=0 _Reveal("Reveal",Float)=1000
 _MoteBudget("Shared theme mote allowance",Float)=117 _PreviewDots("Mini-board dots",Float)=0 _Theme("Theme",Float)=0 _ThemeLayer("Parallax layer",Float)=0 _Aspect("Aspect",Float)=1 _Tail("Fletching",Float)=0
 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass { Tags {"LightMode"="Universal2D"} Cull Off ZWrite Off Blend SrcAlpha [_DstBlend]
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _Noise;
 float _Skin,_Mode,_LightBoard,_Clock,_ArrowSkinTime,_BodyLength,_WindowStart,_Lod,_State,_Opacity,_Flying,_SignatureTime,_Blocked,_Age,_Reveal,_Theme,_ThemeLayer,_Aspect,_Tail;
 float4 _StateTint;float _PreviewDots,_MoteBudget;
 struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 edge:TEXCOORD1;float2 tags:TEXCOORD2;};
 struct vary {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 tags:TEXCOORD1;};
 vary vert(app v){vary o;float clock=_Clock<0?_ArrowSkinTime:_Clock;if(_Mode>.5&&_Mode<1.5&&v.tags.y> -1.5){if(_Skin>6.5&&_Skin<7.5){float angle=(_Lod>.5?0:.025*sin(clock*1.5))+.12*smoothstep(0,1,_Blocked)+_Flying*.45*smoothstep(0,.08,_SignatureTime)*(1-smoothstep(.22,.35,_SignatureTime));v.vertex.xy=mul(float2x2(cos(angle),-sin(angle),sin(angle),cos(angle)),v.vertex.xy);}if(_Skin>7.5&&_Skin<8.5)v.vertex.x-=.10*smoothstep(0,1,_Blocked);}if(_Mode>1.5&&_Mode<3.5)v.vertex.xy+=v.edge*(_Mode<2.5?.28:.46);if(_Mode>.5&&_Mode<1.5&&v.tags.y< -1.5&&v.tags.x>=0){if(_Skin>6.5&&_Skin<7.5){float a=(_Lod>.5?0:.025*sin(clock*1.5))+.12*smoothstep(0,1,_Blocked)+_Flying*.45*smoothstep(0,.08,_SignatureTime)*(1-smoothstep(.22,.35,_SignatureTime));float2 p=mul(float2x2(cos(a),-sin(a),sin(a),cos(a)),v.uv)-v.uv;v.vertex.xy+=v.edge*p.x+float2(-v.edge.y,v.edge.x)*p.y;}if(_Skin>7.5&&_Skin<8.5)v.vertex.xy-=v.edge*.10*smoothstep(0,1,_Blocked);}if(_Skin>4.5&&_Skin<9.5&&_Mode<.5)v.vertex.xy+=v.edge*.35;if(_Skin>8.5&&_Skin<9.5&&_Mode>1.5&&_Mode<2.5)v.vertex.xy+=v.edge*.67;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.tags=v.tags;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float segmentDistance(float2 p,float2 a,float2 b){float2 d=b-a;return length(p-a-d*saturate(dot(p-a,d)/max(.00001,dot(d,d))));}
 float3 rainbow(float x){return .55+.45*cos(6.283185*(x+float3(0,.333,.667)));}
 float3 linearColor(float3 c){
 #ifndef UNITY_COLORSPACE_GAMMA
 return GammaToLinearSpace(c);
 #else
 return c;
 #endif
 }
 float4 theme(float2 uv,float t){
 float z=_ThemeLayer,depth=1+z*.65;float2 p=uv; p.x+=(.004+z*.006)*sin(t*.05+z);p.y+=t*(.0006+z*.0004);
 float2 q=float2((p.x-.5)*_Aspect,p.y);float noise=tex2D(_Noise,q*.7+float2(t*.006/depth,0)).r;
 float3 c=float3(.04,.04,.06);float alpha=z<.5?1:0;
 if(_Theme<.5){ // Dreamlike forest: distant trunks, side mushrooms, curling roots and fireflies.
 c=lerp(float3(.018,.045,.032),float3(.045,.115,.075),p.y);float column=floor(p.x*(9+z*4));float seed=hash(float2(column,z));float trunkCenter=.5+.08*sin(p.y*5+seed*6);float trunkDistance=abs(frac(p.x*(9+z*4))-trunkCenter);float trunks=1-smoothstep(.04+seed*.04,.06+seed*.04,trunkDistance);
 if(z<1.5){c*=1-trunks*.65;float bark=exp(-abs(trunkDistance-.03)*140)*trunks;c+=bark*float3(.035,.045,.025);float roots=exp(-abs(p.y-(.055+.022*sin(p.x*26+t*.04)))*110);c+=roots*float3(.08,.2,.1);if(z>.5){float2 m=float2((abs(p.x-.5)-.43)*6,(p.y-.23)*6);float cap=(1-smoothstep(.52,.60,length(m*float2(1,1.65))))*smoothstep(-.02,.02,m.y);float stem=(1-smoothstep(.06,.10,abs(m.x)))*step(m.y,0)*step(-.45,m.y);float spots=step(.85,hash(floor(m*15)))*cap;float3 mushroom=lerp(float3(.25,.055,.43),float3(.60,.24,.82),saturate(m.y*2));mushroom+=spots*float3(.3,.4,.2);c=lerp(c,mushroom,cap);c=lerp(c,float3(.19,.17,.26),stem);alpha=max(trunks*.2,max(cap*.85,stem*.7)+roots*.5);}}

 }
 else if(_Theme<1.5){ // Three ice-cave depths; independent snow at the front.
 c=lerp(float3(.012,.035,.075),float3(.025,.075,.14),p.y);float crystalColumn=floor(p.x*(8+z*5));float cell=frac(p.x*(8+z*5));float tooth=1-abs(cell*2-1);float spikeHeight=.09+hash(float2(crystalColumn,z))*.20;float spikes=step(p.y,.035+tooth*spikeHeight)+step(1-p.y,.045+tooth*spikeHeight*1.25);float facet=step(.5,cell);float3 ice=lerp(float3(.06,.18,.28),float3(.18,.42,.57),facet);float edge=exp(-abs(cell-.5)*65);ice+=edge*float3(.12,.32,.48);c=lerp(c,ice,saturate(spikes));float shaft=exp(-pow((p.x-.5)*5,2))*pow(p.y,3)*.10;c+=shaft*float3(.35,.7,1);if(z>.5)alpha=saturate(spikes*.70+shaft);

 }
 else if(_Theme<2.5){ // Sandstone masonry and original geometric glyphs, never text.
 c=lerp(float3(.12,.075,.035),float3(.36,.25,.12),p.y);float2 grid=frac(p*float2(9,12));float mortar=step(grid.x,.018)+step(grid.y,.024);c*=1-saturate(mortar)*.4;
 float2 g=(frac(p*float2(6,8))-.5);float glyphKind=floor(hash(floor(p*float2(6,8)))*4);float glyph;
 if(glyphKind<1){glyph=min(abs(length(g*float2(1,1.7))-.22),min(segmentDistance(g,float2(0,-.3),float2(0,.06)),segmentDistance(g,float2(-.20,-.04),float2(.20,-.04))));}
 else if(glyphKind<2){glyph=min(abs(length(g*float2(1,2.1))-.27),length(g)-.045);}
 else if(glyphKind<3){glyph=min(abs(g.y-sin(g.x*18)*.035)-.01,abs(g.y-.13-sin(g.x*18)*.035)-.01);}
 else {glyph=min(segmentDistance(g,float2(-.24,-.10),float2(.15,.02)),min(segmentDistance(g,float2(.15,.02),float2(.20,.17)),segmentDistance(g,float2(-.1,-.02),float2(-.18,.17))));}
 c*=1-(1-smoothstep(.012,.026,glyph))*.45;
 float torches=exp(-length((p-float2(.12,.43))*float2(9,6)))+exp(-length((p-float2(.88,.43))*float2(9,6)));float flicker=.85+.1*sin(t*11)+.05*sin(t*19);c+=torches*float3(.55,.21,.025)*flicker;
 float flameX=min(abs(p.x-.12),abs(p.x-.88));float flameY=p.y-.435;float flame=(1-smoothstep(.017,.022,flameX*(1+flameY*18)))*step(0,flameY)*step(flameY,.06+sin(t*11)*.008);float cup=step(flameX,.025)*step(abs(p.y-.425),.006);c=lerp(c,float3(1,.48,.06),flame);c=lerp(c,float3(.22,.12,.04),cup);if(z>.5){float shadow=.035*sin(p.y*14+t*.6+noise);alpha=saturate(torches*.3+shadow+flame+cup);}

 }
 else if(_Theme<3.5){ // Basalt and moving molten rivers; distortion only modifies their own glow.
 float stone=tex2D(_Noise,q*5+z*.31).r;c=float3(.035,.011,.008)*(1+stone*1.3);float2 lavaUv=q+float2(sin(p.y*43-t*3)*.008,0);float field=tex2D(_Noise,lavaUv*2.5+float2(0,-t*.018)).r;float veins=abs(field-.5);float channel=abs(lavaUv.x+.18*sin(lavaUv.y*5+field*3));float river=min(veins*.6,channel);float molten=exp(-river*(z<.5?85:120));float flow=tex2D(_Noise,float2(p.x*3,p.y*2-t*.07)).r;float3 lava=lerp(float3(1,.16,.015),float3(1,.73,.17),flow);molten*=lerp(.10,1,smoothstep(.13,.45,abs(p.x-.5)));c+=molten*lava;if(z>.5)alpha=molten*.28;

 }
 else if(_Theme<4.5){ // Procedural city silhouettes, windows, pillars and two hologram billboards.
 c=float3(.025,.008,.065);float column=floor(p.x*(12+z*5));float building=.20+hash(float2(column,z))*.4;float skyline=step(p.y,building);c=lerp(c,float3(.035,.02,.09),skyline);float2 win=frac(p*float2(65,90));float lit=step(.75,hash(floor(p*float2(65,90))))*step(.3,win.x)*step(.3,win.y)*skyline;lit*=lerp(.12,1,smoothstep(.12,.45,abs(p.x-.5)));c+=lit*float3(.10,.35,.45);
 float pillar=exp(-abs(frac(p.x*6)-.5)*100)*(.7+.3*sin(t*2+column));c+=pillar*float3(.8,.04,.55);float signs=step(abs(abs(p.x-.5)-.36),.10)*step(abs(p.y-.65),.065);float bars=step(.5,frac(p.y*100))*signs;c+=bars*float3(.1,.65,.8)*(.8+.2*sin(t*9));if(z>.5)alpha=saturate(pillar*.7+signs*.6+skyline*.2);
 }
 else { // Nebula: slowly turning gas layers and a near asteroid silhouette.
 float angle=t*.006/depth;float2 npos=mul(float2x2(cos(angle),-sin(angle),sin(angle),cos(angle)),q-float2(0,.5));float gas=tex2D(_Noise,npos*1.3+z*.17).r;gas=smoothstep(.43,.8,gas);c=float3(.025,.02,.065)+gas*lerp(float3(.25,.08,.45),float3(.55,.13,.38),noise);float2 stargrid=p*float2(70,100);float2 local=frac(stargrid)-.5;float star=exp(-dot(local,local)*700)*step(.97,hash(floor(stargrid)));c+=star*(.6+.4*sin(t*2+hash(floor(stargrid))*30));if(z>.5)alpha=gas*.30+star*.8;
 if(z>1.5){float rocks=0;for(int k=0;k<3;k++){float2 center=float2(frac(.11+k*.37+t*.002),.19+k*.31);float2 rock=(p-center)*float2(_Aspect,1);float angle=atan2(rock.y,rock.x);float radius=(.028+k*.006)*(1+.17*sin(angle*5+k));rocks=max(rocks,1-smoothstep(radius,radius+.004,length(rock)));}c=lerp(c,float3(.025,.020,.040),rocks);alpha=max(alpha,rocks*.85);}
 }
 if(z>1.5&&_Theme>3.5&&_Theme<4.5){float column=floor(p.x*12);float x=frac(p.x*12)-(.15+.7*hash(float2(column,7)));float y=frac(p.y*2-t*.16-hash(float2(column,19)));float data=exp(-abs(x)*110)*exp(-y*16);float dataId=fmod(fmod(column,13)+13,13)+13*fmod(fmod(floor(p.y*2-t*.16-hash(float2(column,19))),3)+3,3);data*=1-step(min(24,floor(_MoteBudget*.25)),dataId);c+=data*float3(.10,.6,.8);alpha=max(alpha,data*.45);}
 if(z>1.5&&_Theme<5){ // <=96 deterministic shader motes, no emitter or CPU simulation.
 float2 field=p*float2(8,12)+float2(sin(t*.3)*.15,t*(_Theme<.5?-.12:.20));float2 cell=floor(field),v=frac(field)-float2(.15+.7*hash(cell),.15+.7*hash(cell+19));float cellId=fmod(fmod(cell.x,9)+9,9)+9*fmod(fmod(cell.y,13)+13,13);float available=_MoteBudget-(_Theme>3.5&&_Theme<4.5?min(24,floor(_MoteBudget*.25)):0);float mote=exp(-dot(v,v)*(_Theme>3.5?1000:350))*step(.55,hash(cell))*(1-step(available,cellId));float blink=.55+.45*sin(t*2+hash(cell)*25);float3 tint=_Theme<.5?float3(.45,1,.2):_Theme<1.5?float3(.4,.85,1):_Theme<2.5?float3(1,.5,.05):_Theme<3.5?float3(1,.3,.02):float3(.4,.7,1);c=lerp(c,tint,mote);alpha=max(alpha,mote*blink*.7);
 }
 return float4(linearColor(c),saturate(alpha));
 }
 #include "RareR1.cginc"
 float4 frag(vary i):SV_Target {
 float t=_Clock<0?_ArrowSkinTime:_Clock;
 if(_Mode>=10&&_Mode<14)return theme(i.uv,t);
 if(_Mode>=14){float2 d=abs((i.uv-.5)*2)-.91;float edge=length(max(d,0))+min(max(d.x,d.y),0);float dots=_PreviewDots*(1-smoothstep(.025,.065,length(frac(i.uv*float2(7,4))-.5)));return float4(dots*.5,dots*.6,dots*.65,max(dots*.20,.35*(1-smoothstep(.03,.10,edge))));}
 if(_Skin>4.5&&_Skin<5.5)return obsidianRender(i.uv,i.tags,t);
 if(_Skin>5.5&&_Skin<6.5)return chromeRender(i.uv,i.tags,t);
 if(_Skin>8.5&&_Skin<9.5)return relicRender(i.uv,i.tags,t);
 if(_Skin>6.5&&_Skin<7.5)return quillRender(i.uv,i.tags,t);
 if(_Skin>7.5&&_Skin<8.5)return viperRender(i.uv,i.tags,t);
 float v=abs(i.uv.y*2-1),u=i.uv.x,life=saturate(_SignatureTime/.9),ease=life*life*(3-2*life);float n=tex2D(_Noise,float2(u*.7-t*.12,i.uv.y*2)).r;
 float3 primary=_Skin<5.5?float3(.035,.04,.055):_Skin<6.5?float3(.79,.82,.85):_Skin<7.5?float3(1,.85,.42):_Skin<8.5?float3(.09,.76,.49):float3(.78,.06,.18);
 float3 accent=_Skin<5.5?float3(.43,.48,1):_Skin<6.5?rainbow(u*.15-t*.2):_Skin<7.5?float3(1,.96,.72):_Skin<8.5?float3(.2,1,.5):float3(1,.35,.45);
 float alpha=1;float3 c=primary;
 if(_Mode>6.5){float2 p=(i.uv-.5)*2;float star=pow(saturate(1-abs(p.x)*abs(p.y)*24-length(p)*.8),2);float fade=smoothstep(.10,.35,_Age)*(1-smoothstep(.50,1,_Age));return float4(linearColor(lerp(primary,float3(1,1,.9),star)),star*fade*_Opacity);}
 if(_Mode>5.5){float2 h=i.uv-float2(-.5,0);float glow=exp(-dot(h*float2(1.5,3),h*float2(1.5,3))*3);return float4(linearColor(accent),glow*(_Skin<5.5?.045:.18)*_Opacity);}
 if(_Mode>4.5){float2 p=(i.uv-.5)*2;float radius=length(p);float angle=atan2(p.y,p.x);float ring=exp(-abs(radius-(.4+.25*ease))*70);if(_Skin>5.5&&_Skin<6.5){c=rainbow(angle*.15+_Age);return float4(linearColor(c),ring*(1-smoothstep(.2,1,_Age)));}float sigil=ring+exp(-abs(radius-.76)*65)*(.4+.6*step(.75,sin(angle*12)));alpha=sigil*(1-smoothstep(.3,1,_Age));c=_Skin>8.5?float3(1,.35,.24):accent;return float4(linearColor(c),alpha);}
 if(_Mode>3.5){clip(_Reveal-i.uv.x);float fade=1-smoothstep(0,1,_Age);float center=exp(-v*(_Skin<5.5?30:7));if(_Skin>6.5&&_Skin<7.5){float stars=pow(saturate(1-abs(frac(u*.5)-.5)*16),4);center+=stars*exp(-v*2)*(.5+.5*sin(_Age*9));}c=lerp(primary,accent,.7);return float4(linearColor(c),center*fade);}
 if(_Mode>.5&&_Mode<1.5){float2 p=i.uv;float x=-p.x;float y=abs(p.y);float width=x*.5;float shape=max(y-width,max(x-1.05,(x-.72)*.8-y));
 if(_Skin>6.5&&_Skin<7.5){float a=saturate(x/1.2);shape=max(y-(.36*sin(a*3.14159)+.012+.009*sin(x*75+y*20)),max(-x,x-1.2));c=lerp(primary,float3(1,1,.85),exp(-y*17));float vanes=pow(saturate(sin((x+y*.65)*70+t*.06)),15);c*=.7+.3*vanes;c+=exp(-y*100)*.4;}
 else if(_Skin>7.5&&_Skin<8.5){shape=max(y-(.11+.19*sin(saturate(x/1.15)*3.14159)),max(-x-.025,x-1.15));float scales=pow(saturate(sin(x*42+y*53)),8);c=lerp(float3(.025,.21,.10),primary,.4+.6*scales);float mouth=exp(-segmentDistance(p,float2(-.62,-.10),float2(-.02,-.06))*80);c*=1-mouth*.8;float eyes=exp(-length(float2(p.x+.25,y-.14))*90);float blink=1-smoothstep(.02,.04,frac(t/4))*(1-smoothstep(.06,.08,frac(t/4)));c+=eyes*float3(.6,1,.13)*blink;float tongue=min(segmentDistance(p,float2(0,0),float2(.14,0)),min(segmentDistance(p,float2(.14,0),float2(.22,.05)),segmentDistance(p,float2(.14,0),float2(.22,-.05))))-.008;float flick=step(.72,frac(t/3.5))*step(frac(t/3.5),.80);if(flick>.5){shape=min(shape,tongue);if(tongue<0)c=float3(.8,.13,.25);}}
 else if(_Skin>8.5){shape=abs((x-.55)/.65)+y/.36-1;c=lerp(float3(.22,.005,.035),primary,frac(x*3+y*2));float gold=1-smoothstep(.035,.08,abs(shape+.04));c=lerp(c,float3(.83,.65,.29),gold);}
 else {float facet=step(y,x*.25);c*=.35+facet*.45;if(_Skin>5.5)c=lerp(c,rainbow(x*.6+t*.2),.16);float glint=exp(-abs(p.y+p.x*.18-.02*sin(t*.7))*90);c+=glint*(_Skin<5.5?.85:.65);if(_Skin<5.5)c+=float3(1,.3,.02)*_Flying*(1-smoothstep(.07,.12,_SignatureTime))*pow(saturate(sin(x*30+y*40)),18);}
 if(_Tail>.5){float barbs=abs(sin(x*22+y*18));shape=max(abs(y-(.10+.22*saturate(x)))-.085,max(x-1.05,-x));c=lerp(primary,accent,barbs*.5);}
 float impact=smoothstep(0,1,_Blocked);if(_Skin<5.5)c+=impact*pow(saturate(sin(x*35+y*41)),25)*float3(.8,.85,1);else if(_Skin<6.5)c+=impact*exp(-length(p-float2(-.1,0))*14)*float3(1,1,1);else if(_Skin>8.5)c=c*(1-impact*.35)+sin(_Blocked*3.14159)*float3(.8,.25,.2);float rim=exp(-abs(shape)*70);alpha=1-smoothstep(-.012,.012,shape);c+=rim*lerp(accent,float3(1,1,1),.65);if(_State>.5)c=lerp(c,_StateTint.rgb,rim);if(_LightBoard>.5)c=lerp(float3(.1,.08,.07),c,1-rim*.7);return float4(linearColor(c),alpha*_Opacity);}
 if(i.tags.x>1.5)discard;if(i.tags.x>.5&&i.tags.y>.5)discard;
 float taper=smoothstep(0,.5,u),endFade=smoothstep(0,.6,_BodyLength-u),core=exp(-v*12);float spec=pow(saturate(sin(u*1.7-t*1.57)),25);
 if(_Skin<5.5){c=lerp(float3(.018,.02,.026),primary,.5+.5*n)+spec*.20*core+(1-smoothstep(.03,.14,abs(v-.70)))*float3(.60,.65,.72);}
 else if(_Skin<6.5){c=lerp(float3(.08,.10,.14),float3(.98,1,1),pow(saturate(sin(i.uv.y*16+u*.2-t*.5)),5));c+=accent*.25;}
 else if(_Skin<7.5){c=lerp(primary,float3(1,.98,.84),core)+spec*.3;}
 else if(_Skin<8.5){float coil=_Flying*(1-smoothstep(.08,.16,_SignatureTime));float wave=sin(u*(6+coil*8)-t*1.8)*(.13+.1*_Flying);float scales=pow(saturate(sin(u*22+v*17)),12);c=lerp(float3(.20,.135,.08),primary,scales*.45+.25);c+=exp(-abs(i.uv.y-.5-wave)*70)*accent*.55;c+=core*float3(.24,.85,.45);}
 else {float bands=1-smoothstep(.05,.1,abs(frac(u/1.2)-.5));c=lerp(float3(.13,.075,.065),float3(.83,.65,.29),bands);c+=float3(1,.7,.2)*bands*_Flying*exp(-abs(u-_SignatureTime*10*_BodyLength)*8);float heart=pow(saturate(sin(t*3.927)),12)+.6*pow(saturate(sin(t*3.927-.65)),16);c+=accent*core*heart*.35;c+=(1-smoothstep(.05,.28,v))*float3(.76,.64,.50);}
 if(_Mode>1.5){float strand=sin(u*2.4-t*(.4+_Mode*.13))*.45;float second=sin(u*2.1+t*.27+1.8)*.21;float glow=exp(-abs(i.uv.y-.5-strand*.3)*(_Mode<2.5?12:24))+exp(-abs(i.uv.y-.5-second)*30)*.6;c=accent;if(_Skin>6.5&&_Skin<7.5){float2 star=float2(frac(u*.8)-.5,i.uv.y-.5-strand*.3);float twinkle=exp(-min(abs(star.x),abs(star.y))*130)*exp(-length(star)*10)*(.5+.5*sin(t*3+floor(u)*7));glow+=twinkle*2;}alpha=glow*(_Mode<2.5?.18:.28)*taper*endFade;if(_Skin>7.5&&_Skin<8.5&&_Mode>2.5){float coil=_Flying*(1-smoothstep(.08,.16,_SignatureTime));float snake=exp(-abs(i.uv.y-.5-sin(u*(2.8+coil*4)-t*.7)*.25)*22);float scales=pow(saturate(sin(u*32+i.uv.y*45)),8);alpha=(snake*.65+glow*.05)*taper*endFade;c=lerp(float3(.035,.25,.12),primary,.45+.55*scales);}if(_Skin<5.5){c=lerp(primary,float3(.6,.65,.75),glow*.2);alpha*=.45;}if(_Skin>8.5){float dx=abs(u-_BodyLength*.5);float flare=(exp(-dx*35)*exp(-v*2)+exp(-v*40)*exp(-dx*4))*(.15+.85*pow(saturate(sin(t*2.094)),16));alpha+=flare;c=lerp(c,float3(1,.85,.8),saturate(flare));}}
 else {alpha=(1-smoothstep(.83,1,v))*taper;if(_Skin>6.5&&_Skin<7.5)alpha*=1-smoothstep(.35,.65,v);if(_Skin>7.5&&_Skin<8.5)alpha*=1-smoothstep(.25,.5,v);c+=spec*.12;if(_State>.5)c=lerp(c,_StateTint.rgb,smoothstep(.65,.9,v));if(_LightBoard>.5)c=lerp(float3(.06,.06,.075),c,1-smoothstep(.72,.95,v));}
 return float4(linearColor(c),saturate(alpha)*_Opacity);
 }
 ENDCG
 }
 }
}
