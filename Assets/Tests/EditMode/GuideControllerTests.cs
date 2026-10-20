using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Drives line fading and independent hint pulses without real frames.</summary>
    public class GuideControllerTests
    {
        private GameObject host; private BoardView boardView; private GuideController guide; private Board board;
        private Arrow tapped, blocker, hint;
        /// <summary>Creates a board with a vertical blocker and two independently free arrows.</summary>
        [SetUp] public void Setup()
        {
            host = new GameObject("Guide tests"); boardView = host.AddComponent<BoardView>(); guide = host.AddComponent<GuideController>();
            board = new Board(5, 5); board.TryAddArrow(1, 2, 0, Direction.Up, out tapped); board.TryAddArrow(2, 2, 3, Direction.Right, out blocker); board.TryAddArrow(3, 4, 4, Direction.Up, out hint); board.TryAddArrow(4, 0, 4, Direction.Left, out _);
            boardView.Show(board); guide.Bind(boardView);
        }
        /// <summary>Removes view, material, and cache resources.</summary>
        [TearDown] public void Cleanup() { Object.DestroyImmediate(host); SpriteFactory.ClearCache(); }
        private SpriteRenderer Line => host.transform.Find("Blocker Guide Line").GetComponent<SpriteRenderer>();
        private SpriteRenderer Marker => host.transform.Find("Blocker Guide Marker").GetComponent<SpriteRenderer>();
        /// <summary>Endpoints, marker, orientation, thickness, and order match the inset world geometry.</summary>
        [Test] public void LineGeometry()
        {
            guide.ShowBlockerLine(tapped, blocker); Assert.That(guide.HasActiveLine, Is.True);
            Assert.That(Vector2.Distance(guide.LineStartWorld, new Vector2(0, -1.65f)), Is.LessThan(0.00001)); Assert.That(Vector2.Distance(guide.LineEndWorld, new Vector2(0, 0.65f)), Is.LessThan(0.00001));
            Assert.That((Vector2)Marker.transform.position, Is.EqualTo(guide.LineEndWorld)); Assert.That(Marker.transform.localScale.x, Is.EqualTo(0.3f));
            Assert.That(Line.transform.localScale.x, Is.EqualTo(2.3f).Within(0.00001)); Assert.That(Line.transform.localScale.y, Is.EqualTo(0.08f)); Assert.That(Mathf.DeltaAngle(Line.transform.eulerAngles.z, 90), Is.Zero.Within(0.00001));
            Assert.That(Line.sortingOrder, Is.GreaterThan(ViewTheme.ArrowSortingOrder).And.LessThan(ViewTheme.SlidingSortingOrder)); Assert.That(Line.sharedMaterial.shader.name, Is.EqualTo("Sprites/Default")); Assert.That(Line.sharedMaterial.mainTexture, Is.SameAs(Line.sprite.texture)); Assert.That(Marker.sharedMaterial.mainTexture, Is.SameAs(Marker.sprite.texture));
        }
        /// <summary>Fades only after the specified start and hides at one second.</summary>
        [Test] public void Fade()
        {
            guide.ShowBlockerLine(tapped, blocker); guide.Tick(0.6f); Assert.That(Line.color.a, Is.EqualTo(1)); guide.Tick(0.2f); Assert.That(Line.color.a, Is.GreaterThan(0).And.LessThan(1)); Assert.That(Marker.color.a, Is.EqualTo(Line.color.a));
            guide.Tick(0.2f); Assert.That(guide.HasActiveLine, Is.False); Assert.That(Line.gameObject.activeSelf || Marker.gameObject.activeSelf, Is.False);
        }
        /// <summary>Repeated connectors reuse exactly the same two objects and reset fading.</summary>
        [Test] public void Reuse()
        {
            guide.ShowBlockerLine(tapped, blocker); var first = Line; var end = Marker; int children = host.transform.childCount; guide.Tick(0.8f);
            for (int i = 0; i < 100; i++) guide.ShowBlockerLine(new Arrow(9, 0, 1, Direction.Right), new Arrow(10, 3, 1, Direction.Up));
            Assert.That(host.transform.childCount, Is.EqualTo(children)); Assert.That(Line, Is.SameAs(first)); Assert.That(Marker, Is.SameAs(end)); Assert.That(Line.color.a, Is.EqualTo(1)); Assert.That(Line.transform.rotation, Is.EqualTo(Quaternion.identity));
        }
        /// <summary>Invalid geometry hides an existing line safely.</summary>
        [Test] public void Invalid() { guide.ShowBlockerLine(tapped, blocker); Assert.DoesNotThrow(() => guide.ShowBlockerLine(blocker, tapped)); Assert.That(guide.HasActiveLine, Is.False); }
        /// <summary>Three pulses rise above the original scale and restore it exactly at completion.</summary>
        [Test] public void HintPulses()
        {
            boardView.TryGetArrowView(hint.Id, out var view); Vector3 scale = view.transform.localScale; Color color = view.SpriteRenderer.color; guide.ShowHint(hint);
            for (int i = 0; i < 3; i++) { guide.Tick(0.32f); Assert.That(view.transform.localScale.x / scale.x, Is.GreaterThan(1)); Assert.That(view.SpriteRenderer.color, Is.EqualTo(ViewTheme.HintTint)); guide.Tick(0.48f); Assert.That(Vector3.Distance(view.transform.localScale, scale), Is.LessThan(0.000001)); }
            Assert.That(guide.HasActiveHint || view.HasHint, Is.False); Assert.That(guide.HintArrowId, Is.EqualTo(-1)); Assert.That(view.SpriteRenderer.color, Is.EqualTo(color));
        }
        /// <summary>New hints clear old arrows and restart the same arrow without stacking.</summary>
        [Test] public void ReplaceHint()
        {
            boardView.TryGetArrowView(hint.Id, out var first); boardView.TryGetArrowView(4, out var second); Vector3 scale = first.transform.localScale;
            guide.ShowHint(hint); guide.Tick(0.32f); guide.ShowHint(board.GetArrowById(4)); Assert.That(first.HasHint, Is.False); Assert.That(first.transform.localScale, Is.EqualTo(scale)); Assert.That(second.HasHint, Is.True);
            guide.Tick(0.32f); guide.ShowHint(board.GetArrowById(4)); Assert.That(second.transform.localScale, Is.EqualTo(scale)); Assert.That(guide.HintArrowId, Is.EqualTo(4));
        }
        /// <summary>Clearing one guide never clears the other.</summary>
        [Test] public void Independent()
        { guide.ShowHint(hint); guide.ShowBlockerLine(tapped, blocker); guide.ClearLine(); Assert.That(guide.HasActiveHint, Is.True); guide.ShowBlockerLine(tapped, blocker); guide.ClearHint(); Assert.That(guide.HasActiveLine, Is.True); guide.ShowHint(hint); guide.ClearAll(); Assert.That(guide.HasActiveLine || guide.HasActiveHint, Is.False); }
        /// <summary>Destroyed hinted views are skipped without exceptions.</summary>
        [Test] public void Destroyed() { guide.ShowHint(hint); boardView.TryGetArrowView(hint.Id, out var view); Object.DestroyImmediate(view.gameObject); Assert.DoesNotThrow(() => guide.Tick(0.1f)); Assert.That(guide.HasActiveHint, Is.False); }
        /// <summary>A missing view does not create a hint.</summary>
        [Test] public void MissingView() { guide.ShowHint(new Arrow(999, 0, 0, Direction.Up)); Assert.That(guide.HasActiveHint, Is.False); }
        /// <summary>Automatic-frame pause freezes both guides; explicit Tick still progresses them.</summary>
        [Test] public void Pause()
        {
            guide.ShowHint(hint); guide.ShowBlockerLine(tapped, blocker); guide.SetPaused(true); boardView.TryGetArrowView(hint.Id, out var view); Vector3 scale = view.transform.localScale;
            guide.AdvanceFrame(0.8f); Assert.That(Line.color.a, Is.EqualTo(1)); Assert.That(view.transform.localScale, Is.EqualTo(scale)); guide.ShowHint(board.GetArrowById(4)); Assert.That(guide.HintArrowId, Is.EqualTo(hint.Id));
            guide.Tick(0.8f); Assert.That(Line.color.a, Is.LessThan(1)); guide.Tick(0.32f); Assert.That(view.transform.localScale.x, Is.GreaterThan(scale.x)); guide.SetPaused(false); guide.AdvanceFrame(10); Assert.That(guide.HasActiveLine || guide.HasActiveHint, Is.False);
        }
        /// <summary>Board replacement clears guides while keeping their reusable objects outside its root.</summary>
        [Test] public void BoardChange()
        { guide.ShowHint(hint); guide.ShowBlockerLine(tapped, blocker); var line = Line; boardView.Show(board.Clone()); Assert.That(guide.HasActiveHint || guide.HasActiveLine, Is.False); Assert.That(line != null, Is.True); Assert.That(line.transform.parent, Is.EqualTo(guide.transform)); }
    }
}
