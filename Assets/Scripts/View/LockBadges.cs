using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Step 15: padlock and key badges. Each padlocked arrow wears a coloured padlock on the middle of its body and its
    /// key arrow wears a key in the same colour. When the key leaves, the padlock pops open; tapping a padlocked arrow
    /// rattles the padlock and pulses the key so the player sees what to free first.
    /// </summary>
    public sealed class LockBadges : MonoBehaviour
    {
        /// <summary>Pair colours, assigned in padlocked-arrow id order.</summary>
        public static readonly Color[] PairColors =
        {
            new Color32(255, 190, 40, 255), new Color32(60, 214, 255, 255), new Color32(255, 84, 172, 255), new Color32(124, 230, 80, 255),
            new Color32(255, 128, 48, 255), new Color32(170, 120, 255, 255), new Color32(255, 70, 80, 255), new Color32(40, 230, 190, 255),
        };
        /// <summary>Badge diameter in cells.</summary>
        public const float BadgeSize = 0.78f;
        /// <summary>Above every arrow layer.</summary>
        public const int SortingOrder = 30;

        private sealed class Badge
        {
            public int ArrowId, PairId; public bool IsKey; public GameObject Root; public SpriteRenderer Disc, Glyph; public PathArrowView Path;
            public float Wobble, Pop = -1; public System.Action Follow;
        }
        private readonly List<Badge> badges = new List<Badge>();
        private readonly Dictionary<int, int> pairOf = new Dictionary<int, int>();   // locked arrow id -> pair index
        private BoardView board; private GameController game; private ArrowAnimator animator;
        private AudioSource audioSource; private static AudioClip unlockClip, rattleClip;
        private static Sprite disc, lockGlyph, keyGlyph;

        /// <summary>Live badges (padlocks and keys), for tests.</summary>
        public int Count { get { int c = 0; foreach (var b in badges) if (b.Pop < 0) c++; return c; } }
        /// <summary>Live padlocks, for tests.</summary>
        public int LockCount { get { int c = 0; foreach (var b in badges) if (b.Pop < 0 && !b.IsKey) c++; return c; } }
        /// <summary>Number of padlock pops played this attempt, for tests.</summary>
        public int UnlockCount { get; private set; }
        /// <summary>Number of padlock rattles played this attempt, for tests.</summary>
        public int RattleCount { get; private set; }

        /// <summary>Hooks the board and game events; safe to call twice.</summary>
        public void Bind(BoardView boardView, GameController controller)
        {
            Unbind(); board = boardView; game = controller; animator = GetComponent<ArrowAnimator>();
            if (game != null) { game.LevelStarted += Rebuild; game.ArrowExited += OnExited; game.ArrowRestored += OnRestored; game.MoveBlocked += OnBlocked; }
            Rebuild();
        }
        private void Unbind()
        {
            if (game != null) { game.LevelStarted -= Rebuild; game.ArrowExited -= OnExited; game.ArrowRestored -= OnRestored; game.MoveBlocked -= OnBlocked; }
        }
        private void OnDestroy() { Unbind(); Clear(); }
        private void Awake() { if (board == null) Bind(GetComponent<BoardView>(), GetComponent<GameController>()); }

        private static void LoadArt()
        {
            if (disc != null && lockGlyph != null && keyGlyph != null) return;
            disc = Make("lock_disc"); lockGlyph = Make("lock_glyph"); keyGlyph = Make("key_glyph");
        }
        private static Sprite Make(string name)
        {
            var t = Resources.Load<Texture2D>("Brand/Locks/" + name); if (t == null) return null;
            var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), t.width / BadgeSize, 0, SpriteMeshType.FullRect);
            s.name = "Lock badge " + name; s.hideFlags = HideFlags.HideAndDontSave; return s;
        }

        /// <summary>Recreates every badge from the live board (new level, restart, undo).</summary>
        public void Rebuild()
        {
            Clear(); pairOf.Clear(); UnlockCount = 0; RattleCount = 0;
            if (board == null || !board.TryGetBoard(out Board b)) return;
            var locked = new List<Arrow>(); foreach (Arrow a in b.Arrows) if (a.LockedBy != 0) locked.Add(a);
            locked.Sort((x, y) => x.Id.CompareTo(y.Id));
            for (int i = 0; i < locked.Count; i++) pairOf[locked[i].Id] = i;
            Sync();
        }

        // Adds missing badges for the current board state.
        private void Sync()
        {
            if (board == null || !board.TryGetBoard(out Board b)) return;
            foreach (Arrow a in b.Arrows)
            {
                if (a.LockedBy == 0 || !pairOf.TryGetValue(a.Id, out int pair)) continue;
                if (b.GetArrowById(a.LockedBy) == null) continue;   // already unlocked
                if (Find(a.Id, false) == null) Add(a.Id, pair, false);
                if (Find(a.LockedBy, true) == null) Add(a.LockedBy, pair, true);
            }
        }

        private Badge Find(int arrowId, bool key) { foreach (var x in badges) if (x.ArrowId == arrowId && x.IsKey == key && x.Pop < 0) return x; return null; }

        private void Add(int arrowId, int pair, bool key)
        {
            if (!board.TryGetArrowView(arrowId, out ArrowView view) || view == null) return;
            LoadArt();
            var badge = new Badge { ArrowId = arrowId, PairId = pair, IsKey = key, Path = view.Path };
            badge.Root = new GameObject(key ? $"Key badge {arrowId}" : $"Padlock badge {arrowId}");
            Transform parent = view.Path != null ? view.Path.transform : view.transform; badge.Root.transform.SetParent(parent, false);
            badge.Disc = badge.Root.AddComponent<SpriteRenderer>(); badge.Disc.sprite = disc; badge.Disc.color = PairColors[pair % PairColors.Length]; badge.Disc.sortingOrder = SortingOrder;
            var g = new GameObject("Glyph"); g.transform.SetParent(badge.Root.transform, false); g.transform.localScale = Vector3.one * .78f; g.transform.localPosition = new Vector3(0, 0, -.001f);
            badge.Glyph = g.AddComponent<SpriteRenderer>(); badge.Glyph.sprite = key ? keyGlyph : lockGlyph; badge.Glyph.sortingOrder = SortingOrder + 1;
            if (badge.Path != null) { badge.Follow = () => Place(badge); badge.Path.GeometryChanged += badge.Follow; }
            Place(badge); badge.Root.transform.localScale = Vector3.one * ScreenScale(); badges.Add(badge);
        }

        private static void Place(Badge badge)
        {
            if (badge.Root == null) return;
            if (badge.Path != null && badge.Path.Ribbon != null)
            {
                var r = badge.Path.Ribbon; Vector2 p = r.Point(badge.Path.Distance + r.BodyLength * .5f);
                badge.Root.transform.localPosition = new Vector3(p.x, p.y, -.01f);
            }
            badge.Root.transform.rotation = Quaternion.identity;
        }

        private void OnExited(Arrow arrow)
        {
            // The key's own badge rides away with the arrow; every padlock it held pops open.
            if (board == null || !board.TryGetBoard(out Board b)) return;
            int opened = 0; foreach (var x in badges) if (!x.IsKey && x.Pop < 0) { var locked = b.GetArrowById(x.ArrowId); if (locked != null && locked.LockedBy == arrow.Id) { x.Pop = 0; UnlockCount++; opened++; } }
            foreach (var x in badges) if (!x.IsKey && x.ArrowId == arrow.Id && x.Pop < 0) x.Pop = 0;   // never happens (locked arrows cannot leave), kept for safety
            if (opened > 0) PlaySound(true);
        }

        private void OnRestored(Arrow arrow)
        {
            // Undo brought a key back: its padlocks close again.
            for (int i = badges.Count - 1; i >= 0; i--) if (badges[i].ArrowId == arrow.Id) Remove(i);
            Sync();
        }

        private void OnBlocked(Arrow arrow, Arrow blocker)
        {
            if (board == null || !board.TryGetBoard(out Board b) || !b.IsHeldByLock(arrow)) return;
            var badge = Find(arrow.Id, false); if (badge != null) badge.Wobble = 1;
            var key = Find(arrow.LockedBy, true); if (key != null) key.Wobble = 1;
            RattleCount++; PlaySound(false);
        }

        private void PlaySound(bool unlock)
        {
            var boot = GetComponent<GameBootstrap>(); if (boot != null && boot.Settings != null && !boot.Settings.Model.SoundEnabled) return;
            if (!Application.isPlaying) return;
            if (unlockClip == null) { var u = GameSounds.Unlock(SoundDesign.SampleRate); unlockClip = AudioClip.Create("Unlock", u.Length, 1, SoundDesign.SampleRate, false); unlockClip.SetData(u, 0); }
            if (rattleClip == null) { var r = GameSounds.LockRattle(SoundDesign.SampleRate); rattleClip = AudioClip.Create("Padlock rattle", r.Length, 1, SoundDesign.SampleRate, false); rattleClip.SetData(r, 0); }
            if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 0; }
            audioSource.PlayOneShot(unlock ? unlockClip : rattleClip, unlock ? .9f : .8f);
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>World-size multiplier that keeps badges about 5 % of the screen width wide, whatever the board size or zoom.</summary>
        public static float ScreenScale()
        {
            var cam = Camera.main; if (cam == null || !cam.orthographic || cam.orthographicSize <= 0 || Screen.height <= 0) return 1f;
            float pixelsPerUnit = Screen.height / (2f * cam.orthographicSize);
            return Mathf.Clamp(Screen.width * .05f / pixelsPerUnit, BadgeSize, 3f) / BadgeSize;
        }
        /// <summary>Advances wobble and pop animations.</summary>
        public void Tick(float dt)
        {
            float baseScale = ScreenScale();
            for (int i = badges.Count - 1; i >= 0; i--)
            {
                var x = badges[i]; if (x.Root == null) { badges.RemoveAt(i); continue; }
                if (x.Pop >= 0)
                {
                    x.Pop += dt / .42f; float t = Mathf.Clamp01(x.Pop);
                    x.Root.transform.localScale = Vector3.one * baseScale * (1 + .9f * EaseOut(t));
                    x.Root.transform.rotation = Quaternion.Euler(0, 0, 40 * t);
                    var c = x.Disc.color; c.a = 1 - t; x.Disc.color = c; var gc = x.Glyph.color; gc.a = 1 - t; x.Glyph.color = gc;
                    if (t >= 1) Remove(i);
                    continue;
                }
                if (x.Wobble > 0)
                {
                    x.Wobble = Mathf.Max(0, x.Wobble - dt / .45f); float k = x.Wobble;
                    x.Root.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin((1 - k) * 34f) * 22f * k);
                    x.Root.transform.localScale = Vector3.one * baseScale * (1 + .18f * k);
                }
                else x.Root.transform.localScale = Vector3.one * baseScale;
            }
        }
        private static float EaseOut(float t) => 1 - (1 - t) * (1 - t);

        private void Remove(int i)
        {
            var x = badges[i]; if (x.Path != null && x.Follow != null) x.Path.GeometryChanged -= x.Follow;
            if (x.Root != null) { if (Application.isPlaying) Destroy(x.Root); else DestroyImmediate(x.Root); }
            badges.RemoveAt(i);
        }
        private void Clear() { for (int i = badges.Count - 1; i >= 0; i--) Remove(i); }
    }
}
