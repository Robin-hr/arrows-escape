using System.Collections.Generic;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 15: padlock/key arrows, dense generator, difficulty rhythm, HUD badge, progress bar and lock coach.</summary>
    public sealed class Step15Tests
    {
        // 6 x 3 board: A (id 1) is free on the bottom row but padlocked by K (id 2); K waits for B (id 3); B is free.
        private static Board LockBoard(bool cycle = false)
        {
            var board = new Board(6, 3);
            board.TryAddArrow(new Arrow(1, new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0) }, Direction.Right, ArrowKind.Normal, 2), out _);
            board.TryAddArrow(new Arrow(2, new[] { new Cell(1, 2), new Cell(2, 2) }, Direction.Right, ArrowKind.Normal, cycle ? 1 : 0), out _);
            board.TryAddArrow(new Arrow(3, new[] { new Cell(4, 1), new Cell(4, 2) }, Direction.Up), out _);
            return board;
        }

        [Test] public void PadlockWaitsForItsKey()
        {
            var board = LockBoard(); Arrow a = board.GetArrowById(1), k = board.GetArrowById(2), b = board.GetArrowById(3);
            Assert.That(board.GetBlockingCell(a), Is.Null, "the path itself is clear");
            Assert.That(board.CanExit(a), Is.False); Assert.That(board.GetBlocker(a), Is.SameAs(k)); Assert.That(board.IsHeldByLock(a), Is.True);
            Assert.That(board.GetLockedBy(k).Select(x => x.Id), Is.EqualTo(new[] { 1 }));
            var session = new GameSession(board);
            var held = session.TryMove(a); Assert.That(held.Outcome, Is.EqualTo(MoveOutcome.Blocked)); Assert.That(held.HeldByLock, Is.True); Assert.That(held.Blocker, Is.SameAs(k)); Assert.That(held.MistakeCount, Is.EqualTo(1));
            var physical = session.TryMove(k); Assert.That(physical.HeldByLock, Is.False); Assert.That(physical.Blocker, Is.SameAs(b));
            Assert.That(session.TryMove(b).Outcome, Is.EqualTo(MoveOutcome.Exited));
            Assert.That(session.TryMove(k).Outcome, Is.EqualTo(MoveOutcome.Exited));
            Assert.That(board.CanExit(a), Is.True, "the key has left");
            Assert.That(session.TryUndo().Outcome, Is.EqualTo(UndoOutcome.Undone)); Assert.That(board.CanExit(board.GetArrowById(1)), Is.False, "undo puts the key back");
            Assert.That(board.GetArrowById(2).LockedBy, Is.Zero);
            Assert.That(board.Clone().GetArrowById(1).LockedBy, Is.EqualTo(2), "clones keep padlocks");
        }

        [Test] public void SolversHintsAndAnalysisRespectPadlocks()
        {
            var board = LockBoard();
            Assert.That(LevelSolvability.IsSolvable(board), Is.True); Assert.That(LevelSolvability.CountFreeArrows(board), Is.EqualTo(1));
            var solved = PuzzleSolver.Analyze(board); Assert.That(solved.Status, Is.EqualTo(SolveStatus.Solved)); Assert.That(solved.Path.Select(m => m.ArrowId), Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(HintFinder.Find(board).Arrow.Id, Is.EqualTo(3), "the hint never points at a padlocked arrow");
            var analysis = LevelAnalyzer.Analyze(board, new AnalysisOptions(64, 16)); Assert.That(analysis.Status, Is.EqualTo(SolveStatus.Solved)); Assert.That(analysis.FreeExitsAtStart, Is.EqualTo(1));
            var cycle = LockBoard(true);
            Assert.That(LevelSolvability.IsSolvable(cycle), Is.False, "A waits for K, K waits for A"); Assert.That(PuzzleSolver.Analyze(cycle).Status, Is.EqualTo(SolveStatus.Unsolvable));
            Assert.Throws<System.ArgumentException>(() => new Arrow(5, new[] { new Cell(0, 0) }, Direction.Up, ArrowKind.Normal, 5));
            Assert.Throws<System.ArgumentException>(() => new Arrow(5, new[] { new Cell(0, 0) }, Direction.Up, ArrowKind.Ice, 2));
        }

        [Test] public void LevelFilesCarryPadlocks()
        {
            var data = LevelExport.ToLevelData(LockBoard(), "Lock test");
            Assert.That(data.arrows.First(x => x.id == 1).lockedBy, Is.EqualTo(2));
            var json = JsonUtility.ToJson(data); var back = LevelBuilder.Build(JsonUtility.FromJson<LevelData>(json));
            Assert.That(back.Success, Is.True); Assert.That(back.Board.GetArrowById(1).LockedBy, Is.EqualTo(2));
            data.arrows.First(x => x.id == 1).lockedBy = 99; var bad = LevelBuilder.Build(data);
            Assert.That(bad.Success, Is.False); Assert.That(string.Join(";", bad.Errors), Does.Contain("lockedBy"));
            Assert.That(JsonUtility.FromJson<ArrowData>("{\"id\":4,\"x\":0,\"y\":0,\"direction\":\"Up\"}").lockedBy, Is.Zero, "old level files have no padlocks");
        }

        [Test] public void DenseGeneratorPacksSolvableBoards()
        {
            var mask = ShapeMask.Build(ShapeKind.Square, 26, 26); var settings = new DenseLevelSettings(3, 18);
            Board a = DenseLevelGenerator.Generate(mask, settings, 77), b = DenseLevelGenerator.Generate(mask, settings, 77);
            Assert.That(PathTestBoards.Signature(a), Is.EqualTo(PathTestBoards.Signature(b)), "deterministic");
            Assert.That(LevelSolvability.IsSolvable(a), Is.True);
            Assert.That(DenseLevelGenerator.CoveragePermille(a, mask), Is.GreaterThanOrEqualTo(900));
            int free = LevelSolvability.CountFreeArrows(a); Assert.That(free, Is.GreaterThanOrEqualTo(2)); Assert.That(free * 4, Is.LessThan(a.Arrows.Count));
            Assert.That(a.Arrows.Max(x => x.Length), Is.GreaterThan(8));
            var locked = LockPlanner.AddLocks(a, 4, 9);
            Assert.That(LockPlanner.CountLocks(locked), Is.EqualTo(4)); Assert.That(LevelSolvability.IsSolvable(locked), Is.True);
            Assert.That(PathTestBoards.Signature(LockPlanner.AddLocks(a, 4, 9)) + LockPlanner.CountLocks(locked), Is.EqualTo(PathTestBoards.Signature(locked) + 4));
            foreach (Arrow x in locked.Arrows) if (x.LockedBy != 0) Assert.That(locked.GetArrowById(x.LockedBy).LockedBy, Is.Zero, "a key is never padlocked itself");
        }

        [Test] public void DifficultyRhythmAndDailyPadlocks()
        {
            Assert.That(LevelBadge.ForCatalog(70), Is.EqualTo(LevelBadgeKind.SuperHard)); Assert.That(LevelBadge.ForCatalog(65), Is.EqualTo(LevelBadgeKind.Hard)); Assert.That(LevelBadge.ForCatalog(71), Is.EqualTo(LevelBadgeKind.Normal));
            Assert.That(LevelBadge.ForJourney(10), Is.EqualTo(LevelBadgeKind.SuperHard)); Assert.That(LevelBadge.ForJourney(15), Is.EqualTo(LevelBadgeKind.Hard)); Assert.That(LevelBadge.Label(LevelBadgeKind.SuperHard), Is.EqualTo("SUPER HARD"));
            var daily = DailyPuzzleBuilder.Build(20261125);
            Assert.That(LockPlanner.CountLocks(daily), Is.EqualTo(DailyPuzzleBuilder.Locks)); Assert.That(daily.Arrows.Count, Is.GreaterThan(120)); Assert.That(LevelSolvability.IsSolvable(daily), Is.True);
            Assert.That(GameSounds.Unlock(SoundDesign.SampleRate).Max(System.Math.Abs), Is.InRange(0.5f, 0.75f));
            Assert.That(GameSounds.LockRattle(SoundDesign.SampleRate).Max(System.Math.Abs), Is.InRange(0.4f, 0.65f));
        }

        [Test] public void BadgesRattleAndPopOpen()
        {
            HudController.OverrideLockTip(false);
            try
            {
                using (var scene = new EconomyTestScene(withUi: true))
                {
                    scene.Flow.MainMenu.PlayButton.onClick.Invoke();
                    scene.Game.StartLevel(LockBoard(), "Lock test");
                    var badges = scene.Bootstrap.GetComponent<LockBadges>(); Assert.That(badges, Is.Not.Null);
                    Assert.That(badges.Count, Is.EqualTo(2)); Assert.That(badges.LockCount, Is.EqualTo(1));
                    Assert.That(scene.Hud.Hud.LockTip.gameObject.activeSelf, Is.True, "first padlock level shows the coach");
                    scene.Tap(1, 0);   // the padlocked arrow
                    Assert.That(badges.RattleCount, Is.EqualTo(1)); Assert.That(scene.Game.MistakeCount, Is.EqualTo(1));
                    scene.Tap(4, 1); Assert.That(scene.Hud.Hud.Progress, Is.EqualTo(1f / 3).Within(0.001f));
                    scene.Tap(1, 2);   // the key leaves
                    Assert.That(badges.UnlockCount, Is.EqualTo(1)); badges.Tick(1f); Assert.That(badges.LockCount, Is.Zero);
                    scene.Game.Undo(); Assert.That(badges.LockCount, Is.EqualTo(1), "undoing the key closes the padlock again");
                }
            }
            finally { HudController.OverrideLockTip(null); }
        }

        [Test] public void HudShowsDifficultyAndZoomOnBigBoards()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                scene.Flow.MainMenu.PlayButton.onClick.Invoke();
                Assert.That(scene.Hud.Hud.DifficultyPill.gameObject.activeSelf, Is.False, "Normal levels show no label");
                Assert.That(scene.Bootstrap.TryStartJourneyLevelIgnoringLock(9), Is.True);   // Journey 10
                Assert.That(scene.Hud.CurrentBadge(), Is.EqualTo(LevelBadgeKind.SuperHard));
                scene.Hud.SetVisible(true);
                Assert.That(scene.Hud.Hud.DifficultyPill.gameObject.activeSelf, Is.True); Assert.That(scene.Hud.Hud.DifficultyText.text, Is.EqualTo("SUPER HARD"));
                Assert.That(scene.Game.Session.Board.Arrows.Count, Is.GreaterThan(120));
                Assert.That(scene.Bootstrap.GetComponent<BoardZoom>().Forced, Is.True);
            }
        }

        [Test] public void SettingsLinkThePrivacyPolicyAndBonusesAreHonest()
        {
            using (var scene = new EconomyTestScene(withUi: true))
            {
                var settings = scene.Flow.SettingsScreen; Assert.That(settings.PrivacyButton, Is.Not.Null);
                StringAssert.Contains("Privacy Policy", settings.PrivacyButton.GetComponentInChildren<UnityEngine.UI.Text>().text);
                settings.PrivacyButton.onClick.Invoke(); Assert.That(settings.LastOpenedUrl, Is.EqualTo(AppInfo.PrivacyPolicyUrl));
                Assert.That(AppInfo.PrivacyPolicyUrl, Does.StartWith("https://"));
                foreach (var text in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Assert.That(text.text.ToUpperInvariant(), Does.Not.Contain("WATCH AD").And.Not.Contain("REWARDED AD"), "no ad wording until a real ad network ships");
            }
        }
    }
}
