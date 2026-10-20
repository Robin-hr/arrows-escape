using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks a single bounded background pulse and exact cleanup.</summary>
    public class CameraPulsePlayerTests
    {
        /// <summary>Brightness is bounded, restart does not drift, and clear restores exactly.</summary>
        [Test]public void BoundedAndRestored()
        {using(var s=new EffectsTestScene()){var p=new CameraPulsePlayer();p.Start();Assert.That(s.Camera.backgroundColor,Is.EqualTo(ViewTheme.Background));p.Tick(.24f);Assert.That(s.Camera.backgroundColor,Is.EqualTo(Color.Lerp(ViewTheme.Background,ViewTheme.CameraPulseColor,ViewTheme.CameraPulseStrength)));p.Start();Assert.That(s.Camera.backgroundColor,Is.EqualTo(ViewTheme.Background));p.Tick(.1f);p.ClearAll();Assert.That(s.Camera.backgroundColor,Is.EqualTo(ViewTheme.Background));p.Start();p.Tick(1);Assert.That(s.Camera.backgroundColor,Is.EqualTo(ViewTheme.Background));Assert.That(p.IsActive,Is.False);}}
        /// <summary>A missing or destroyed main camera never causes an exception.</summary>
        [Test]public void MissingAndDestroyed()
        {using(var s=new EffectsTestScene()){s.Camera.tag="Untagged";var p=new CameraPulsePlayer();Assert.DoesNotThrow(p.Start);Assert.That(p.IsActive,Is.False);s.Camera.tag="MainCamera";p.Start();Object.DestroyImmediate(s.Camera.gameObject);Assert.DoesNotThrow(()=>p.Tick(.2f));Assert.DoesNotThrow(p.ClearAll);Assert.That(p.IsActive,Is.False);}}
    }
}
