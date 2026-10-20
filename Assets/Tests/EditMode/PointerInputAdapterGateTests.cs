using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;

namespace ArrowEscape.Tests
{
    /// <summary>Tests the pure UI predicate independently of hardware input.</summary>
    public class PointerInputAdapterGateTests
    {
        /// <summary>Absent blockers accept points; present blockers invert their result.</summary>
        [TestCase(0)][TestCase(1)][TestCase(2)] public void Gate(int mode)
        { var host = new GameObject(); try { var input = host.AddComponent<PointerInputAdapter>(); if (mode != 0) input.IsScreenPointBlocked = p => mode == 1; Assert.That(input.ShouldAcceptPointerDown(new Vector2(23, 45)), Is.EqualTo(mode != 1)); } finally { Object.DestroyImmediate(host); } }
    }
}
