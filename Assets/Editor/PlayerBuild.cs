using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArrowEscape.EditorTools
{
    /// <summary>
    /// Player builds (Step 14N): Tools > Arrow Escape > Build Android APK writes Builds/Android/ArrowsEscape-0.1.0.apk;
    /// Build Windows writes a desktop smoke-test player. Each run reports to Logs/DevBridge/build.txt.
    /// </summary>
    public static class PlayerBuild
    {
        private const string Report = "Logs/DevBridge/build.txt";

        /// <summary>Builds the Android APK.</summary>
        [MenuItem("Tools/Arrow Escape/Build Android APK")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;
            Build(BuildTarget.Android, BuildTargetGroup.Android, "Builds/Android/ArrowsEscape-" + ReleaseSetup.Version + ".apk");
        }

        /// <summary>Builds a Windows player for a quick desktop check.</summary>
        [MenuItem("Tools/Arrow Escape/Build Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "Builds/Windows/ArrowsEscape.exe");

        private static void Build(BuildTarget target, BuildTargetGroup group, string path)
        {
            var log = new StringBuilder();
            void Write() { try { Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllText(Report, log.ToString()); } catch (Exception) { } }
            log.AppendLine("started " + DateTime.Now.ToString("HH:mm:ss") + " target " + target); Write();
            try
            {
                bool supported = BuildPipeline.IsBuildTargetSupported(group, target);
                log.AppendLine("module installed: " + supported);
                if (!supported) { log.AppendLine("RESULT MissingModule"); Write(); return; }
                ReleaseSetup.Apply();
                string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };
                log.AppendLine("scenes: " + string.Join(", ", scenes));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var options = new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = target, targetGroup = group, options = BuildOptions.None };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                log.AppendLine("RESULT " + summary.result + " size " + (summary.totalSize / 1048576.0).ToString("0.0") + " MB time " + summary.totalTime + " errors " + summary.totalErrors + " warnings " + summary.totalWarnings);
                log.AppendLine("output " + Path.GetFullPath(path));
                foreach (BuildStep step in report.steps)
                    foreach (BuildStepMessage message in step.messages)
                        if (message.type == LogType.Error || message.type == LogType.Exception) log.AppendLine("ERROR [" + step.name + "] " + message.content);
            }
            catch (Exception e) { log.AppendLine("RESULT Exception " + e); }
            log.AppendLine("finished " + DateTime.Now.ToString("HH:mm:ss"));
            Write();
        }
    }
}
