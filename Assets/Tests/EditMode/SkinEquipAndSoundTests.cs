using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Immediate equipment and quiet texture cues preserve the musical game contract.</summary>
    public sealed class SkinEquipAndSoundTests
    {
        /// <summary>Purchase/equip/undo use the same board and every Normal view gets the recipe.</summary>
        [Test] public void EquipAllNormalAndRestoreClassic()
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.Inventory.Shop.AddCoins(1000);scene.Bootstrap.Inventory.TryBuy("plasma_bolt");scene.Bootstrap.ShowLevel(0);scene.Bootstrap.Inventory.TryEquip("plasma_bolt");
                foreach(var view in scene.Board.GetComponentsInChildren<ArrowView>())Assert.That(view.Skin.Skin.Id,Is.EqualTo("plasma_bolt"));scene.Tap(3,0);scene.Game.Undo();scene.Board.TryGetArrowView(4,out var restored);Assert.That(restored.Skin.Skin.Id,Is.EqualTo("plasma_bolt"));
                scene.Bootstrap.Inventory.TryEquip("classic");foreach(var view in scene.Board.GetComponentsInChildren<ArrowView>())Assert.That(view.Skin.Skin,Is.Null);
            }
        }
        /// <summary>Texture plays once per exit underneath unchanged note steps and stops with sound off.</summary>
        [Test] public void ExitTextureDoesNotAdvanceMelody()
        {
            using(var scene=new EconomyTestScene())
            {
                var sounds=new RecordingSoundPlayer();var audio=scene.Game.gameObject.AddComponent<AudioDirector>();audio.Initialize(scene.Game,scene.Bootstrap.Settings.Model,sounds);scene.Bootstrap.Inventory.Shop.AddCoins(150);scene.Bootstrap.Inventory.TryBuy("frostbite");scene.Bootstrap.Inventory.TryEquip("frostbite");
                var b=new Board(3,4);for(int y=0;y<3;y++)b.TryAddArrow(y+1,0,y,Direction.Right,out _);scene.Game.StartLevel(b);sounds.Events.Clear();
                for(int y=0;y<2;y++){Vector2 point=scene.Board.CellToWorld(0,y);scene.Game.GetComponent<TapController>().PointerDown(1,point);scene.Game.GetComponent<TapController>().PointerUp(1,point);}
                Assert.That(sounds.Events.Where(e=>e.Cue==SoundCue.Exit).Select(e=>e.ScaleStep),Is.EqualTo(new[]{0,1}));Assert.That(sounds.Events.Count(e=>e.Cue==SoundCue.SkinExit),Is.EqualTo(2));
                scene.Bootstrap.Settings.Model.SoundEnabled=false;sounds.Events.Clear();Vector2 last=scene.Board.CellToWorld(0,2);scene.Game.GetComponent<TapController>().PointerDown(1,last);scene.Game.GetComponent<TapController>().PointerUp(1,last);Assert.That(sounds.Events,Is.Empty);
            }
        }
        /// <summary>Blocked texture is additive to one thud and does not consume a scale step.</summary>
        [Test] public void BlockedTextureThenFirstExitNote()
        {
            using(var scene=new EconomyTestScene())
            {
                var sounds=new RecordingSoundPlayer();var audio=scene.Game.gameObject.AddComponent<AudioDirector>();audio.Initialize(scene.Game,scene.Bootstrap.Settings.Model,sounds);scene.Bootstrap.Inventory.Shop.AddCoins(150);scene.Bootstrap.Inventory.TryBuy("plasma_bolt");scene.Bootstrap.Inventory.TryEquip("plasma_bolt");var b=new Board(3,2);b.TryAddArrow(1,0,0,Direction.Right,out _);b.TryAddArrow(2,1,0,Direction.Right,out _);scene.Game.StartLevel(b);sounds.Events.Clear();scene.Tap(0,0);
                Assert.That(sounds.Events.Count(e=>e.Cue==SoundCue.Blocked),Is.EqualTo(1));Assert.That(sounds.Events.Count(e=>e.Cue==SoundCue.SkinBlocked),Is.EqualTo(1));scene.Tap(1,0);Assert.That(sounds.Events.Single(e=>e.Cue==SoundCue.Exit).ScaleStep,Is.Zero);
            }
        }
        /// <summary>Automatic skin clocks freeze on pause; hidden boards clear trails without changing the puzzle.</summary>
        [Test] public void PauseAndHiddenBoard()
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.ShowLevel(0);scene.Board.SetArrowSkin("frostbite");var director=scene.Game.GetComponent<SkinDirector>();director.Tick(.1f);float before=director.Clock;scene.Game.SetPaused(true);director.AdvanceFrame(1);Assert.That(director.Clock,Is.EqualTo(before));director.Tick(.1f);Assert.That(director.Clock,Is.GreaterThan(before));scene.Board.TryGetArrowView(1,out var view);director.PlayFlight(view);scene.Board.SetVisible(false);director.AdvanceFrame(.1f);Assert.That(director.LiveParticles,Is.Zero);Assert.That(scene.Game.Session.Board.Arrows.Count,Is.EqualTo(8));
            }
        }
        /// <summary>Ten texture pairs are distinct, repeatable, finite and have a click-free end.</summary>
        [Test] public void TenDeterministicSoundTextures()
        {for(int skin=0;skin<10;skin++){var a=SkinSoundSynth.Render(skin,false);Assert.That(a,Is.EqualTo(SkinSoundSynth.Render(skin,false)));Assert.That(a[a.Length-1],Is.Zero);Assert.That(a.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x)),Is.True);Assert.That(SkinSoundSynth.Render(skin,true).Length,Is.LessThan(a.Length));if(skin>0)Assert.That(a,Is.Not.EqualTo(SkinSoundSynth.Render(skin-1,false)));}}
    }
}
