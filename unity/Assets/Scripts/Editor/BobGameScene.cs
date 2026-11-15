// Battle of the Brooms: builds Assets/Scenes/Game.unity (the playable single-player scene) and runs it as a test.
//   Menu Bob > Build Game Scene   art library from bob-art-manifest.json + scene + build settings
//   Menu Bob > Run Game Test      a bot plays one match in Play mode; Console shows GAME TEST PASSED
//   Command line (Editor must be closed on this project):
//     Unity.exe -batchmode -quit -projectPath <proj> -executeMethod Bob.EditorTools.BobGameScene.Build -logFile -
//     Unity.exe -projectPath <proj> -executeMethod Bob.EditorTools.BobGameScene.RunTest -logFile <log>   (no -batchmode: needs a Game view)
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bob.Art.EditorTools;
using Bob.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobGameScene
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";
        public const string LibraryPath = "Assets/Art/BobArtLibrary.asset";
        const string AudioPrefab = "Assets/Art/BobAudio.prefab";
        const string ExitKey = "Bob.AutoGame.Exit";

        [MenuItem("Bob/Build Game Scene")]
        public static void Build()
        {
            var lib = BuildLibrary();
            // Mobile player settings: landscape only, either way round.
            PlayerSettings.productName = "Battle of the Brooms";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefab) == null) BobArtMenu.BuildAudioPrefab();

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 3.2f; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0xE9, 0xDD, 0xB8, 0xFF); camGo.transform.position = new Vector3(4, 2.8f, -10);

            var game = new GameObject("BobGame").AddComponent<BobGame>();
            game.art = lib;

            var audio = AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefab);
            if (audio != null) PrefabUtility.InstantiatePrefab(audio);

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"Bob: {ScenePath} built ({lib.entries.Count} art entries). Press Play, or Bob > Run Game Test.");
        }

        /// <summary>One entry per manifest sprite, named by its file name (sweep_walk, part_soap, cart...).</summary>
        static BobArtLibrary BuildLibrary()
        {
            BobArtImporter.ClearCache();
            var m = BobArtImporter.Manifest;
            var lib = AssetDatabase.LoadAssetAtPath<BobArtLibrary>(LibraryPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<BobArtLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
            lib.entries = new List<BobArtLibrary.Entry>();
            if (m == null) { Debug.LogError("Bob: no " + BobArtImporter.ManifestPath); return lib; }
            int missing = 0;
            foreach (var s in m.sprites)
            {
                var png = BobArtImporter.ArtRoot + s.path + ".png";
                var sprites = AssetDatabase.LoadAllAssetsAtPath(png).OfType<Sprite>().ToList();
                if (sprites.Count == 0) { missing++; continue; }
                Sprite[] frames = sprites.Count == 1 ? sprites.ToArray()
                    : sprites.OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out var k) ? k : 0).ToArray();
                lib.entries.Add(new BobArtLibrary.Entry { name = Path.GetFileName(s.path), frames = frames, fps = s.fps, loop = s.loop });
            }
            if (missing > 0) Debug.LogWarning($"Bob: {missing} manifest sprites not imported yet (Bob > Art > Reimport Art, then build again)");
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return lib;
        }

        /// <summary>Rebuilds only the sprite table from the manifest, with no scene writes, after new art is imported.</summary>
        [MenuItem("Bob/Art/Rebuild Sprite Library")]
        public static void RebuildLibrary()
        {
            var lib = BuildLibrary();
            Debug.Log($"Bob: art library rebuilt ({lib.entries.Count} entries).");
        }

        /// <summary>SessionState outlives a batch run, so every entry point clears the modes it does not want;
        /// otherwise a leftover task flag silently changes what the next test does.</summary>
        static void ClearModes()
        {
            SessionState.SetBool("bob.taskbot", false);
        }

        [MenuItem("Bob/Run Game Test")]
        public static void RunTestFromMenu() { ClearModes(); SessionState.SetBool(ExitKey, false); BeginTest(); }

        public static void RunTaskTest() { ClearModes(); SessionState.SetBool(ExitKey, true); SessionState.SetBool("bob.taskbot", true); BeginTest(); }

        public static void RunTest() { ClearModes(); SessionState.SetBool(ExitKey, true); BeginTest(); }

        /// <summary>Test at another screen size: BOB_VIEW=2400x1080 (phone aspect) before starting Unity. Uses the Game view's
        /// fixed-resolution list through reflection (no public API for it).</summary>
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
                Debug.Log("[BobGameTest] Game view set to " + env);
            }
            catch (System.Exception e) { Debug.LogWarning("[BobGameTest] could not set the Game view size: " + e.Message); }
        }

        static void BeginTest()
        {
            ApplyViewSize();
            if (!File.Exists(ScenePath)) Build();
            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetBool(BobGame.AutoKey, true);
            SessionState.SetBool(WaitKey, true);
            Hook();
        }

        const string WaitKey = "Bob.AutoWaitPlay";
        static double settleFrom;

        /// <summary>The Editor changes define symbols when it starts (Sentis analytics), which recompiles scripts. Entering Play before that
        /// settles would reload the domain mid-test and kill the bot, so wait for a quiet 12 s, then play.</summary>
        static void StartSettleWaiter()
        {
            settleFrom = EditorApplication.timeSinceStartup;
            EditorApplication.update -= SettleTick; EditorApplication.update += SettleTick;
        }

        static void SettleTick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) settleFrom = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - settleFrom < 12) return;
            EditorApplication.update -= SettleTick;
            SessionState.SetBool(WaitKey, false);
            EditorApplication.EnterPlaymode();
        }

        static double deadline;

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (!SessionState.GetBool(BobGame.AutoKey, false)) return;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            deadline = EditorApplication.timeSinceStartup + 420;
            EditorApplication.update -= Watchdog; EditorApplication.update += Watchdog;
            if (SessionState.GetBool(WaitKey, false) && !EditorApplication.isPlaying) StartSettleWaiter();
        }

        static void Watchdog() { if (EditorApplication.timeSinceStartup > deadline) { Debug.LogError("[BobGameTest] TIMEOUT"); Finish(1); } }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (msg.StartsWith("GAME TEST PASSED")) Finish(0);
            else if (msg.StartsWith("GAME TEST FAILED")) Finish(1);
            else if (type == LogType.Exception && stack.Contains("Bob.")) { Debug.LogError("[BobGameTest] exception in game code: " + msg); Finish(1); }
        }

        static void Finish(int code)
        {
            Application.logMessageReceived -= OnLog;
            EditorApplication.update -= Watchdog;
            SessionState.SetBool(BobGame.AutoKey, false); SessionState.SetBool("bob.taskbot", false);
            bool exit = SessionState.GetBool(ExitKey, false);
            SessionState.SetBool(ExitKey, false);
            EditorApplication.ExitPlaymode();
            if (exit) EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
