// Battle of the Brooms: builds Assets/Scenes/Coop.unity, the online co-op scene (Coop/BobCoopGame.cs).
//   Menu Bob > Build Coop Scene   (builds the art library + Game scene first if they are missing, refreshes the audio prefab
//                                  so new sounds in Assets/Audio/SFX are included, then adds Coop after Game in Build Settings)
// Kept apart from BobGameScene.cs so the single-player scene and this one can change independently.
// UNCOMPILED IN UNITY when written (cloud session): only type-checked with unity-typecheck/.
using System.IO;
using System.Linq;
using Bob.Coop;
using Bob.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobCoopScene
    {
        public const string ScenePath = "Assets/Scenes/Coop.unity";
        const string LibraryPath = "Assets/Art/BobArtLibrary.asset", AudioPrefab = "Assets/Art/BobAudio.prefab", GameScene = "Assets/Scenes/Game.unity";

        // Bot test: start the other players first (cd server && node mp-host.js 2 ../unity/Logs/bob-mp-match.txt).
        // Command line (no -batchmode, needs a Game view): -executeMethod Bob.EditorTools.BobCoopScene.RunTest
        [MenuItem("Bob/Run Coop Test")]
        public static void RunTestFromMenu() { SessionState.SetBool("Bob.CoopExit", false); BeginTest(); }

        public static void RunTest() { SessionState.SetBool("Bob.CoopExit", true); BeginTest(); }

        static double deadline;

        // Test at another screen size: BOB_VIEW=1920x1080 (or 2400x1080, 1440x1080) before starting Unity. Uses the Game view's
        // fixed-resolution list through reflection (no public API for it).
        static void ApplyViewSize()
        {
            var env = System.Environment.GetEnvironmentVariable("BOB_VIEW"); if (string.IsNullOrEmpty(env)) return;
            var wh = env.Split('x'); if (wh.Length != 2 || !int.TryParse(wh[0], out var w) || !int.TryParse(wh[1], out var h)) return;
            try
            {
                var T = typeof(Editor).Assembly;
                var sizesType = T.GetType("UnityEditor.GameViewSizes"); var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var inst = single.GetProperty("instance").GetValue(null);
                var group = sizesType.GetMethod("GetGroup").Invoke(inst, new object[] { 0 }); // Standalone
                var gt = group.GetType();
                var sizeType = T.GetType("UnityEditor.GameViewSize"); var kind = T.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { kind, typeof(int), typeof(int), typeof(string) });
                var size = ctor.Invoke(new object[] { System.Enum.Parse(kind, "FixedResolution"), w, h, "Bob " + env });
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                int index = (int)gt.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                var gv = T.GetType("UnityEditor.GameView"); var win = EditorWindow.GetWindow(gv);
                gv.GetMethod("SizeSelectionCallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(win, new object[] { index, size });
                Debug.Log("[BobCoopTest] Game view set to " + env);
            }
            catch (System.Exception e) { Debug.LogWarning("[BobCoopTest] could not set the Game view size: " + e.Message); }
        }

        static void BeginTest()
        {
            ApplyViewSize();
            if (!File.Exists(ScenePath)) Build();
            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetBool(BobCoopGame.AutoKey, true);
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            deadline = EditorApplication.timeSinceStartup + 300;
            EditorApplication.update -= Watchdog; EditorApplication.update += Watchdog;
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (!SessionState.GetBool(BobCoopGame.AutoKey, false)) return;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            deadline = EditorApplication.timeSinceStartup + 300;
            EditorApplication.update -= Watchdog; EditorApplication.update += Watchdog;
        }

        static void Watchdog() { if (EditorApplication.timeSinceStartup > deadline) { Debug.LogError("[BobCoopTest] TIMEOUT"); Finish(1); } }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (msg.StartsWith("COOP TEST PASSED")) Finish(0);
            else if (msg.StartsWith("COOP TEST FAILED")) Finish(1);
            else if (type == LogType.Exception && stack.Contains("Bob.")) { Debug.LogError("[BobCoopTest] exception in game code: " + msg); Finish(1); }
        }

        static void Finish(int code)
        {
            Application.logMessageReceived -= OnLog; EditorApplication.update -= Watchdog;
            SessionState.SetBool(BobCoopGame.AutoKey, false);
            bool exit = SessionState.GetBool("Bob.CoopExit", false); SessionState.SetBool("Bob.CoopExit", false);
            EditorApplication.ExitPlaymode();
            if (exit) EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }

        [MenuItem("Bob/Build Coop Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<BobArtLibrary>(LibraryPath) == null)
                EditorApplication.ExecuteMenuItem("Bob/Build Game Scene"); // makes the art library (and the Game scene)
            EditorApplication.ExecuteMenuItem("Bob/Art/Build Audio Prefab"); // picks up every clip in Assets/Audio/SFX
            var lib = AssetDatabase.LoadAssetAtPath<BobArtLibrary>(LibraryPath);
            if (lib == null) { Debug.LogError("Bob: no art library at " + LibraryPath + ". Run Bob > Build Game Scene, then try again."); return; }

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 3.2f; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0xE9, 0xDD, 0xB8, 0xFF); camGo.transform.position = new Vector3(4, 2.8f, -10);

            var game = new GameObject("BobCoopGame").AddComponent<BobCoopGame>();
            game.art = lib;
            var audio = AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefab);
            if (audio != null) PrefabUtility.InstantiatePrefab(audio);

            EditorSceneManager.SaveScene(scene, ScenePath);
            // Game stays first (it is the start scene); Coop right after it.
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            int at = list.FindIndex(s => s.path == GameScene);
            list.Insert(at < 0 ? list.Count : at + 1, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Bob: " + ScenePath + " built. Start the server (start-server.ps1), press Play here, and play a second copy " +
                      "(a build, or ParrelSync / Multiplayer Play Mode) to join the same room code.");
        }
    }
}
