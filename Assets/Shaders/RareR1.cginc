// Authored analytical materials only. Reference artwork is never texture input.
// A surface pass composites the under-coil, cylindrical shaft and over-coil.
float coverage(float d){return 1-smoothstep(-.004,.008,d);}
float3 jade(float u,float y,float center,float radius,float clock)
{
    float across=(y-center)/radius;
    float dome=sqrt(saturate(1-across*across));
    float2 grid=float2(u*19,(across+1)*3.6);
    grid.x+=floor(grid.y)*.5;
    float2 tile=frac(grid)-.5;
    float scaleEdge=pow(saturate(1-length(tile*float2(1,1.3))),5);
    float ridges=exp(-abs(length(tile*float2(1,1.3))-.43)*32);
    float belly=smoothstep(.35,.85,-across);
    float3 base=lerp(float3(.025,.20,.085),float3(.16,.59,.29),dome);
    base=lerp(base,float3(.34,.58,.22)*(.55+.45*dome),belly*.65);
    base*=.68+.42*scaleEdge;
    base*=.89+.11*sin(floor(grid.x)*23+floor(grid.y)*11);
    base+=ridges*float3(.04,.13,.025)*(.3+.7*dome);
    float highlight=exp(-pow((across-.32)*7,2))*(.7+.3*sin(u*3+clock*.3));
    base+=highlight*float3(.35,.54,.28)+pow(dome,24)*.11;
    base+=exp(-abs(abs(across)-.92)*38)*float3(.13,.37,.12);
    return base;
}
float4 feather(float2 p,float t,float3 dark,float3 gold)
{
    float x=-p.x,side=p.y>=0?1:-1;
    float quill=.025*sin(x*3),y=p.y-quill;
    float extent=(side>0?.24:.20)*sin(saturate(x/1.15)*3.14159);
    float serration=.008*sin(x*117+side*3)+.005*sin(x*71);
    float d=max(abs(y)-extent-serration,max(-x,x-1.15));
    float barb=frac(x*21+abs(y)*8);
    float ridge=pow(saturate(1-abs(barb-.52)*3),9);
    float lit=sqrt(saturate(1-pow(y/max(.02,extent),2)));
    float3 c=lerp(dark,gold,.3+.55*lit)*(.62+.38*ridge);
    c+=exp(-abs(y)*160)*gold*.6+exp(-abs(d)*140)*gold*.3;
    if(_State>.5)c=lerp(c,_StateTint.rgb,exp(-abs(d)*85));
    return float4(c,coverage(d));
}
float4 viperRender(float2 uv,float2 tags,float t)
{
    float age=_SignatureTime;
    float charge=_Flying*smoothstep(0,.09,age)*(1-smoothstep(.22,.34,age));
    float strike=_Flying*smoothstep(.18,.30,age)*(1-smoothstep(.50,.75,age));
    if(_Mode>6.5)return float4(.1,.8,.22,0);
    if(_Mode>5.5)return float4(.1,.8,.22,0);
    if(_Mode>4.5){float2 p=(uv-.5)*2;float a=smoothstep(0,.12,_Age)*(1-smoothstep(.30,.52,_Age));float angle=atan2(p.y,p.x);float r=.40+.11*sin(angle*2+_Age*13);float d=abs(length(p)-r)-.07;float4 coil=float4(jade(angle*.25,p.y,0,.9,t),coverage(d)*a);return float4(linearColor(coil.rgb),coil.a);}
    if(_Mode>3.5){float y=abs(uv.y*2-1);float fade=1-smoothstep(.1,1,_Age);clip(_Reveal-uv.x);return float4(linearColor(float3(.11,.65,.21)),exp(-y*6)*fade*.4);}
    if(_Mode>.5&&_Mode<1.5)
    {
        if(_Tail>.5||tags.x<-.5){float2 fp=uv;fp.y-=sign(fp.y)*(.065+.10*saturate(-fp.x));float4 f=feather(fp,t,float3(.027,.13,.045),float3(.38,.63,.16));f.rgb*=.92+.08*sin(-uv.x*143+uv.y*95);f.a*=smoothstep(.015,.045,abs(uv.y));f.rgb=linearColor(f.rgb);f.a*=_Opacity;return f;}
        float2 p=uv;float x=-p.x;float jawOpen=.12*strike+.04*_Blocked;
        float center=.012*sin(x*4);
        float width=lerp(.025,.255,smoothstep(0,.28,x))*(1-.65*smoothstep(.57,1.12,x));
        width*=smoothstep(1.14,1.02,x);
        float d=max(abs(p.y-center)-width,max(-x,x-1.12));
        float3 c=jade(x+2,p.y,center,width,t);
        float brow=exp(-segmentDistance(p,float2(-.55,.18),float2(-.19,.14))*65);
        c+=brow*float3(.28,.49,.13);
        float lipCurve=-.075-.025*sin(x*4)-jawOpen*smoothstep(.05,.6,x);
        float lip=exp(-abs(p.y-lipCurve)*150)*smoothstep(.05,.12,x)*(1-smoothstep(.65,.8,x));
        c*=1-lip*.85;
        float gape=smoothstep(.02,.13,x)*(1-smoothstep(.53,.70,x));
        float gap=coverage(max(p.y+.04,(-.04-jawOpen*gape)-p.y))*gape*max(strike,_Blocked);
        c=lerp(c,float3(.025,.006,.009),gap);
        float fang=coverage(max(abs(p.x+.24)-.018,abs(p.y+.07)-.035))*strike;c=lerp(c,float3(.91,.98,.68),fang);
        c*=.92+.08*tex2D(_Noise,p*4+.17).r;
        float eye=length((p-float2(-.36,.105))*float2(1,1.3));
        c=lerp(c,float3(.72,.94,.04),coverage(eye-.045));
        c=lerp(c,float3(.015,.03,.004),coverage(max(abs(p.x+.36)-.008,abs(p.y-.105)-.032)));
        c+=exp(-length(p-float2(-.37,.12))*180)*.7;
        float nostril=exp(-length((p-float2(-.09,.07))*float2(1,1.3))*130);c*=1-nostril*.95;
        float tongue=min(segmentDistance(p,float2(-.01,-.035),float2(.13,-.04)),min(segmentDistance(p,float2(.13,-.04),float2(.24,.015)),segmentDistance(p,float2(.13,-.04),float2(.24,-.095))))-.007;
        float flick=smoothstep(.70,.72,frac(t/3.5))*(1-smoothstep(.78,.80,frac(t/3.5)));
        if(flick>0||strike>.2){float a=coverage(tongue);c=lerp(c,float3(.64,.10,.17),a);d=min(d,tongue);}
        float rim=exp(-abs(d)*95);c+=rim*float3(.20,.50,.17);
        if(_State>.5)c=lerp(c,_StateTint.rgb,saturate(rim*1.5));
        if(_LightBoard>.5)c=lerp(c,float3(.03,.12,.045),rim*.65);
        return float4(linearColor(c),coverage(d)*_Opacity);
    }
    if(tags.x>1.5||tags.x>.5&&tags.y>.5)discard;
    float u=uv.x,y=(uv.y-.5)*.96;
    float taper=smoothstep(0,.25,u)*(1-smoothstep(_BodyLength-.70,_BodyLength-.40,u));
    if(_Mode>1.5){float a=exp(-pow(y*3,2))*.12*taper*(1-smoothstep(.45,1,abs(uv.y*2-1)));return float4(linearColor(float3(.06,.45,.13)),a*_Opacity);}
    float phase=u*(3.9+charge*1.3)-.10*sin(t*.8)+strike*sin(u*2-age*15)*.7;
    float center=sin(phase)*(.20+charge*.09);
    float radius=.15+charge*.018;
    float snake=coverage(abs(y-center)-radius)*taper;
    float rod=coverage(abs(y)-.095)*taper;
    float cylinder=sqrt(saturate(1-pow(y/.095,2)));
    float grain=sin(u*93+sin(u*7)*3+y*250)*sin(u*11+y*37);
    float3 wood=lerp(float3(.10,.053,.022),float3(.35,.24,.12),cylinder)*(.86+.14*grain);
    wood+=exp(-pow((y-.025)*65,2))*float3(.15,.11,.055);
    float3 snakeColor=jade(u,y,center,radius,t);
    float over=smoothstep(-.12,.12,cos(phase));
    float3 c=lerp(snakeColor,wood,rod);
    c=lerp(c,snakeColor,snake*over);
    float a=max(rod,snake);
    float outline=max(exp(-abs(abs(y-center)-radius)*160)*snake,exp(-abs(abs(y)-.095)*140)*rod);
    if(_State>.5)c=lerp(c,_StateTint.rgb,outline);
    if(_LightBoard>.5)c=lerp(c,float3(.02,.075,.025),outline*.5);
    return float4(linearColor(c),a*_Opacity);
}
// The feather is authored from its curved rachis and individual vane ridges.
float sparkle(float2 p,float size)
{
    p/=max(.01,size);float r=length(p);float rays=pow(saturate(1-abs(p.x)/.035),2)*pow(saturate(1-abs(p.y)/.9),2)+pow(saturate(1-abs(p.y)/.035),2)*pow(saturate(1-abs(p.x)/.9),2);
    return exp(-r*r*500)+rays;
}
float4 quillRender(float2 uv,float2 tags,float t)
{
    float3 gold=float3(1,.79,.28),cream=float3(1,.97,.76);
    if(_Mode>6.5){float2 p=(uv-.5)*2;float star=sparkle(p,.95);float fade=smoothstep(.08,.28,_Age)*(1-smoothstep(.62,1,_Age));return float4(linearColor(cream),star*fade);}
    if(_Mode>5.5)return float4(0,0,0,0);
    if(_Mode>4.5){float2 p=(uv-.5)*2;float swell=smoothstep(0,.10,_Age)*(1-smoothstep(.30,.55,_Age));float star=sparkle(p,.95);float arcs=exp(-abs(length(p)-.52)*65)*.2;return float4(linearColor(cream),(star+arcs)*swell);}
    if(_Mode>3.5){clip(_Reveal-uv.x);float y=abs(uv.y*2-1);float ink=exp(-y*1.2)*(1-smoothstep(.5,1,y));float constellation=exp(-y*12);float a=lerp(ink,constellation,smoothstep(.3,.65,_Age))*(1-smoothstep(.65,1,_Age));return float4(linearColor(lerp(gold,cream,exp(-y*8))),a);}
    if(_Mode>.5&&_Mode<1.5)
    {
        if(_Tail>.5||tags.x<-.5){float2 fp=uv;fp.y-=sign(fp.y)*(.05+.09*saturate(-fp.x));float4 f=feather(fp,t,float3(.42,.24,.045),cream);f.a*=smoothstep(.01,.035,abs(uv.y));f.rgb=linearColor(f.rgb);f.a*=_Opacity;return f;}
        float x=-uv.x,q=saturate(x/1.12),rachis=.075*sin(q*3.14159);
        float y=uv.y-rachis,side=y>0?1:-1;
        float width=(side>0?.25:.16)*pow(saturate(sin(q*3.14159)),.58);
        float barbPhase=x*33+abs(y)*12+abs(y)*abs(y)*5+.11*sin(x*19);
        float tear=.010*sin(x*83+side*1.4)+.005*sin(x*157);
        float d=max(abs(y)-width-tear,max(-x,x-1.12));
        float cuts=exp(-pow((x-.23-abs(y)*.30)*95,2))+exp(-pow((x-.52-abs(y)*.27)*110,2))+exp(-pow((x-.81-abs(y)*.25)*95,2));
        d+=cuts*smoothstep(.45,.95,abs(y)/max(width,.01))*.12;
        float fiber=pow(saturate(.5+.5*cos(barbPhase*6.283)),8);
        float fine=pow(saturate(.5+.5*cos((x*114+abs(y)*39)*6.283)),12);
        float dome=sqrt(saturate(1-pow(y/max(width,.01),2)));
        float3 c=lerp(float3(.51,.29,.055),gold,.28+.65*dome);
        c=lerp(c,cream,saturate(.38+.55*q+.25*y)*.65);
        c*=.83+.11*fiber+.06*fine;
        c*=.92+.08*tex2D(_Noise,float2(x*4,y*11)).r;
        c=lerp(c,cream,fiber*smoothstep(-.08,.20,y)*.16);
        float quill=exp(-abs(y)*150),spec=exp(-pow((y-.025)*50,2));c+=quill*cream*.65+spec*.13;
        float rim=exp(-abs(d)*110);c+=rim*lerp(gold*.25,cream,smoothstep(-.1,.15,y))*.18;
        if(_State>.5)c=lerp(c,_StateTint.rgb,rim);
        if(_LightBoard>.5)c=lerp(c,float3(.28,.13,.015),rim*.65);
        float surface=coverage(d);float2 halo=(uv-float2(-.55,.04))*float2(1.8,3.0);float glow=exp(-dot(halo,halo)*2)*.17;float alpha=surface+(1-surface)*glow;c=lerp(gold,c,surface);
        return float4(linearColor(c),saturate(alpha)*_Opacity);
    }
    if(tags.x>1.5||tags.x>.5&&tags.y>.5)discard;
    float u=uv.x,y=(uv.y-.5)*.96,taper=smoothstep(0,.16,u)*(1-smoothstep(_BodyLength-.5,_BodyLength-.3,u));
    if(_Mode>1.5)
    {
        float wave=.25*sin(u*1.2-t*.22),wave2=.24*sin(u*1.1-t*.18+1.2),cloud=exp(-pow((y-wave)*26,2))+exp(-pow((y-wave2)*30,2))*.7;
        float2 cell=float2(u*40,y*70);float2 key=floor(cell);float2 p=frac(cell)-.5;
        float glitter=exp(-dot(p,p)*95)*step(.58,hash(key))*(.5+.5*sin(t*2+hash(key)*20));
        float2 star=float2(frac(u*.65)-.5,(y-wave)*1.8);float stars=sparkle(star,.4)*(.6+.4*sin(t*2+floor(u)*4));
        float a=(cloud*.02+glitter*cloud*2.5+stars*1.0)*taper*(1-smoothstep(.7,1,abs(uv.y*2-1)));
        return float4(linearColor(lerp(gold,cream,.65)),a*_Opacity);
    }
    float r=.155,dome=sqrt(saturate(1-pow(y/r,2)));
    float3 c=lerp(float3(.31,.16,.025),gold,dome);
    c+=exp(-pow((y-.045)*70,2))*cream*.6;
    c*=.985+.015*sin(u*173+y*90);
    float rim=exp(-abs(abs(y)-r)*130);if(_State>.5)c=lerp(c,_StateTint.rgb,rim);if(_LightBoard>.5)c=lerp(c,float3(.22,.12,.01),rim*.6);
    return float4(linearColor(c),coverage(abs(y)-r)*taper*_Opacity);
}
float3 raisedGold(float y,float width,float u,float t)
{
    float a=y/width,dome=sqrt(saturate(1-a*a));
    float3 c=lerp(float3(.22,.105,.025),float3(.83,.65,.29),dome);
    c+=exp(-pow((a-.36)*9,2))*float3(.65,.58,.39);
    c+=exp(-abs(abs(a)-.90)*40)*float3(.18,.15,.08);
    c*=.95+.05*sin(u*130+y*80);return c;
}
float4 relicRender(float2 uv,float2 tags,float t)
{
    float heart=.25+.55*pow(saturate(sin(t*3.927)),12)+.25*pow(saturate(sin(t*3.927-.65)),16);
    float charge=_Flying*smoothstep(0,.11,_SignatureTime)*(1-smoothstep(.25,.38,_SignatureTime));
    float3 red=float3(.78,.035,.15),pink=float3(1,.36,.45),gold=float3(.83,.65,.29);
    if(_Mode>6.5)return float4(0,0,0,0);
    if(_Mode>5.5)return float4(0,0,0,0);
    if(_Mode>4.5){float2 p=(uv-.5)*2;float r=length(p),a=atan2(p.y,p.x);float ring=exp(-abs(r-.62)*90)+exp(-abs(r-.83)*100)*.65;float runes=step(.83,cos(a*16))*exp(-abs(r-.73)*45);float spokes=exp(-abs(sin(a*4))*60)*step(.18,r)*step(r,.55);float fade=smoothstep(0,.08,_Age)*(1-smoothstep(.45,1,_Age));return float4(linearColor(lerp(red,gold,saturate(runes))),saturate(ring+runes+spokes)*fade);}
    if(_Mode>3.5){clip(_Reveal-uv.x);float y=abs(uv.y*2-1);float rays=exp(-y*5)+.15*exp(-y);return float4(linearColor(pink),rays*(1-smoothstep(.25,1,_Age)));}
    if(_Mode>.5&&_Mode<1.5)
    {
        if(_Tail>.5||tags.x<-.5){float2 fp=uv;fp.y-=sign(fp.y)*(.055+.09*saturate(-fp.x));float4 f=feather(fp,t,float3(.16,.008,.035),float3(.86,.18,.29));f.a*=smoothstep(.015,.04,abs(uv.y));f.rgb=linearColor(f.rgb);f.a*=_Opacity;return f;}
        float2 p=uv;float x=-p.x,y=abs(p.y);
        float diamond=abs((x-.46)/.47)+y/.30-1;
        float sideGem=abs((x-.89)/.16)+abs(y-.24)/.13-1;
        float socket=length(float2((x-1.025)/.10,p.y/.17))-1;
        float chain=min(segmentDistance(p,float2(-1.04,0),float2(-.89,.24)),min(segmentDistance(p,float2(-1.04,0),float2(-.89,-.24)),min(segmentDistance(p,float2(-.89,.24),float2(-.59,.16)),segmentDistance(p,float2(-.89,-.24),float2(-.59,-.16)))))-.024;
        float d=min(diamond,min(sideGem,min(socket,chain)));
        float edge=abs(diamond+.12),frame=1-smoothstep(.12,.25,-diamond);
        float facets=step(p.y,(x-.46)*.5),cut=abs((x-.46)/.33)+y/.20;
        float3 c=lerp(float3(.13,.002,.027),red,.15+.55*facets);
        c=lerp(float3(.20,.003,.036),c,smoothstep(.45,.9,cut));c+=exp(-abs(cut-.75)*90)*pink*.24; c+=exp(-length((p-float2(-.62,.12))*float2(1,1.4))*100)*pink*.7;
        c+=exp(-abs(p.y-(x-.46)*.34-.08)*90)*pink*.48;
        float glassBand=exp(-pow((p.y+(x-.46)*.18-.10)*85,2));c+=glassBand*float3(1,.62,.69)*.38;
        c+=exp(-length((p-float2(-.53,.12))*float2(1,1.7))*120)*float3(1,.85,.90);c*=.97+.03*tex2D(_Noise,p*19).r;
        float bevel=pow(saturate(1-abs(diamond+.11)/.14),.65);
        float3 metal=raisedGold(p.y,.33,x,t)*(.45+.55*bevel);
        metal+=exp(-abs(diamond+.025)*130)*float3(.65,.58,.40)*smoothstep(-.25,.15,p.y);
        metal+=exp(-abs(diamond)*95)*.45;
        metal+=pow(saturate(sin(x*93+p.y*57)),24)*frame*.17;
        c=lerp(c,metal,frame);
        float engrave=exp(-abs(abs((x-.46)/.41)+y/.255-.99)*150);c*=1-engrave*.65;
        if(sideGem<diamond){float gemRim=1-smoothstep(.10,.24,abs(sideGem+.1));c=lerp(red*(.3+.5*step(p.y,0)),metal,gemRim);c+=exp(-length(p-float2(-.87,.25))*90)*pink;}
        if(chain<min(diamond,sideGem)||socket<min(diamond,sideGem))c=raisedGold(p.y,.17,x,t);
        float rim=exp(-abs(d)*80);if(_State>.5)c=lerp(c,_StateTint.rgb,rim);
        if(_LightBoard>.5)c=lerp(c,float3(.16,.025,.04),rim*.6);
        float flare=sparkle((p+float2(.46,0))*float2(1,.75),.85)*charge;
        c=lerp(c,pink+gold*.5,saturate(flare));
        float glow=exp(-length((p+float2(.46,0))*float2(1.5,2.5))*4)*(.10+.25*charge);
        c=lerp(red*.65,c,coverage(d));c+=pink*flare;return float4(linearColor(c),saturate(coverage(d)+glow+flare)*_Opacity);
    }
    if(tags.x>1.5||tags.x>.5&&tags.y>.5)discard;
    float u=uv.x,taper=smoothstep(0,.18,u)*(1-smoothstep(_BodyLength-.55,_BodyLength-.35,u));
    if(_Mode>1.5)
    {
        float y=(uv.y-.5)*2.16,dx=u-_BodyLength*.53;
        float denseScale=_Lod>.5?.35:1;
        float vertical=exp(-abs(dx)*38)*pow(saturate(1-abs(y)/denseScale),1.7);
        float horizontal=exp(-abs(y)*95)*pow(saturate(1-abs(dx)/.75),2);
        float sideRays=exp(-abs(abs(dx)-.18)*85)*pow(saturate(1-abs(y)/(.6*denseScale)),2)*.42;
        float bloom=exp(-y*y*15)*.08+exp(-dx*dx*5-y*y*5)*.20*heart,star=(vertical+horizontal+sideRays)*heart;
        star+=exp(-dx*dx*40-y*y*25)*heart*.6;float a=(bloom+star)*taper*(1-smoothstep(.85,1,abs(uv.y*2-1)));
        return float4(linearColor(lerp(red,float3(1,.85,.83),saturate(star*1.5))),a*_Opacity);
    }
    float y=(uv.y-.5)*.96,r=.155,dome=sqrt(saturate(1-pow(y/r,2)));
    float3 c=lerp(float3(.09,.045,.05),float3(.50,.34,.30),saturate(dome*.70+y/r*.45));c+=exp(-pow((y-.045)*85,2))*.38; c*=.91+.09*tex2D(_Noise,float2(u*4,y*30)).r;
    float du=(frac(u/1.2)-.5)*1.2,ring=coverage(length(float2(du/.04,y/.18))-1);
    float bandLight=_Flying*smoothstep(u/max(.1,_BodyLength)*.20,u/max(.1,_BodyLength)*.20+.04,_SignatureTime)*(1-smoothstep(.30,.5,_SignatureTime));
    float3 collar=raisedGold(y,.18,u,t)*(.55+.45*sqrt(saturate(1-pow(du/.04,2))))+gold*bandLight;
    c=lerp(c,collar,ring);
    float a=max(coverage(abs(y)-r),ring),rim=exp(-abs(abs(y)-r)*130);
    if(_State>.5)c=lerp(c,_StateTint.rgb,rim);if(_LightBoard>.5)c=lerp(c,float3(.08,.02,.03),rim*.6);
    return float4(linearColor(c),a*taper*_Opacity);
}
float3 chromeMetal(float across,float along,float t)
{
    float dome=sqrt(saturate(1-across*across));
    float reflection=pow(saturate(.5+.5*sin(across*8+along*.25-t*.28)),4);
    float3 c=lerp(float3(.34,.39,.44),float3(.86,.91,.96),reflection)*(.65+.35*dome);
    c+=exp(-pow((across-.38)*15,2))*.38;
    c+=exp(-abs(abs(across)-.93)*50)*float3(.12,.16,.20);
    c+=rainbow(along*.13+t*.04)*.08;
    return c;
}
float4 chromeRender(float2 uv,float2 tags,float t)
{
    float3 silver=float3(.79,.82,.85);
    if(_Mode>6.5)return float4(0,0,0,0);
    if(_Mode>5.5)return float4(0,0,0,0);
    if(_Mode>4.5)
    {
        float2 p=(uv-.5)*2;float build=smoothstep(0,.10,_Age)*(1-smoothstep(.23,.35,_Age));
        float gather=exp(-abs(length(p)-lerp(.80,.22,smoothstep(0,.25,_Age)))*65);
        float star=(sparkle(p,.95)+gather*.85+exp(-dot(p,p)*8)*.22)*build;
        float cone=min(segmentDistance(p,float2(-.65,.62),float2(.75,0)),segmentDistance(p,float2(-.65,-.62),float2(.75,0)));
        float ring=abs(length(float2((p.x+.65)*3,p.y/.62))-1);
        float action=smoothstep(.20,.35,_Age)*(1-smoothstep(.65,1,_Age));
        float a=star+(exp(-cone*90)+exp(-ring*70))*.8*action;
        return float4(linearColor(lerp(rainbow(atan2(p.y,p.x)*.18+_Age),float3(1,1,1),saturate(star))),saturate(a));
    }
    if(_Mode>3.5){clip(_Reveal-uv.x);float y=uv.y*2-1;float a=exp(-y*y*3)*(1-smoothstep(.65,1,abs(y)))*(1-smoothstep(.25,1,_Age));return float4(linearColor(rainbow(uv.y*.7+_Age*.2)),a*.65);}
    if(_Mode>.5&&_Mode<1.5)
    {
        if(_Tail>.5||tags.x<-.5){float2 fp=uv;fp.y-=sign(fp.y)*(.05+.09*saturate(-fp.x));float4 f=feather(fp,t,float3(.12,.18,.23),float3(.87,.95,1));f.rgb+=rainbow(-uv.x*.25+t*.08)*.24*smoothstep(.5,.95,abs(fp.y)/.24);f.a*=smoothstep(.015,.04,abs(uv.y));f.rgb=linearColor(f.rgb);f.a*=_Opacity;return f;}
        float2 p=uv;float x=-p.x,y=abs(p.y);
        float d=max(y-x*.46,max(x-1.12,(x-.72)*.85-y));
        float facet=step(y,x*.23);float across=p.y/max(.02,x*.46);
        float3 c=chromeMetal(across*.8,x*2,t)*(.65+.35*facet);c*=.96+.04*tex2D(_Noise,p*18).r;
        float band=exp(-abs(p.y+(x-.3)*.23-.03*sin(t*.7))*70);c+=band*.38;
        float rim=exp(-abs(d)*100);c+=rim*(float3(.65,.72,.78)+rainbow(x*.5+t*.05)*.3);
        c+=rainbow(x*.35-p.y*.5+t*.03)*.14;c+=exp(-abs(y-x*.23)*120)*float3(.18,.22,.25);
        if(_State>.5)c=lerp(c,_StateTint.rgb,rim);if(_LightBoard>.5)c=lerp(c,float3(.08,.13,.17),rim*.7);
        c+=exp(-length(p)*18)*_Blocked;
        float glow=exp(-length((p+float2(.55,0))*float2(2,2))*4)*.08;
        return float4(linearColor(c),saturate(coverage(d)+glow)*_Opacity);
    }
    if(tags.x>1.5||tags.x>.5&&tags.y>.5)discard;
    float u=uv.x,taper=smoothstep(0,.25,u)*(1-smoothstep(_BodyLength-.6,_BodyLength-.35,u));
    if(_Mode>1.5)
    {
        float y=(uv.y-.5)*.82,total=0;float3 c=0;
        for(int k=0;k<4;k++)
        {
            float phase=u*.95-t*(.16+k*.035)+k*1.65,center=.27*sin(phase),width=.125+.025*sin(u*.7+k);
            float ribbon=exp(-pow((y-center)/width,2)*2)*(.45+.20*cos(phase));
            float3 hue=k==0?float3(1,.18,.72):k==1?float3(.08,.82,1):k==2?float3(1,.78,.18):float3(.12,1,.48);
            float crease=exp(-pow((y-center+width*.25)/(width*.18),2))*.16;c+=hue*ribbon*(.85+crease)+crease*ribbon*.10;total+=ribbon;
        }
        float rim=1-smoothstep(.75,1,abs(uv.y*2-1));
        return float4(linearColor(c/max(.001,total)),saturate(total)*taper*rim*_Opacity);
    }
    float y=(uv.y-.5)*.96,r=.155;
    float3 c=chromeMetal(y/r,u,t);c*=.97+.03*tex2D(_Noise,float2(u*6,y*30)).r;
    float race=_Flying*(1-smoothstep(.24,.35,_SignatureTime))*exp(-pow(u/max(.1,_BodyLength)-smoothstep(0,.24,_SignatureTime),2)*120);c+=race*.25;
    float collarA=abs(u-(_BodyLength-1.08)),collarB=abs(u-(_BodyLength-.87));
    float collar=min(collarA,collarB),ring=coverage(length(float2(collar/.055,y/.19))-1);
    c=lerp(c,chromeMetal(y/.19,u+1,t)+.12,ring);
    float a=max(coverage(abs(y)-r),ring),edge=exp(-abs(abs(y)-r)*130);
    if(_State>.5)c=lerp(c,_StateTint.rgb,edge);if(_LightBoard>.5)c=lerp(c,float3(.07,.10,.14),edge*.7);
    return float4(linearColor(c),a*taper*_Opacity);
}
float4 obsidianRender(float2 uv,float2 tags,float t)
{
    float heat=_Flying*smoothstep(0,.055,_SignatureTime)*(1-smoothstep(.16,.24,_SignatureTime));
    float coolFlash=_Flying*smoothstep(.16,.20,_SignatureTime)*(1-smoothstep(.23,.30,_SignatureTime));
    float3 lava=float3(1,.31,.025),rimColor=float3(.43,.48,.65);
    if(_Mode>6.5)return float4(0,0,0,0);
    if(_Mode>5.5)return float4(0,0,0,0);
    if(_Mode>4.5)
    {
        float2 p=(uv-.5)*2;float a=0;float3 c=0;float move=smoothstep(0,1,_Age);
        // Twelve independently tumbling analytical glass fragments share this single draw.
        for(int k=0;k<12;k++)
        {
            float angle=k*6.283185/12+.13*sin(k*7.1),radius=.18+.61*move;
            float2 center=float2(cos(angle),sin(angle))*radius;
            float spin=k*1.71+move*(4+k*.13);float2 q=p-center;
            q=mul(float2x2(cos(spin),-sin(spin),sin(spin),cos(spin)),q);
            float size=(.8+.2*sin(k*4.2))*(1-smoothstep(.65,1,_Age));
            float d=abs(q.x)/max(.001,.045*size)+abs(q.y)/max(.001,.10*size)-1;
            float fragment=coverage(d),glint=exp(-abs(d+.16)*30)*(.35+.65*pow(saturate(cos(spin)),4));
            float3 glass=lerp(float3(.025,.03,.045),float3(.75,.80,.88),glint);
            c=lerp(c,glass,fragment);a=max(a,fragment);
        }
        return float4(linearColor(c),a*smoothstep(0,.10,_Age)*(1-smoothstep(.80,1,_Age)));
    }
    if(_Mode>3.5){clip(_Reveal-uv.x);float y=abs(uv.y*2-1),cut=exp(-y*16);return float4(linearColor(float3(.8,.85,.95)),cut*(1-smoothstep(.35,1,_Age)));}
    if(_Mode>.5&&_Mode<1.5)
    {
        if(_Tail>.5||tags.x<-.5){float2 fp=uv;fp.y-=sign(fp.y)*(.04+.07*saturate(-fp.x));float4 f=feather(fp,t,float3(.006,.008,.012),float3(.22,.26,.33));f.a*=smoothstep(.015,.04,abs(uv.y));f.rgb=linearColor(f.rgb);f.a*=_Opacity;return f;}
        float2 p=uv;float x=-p.x,y=abs(p.y);
        float d=max(y-x*.43,max(x-1.12,(x-.75)*.82-y));
        float facet=step(-x*.18+.015,p.y),inner=step(y,x*.23);
        float3 c=lerp(float3(.025,.028,.04),float3(.18,.20,.24),inner);
        c*=.65+.35*facet;
        float reflection=exp(-abs(p.y-x*.23-.015*sin(t*.5))*65);c+=reflection*float3(.65,.70,.78);
        float edge=exp(-abs(d)*100);c+=edge*rimColor*.55;
        float cracks=min(segmentDistance(p,float2(-.88,.27),float2(-.46,-.12)),min(segmentDistance(p,float2(-.46,-.12),float2(-.10,.02)),segmentDistance(p,float2(-.58,.03),float2(-.83,-.22))));
        c+=exp(-cracks*95)*float3(.6,.7,.9)*_Blocked;
        c+=(exp(-cracks*90)+exp(-cracks*22)*.30+.12)*lava*heat+c*coolFlash*2+edge*coolFlash;
        c*=.96+.04*tex2D(_Noise,p*20).r;
        if(_State>.5)c=lerp(c,_StateTint.rgb,edge);if(_LightBoard>.5)c=lerp(c,float3(.012,.015,.025),edge*.5);
        return float4(linearColor(c),coverage(d)*_Opacity);
    }
    if(tags.x>1.5||tags.x>.5&&tags.y>.5)discard;
    float u=uv.x,y=(uv.y-.5)*.96,taper=smoothstep(0,.55,u)*(1-smoothstep(_BodyLength-.5,_BodyLength-.28,u));
    if(_Mode>1.5){float a=exp(-y*y*35)*(.065+.2*heat)*taper*(1-smoothstep(.7,1,abs(uv.y*2-1)));return float4(linearColor(lerp(rimColor,lava,heat)),a*_Opacity);}
    float a=0;float3 c=0;
    for(int k=0;k<6;k++)
    {
        float fan=smoothstep(0,.8,u),center=((k-2.5)*.075+.055*sin(u*1.25-t*.20+k*.83)+.018*sin(u*2.3+k*2.1))*fan;
        float r=.037+.008*sin(u*.7+k),across=(y-center)/r;
        float strand=coverage(abs(y-center)-r)*smoothstep(k*.045,k*.045+.18,u);
        float dome=sqrt(saturate(1-across*across));
        float3 glass=lerp(float3(.018,.022,.032),float3(.12,.14,.17),dome);
        float highlight=exp(-pow((across-.25)*2.2,2))*(.17+.30*pow(saturate(sin(u*.8-t*.39+k*.5)),12));glass+=highlight*float3(.75,.78,.85);
        glass+=exp(-abs(abs(across)-.92)*45)*rimColor*.24;
        float vein=min(abs(y-(.10*sin(u*3)+.025*sin(u*9))),abs(y+.13*sin(u*2.2)+.01));float cracks=exp(-vein*95);glass+=(cracks+.12)*lava*heat;glass+=_Lod*.10*dome;glass*=(_Lod>.5?1.20:1);
        glass+=coolFlash*highlight;
        c=lerp(c,glass,strand);a=max(a,strand);
    }
    float edge=exp(-abs(abs(y)-.28)*110);if(_State>.5)c=lerp(c,_StateTint.rgb,edge);
    return float4(linearColor(c),a*taper*_Opacity);
}
