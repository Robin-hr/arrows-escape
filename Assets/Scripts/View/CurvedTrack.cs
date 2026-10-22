using System.Collections.Generic;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Turns a cell-centre polyline into a smooth track: every bend between the tail and the head cell becomes a
    /// rounded curve (quadratic fillet) instead of a hard 90-degree corner. The head cell keeps its exact position.
    /// </summary>
    public static class CurvedTrack
    {
        /// <summary>Bend radius in cells; below half a cell so back-to-back stair bends never overlap.</summary>
        public const float Radius = 0.36f;
        /// <summary>Points per bend; enough for a visually round curve at phone sizes.</summary>
        public const int BendSegments = 6;

        /// <summary>
        /// Builds the smoothed points. <paramref name="headIndex"/> is the original index of the head cell;
        /// <paramref name="headDistance"/> returns its distance along the new track (the arrow's body length).
        /// </summary>
        public static Vector2[] Build(Vector2[] points, int headIndex, out float headDistance)
        {
            var result = new List<Vector2>(points.Length * (BendSegments + 1));
            int headOut = 0;
            for (int i = 0; i < points.Length; i++)
            {
                bool interior = i > 0 && i < points.Length - 1 && i != headIndex;
                if (interior)
                {
                    Vector2 inDir = points[i] - points[i - 1], outDir = points[i + 1] - points[i];
                    float inLen = inDir.magnitude, outLen = outDir.magnitude;
                    if (inLen > 1e-4f && outLen > 1e-4f)
                    {
                        inDir /= inLen; outDir /= outLen;
                        if (Mathf.Abs(Vector2.Dot(inDir, outDir)) < 0.999f)
                        {
                            float r = Mathf.Min(Radius, inLen * 0.5f, outLen * 0.5f);
                            Vector2 start = points[i] - inDir * r, end = points[i] + outDir * r;
                            for (int s = 0; s <= BendSegments; s++)
                            {
                                float t = (float)s / BendSegments, u = 1 - t;
                                result.Add(u * u * start + 2 * u * t * points[i] + t * t * end);
                            }
                            continue;
                        }
                    }
                }
                if (i == headIndex) headOut = result.Count;
                result.Add(points[i]);
            }
            headDistance = 0;
            for (int i = 1; i <= headOut; i++) headDistance += Vector2.Distance(result[i - 1], result[i]);
            return result.ToArray();
        }
    }
}
