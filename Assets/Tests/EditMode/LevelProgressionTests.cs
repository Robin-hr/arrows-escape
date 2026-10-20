using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArrowEscape.Tests
{
    /// <summary>Exercises completion buttons across every sample level and repeated UI callbacks.</summary>
    public class LevelProgressionTests
    {
        private GameObject host, uiHost, cameraHost;
        private GameBootstrap bootstrap;
        private GameController game;
        private BoardView view;
        private HudController hud;

        /// <summary>Builds the actual gameplay and completion panel without scene edits.</summary>
        [SetUp]
        public void SetUp()
        {
            cameraHost = new GameObject("Progression camera", typeof(Camera));
            cameraHost.tag = "MainCamera";
            host = new GameObject("Progression gameplay");
            bootstrap = host.AddComponent<GameBootstrap>();
            game = host.GetComponent<GameController>();
            view = host.GetComponent<BoardView>();
            uiHost = new GameObject("Progression UI");
            hud = uiHost.AddComponent<HudController>();
            hud.Bind(game, bootstrap);
        }

        /// <summary>Removes runtime objects and generated sprites.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(uiHost);
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(cameraHost);
            SpriteFactory.ClearCache();
        }

        /// <summary>Completion proceeds 1, 2, 3; the debug jump still wraps to 1.</summary>
        [Test]
        public void CompleteAllThreeInOrder()
        {
            bootstrap.ShowLevel(0);
            for (int index = 0; index < 3; index++)
            {
                AssertLevel(index);
                ClearCurrentLevel();
                if (index == 2) bootstrap.DebugJump(1);
                else hud.CompletePanel.NextButton.onClick.Invoke();
                AssertLevel((index + 1) % 3);
                Assert.That(hud.CompletePanel.IsVisible, Is.False);
                Assert.That(game.MoveCount, Is.Zero);
                Assert.That(game.MistakeCount, Is.Zero);
                Assert.That(game.UndoCount, Is.Zero);
            }
        }

        /// <summary>A repeated completion callback must never skip the newly started level.</summary>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RepeatedNextCallbackAdvancesOnlyOnce(int index)
        {
            bootstrap.ShowLevel(index);
            ClearCurrentLevel();
            if (index == 2) bootstrap.DebugJump(1);
            else hud.CompletePanel.NextButton.onClick.Invoke();
            Board started = game.Session.Board;
            hud.CompletePanel.NextButton.onClick.Invoke();
            AssertLevel((index + 1) % 3);
            Assert.That(game.Session.Board, Is.SameAs(started));
        }

        /// <summary>Repeated replay callbacks cannot reset an attempt a second time.</summary>
        [Test]
        public void RepeatedReplayCallbackStartsOnlyOnce()
        {
            bootstrap.EnsureInitialized();
            bootstrap.Progress.Tracker.RecordCompletion("level_001", 0, 0);
            bootstrap.ShowLevel(1);
            ClearCurrentLevel();
            hud.CompletePanel.ReplayButton.onClick.Invoke();
            Board started = game.Session.Board;
            hud.CompletePanel.ReplayButton.onClick.Invoke();
            AssertLevel(1);
            Assert.That(game.Session.Board, Is.SameAs(started));
        }

        private void AssertLevel(int index)
        {
            int count = index == 0 ? 8 : index == 1 ? 13 : 19;
            Assert.That(game.LevelName, Is.EqualTo("Level " + (index + 1)));
            Assert.That(hud.Hud.LevelText.text, Is.EqualTo(game.LevelName));
            Assert.That(game.StartingArrowCount, Is.EqualTo(count));
            Assert.That(game.Session.Board.Arrows.Count, Is.EqualTo(count));
        }

        private void ClearCurrentLevel()
        {
            Board board = game.Session.Board;
            var taps = host.GetComponent<TapController>();
            while (!game.IsCleared)
            {
                Arrow free = null;
                foreach (Arrow arrow in board.Arrows)
                    if (board.CanExit(arrow)) { free = arrow; break; }
                Assert.That(free, Is.Not.Null);
                if (board.Arrows.Count == 1)
                    LogAssert.Expect(LogType.Log, "Level cleared. Mistakes: 0");
                Vector2 position = view.CellToWorld(free.X, free.Y);
                taps.PointerDown(1, position);
                taps.PointerUp(1, position);
            }
            Assert.That(hud.CompletePanel.IsVisible, Is.True);
        }
    }
}
