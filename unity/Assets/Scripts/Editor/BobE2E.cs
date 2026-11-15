// Battle of the Brooms: the command-line / menu entry for the end-to-end run (docs 24 section 15.17). It opens the Game
// scene, waits for the editor to stop recompiling, enters Play mode and lets BobE2EDriver press its way through the
// shipped flow; the driver's `[E2E] E2E PASSED / E2E FAILED` line becomes the exit code. The wallet the run spends is
// snapshotted here and put back afterwards, so a test never leaves the developer's profile richer.
//   Unity.exe -projectPath <proj> -executeMethod Bob.EditorTools.BobE2E.Run -logFile <log>   (no -batchmode: needs a Game view)
using System.Collections.Generic;
using Bob.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobE2E
    {
        const string ExitKey = "Bob.E2E.Exit";
        static readonly string[] Wallet =
        {
            "bob.guest", "bob.coins", "bob.xp", "bob.taskRace.best", "bob.taskRace.matches", "bob.taskRace.wins",
            "bob.story.lossStreak",
            "bob.story.stars.1", "bob.story.stars.2", "bob.story.stars.3",
            "bob.story.stars.4", "bob.story.stars.5", "bob.story.stars.6",
        };

        static readonly Dictionary<string, int> before = new Dictionary<string, int>();
        static double quietFrom, deadline;

        [MenuItem("Bob/Run E2E")]
        public static void RunFromMenu() { Begin(false); }

        public static void Run() { Begin(true); }

        static void Begin(bool exit)
        {
            before.Clear();
            foreach (var key in Wallet) before[key] = PlayerPrefs.GetInt(key, 0);
            // The login screen is the first screen of the flow: a leftover guest flag would skip S02.
            PlayerPrefs.SetInt("bob.guest", 0);
            PlayerPrefs.Save();

            SessionState.SetBool(BobE2EDriver.ArmKey, true);
            SessionState.SetBool(ExitKey, exit);
            ApplyViewSize();
            EditorSceneManager.OpenScene(BobGameScene.ScenePath);
            quietFrom = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }

        /// <summary>BOB_VIEW=2400x1080 (phone aspect): the same reflection trick BobGameScene uses; no public API for it.</summary>
        static void ApplyViewSize()
        {
            var env = System.Environment.GetEnvironmentVariable("BOB_VIEW"); if (string.IsNullOrEmpty(env)) return;
            var wh = env.Split('x'); if (wh.Length != 2 || !int.TryParse(wh[0], out var w) || !int.TryParse(wh[1], out var h)) return;
            try
            {
                var T = typeof(Editor).Assembly;
                var sizesType = T.GetType("UnityEditor.GameViewSizes"); var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var inst = single.GetProperty("instance").GetValue(null);
                var group = sizesType.GetMethod("GetGroup").Invoke(inst, new object[] { 0 });
                var gt = group.GetType();
                var sizeType = T.GetType("UnityEditor.GameViewSize"); var kind = T.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { kind, typeof(int), typeof(int), typeof(string) });
                var size = ctor.Invoke(new object[] { System.Enum.Parse(kind, "FixedResolution"), w, h, "Bob " + env });
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                int index = (int)gt.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                var gv = T.GetType("UnityEditor.GameView"); var win = EditorWindow.GetWindow(gv);
                gv.GetMethod("SizeSelectionCallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(win, new object[] { index, size });
                Debug.Log("[E2E] Game view set to " + env);
            }
            catch (System.Exception e) { Debug.LogWarning("[E2E] could not set the Game view size: " + e.Message); }
        }

        /// <summary>Entering Play mode reloads the domain and drops the update hook, so arm it again from SessionState.</summary>
        [InitializeOnLoadMethod]
        static void Rehook()
        {
            if (!SessionState.GetBool(BobE2EDriver.ArmKey, false)) return;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            deadline = EditorApplication.timeSinceStartup + 900;
            EditorApplication.update -= Watchdog; EditorApplication.update += Watchdog;
            quietFrom = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }

        /// <summary>The editor changes define symbols when it starts, which recompiles: wait for a quiet 12 s, then play.</summary>
        static void Tick()
        {
            if (EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) quietFrom = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - quietFrom < 12) return;
            EditorApplication.update -= Tick;
            EditorApplication.EnterPlaymode();
        }

        static void Watchdog()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Debug.LogError("[E2E] TIMEOUT"); Finish(1); }
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (msg.Contains("E2E PASSED")) Finish(0);
            else if (msg.Contains("E2E FAILED") || msg.Contains("E2E] TIMEOUT")) Finish(1);
            else if (type == LogType.Exception && stack.Contains("Bob.")) { Debug.LogError("[E2E] exception: " + msg); Finish(1); }
        }

        static void Finish(int code)
        {
            Application.logMessageReceived -= OnLog;
            EditorApplication.update -= Watchdog;
            EditorApplication.update -= Tick;
            SessionState.SetBool(BobE2EDriver.ArmKey, false);
            bool exit = SessionState.GetBool(ExitKey, false);
            SessionState.SetBool(ExitKey, false);
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () =>
            {
                foreach (var kv in before) PlayerPrefs.SetInt(kv.Key, kv.Value);
                PlayerPrefs.Save();
                Debug.Log($"[E2E] wallet restored, exit code {code}");
                if (exit) EditorApplication.Exit(code);
            };
        }
    }
}
