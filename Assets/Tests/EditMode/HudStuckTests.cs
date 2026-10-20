using System.Reflection;
using ArrowEscape.Core;
using ArrowEscape.UI;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Stuck warning events and actual rectangle layout stay inside reserved margins.</summary>
    public class HudStuckTests
    {
        /// <summary>Checks events and text with isolated ice-arrow state.</summary>
        [Test]public void EventsAndText()
        {
            using(var s=new ProgressTestScene(withUi:true))
            {
                Assert.That(s.Hud.Hud.StuckBanner.gameObject.activeSelf,Is.False);s.Bootstrap.TryStartSandboxLevel("TestLevels/ice_002");
                s.Tap(3,0);s.Tap(0,0);Assert.That(s.Hud.Hud.StuckBanner.gameObject.activeSelf,Is.True);
                Assert.That(s.Hud.Hud.StuckText.text,Is.EqualTo("No way out from here.\nUndo or Restart."));Assert.That(s.Hud.Hud.HintButton.interactable,Is.False);
                s.Game.Undo();Assert.That(s.Hud.Hud.StuckBanner.gameObject.activeSelf,Is.False);Assert.That(s.Hud.Hud.HintButton.interactable,Is.True);
                s.Tap(0,0);Assert.That(s.Game.IsStuck,Is.True);s.Bootstrap.RestartLevel();Assert.That(s.Hud.Hud.StuckBanner.gameObject.activeSelf,Is.False);
            }
        }
        [TestCase(1080,1920)][TestCase(1920,1080)][TestCase(1080,1080)]
        public void Layout(float width,float height)
        {
            var root=new GameObject("Safe rectangle",typeof(RectTransform));var owner=new GameObject("HUD");
            try
            {
                var safe=(RectTransform)root.transform;safe.sizeDelta=new Vector2(width,height);
                var hud=owner.AddComponent<HudView>();hud.Build(safe);hud.SetStuckVisible(true);
                typeof(HudView).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);Canvas.ForceUpdateCanvases();
                var corners=new Vector3[4];hud.StuckBanner.GetWorldCorners(corners);float low=corners[0].y,high=corners[1].y;
                var top=(RectTransform)safe.Find("TopBar");top.GetWorldCorners(corners);Assert.That(high,Is.LessThan(corners[0].y));
                var bottom=(RectTransform)safe.Find("BottomBar");bottom.GetWorldCorners(corners);Assert.That(low,Is.GreaterThan(corners[1].y));
                safe.GetWorldCorners(corners);Assert.That(high,Is.LessThanOrEqualTo(corners[1].y));Assert.That(low,Is.GreaterThanOrEqualTo(corners[0].y));
            }
            finally{Object.DestroyImmediate(owner);Object.DestroyImmediate(root);}
        }
    }
}
