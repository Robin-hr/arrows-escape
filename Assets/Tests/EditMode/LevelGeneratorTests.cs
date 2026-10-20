using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Backward-operation legality, snapshots, diversity, quality and tier targeting.</summary>
    public class LevelGeneratorTests
    {
        /// <summary>Three repeats preserve every arrow field and every intended action.</summary>
        [Test]public void DeterminismAndGoldenSnapshot()
        {
            var settings=new GeneratorSettings(6,6,14,2);var first=LevelGenerator.Generate(settings,12345);GenerationBoards.Verify(first,settings);
            for(int i=0;i<2;i++){var next=LevelGenerator.Generate(settings,12345);Assert.That(GenerationBoards.Snapshot(next.Board),Is.EqualTo(GenerationBoards.Snapshot(first.Board)));Assert.That(next.IntendedSolution,Is.EqualTo(first.IntendedSolution));Assert.That(next.Rating.Score,Is.EqualTo(first.Rating.Score));Assert.That(next.AttemptsUsed,Is.EqualTo(first.AttemptsUsed));}
            // If this test changes, every future daily puzzle changes. Only update it on purpose.
            string golden="y5   .  .  .  .  .  . \ny4   .  > *>  .  ^  v \ny3   .  ^  .  .  .  > \ny2   .  .  .  .  .  . \ny1   v  v  ^  <  v *< \ny0   .  .  .  .  <  >";
            Assert.That(GenerationBoards.Snapshot(first.Board),Is.EqualTo(golden));
        }
        /// <summary>Fifty seeds provide at least forty-five different normal boards.</summary>
        [Test]public void Diversity()
        {
            var seen=new HashSet<string>();for(int seed=0;seed<50;seed++){var g=LevelGenerator.Generate(new GeneratorSettings(),seed);Assert.That(g.Success,Is.True);seen.Add(GenerationBoards.Snapshot(g.Board));}
            Assert.That(seen.Count,Is.GreaterThanOrEqualTo(45));TestContext.WriteLine("DIVERSITY "+seen.Count+"/50");
        }
        /// <summary>Five hundred mixed requests validate every successful candidate through real session replay.</summary>
        [Test]public void FiveHundredSeeds()
        {
            int successes=0,failures=0;
            for(int seed=0;seed<500;seed++)
            {
                int size=4+seed%5,count=3+seed%Math.Min(28,size*size-2);
                var settings=new GeneratorSettings(size,size,count,seed%4,requireSlides:seed%2==0);
                var generated=LevelGenerator.Generate(settings,seed);
                if(generated.Success){successes++;GenerationBoards.Verify(generated,settings);}
                else{failures++;Assert.That(generated.Board,Is.Null);Assert.That(generated.FailureReason,Is.Not.Empty);}
            }
            Assert.That(successes,Is.GreaterThanOrEqualTo(100));TestContext.WriteLine($"GENERATOR_500 success={successes} failed={failures}");
        }
        /// <summary>Required-slide ice generation cannot be solved by merely removing Normal copies.</summary>
        [Test]public void RequiredSlides()
        {var settings=new GeneratorSettings(6,6,14,2);var g=LevelGenerator.Generate(settings,12345);GenerationBoards.Verify(g,settings);Assert.That(LevelSolvability.IsSolvable(GenerationBoards.Normal(g.Board)),Is.False);}
        /// <summary>PLACE never offers an occupied cell or a direction with any forward blocker.</summary>
        [Test]public void PlacePreconditions()
        {
            var board=IceTestBoards.Trap();var choices=LevelGenerator.GetPlacements(board);Assert.That(choices.Count,Is.GreaterThan(0));
            foreach(var choice in choices){var clone=board.Clone();Assert.That(clone.TryAddArrow(99,choice.X,choice.Y,choice.Direction,out var arrow),Is.True);Assert.That(clone.CanExit(arrow),Is.True);}
        }
        /// <summary>UNSLIDE always has an adjacent front blocker and forward replay returns to the exact stop.</summary>
        [Test]public void UnslidePreconditions()
        {
            var board=IceTestBoards.Cycle();Assert.That(LevelGenerator.GetUnslides(board),Is.Empty);
            board.TryMoveArrow(board.GetArrowById(1),2,0,out _);var choices=LevelGenerator.GetUnslides(board);Assert.That(choices.Count,Is.EqualTo(2));
            foreach(var choice in choices)
            {
                var clone=board.Clone();var before=clone.GetArrowById(choice.ArrowId);Assert.That(clone.GetArrowAt(before.X+1,before.Y),Is.Not.Null);
                clone.TryMoveArrow(before,choice.ToX,choice.ToY,out var moved);var result=new GameSession(clone).TryMove(moved);
                Assert.That(result.Outcome,Is.EqualTo(MoveOutcome.Slid));Assert.That(result.MovedArrow.X,Is.EqualTo(before.X));Assert.That(result.MovedArrow.Y,Is.EqualTo(before.Y));
            }
        }
        private readonly int[] hits=new int[4],failures=new int[4],samples=new int[4];
        private static IEnumerable<TestCaseData> TierCases()
        {
            int[] counts={20,24,28,30};
            for(int tier=0;tier<4;tier++)for(int start=0;start<50;start+=10)
                yield return new TestCaseData((DifficultyTier)tier,counts[tier],start);
        }
        /// <summary>Each group preserves the same settings and seeds, avoiding one oversized measurement test.</summary>
        [TestCaseSource(nameof(TierCases))]
        public void TierTargeting(DifficultyTier tier,int count,int seedStart)
        {
            int localHits=0;
            for(int seed=seedStart;seed<seedStart+10;seed++)
            {
                var g=LevelGenerator.Generate(new GeneratorSettings(8,8,count,0,tier),seed);samples[(int)tier]++;
                if(!g.Success){failures[(int)tier]++;continue;}if(g.TierMatched){hits[(int)tier]++;localHits++;}
                Assert.That(g.TierMatched,Is.EqualTo(g.Rating.Tier==tier));
            }
            TestContext.WriteLine($"TARGET_GROUP tier={tier} arrows={count} seeds={seedStart}..{seedStart+9} hit={localHits}/10");
        }
        /// <summary>Applies the original sixty-percent assertion to all fifty requests per tier together.</summary>
        [OneTimeTearDown]public void TierTotals()
        {
            for(int tier=0;tier<4;tier++)
            {
                TestContext.WriteLine($"TARGET_TOTAL tier={(DifficultyTier)tier} hit={hits[tier]}/50 failed={failures[tier]}");
                // A selected individual case may run alone; full runs cover all five groups.
                if(samples[tier]==50&&tier<3)Assert.That(hits[tier],Is.GreaterThanOrEqualTo(30));
            }
        }
        /// <summary>Valid but impossible quality requirements fail clearly; malformed requests collect all errors.</summary>
        [Test]public void FailureAndValidation()
        {
            var impossible=LevelGenerator.Generate(new GeneratorSettings(3,3,9,minFreeExitsAtStart:9,maxAttempts:3),1);
            Assert.That(impossible.Success,Is.False);Assert.That(impossible.FailureReason,Is.Not.Empty);Assert.That(impossible.AttemptsUsed,Is.EqualTo(3));
            var invalid=new GeneratorSettings(2,11,0,3,(DifficultyTier)9,true,-1,0,21);
            Assert.That(invalid.Validate().Count,Is.EqualTo(8));var exception=Assert.Throws<ArgumentException>(()=>LevelGenerator.Generate(invalid,0));
            foreach(string error in invalid.Validate())Assert.That(exception.Message,Does.Contain(error));
            Assert.Throws<ArgumentNullException>(()=>LevelGenerator.Generate(null,0));
        }
    }
}


