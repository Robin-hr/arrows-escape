using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace ArrowEscape.Editor
{
    /// <summary>Writes a labelled dark contact sheet without modifying or resampling source assets.</summary>
    public static class ArrowArtContact
    {
        private const string Alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789";
        private static readonly string[] Glyphs={"01110100011000111111100011000110001","11110100011000111110100011000111110","01111100001000010000100001000001111","11110100011000110001100011000111110","11111100001000011110100001000011111","11111100001000011110100001000010000","01111100001000010111100011000101111","10001100011000111111100011000110001","11111001000010000100001000010011111","00111000100001000010100101001001100","10001100101010011000101001001010001","10000100001000010000100001000011111","10001110111010110101100011000110001","10001110011010110011100011000110001","01110100011000110001100011000101110","11110100011000111110100001000010000","01110100011000110001101011001001101","11110100011000111110101001001010001","01111100001000001110000010000111110","11111001000010000100001000010000100","10001100011000110001100011000101110","10001100011000110001100010101000100","10001100011000110101101011101110001","10001100010101000100010101000110001","10001100010101000100001000010000100","11111000010001000100010001000011111","00000000000000000000000000000011111","01110100011001110101110011000101110","00100011000010000100001000010001110","01110100010000100010001000100011111","11110000010000101110000010000111110","00010001100101010010111110001000010","11111100001000011110000010000111110","01110100001000011110100011000101110","11111000010001000100010000100001000","01110100011000101110100011000101110","01110100011000101111000010000101110"};
        /// <summary>Creates a deterministic labelled grid of extracted references.</summary>
        public static void Write(IReadOnlyList<Texture2D> images,IReadOnlyList<string> ids,string path,int columns=3)
        {
            int rows=(images.Count+columns-1)/columns;var t=new Texture2D(columns*440,rows*160,TextureFormat.RGBA32,false);var colors=new Color32[t.width*t.height];for(int i=0;i<colors.Length;i++)colors[i]=new Color32(14,17,22,255);t.SetPixels32(colors);
            for(int i=0;i<images.Count;i++)
            {
                Texture2D src=images[i];int ox=i%columns*440,oy=(rows-1-i/columns)*160;float scale=Mathf.Min(405f/src.width,115f/src.height);int w=Mathf.RoundToInt(src.width*scale),h=Mathf.RoundToInt(src.height*scale);
                for(int y=0;y<h;y++)for(int x=0;x<w;x++){Color c=src.GetPixelBilinear(x/(float)w,y/(float)h);t.SetPixel(ox+18+x+(405-w)/2,oy+32+y+(115-h)/2,Color.Lerp(new Color32(14,17,22,255),c,c.a));}
                string label=ids[i].ToUpperInvariant();for(int c=0;c<label.Length;c++){int g=Alphabet.IndexOf(label[c]);if(g<0)continue;for(int y=0;y<7;y++)for(int x=0;x<5;x++)if(Glyphs[g][y*5+x]=='1')for(int a=0;a<2;a++)for(int b=0;b<2;b++)t.SetPixel(ox+18+c*12+x*2+a,oy+10+(6-y)*2+b,Color.white);}
            }
            t.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);
        }
    }
}
