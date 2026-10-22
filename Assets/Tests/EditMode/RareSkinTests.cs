using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Rare procedural recipes reuse exact state layers, bounded signatures and the signature melody.</summary>
    public sealed class RareSkinTests
    {
        private static readonly string[] ids={"obsidian_shard","chromium_spike","celestial_quill","emerald_viper","crimson_relic"};
        /// <summary>Every rebuilt recipe is procedural and has a name-defining promise and a dark palette.</summary>
        [TestCaseSource(nameof(ids))]public void Registry(string id){var skin=ArrowSkinRegistry.Find(id);Assert.That(skin.IsRare&&skin.IsProcedural&&skin.PrefersDarkBoard,Is.True);Assert.That(skin.Signature,Is.Not.Empty);Assert.That(ArrowSkinResources.Load().Find(id),Is.Null);}
        /// <summary>One hundred state cycles restore the exact transform and layer state.</summary>
        [TestCaseSource(nameof(ids))]public void StateAndSignature(string id)
        {
            var host=new GameObject("Rare state fixture");try
            {
                var preview=host.AddComponent<LiveSkinPreview>();preview.Initialize(null,Vector2.zero);preview.AutoReplay=false;preview.SetDark(true);preview.ShowStraight(id);preview.Tick(.01f);var view=preview.View;var position=view.transform.position;var scale=view.transform.localScale;var color=view.BaseColor;
                for(int i=0;i<100;i++){view.SetHighlight(true);view.SetHint(Color.green,1.18f);view.SetFlash(Color.red,1.15f);view.ClearFlash();view.ClearHint();view.SetHighlight(false);}Assert.That(view.transform.position,Is.EqualTo(position));Assert.That(view.transform.localScale,Is.EqualTo(scale));Assert.That(view.BaseColor,Is.EqualTo(color));
                int starts=preview.Signatures.Starts;preview.Fly();Assert.That(preview.Signatures.Starts,Is.EqualTo(starts+1));for(int i=0;i<90;i++)preview.Tick(.01f);Assert.That(preview.Signatures.HasActive,Is.False);Assert.That(view.Skin.Rare.IsFlying,Is.False);
            }finally{Object.DestroyImmediate(host);}
        }
        /// <summary>Flavor audio is exactly once beneath the unchanged first scale step.</summary>
        [TestCaseSource(nameof(ids))]public void SoundAndInstantMove(string id)
        {
            using(var scene=new EconomyTestScene())
            {
                var sounds=new RecordingSoundPlayer();var audio=scene.Game.gameObject.AddComponent<AudioDirector>();audio.Initialize(scene.Game,scene.Bootstrap.Settings.Model,sounds);scene.Bootstrap.Inventory.Shop.AddCoins(1000);scene.Bootstrap.Inventory.TryBuy(id);scene.Bootstrap.Inventory.TryEquip(id);
                var board=new Board(10,3);for(int y=0;y<3;y++)board.TryAddArrow(new Arrow(y+1,new[]{new Cell(0,y),new Cell(1,y),new Cell(2,y),new Cell(3,y)},Direction.Right),out _);scene.Game.StartLevel(board);sounds.Events.Clear();scene.Tap(3,0);Assert.That(board.Arrows.Count,Is.EqualTo(2));Assert.That(sounds.Events.Count(e=>e.Cue==SoundCue.SkinExit),Is.EqualTo(1));Assert.That(sounds.Events.Single(e=>e.Cue==SoundCue.Exit).ScaleStep,Is.Zero);scene.Tap(3,1);Assert.That(board.Arrows.Count,Is.EqualTo(1));Assert.That(scene.Game.MoveCount,Is.EqualTo(2));scene.Bootstrap.Settings.Model.SoundEnabled=false;sounds.Events.Clear();scene.Game.Undo();scene.Tap(3,1);Assert.That(sounds.Events,Is.Empty);
                var director=scene.Game.GetComponent<SkinDirector>();Assert.That(director.LiveParticles,Is.LessThanOrEqualTo(600));director.SetLodOverride(true);Assert.That(director.IsLod,Is.True);scene.Game.RestartLevel();Assert.That(director.Signatures.HasActive,Is.False);
            }
        }
        /// <summary>HUD removes the old authoring prefix without truncating a valid shape name.</summary>
        [Test]public void FullShapeTitle(){Assert.That(ArrowEscape.UI.HudView.DisplayName("Shape 17: Trapezium"),Is.EqualTo("Trapezium"));Assert.That(ArrowEscape.UI.HudView.DisplayName("Trapezium"),Is.EqualTo("Trapezium"));Assert.That(ArrowEscape.UI.HudView.DisplayName("Level 1"),Is.EqualTo("Level 1"));}
    }
}
