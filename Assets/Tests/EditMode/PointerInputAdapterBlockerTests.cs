using System;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Verifies independent screen and HUD pointer gates compose without replacing each other.</summary>
    public class PointerInputAdapterBlockerTests
    {
        /// <summary>Registration, removal, duplicate/null handling, and the legacy property compose.</summary>
        [Test] public void Composition()
        {
            var host = new GameObject(); try {
                var a = host.AddComponent<PointerInputAdapter>(); Func<Vector2, bool> left = p => p.x < 0, right = p => p.x > 0;
                Assert.That(a.ShouldAcceptPointerDown(Vector2.zero), Is.True); a.AddBlocker(null); a.RemoveBlocker(null); a.AddBlocker(left); a.AddBlocker(right); a.AddBlocker(right);
                Assert.That(a.ShouldAcceptPointerDown(Vector2.left), Is.False); Assert.That(a.ShouldAcceptPointerDown(Vector2.right), Is.False);
                a.RemoveBlocker(left); Assert.That(a.ShouldAcceptPointerDown(Vector2.left), Is.True); Assert.That(a.ShouldAcceptPointerDown(Vector2.right), Is.False);
                a.IsScreenPointBlocked = p => p.y > 0; Assert.That(a.ShouldAcceptPointerDown(Vector2.up), Is.False); a.RemoveBlocker(right); Assert.That(a.ShouldAcceptPointerDown(Vector2.right), Is.True); Assert.That(a.ShouldAcceptPointerDown(Vector2.up), Is.False);
            } finally { UnityEngine.Object.DestroyImmediate(host); }
        }
    }
}
