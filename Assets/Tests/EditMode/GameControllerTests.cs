using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArrowEscape.Tests
{
    /// <summary>Exercises the complete tap/session/animation flow with explicit ticks.</summary>
    public class GameControllerTests
    {
        private GameObject host;
        private GameController game;
        private BoardView view;
        private TapController taps;
        private ArrowAnimator animator;
        private Board board;
        private int exited, blocked, cleared, mistakeEvents;
        private Arrow lastArrow, lastBlocker;

        /// <summary>Creates a three-arrow chain and listens only after its initial reset.</summary>
        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Game controller test");
            game = host.AddComponent<GameController>();
            view = host.GetComponent<BoardView>();
            taps = host.GetComponent<TapController>();
            animator = host.GetComponent<ArrowAnimator>();
            board = new Board(3, 1);
            for (int i = 0; i < 3; i++) board.TryAddArrow(i + 1, i, 0, Direction.Right, out _);
            game.StartLevel(board);
            exited = blocked = cleared = mistakeEvents = 0;
            lastArrow = lastBlocker = null;
            game.ArrowExited += arrow => { exited++; lastArrow = arrow; };
            game.MoveBlocked += (arrow, blocker) => { blocked++; lastArrow = arrow; lastBlocker = blocker; };
            game.MistakeCountChanged += value => mistakeEvents++;
            game.BoardCleared += () => cleared++;
        }

        /// <summary>Stops animations and removes all runtime objects.</summary>
        [TearDown]
        public void TearDown()
        {
            animator.StopAll();
            Object.DestroyImmediate(host);
            SpriteFactory.ClearCache();
        }

        private void Tap(int x, int y)
        {
            Vector2 position = view.CellToWorld(x, y);
            taps.PointerDown(1, position);
            taps.PointerUp(1, position);
        }

        /// <summary>A free tap removes logic and lookup immediately, then destroys its released child.</summary>
        [Test]
        public void FreeTapExitsBeforeSlideCompletes()
        {
            view.TryGetArrowView(3, out ArrowView sliding);
            Arrow arrow = board.GetArrowById(3);
            Tap(2, 0);
            Assert.That(board.GetArrowById(3), Is.Null);
            Assert.That(view.TryGetArrowView(3, out _), Is.False);
            Assert.That(exited, Is.EqualTo(1));
            Assert.That(lastArrow, Is.SameAs(arrow));
            Assert.That(sliding != null, Is.True);
            Assert.That(sliding.transform.parent, Is.EqualTo(view.transform.GetChild(0)));
            animator.Tick(ViewTheme.SlideDuration);
            Assert.That(sliding == null, Is.True);
            Assert.That(host.GetComponentsInChildren<ArrowView>().Length, Is.EqualTo(2));
        }

        /// <summary>A blocked tap changes only mistakes and schedules feedback for the nearest blocker.</summary>
        [Test]
        public void BlockedTapCountsAndRestores()
        {
            view.TryGetArrowView(1, out ArrowView arrow);
            Vector3 position = arrow.transform.position;
            Tap(0, 0);
            Assert.That(board.Arrows.Count, Is.EqualTo(3));
            Assert.That(game.MistakeCount, Is.EqualTo(1));
            Assert.That(blocked, Is.EqualTo(1));
            Assert.That(mistakeEvents, Is.EqualTo(1));
            Assert.That(lastArrow.Id, Is.EqualTo(1));
            Assert.That(lastBlocker.Id, Is.EqualTo(2));
            animator.Tick(1);
            Assert.That(arrow.transform.position, Is.EqualTo(position));
            Assert.That(arrow.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
        }

        /// <summary>A sliding arrow cannot be selected or exited twice.</summary>
        [Test]
        public void RapidDoubleTapExitsOnce()
        {
            Tap(2, 0);
            Tap(2, 0);
            Assert.That(exited, Is.EqualTo(1));
            Assert.That(board.Arrows.Count, Is.EqualTo(2));
            Assert.That(game.MistakeCount, Is.Zero);
        }

        /// <summary>Logical clearing fires once, without waiting for any slide tick.</summary>
        [Test]
        public void WholeLevelClearsExactlyOnce()
        {
            Tap(2, 0);
            Tap(1, 0);
            LogAssert.Expect(LogType.Log, "Level cleared. Mistakes: 0");
            Tap(0, 0);
            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(game.IsCleared, Is.True);
            Assert.That(animator.HasActiveAnimations, Is.True);
            Tap(0, 0);
            Tap(1, 0);
            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(exited, Is.EqualTo(3));
            animator.Tick(1);
            Assert.That(host.GetComponentsInChildren<ArrowView>(), Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Old sliding views, feedback, and mistakes disappear when the next board starts.</summary>
        [Test]
        public void StartLevelResetsEverything()
        {
            Tap(0, 0);
            Tap(2, 0);
            ArrowView[] old = host.GetComponentsInChildren<ArrowView>();
            var next = new Board(4, 4);
            next.TryAddArrow(9, 1, 2, Direction.Up, out _);
            game.StartLevel(next);
            Assert.That(game.MistakeCount, Is.Zero);
            Assert.That(animator.HasActiveAnimations, Is.False);
            foreach (ArrowView previous in old) Assert.That(previous == null, Is.True);
            Assert.That(host.GetComponentsInChildren<ArrowView>().Length, Is.EqualTo(1));
            Assert.That(view.TryGetArrowView(9, out _), Is.True);
            Assert.That(view.transform.childCount, Is.EqualTo(1));
        }

        /// <summary>The session, view, and tap resolver observe the same board instance.</summary>
        [Test]
        public void SharedBoardUnblocksImmediately()
        {
            Assert.That(game.Session.Board, Is.SameAs(board));
            Assert.That(view.TryGetBoard(out Board displayed), Is.True);
            Assert.That(displayed, Is.SameAs(board));
            Tap(2, 0);
            Assert.That(board.CanExit(board.GetArrowById(2)), Is.True);
            Tap(1, 0);
            Assert.That(exited, Is.EqualTo(2));
            Assert.That(game.MistakeCount, Is.Zero);
        }

        /// <summary>Level 1 plays through real tap conversion while all its slides may overlap.</summary>
        [Test]
        public void JsonLevelOneClearsWithoutMistakes()
        {
            LevelLoadResult loaded = LevelRepository.LoadFromResources("Levels/level_001");
            Assert.That(loaded.Success, Is.True);
            game.StartLevel(loaded.Board);
            while (!game.IsCleared)
            {
                Arrow free = null;
                foreach (Arrow arrow in game.Session.Board.Arrows)
                    if (game.Session.Board.CanExit(arrow)) { free = arrow; break; }
                Assert.That(free, Is.Not.Null);
                if (game.Session.Board.Arrows.Count == 1)
                    LogAssert.Expect(LogType.Log, "Level cleared. Mistakes: 0");
                Tap(free.X, free.Y);
            }
            Assert.That(game.MistakeCount, Is.Zero);
            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(exited, Is.EqualTo(8));
            animator.Tick(1);
            Assert.That(host.GetComponentsInChildren<ArrowView>(), Is.Empty);
        }

        /// <summary>Clearing via BoardView also removes released sliding children safely.</summary>
        [Test]
        public void ClearRemovesReleasedViews()
        {
            view.TryGetArrowView(3, out ArrowView sliding);
            Tap(2, 0);
            view.Clear();
            Assert.That(sliding == null, Is.True);
            Assert.That(view.transform.childCount, Is.Zero);
            Assert.DoesNotThrow(() => animator.Tick(1));
            Assert.That(animator.HasActiveAnimations, Is.False);
        }

        /// <summary>The restart menu reloads JSON, resets mistakes, and stops old slides.</summary>
        [Test]
        public void RestartReloadsOriginalJson()
        {
            game.SetLevelInfo("Level 1", "Levels/level_001");
            game.StartLevel(LevelRepository.LoadFromResources("Levels/level_001").Board);
            Board original = game.Session.Board;
            Tap(0, 0);
            Tap(3, 2);
            game.RestartLevel();
            Assert.That(game.Session.Board, Is.Not.SameAs(original));
            Assert.That(game.Session.Board.Arrows.Count, Is.EqualTo(8));
            Assert.That(game.MistakeCount, Is.Zero);
            Assert.That(animator.HasActiveAnimations, Is.False);
        }

        /// <summary>Repeated blocked taps remain playable and restore feedback without position drift.</summary>
        [Test]
        public void RepeatedBlockedTapsNeverLoseLevel()
        {
            for (int i = 0; i < 10; i++) { Tap(0, 0); animator.Tick(0.05f); }
            Assert.That(game.MistakeCount, Is.EqualTo(10));
            Assert.That(game.IsCleared, Is.False);
            Assert.That(board.Arrows.Count, Is.EqualTo(3));
            animator.Tick(1);
            view.TryGetArrowView(1, out ArrowView arrow);
            Assert.That(arrow.transform.position, Is.EqualTo(view.CellToWorld(0, 0)));
        }

        /// <summary>Board replacement is announced once, after new views and the live board are ready.</summary>
        [Test]
        public void BoardChangedFiresAfterReplacementIsReady()
        {
            var next = new Board(2, 2);
            next.TryAddArrow(7, 1, 1, Direction.Up, out _);
            int changed = 0;
            view.BoardChanged += () =>
            {
                changed++;
                Assert.That(view.TryGetBoard(out Board shown), Is.True);
                Assert.That(shown, Is.SameAs(next));
                Assert.That(view.TryGetArrowView(7, out _), Is.True);
            };
            game.StartLevel(next);
            Assert.That(changed, Is.EqualTo(1));
        }
    }
}
