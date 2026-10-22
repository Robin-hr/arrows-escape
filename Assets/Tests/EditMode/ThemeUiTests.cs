using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Full live theme cards preserve mask boundaries, labels and the existing navigation model.</summary>
    public sealed class ThemeUiTests
    {
        /// <summary>Every theme preview is live, fills its rounded area, and leaves both text rows unobscured.</summary>
        [Test]public void CardsAndDetail()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {scene.Flow.OpenShop();scene.Flow.Shop.ThemesButton.onClick.Invoke();Canvas.ForceUpdateCanvases();foreach(var card in scene.Flow.Shop.Cards){if(card.Item.Id=="paper")continue;Assert.That(card.Preview.texture,Is.TypeOf<RenderTexture>());Assert.That(card.Preview.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode,Is.EqualTo(UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent));var area=(RectTransform)card.Preview.transform.parent;Assert.That(area.GetComponent<UnityEngine.UI.Mask>(),Is.Not.Null);var name=(RectTransform)card.Rect.Find("Face/Name");var state=(RectTransform)card.Rect.Find("Face/State");Assert.That(area.anchorMin.y,Is.GreaterThan(name.anchorMax.y));Assert.That(name.anchorMin.y,Is.GreaterThan(state.anchorMax.y));}scene.Flow.Shop.OpenDetail(1);Assert.That(scene.Flow.Shop.Detail.HasLivePreview,Is.True);Assert.That(scene.Flow.Shop.Detail.Preview.texture,Is.TypeOf<RenderTexture>());}
        }
        /// <summary>Equipment events apply the live theme through the existing menu background interface.</summary>
        [Test]public void MenuUpdatesAndPaperReturns()
        {using(var scene=new EconomyTestScene(withUi:true)){scene.Bootstrap.Inventory.Shop.AddCoins(1000);scene.Bootstrap.Inventory.TryBuy("cosmic_nebula");scene.Bootstrap.Inventory.TryEquip("cosmic_nebula");Assert.That(scene.Flow.MainMenu.Background,Is.TypeOf<NeonMenuBackground>());Assert.That(scene.Flow.MainMenu.ThemeId,Is.EqualTo("cosmic_nebula"));scene.Bootstrap.Inventory.TryEquip("paper");Assert.That(scene.Flow.MainMenu.Background,Is.TypeOf<NeonMenuBackground>());Assert.That(scene.Flow.MainMenu.ThemeId,Is.EqualTo("paper"));}}
        /// <summary>All simultaneously open previews share the separate 150-mote theme budget.</summary>
        [Test]public void ThemeBudgetIsGlobal()
        {using(var scene=new EconomyTestScene(withUi:true)){scene.Flow.OpenShop();scene.Flow.Shop.ThemesButton.onClick.Invoke();int count=Object.FindObjectsByType<ThemeBackdrop>(FindObjectsSortMode.None).Sum(t=>t.ParticleCount);Assert.That(count,Is.LessThanOrEqualTo(150));}}
    }
}
