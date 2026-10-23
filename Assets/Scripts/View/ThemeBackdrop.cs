using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Play-area background (Step 14J): a smooth diagonal gradient, drifting soft light orbs (gradient themes) or
    /// floating rotating 3D solids (premium themes, pre-rendered rotation frames), and a frosted-glass board panel.
    /// Under the panel the solids are drawn from a pre-blurred atlas (sprite mask), so the board area shows a blurred
    /// background and the arrows stay clear. No allocations per frame.
    /// </summary>
    public sealed class ThemeBackdrop : MonoBehaviour
    {
        /// <summary>Animated pieces per premium wallpaper.</summary>
        public const int Pieces = 36;
        /// <summary>Soft light orbs per gradient theme.</summary>
        public const int Orbs = 5;
        private const int AtlasGrid = 16, Frames = 48, SphereTile = 240;
        private static Sprite[] sharpSprites, blurSprites;
        private static Material spriteMaterial;
        private static Sprite panelSprite;
        private Camera camera;
        private BoardView board;
        private GameController game;
        private MeshRenderer gradient;
        private Mesh gradientMesh;
        private readonly Color[] gradientColors = new Color[9];
        private SpriteRenderer[] sharp, soft;
        private SpriteRenderer panel;
        private SpriteMask mask;
        private float[] u, v, du, dv, size, phase, fps, hue, depth;
        private int[] shape, frame;
        private float clock;
        private bool sampleBoard; private Bounds sampleBounds;
        private int selected = -2;
        /// <summary>Equipped theme identity; Paper leaves the original presentation untouched.</summary>
        public string ThemeId { get; private set; } = "paper";
        /// <summary>Animated pieces currently on screen.</summary>
        public int ParticleCount => selected < 0 ? 0 : ThemeDesign.IsAnimated(selected) ? Pieces : Orbs;
        /// <summary>Batched draws: gradient, sharp pieces, blurred pieces and the glass panel.</summary>
        public int DrawCount => selected < 0 ? 0 : ThemeDesign.IsAnimated(selected) ? 4 : 3;
        /// <summary>Explicit theme clock for pause and allocation tests.</summary>
        public float Clock => clock;
        /// <summary>True when the current theme animates 3D solids.</summary>
        public bool IsAnimated => selected >= 0 && ThemeDesign.IsAnimated(selected);
        /// <summary>The frosted board panel.</summary>
        public SpriteRenderer Panel => panel;
        /// <summary>Optional fixed board panel for a shop detail miniature.</summary>
        public void SetPreviewBoard(Bounds bounds) { sampleBoard = true; sampleBounds = bounds; Tick(0); }

        private static void LoadArt()
        {
            if (spriteMaterial == null) spriteMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Theme sprites", hideFlags = HideFlags.HideAndDontSave };
            if (sharpSprites == null || sharpSprites[0] == null) sharpSprites = Slice("Themes/geo_atlas", 128);
            if (blurSprites == null || blurSprites[0] == null) blurSprites = Slice("Themes/geo_atlas_blur", 64);
            if (panelSprite == null)
            {
                var t = Resources.Load<Texture2D>("Themes/panel_round");
                if (t != null) { panelSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), t.width, 0, SpriteMeshType.FullRect, new Vector4(30, 30, 30, 30)); panelSprite.hideFlags = HideFlags.HideAndDontSave; }
            }
        }
        /// <summary>One sharp pre-rendered 3D solid frame (shape * 48 + frame, 240 = sphere) for UI decorations.</summary>
        public static Sprite GeoSprite(int index) { LoadArt(); return sharpSprites == null || index < 0 || index > SphereTile ? null : sharpSprites[index]; }
        /// <summary>Rotation frames per solid in the shared atlas.</summary>
        public const int GeoFrames = Frames;
        private static Sprite[] Slice(string path, int tile)
        {
            var sprites = new Sprite[SphereTile + 1]; var t = Resources.Load<Texture2D>(path); if (t == null) return sprites;
            for (int i = 0; i <= SphereTile; i++)
            {
                int x = i % AtlasGrid, y = AtlasGrid - 1 - i / AtlasGrid;
                sprites[i] = Sprite.Create(t, new Rect(x * tile, y * tile, tile, tile), new Vector2(.5f, .5f), tile, 0, SpriteMeshType.FullRect);
                sprites[i].hideFlags = HideFlags.HideAndDontSave;
            }
            return sprites;
        }

        /// <summary>Binds a camera and optionally a real play board; preview callers own their camera.</summary>
        public void Bind(Camera target, BoardView view = null, GameController controller = null)
        {
            camera = target; board = view; game = controller;
            if (gradient != null) return;
            LoadArt();
            gradientMesh = new Mesh { name = "Theme gradient", hideFlags = HideFlags.HideAndDontSave };
            var verts = new Vector3[9]; var uv = new Vector2[9]; var tris = new int[24]; int t = 0;
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) { verts[y * 3 + x] = new Vector3(x * .5f - .5f, y * .5f - .5f); uv[y * 3 + x] = new Vector2(.5f, .5f); }
            for (int y = 0; y < 2; y++) for (int x = 0; x < 2; x++) { int i = y * 3 + x; tris[t++] = i; tris[t++] = i + 3; tris[t++] = i + 4; tris[t++] = i; tris[t++] = i + 4; tris[t++] = i + 1; }
            gradientMesh.vertices = verts; gradientMesh.uv = uv; gradientMesh.triangles = tris; gradientMesh.colors = gradientColors;
            var go = Child("Theme gradient"); go.AddComponent<MeshFilter>().sharedMesh = gradientMesh;
            gradient = go.AddComponent<MeshRenderer>(); gradient.sharedMaterial = spriteMaterial; gradient.sortingOrder = -30; gradient.enabled = false;
            var props = new MaterialPropertyBlock(); props.SetTexture("_MainTex", Texture2D.whiteTexture); gradient.SetPropertyBlock(props);
            sharp = new SpriteRenderer[Pieces]; soft = new SpriteRenderer[Pieces];
            u = new float[Pieces]; v = new float[Pieces]; du = new float[Pieces]; dv = new float[Pieces]; size = new float[Pieces]; phase = new float[Pieces]; fps = new float[Pieces]; hue = new float[Pieces]; depth = new float[Pieces];
            shape = new int[Pieces]; frame = new int[Pieces];
            for (int i = 0; i < Pieces; i++)
            {
                sharp[i] = Child("Theme solid " + i).AddComponent<SpriteRenderer>(); sharp[i].sharedMaterial = spriteMaterial; sharp[i].maskInteraction = SpriteMaskInteraction.VisibleOutsideMask; sharp[i].enabled = false;
                soft[i] = Child("Theme solid blurred " + i).AddComponent<SpriteRenderer>(); soft[i].sharedMaterial = spriteMaterial; soft[i].maskInteraction = SpriteMaskInteraction.VisibleInsideMask; soft[i].enabled = false;
            }
            var maskObject = Child("Board glass mask"); mask = maskObject.AddComponent<SpriteMask>(); mask.sprite = panelSprite;
            mask.isCustomRangeActive = true; mask.backSortingOrder = -27; mask.frontSortingOrder = -10; mask.enabled = false;
            panel = Child("Board glass panel").AddComponent<SpriteRenderer>(); panel.sprite = panelSprite; panel.drawMode = SpriteDrawMode.Sliced; panel.sharedMaterial = spriteMaterial; panel.sortingOrder = -6; panel.enabled = false;
        }

        private GameObject Child(string name) { var go = new GameObject(name); go.layer = gameObject.layer; go.transform.SetParent(transform, false); return go; }

        /// <summary>Changes only the visual recipe; no settings or progress data are written.</summary>
        public void SetTheme(string id)
        {
            ThemeId = ThemeDesign.Index(id) < 0 ? "paper" : id; int next = ThemeDesign.Index(ThemeId); if (next == selected) return; selected = next; clock = 0;
            if (gradient == null) return;
            bool on = next >= 0, animated = on && ThemeDesign.IsAnimated(next);
            gradient.enabled = on;
            if (on)
            {
                var s = ThemeDesign.Stops(next);
                for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                {
                    float t = Mathf.Clamp01(0.65f * (1 - y * .5f) + 0.35f * (x * .5f));
                    gradientColors[y * 3 + x] = t < .5f ? Color.Lerp(s[0], s[1], t * 2) : Color.Lerp(s[1], s[2], t * 2 - 1);
                }
                gradientMesh.colors = gradientColors;
                Seed(next);
            }
            int count = !on ? 0 : animated ? Pieces : Orbs;
            for (int i = 0; i < Pieces; i++) { sharp[i].enabled = animated && i < count; soft[i].enabled = on && i < count; soft[i].maskInteraction = animated ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None; }
            mask.enabled = animated && (board != null || sampleBoard);
            panel.enabled = on && (board != null || sampleBoard);
            panel.color = next == 6 ? new Color(0.10f, 0.12f, 0.32f, 0.46f) : animated ? new Color(0.02f, 0.03f, 0.10f, 0.42f) : new Color(0f, 0f, 0.04f, 0.26f);
            Tick(0);
        }

        // Deterministic layout per theme so every device shows the same wallpaper.
        private void Seed(int index)
        {
            uint state = (uint)(index * 7919 + 17);
            float Next() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return (state & 0xFFFFFF) / (float)0xFFFFFF; }
            bool animated = ThemeDesign.IsAnimated(index);
            for (int i = 0; i < Pieces; i++)
            {
                u[i] = Next(); v[i] = Next(); depth[i] = Next(); hue[i] = Next() * 0.3f; phase[i] = Next() * Frames; fps[i] = 3f + Next() * 5f;
                shape[i] = animated ? (Next() < 0.2f ? 5 : (int)(Next() * 5) % 5) : 5;
                size[i] = animated ? Mathf.Lerp(0.05f, 0.21f, depth[i] * depth[i]) : Mathf.Lerp(0.45f, 0.8f, Next());
                float speed = animated ? Mathf.Lerp(0.008f, 0.03f, depth[i]) : 0.01f + Next() * 0.012f;
                float dir = index == 8 ? -1 : 1;             // golden facets fall, the others rise
                du[i] = (Next() - 0.5f) * speed * 0.6f; dv[i] = index == 7 ? 0 : speed * dir;
                if (!animated) { du[i] = (Next() - 0.5f) * 0.02f; dv[i] = (Next() - 0.5f) * 0.02f; }
                frame[i] = -1;
            }
        }

        /// <summary>Advances the motion; no per-frame allocations.</summary>
        public void Tick(float dt)
        {
            if (selected < 0 || camera == null || gradient == null || !(dt >= 0) || float.IsInfinity(dt)) return;
            clock += dt;
            float height = camera.orthographicSize * 2, width = height * camera.aspect; Vector3 center = camera.transform.position; center.z = 0;
            gradient.transform.position = center; gradient.transform.localScale = new Vector3(width * 1.04f, height * 1.04f, 1);
            bool animated = ThemeDesign.IsAnimated(selected); int count = animated ? Pieces : Orbs; var palette = ThemeDesign.Palette(selected);
            for (int i = 0; i < count; i++)
            {
                if (selected == 7) { float a = 0.03f * (0.4f + depth[i]) * dt; float x = u[i] - .5f, y = v[i] - .5f; u[i] = .5f + x * Mathf.Cos(a) - y * Mathf.Sin(a); v[i] = .5f + x * Mathf.Sin(a) + y * Mathf.Cos(a); }
                u[i] += du[i] * dt; v[i] += dv[i] * dt;
                float margin = size[i] * 0.6f;
                if (v[i] > 1 + margin) v[i] -= 1 + 2 * margin; else if (v[i] < -margin) v[i] += 1 + 2 * margin;
                if (u[i] > 1 + margin) u[i] -= 1 + 2 * margin; else if (u[i] < -margin) u[i] += 1 + 2 * margin;
                Vector3 p = center + new Vector3((u[i] - .5f) * width, (v[i] - .5f) * height, 0);
                float s = size[i] * height;
                int f = shape[i] == 5 ? SphereTile : shape[i] * Frames + ((int)(phase[i] + clock * fps[i]) % Frames);
                if (f != frame[i] && sharpSprites != null && sharpSprites[f] != null) { frame[i] = f; sharp[i].sprite = sharpSprites[f]; soft[i].sprite = blurSprites[f]; }
                float k = Mathf.Repeat(u[i] * 0.7f + v[i] * 0.3f + hue[i], 1f) * 2f;
                Color tint = k < 1 ? Color.Lerp(palette[0], palette[1], k) : Color.Lerp(palette[1], palette[2], k - 1);
                int order = -26 + (int)(depth[i] * 12);
                if (animated)
                {
                    sharp[i].transform.position = p; sharp[i].transform.localScale = Vector3.one * s; sharp[i].color = tint; sharp[i].sortingOrder = order;
                    soft[i].transform.position = p; soft[i].transform.localScale = Vector3.one * s; tint.a = 0.9f; soft[i].color = tint; soft[i].sortingOrder = order;
                }
                else { soft[i].transform.position = p; soft[i].transform.localScale = Vector3.one * s; tint.a = 0.16f; soft[i].color = tint; soft[i].sortingOrder = -28; }
            }
            Bounds area = default; bool hasArea = false;
            if (sampleBoard) { area = sampleBounds; hasArea = true; }
            if (board != null) { area = board.ContentBounds; hasArea = board.IsVisible; }
            if (hasArea)
            {
                Vector2 extent = (Vector2)area.size + new Vector2(.7f, .7f);
                panel.transform.position = area.center; panel.size = extent; panel.transform.localScale = Vector3.one;
                mask.transform.position = area.center; mask.transform.localScale = new Vector3(extent.x, extent.y, 1);
            }
            panel.enabled = hasArea; mask.enabled = hasArea && animated;
        }

        /// <summary>Gameplay pause freezes motion; hidden boards suppress their backdrop.</summary>
        public void AdvanceFrame(float dt)
        {
            bool visible = board == null || board.IsVisible;
            if (gradient != null && selected >= 0) gradient.enabled = visible;
            if (visible && (game == null || !game.IsPaused)) Tick(dt);
        }
        private void Update() { if (Application.isPlaying) AdvanceFrame(Time.deltaTime); }
        private void OnDestroy() { if (gradientMesh != null) { if (Application.isPlaying) Destroy(gradientMesh); else DestroyImmediate(gradientMesh); } }
    }
}
