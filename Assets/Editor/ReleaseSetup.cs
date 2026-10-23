using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ArrowEscape.EditorTools
{
    /// <summary>
    /// Applies the app identity once per editor session: name, IDs, portrait orientation, dark splash,
    /// app icons (including Android adaptive layers) and texture import settings for the neon brand art.
    /// Safe to run repeatedly; also available as Tools > Arrow Escape > Apply Release Setup.
    /// </summary>
    [InitializeOnLoad]
    public static class ReleaseSetup
    {
        private const string SessionKey = "ArrowEscape.ReleaseSetup.v2";
        private const string IconFolder = "Assets/Art/AppIcon/";
        private const string BrandFolder = "Assets/Resources/Brand";
        /// <summary>Store name shown under the icon.</summary>
        public const string ProductName = "Arrows Escape";
        /// <summary>Publisher name.</summary>
        public const string CompanyName = "Hebrus Robin";
        /// <summary>Package / bundle identifier.</summary>
        public const string ApplicationId = "com.hebrusrobin.arrowsescape";

        static ReleaseSetup() { EditorApplication.delayCall += AutoApply; }

        private static void AutoApply()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        }

        /// <summary>Applies every setting now.</summary>
        [MenuItem("Tools/Arrow Escape/Apply Release Setup")]
        public static void Apply()
        {
            Step("identity", ApplyIdentity);
            Step("orientation", ApplyOrientation);
            Step("splash", ApplySplash);
            Step("brand textures", ApplyBrandImports);
            Step("icons", ApplyIcons);
            Step("platforms", ApplyPlatforms);
            AssetDatabase.SaveAssets();
        }

        private static void Step(string name, Action action)
        {
            try { action(); }
            catch (Exception e) { Debug.LogWarning("Arrow Escape release setup (" + name + "): " + e.Message); }
        }

        private static void ApplyIdentity()
        {
            if (PlayerSettings.productName != ProductName) PlayerSettings.productName = ProductName;
            if (PlayerSettings.companyName != CompanyName) PlayerSettings.companyName = CompanyName;
            if (PlayerSettings.bundleVersion == "1.0" || string.IsNullOrEmpty(PlayerSettings.bundleVersion)) PlayerSettings.bundleVersion = "0.1.0";
            foreach (NamedBuildTarget target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone })
                if (PlayerSettings.GetApplicationIdentifier(target) != ApplicationId) PlayerSettings.SetApplicationIdentifier(target, ApplicationId);
        }

        private static void ApplyOrientation()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        /// <summary>Release version shown in stores and in the APK.</summary>
        public const string Version = "0.1.0";
        /// <summary>Android version code (increase for every upload).</summary>
        public const int AndroidVersionCode = 1;

        // Step 14N cross-platform settings: Android IL2CPP ARM64 with internet (world ranking) and edge-to-edge drawing,
        // iOS full screen, desktop portrait windows that can be resized.
        private static void ApplyPlatforms()
        {
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = AndroidVersionCode;
            PlayerSettings.iOS.buildNumber = AndroidVersionCode.ToString();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.startInFullscreen = true;
            if ((int)PlayerSettings.Android.minSdkVersion < 26) PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.runInBackground = false;
        }

        private static void ApplySplash()
        {
            PlayerSettings.SplashScreen.backgroundColor = new Color32(5, 6, 26, 255);
            try { PlayerSettings.SplashScreen.showUnityLogo = false; } catch (Exception) { /* license may require the logo */ }
        }

        private static void ApplyBrandImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { BrandFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool changed = false;
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                if (importer.npotScale != TextureImporterNPOTScale.None) { importer.npotScale = TextureImporterNPOTScale.None; changed = true; }
                if (importer.textureCompression != TextureImporterCompression.CompressedHQ) { importer.textureCompression = TextureImporterCompression.CompressedHQ; changed = true; }
                if (importer.maxTextureSize < 2048) { importer.maxTextureSize = 2048; changed = true; }
                if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static Texture2D Icon(string file, bool transparent, int maxSize)
        {
            string path = IconFolder + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            bool changed = false;
            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.npotScale != TextureImporterNPOTScale.None) { importer.npotScale = TextureImporterNPOTScale.None; changed = true; }
            if (importer.alphaIsTransparency != transparent) { importer.alphaIsTransparency = transparent; changed = true; }
            if (importer.maxTextureSize != maxSize) { importer.maxTextureSize = maxSize; changed = true; }
            if (changed) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ApplyIcons()
        {
            Texture2D main = Icon("icon_1024.png", false, 1024);
            Texture2D background = Icon("android_adaptive_background.png", false, 512);
            Texture2D foreground = Icon("android_adaptive_foreground.png", true, 512);
            if (main == null) { Debug.LogWarning("Arrow Escape release setup: icon_1024.png is missing."); return; }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { main }, IconKind.Any);
            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                bool adaptive = kind.ToString().IndexOf("Adaptive", StringComparison.OrdinalIgnoreCase) >= 0;
                foreach (PlatformIcon icon in icons)
                {
                    if (adaptive && icon.maxLayerCount >= 2 && background != null && foreground != null) icon.SetTextures(background, foreground);
                    else icon.SetTexture(main);
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
            }
            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                foreach (PlatformIcon icon in icons) icon.SetTexture(main);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
            }
        }
    }
}
