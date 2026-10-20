using ArrowEscape.Core;
using ArrowEscape.UI;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks menu columns against portrait, landscape, and square usable rectangles.</summary>
    public class MenuLayoutTests
    {
        /// <summary>Every responsive column remains inside its safe-area bounds after resizing.</summary>
        [TestCase(1080, 1920)][TestCase(1920, 1080)][TestCase(1080, 1080)]
        public void Fit(float width, float height)
        {
            using (var scene = new FlowTestScene())
            {
                var fits = scene.Flow.GetComponentsInChildren<MenuFit>(true);
                // MenuFit components live on descendants of each independently activated screen.
                Assert.That(fits.Length, Is.GreaterThanOrEqualTo(5));
                foreach (var fit in fits)
                {
                    var column = (RectTransform)fit.transform; var safe = (RectTransform)column.parent;
                    safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f); safe.sizeDelta = new Vector2(width, height);
                    if (column.name == "Header") continue;
                    fit.Refit(); float scale = column.localScale.x;
                    Assert.That(column.rect.width * scale, Is.LessThanOrEqualTo(width + 0.01f));
                    Assert.That(column.rect.height * scale, Is.LessThanOrEqualTo(height + 0.01f));
                    Assert.That(scale, Is.GreaterThan(0).And.LessThanOrEqualTo(1));
                }
                scene.Flow.MainMenu.PlayButton.onClick.Invoke();
                scene.Hud.Hud.PauseButton.onClick.Invoke();
                Canvas.ForceUpdateCanvases();
                Assert.That(scene.Hud.IsVisible && scene.Flow.PauseScreen.Root.gameObject.activeSelf, Is.True);
                Assert.That(scene.Flow.MenusCanvas.sortingOrder, Is.GreaterThan(scene.Hud.GetComponentInChildren<Canvas>(true).sortingOrder));
            }
        }
    }
}
