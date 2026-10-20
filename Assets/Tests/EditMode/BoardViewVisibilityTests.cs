using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Checks that replacement boards never override flow's visibility choice.</summary>
    public class BoardViewVisibilityTests
    {
        /// <summary>Hidden show, clear, replacement show, then explicit visibility all preserve the flag.</summary>
        [Test] public void HiddenReplacement()
        {
            var host = new GameObject(); try { var view = host.AddComponent<BoardView>(); var board = new Board(2, 2); board.TryAddArrow(1, 0, 0, Direction.Up, out _); view.SetVisible(false); view.Show(board); Assert.That(view.IsVisible, Is.False); view.TryGetArrowView(1, out var arrow); Assert.That(arrow.gameObject.activeInHierarchy, Is.False); view.Clear(); view.Show(board); Assert.That(view.IsVisible, Is.False); view.SetVisible(true); view.TryGetArrowView(1, out arrow); Assert.That(arrow.gameObject.activeInHierarchy, Is.True); } finally { Object.DestroyImmediate(host); SpriteFactory.ClearCache(); }
        }
    }
}
