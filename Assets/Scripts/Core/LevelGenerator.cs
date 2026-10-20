using System;
using System.Collections.Generic;
namespace ArrowEscape.Core
{
    /// <summary>Backward PLACE/UNSLIDE generation with deterministic, explicitly ordered choices.</summary>
    public static class LevelGenerator
    {
        /// <summary>Per-candidate exhaustive budget; playouts and authority solving still run in full.</summary>
        public const int AnalysisStateLimit=5000;
        /// <summary>Attempts bounded candidates, returning the closest passing tier when needed.</summary>
        public static GenerationResult Generate(GeneratorSettings settings,int seed)
        {
            if(settings==null)throw new ArgumentNullException(nameof(settings));
            var errors=settings.Validate();if(errors.Count>0)throw new ArgumentException(string.Join(" ",errors),nameof(settings));
            Board best=null;LevelAnalysis bestAnalysis=null;DifficultyRating bestRating=null;List<SolverMove> bestPath=null;
            int bestDistance=int.MaxValue;var rejected=new int[6];
            for(int attempt=0;attempt<settings.MaxAttempts;attempt++)
            {
                Board candidate=Build(settings,new SeededRandom(Mix(seed,attempt)),out List<SolverMove> path);
                if(candidate==null){rejected[0]++;continue;}
                int rejection=QuickReject(candidate,settings);if(rejection>=0){rejected[rejection]++;continue;}
                LevelAnalysis analysis=LevelAnalyzer.Analyze(candidate,new AnalysisOptions(AnalysisStateLimit));
                if(analysis.Status!=SolveStatus.Solved){rejected[4]++;continue;}
                if(settings.IceCount>0&&settings.RequireSlides&&analysis.SlidesInSolution==0){rejected[5]++;continue;}
                DifficultyRating rating=DifficultyRater.Rate(analysis);int distance=Distance(rating.Score,settings.TargetTier);
                if(distance==0)return new GenerationResult(candidate,seed,attempt+1,analysis,rating,true,path);
                if(distance<bestDistance){best=candidate;bestAnalysis=analysis;bestRating=rating;bestPath=path;bestDistance=distance;}
            }
            if(best!=null)return new GenerationResult(best,seed,settings.MaxAttempts,bestAnalysis,bestRating,false,bestPath);
            string[] reasons={"No legal PLACE remained before the requested count.","Too few free exits at the start.",
                "Every arrow was free at the start.","More than 60 percent of arrows faced one direction.",
                "The authority solver did not prove solvability.","Required slides were not necessary."};
            int main=0;for(int i=1;i<rejected.Length;i++)if(rejected[i]>rejected[main])main=i;
            return new GenerationResult(null,seed,settings.MaxAttempts,null,null,false,null,reasons[main]);
        }
        // Fixed 32-bit avalanche of seed + golden-ratio increment * (attempt+1); never a platform hash.
        private static int Mix(int seed,int attempt)
        {
            unchecked {uint z=(uint)seed+0x9E3779B9u*((uint)attempt+1);z=(z^(z>>16))*0x85EBCA6Bu;
                z=(z^(z>>13))*0xC2B2AE35u;return (int)(z^(z>>16));}
        }
        private static Board Build(GeneratorSettings settings,SeededRandom random,out List<SolverMove> solution)
        {
            var board=new Board(settings.Width,settings.Height);var reverse=new List<SolverMove>();int iceLeft=settings.IceCount;
            for(int id=1;id<=settings.ArrowCount;id++)
            {
                List<Arrow> placements=GetPlacements(board);if(placements.Count==0){solution=null;return null;}
                var weights=new List<int>(placements.Count);
                foreach(Arrow place in placements)
                {
                    int blockers=0;foreach(Arrow existing in board.Arrows)if(OnForwardLine(existing,place.X,place.Y))blockers++;
                    weights.Add(1+settings.BlockBias*blockers);
                }
                Arrow chosen=placements[random.NextWeighted(weights)];
                bool ice=random.NextInt(settings.ArrowCount-id+1)<iceLeft;if(ice)iceLeft--;
                board.TryAddArrow(id,chosen.X,chosen.Y,chosen.Direction,ice?ArrowKind.Ice:ArrowKind.Normal,out _);
                reverse.Add(new SolverMove(id,false));
                List<SolverMove> unslides=GetUnslides(board);
                if(unslides.Count>0&&random.NextInt(2)==0)Unslide(board,unslides,random,reverse);
            }
            // Every reverse move meets its preconditions on the exact intermediate forward board.
            // Reversing these operations therefore gives a legal clearing solution.
            return Renumber(board,reverse,out solution);
        }
        internal static List<Arrow> GetPlacements(Board board)
        {
            var choices=new List<Arrow>();
            for(int y=board.Height-1;y>=0;y--)for(int x=0;x<board.Width;x++)
            {
                if(board.GetArrowAt(x,y)!=null)continue;
                for(int d=0;d<4;d++)
                {
                    Direction direction=(Direction)d;int cx=x+direction.GetDx(),cy=y+direction.GetDy();
                    while(board.InBounds(cx,cy)&&board.GetArrowAt(cx,cy)==null){cx+=direction.GetDx();cy+=direction.GetDy();}
                    if(!board.InBounds(cx,cy))choices.Add(new Arrow(0,x,y,direction));
                }
            }
            return choices;
        }
        internal static List<SolverMove> GetUnslides(Board board)
        {
            var arrows=new List<Arrow>(board.Arrows);arrows.Sort((a,b)=>a.Id.CompareTo(b.Id));var choices=new List<SolverMove>();
            foreach(Arrow arrow in arrows)
            {
                if(arrow.Kind!=ArrowKind.Ice)continue;int dx=arrow.Direction.GetDx(),dy=arrow.Direction.GetDy();
                if(board.GetArrowAt(arrow.X+dx,arrow.Y+dy)==null)continue;
                int x=arrow.X-dx,y=arrow.Y-dy;
                while(board.InBounds(x,y)&&board.GetArrowAt(x,y)==null)
                {choices.Add(new SolverMove(arrow.Id,true,x,y));x-=dx;y-=dy;}
            }
            return choices;
        }
        private static void Unslide(Board board,List<SolverMove> choices,SeededRandom random,List<SolverMove> reverse)
        {
            var ids=new List<int>();int previous=-1;
            foreach(SolverMove move in choices)if(ids.Count==0||move.ArrowId!=previous){ids.Add(move.ArrowId);previous=move.ArrowId;}
            int id=ids[random.NextInt(ids.Count)],start=0;while(choices[start].ArrowId!=id)start++;
            int count=0;while(start+count<choices.Count&&choices[start+count].ArrowId==id)count++;
            SolverMove chosen=choices[start+random.NextInt(count)];Arrow before=board.GetArrowById(id);
            board.TryMoveArrow(before,chosen.ToX,chosen.ToY,out _);reverse.Add(new SolverMove(id,true,before.X,before.Y));
        }
        private static bool OnForwardLine(Arrow arrow,int x,int y)
        {
            int dx=arrow.Direction.GetDx(),dy=arrow.Direction.GetDy();
            return dx!=0?y==arrow.Y&&((long)x-arrow.X)*dx>0:x==arrow.X&&((long)y-arrow.Y)*dy>0;
        }
        private static Board Renumber(Board source,List<SolverMove> reverse,out List<SolverMove> solution)
        {
            var result=new Board(source.Width,source.Height);var remap=new Dictionary<int,int>();int id=1;
            for(int y=source.Height-1;y>=0;y--)for(int x=0;x<source.Width;x++)
            {
                Arrow arrow=source.GetArrowAt(x,y);if(arrow==null)continue;
                remap.Add(arrow.Id,id);result.TryAddArrow(id++,x,y,arrow.Direction,arrow.Kind,out _);
            }
            solution=new List<SolverMove>(reverse.Count);
            for(int i=reverse.Count-1;i>=0;i--){var move=reverse[i];solution.Add(new SolverMove(remap[move.ArrowId],move.IsSlide,move.ToX,move.ToY));}
            return result;
        }
        private static int QuickReject(Board board,GeneratorSettings settings)
        {
            int free=LevelSolvability.CountFreeArrows(board);
            if(free<settings.MinFreeExitsAtStart)return 1;
            if(board.Arrows.Count>2&&free==board.Arrows.Count)return 2;
            var directions=new int[4];foreach(Arrow arrow in board.Arrows)directions[(int)arrow.Direction]++;
            if(board.Arrows.Count>=8)foreach(int count in directions)if(count*100>board.Arrows.Count*60)return 3;
            if(settings.IceCount>0&&settings.RequireSlides)
            {
                var normal=new Board(board.Width,board.Height);
                foreach(Arrow arrow in board.Arrows)normal.TryAddArrow(arrow.Id,arrow.X,arrow.Y,arrow.Direction,out _);
                if(LevelSolvability.IsSolvable(normal))return 5;
            }
            return -1;
        }
        private static int Distance(int score,DifficultyTier? tier)
        {
            if(!tier.HasValue)return 0;int low=0,high=1000;
            switch(tier.Value)
            {
                case DifficultyTier.Easy:high=DifficultyWeights.MediumThreshold-1;break;
                case DifficultyTier.Medium:low=DifficultyWeights.MediumThreshold;high=DifficultyWeights.HardThreshold-1;break;
                case DifficultyTier.Hard:low=DifficultyWeights.HardThreshold;high=DifficultyWeights.ExpertThreshold-1;break;
                case DifficultyTier.Expert:low=DifficultyWeights.ExpertThreshold;break;
            }
            return score<low?low-score:score>high?score-high:0;
        }
    }
}

