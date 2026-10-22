using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>A continuous sampled surface for wide rare materials, without overlapping corner discs.</summary>
    public sealed class RareRibbonSurface
    {
        private readonly PathRibbon path;
        private readonly Vector3[] vertices;
        private readonly Vector2[] uv;
        private readonly Vector2[] edges;
        private float previous=float.NaN;
        /// <summary>Sample spacing in cells; dense enough that rounded bends stay smooth.</summary>
        public const float Step=.2f;
        /// <summary>Owned reusable mesh. Only travel updates its prepared buffers.</summary>
        public Mesh Mesh {get;}
        /// <summary>Creates fixed topology outside animation paths.</summary>
        public RareRibbonSurface(PathRibbon ribbon)
        {
            path=ribbon;int sections=Mathf.CeilToInt(path.BodyLength/Step)+2;
            vertices=new Vector3[(sections+1)*2];uv=new Vector2[vertices.Length];edges=new Vector2[vertices.Length];
            var triangles=new int[sections*6];for(int i=0;i<sections;i++){int v=i*2,t=i*6;triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+3;triangles[t+3]=v;triangles[t+4]=v+3;triangles[t+5]=v+1;}
            Mesh=new Mesh{name="Continuous rare material surface",hideFlags=HideFlags.HideAndDontSave};Mesh.MarkDynamic();Build(0);Mesh.triangles=triangles;
        }
        /// <summary>Samples the existing authoritative track, with a short rounded turn transition.</summary>
        public void Build(float distance)
        {
            if(previous==distance)return;previous=distance;int sections=vertices.Length/2-1;
            for(int i=0;i<=sections;i++)
            {
                float u=i==0?distance:i==sections?distance+path.BodyLength:Mathf.Clamp(Mathf.Floor(distance/Step)*Step+i*Step,distance,distance+path.BodyLength);
                float s=u-distance;Vector2 center=path.Point(u);
                Vector2 incoming=(center-path.Point(Mathf.Max(0,u-.001f))).normalized;
                Vector2 outgoing=(path.Point(Mathf.Min(path.TrackLength,u+.001f))-center).normalized;
                if(incoming==Vector2.zero)incoming=outgoing;if(outgoing==Vector2.zero)outgoing=incoming;
                Vector2 normal=new Vector2(-incoming.y-outgoing.y,incoming.x+outgoing.x)/Mathf.Max(.1f,1+Vector2.Dot(incoming,outgoing));
                int v=i*2;vertices[v]=center-normal*.13f;vertices[v+1]=center+normal*.13f;
                uv[v]=new Vector2(s,0);uv[v+1]=new Vector2(s,1);edges[v]=-normal;edges[v+1]=normal;
            }
            Mesh.vertices=vertices;Mesh.uv=uv;Mesh.uv2=edges;Mesh.RecalculateBounds();
            var bounds=Mesh.bounds;bounds.Expand(2);Mesh.bounds=bounds;
        }
        /// <summary>Releases the single owned mesh.</summary>
        public void Dispose(){if(Mesh==null)return;if(Application.isPlaying)Object.Destroy(Mesh);else Object.DestroyImmediate(Mesh);}
    }
}
