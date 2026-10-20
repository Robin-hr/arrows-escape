using ArrowEscape.Core;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14B labels, economy header, and preserved menu navigation.</summary>
    public sealed class MenuTests
    {
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void FirstLaunchAndAllButtons()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                var menu=scene.Flow.MainMenu;Assert.That(menu.PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text,Is.EqualTo("Play"));Assert.That(menu.StarsText.text,Is.EqualTo("0 / 9"));Assert.That(menu.CoinsText.text,Is.EqualTo("0"));
                menu.CoinsButton.onClick.Invoke();Assert.That(scene.Flow.Shop.IsVisible,Is.True);scene.Flow.HandleBack();
                menu.LevelsButton.onClick.Invoke();Assert.That(scene.Flow.Machine.Current,Is.EqualTo(ScreenState.LevelSelect));scene.Flow.HandleBack();
                menu.SettingsButton.onClick.Invoke();Assert.That(scene.Flow.Machine.Current,Is.EqualTo(ScreenState.Settings));scene.Flow.HandleBack();
                menu.PlayButton.onClick.Invoke();Assert.That(scene.Bootstrap.CurrentLevelIndex,Is.Zero);Assert.That(scene.Flow.Machine.Current,Is.EqualTo(ScreenState.Playing));
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void ContinueStarsAndCoinsAfterClear()
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                scene.Flow.MainMenu.PlayButton.onClick.Invoke();scene.Clear();scene.Hud.CompletePanel.LevelsButton.onClick.Invoke();scene.Flow.Levels.BackButton.onClick.Invoke();var menu=scene.Flow.MainMenu;
                Assert.That(menu.PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text,Is.EqualTo("Continue - Level 2"));Assert.That(menu.StarsText.text,Is.EqualTo("3 / 9"));Assert.That(menu.CoinsText.text,Is.EqualTo("40"));menu.PlayButton.onClick.Invoke();Assert.That(scene.Bootstrap.CurrentLevelIndex,Is.EqualTo(1));
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void RestartAppKeepsEquipmentAndCoins()
        {
            var inventory=new FakeInventoryStore();
            using(var scene=new EconomyTestScene(inventory:inventory)){scene.Bootstrap.Inventory.Shop.AddCoins(500);scene.Bootstrap.Inventory.TryBuy("plasma_bolt");scene.Bootstrap.Inventory.TryEquip("plasma_bolt");}
            using(var reopened=new EconomyTestScene(withUi:true,inventory:inventory)){Assert.That(reopened.Bootstrap.Inventory.State.Coins,Is.EqualTo(350));Assert.That(reopened.Bootstrap.Inventory.State.EquippedArrowId,Is.EqualTo("plasma_bolt"));Assert.That(reopened.Flow.MainMenu.CoinsText.text,Is.EqualTo("350"));}
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [TestCase(1080,1920)] [TestCase(1920,1080)] [TestCase(1080,1080)]
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        public void HeaderDoesNotOverlapContent(float width,float height)
        {
            using(var scene=new EconomyTestScene(withUi:true))
            {
                var menu=scene.Flow.MainMenu;var safe=(RectTransform)menu.Root.Find("SafeArea");safe.anchorMin=safe.anchorMax=new Vector2(.5f,.5f);safe.sizeDelta=new Vector2(width,height);
                Canvas.ForceUpdateCanvases();foreach(var fit in menu.Root.GetComponentsInChildren<ArrowEscape.UI.MenuFit>(true))fit.Refit();menu.Tick(1);
                var top=new Vector3[4];var title=new Vector3[4];((RectTransform)menu.CoinsButton.transform).GetWorldCorners(top);
                ((RectTransform)menu.Root.Find("SafeArea/MenuContent/Column/Title")).GetWorldCorners(title);
                Assert.That(title[2].y,Is.LessThan(top[0].y));
            }
        }
        /// <summary>Isolated Step 14B state or regression check; never accesses player saves.</summary>
        [Test] public void FontIsBundledAndButtonWidthLimited()
        {using(var scene=new EconomyTestScene(withUi:true)){Assert.That(ArrowEscape.UI.UiStyle.Font.name,Does.Contain("Fredoka"));Assert.That(((RectTransform)scene.Flow.MainMenu.PlayButton.transform).rect.width,Is.LessThanOrEqualTo(520));Assert.That(scene.Flow.MainMenu.GetComponentsInChildren<ArrowEscape.UI.PathPreviewGraphic>(true).Length,Is.GreaterThanOrEqualTo(5));foreach(var graphic in scene.Flow.MainMenu.GetComponentsInChildren<ArrowEscape.UI.PathPreviewGraphic>(true))Assert.That(graphic.GetComponent<CanvasRenderer>(),Is.Not.Null);}}
    }
}
