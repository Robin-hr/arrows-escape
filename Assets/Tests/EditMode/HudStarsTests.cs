using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Step 14A regression coverage with isolated state and no player-save access.</summary>
    public sealed class HudStarsTests
    {
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void BoundariesAndReset()
        {
            using(var scene=new ProgressTestScene(withUi:true))
            {
                scene.Bootstrap.ShowLevel(0);var hud=scene.Hud.Hud;
                int[] moves={15,16,20,21};Color[] colors={ViewTheme.HudText,ViewTheme.MovesAmber,ViewTheme.MovesAmber,ViewTheme.MovesRed};
                for(int i=0;i<moves.Length;i++)
                {
                    hud.SetState("Stars",moves[i],15,0,false);hud.SetStarState(moves[i],15);
                    Assert.That(hud.MovesText.text,Is.EqualTo("Moves "+moves[i]+" / 15"));Assert.That(hud.MovesText.color,Is.EqualTo(colors[i]));
                    Assert.That(hud.Stars.Count,Is.EqualTo(StarRating.StarsIfClearedNow(moves[i],15)));
                    hud.Tick(1);Assert.That(hud.Stars.Slot(2).rectTransform.anchoredPosition.y,Is.Zero);
                }
                scene.Game.RestartLevel();Assert.That(hud.Stars.Count,Is.EqualTo(3));Assert.That(hud.MovesText.color,Is.EqualTo(ViewTheme.HudText));
            }
        }
        /// <summary>Verifies star rules, presentation or persistence at their observable boundary.</summary>
        [Test] public void MeterSharesRealAcceptedTapCount()
        {
            using(var scene=new ProgressTestScene(withUi:true))
            {
                scene.Bootstrap.ShowLevel(0);for(int i=0;i<9;i++)scene.Tap(0,0);
                Assert.That(scene.Hud.Hud.MovesText.text,Is.EqualTo("Moves 9 / 8"));Assert.That(scene.Hud.Hud.Stars.Count,Is.EqualTo(2));
                scene.ExitOne();scene.Game.Undo();Assert.That(scene.Hud.Hud.MovesText.text,Is.EqualTo("Moves 10 / 8"));
            }
        }
    }
}
