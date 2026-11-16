// Battle of the Brooms: Android player settings and a one-command debug APK build.
//   Unity.exe -batchmode -quit -projectPath unity -executeMethod Bob.EditorTools.BobAndroidBuild.BuildApk
// Optional CLI flags (both before or after -executeMethod):
//   -bobVersionCode N   use this AndroidBundleVersionCode (default: current + 1)
//   -bobNoBump          keep the current versionCode
//   -bobVersionName X   bump the versionName (default: keep the project value)
// The APK lands in ../Builds/BOB-prototype-v1.apk. Needs Android Build Support (SDK, NDK, OpenJDK) from Unity Hub.
// A failed build restores the previous versionCode and the Editor's platform, so a retry never burns a release number.
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
        const string ApkPath = "../Builds/BOB-prototype-v1.apk";

        [MenuItem("Bob/Android/Apply Player Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "BOB";
            PlayerSettings.productName = "Battle of the Brooms";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, Package);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.Android.forceInternetPermission = true;
            KeepShaders("Standard", "Unlit/Texture"); // the 2.5D room creates these materials in code, so a player build must not strip them
            Debug.Log("Bob: Android player settings applied (" + Package + ", IL2CPP, ARM64, landscape).");
        }

        static void KeepShaders(params string[] names)
        {
            // ProjectSettings assets are not in the AssetDatabase path table, so LoadAssetAtPath returns null there.
            var found = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            var gs = found != null && found.Length > 0 ? found[0] : null;
            if (gs == null) { Debug.LogWarning("Bob: GraphicsSettings.asset not found; shaders may be stripped"); return; }
            var so = new SerializedObject(gs); var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) { Debug.LogWarning("Bob: m_AlwaysIncludedShaders not found; shaders may be stripped"); return; }
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

        [MenuItem("Bob/Android/Switch Editor Back To Windows")]
        public static void BackToWindows()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }

        // ---------------------------------------------------------------- CLI flags

        static string FlagValue(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return null;
        }

        static bool HasFlag(string flag) => System.Environment.GetCommandLineArgs().Contains(flag);

        // ---------------------------------------------------------------- build

        [MenuItem("Bob/Android/Build APK")]
        public static void BuildApk()
        {
            int previousCode = PlayerSettings.Android.bundleVersionCode;
            string previousName = PlayerSettings.bundleVersion;
            bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
            bool bumped = false, built = false, named = false;
            int exit = 1;
            try
            {
                Apply();

                string name = FlagValue("-bobVersionName");
                if (!string.IsNullOrEmpty(name) && name != PlayerSettings.bundleVersion)
                {
                    PlayerSettings.bundleVersion = name;
                    named = true;
                    Debug.Log("Bob: versionName -> " + name);
                }

                string asked = FlagValue("-bobVersionCode");
                int code = 0;
                if (!string.IsNullOrEmpty(asked)) int.TryParse(asked, out code);
                if (code <= 0 && !HasFlag("-bobNoBump")) code = previousCode + 1;
                if (code > 0 && code != previousCode)
                {
                    PlayerSettings.Android.bundleVersionCode = code;
                    bumped = true;
                }
                Debug.Log($"Bob: building versionCode {PlayerSettings.Android.bundleVersionCode} ({PlayerSettings.bundleVersion})");

                // A dropped clip is moved to Resources by a deferred editor call that batchmode never reaches before -quit.
                int clips = BobVideoImportSettings.MovePending();
                Debug.Log($"Bob: {clips} video clip(s) moved to Resources/Video before the build.");

                // The lobby shell and the one match scene of docs 24. Coop is retired: the APK must not boot a mode
                // whose rules no longer exist.
                var scenes = new[] { "Assets/Scenes/Game.unity", "Assets/Scenes/TaskRace.unity" }.Where(File.Exists).ToArray();
                if (scenes.Length == 0) throw new BuildFailedException("Bob: no scene found to build");

                SwitchToAndroid();
                Directory.CreateDirectory("../Builds");
                var path = Path.GetFullPath(ApkPath);
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes, locationPathName = path, target = BuildTarget.Android, options = BuildOptions.None
                });
                var s = report.summary;
                built = s.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
                Debug.Log($"Bob: APK build {s.result}, {s.totalErrors} errors, {s.totalSize / 1048576} MB -> {path}");
                exit = built ? 0 : 1;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Bob: build failed - " + e);
                built = false;
                exit = 1;
            }
            finally
            {
                // A failed build must not burn a release number or leave the project settings dirty.
                if (!built)
                {
                    if (bumped) PlayerSettings.Android.bundleVersionCode = previousCode;
                    if (named) PlayerSettings.bundleVersion = previousName;
                    if (bumped || named)
                        Debug.LogWarning($"Bob: build failed; versionCode/name restored to {previousCode} ({previousName})");
                }
                EditorUserBuildSettings.buildAppBundle = previousAppBundle;
                BackToWindows(); // a build leaves the Editor on Android; the next Play session would then recompile (define symbols change) and kill the bot test
            }

            if (Application.isBatchMode) EditorApplication.Exit(exit);
        }

        static void SwitchToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) return;
            Debug.Log("Bob: switching the active build target to Android (a clean clone starts on Standalone)");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Bob: could not switch the active build target to Android (Android Build Support installed?)");
        }
    }
}
