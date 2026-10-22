using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Immutable visual recipe. Optional layers never affect gameplay or inventory.</summary>
    public sealed class ArrowSkin
    {
        /// <summary>Permanent catalog id.</summary>
        public string Id { get; }
        /// <summary>Shader pattern index.</summary>
        public int Index { get; }
        /// <summary>Across-width body colors.</summary>
        public Color Primary { get; }
        /// <summary>Core, rim and trail accent.</summary>
        public Color Secondary { get; }
        /// <summary>Signature idle loop in seconds.</summary>
        public float Period { get; }
        /// <summary>Lingering particle life.</summary>
        public float TrailSeconds { get; }
        /// <summary>Presentation preference only; never saved as a theme or setting.</summary>
        public bool PrefersDarkBoard=>IsElemental||IsRare;
        /// <summary>Fully procedural Rare and Premium recipes.</summary>
        public bool IsRare=>Index>=5&&Index<=9;
        /// <summary>Recipes rendered without cropped art.</summary>
        public bool IsProcedural=>IsElemental||IsRare;
        /// <summary>Procedural Elemental recipes, independent of the permanent numeric pattern indices.</summary>
        public bool IsElemental=>Index>=0&&Index<=3;
        /// <summary>Per-recipe presentation background; never changes the saved theme.</summary>
        public Color DarkBoardColor=>Index==5?new Color32(16,18,24,255):Index==6?new Color32(15,17,22,255):Index==7?new Color32(14,13,24,255):Index==8?new Color32(7,19,13,255):Index==9?new Color32(20,7,10,255):Index==0?new Color32(11,22,32,255):Index==2?new Color32(7,20,24,255):Index==3?new Color32(14,12,20,255):new Color32(13,15,18,255);
        /// <summary>The short name-defining promise shown beneath the shop description.</summary>
        public string Signature=>Index==0?"Its frozen fangs bite, and frost spreads where it strikes.":Index==1?"Burns a blazing streak through everything it passes.":Index==2?"Raw energy, barely contained, released in a single bolt.":Index==3?"It swallows the light and leaves in a whisper.":Index==5?"Born of fire, cooled to a razor edge.":Index==6?"Mirror-bright chrome that pierces light itself.":Index==7?"Writes its path across the stars.":Index==8?"Coils, hisses, and strikes.":Index==9?"An ancient heart still beats inside.":"";
        /// <summary>Constructs a recipe with no mutable runtime state.</summary>
        public ArrowSkin(string id,int index,string primary,string secondary,float period,float trail)
        {Id=id;Index=index;ColorUtility.TryParseHtmlString(primary,out Color a);ColorUtility.TryParseHtmlString(secondary,out Color b);Primary=a;Secondary=b;Period=period;TrailSeconds=trail;}
    }
}
