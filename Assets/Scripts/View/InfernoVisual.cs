using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Fire-only renderer: moving ribbon, broad halo and symmetric SDF flame head.</summary>
    public sealed class InfernoVisual : MonoBehaviour
    {
        private MeshRenderer body,halo,head;
        private MaterialPropertyBlock properties;
        private Mesh quad,bodyMesh;
        private PathRibbon ribbon;
        private Vector2[] tags;
        private float lastDistance=float.NaN;
        private float temperature,impact,returnDistance;
        private bool flying,returning;
        /// <summary>Current monotonic outgoing temperature (return reverses it).</summary>
        public float Temperature=>temperature;
        /// <summary>Whether the current motion is an exit or return.</summary>
        public bool IsFlying=>flying;
        /// <summary>Switches recipes without scanning child names or creating managed strings.</summary>
        public void SetVisible(bool visible){if(body!=null){body.enabled=halo.enabled=visible;head.enabled=false;}}
        /// <summary>Builds fixed renderers once; the body mesh remains owned by ArrowView.</summary>
        public void Initialize(Mesh mesh,PathRibbon path)
        {
            properties=new MaterialPropertyBlock();ribbon=path;tags=new Vector2[path.Vertices.Length];bodyMesh=new Mesh{name="Inferno seam-free ribbon",hideFlags=HideFlags.HideAndDontSave};bodyMesh.MarkDynamic();UpdateGeometry(0);bodyMesh.triangles=path.Triangles;bodyMesh.bounds=mesh.bounds;halo=Child("Inferno soft halo",bodyMesh);body=Child("Inferno white-hot body",bodyMesh);
            quad=new Mesh{name="Procedural Inferno head quad",hideFlags=HideFlags.HideAndDontSave};quad.vertices=new[]{new Vector3(-.6f,-.8f),new Vector3(1.5f,-.8f),new Vector3(1.5f,.8f),new Vector3(-.6f,.8f)};quad.uv=new[]{new Vector2(-.6f,-.8f),new Vector2(1.5f,-.8f),new Vector2(1.5f,.8f),new Vector2(-.6f,.8f)};quad.triangles=new[]{0,1,2,0,2,3};head=Child("Inferno SDF flame head",quad);
        }
        private MeshRenderer Child(string name,Mesh mesh){var go=new GameObject(name);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;return go.AddComponent<MeshRenderer>();}
        /// <summary>Starts from cold red-orange; no effect changes logical movement.</summary>
        public void BeginFlight(){flying=true;returning=false;temperature=0;}
        /// <summary>Return uses the existing path animator in reverse.</summary>
        public void BeginReturn(float distance){flying=returning=true;returnDistance=Mathf.Max(.01f,distance);temperature=1;}
        /// <summary>Restores the idle material after motion.</summary>
        public void EndFlight(){flying=returning=false;temperature=0;}
        /// <summary>A short local white flare layered over the existing blocked feedback.</summary>
        public void Blocked(){impact=.12f;}
        /// <summary>Explicit effect time, driven by the board/preview controller.</summary>
        public void Tick(float dt){impact=Mathf.Max(0,impact-dt);}
        /// <summary>Applies all fire properties without replacing any classic or other-skin renderer.</summary>
        public void Apply(Vector2 headPoint,Vector2 direction,float distance,float length,float progress,float clock,bool light,bool lod,bool feedback,Color tint,float alpha,int sorting)
        {
            if(body==null)return;UpdateGeometry(distance);if(flying){if(returning){temperature=Mathf.Sqrt(Mathf.Clamp01(distance/returnDistance));if(distance<=0)EndFlight();}else temperature=Mathf.Max(temperature,ArrowEscape.Core.InfernoDesign.Temperature(progress));}
            properties.Clear();properties.SetFloat("_Temperature",temperature);properties.SetFloat("_Flying",flying?1:0);properties.SetFloat("_Blocked",impact/.12f);properties.SetFloat("_Clock",clock);properties.SetFloat("_WindowStart",distance);properties.SetFloat("_BodyLength",length);properties.SetFloat("_Lod",lod?1:0);properties.SetFloat("_State",feedback?1:0);properties.SetColor("_StateTint",tint);properties.SetFloat("_Opacity",alpha);
            Render(halo,2,light,sorting);Render(body,0,light,sorting);
            head.transform.localPosition=headPoint-direction*1.1f;head.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);Render(head,1,light,sorting);head.enabled=false;/* The uniform classic head replaces the flame head. */
        }
        private void UpdateGeometry(float distance)
        {
            if(distance==lastDistance)return;lastDistance=distance;
            for(int i=0;i<tags.Length;i++)
            {
                tags[i]=ribbon.SkinTags[i];if(tags[i].x!=1)continue;
                float u=distance+ribbon.UV[i].x;Vector2 before=(ribbon.Point(u)-ribbon.Point(Mathf.Max(0,u-.05f))).normalized,after=(ribbon.Point(u+.05f)-ribbon.Point(u)).normalized;
                if(Vector2.Dot(before,after)>.995f)tags[i].y=1;
            }
            bodyMesh.vertices=ribbon.Vertices;bodyMesh.uv=ribbon.UV;bodyMesh.uv2=ribbon.Edges;bodyMesh.uv3=tags;
        }
        private void Render(MeshRenderer renderer,int mode,bool light,int sorting){renderer.sharedMaterial=InfernoMaterials.Get(mode,light);renderer.SetPropertyBlock(properties);renderer.sortingOrder=sorting;}
        private void OnDestroy(){if(bodyMesh!=null){if(Application.isPlaying)Destroy(bodyMesh);else DestroyImmediate(bodyMesh);}if(quad!=null){if(Application.isPlaying)Destroy(quad);else DestroyImmediate(quad);}}
    }
}
