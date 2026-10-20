using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Reusable ribbon buffers for a moving window of a fixed polyline track.</summary>
    public sealed class PathRibbon
    {
        private readonly Vector2[] track;
        private readonly float[] distances;
        private readonly float length;
        /// <summary>Fixed vertex buffer, reused by every build.</summary>
        public Vector3[] Vertices { get; }
        /// <summary>Fixed triangle indices.</summary>
        public int[] Triangles { get; }
        /// <summary>Total track distance.</summary>
        public float TrackLength => distances[distances.Length-1];
        /// <summary>Creates capacity for each segment, rounded join and arrowhead.</summary>
        public PathRibbon(Vector2[] points,float bodyLength)
        {
            track=(Vector2[])points.Clone();length=bodyLength;distances=new float[points.Length];
            for(int i=1;i<points.Length;i++)distances[i]=distances[i-1]+Vector2.Distance(points[i-1],points[i]);
            int sections=points.Length-1;
            Vertices=new Vector3[sections*4+points.Length*13+3];
            Triangles=new int[sections*6+points.Length*36+3];int t=0;
            for(int i=0;i<sections;i++){int v=i*4;Triangles[t++]=v;Triangles[t++]=v+1;Triangles[t++]=v+2;Triangles[t++]=v;Triangles[t++]=v+2;Triangles[t++]=v+3;}
            for(int i=0;i<points.Length;i++){int v=sections*4+i*13;for(int j=0;j<12;j++){Triangles[t++]=v;Triangles[t++]=v+1+j;Triangles[t++]=v+1+(j+1)%12;}}
            int head=Vertices.Length-3;Triangles[t++]=head;Triangles[t++]=head+1;Triangles[t]=head+2;
        }
        /// <summary>Position at a distance along the track.</summary>
        public Vector2 Point(float distance)
        {
            distance=Mathf.Clamp(distance,0,TrackLength);
            for(int i=1;i<track.Length;i++)if(distance<=distances[i])return Vector2.Lerp(track[i-1],track[i],(distance-distances[i-1])/(distances[i]-distances[i-1]));
            return track[track.Length-1];
        }
        /// <summary>Writes a constant-width rounded ribbon window without allocating.</summary>
        public void Build(float start)
        {
            float end=Mathf.Min(TrackLength,start+length),shaftEnd=Mathf.Max(start,end-ViewTheme.PathHeadLength);
            float radius=ViewTheme.PathLineWidth*.5f;
            for(int i=0;i<track.Length-1;i++)
            {
                Vector2 a=Point(Mathf.Clamp(distances[i],start,shaftEnd)),b=Point(Mathf.Clamp(distances[i+1],start,shaftEnd));
                Vector2 d=(track[i+1]-track[i]).normalized,n=new Vector2(-d.y,d.x)*radius;int v=i*4;
                Vertices[v]=a+n;Vertices[v+1]=b+n;Vertices[v+2]=b-n;Vertices[v+3]=a-n;
            }
            // Twelve sectors give six segments around each semicircular cap/corner.
            for(int i=0;i<track.Length;i++)
            {
                Vector2 center=Point(Mathf.Clamp(distances[i],start,shaftEnd));int v=(track.Length-1)*4+i*13;
                Vertices[v]=center;
                for(int j=0;j<12;j++){float a=j*Mathf.PI/6;Vertices[v+j+1]=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}
            }
            Vector2 tip=Point(end),direction=(tip-Point(Mathf.Max(start,end-.01f))).normalized;
            Vector2 side=new Vector2(-direction.y,direction.x)*ViewTheme.PathHeadWidth*.5f;
            int h=Vertices.Length-3;Vertices[h]=tip;Vertices[h+1]=tip-direction*ViewTheme.PathHeadLength+side;Vertices[h+2]=tip-direction*ViewTheme.PathHeadLength-side;
        }
    }
}
