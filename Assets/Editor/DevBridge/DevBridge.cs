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
    ///   resolve                      re-resolve Packages/manifest.json
    ///   test[:regex]                 run Edit Mode tests -> tests.txt
    ///   menu:Path/To/Item            run an Editor menu item
    ///   open:Assets/Scenes/X.unity   open a scene
    ///   play:1.5=shot;4=call:OpenProfile;6=shot;8=stop   enter Play mode and run a timeline
    ///                                (call:Type.Method targets another ArrowEscape.UI component, e.g. call:LoadingOverlay.HideImmediate)
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
            if (EditorApplication.isPlaying)
            {
                RunTimeline();
                // Keep the game rendering while the editor window is in the background, so every shot lands.
                if (!string.IsNullOrEmpty(SessionState.GetString(TimelineKey, ""))) { EditorApplication.QueuePlayerLoopUpdate(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); }
                return;
            }
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
            if (command == "resolve") { UnityEditor.PackageManager.Client.Resolve(); Write("menu.txt", Now + " resolve"); return; }
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
        private static int lastActionFrame = -10;
        private static string pendingShot;
        private static void RunTimeline()
        {
            string timeline = SessionState.GetString(TimelineKey, "");
            if (string.IsNullOrEmpty(timeline)) return;
            float start = SessionState.GetFloat(StartKey, -1f);
            if (start < 0) { start = Time.realtimeSinceStartup; SessionState.SetFloat(StartKey, start); lastActionFrame = -10; pendingShot = null; Application.runInBackground = true; }
            // A capture is written when the game view next renders; wait for it so later actions never leak into it.
            if (pendingShot != null) { if (!File.Exists(pendingShot)) return; pendingShot = null; SessionState.SetFloat(StartKey, Time.realtimeSinceStartup - ShotTime); start = SessionState.GetFloat(StartKey, 0); }
            float now = Time.realtimeSinceStartup - start;
            // One action per rendered frame (at least two frames apart), in order, so captures never overwrite each other.
            if (Time.frameCount < lastActionFrame + 3) return;
            string[] entries = timeline.Split(';');
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry)) continue;
                int eq = entry.IndexOf('=');
                if (eq < 0 || !float.TryParse(entry.Substring(0, eq), NumberStyles.Float, CultureInfo.InvariantCulture, out float at)) continue;
                if (now < at) return;
                SessionState.SetString(TimelineKey, string.Join(";", entries, i + 1, entries.Length - i - 1));
                lastActionFrame = Time.frameCount;
                Act(entry.Substring(eq + 1).Trim(), at);
                return;
            }
            SessionState.SetString(TimelineKey, "");
        }

        private static void Act(string action, float at)
        {
            try
            {
                if (action == "shot")
                {
                    string file = Path.Combine(Dir, "shot_" + at.ToString("0.0", CultureInfo.InvariantCulture) + ".png");
                    try { if (File.Exists(file)) File.Delete(file); } catch (Exception) { }
                    ScreenCapture.CaptureScreenshot(file); pendingShot = file; ShotTime = at;
                    Append("play-log.txt", Now + " shot " + file);
                }
                else if (action.StartsWith("probe:", StringComparison.Ordinal))
                {
                    string[] parts = action.Substring(6).Split(',');
                    float seconds = parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float sec) ? sec : 3f;
                    FrameProbe.Run(parts[0], seconds, Path.Combine(Dir, "probe.txt"));
                    Append("play-log.txt", Now + " " + action);
                }
                else if (action == "stop") { SessionState.SetString(TimelineKey, ""); EditorApplication.isPlaying = false; }
                else if (action.StartsWith("call:", StringComparison.Ordinal))
                {
                    string target = action.Substring(5), typeName = "AppFlowController", argument = null;
                    int paren = target.IndexOf('(');
                    if (paren > 0 && target.EndsWith(")", StringComparison.Ordinal)) { argument = target.Substring(paren + 1, target.Length - paren - 2); target = target.Substring(0, paren); }
                    int dot = target.IndexOf('.');
                    if (dot > 0) { typeName = target.Substring(0, dot); target = target.Substring(dot + 1); }
                    Type type = Type.GetType("ArrowEscape.UI." + typeName + ", ArrowEscape.UI");
                    UnityEngine.Object flow = type == null ? null : UnityEngine.Object.FindFirstObjectByType(type);
                    MethodInfo method = null; object[] args = null;
                    if (type != null && argument == null) method = type.GetMethod(target, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    else if (type != null)
                        foreach (MethodInfo candidate in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            ParameterInfo[] ps = candidate.GetParameters();
                            if (candidate.Name != target || ps.Length != 1) continue;
                            Type pt = ps[0].ParameterType;
                            if (pt == typeof(int) && int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv)) { method = candidate; args = new object[] { iv }; break; }
                            if (pt == typeof(float) && float.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out float fv)) { method = candidate; args = new object[] { fv }; break; }
                            if (pt == typeof(string)) { method = candidate; args = new object[] { argument }; break; }
                            if (pt.IsEnum) { try { args = new object[] { Enum.Parse(pt, argument) }; method = candidate; break; } catch (Exception) { } }
                        }
                    method?.Invoke(flow, args);
                    Append("play-log.txt", Now + " " + action + (method == null || flow == null ? " (not found)" : ""));
                }
                else if (action == "dump")
                {
                    // Lists every renderer under the first arrow view (debugging presentation in Play mode).
                    var text = new StringBuilder();
                    Type viewType = Type.GetType("ArrowEscape.View.ArrowView, ArrowEscape.View");
                    var views = UnityEngine.Object.FindObjectsByType(viewType, FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    text.AppendLine("views " + views.Length);
                    if (views.Length > 0)
                        foreach (var r in ((Component)views[0]).GetComponentsInChildren<Renderer>(true))
                        {
                            string extra = r is SpriteRenderer sr ? " col=" + sr.color + " sprite=" + (sr.sprite != null ? sr.sprite.name : "null") : "";
                            text.AppendLine(r.name + " en=" + r.enabled + " act=" + r.gameObject.activeInHierarchy + " ord=" + r.sortingOrder + " layer=" + r.gameObject.layer + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.name : "null") + " pos=" + r.transform.position + " bounds=" + r.bounds.size + extra);
                        }
                    Write("dump.txt", text.ToString());
                }
                else if (action == "solve")
                {
                    // Taps hinted arrows until the level is cleared (screenshots of the completion screen).
                    Type gameType = Type.GetType("ArrowEscape.View.GameController, ArrowEscape.View"), tapType = Type.GetType("ArrowEscape.View.TapController, ArrowEscape.View");
                    Type boardViewType = Type.GetType("ArrowEscape.View.BoardView, ArrowEscape.View"), hintType = Type.GetType("ArrowEscape.Core.HintFinder, ArrowEscape.Core");
                    object game = UnityEngine.Object.FindFirstObjectByType(gameType), taps = UnityEngine.Object.FindFirstObjectByType(tapType), view = UnityEngine.Object.FindFirstObjectByType(boardViewType);
                    int tapped = 0;
                    for (int guard = 0; guard < 500 && game != null && taps != null && view != null; guard++)
                    {
                        if ((bool)gameType.GetProperty("IsCleared").GetValue(game)) break;
                        object session = gameType.GetProperty("Session").GetValue(game); object board = session.GetType().GetProperty("Board").GetValue(session);
                        object hint = hintType.GetMethod("Find").Invoke(null, new[] { board }); object arrow = hint.GetType().GetProperty("Arrow").GetValue(hint);
                        if (arrow == null) break;
                        int x = (int)arrow.GetType().GetProperty("X").GetValue(arrow), y = (int)arrow.GetType().GetProperty("Y").GetValue(arrow);
                        Vector3 world = (Vector3)boardViewType.GetMethod("CellToWorld", new[] { typeof(int), typeof(int) }).Invoke(view, new object[] { x, y });
                        tapType.GetMethod("PointerDown").Invoke(taps, new object[] { 1, (Vector2)world }); tapType.GetMethod("PointerUp").Invoke(taps, new object[] { 1, (Vector2)world });
                        tapped++;
                    }
                    Append("play-log.txt", Now + " solve tapped " + tapped);
                }
                else if (action.StartsWith("click:", StringComparison.Ordinal))
                {
                    string path = action.Substring(6);
                    GameObject target = GameObject.Find(path);
                    var button = target == null ? null : target.GetComponent<UnityEngine.UI.Button>();
                    if (button == null)
                        foreach (var candidate in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                            if (PathOf(candidate.transform).EndsWith(path, StringComparison.Ordinal)) { button = candidate; break; }
                    button?.onClick.Invoke();
                    Append("play-log.txt", Now + " " + action + (button == null ? " (not found)" : ""));
                }
            }
            catch (Exception e) { Append("play-log.txt", Now + " " + action + " failed: " + e.Message); }
        }

        private static float ShotTime;
        private static string PathOf(Transform t) { string p = t.name; while (t.parent != null) { t = t.parent; p = t.name + "/" + p; } return p; }

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
