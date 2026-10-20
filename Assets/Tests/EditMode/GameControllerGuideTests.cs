using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace ArrowEscape.Tests
{
    /// <summary>Exercises guide lifecycle through real tap, undo, pause, and restart paths.</summary>
    public class GameControllerGuideTests
    {
        private GameObject host; private GameController game; private BoardView view; private GuideController guide; private TapController taps;
        /// <summary>Creates an optional guide without making it a gameplay requirement.</summary>
        [SetUp] public void Setup() { host = new GameObject("Guide integration"); game = host.AddComponent<GameController>(); view = host.GetComponent<BoardView>(); taps = host.GetComponent<TapController>(); guide = host.AddComponent<GuideController>(); game.StartLevel(LevelRepository.LoadFromResources("Levels/level_001").Board); }
        /// <summary>Removes all runtime resources.</summary>
        [TearDown] public void Cleanup() { Object.DestroyImmediate(host); SpriteFactory.ClearCache(); }
        private void Tap(Arrow a) { Vector2 p = view.CellToWorld(a.X, a.Y); taps.PointerDown(1, p); taps.PointerUp(1, p); }
        /// <summary>Hints match the pure finder and preserve counters, history, board instances, and order.</summary>
        [Test] public void PureRequest()
        {
            Board board = game.Session.Board; var arrows = new System.Collections.Generic.List<Arrow>(board.Arrows); HintResult expected = HintFinder.Find(board); int events = 0; Arrow shown = null; game.HintShown += a => { events++; shown = a; };
            Assert.That(game.ShowHint(), Is.True); Assert.That(game.HintCount, Is.EqualTo(1)); Assert.That(events, Is.EqualTo(1)); Assert.That(shown, Is.SameAs(expected.Arrow)); Assert.That(guide.HintArrowId, Is.EqualTo(expected.Arrow.Id));
            Assert.That(game.Session.Board, Is.SameAs(board)); Assert.That(game.MoveCount + game.MistakeCount + game.UndoCount, Is.Zero); Assert.That(game.CanUndo, Is.False); Assert.That(game.Session.TryUndo().Outcome, Is.EqualTo(UndoOutcome.NothingToUndo));
            for (int i = 0; i < arrows.Count; i++) Assert.That(board.Arrows[i], Is.SameAs(arrows[i]));
            game.ShowHint(); Assert.That(game.HintCount, Is.EqualTo(2)); int hinted = 0; foreach (var v in host.GetComponentsInChildren<ArrowView>()) if (v.HasHint) hinted++; Assert.That(hinted, Is.EqualTo(1));
        }
        /// <summary>A hint also preserves nonzero mistakes, moves, undos, and pending history.</summary>
        [Test] public void ExistingHistory()
        {
            Tap(game.Session.Board.GetArrowById(1)); Tap(game.Session.Board.GetArrowById(4)); game.Undo(); Tap(game.Session.Board.GetArrowById(4));
            int moves = game.MoveCount, mistakes = game.MistakeCount, undos = game.UndoCount; game.ShowHint(); Assert.That(game.MoveCount, Is.EqualTo(moves)); Assert.That(game.MistakeCount, Is.EqualTo(mistakes)); Assert.That(game.UndoCount, Is.EqualTo(undos)); Assert.That(game.CanUndo, Is.True); game.Undo(); Assert.That(game.Session.Board.GetArrowById(4), Is.Not.Null);
        }
        /// <summary>Hint layers clear before the released view starts sliding.</summary>
        [Test] public void ExitHinted()
        {
            game.ShowHint(); Arrow a = game.Session.Board.GetArrowById(guide.HintArrowId); view.TryGetArrowView(a.Id, out var arrow); guide.Tick(0.32f);
            game.ArrowExited += exited => Assert.That(arrow.HasHint, Is.False);
            Tap(a); Assert.That(guide.HasActiveHint || guide.HasActiveLine, Is.False); Assert.That(arrow.HasHint, Is.False); Assert.That(arrow.transform.localScale, Is.EqualTo(Vector3.one * ViewTheme.ArrowFillRatio)); Assert.That(arrow.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
        }
        /// <summary>Another free exit also invalidates a hint.</summary>
        [Test] public void ExitDifferent()
        { game.ShowHint(); Arrow other = null; foreach (Arrow a in game.Session.Board.Arrows) if (a.Id != guide.HintArrowId && game.Session.Board.CanExit(a)) { other = a; break; } Assert.That(other, Is.Not.Null); Tap(other); Assert.That(guide.HasActiveHint, Is.False); }
        /// <summary>A blocked tap retains its hint and connects to the nearest blocker.</summary>
        [Test] public void BlockedKeepsHint()
        {
            game.ShowHint(); int hintId = guide.HintArrowId; Arrow tapped = game.Session.Board.GetArrowById(1); Arrow blocker = game.Session.Board.GetBlocker(tapped); Tap(tapped);
            Assert.That(guide.HasActiveLine && guide.HasActiveHint, Is.True); Assert.That(guide.HintArrowId, Is.EqualTo(hintId)); GuideGeometry.TryComputeSegment(tapped, blocker, ViewTheme.GuideInset, out var segment);
            Assert.That(guide.LineStartWorld, Is.EqualTo((Vector2)view.CellToWorld(segment.StartX, segment.StartY))); Assert.That(guide.LineEndWorld, Is.EqualTo((Vector2)view.CellToWorld(segment.EndX, segment.EndY))); Assert.That(game.MistakeCount, Is.EqualTo(1));
        }
        /// <summary>Exits, undo, restart, and replacement all clear stale guides.</summary>
        [Test] public void ClearLifecycle()
        {
            game.ShowHint(); Tap(game.Session.Board.GetArrowById(1)); Tap(game.Session.Board.GetArrowById(4)); Assert.That(guide.HasActiveLine || guide.HasActiveHint, Is.False);
            game.ShowHint(); Tap(game.Session.Board.GetArrowById(1)); game.Undo(); Assert.That(guide.HasActiveLine || guide.HasActiveHint, Is.False); Assert.That(game.HintCount, Is.EqualTo(2));
            game.ShowHint(); Tap(game.Session.Board.GetArrowById(1)); game.RestartLevel(); Assert.That(guide.HasActiveLine || guide.HasActiveHint, Is.False); Assert.That(game.HintCount, Is.Zero);
            game.ShowHint(); game.StartLevel(game.Session.Board.Clone()); Assert.That(guide.HasActiveHint, Is.False); Assert.That(game.HintCount, Is.Zero);
        }
        /// <summary>Paused ignored input preserves both guides and the request count.</summary>
        [Test] public void PausedIgnored()
        { game.ShowHint(); Tap(game.Session.Board.GetArrowById(1)); game.SetPaused(true); int count = game.HintCount; Assert.That(game.CanHint, Is.False); Assert.That(game.ShowHint(), Is.False); Tap(game.Session.Board.GetArrowById(4)); game.Undo(); Assert.That(guide.HasActiveHint && guide.HasActiveLine, Is.True); Assert.That(guide.IsPaused, Is.True); Assert.That(game.HintCount, Is.EqualTo(count)); }
        /// <summary>Following hints clears all samples without mistakes or missing suggestions.</summary>
        [TestCase(1)][TestCase(2)][TestCase(3)] public void FollowHints(int index)
        {
            game.StartLevel(LevelRepository.LoadFromResources("Levels/level_00" + index).Board); int arrows = game.StartingArrowCount;
            while (!game.IsCleared) { Assert.That(game.ShowHint(), Is.True); Arrow a = game.Session.Board.GetArrowById(guide.HintArrowId); if (game.Session.Board.Arrows.Count == 1) LogAssert.Expect(LogType.Log, "Level cleared. Mistakes: 0"); Tap(a); }
            Assert.That(game.HintCount, Is.EqualTo(arrows)); Assert.That(game.MistakeCount + game.UndoCount, Is.Zero); Assert.That(game.CanHint, Is.False); Assert.That(game.ShowHint(), Is.False); Assert.That(game.HintCount, Is.EqualTo(arrows));
        }
        /// <summary>Optional visuals never become required for logical hints, taps, undo, or restart.</summary>
        [Test] public void NoGuide()
        {
            Object.DestroyImmediate(guide); game.StartLevel(game.Session.Board.Clone()); int events = 0; game.HintShown += a => events++;
            Assert.DoesNotThrow(() => Assert.That(game.ShowHint(), Is.True)); Assert.That(events, Is.EqualTo(1)); Assert.That(game.HintCount, Is.EqualTo(1)); Tap(game.Session.Board.GetArrowById(4)); Assert.DoesNotThrow(game.Undo); Assert.DoesNotThrow(game.RestartLevel); Assert.That(game.HintCount, Is.Zero);
        }
        /// <summary>An unsolvable board has no suggestion and does not count a request.</summary>
        [Test] public void NoFreeArrow()
        {
            var board = new Board(2, 1); board.TryAddArrow(1, 0, 0, Direction.Right, out _); board.TryAddArrow(2, 1, 0, Direction.Left, out _); game.StartLevel(board);
            Assert.That(game.ShowHint(), Is.False); Assert.That(game.HintCount, Is.Zero); Assert.That(guide.HasActiveHint, Is.False);
        }
        /// <summary>Hint requests never save progress or affect flawless best scoring.</summary>
        [Test] public void NoSavedHintState()
        {
            using (var s = new ProgressTestScene())
            {
                s.Bootstrap.ShowLevel(0); int saves = s.Store.Saves; s.Game.ShowHint(); s.Game.ShowHint(); Assert.That(s.Store.Saves, Is.EqualTo(saves)); s.Clear(); s.Bootstrap.Progress.Tracker.TryGetRecord("level_001", out var record); Assert.That(record.bestMistakes, Is.Zero); Assert.That(record.bestUndos, Is.Zero);
            }
        }
    }
}
