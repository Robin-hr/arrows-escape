using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Step 17: the faint dotted-paper pattern behind the whole play area (one dot per cell, aligned with the board),
    /// like the reference look. Static: one quad that follows the camera, no animation, drawn under every arrow.
    /// </summary>
    public sealed class PaperDots : MonoBehaviour
    {
        /// <summary>Sorting order under the cell dots and arrows.</summary>
        public const int SortingOrder = -20;
        private BoardView board;
        private MeshRenderer meshRenderer;
        private GameObject quad;
        private Mesh mesh;
        private Material material;
        private static Texture2D dotTexture;
        private readonly Vector3[] vertices = new Vector3[4];
        private readonly Vector2[] uv = new Vector2[4];
        private readonly Color[] colors = new Color[4];
        private Color tint = new Color(0, 0, 0, .12f);
        private Rect lastRect;
        private Vector3 lastOrigin;
        /// <summary>The rendered quad, for tests.</summary>
        public MeshRenderer Renderer => meshRenderer;
        /// <summary>Current dot colour.</summary>
        public Color Tint => tint;

        /// <summary>Creates the quad under the board owner.</summary>
        public void Bind(BoardView owner)
        {
            board = owner; if (meshRenderer != null) return;
            quad = new GameObject("Paper Dots"); var go = quad;   // scene root: the board owner keeps exactly one child ("Board")
            mesh = new Mesh { name = "Paper dots" }; mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = go.AddComponent<MeshRenderer>(); meshRenderer.sortingOrder = SortingOrder;
            material = new Material(Shader.Find("Sprites/Default")) { name = "Paper dots", hideFlags = HideFlags.HideAndDontSave, mainTexture = Texture() };
            meshRenderer.sharedMaterial = material;
            mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            Refresh(true);
        }

        /// <summary>Sets the dot colour (ink on light boards, faint white on dark boards).</summary>
        public void SetTint(Color value) { tint = value; Refresh(true); }

        private void LateUpdate() => Refresh(false);

        /// <summary>Fits the quad to the camera view; only rebuilds when the view or board moves.</summary>
        public void Refresh(bool force)
        {
            if (meshRenderer == null || board == null) return;
            bool show = board.IsVisible && board.TryGetBoard(out _);
            if (meshRenderer.enabled != show) meshRenderer.enabled = show;
            var cam = Camera.main; if (cam == null || !show) return;
            float h = cam.orthographic ? cam.orthographicSize * 2 + 2 : 60, w = h * Mathf.Max(.1f, cam.aspect) + 2;
            Vector3 c = cam.transform.position; var rect = new Rect(c.x - w * .5f, c.y - h * .5f, w, h);
            Vector3 origin = board.CellToWorld(0, 0);
            if (!force && rect == lastRect && origin == lastOrigin) return;
            lastRect = rect; lastOrigin = origin;
            vertices[0] = new Vector3(rect.xMin, rect.yMin, 0); vertices[1] = new Vector3(rect.xMin, rect.yMax, 0);
            vertices[2] = new Vector3(rect.xMax, rect.yMax, 0); vertices[3] = new Vector3(rect.xMax, rect.yMin, 0);
            for (int i = 0; i < 4; i++) { uv[i] = new Vector2(vertices[i].x - origin.x + .5f, vertices[i].y - origin.y + .5f); colors[i] = tint; }
            mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.RecalculateBounds();
        }

        // One soft dot in the middle of a repeating cell tile, with mipmaps so far zooms stay calm.
        private static Texture2D Texture()
        {
            if (dotTexture != null) return dotTexture;
            const int n = 64; dotTexture = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "Paper dot", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x + .5f - n * .5f) * (x + .5f - n * .5f) + (y + .5f - n * .5f) * (y + .5f - n * .5f));
                byte a = (byte)(255 * Mathf.Clamp01(3.6f - d));
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
            dotTexture.SetPixels32(px); dotTexture.Apply(true); return dotTexture;
        }

        private void OnDestroy() { if (quad != null) DestroyImmediate(quad); if (material != null) DestroyImmediate(material); if (mesh != null) DestroyImmediate(mesh); }
    }
}
