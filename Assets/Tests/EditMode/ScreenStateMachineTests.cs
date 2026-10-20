using System;
using System.Collections.Generic;
using ArrowEscape.Core;
using NUnit.Framework;
namespace ArrowEscape.Tests
{
    /// <summary>Covers every navigation method from every state, including rejected transitions.</summary>
    public class ScreenStateMachineTests
    {
        /// <summary>All six states by all seven transition methods.</summary>
        public static IEnumerable<TestCaseData> Cases()
        {
            foreach (ScreenState state in Enum.GetValues(typeof(ScreenState)))
                foreach (string method in new[] { "OpenLevelSelect", "OpenSettings", "StartPlaying", "Pause", "Resume", "LevelCompleted", "ToMainMenu" })
                    yield return new TestCaseData(state, method);
        }
        /// <summary>MainMenu is the initial state.</summary>
        [Test] public void Initial() => Assert.That(new ScreenStateMachine().Current, Is.EqualTo(ScreenState.MainMenu));
        /// <summary>Allowed transitions change exactly once; rejected ones change nothing.</summary>
        [TestCaseSource(nameof(Cases))] public void Matrix(ScreenState state, string method)
        {
            var m = At(state); int changes = 0; m.StateChanged += (old, next) => { changes++; Assert.That(m.Current, Is.EqualTo(next)); Assert.That(old, Is.EqualTo(state)); };
            ScreenState target = state; bool allowed = false;
            switch (method)
            {
                case "OpenLevelSelect": allowed = state == ScreenState.MainMenu || state == ScreenState.LevelComplete; target = ScreenState.LevelSelect; break;
                case "OpenSettings": allowed = state == ScreenState.MainMenu || state == ScreenState.Paused; target = ScreenState.Settings; break;
                case "StartPlaying": allowed = state != ScreenState.Settings; target = ScreenState.Playing; break;
                case "Pause": allowed = state == ScreenState.Playing; target = ScreenState.Paused; break;
                case "Resume": allowed = state == ScreenState.Paused; target = ScreenState.Playing; break;
                case "LevelCompleted": allowed = state == ScreenState.Playing; target = ScreenState.LevelComplete; break;
                case "ToMainMenu": allowed = state == ScreenState.LevelSelect || state == ScreenState.Paused || state == ScreenState.LevelComplete; target = ScreenState.MainMenu; break;
            }
            bool result = (bool)typeof(ScreenStateMachine).GetMethod(method).Invoke(m, null);
            Assert.That(result, Is.EqualTo(allowed)); Assert.That(m.Current, Is.EqualTo(allowed ? target : state)); Assert.That(changes, Is.EqualTo(allowed && target != state ? 1 : 0));
        }
        /// <summary>Back follows each state's explicit rule.</summary>
        [TestCase(ScreenState.MainMenu, ScreenState.MainMenu, false)][TestCase(ScreenState.LevelSelect, ScreenState.MainMenu, true)]
        [TestCase(ScreenState.Playing, ScreenState.Paused, true)][TestCase(ScreenState.Paused, ScreenState.Playing, true)]
        [TestCase(ScreenState.LevelComplete, ScreenState.LevelSelect, true)][TestCase(ScreenState.Settings, ScreenState.MainMenu, true)]
        public void Back(ScreenState state, ScreenState target, bool works)
        { var m = At(state); Assert.That(m.Back(), Is.EqualTo(works)); Assert.That(m.Current, Is.EqualTo(target)); }
        /// <summary>Settings remembers either allowed origin.</summary>
        [TestCase(ScreenState.MainMenu)][TestCase(ScreenState.Paused)] public void SettingsReturn(ScreenState origin)
        { var m = At(origin); m.OpenSettings(); Assert.That(m.SettingsReturnState, Is.EqualTo(origin)); m.Back(); Assert.That(m.Current, Is.EqualTo(origin)); }
        /// <summary>A complete session raises the exact ordered sequence.</summary>
        [Test] public void Session()
        {
            var m = new ScreenStateMachine(); var sequence = new List<ScreenState>(); m.StateChanged += (old, next) => sequence.Add(next);
            m.OpenLevelSelect(); m.StartPlaying(); m.Pause(); m.OpenSettings(); m.Back(); m.Resume(); m.LevelCompleted(); m.OpenLevelSelect(); m.Back();
            Assert.That(sequence, Is.EqualTo(new[] { ScreenState.LevelSelect, ScreenState.Playing, ScreenState.Paused, ScreenState.Settings, ScreenState.Paused, ScreenState.Playing, ScreenState.LevelComplete, ScreenState.LevelSelect, ScreenState.MainMenu }));
        }
        private static ScreenStateMachine At(ScreenState state)
        {
            var m = new ScreenStateMachine();
            if (state == ScreenState.LevelSelect) m.OpenLevelSelect();
            else if (state == ScreenState.Settings) m.OpenSettings();
            else if (state != ScreenState.MainMenu) { m.StartPlaying(); if (state == ScreenState.Paused) m.Pause(); if (state == ScreenState.LevelComplete) m.LevelCompleted(); }
            return m;
        }
    }
}
