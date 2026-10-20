using System.Linq;
using ArrowEscape.Core;
using ArrowEscape.Data;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArrowEscape.Tests
{
    /// <summary>Drives actual gameplay events with recording audio instead of a device.</summary>
    public class AudioDirectorTests
    {
        private AudioTestScene scene;
        /// <summary>Creates four independent arrows so undo remains available.</summary>
        [SetUp] public void Setup() => scene = new AudioTestScene();
        /// <summary>Removes event subscribers and runtime objects.</summary>
        [TearDown] public void Cleanup() => scene?.Dispose();
        private SoundEvent[] Cues(SoundCue cue) => scene.Player.Events.Where(e => e.Cue == cue).ToArray();
        private static void Lifecycle(AudioDirector director, string method, params object[] arguments) =>
            typeof(AudioDirector).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(director, arguments);
        private static Board BlockedBoard()
        {
            var board = new Board(3,2); board.TryAddArrow(1,0,0,Direction.Right,out _);
            board.TryAddArrow(2,1,0,Direction.Up,out _); board.TryAddArrow(3,2,0,Direction.Up,out _); return board;
        }
        /// <summary>Post-exit MoveCount selects steps zero, one, two.</summary>
        [Test] public void FirstThreeExits()
        {
            scene.Game.StartLevel(AudioTestScene.FreeBoard(3)); scene.TapId(1); scene.TapId(2); scene.TapId(3);
            Assert.That(Cues(SoundCue.Exit).Select(e => e.ScaleStep), Is.EqualTo(new[] { 0,1,2 }));
        }
        /// <summary>A blocked tap makes one thud without advancing the melody.</summary>
        [Test] public void BlockedDoesNotAdvance()
        {
            scene.Game.StartLevel(BlockedBoard()); scene.TapId(1);
            Assert.That(scene.Player.Events.Count, Is.EqualTo(1)); Assert.That(scene.Player.Events[0], Is.EqualTo(SoundPlanner.ForBlocked()));
            scene.TapId(2); Assert.That(Cues(SoundCue.Exit).Single().ScaleStep, Is.Zero);
        }
        /// <summary>Undo uses the updated count; the repeated exit returns to the same pitch.</summary>
        [Test] public void UndoNote()
        {
            scene.TapId(1); scene.TapId(2); scene.Game.Undo(); scene.TapId(2);
            Assert.That(scene.Player.Events.Select(e => e.ScaleStep), Is.EqualTo(new[] { 0,1,1,1 }));
            Assert.That(scene.Player.Events[2].Cue, Is.EqualTo(SoundCue.Undo));
            Assert.That(scene.Player.Events[2].Volume, Is.LessThan(scene.Player.Events[1].Volume));
        }
        /// <summary>Only a successful hint request chimes.</summary>
        [Test] public void Hint()
        {
            Assert.That(scene.Game.ShowHint(), Is.True); Assert.That(scene.Player.Events.Single(), Is.EqualTo(SoundPlanner.ForHint()));
            scene.Game.SetPaused(true); Assert.That(scene.Game.ShowHint(), Is.False); Assert.That(scene.Player.Events.Count, Is.EqualTo(1));
        }
        /// <summary>The final exit precedes exactly one arpeggio, including after replay.</summary>
        [Test] public void CompleteAndReplay()
        {
            scene.Game.StartLevel(AudioTestScene.FreeBoard(1)); scene.TapId(1);
            Assert.That(scene.Player.Events.Select(e => e.Cue), Is.EqualTo(new[] { SoundCue.Exit,SoundCue.LevelComplete }));
            scene.Game.ShowHint(); scene.Game.Undo(); Assert.That(scene.Player.Events.Count, Is.EqualTo(2));
            scene.Game.RestartLevel(); scene.TapId(1); Assert.That(Cues(SoundCue.LevelComplete).Length, Is.EqualTo(2));
        }
        /// <summary>Sound off silences every gameplay cue and stops playing voices immediately.</summary>
        [Test] public void Disabled()
        {
            int stops = scene.Player.Stops; scene.Settings.SoundEnabled = false; Assert.That(scene.Player.Stops, Is.EqualTo(stops + 1));
            scene.Game.StartLevel(BlockedBoard()); scene.Game.ShowHint(); scene.TapId(1); scene.TapId(2); scene.Game.Undo();
            scene.TapId(2); scene.TapId(1); scene.TapId(3); Assert.That(scene.Player.Events, Is.Empty);
            stops = scene.Player.Stops; scene.Settings.HapticsEnabled = false; Assert.That(scene.Player.Stops, Is.EqualTo(stops));
            scene.Settings.SoundEnabled = true; Assert.That(scene.Player.Events.Single(), Is.EqualTo(SoundPlanner.ForPreview()));
            scene.Settings.HapticsEnabled = true; Assert.That(scene.Player.Events.Count, Is.EqualTo(1)); Assert.That(scene.Player.Stops, Is.EqualTo(stops));
        }
        /// <summary>Restart and a named new level stop existing voices.</summary>
        [Test] public void LevelStartStops()
        {
            int before = scene.Player.Stops; scene.Game.RestartLevel(); Assert.That(scene.Player.Stops, Is.EqualTo(before + 1));
            scene.Game.StartLevel(AudioTestScene.FreeBoard(2)); Assert.That(scene.Player.Stops, Is.EqualTo(before + 2));
        }
        /// <summary>Paused taps never reach gameplay sound events.</summary>
        [Test] public void Paused()
        { scene.Game.SetPaused(true); scene.TapId(1); Assert.That(scene.Player.Events, Is.Empty); scene.Game.SetPaused(false); scene.TapId(1); Assert.That(scene.Player.Events.Count, Is.EqualTo(1)); }
        /// <summary>Destroy unsubscribes game and preference events.</summary>
        [Test] public void DestroyUnsubscribes()
        {
            Object.DestroyImmediate(scene.Director); int stops = scene.Player.Stops;
            scene.TapId(1); scene.Game.ShowHint(); scene.Settings.SoundEnabled = false; scene.Settings.SoundEnabled = true;
            Assert.That(scene.Player.Events, Is.Empty); Assert.That(scene.Player.Stops, Is.EqualTo(stops));
        }
        /// <summary>Application pause stops voices and the scale demonstration.</summary>
        [Test] public void ApplicationPause()
        {
            int before = scene.Player.Stops; scene.Director.TestPlayScale(); Lifecycle(scene.Director,"OnApplicationPause",true);
            Assert.That(scene.Player.Stops, Is.EqualTo(before + 1)); scene.Director.Tick(10); Assert.That(scene.Player.Events, Is.Empty);
            Lifecycle(scene.Director,"OnApplicationPause",false); Assert.That(scene.Player.Stops, Is.EqualTo(before + 1));
        }
        /// <summary>The demonstration plays eleven notes at quarter-second boundaries, then stops.</summary>
        [Test] public void ScaleTiming()
        {
            scene.Director.TestPlayScale(); scene.Director.Tick(.24f); Assert.That(scene.Player.Events, Is.Empty);
            scene.Director.Tick(.01f); Assert.That(scene.Player.Events.Single().ScaleStep, Is.Zero);
            for (int i = 1; i <= 10; i++) { scene.Director.Tick(.25f); Assert.That(scene.Player.Events.Count, Is.EqualTo(i + 1)); Assert.That(scene.Player.Events[i].ScaleStep, Is.EqualTo(i)); }
            scene.Director.Tick(20); Assert.That(scene.Player.Events.Count, Is.EqualTo(11));
            scene.Settings.SoundEnabled = false; scene.Director.TestPlayScale(); scene.Director.TestPlayBlocked(); scene.Director.TestPlayLevelComplete(); scene.Director.Tick(10); Assert.That(scene.Player.Events.Count, Is.EqualTo(11));
        }
        /// <summary>A large tick has the same ordered result, and setting changes cancel the demo.</summary>
        [Test] public void ScaleLargeTickAndMute()
        {
            scene.Director.TestPlayScale(); scene.Director.Tick(11 * .25f);
            Assert.That(scene.Player.Events.Select(e => e.ScaleStep), Is.EqualTo(Enumerable.Range(0,11)));
            scene.Player.Events.Clear(); scene.Director.TestPlayScale(); scene.Director.Tick(.25f); scene.Settings.SoundEnabled = false;
            scene.Director.Tick(10); scene.Settings.SoundEnabled = true; scene.Director.Tick(10);
            Assert.That(scene.Player.Events.Select(e => e.Cue), Is.EqualTo(new[] { SoundCue.Exit,SoundCue.Preview }));
        }
        /// <summary>Missing gameplay warns exactly once and disables the component.</summary>
        [Test] public void MissingController()
        {
            var host = new GameObject("No game"); var director = host.AddComponent<AudioDirector>();
            try
            {
                LogAssert.Expect(LogType.Warning,"ArrowEscape audio: no GameController found; audio disabled.");
                director.Initialize(null,null,new RecordingSoundPlayer()); director.Initialize(null,null,new RecordingSoundPlayer());
                Assert.That(director.enabled, Is.False);
            }
            finally { Object.DestroyImmediate(host); }
        }
        /// <summary>Start without any game also uses the one-warning path.</summary>
        [Test] public void MissingControllerInStart()
        {
            scene.Dispose(); scene = null;
            var host = new GameObject("No game at Start"); var director = host.AddComponent<AudioDirector>();
            try
            {
                LogAssert.Expect(LogType.Warning,"ArrowEscape audio: no GameController found; audio disabled.");
                Lifecycle(director,"Start"); Lifecycle(director,"Start"); Assert.That(director.enabled, Is.False);
            }
            finally { Object.DestroyImmediate(host); }
        }
        /// <summary>Null settings always permit sound, regardless of another settings model.</summary>
        [Test] public void NullSettings()
        {
            scene.Director.Initialize(scene.Game,null,scene.Player); scene.Settings.SoundEnabled = false; scene.TapId(1);
            Assert.That(scene.Player.Events.Single().Cue, Is.EqualTo(SoundCue.Exit));
        }
        /// <summary>Reinitializing removes old subscriptions instead of doubling sound requests.</summary>
        [Test] public void Reinitialize()
        {
            scene.Director.Initialize(scene.Game,scene.Settings,scene.Player); scene.Director.Initialize(scene.Game,scene.Settings,scene.Player);
            scene.TapId(1); Assert.That(scene.Player.Events.Count, Is.EqualTo(1));
        }
        /// <summary>Context test cues respect the same preference gate.</summary>
        [Test] public void ContextCues()
        {
            scene.Director.TestPlayBlocked(); scene.Director.TestPlayLevelComplete();
            Assert.That(scene.Player.Events.Select(e => e.Cue), Is.EqualTo(new[] { SoundCue.Blocked,SoundCue.LevelComplete }));
        }
        /// <summary>Real samples emit one exit per arrow and one final completion.</summary>
        [TestCase(1)][TestCase(2)][TestCase(3)] public void Samples(int index)
        {
            Board board = LevelRepository.LoadFromResources("Levels/level_00" + index).Board; scene.Game.StartLevel(board); int count = board.Arrows.Count;
            while (!scene.Game.IsCleared) scene.Tap(HintFinder.Find(board).Arrow);
            SoundEvent[] exits = Cues(SoundCue.Exit); Assert.That(exits.Length, Is.EqualTo(count)); Assert.That(Cues(SoundCue.LevelComplete).Length, Is.EqualTo(1));
            for (int i = 0; i < count; i++) Assert.That(exits[i].ScaleStep, Is.EqualTo(MusicScale.PingPongIndex(i,11)));
            Assert.That(scene.Player.Events.Last().Cue, Is.EqualTo(SoundCue.LevelComplete));
        }
        /// <summary>Without a listener, the director adds one to its host; repeated checks add none.</summary>
        [Test] public void ListenerFallback()
        {
            AudioListener[] previous = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            var objects = previous.Select(l => l.gameObject).Distinct().ToArray();
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            string[] tags = cameras.Select(c => c.tag).ToArray();
            bool fixtureActive = scene.Host.activeSelf;
            foreach (var obj in objects) obj.SetActive(false);
            // A scene listener means our fixture has no listener of its own. Hide its
            // director too, so the static fallback can only select the test host.
            scene.Host.SetActive(false);
            for (int i = 0; i < cameras.Length; i++) cameras[i].tag = "Untagged";
            var host = new GameObject("Listener fallback"); var director = host.AddComponent<AudioDirector>();
            try
            {
                AudioDirector.EnsureAudioListener(); AudioDirector.EnsureAudioListener();
                Assert.That(host.GetComponents<AudioListener>().Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.enabled), Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(host);
                for (int i = 0; i < cameras.Length; i++) cameras[i].tag = tags[i];
                foreach (var obj in objects) obj.SetActive(true);
                scene.Host.SetActive(fixtureActive);
            }
        }
        /// <summary>Reproduces running the fallback test while SampleScene already supplies a listener.</summary>
        [Test] public void ListenerFallbackWithSceneCamera()
        {
            var cameraHost = new GameObject("Existing scene camera",typeof(Camera),typeof(AudioListener));
            cameraHost.tag = "MainCamera";
            AudioListener fixtureListener = scene.Host.GetComponent<AudioListener>();
            if (fixtureListener != null) Object.DestroyImmediate(fixtureListener);
            try { ListenerFallback(); }
            finally { Object.DestroyImmediate(cameraHost); }
        }
        /// <summary>An existing listener is reused; camera fallback creates only one component.</summary>
        [Test] public void ExistingAndCameraListener()
        {
            int before = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length;
            AudioDirector.EnsureAudioListener(); Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(before));
            var host = new GameObject("Second listener",typeof(AudioListener));
            try { AudioDirector.EnsureAudioListener(); Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.enabled), Is.EqualTo(1)); }
            finally { Object.DestroyImmediate(host); AudioDirector.EnsureAudioListener(); }
        }
    }
}
