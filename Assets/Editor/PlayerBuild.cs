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
            Build(BuildTarget.Android, BuildTargetGroup.Android, "Builds/Android/ArrowsKeymaster-" + ReleaseSetup.Version + ".apk");
        }

        /// <summary>Step 15: Google Play upload. Builds a release-signed Android App Bundle (.aab) with the upload key in Keystore/upload.properties.</summary>
        [MenuItem("Tools/Arrow Escape/Build Android App Bundle (Google Play)")]
        public static void BuildAndroidBundle()
        {
            if (!UploadKey.Exists) { File.WriteAllText(Report, "RESULT NoUploadKey: create Keystore/upload.properties first (see AGENTS.md, Step 15)\n"); return; }
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            try { Build(BuildTarget.Android, BuildTargetGroup.Android, "Builds/Android/ArrowsKeymaster-" + ReleaseSetup.Version + ".aab"); }
            finally { EditorUserBuildSettings.buildAppBundle = false; }
        }

        /// <summary>Step 15: exports the Gradle project (for the store-compliance scan; not for upload).</summary>
        [MenuItem("Tools/Arrow Escape/Export Android Gradle Project")]
        public static void ExportAndroidProject()
        {
            EditorUserBuildSettings.buildAppBundle = false; EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            try { Build(BuildTarget.Android, BuildTargetGroup.Android, "Builds/AndroidProject"); }
            finally { EditorUserBuildSettings.exportAsGoogleAndroidProject = false; }
        }

        /// <summary>Builds a Windows player for a quick desktop check.</summary>
        [MenuItem("Tools/Arrow Escape/Build Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "Builds/Windows/ArrowsKeymaster.exe");

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
                if (target == BuildTarget.Android) log.AppendLine("signing: " + UploadKey.Apply());
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

    /// <summary>
    /// Step 15: the Google Play upload key. Keystore/upload.properties (kept out of git) holds storeFile, storePassword,
    /// keyAlias and keyPassword. With it every Android build is release-signed; without it builds use the debug key.
    /// </summary>
    public static class UploadKey
    {
        /// <summary>Properties file next to the project (never committed).</summary>
        public const string PropertiesPath = "Keystore/upload.properties";
        /// <summary>True when the properties file and the keystore it names both exist.</summary>
        public static bool Exists { get { var p = Read(); return p != null && File.Exists(p["storeFile"]); } }
        private static System.Collections.Generic.Dictionary<string, string> Read()
        {
            if (!File.Exists(PropertiesPath)) return null;
            var map = new System.Collections.Generic.Dictionary<string, string>();
            foreach (string line in File.ReadAllLines(PropertiesPath)) { int eq = line.IndexOf('='); if (eq > 0 && !line.TrimStart().StartsWith("#")) map[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim(); }
            foreach (string key in new[] { "storeFile", "storePassword", "keyAlias", "keyPassword" }) if (!map.ContainsKey(key)) return null;
            if (!Path.IsPathRooted(map["storeFile"])) map["storeFile"] = Path.GetFullPath(Path.Combine("Keystore", map["storeFile"]));
            return map;
        }
        /// <summary>Points the Android player settings at the upload key, or back at the debug key. Returns a short status.</summary>
        public static string Apply()
        {
            var p = Read();
            if (p == null || !File.Exists(p["storeFile"])) { PlayerSettings.Android.useCustomKeystore = false; return "debug key (no Keystore/upload.properties)"; }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = p["storeFile"]; PlayerSettings.Android.keystorePass = p["storePassword"];
            PlayerSettings.Android.keyaliasName = p["keyAlias"]; PlayerSettings.Android.keyaliasPass = p["keyPassword"];
            return "upload key " + p["keyAlias"];
        }
    }
}
