// Battle of the Brooms: Android player settings and a one-command debug APK build.
//   Unity.exe -batchmode -quit -projectPath unity -executeMethod Bob.EditorTools.BobAndroidBuild.BuildApk
// The APK lands in ../Builds/BOB-prototype-v1.apk. Needs Android Build Support (SDK, NDK, OpenJDK) from Unity Hub.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobAndroidBuild
    {
        public const string Package = "com.bob.battleofthebrooms";

        [MenuItem("Bob/Android/Apply Player Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "BOB";
            PlayerSettings.productName = "Battle of the Brooms";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, Package);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed; // plain http to the PC's LAN Nakama
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.Android.forceInternetPermission = true;
            KeepShaders("Standard", "Unlit/Texture"); // the 2.5D room creates these materials in code, so a player build must not strip them
            Debug.Log("Bob: Android player settings applied (" + Package + ", IL2CPP, ARM64, landscape).");
        }

        static void KeepShaders(params string[] names)
        {
            var gs = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) { Debug.LogWarning("Bob: GraphicsSettings.asset not found; shaders may be stripped"); return; }
            var so = new SerializedObject(gs); var arr = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (var n in names)
            {
                var sh = Shader.Find(n); if (sh == null) { Debug.LogWarning("Bob: shader not found: " + n); continue; }
                bool has = false;
                for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
                if (has) continue;
                arr.arraySize++; arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            }
            so.ApplyModifiedProperties(); AssetDatabase.SaveAssets();
        }

        [MenuItem("Bob/Android/Build APK")]
        public static void BuildApk()
        {
            Apply();
            var scenes = new[] { "Assets/Scenes/Game.unity", "Assets/Scenes/Coop.unity" }.Where(File.Exists).ToArray();
            Directory.CreateDirectory("../Builds");
            var path = Path.GetFullPath("../Builds/BOB-prototype-v1.apk");
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = path, target = BuildTarget.Android, options = BuildOptions.None
            });
            var s = report.summary;
            Debug.Log($"Bob: APK build {s.result}, {s.totalErrors} errors, {s.totalSize / 1048576} MB -> {path}");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
