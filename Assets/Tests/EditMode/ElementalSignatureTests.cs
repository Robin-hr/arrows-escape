using System;
using System.IO;
using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
namespace ArrowEscape.Tests
{
    /// <summary>Signatures are bounded reversible presentation, never a gameplay lock.</summary>
    public sealed class ElementalSignatureTests
    {
        /// <summary>One start per event, immediate subsequent taps, and board residues clear at completion.</summary>
        [TestCase("frostbite")][TestCase("inferno_streak")][TestCase("plasma_bolt")][TestCase("void_whisper")]
        public void ExitSignaturesNeverDelayLogic(string id)
        {
            using(var scene=new EconomyTestScene())
            {
                var model=new Board(6,4);for(int y=0;y<3;y++)model.TryAddArrow(new Arrow(y+1,new[]{new Cell(0,y),new Cell(1,y)},Direction.Right),out _);scene.Game.StartLevel(model);scene.Board.SetArrowSkin(id);var director=scene.Game.GetComponent<SkinDirector>();
                scene.Tap(1,0);scene.Tap(1,1);Assert.That(scene.Game.MoveCount,Is.EqualTo(2));Assert.That(director.Signatures.Starts,Is.EqualTo(2));Assert.That(director.Signatures.HasActive,Is.True);director.Tick(1.3f);Assert.That(director.Signatures.HasActive,Is.False);
                UnityEngine.TestTools.LogAssert.Expect(LogType.Log,"Level cleared. Mistakes: 0");scene.Tap(1,2);Assert.That(director.Signatures.Starts,Is.EqualTo(3));Assert.That(director.Signatures.HasActive,Is.False);
            }
        }
        /// <summary>Length-one arrows leave the complete real exit track, not a fixed preview-length streak.</summary>
        [TestCase("frostbite")][TestCase("inferno_streak")][TestCase("plasma_bolt")][TestCase("void_whisper")]
        public void SingleCellResidueUsesTheFullExitDistance(string id)
        {
            using(var scene=new EconomyTestScene())
            {
                var model=new Board(12,3);model.TryAddArrow(1,1,0,Direction.Right,out _);model.TryAddArrow(2,0,2,Direction.Up,out _);scene.Game.StartLevel(model);scene.Board.SetArrowSkin(id);scene.Board.TryGetArrowView(1,out var view);Vector2 start=view.Skin.TailWorld,end=view.Skin.HeadWorld+view.Skin.DirectionWorld*AnimationMath.SlideDistance(12,3,1,0,Direction.Right);scene.Tap(1,0);var effect=scene.Game.GetComponent<SkinDirector>().Signatures;Assert.That(Vector2.Distance(effect.LastTrackStart,start),Is.LessThan(.0001f));Assert.That(Vector2.Distance(effect.LastTrackEnd,end),Is.LessThan(.0001f));if(id!="inferno_streak"){scene.Game.GetComponent<ArrowAnimator>().Tick(.15f);var block=new MaterialPropertyBlock();view.Skin.Elemental.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Elemental SDF head").GetPropertyBlock(block);Assert.That(block.GetFloat("_WindowStart"),Is.EqualTo(view.Offset.magnitude).Within(.0001f));Assert.That(view.Offset.magnitude,Is.GreaterThan(0));}
            }
        }
        /// <summary>Half-duration plasma is a presentation choice; normal arrow timing is unchanged.</summary>
        [Test] public void PlasmaExitsBeforeNormalDuration()
        {
            using(var scene=new EconomyTestScene())
            {
                var model=new Board(6,3);model.TryAddArrow(new Arrow(1,new[]{new Cell(0,0),new Cell(1,0)},Direction.Right),out _);model.TryAddArrow(2,0,2,Direction.Up,out _);scene.Game.StartLevel(model);scene.Board.SetArrowSkin("plasma_bolt");scene.Board.TryGetArrowView(1,out var view);float normal=AnimationMath.PathExitDuration(view.Path.ExitDistance);scene.Tap(1,0);scene.Game.GetComponent<ArrowAnimator>().Tick(normal*.51f);Assert.That(view==null,Is.True);Assert.That(scene.Game.MoveCount,Is.EqualTo(1));
            }
        }
        /// <summary>Undo reverses travel and explicitly restores the idle procedural state at the cell.</summary>
        [TestCase("frostbite")][TestCase("plasma_bolt")][TestCase("void_whisper")]
        public void UndoEndsAtExactRest(string id)
        {
            using(var scene=new EconomyTestScene())
            {
                var model=new Board(6,4);model.TryAddArrow(new Arrow(1,new[]{new Cell(0,0),new Cell(1,0),new Cell(1,1),new Cell(2,1)},Direction.Right),out _);model.TryAddArrow(2,5,3,Direction.Up,out _);scene.Game.StartLevel(model);scene.Board.SetArrowSkin(id);scene.Tap(2,1);var animator=scene.Game.GetComponent<ArrowAnimator>();animator.Tick(.15f);scene.Game.Undo();scene.Board.TryGetArrowView(1,out var restored);if(id=="void_whisper")Assert.That(scene.Game.GetComponent<SkinDirector>().Signatures.HasReturnRift,Is.True);animator.Tick(2);scene.Game.GetComponent<SkinDirector>().Tick(2);Assert.That(scene.Game.GetComponent<SkinDirector>().Signatures.HasReturnRift,Is.False);Assert.That(restored.Path.Distance,Is.Zero);Assert.That(restored.Skin.Elemental.IsFlying,Is.False);Assert.That(restored.BaseWorldPosition,Is.EqualTo((Vector2)scene.Board.CellToWorld(2,1)));
            }
        }
        /// <summary>Undo, restart and board replacement remove every lingering board residue.</summary>
        [TestCase("frostbite")][TestCase("inferno_streak")][TestCase("plasma_bolt")][TestCase("void_whisper")]
        public void BoardChangesClearResidues(string id)
        {
            using(var scene=new EconomyTestScene())
            {
                scene.Bootstrap.ShowLevel(0);scene.Board.SetArrowSkin(id);var d=scene.Game.GetComponent<SkinDirector>();scene.Tap(3,0);Assert.That(d.Signatures.HasActive,Is.True);scene.Game.Undo();Assert.That(d.Signatures.HasActive,Is.False);scene.Tap(3,0);scene.Game.RestartLevel();Assert.That(d.Signatures.HasActive,Is.False);scene.Tap(3,0);scene.Bootstrap.ShowLevel(1);Assert.That(d.Signatures.HasActive,Is.False);
            }
        }
        /// <summary>Mesh dots recover exact original values when the shadow effect is removed.</summary>
        [Test] public void VoidDotsRestoreExactly()
        {
            var go=new GameObject("Void absorption");try
            {
                var view=go.AddComponent<BoardView>();var board=new Board(6,4);board.TryAddArrow(new Arrow(1,new[]{new Cell(0,0),new Cell(1,0),new Cell(1,1)},Direction.Up),out _);view.Show(board);var original=(Color[])view.PathDots.Colors.Clone();var dim=new DotAbsorption(view);dim.Refresh(true,true);Assert.That(view.PathDots.Colors,Is.Not.EqualTo(original));dim.Clear();Assert.That(view.PathDots.Colors,Is.EqualTo(original));
                view.SetArrowSkin("void_whisper");var dark=(Color[])view.PathDots.Colors.Clone();view.SetArrowSkin("classic");Assert.That(view.PathDots.Colors,Is.EqualTo(original));Assert.That(dark,Is.Not.EqualTo(original));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        /// <summary>Old dimming buffers must never touch the replacement level's smaller dot mesh.</summary>
        [Test] public void VoidHandlesLargeToSmallBoardReplacement()
        {
            using(var scene=new EconomyTestScene())
            {
                foreach(int size in new[]{24,3,12,4})
                {
                    var board=new Board(size,size);board.TryAddArrow(new Arrow(1,new[]{new Cell(0,0),new Cell(1,0)},Direction.Right),out _);
                    Assert.DoesNotThrow(()=>scene.Game.StartLevel(board));scene.Board.SetArrowSkin("void_whisper");Assert.That(scene.Board.PathDots.Colors.Length,Is.EqualTo(size*size*13));scene.Board.SetArrowSkin("classic");Assert.That(scene.Board.PathDots.Colors.All(c=>c==ViewTheme.CellDot),Is.True);scene.Board.SetArrowSkin("void_whisper");
                }
            }
        }
        /// <summary>Warmed idle, signature and light-return ticks allocate no managed memory.</summary>
        [TestCase("frostbite")][TestCase("plasma_bolt")][TestCase("void_whisper")]
        public void BudgetsAndLod(string id)
        {
            var go=new GameObject("Elemental budgets");try
            {
                var b=go.AddComponent<BoardView>();var board=new Board(10,10);for(int i=0;i<100;i++)board.TryAddArrow(i+1,i%10,i/10,Direction.Right,out _);b.Show(board);b.SetArrowSkin(id);var d=go.GetComponent<SkinDirector>();d.SetLodOverride(false);b.TryGetArrowView(1,out var view);for(int i=0;i<100;i++)d.PlayFlight(view);d.Tick(.001f);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20;i++)d.Tick(.001f);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(SkinParticlePool.GlobalLiveCount,Is.LessThanOrEqualTo(600));d.SetLodOverride(true);Assert.That(view.Skin.IsLod,Is.True);int ambient=d.AmbientStarts;d.Tick(1);Assert.That(d.AmbientStarts,Is.EqualTo(ambient));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        /// <summary>Only Editor comparison code may open the visual targets or their source directories.</summary>
        [Test] public void RuntimeNeverReferencesTargetsOrRetiredArt()
        {
            foreach(string file in Directory.GetFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories)){string text=File.ReadAllText(file);Assert.That(text.Contains("reference.png")||text.Contains("ArtSource/Skins"),Is.False,file);}
            foreach(string id in new[]{"frostbite","plasma_bolt","void_whisper","thunderbolt_dart"}){Assert.That(ArrowSkinResources.Load().Find(id),Is.Null);Assert.That(Directory.Exists("Assets/Art/Arrows/"+id),Is.False);}
            Assert.That(ArrowSkinResources.Load().Elemental,Is.Not.Null);
        }
        /// <summary>Sound layers once per exit, preserves note steps, and obeys the live preference.</summary>
        [TestCase("frostbite")][TestCase("plasma_bolt")][TestCase("void_whisper")]
        public void SoundUnderMelodyAndMute(string id)
        {
            using(var scene=new EconomyTestScene())
            {
                var player=new RecordingSoundPlayer();var audio=scene.Game.gameObject.AddComponent<AudioDirector>();audio.Initialize(scene.Game,scene.Bootstrap.Settings.Model,player);scene.Bootstrap.ShowLevel(0);scene.Board.SetArrowSkin(id);player.Events.Clear();scene.Tap(3,0);Assert.That(player.Events.Count(e=>e.Cue==SoundCue.SkinExit),Is.EqualTo(1));Assert.That(player.Events.Single(e=>e.Cue==SoundCue.Exit).ScaleStep,Is.Zero);scene.Bootstrap.Settings.Model.SoundEnabled=false;player.Events.Clear();scene.Tap(0,3);Assert.That(player.Events,Is.Empty);
            }
        }
    }
}
