using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Antialiased white UI glyph rasterizer; runs only on a cache miss.</summary>
    internal static class UiSpriteRaster
    {
        internal static Sprite Create(UiSpriteShape shape)
        {
            const int size=96;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float alpha=0;
                for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++)alpha+=Coverage(shape,new Vector2((x+(sx+.5f)/2)/size,(y+(sy+.5f)/2)/size));
                pixels[y*size+x]=new Color(1,1,1,alpha/4);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=shape.ToString(),hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels(pixels);texture.Apply();
            bool sliced=shape==UiSpriteShape.RoundedRect||shape==UiSpriteShape.Card||shape==UiSpriteShape.Shadow;
            var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size,0,SpriteMeshType.FullRect,sliced?new Vector4(24,24,24,24):Vector4.zero);
            sprite.hideFlags=HideFlags.HideAndDontSave;return sprite;
        }
        private static float Coverage(UiSpriteShape shape,Vector2 p)
        {
            Vector2 q=p-new Vector2(.5f,.5f);float r=q.magnitude;bool fill=false;
            switch(shape)
            {
                case UiSpriteShape.RoundedRect: case UiSpriteShape.Card: case UiSpriteShape.Shadow:
                    float radius=shape==UiSpriteShape.Card?.167f:.208f;
                    Vector2 d=new Vector2(Mathf.Max(Mathf.Abs(q.x)-(.5f-radius),0),Mathf.Max(Mathf.Abs(q.y)-(.5f-radius),0));
                    float edge=d.magnitude-radius;
                    return shape==UiSpriteShape.Shadow?Mathf.Clamp01((.04f-edge)/.12f)*.2f:edge<=0?1:0;
                case UiSpriteShape.Coin: fill=r<.44f&&(r>.33f||Mathf.Abs(q.x)<.045f&&Mathf.Abs(q.y)<.22f);break;
                case UiSpriteShape.Lock: fill=(Mathf.Abs(q.x)<.28f&&q.y<.05f&&q.y>-.35f)||(r<.27f&&r>.18f&&q.y>=0);break;
                case UiSpriteShape.Check: fill=Line(p,new Vector2(.16f,.48f),new Vector2(.4f,.24f),.06f)||Line(p,new Vector2(.4f,.24f),new Vector2(.85f,.77f),.06f);break;
                case UiSpriteShape.Back: fill=Line(p,new Vector2(.2f,.5f),new Vector2(.8f,.5f),.055f)||Line(p,new Vector2(.2f,.5f),new Vector2(.48f,.78f),.055f)||Line(p,new Vector2(.2f,.5f),new Vector2(.48f,.22f),.055f);break;
                case UiSpriteShape.Cart: fill=Line(p,new Vector2(.15f,.8f),new Vector2(.3f,.8f),.04f)||Line(p,new Vector2(.3f,.8f),new Vector2(.4f,.34f),.04f)||Line(p,new Vector2(.4f,.34f),new Vector2(.78f,.34f),.04f)||Mathf.Abs(p.y-.67f)<.04f&&p.x>.32f&&p.x<.86f||(p-new Vector2(.45f,.2f)).magnitude<.065f||(p-new Vector2(.76f,.2f)).magnitude<.065f;break;
                case UiSpriteShape.Gear: fill=r>.19f&&r<.33f;for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var v=new Vector2(Mathf.Cos(a),Mathf.Sin(a));fill|=Line(p,new Vector2(.5f,.5f)+v*.28f,new Vector2(.5f,.5f)+v*.43f,.065f);}break;
                case UiSpriteShape.Grid: fill=(Mathf.Abs(q.x)<.11f||Mathf.Abs(q.x-.29f)<.11f||Mathf.Abs(q.x+.29f)<.11f)&&(Mathf.Abs(q.y)<.11f||Mathf.Abs(q.y-.29f)<.11f||Mathf.Abs(q.y+.29f)<.11f);break;
            }
            return fill?1:0;
        }
        private static bool Line(Vector2 p,Vector2 a,Vector2 b,float width)
        {Vector2 d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);return (p-a-d*t).sqrMagnitude<=width*width;}
    }
}
