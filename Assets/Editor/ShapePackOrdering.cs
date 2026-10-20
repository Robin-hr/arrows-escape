using System;
using System.Collections.Generic;
namespace ArrowEscape.Editor
{
    /// <summary>Stable score order with nearest valid swaps and four intervening levels per shape.</summary>
    public static class ShapePackOrdering
    {
        /// <summary>Orders successful items and assigns their final catalog numbers.</summary>
        public static IReadOnlyList<ShapePackItem> Order(IReadOnlyList<ShapePackItem> source)
        {
            var items=new List<ShapePackItem>(source);
            foreach(var item in items)if(item.Result==null||!item.Result.Success)throw new InvalidOperationException("A pack entry failed: "+item.Entry.Shape);
            items.Sort((a,b)=>{int c=a.Result.Rating.Score.CompareTo(b.Result.Rating.Score);return c!=0?c:a.Entry.Number.CompareTo(b.Entry.Number);});
            for(int i=0;i<items.Count;i++)
            {
                if(VarietyAt(items,i))continue;
                bool found=false;
                for(int j=i+1;j<items.Count;j++)
                {
                    Swap(items,i,j);
                    bool valid=ScoreEdges(items);
                    for(int k=0;k<=i&&valid;k++)valid=VarietyAt(items,k);
                    if(valid){found=true;break;}
                    Swap(items,i,j);
                }
                if(!found)throw new InvalidOperationException("No permitted variety swap at position "+(i+4));
            }
            if(items.Count!=57||items[0].Result.Rating.Score>230||!ScoreEdges(items))throw new InvalidOperationException("Pack fails count, opening score or neighbour smoothness.");
            for(int i=0;i<items.Count;i++)
            {
                var old=items[i];var e=old.Entry;
                items[i]=new ShapePackItem(new ShapePackEntry(i+4,e.Shape,e.Width,e.Height,e.BaseSeed,targetArea:e.TargetArea),old.Result,old.Milliseconds);
            }
            return items.AsReadOnly();
        }
        private static bool VarietyAt(List<ShapePackItem> items,int i)
        {for(int j=Math.Max(0,i-4);j<i;j++)if(items[j].Entry.Shape==items[i].Entry.Shape)return false;return true;}
        private static bool ScoreEdges(List<ShapePackItem> items)
        {for(int i=1;i<items.Count;i++){int d=items[i].Result.Rating.Score-items[i-1].Result.Rating.Score;if(d < -15||d>45)return false;}return true;}
        private static void Swap(List<ShapePackItem> items,int a,int b){var old=items[a];items[a]=items[b];items[b]=old;}
    }
}
