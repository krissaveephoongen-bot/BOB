// Battle of the Brooms: runs BobSmokeTest in real Play mode without clicks, then quits with exit code 0 (pass) or 1.
// Unity.exe -projectPath <proj> -executeMethod Bob.EditorTools.BobAutoSmoke.Run -logFile <log>   (no -batchmode, no -quit)
// Also available as menu: Bob > Run Smoke Test.
using Bob.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobAutoSmoke
    {
        const string ScenePath = "Assets/Scenes/SmokeTest.unity";
        const string AutoKey = "Bob.AutoSmoke.Exit";
        static double deadline;

        [MenuItem("Bob/Run Smoke Test")]
        public static void RunFromMenu() { SessionState.SetBool(AutoKey, false); Begin(); }

        public static void Run() { SessionState.SetBool(AutoKey, true); Begin(); }

        static void Begin()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("BobSmokeTest").AddComponent<BobSmokeTest>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Hook();
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (!SessionState.GetBool(AutoKey, false)) return;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update -= Watchdog;
            EditorApplication.update += Watchdog;
        }

        static void Watchdog()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Debug.LogError("[BobAutoSmoke] TIMEOUT"); Finish(1); }
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (msg.Contains("SMOKE TEST PASSED")) Finish(0);
            else if (msg.Contains("SMOKE TEST FAILED") || msg.Contains("SMOKE TEST: client and server rules differ")) Finish(1);
        }

        static void Finish(int code)
        {
            Application.logMessageReceived -= OnLog;
            EditorApplication.update -= Watchdog;
            SessionState.SetBool(AutoKey, false);
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
