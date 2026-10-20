using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using NUnit.Framework;

namespace ArrowEscape.Tests
{
    /// <summary>Checks the actual bundled Resources files, including difficulty constraints.</summary>
    public class SampleLevelsTests
    {
        /// <summary>Every discovered level loads, solves, and starts with blocked arrows.</summary>
        [Test]
        public void EveryDiscoveredLevelLoadsAndSolves()
        {
            var paths = LevelRepository.GetLevelResourcePaths();
            Assert.That(paths.Count, Is.GreaterThan(0));
            foreach (string path in paths)
            {
                LevelLoadResult result = LevelRepository.LoadFromResources(path);
                Assert.That(result.Success, Is.True, path + ": " + string.Join("; ", result.Errors));
                Assert.That(LevelSolvability.IsSolvable(result.Board), Is.True, path);
                Assert.That(LevelSolvability.CountFreeArrows(result.Board),
                    Is.LessThan(result.Board.Arrows.Count), path);
            }
        }

        /// <summary>The expected three files are discovered in name order.</summary>
        [Test]
        public void ExactlyThreeLevelsAreFoundInOrder()
        {
            OriginalCatalogAssertions.Check();
        }

        /// <summary>Sample sizes and genuine three-arrow chains match the design.</summary>
        [TestCase("Levels/level_001", 4, 6, 8)]
        [TestCase("Levels/level_002", 5, 10, 14)]
        [TestCase("Levels/level_003", 6, 16, 22)]
        public void SampleHasRequiredSizeAndChain(string path, int size, int min, int max)
        {
            LevelLoadResult result = LevelRepository.LoadFromResources(path);
            Assert.That(result.Success, Is.True, string.Join("; ", result.Errors));
            Board board = result.Board;
            Assert.That(board.Width, Is.EqualTo(size));
            Assert.That(board.Height, Is.EqualTo(size));
            Assert.That(board.Arrows.Count, Is.InRange(min, max));
            // Find A -> B -> C, where C can exit at the start.
            bool hasChain = board.Arrows.Any(a =>
            {
                Arrow b = board.GetBlocker(a);
                Arrow c = b == null ? null : board.GetBlocker(b);
                return c != null && board.CanExit(c);
            });
            Assert.That(hasChain, Is.True, path);
        }

        /// <summary>A missing resource returns an informative failure.</summary>
        [Test]
        public void MissingResourceFails()
        {
            LevelLoadResult result = LevelRepository.LoadFromResources("Levels/does_not_exist");
            Assert.That(result.Success, Is.False);
            Assert.That(result.Board, Is.Null);
            Assert.That(string.Join("; ", result.Errors), Does.Contain("Levels/does_not_exist").And.Contain("not found"));
        }
    }
}
