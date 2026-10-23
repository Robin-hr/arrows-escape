using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Integer-only backward construction of stationary Normal paths.</summary>
    public static class ShapeLevelGenerator
    {
        /// <summary>Returns the first tier match, otherwise the closest passing candidate.</summary>
        public static ShapeGenerationResult Generate(ShapeLevelSettings settings,int seed)
        {
            if(settings==null)throw new ArgumentNullException(nameof(settings));
            var errors=settings.Validate();if(errors.Count>0)throw new ArgumentException(string.Join(" ",errors));
            return Generate(settings,ShapeMask.Build(settings.Shape,settings.Width,settings.Height),seed);
        }
        /// <summary>Generates inside a custom silhouette (Journey levels); the settings' shape and box are not used.</summary>
        public static ShapeGenerationResult Generate(ShapeLevelSettings settings,ShapeMask mask,int seed)
        {
            if(settings==null)throw new ArgumentNullException(nameof(settings));if(mask==null)throw new ArgumentNullException(nameof(mask));
            if(settings.MinArrowLength>ShapeLevelSettings.CatalogMaxArrowLength)return new ShapeGenerationResult(null,mask,seed,0,0,null,null,false,"Minimum length exceeds the catalog maximum of ten cells.");
            ShapeGenerationResult best=null;int bestDistance=int.MaxValue;var rejected=new int[4];
            for(int attempt=0;attempt<settings.MaxAttempts;attempt++)
            {
                Board board=Build(mask,settings,new SeededRandom(Mix(seed,attempt)));
                int occupied=0,free=0;var counts=new int[4];foreach(Arrow arrow in board.Arrows){occupied+=arrow.Length;counts[(int)arrow.Direction]++;if(board.CanExit(arrow))free++;}
                int coverage=occupied*1000/mask.Area;
                if(coverage<settings.CoverageTargetPermille){rejected[0]++;continue;}
                if(board.Arrows.Count<settings.MinArrows||board.Arrows.Count>settings.MaxArrows||free<settings.MinFree||free*1000>settings.MaxFreeAtStartPermille*board.Arrows.Count){rejected[1]++;continue;}
                bool biased=false;foreach(int count in counts)if(count*100>45*board.Arrows.Count)biased=true;
                if(biased){rejected[2]++;continue;}
                if(!LevelSolvability.IsSolvable(board)){rejected[3]++;continue;}
                // Exhaustive search is bounded; deterministic playout metrics always run.
                LevelAnalysis analysis=LevelAnalyzer.Analyze(board,new AnalysisOptions(128,32));
                if(analysis.Status!=SolveStatus.Solved){rejected[3]++;continue;}
                DifficultyRating rating=DifficultyRater.Rate(analysis);int distance=TierDistance(rating.Score,settings.TargetTier);
                var result=new ShapeGenerationResult(board,mask,seed,attempt+1,coverage,analysis,rating,distance==0);
                if(distance==0)return result;
                if(distance<bestDistance){best=result;bestDistance=distance;}
            }
            if(best!=null)return new ShapeGenerationResult(best.Board,mask,seed,settings.MaxAttempts,best.CoveragePermille,best.Analysis,best.Rating,false);
            int main=0;for(int i=1;i<rejected.Length;i++)if(rejected[i]>rejected[main])main=i;
            string[] reasons={"Coverage below target","Initially free-arrow proportion outside limit","A head direction exceeds 45 percent","Solvability was not proven"};
            return new ShapeGenerationResult(null,mask,seed,settings.MaxAttempts,0,null,null,false,reasons[main]+" ("+rejected[main]+" attempts).");
        }
        /// <summary>Distance to the existing tier interval; zero for an unspecified target.</summary>
        public static int TierDistance(int score,DifficultyTier? tier)
        {
            if(!tier.HasValue)return 0;
            int[] bounds={0,DifficultyWeights.MediumThreshold,DifficultyWeights.HardThreshold,DifficultyWeights.ExpertThreshold,1001};
            int low=bounds[(int)tier],high=bounds[(int)tier+1]-1;return score<low?low-score:score>high?score-high:0;
        }
        private static int Mix(int seed,int attempt)
        {
            // Fixed Weyl progression; SplitMix64 performs the seed diffusion itself.
            return unchecked(seed+(int)(0x9E3779B9U*(uint)attempt));
        }
        private static Board Build(ShapeMask mask,ShapeLevelSettings s,SeededRandom random)
        {
            var board=new Board(mask.Width,mask.Height);var placed=new List<Arrow>();
            var heads=new List<Cell>();var directions=new List<Direction>();var weights=new List<int>();int misses=0;bool rebuild=true;var rayCounts=new int[mask.Width,mask.Height];
            while(misses<200)
            {
                // Failed growth changes no occupancy. Reuse the exact ordered candidates and
                // weights until a successful placement; this consumes precisely the same RNG draws.
                if(rebuild)
                {
                heads.Clear();directions.Clear();weights.Clear();Array.Clear(rayCounts,0,rayCounts.Length);
                // Cache how many existing head rays contain each cell, preserving the exact weights.
                foreach(Arrow a in placed)for(int rx=a.X+a.Direction.GetDx(),ry=a.Y+a.Direction.GetDy();board.InBounds(rx,ry);rx+=a.Direction.GetDx(),ry+=a.Direction.GetDy())rayCounts[rx,ry]++;
                for(int y=mask.Height-1;y>=0;y--)for(int x=0;x<mask.Width;x++)
                {
                    if(!Empty(mask,board,x,y))continue;
                    for(int directionIndex=0;directionIndex<4;directionIndex++)
                    {
                        Direction d=(Direction)directionIndex;int dx=d.GetDx(),dy=d.GetDy();if(!Empty(mask,board,x-dx,y-dy)||!ClearRay(board,x,y,d))continue;
                        int blocks=rayCounts[x,y];
                        heads.Add(new Cell(x,y));directions.Add(d);weights.Add(1+3*blocks);
                    }
                }
                rebuild=false;
                }
                if(heads.Count==0)break;
                int choice=random.NextWeighted(weights);var cells=Grow(mask,board,heads[choice],directions[choice],s,random);
                if(cells.Count<s.MinArrowLength){misses++;continue;}
                cells.Reverse();board.TryAddArrow(new Arrow(placed.Count+1,cells,directions[choice]),out Arrow arrow);placed.Add(arrow);misses=0;rebuild=true;
            }
            ExtendTails(board,mask,placed,Math.Min(s.MaxArrowLength,10),s.MaxBends);
            // Later arrows can only block earlier arrows. Removing in reverse placement order
            // reaches exactly the board on which each clear-ray PLACE was legal.
            placed.Sort((a,b)=>a.Y!=b.Y?b.Y.CompareTo(a.Y):a.X.CompareTo(b.X));var result=new Board(mask.Width,mask.Height);
            for(int i=0;i<placed.Count;i++)result.TryAddArrow(new Arrow(i+1,placed[i].Cells,placed[i].Direction),out _);
            return result;
        }
        private static List<Cell> Grow(ShapeMask mask,Board board,Cell head,Direction direction,ShapeLevelSettings s,SeededRandom random)
        {
            // When both dimensions exceed twice the length cap, favor short targets to avoid sealing large pockets.
            int target=random.NextInt(s.MinArrowLength,Math.Min(s.MaxArrowLength,10)+1);
            if(mask.Width>2*s.MaxArrowLength&&mask.Height>2*s.MaxArrowLength)target=Math.Min(target,Math.Min(random.NextInt(s.MinArrowLength,Math.Min(s.MaxArrowLength,10)+1),random.NextInt(s.MinArrowLength,Math.Min(s.MaxArrowLength,10)+1)));var cells=new List<Cell>{head,new Cell(head.X-direction.GetDx(),head.Y-direction.GetDy())};
            while(cells.Count<target)
            {
                Cell tail=cells[cells.Count-1],previous=cells[cells.Count-2];var choices=new List<Cell>();int fewest=5;
                for(int d=0;d<4;d++)
                {
                    var dir=(Direction)d;var next=new Cell(tail.X+dir.GetDx(),tail.Y+dir.GetDy());
                    if(!Empty(mask,board,next.X,next.Y)||cells.Contains(next)||OnRay(head,direction,next))continue;
                    int neighbors=0;for(int k=0;k<4;k++){var nd=(Direction)k;var c=new Cell(next.X+nd.GetDx(),next.Y+nd.GetDy());if(Empty(mask,board,c.X,c.Y)&&!cells.Contains(c)&&!OnRay(head,direction,c))neighbors++;}
                    if(neighbors<fewest){fewest=neighbors;choices.Clear();}if(neighbors==fewest)choices.Add(next);
                }
                if(choices.Count==0)break;if(s.MaxBends<int.MaxValue)for(int j=choices.Count-1;j>=0;j--){cells.Add(choices[j]);bool tooBent=Bends(cells)>s.MaxBends;cells.RemoveAt(cells.Count-1);if(tooBent)choices.RemoveAt(j);}if(choices.Count==0)break;bool turn=random.NextInt(100)<s.TurnBias;var preferred=new List<Cell>();
                foreach(Cell c in choices){bool straight=c.X-tail.X==tail.X-previous.X&&c.Y-tail.Y==tail.Y-previous.Y;if(straight!=turn)preferred.Add(c);}
                var pool=preferred.Count>0?preferred:choices;cells.Add(pool[random.NextInt(pool.Count)]);
            }
            return cells;
        }
        private static void ExtendTails(Board board,ShapeMask mask,List<Arrow> placed,int maxLength,int maxBends)
        {
            // A tail added to an earlier arrow must not obstruct itself or any later arrow:
            // those later arrows still have to leave first in the reversed construction.
            bool changed;do{changed=false;for(int i=0;i<placed.Count;i++)
            {
                Arrow a=placed[i];if(a.Length>=maxLength)continue;Cell tail=a.Cells[0];
                for(int d=0;d<4;d++)
                {
                    var dir=(Direction)d;var c=new Cell(tail.X+dir.GetDx(),tail.Y+dir.GetDy());if(!Empty(mask,board,c.X,c.Y))continue;
                    bool blocked=false;for(int j=i;j<placed.Count;j++)if(OnRay(placed[j],c)){blocked=true;break;}if(blocked)continue;
                    var cells=new List<Cell>{c};for(int k=0;k<a.Length;k++)cells.Add(a.Cells[k]);if(maxBends<int.MaxValue&&Bends(cells)>maxBends)continue;board.Remove(a);board.TryAddArrow(new Arrow(a.Id,cells,a.Direction),out Arrow grown);placed[i]=grown;changed=true;break;
                }
            }}while(changed);
        }
        private static int Bends(IReadOnlyList<Cell> cells)
        {int bends=0;for(int i=2;i<cells.Count;i++)if(cells[i].X-cells[i-1].X!=cells[i-1].X-cells[i-2].X||cells[i].Y-cells[i-1].Y!=cells[i-1].Y-cells[i-2].Y)bends++;return bends;}
        private static bool Empty(ShapeMask m,Board b,int x,int y)=>m.Contains(x,y)&&b.GetArrowAt(x,y)==null;
        private static bool ClearRay(Board b,int x,int y,Direction d){for(x+=d.GetDx(),y+=d.GetDy();b.InBounds(x,y);x+=d.GetDx(),y+=d.GetDy())if(b.GetArrowAt(x,y)!=null)return false;return true;}
        private static bool OnRay(Arrow a,Cell c)=>OnRay(new Cell(a.X,a.Y),a.Direction,c);
        private static bool OnRay(Cell head,Direction d,Cell c)=>d.GetDx()!=0?c.Y==head.Y&&(c.X-head.X)*d.GetDx()>0:c.X==head.X&&(c.Y-head.Y)*d.GetDy()>0;
    }
}







