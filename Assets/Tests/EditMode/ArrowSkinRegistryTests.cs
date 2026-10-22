using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Only ten recipes are implemented and Classic/Ice preserve the original contract.</summary>
    public sealed class ArrowSkinRegistryTests
    {
        /// <summary>Permanent ids match the available catalog, while Part 2 remains tint-only.</summary>
        [Test] public void TenRecipesAndTenFallbacks()
        {
            Assert.That(ArrowSkinRegistry.All.Count,Is.EqualTo(9));int implemented=0,fallback=0;
            foreach(var item in ShopCatalog.Items)if(item.Slot==ShopSlot.Arrow&&item.Id!="classic"){if(ArrowSkinRegistry.Find(item.Id)!=null)implemented++;else fallback++;}
            Assert.That(implemented,Is.EqualTo(9));Assert.That(fallback,Is.EqualTo(10));Assert.That(ArrowSkinRegistry.Find("classic"),Is.Null);Assert.That(ArrowSkinRegistry.Find("unknown"),Is.Null);
            foreach(var skin in ArrowSkinRegistry.All){Assert.That(ShopCatalog.Find(skin.Id),Is.Not.Null);if(!skin.PrefersDarkBoard)Assert.That(ArrowSkinResources.Load().Find(skin.Id).Full,Is.Not.Null);}
        }
        /// <summary>Unskinning restores the exact original sprite/material/rotation/scale/color; Ice never gains a skin.</summary>
        [Test] public void ClassicAndIceAreExact()
        {
            var host=new GameObject("skin compatibility");try
            {
                var board=host.AddComponent<BoardView>();var model=new Board(3,3);model.TryAddArrow(1,0,0,Direction.Up,out _);model.TryAddArrow(2,2,2,Direction.Left,ArrowKind.Ice,out _);board.Show(model);board.TryGetArrowView(1,out var normal);board.TryGetArrowView(2,out var ice);
                var sprite=normal.SpriteRenderer.sprite;var material=normal.SpriteRenderer.sharedMaterial;var position=normal.transform.position;var scale=normal.transform.localScale;var rotation=normal.transform.rotation;var color=normal.SpriteRenderer.color;
                foreach(var skin in ArrowSkinRegistry.All){board.SetArrowSkin(skin.Id);Assert.That(ice.Skin,Is.Null);Assert.That(ice.BaseColor,Is.EqualTo(ViewTheme.IceArrowColor));}
                board.SetArrowSkin("classic");Assert.That(normal.SpriteRenderer.enabled,Is.True);Assert.That(normal.SpriteRenderer.sprite,Is.SameAs(sprite));Assert.That(normal.SpriteRenderer.sharedMaterial,Is.SameAs(material));Assert.That(normal.transform.position,Is.EqualTo(position));Assert.That(normal.transform.localScale,Is.EqualTo(scale));Assert.That(normal.transform.rotation,Is.EqualTo(rotation));Assert.That(normal.SpriteRenderer.color,Is.EqualTo(color));
            }finally{Object.DestroyImmediate(host);}
        }
        /// <summary>Explicit shader references and blend settings are present for a player build.</summary>
        [Test] public void LightDarkUsesAlphaOrAdditive()
        {var r=ArrowSkinResources.Load();Assert.That(r.Ribbon,Is.Not.Null);Assert.That(r.Glow,Is.Not.Null);Assert.That(SkinMaterials.IsLight(ViewTheme.Background),Is.True);Assert.That(SkinMaterials.IsLight(new Color32(14,17,22,255)),Is.False);Assert.That(SkinMaterials.Glow(true).GetInt("_DstBlend"),Is.EqualTo((int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));Assert.That(SkinMaterials.Glow(false).GetInt("_DstBlend"),Is.EqualTo((int)UnityEngine.Rendering.BlendMode.One));}
    }
}
