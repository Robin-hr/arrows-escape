using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Twelve reusable fifty-particle systems: the live total cannot exceed six hundred.</summary>
    public sealed class SkinParticlePool
    {
        private static readonly SkinParticlePool[] instances=new SkinParticlePool[16];
        private int instanceSlot=-1;
        private readonly Transform owner;
        private readonly EffectsController legacyEffects;
        private readonly ParticleSystem[] systems=new ParticleSystem[12];
        private readonly ParticleSystemRenderer[] renderers=new ParticleSystemRenderer[12];
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        private readonly SkinTrailRibbon[] ribbons=new SkinTrailRibbon[12];
        private readonly SpriteRenderer[] frost=new SpriteRenderer[32];
        private readonly float[] frostTime=new float[32];
        private int next,nextFrost;
        private uint seed=7177;
        private readonly ParticleSystem.MinMaxGradient fireColor,normalColor,rareColor;
        private readonly ParticleSystem.MinMaxCurve normalSize,rareSize;
        /// <summary>Refreshes a pooled lingering signature beam.</summary>
        public void Beam(int index,ArrowSkin skin,Vector2 start,Vector2 end,bool light)=>ribbons[index%ribbons.Length].Set(skin,start,end,light);
        /// <summary>Number of fixed pooled emitters.</summary>
        public int PoolCount=>systems.Length;
        /// <summary>Hard particle budget includes active shop/gallery previews.</summary>
        /// <summary>Reserve the old sprite pool's capacity whenever it can play completion confetti.</summary>
        public static int NativeCapacity
        {get{PruneDestroyedHosts();int capacity=SkinEffectRules.ParticleLimit;for(int i=0;i<instances.Length;i++)if(instances[i]!=null&&instances[i].legacyEffects!=null){capacity-=ViewTheme.ParticleCapacity;break;}return Mathf.Max(0,capacity-ElementalSignatures.ActiveGlassFragments);}}
        public static int GlobalLiveCount{get{PruneDestroyedHosts();int n=0;for(int i=0;i<instances.Length;i++)if(instances[i]!=null)n+=instances[i].LiveCount;return n;}}
        /// <summary>Priority edge fragments reclaim old pooled motes without exceeding the shared cap.</summary>
        public static void EnforceBudget()
        {
            int limit=NativeCapacity,live=GlobalLiveCount;
            for(int i=0;i<instances.Length&&live>limit;i++){var pool=instances[i];if(pool==null)continue;for(int j=0;j<pool.systems.Length&&live>limit;j++){var system=pool.systems[j];if(system==null)continue;int count=system.particleCount;if(count==0)continue;system.Clear(false);live-=count;}}
        }
        private static void PruneDestroyedHosts()
        {for(int i=0;i<instances.Length;i++)if(instances[i]!=null&&instances[i].owner==null)instances[i]=null;}
        /// <summary>Unregisters a destroyed host from the global budget.</summary>
        public void Dispose(){Clear();for(int i=0;i<ribbons.Length;i++)ribbons[i].Dispose();if(instanceSlot>=0&&instances[instanceSlot]==this)instances[instanceSlot]=null;}
        /// <summary>Current native particle count across all fixed-capacity systems.</summary>
        public int LiveCount {get{int n=0;for(int i=0;i<systems.Length;i++)if(systems[i]!=null)n+=systems[i].particleCount;return n;}}
        /// <summary>Creates every emitter and frost mark before gameplay.</summary>
        public SkinParticlePool(Transform parent)
        {
            owner=parent;PruneDestroyedHosts();
            legacyEffects=parent.GetComponent<EffectsController>();
            for(int i=0;i<instances.Length;i++)if(instances[i]==null){instances[i]=this;instanceSlot=i;break;}
            var root=new GameObject("Pooled skin effects").transform;root.SetParent(parent,false);
            for(int i=0;i<systems.Length;i++)
            {
                ribbons[i]=new SkinTrailRibbon(root,i);
                var go=new GameObject("Skin emitter "+i);go.transform.SetParent(root,false);var ps=go.AddComponent<ParticleSystem>();systems[i]=ps;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(i+1)*7177;
                var main=ps.main;main.playOnAwake=false;main.loop=false;main.maxParticles=50;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.startLifetime=.65f;main.startSize=.065f;main.gravityModifier=.035f;
                var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
                var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(.7f,.45f),new GradientAlphaKey(0,1)});color.color=gradient;
                var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                renderers[i]=go.GetComponent<ParticleSystemRenderer>();renderers[i].renderMode=ParticleSystemRenderMode.Billboard;renderers[i].sortingOrder=ViewTheme.EffectsSortingOrder;renderers[i].sharedMaterial=SkinMaterials.Glow(true);
            }
            var rareGradient=new Gradient();rareGradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.9f,.12f),new GradientAlphaKey(.7f,.55f),new GradientAlphaKey(0,1)});rareColor=new ParticleSystem.MinMaxGradient(rareGradient);rareSize=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
            normalColor=systems[0].colorOverLifetime.color;normalSize=systems[0].sizeOverLifetime.size;
            var fireGradient=new Gradient();fireGradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.95f,.65f),new GradientAlphaKey(0,1)});fireColor=new ParticleSystem.MinMaxGradient(fireGradient);
            for(int i=0;i<frost.Length;i++){var go=new GameObject("Frost footprint "+i);go.transform.SetParent(root,false);frost[i]=go.AddComponent<SpriteRenderer>();frost[i].sprite=SpriteFactory.GetSquareSprite();frost[i].sharedMaterial=SkinMaterials.Glow(true);frost[i].sortingOrder=ViewTheme.EffectsSortingOrder;frost[i].enabled=false;}
        }
        private float Random(){seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;return (seed&0xffffff)/16777216f;}
        /// <summary>Emits a deterministic styled puff; the chosen pool slot is reused in place.</summary>
        public void Emit(ArrowSkin skin,Vector2 point,Vector2 direction,int count,bool light,bool returning=false)
        {
            if(skin==null||instanceSlot<0)return;int slot=next++%systems.Length;var ps=systems[slot];if(ps==null)return;
            var motion=ps.noise;motion.enabled=skin.IsRare;if(skin.IsRare){motion.strength=.12f;motion.frequency=.8f;motion.scrollSpeed=.2f;}var spin=ps.rotationOverLifetime;spin.enabled=skin.IsRare;if(skin.IsRare)spin.z=new ParticleSystem.MinMaxCurve(skin.Index==5?5f:1f);var main=ps.main;main.gravityModifier=skin.Index==5?.12f:.035f;
            if(skin.Index==1){EmitFire(slot,point,direction,count,light,returning);return;}
            var renderer=renderers[slot];renderer.sharedMaterial=SkinMaterials.Glow(light);properties.Clear();properties.SetFloat("_Art",2);properties.SetFloat("_Shape",skin.Index);properties.SetColor("_Tint",Color.white);renderer.SetPropertyBlock(properties);
            renderer.renderMode=ParticleSystemRenderMode.Billboard;var normalColors=ps.colorOverLifetime;normalColors.color=skin.IsRare?rareColor:normalColor;var normalSizes=ps.sizeOverLifetime;normalSizes.size=skin.IsRare?rareSize:normalSize;
            int remaining=Mathf.Max(0,NativeCapacity-GlobalLiveCount);
            for(int i=0;i<Mathf.Min(count,Mathf.Min(50,remaining));i++)
            {
                Vector2 side=new Vector2(-direction.y,direction.x);Vector2 jitter=side*(Random()-.5f)*.2f;
                var particle=new ParticleSystem.EmitParams();particle.position=point+jitter;particle.velocity=(returning?direction:-direction)*(.15f+Random()*.8f)+side*(Random()-.5f)*.8f;
                
                particle.startLifetime=skin.TrailSeconds*(.65f+Random()*.35f);particle.startSize=skin.Index==3?.15f+Random()*.18f:.035f+Random()*.09f;
                particle.startColor=skin.Index==6?Color.HSVToRGB(Random(),.65f,1):skin.Index==1&&Random()>.83f?Color.white:Color.Lerp(skin.Primary,skin.Secondary,Random());particle.rotation=Random()*360;ps.Emit(particle,1);
            }
            ps.Pause(false);
        }
        private void EmitFire(int slot,Vector2 point,Vector2 forward,int count,bool light,bool returning)
        {
            var ps=systems[slot];var renderer=renderers[slot];renderer.sharedMaterial=InfernoMaterials.Get(3,light);renderer.renderMode=ParticleSystemRenderMode.Billboard;properties.Clear();properties.SetFloat("_Round",slot%2);renderer.SetPropertyBlock(properties);
            var fireColors=ps.colorOverLifetime;fireColors.color=fireColor;var fireSizes=ps.sizeOverLifetime;fireSizes.size=new ParticleSystem.MinMaxCurve(1);
            int available=Mathf.Min(50,Mathf.Min(count,Mathf.Max(0,NativeCapacity-GlobalLiveCount)));Vector2 side=new Vector2(-forward.y,forward.x);
            for(int i=0;i<available;i++)
            {
                float speed=(count>2?4f:.8f)+Random()*(count>2?10f:2f),spread=(Random()-.5f)*(count>2?4f:1.5f);var p=new ParticleSystem.EmitParams();p.position=point+side*(Random()-.5f)*.12f;p.velocity=forward*(returning?speed:-speed)+side*spread;p.startLifetime=.32f+Random()*.22f;p.startSize=.035f+Random()*.065f;
                p.startColor=Color.Lerp(Color.white,new Color(1,.945f,.69f),Random()*.7f);p.rotation=Mathf.Atan2(p.velocity.y,p.velocity.x)*Mathf.Rad2Deg;ps.Emit(p,1);
            }
            ps.Pause(false);
        }
        /// <summary>Reuses a faint cell-size frost mark, fading independently over 1.2 seconds.</summary>
        public void Frost(Vector2 point)
        {
            int i=nextFrost++%frost.Length;frostTime[i]=1.2f;var view=frost[i];view.transform.position=new Vector3(Mathf.Round(point.x*2)*.5f,Mathf.Round(point.y*2)*.5f,0);view.transform.localScale=Vector3.one*.65f;view.color=new Color(.2f,.65f,.95f,.09f);view.enabled=true;
            properties.Clear();properties.SetFloat("_Art",1);properties.SetColor("_Tint",Color.white);view.SetPropertyBlock(properties);
        }
        /// <summary>Explicit simulation keeps effects frozen during gameplay pause; no managed allocations.</summary>
        public void Tick(float dt)
        {
            for(int i=0;i<ribbons.Length;i++)ribbons[i].Tick(dt);
            for(int i=0;i<systems.Length;i++)if(systems[i]!=null&&systems[i].particleCount>0)systems[i].Simulate(dt,false,false,false);
            for(int i=0;i<frost.Length;i++)if(frostTime[i]>0)
            {frostTime[i]=Mathf.Max(0,frostTime[i]-dt);frost[i].color=new Color(.2f,.65f,.95f,.09f*frostTime[i]/1.2f);frost[i].enabled=frostTime[i]>0;}
        }
        /// <summary>Clears all native effects without deleting or allocating pool objects.</summary>
        public void Clear()
        {for(int i=0;i<ribbons.Length;i++)ribbons[i].Clear();for(int i=0;i<systems.Length;i++)if(systems[i]!=null)systems[i].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);for(int i=0;i<frost.Length;i++)if(frost[i]!=null){frostTime[i]=0;frost[i].enabled=false;}}
    }
}
