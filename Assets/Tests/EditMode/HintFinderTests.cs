using System.Collections.Generic;
using System.Reflection;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Checks deterministic hint ranking against an independent remove-and-recheck oracle.</summary>
    public class HintFinderTests
    {
        /// <summary>Missing, empty, and already-cleared boards have no hint.</summary>
        [TestCase(0)][TestCase(1)][TestCase(2)] public void NoBoard(int kind)
        {
            Board board = kind == 0 ? null : new Board(2, 2);
            if (kind == 2) { board.TryAddArrow(1, 0, 0, Direction.Up, out Arrow a); board.Remove(a); }
            Assert.That(HintFinder.Find(board), Is.SameAs(HintResult.None)); Assert.That(HintResult.None.Arrow, Is.Null);
        }
        /// <summary>An unsolvable opposing pair has no candidate.</summary>
        [Test] public void Facing() { var b = new Board(2, 1); b.TryAddArrow(1, 0, 0, Direction.Right, out _); b.TryAddArrow(2, 1, 0, Direction.Left, out _); Assert.That(HintFinder.Find(b).HasHint, Is.False); }
        /// <summary>An independent free arrow has zero dependency scores.</summary>
        [Test] public void Single() { var b = new Board(2, 2); b.TryAddArrow(7, 1, 1, Direction.Up, out Arrow a); var h = HintFinder.Find(b); Assert.That(h.Arrow, Is.SameAs(a)); Assert.That(h.NewlyFreed, Is.Zero); Assert.That(h.Dependents, Is.Zero); }
        /// <summary>The final arrow in a three-arrow row frees one and is depended on by two.</summary>
        [Test] public void Chain() { var b = new Board(3, 1); for (int x = 0; x < 3; x++) b.TryAddArrow(x + 1, x, 0, Direction.Right, out _); var h = HintFinder.Find(b); Assert.That(h.Arrow.Id, Is.EqualTo(3)); Assert.That(h.NewlyFreed, Is.EqualTo(1)); Assert.That(h.Dependents, Is.EqualTo(2)); }
        /// <summary>More immediately freed arrows beats a lower id.</summary>
        [Test] public void NewlyFreedWins() { var h = HintFinder.Find(HandBoard(2)); Assert.That(h.Arrow.Id, Is.EqualTo(10)); Assert.That(h.NewlyFreed, Is.EqualTo(2)); }
        /// <summary>Equal immediate benefit prefers more total dependents.</summary>
        [Test] public void DependentsWin() { var h = HintFinder.Find(HandBoard(3)); Assert.That(h.Arrow.Id, Is.EqualTo(10)); Assert.That(h.NewlyFreed, Is.EqualTo(1)); Assert.That(h.Dependents, Is.EqualTo(2)); }
        /// <summary>Equal scores prefer the lower id regardless of insertion order.</summary>
        [Test] public void LowestId() { var h = HintFinder.Find(HandBoard(4)); Assert.That(h.Arrow.Id, Is.EqualTo(-5)); }
        /// <summary>Hints are stable across repeated requests and deep clones, without changing order or instances.</summary>
        [Test] public void DeterministicAndUnchanged()
        {
            Board b = HandBoard(2); var original = new List<Arrow>(b.Arrows); var expected = HintFinder.Find(b);
            for (int i = 0; i < 10; i++) { var h = HintFinder.Find(b); Assert.That(h.Arrow, Is.SameAs(expected.Arrow)); Assert.That(b.CanExit(h.Arrow), Is.True); }
            var cloned = HintFinder.Find(b.Clone()); Assert.That(cloned.Arrow.Id, Is.EqualTo(expected.Arrow.Id)); Assert.That(cloned.NewlyFreed, Is.EqualTo(expected.NewlyFreed)); Assert.That(cloned.Dependents, Is.EqualTo(expected.Dependents));
            Assert.That(b.Arrows.Count, Is.EqualTo(original.Count)); for (int i = 0; i < original.Count; i++) Assert.That(b.Arrows[i], Is.SameAs(original[i]));
        }
        /// <summary>Every score, including non-candidates, agrees with the independent clone/remove oracle.</summary>
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        public void BruteForceScores(int index)
        {
            Board b = index < 3 ? LevelRepository.LoadFromResources("TestLevels/legacy_level_00" + (index + 1)).Board : HandBoard(index - 3);
            // Inspect all private scores rather than widening the production public API just for tests.
            object[] arguments = { b, null, null, null };
            typeof(HintFinder).GetMethod("ScorePaths", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
            int[] freed = (int[])arguments[1], dependents = (int[])arguments[2]; bool[] free = (bool[])arguments[3];
            for (int i = 0; i < b.Arrows.Count; i++)
            {
                Arrow candidate = b.Arrows[i]; Board after = b.Clone(); after.Remove(after.GetArrowById(candidate.Id)); int count = 0, dependencyCount = 0;
                foreach (Arrow a in b.Arrows)
                {
                    if (a.Id == candidate.Id) continue;
                    if (!b.CanExit(a) && after.CanExit(after.GetArrowById(a.Id))) count++;
                    if ((a.Direction == Direction.Up && a.X == candidate.X && a.Y < candidate.Y) ||
                        (a.Direction == Direction.Down && a.X == candidate.X && a.Y > candidate.Y) ||
                        (a.Direction == Direction.Right && a.Y == candidate.Y && a.X < candidate.X) ||
                        (a.Direction == Direction.Left && a.Y == candidate.Y && a.X > candidate.X)) dependencyCount++;
                }
                Assert.That(freed[i], Is.EqualTo(count), "NewlyFreed for " + candidate.Id); Assert.That(dependents[i], Is.EqualTo(dependencyCount)); Assert.That(free[i], Is.EqualTo(b.CanExit(candidate)));
            }
        }
        /// <summary>Following hints clears every sample without a missing or blocked suggestion.</summary>
        [TestCase(1)][TestCase(2)][TestCase(3)] public void Samples(int index)
        {
            Board b = LevelRepository.LoadFromResources("TestLevels/legacy_level_00" + index).Board; int count = b.Arrows.Count;
            while (!b.IsCleared()) { HintResult h = HintFinder.Find(b); Assert.That(h.HasHint, Is.True); Assert.That(b.CanExit(h.Arrow), Is.True); b.Remove(h.Arrow); count--; }
            Assert.That(count, Is.Zero);
        }
        /// <summary>A dense thirty-by-thirty board is handled without cloning or timing assumptions.</summary>
        [Test] public void Dense() { var b = new Board(30, 30); for (int y = 0; y < 30; y++) for (int x = 0; x < 30; x++) b.TryAddArrow(y * 30 + x, x, y, Direction.Right, out _); HintResult h = null; Assert.DoesNotThrow(() => h = HintFinder.Find(b)); Assert.That(h.Arrow.X, Is.EqualTo(29)); Assert.That(h.Arrow.Id, Is.EqualTo(29)); Assert.That(h.Dependents, Is.EqualTo(29)); }
        private static Board HandBoard(int index)
        {
            var b = new Board(5, 5);
            if (index == 0) { for (int x = 0; x < 3; x++) b.TryAddArrow(x + 1, x, 0, Direction.Right, out _); }
            if (index == 1) { for (int y = 0; y < 3; y++) b.TryAddArrow(y + 1, 0, y, Direction.Up, out _); }
            if (index == 2) { b.TryAddArrow(10, 2, 2, Direction.Right, out _); b.TryAddArrow(11, 0, 2, Direction.Right, out _); b.TryAddArrow(12, 2, 0, Direction.Up, out _); b.TryAddArrow(1, 4, 4, Direction.Up, out _); b.TryAddArrow(13, 4, 3, Direction.Up, out _); }
            if (index == 3) { b.TryAddArrow(10, 3, 0, Direction.Up, out _); b.TryAddArrow(11, 2, 0, Direction.Right, out _); b.TryAddArrow(12, 0, 0, Direction.Right, out _); b.TryAddArrow(1, 4, 2, Direction.Up, out _); b.TryAddArrow(13, 4, 1, Direction.Up, out _); }
            if (index == 4) { b.TryAddArrow(9, 0, 0, Direction.Down, out _); b.TryAddArrow(-5, 4, 4, Direction.Up, out _); b.TryAddArrow(2, 2, 0, Direction.Down, out _); }
            return b;
        }
    }
}
