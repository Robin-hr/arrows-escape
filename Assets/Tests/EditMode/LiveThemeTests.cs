using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Live themes remain cosmetic, bounded and independent of player-file formats.</summary>
    public sealed class LiveThemeTests
    {
        private static readonly string[] ids={"grad_midnight","grad_aurora","grad_sunset","grad_lagoon","grad_emerald","grad_rose","geo_crystal","geo_neon","geo_gold"};
        /// <summary>Every released theme can be bought and equipped at its unchanged price.</summary>
        [TestCaseSource(nameof(ids))]public void PurchaseAndPresentation(string id)
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.Inventory.Shop.AddCoins(10000);int before=scene.Bootstrap.Inventory.State.Coins;Assert.That(scene.Bootstrap.Inventory.TryBuy(id),Is.True);Assert.That(scene.Bootstrap.Inventory.State.Coins,Is.EqualTo(before-ShopCatalog.Find(id).Price));Assert.That(scene.Bootstrap.Inventory.TryEquip(id),Is.True);
                Assert.That(scene.Board.ThemeId,Is.EqualTo(id));Assert.That(scene.Board.IsLightBoard,Is.False);Assert.That(scene.Game.GetComponent<ThemeBackdrop>().ThemeId,Is.EqualTo(id));
                scene.Bootstrap.ShowLevel(0);foreach(var arrow in scene.Board.GetComponentsInChildren<ArrowView>())Assert.That(arrow.BaseColor,Is.EqualTo(ThemeDesign.ClassicInk(id)));
                scene.Board.SetArrowSkin("inferno_streak");scene.Game.GetComponent<CameraFitter>().ApplyBackground();Assert.That(Camera.main.backgroundColor,Is.EqualTo(ThemeDesign.Background(id)));
                var effect=scene.Game.GetComponent<ThemeBackdrop>();Assert.That(effect.DrawCount,Is.LessThanOrEqualTo(8));Assert.That(effect.ParticleCount,Is.LessThanOrEqualTo(150));scene.Game.SetPaused(true);float clock=effect.Clock;effect.AdvanceFrame(1);Assert.That(effect.Clock,Is.EqualTo(clock));effect.Tick(.1f);Assert.That(effect.Clock,Is.GreaterThan(clock));
                scene.Bootstrap.Inventory.TryEquip("paper");scene.Bootstrap.Inventory.TryEquip("classic");Assert.That(scene.Board.HasDarkTheme,Is.False);Assert.That(Camera.main.backgroundColor,Is.EqualTo(ViewTheme.Background));
            }
        }
        /// <summary>Feedback has at least three-to-one palette contrast on every equipped dark theme.</summary>
        [TestCaseSource(nameof(ids))]public void PaletteContrast(string id)
        {var background=ThemeDesign.Background(id);Assert.That(ArrowTint.Contrast(ThemeDesign.ClassicInk(id),background),Is.GreaterThanOrEqualTo(3));foreach(var color in new[]{ViewTheme.HighlightColor,ViewTheme.BlockerTint,ViewTheme.HintTint,ViewTheme.GuideLineColor})Assert.That(ArrowTint.Contrast(ArrowTint.EnsureContrast(color,background),background),Is.GreaterThanOrEqualTo(3));}
        /// <summary>Prepared parallax updates allocate no managed memory.</summary>
        [Test]public void TickIsAllocationFree()
        {
            var host=new GameObject("Theme allocation fixture");try{var camera=host.AddComponent<Camera>();var theme=host.AddComponent<ThemeBackdrop>();theme.Bind(camera);theme.SetTheme("geo_neon");theme.Tick(.01f);long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)theme.Tick(.01f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);}finally{Object.DestroyImmediate(host);}
        }
        /// <summary>Paper keeps its exact baseline colors.</summary>
        [Test]public void PaperIsUnchanged(){Assert.That(ThemeDesign.Background("paper"),Is.EqualTo(ViewTheme.Background));Assert.That(ThemeDesign.ClassicInk("paper"),Is.EqualTo(ViewTheme.ArrowColor));}
    }
}
