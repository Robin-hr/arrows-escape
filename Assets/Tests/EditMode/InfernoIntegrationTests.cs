using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Inferno remains a cosmetic observer of sound, undo, shop and pause flows.</summary>
    public sealed class InfernoIntegrationTests
    {
        /// <summary>One flavor under each unchanged note; disabled sound emits neither.</summary>
        [Test] public void MelodyAndMuteStayUnchanged()
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.Inventory.Shop.AddCoins(150);scene.Bootstrap.Inventory.TryBuy("inferno_streak");scene.Bootstrap.Inventory.TryEquip("inferno_streak");var sounds=new RecordingSoundPlayer();scene.Game.gameObject.AddComponent<AudioDirector>().Initialize(scene.Game,scene.Bootstrap.Settings.Model,sounds);
                var model=new Board(4,4);for(int i=0;i<4;i++)model.TryAddArrow(i+1,0,i,Direction.Right,out _);scene.Game.StartLevel(model);sounds.Events.Clear();scene.Tap(0,0);scene.Tap(0,1);
                Assert.That(sounds.Events.Where(e=>e.Cue==SoundCue.Exit).Select(e=>e.ScaleStep),Is.EqualTo(new[]{0,1}));Assert.That(sounds.Events.Count(e=>e.Cue==SoundCue.SkinExit),Is.EqualTo(2));scene.Bootstrap.Settings.Model.SoundEnabled=false;sounds.Events.Clear();scene.Tap(0,2);Assert.That(sounds.Events,Is.Empty);
            }
        }
        /// <summary>The real undo animator returns the fire to its exact cells and idle temperature.</summary>
        [Test] public void PathUndoReturnsToRest()
        {
            using(var scene=new EconomyTestScene())
            {
                var model=new Board(6,4);model.TryAddArrow(new Arrow(1,new[]{new Cell(0,0),new Cell(1,0),new Cell(1,1),new Cell(2,1)},Direction.Right),out _);model.TryAddArrow(2,5,3,Direction.Up,out _);scene.Game.StartLevel(model);scene.Board.SetArrowSkin("inferno_streak");scene.Tap(2,1);var animator=scene.Game.GetComponent<ArrowAnimator>();animator.Tick(.1f);scene.Game.Undo();scene.Board.TryGetArrowView(1,out var restored);Assert.That(restored.Skin.Inferno.IsFlying,Is.True);animator.Tick(2);Assert.That(restored.Path.Distance,Is.Zero);Assert.That(restored.Skin.Inferno.Temperature,Is.Zero);Assert.That(restored.Skin.Inferno.IsFlying,Is.False);Assert.That(restored.BaseWorldPosition,(Is.EqualTo((Vector2)scene.Board.CellToWorld(2,1))));
            }
        }
        /// <summary>Automatic fire time stops with pause and hiding; explicit ticks remain testable.</summary>
        [Test] public void PauseAndHiddenBoard()
        {using(var scene=new EconomyTestScene()){scene.Bootstrap.ShowLevel(0);scene.Board.SetArrowSkin("inferno_streak");var director=scene.Game.GetComponent<SkinDirector>();director.Tick(.1f);float before=director.Clock;scene.Game.SetPaused(true);director.AdvanceFrame(.3f);Assert.That(director.Clock,Is.EqualTo(before));director.Tick(.1f);Assert.That(director.Clock,Is.GreaterThan(before));scene.Board.SetVisible(false);director.AdvanceFrame(1);Assert.That(director.LiveParticles,Is.Zero);}}
        /// <summary>Only Inferno uses a live straight card; its detail is a live dark bent board.</summary>
        [Test] public void ShopIsProceduralWithoutLayoutChanges()
        {using(var scene=new EconomyTestScene(withUi:true)){scene.Flow.OpenShop();var card=scene.Flow.Shop.Cards.First(x=>x.Item.Id=="inferno_streak");Assert.That(card.Preview.texture,Is.TypeOf<RenderTexture>());var frost=scene.Flow.Shop.Cards.First(x=>x.Item.Id=="frostbite");Assert.That(frost.Preview.texture,Is.TypeOf<RenderTexture>());scene.Flow.Shop.OpenDetail(2);Assert.That(scene.Flow.Shop.Detail.HasLivePreview,Is.True);Assert.That(scene.Flow.Shop.Detail.Preview.texture,Is.TypeOf<RenderTexture>());scene.Flow.Shop.Close();Assert.That(card.Preview.texture,Is.Null);}}
        /// <summary>HUD labels and all move-budget states remain readable on Inferno's dark board.</summary>
        [Test] public void HudContrastRestoresWithClassic()
        {using(var scene=new EconomyTestScene(withUi:true)){scene.Bootstrap.ShowLevel(0);scene.Board.SetArrowSkin("inferno_streak");Assert.That(scene.Hud.Hud.LevelText.color,Is.EqualTo(Color.white));scene.Hud.Hud.SetStarState(9,8);Assert.That(scene.Hud.Hud.MovesText.color.g,Is.GreaterThan(.7f));scene.Board.SetArrowSkin("classic");Assert.That(scene.Hud.Hud.LevelText.color,Is.EqualTo(ViewTheme.HudText));}}
    }
}
