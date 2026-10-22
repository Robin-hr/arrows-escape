using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ArrowEscape.DevBridge
{
    /// <summary>
    /// Development-only helper so the AI assistant can work on this project while the Editor is open.
    /// It watches Logs/DevBridge/cmd.txt once a second and writes results next to it:
    ///   refresh                      import changed files (compile errors go to compile-errors.txt)
    ///   test[:regex]                 run Edit Mode tests -> tests.txt
    ///   menu:Path/To/Item            run an Editor menu item
    ///   open:Assets/Scenes/X.unity   open a scene
    ///   play:1.5=shot;4=call:OpenProfile;6=shot;8=stop   enter Play mode and run a timeline
    /// Never included in player builds (Editor-only assembly). Delete the folder to remove it.
    /// </summary>
    [InitializeOnLoad]
    internal static class DevBridge
    {
        private static readonly string Dir = Path.GetFullPath("Logs/DevBridge");
        private const string TimelineKey = "ArrowEscape.DevBridge.Timeline", StartKey = "ArrowEscape.DevBridge.Start";
        private static double nextPoll;
        private static TestRunnerApi api;

        static DevBridge()
        {
            Directory.CreateDirectory(Dir);
            EditorApplication.update += Poll;
            CompilationPipeline.compilationStarted += _ => Write("compile-status.txt", "compiling " + Now);
            CompilationPipeline.compilationFinished += _ => Write("compile-status.txt", "finished " + Now);
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
            Write("ready.txt", Now);
        }

        private static string Now => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        private static void Write(string file, string text)
        {
            try { File.WriteAllText(Path.Combine(Dir, file), text + Environment.NewLine); } catch (Exception) { }
        }

        private static void Append(string file, string text)
        {
            try { File.AppendAllText(Path.Combine(Dir, file), text + Environment.NewLine); } catch (Exception) { }
        }

        private static void OnAssemblyCompiled(string assembly, CompilerMessage[] messages)
        {
            var errors = new StringBuilder();
            foreach (CompilerMessage m in messages)
                if (m.type == CompilerMessageType.Error) errors.AppendLine(m.file + "(" + m.line + "): " + m.message);
            string name = Path.GetFileNameWithoutExtension(assembly);
            Write("compile-" + name + ".txt", errors.Length == 0 ? "ok " + Now : "ERRORS " + Now + Environment.NewLine + errors);
            if (errors.Length > 0) Append("compile-errors.txt", Now + " " + name + Environment.NewLine + errors);
        }

        private static void Poll()
        {
            if (EditorApplication.isPlaying) { RunTimeline(); return; }
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1.0;
            string path = Path.Combine(Dir, "cmd.txt");
            if (!File.Exists(path)) return;
            string command;
            try { command = File.ReadAllText(path).Trim(); File.Delete(path); } catch (Exception) { return; }
            Write("last-command.txt", Now + " " + command);
            try { Run(command); }
            catch (Exception e) { Write("last-error.txt", Now + " " + e); }
        }

        private static void Run(string command)
        {
            if (command == "refresh") { AssetDatabase.Refresh(); return; }
            if (command.StartsWith("test", StringComparison.Ordinal)) { RunTests(command.Length > 5 ? command.Substring(5) : null); return; }
            if (command.StartsWith("open:", StringComparison.Ordinal)) { UnityEditor.SceneManagement.EditorSceneManager.OpenScene(command.Substring(5)); Write("menu.txt", Now + " opened " + command.Substring(5)); return; }
            if (command.StartsWith("menu:", StringComparison.Ordinal)) { Write("menu.txt", Now + " " + EditorApplication.ExecuteMenuItem(command.Substring(5))); return; }
            if (command.StartsWith("play:", StringComparison.Ordinal))
            {
                SessionState.SetString(TimelineKey, command.Substring(5));
                SessionState.SetFloat(StartKey, -1f);
                EditorApplication.isPlaying = true;
            }
        }

        private static void RunTests(string filter)
        {
            if (api == null) api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
            var f = new Filter { testMode = TestMode.EditMode };
            if (!string.IsNullOrEmpty(filter)) f.groupNames = new[] { filter };
            Write("tests-status.txt", "running " + Now);
            api.Execute(new ExecutionSettings(f));
        }

        // Timeline: "time=action;time=action". Actions: shot, stop, call:Method (on AppFlowController), click:ButtonPath.
        private static void RunTimeline()
        {
            string timeline = SessionState.GetString(TimelineKey, "");
            if (string.IsNullOrEmpty(timeline)) return;
            float start = SessionState.GetFloat(StartKey, -1f);
            if (start < 0) { start = Time.realtimeSinceStartup; SessionState.SetFloat(StartKey, start); }
            float now = Time.realtimeSinceStartup - start;
            var remaining = new List<string>();
            foreach (string entry in timeline.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                int eq = entry.IndexOf('=');
                if (eq < 0 || !float.TryParse(entry.Substring(0, eq), NumberStyles.Float, CultureInfo.InvariantCulture, out float at)) continue;
                if (now < at) { remaining.Add(entry); continue; }
                Act(entry.Substring(eq + 1).Trim(), at);
            }
            SessionState.SetString(TimelineKey, string.Join(";", remaining));
        }

        private static void Act(string action, float at)
        {
            try
            {
                if (action == "shot")
                {
                    string file = Path.Combine(Dir, "shot_" + at.ToString("0.0", CultureInfo.InvariantCulture) + ".png");
                    ScreenCapture.CaptureScreenshot(file);
                    Append("play-log.txt", Now + " shot " + file);
                }
                else if (action == "stop") { SessionState.SetString(TimelineKey, ""); EditorApplication.isPlaying = false; }
                else if (action.StartsWith("call:", StringComparison.Ordinal))
                {
                    Type type = Type.GetType("ArrowEscape.UI.AppFlowController, ArrowEscape.UI");
                    UnityEngine.Object flow = type == null ? null : UnityEngine.Object.FindFirstObjectByType(type);
                    MethodInfo method = type?.GetMethod(action.Substring(5), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    method?.Invoke(flow, null);
                    Append("play-log.txt", Now + " " + action + (method == null || flow == null ? " (not found)" : ""));
                }
                else if (action.StartsWith("click:", StringComparison.Ordinal))
                {
                    GameObject target = GameObject.Find(action.Substring(6));
                    var button = target == null ? null : target.GetComponent<UnityEngine.UI.Button>();
                    button?.onClick.Invoke();
                    Append("play-log.txt", Now + " " + action + (button == null ? " (not found)" : ""));
                }
            }
            catch (Exception e) { Append("play-log.txt", Now + " " + action + " failed: " + e.Message); }
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                var text = new StringBuilder();
                text.AppendLine("finished " + Now);
                text.AppendLine("passed " + result.PassCount + " failed " + result.FailCount + " skipped " + result.SkipCount + " inconclusive " + result.InconclusiveCount);
                Collect(result, text);
                Write("tests.txt", text.ToString());
                Write("tests-status.txt", "finished " + Now);
            }

            private static void Collect(ITestResultAdaptor result, StringBuilder text)
            {
                if (result.HasChildren) { foreach (ITestResultAdaptor child in result.Children) Collect(child, text); return; }
                if (result.TestStatus != TestStatus.Failed) return;
                text.AppendLine("FAIL " + result.FullName);
                text.AppendLine("  " + (result.Message ?? "").Trim());
                string stack = result.StackTrace ?? "";
                string[] lines = stack.Split('\n');
                for (int i = 0; i < lines.Length && i < 6; i++) text.AppendLine("    " + lines[i].Trim());
            }
        }
    }
}
