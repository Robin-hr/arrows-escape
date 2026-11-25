using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>
    /// Step 17 TOOLS > FREE: briefly marks every arrow that can leave right now (padlocks and blockers respected).
    /// Purely visual: it uses the arrow's hint layer and clears on the next move, undo, new level or after a few seconds.
    /// </summary>
    public sealed class FreeArrowReveal : MonoBehaviour
    {
        /// <summary>How long the marks stay.</summary>
        public const float Seconds = 2.6f;
        /// <summary>Mark colour (fresh green).</summary>
        public static readonly Color Tint = new Color32(46, 196, 120, 255);
        private readonly List<ArrowView> marked = new List<ArrowView>();
        private float remaining;
        private GameController game; private BoardView board;
        /// <summary>Arrows currently marked.</summary>
        public int MarkedCount => marked.Count;

        private bool hooked;
        // Edit Mode never calls Awake, so the events are hooked on first use.
        private void Hook() { if (hooked) return; game = GetComponent<GameController>(); board = GetComponent<BoardView>(); if (game != null) { game.MoveCountChanged += OnChange; game.LevelStarted += Clear; hooked = true; } }
        private void Awake() => Hook();
        private void OnDestroy() { if (game != null && hooked) { game.MoveCountChanged -= OnChange; game.LevelStarted -= Clear; } }
        private void OnChange() => Clear();

        /// <summary>Marks every free arrow; returns how many were marked.</summary>
        public int Reveal()
        {
            Clear(); Hook();
            if (game == null || board == null || game.Session == null) return 0;
            Board b = game.Session.Board;
            foreach (Arrow a in b.Arrows)
                if (b.CanExit(a) && board.TryGetArrowView(a.Id, out ArrowView view) && view != null) { view.SetHint(Tint, 1.06f); marked.Add(view); }
            remaining = Seconds; return marked.Count;
        }

        /// <summary>Removes the marks.</summary>
        public void Clear() { foreach (var v in marked) if (v != null) v.ClearHint(); marked.Clear(); remaining = 0; }

        /// <summary>Explicit timer for tests.</summary>
        public void Tick(float dt) { if (remaining <= 0) return; remaining -= dt; if (remaining <= 0) Clear(); }
        private void Update() { if (game == null || !game.IsPaused) Tick(Time.deltaTime); }
    }
}
