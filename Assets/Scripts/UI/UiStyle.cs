using ArrowEscape.View;
using UnityEngine;
namespace ArrowEscape.UI
{
    /// <summary>Shared reference-size UI tokens. Reference units are twice the design point size.</summary>
    public static class UiStyle
    {
        /// <summary>Neon space background behind full screens.</summary>
        public static Color Background=>new Color32(5,6,26,255);
        /// <summary>Dark glass card surface.</summary>
        public static Color Surface=>new Color32(20,16,72,245);
        /// <summary>Primary text on neon surfaces.</summary>
        public static Color Ink=>new Color32(236,240,255,255);
        /// <summary>Secondary readable text.</summary>
        public static Color Muted=>new Color32(170,170,220,255);
        /// <summary>Coin and star gold.</summary>
        public static Color Gold=>new Color32(245,183,0,255);
        /// <summary>Destructive action tint.</summary>
        public static Color Danger=>new Color32(229,72,77,255);
        /// <summary>32-point heading.</summary>
        public const int Heading=64;
        /// <summary>22-point section title.</summary>
        public const int Section=44;
        /// <summary>17-point body.</summary>
        public const int Body=34;
        /// <summary>14-point caption.</summary>
        public const int Caption=28;
        private static Font font;
        /// <summary>OFL Fredoka bundled as the single new font, with a built-in fallback.</summary>
        public static Font Font
        {get{if(font==null)font=Resources.Load<Font>("Fredoka");if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");return font;}}
        /// <summary>Category frame color.</summary>
        public static Color Category(string category)
        {
            string hex=category=="Elemental & Magic"?"#38BDF8":category=="Rare & Premium"?"#A78BFA":category=="Tactical & Sci-Fi"?"#34D399":category=="Organic & Strange"?"#84CC16":category=="Legendary"?"#F59E0B":
                category=="Themes"?"#FF5FD2":category=="Cute Crew"?"#FF9CC8":category=="Cosmic Crew"?"#7CF0FF":category=="Metal Frames"?"#E0B080":category=="Magic Frames"?"#B85CFF":category=="Mythic Frames"?"#FFC83C":"#38E1FF";
            ColorUtility.TryParseHtmlString(hex,out Color color);return color;
        }
        /// <summary>Adds a cached glyph without blocking pointer rays.</summary>
        public static UnityEngine.UI.Image Icon(string name,Transform parent,UiSpriteShape shape,Vector2 position,float size,Color color)
        {
            var rect=UiFactory.Rect(name,parent,new Vector2(size,size),position);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite=SpriteFactory.GetUiSprite(shape);image.color=color;image.raycastTarget=false;return image;
        }
        /// <summary>Shared rounded surface and soft shadow; preserves its existing layout.</summary>
        public static void SurfaceFor(UnityEngine.UI.Image image,bool shadow=false,bool card=false)
        {
            image.sprite=SpriteFactory.GetUiSprite(card?UiSpriteShape.Card:UiSpriteShape.RoundedRect);image.type=UnityEngine.UI.Image.Type.Sliced;
            if(!shadow)return;
            var rect=UiFactory.Rect("SoftShadow",image.transform,new Vector2(0,0),Vector2.zero);UiFactory.Stretch(rect);rect.anchoredPosition=new Vector2(0,-6);rect.offsetMin-=Vector2.one*7;rect.offsetMax+=Vector2.one*7;rect.SetAsFirstSibling();
            var shade=rect.gameObject.AddComponent<UnityEngine.UI.Image>();shade.sprite=SpriteFactory.GetUiSprite(UiSpriteShape.Shadow);shade.type=UnityEngine.UI.Image.Type.Sliced;shade.color=new Color(0,0,0,.28f);shade.raycastTarget=false;
        }
    }
}
