using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks exact restoration and hint/highlight/flash composition through Apply.</summary>
    public class ArrowViewHintLayerTests
    {
        private GameObject host; private ArrowView view; private Vector3 position, scale; private Color color; private Quaternion rotation;
        /// <summary>Creates a real board arrow.</summary>
        [SetUp] public void Setup() { host = new GameObject(); var boardView = host.AddComponent<BoardView>(); var b = new Board(3, 3); b.TryAddArrow(1, 2, 1, Direction.Right, out _); boardView.Show(b); boardView.TryGetArrowView(1, out view); position = view.transform.position; scale = view.transform.localScale; color = view.SpriteRenderer.color; rotation = view.transform.rotation; }
        /// <summary>Removes created resources.</summary>
        [TearDown] public void Cleanup() { Object.DestroyImmediate(host); SpriteFactory.ClearCache(); }
        /// <summary>Hint on/off restores exact base values.</summary>
        [Test] public void Restore() { view.SetHint(ViewTheme.HintTint, 1.18f); Assert.That(view.HasHint, Is.True); view.ClearHint(); Assert.That(view.HasHint, Is.False); AssertBase(); }
        /// <summary>Tint precedence is flash, highlight, hint, normal.</summary>
        [Test] public void Precedence() { view.SetHint(ViewTheme.HintTint, 1.18f); view.SetHighlight(true); view.SetFlash(ViewTheme.BlockerTint, 1.15f); Assert.That(view.SpriteRenderer.color, Is.EqualTo(ViewTheme.BlockerTint)); view.ClearFlash(); Assert.That(view.SpriteRenderer.color, Is.EqualTo(ViewTheme.HighlightColor)); view.SetHighlight(false); Assert.That(view.SpriteRenderer.color, Is.EqualTo(ViewTheme.HintTint)); view.ClearHint(); AssertBase(); }
        /// <summary>All active scale multipliers combine.</summary>
        [Test] public void ScaleProduct() { view.SetHint(ViewTheme.HintTint, 1.18f); view.SetHighlight(true); view.SetFlash(ViewTheme.BlockerTint, 1.15f); Assert.That(Vector3.Distance(view.transform.localScale, scale * (1.18f * ViewTheme.HighlightScaleMultiplier * 1.15f)), Is.LessThan(0.000001)); }
        /// <summary>Repeated setting never accumulates scale drift.</summary>
        [Test] public void NoDrift() { for (int i = 0; i < 100; i++) { view.SetHint(ViewTheme.HintTint, 1.18f); view.ClearHint(); } AssertBase(); }
        /// <summary>ResetVisual clears the added layer.</summary>
        [Test] public void Reset() { view.SetHint(ViewTheme.HintTint, 1.18f); view.SetHighlight(true); view.SetOffset(Vector2.one); view.ResetVisual(); Assert.That(view.HasHint, Is.False); AssertBase(); }
        /// <summary>Hints never move or rotate an arrow.</summary>
        [Test] public void NoMotion() { view.SetHint(ViewTheme.HintTint, 1.18f); Assert.That(view.transform.position, Is.EqualTo(position)); Assert.That(view.transform.rotation, Is.EqualTo(rotation)); }
        private void AssertBase() { Assert.That(view.transform.position, Is.EqualTo(position)); Assert.That(view.transform.rotation, Is.EqualTo(rotation)); Assert.That(view.transform.localScale, Is.EqualTo(scale)); Assert.That(view.SpriteRenderer.color, Is.EqualTo(color)); }
    }
}
