using ArrowEscape.Core;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Temporary skin tint with a WCAG luminance guard; Classic and Ice remain exact.</summary>
    public static class ArrowTint
    {
        /// <summary>Returns the normal arrow base tint at contrast at least three to one.</summary>
        public static Color For(string id,Color background)
        {
            if(id=="classic")return ViewTheme.ArrowColor;
            ShopItem item=ShopCatalog.Find(id);
            if(item==null||item.Slot!=ShopSlot.Arrow||!ColorUtility.TryParseHtmlString(item.PrimaryColor,out Color color))return ViewTheme.ArrowColor;
            return EnsureContrast(color,background);
        }
        /// <summary>Steps toward the better contrasting endpoint until the threshold is reached.</summary>
        public static Color EnsureContrast(Color color,Color background)
        {
            Color target=Contrast(Color.black,background)>=Contrast(Color.white,background)?Color.black:Color.white;
            Color original=color;
            for(int i=1;i<=20&&Contrast(color,background)<3;i++)color=Color.Lerp(original,target,i/20f);
            color.a=1;return color;
        }
        /// <summary>WCAG relative luminance contrast ratio.</summary>
        public static float Contrast(Color a,Color b)
        {float x=Luminance(a),y=Luminance(b);return (Mathf.Max(x,y)+.05f)/(Mathf.Min(x,y)+.05f);}
        private static float Luminance(Color c)=>.2126f*Linear(c.r)+.7152f*Linear(c.g)+.0722f*Linear(c.b);
        private static float Linear(float c)=>c<=.04045f?c/12.92f:Mathf.Pow((c+.055f)/1.055f,2.4f);
    }
}
