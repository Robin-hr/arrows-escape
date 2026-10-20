using System.Collections.Generic;
using ArrowEscape.Core;
using ArrowEscape.UI;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks tile reuse, exact callbacks, and large scrollable catalogs.</summary>
    public class LevelSelectViewTests
    {
        private GameObject host; private LevelSelectView view; private int selected;
        /// <summary>Builds an overlay grid without an EventSystem or file access.</summary>
        [SetUp] public void Setup()
        {
            host = new GameObject("Select tests", typeof(RectTransform), typeof(Canvas)); host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            view = host.AddComponent<LevelSelectView>(); selected = -1; view.Build(host.transform, i => selected = i, () => { });
        }
        /// <summary>Removes all generated controls.</summary>
        [TearDown] public void Cleanup() => Object.DestroyImmediate(host);
        /// <summary>Locked tiles are dimmed and non-interactable; unlocked callbacks carry the index.</summary>
        [Test] public void TilesAndReuse()
        {
            string[] ids = { "a", "b", "c" }; var progress = new ProgressTracker(); view.Refresh(LevelSelectModel.Build(ids, progress)); var first = view.TileButtons[0];
            Assert.That(view.TileButtons.Count, Is.EqualTo(3)); Assert.That(view.TileButtons[1].interactable, Is.False); view.TileButtons[1].onClick.Invoke(); Assert.That(selected, Is.EqualTo(-1));
            first.onClick.Invoke(); Assert.That(selected, Is.Zero); progress.RecordCompletion("a", 0, 0); view.Refresh(LevelSelectModel.Build(ids, progress)); Assert.That(view.TileButtons[0], Is.SameAs(first)); Assert.That(first.GetComponentInChildren<UnityEngine.UI.Text>().text, Does.Contain("Flawless")); Assert.That(view.TileButtons[1].interactable, Is.True);
            view.TileButtons[1].onClick.Invoke(); Assert.That(selected, Is.EqualTo(1)); progress.RecordCompletion("a", 0, 1); view.Refresh(LevelSelectModel.Build(ids, progress)); Assert.That(first.GetComponentInChildren<UnityEngine.UI.Text>().text, Does.Contain("Flawless"));
        }
        /// <summary>Completed non-flawless tiles show their best mistake count.</summary>
        [Test] public void BestText() { var tracker = new ProgressTracker(); tracker.RecordCompletion("a", 3, 1); view.Refresh(LevelSelectModel.Build(new[] { "a" }, tracker)); Assert.That(view.TileButtons[0].GetComponentInChildren<UnityEngine.UI.Text>().text, Does.Contain("3 mistakes")); }
        /// <summary>Hundreds or a thousand levels grow vertical content and reveal the resume row.</summary>
        [TestCase(200)][TestCase(1000)] public void LargeCatalog(int count)
        {
            var ids = new List<string>(); var tracker = new ProgressTracker();
            for (int i = 0; i < count; i++) { string id = "level_" + i; ids.Add(id); if (i < count - 1) tracker.RecordCompletion(id, 0, 0); }
            view.Refresh(LevelSelectModel.Build(ids, tracker)); Canvas.ForceUpdateCanvases(); Assert.That(view.TileButtons.Count, Is.EqualTo(count)); Assert.That(view.Scroll.content.rect.height, Is.GreaterThan(view.Scroll.viewport.rect.height));
            var grid = view.Scroll.content.GetComponent<UnityEngine.UI.GridLayoutGroup>(); Assert.That(grid.cellSize.x, Is.GreaterThanOrEqualTo(200)); Assert.That(view.Scroll.vertical && !view.Scroll.horizontal, Is.True); Assert.That(view.Scroll.verticalNormalizedPosition, Is.LessThan(0.05f));
        }
    }
}
