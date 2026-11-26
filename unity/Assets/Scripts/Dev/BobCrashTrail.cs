// Battle of the Brooms: a synchronous trail of every Unity log line, so a phone that hard-exits can say where it died.
// The tester's device is not reachable over adb, and this crash is not a managed exception - the process just disappears
// when the match scene loads. Lines are flushed one write per line on purpose: a buffered writer loses the tail, which is
// the only part that matters. Development builds and the editor only.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bob.Dev
{
    [DisallowMultipleComponent]
    public class BobCrashTrail : MonoBehaviour
    {
        const int ShowLines = 120;
        const int KeepLines = 900;
        const long RotateAtBytes = 900_000;
        const int NativeLines = 140;
        const int RunLines = 160;

        static readonly string[] NativeKeys = {
            "Fatal signal", "SIGSEGV", "SIGABRT", "Abort message", "backtrace", "#0", "libc", "DEBUG",
            "libunity", "libil2cpp", "mali", "vulkan", "GLES", "lowmemorykiller", "am_crash", "ANR",
        };

        static readonly string[] KillKeys = {
            "am_kill", "am_proc_died", "am_anr", "am_crash", "anr", "watchdog", "lowmemorykiller", "lmkd",
        };

        static readonly object Gate = new object();
        static BobCrashTrail live;
        static bool writeFailed;

        readonly List<string> lines = new List<string>();
        bool open, lastRunDied, nativeOnly, pullOnBoot, capturing;
        Vector2 scroll;
        int reported;
        static string filePath;

        static string FilePath => Path.Combine(Application.persistentDataPath, "bob-trail.txt");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (live != null) return;
            var go = new GameObject("BobCrashTrail");
            DontDestroyOnLoad(go);
            go.AddComponent<BobCrashTrail>();
        }

        void Awake()
        {
            live = this;
            // persistentDataPath is main-thread only, so the capture thread gets the path handed to it.
            filePath = FilePath;
            if (File.Exists(filePath))
            {
                lock (Gate)
                {
                    foreach (var l in File.ReadAllLines(filePath)) lines.Add(l);
                    lastRunDied = !EndsWithCleanExit();
                }
            }
            Application.logMessageReceivedThreaded += OnLog;
            SceneManager.sceneLoaded += (s, m) => Write("scene loaded " + s.name);
            Write("=== boot v" + Application.version + " " + DateTime.Now.ToString("MM-dd HH:mm:ss")
                  + " " + SystemInfo.deviceModel + " ram " + SystemInfo.systemMemorySize + "MB"
                  + " " + SystemInfo.operatingSystem
                  + (lastRunDied ? " | PREV RUN ENDED WITHOUT CLEAN EXIT" : ""));
            if (lastRunDied) pullOnBoot = true;
        }

        // Android lets a process read its own logcat entries without any permission, and the crash/events ring buffers
        // still hold the previous run - so a phone with no adb attached can report its own native crash on next boot.
        // System.Diagnostics.Process is not supported under IL2CPP on Android (it fails with "Native error= Success"),
        // so the child process starts through java.lang.ProcessBuilder over JNI instead - and that has to happen on the
        // main thread: a worker thread is not attached to the JVM, and v7's trail showed the JNI calls throwing
        // NullReferenceException there. One buffer per frame keeps the blocking reads out of a single frame.
        void Pull()
        {
            if (capturing || Application.platform != RuntimePlatform.Android) return;
            capturing = true;
            StartCoroutine(CaptureRoutine());
        }

        IEnumerator CaptureRoutine()
        {
            Write("[native] capture begin");
            yield return null;
            string dead = ReadLogcat(new[] { "logcat", "-d", "-b", "crash" }, "crash", null); yield return null;
            ReadLogcat(new[] { "logcat", "-d", "-b", "system", "-t", "400" }, "system", NativeKeys); yield return null;
            ReadLogcat(new[] { "logcat", "-d", "-b", "main", "-t", "700" }, "main", NativeKeys); yield return null;
            ReadLogcat(new[] { "logcat", "-d", "-b", "events", "-t", "700" }, "events", KillKeys); yield return null;
            // The tombstone says a Unity thread aborted, but NativeKeys above drops Unity's own "Fatal error" /
            // "Assertion" / "BUG" lines. The dead process's last words are still in the main buffer - find them by pid.
            if (dead != null)
            {
                ReadRun(new[] { "logcat", "-d", "-b", "main", "-t", "4000" }, "main", dead); yield return null;
                ReadRun(new[] { "logcat", "-d", "-b", "system", "-t", "2000" }, "system", dead);
            }
            Write("[native] capture end");
            capturing = false;
        }

        // Returns the crashed pid parsed from the tombstone header, or null.
        string ReadLogcat(string[] argv, string tag, string[] keys)
        {
            int exit;
            var all = RunLogcat(argv, tag, out exit);
            if (all == null) return null;
            var kept = new List<string>();
            string dead = null;
            foreach (var l in all)
            {
                // Last one wins: the crash buffer holds earlier tombstones too, and the newest is the death to chase.
                var d = DeadPid(l);
                if (d != null) dead = d;
                if (keys != null && !Matches(l, keys)) continue;
                kept.Add(l);
            }
            int n = Math.Min(NativeLines, kept.Count);
            Write("[native] " + tag + " kept " + n + " of " + kept.Count + " (exit " + exit + ")");
            for (int i = kept.Count - n; i < kept.Count; i++) Write("[native " + tag + "] " + kept[i]);
            return dead;
        }

        void ReadRun(string[] argv, string tag, string pid)
        {
            int exit;
            var all = RunLogcat(argv, tag, out exit);
            if (all == null) return;
            var mine = new List<string>();
            foreach (var l in all) if (IsPid(l, pid)) mine.Add(Body(l));
            int n = Math.Min(RunLines, mine.Count);
            Write("[native] " + tag + " lines of pid " + pid + ": " + n + " of " + mine.Count + " (exit " + exit + ")");
            for (int i = mine.Count - n; i < mine.Count; i++) Write("[native " + tag + "~] " + mine[i]);
        }

        List<string> RunLogcat(string[] argv, string tag, out int exit)
        {
            var all = new List<string>();
            exit = -1;
            try
            {
                using (var builder = new AndroidJavaObject("java.lang.ProcessBuilder", (object)argv))
                using (var proc = builder.Call<AndroidJavaObject>("start"))
                using (var stream = proc.Call<AndroidJavaObject>("getInputStream"))
                using (var reader = new AndroidJavaObject("java.io.InputStreamReader", stream))
                using (var buffered = new AndroidJavaObject("java.io.BufferedReader", reader))
                {
                    string line;
                    while ((line = buffered.Call<string>("readLine")) != null)
                    {
                        if (line.Length > 300) line = line.Substring(0, 300);
                        all.Add(line);
                        if (all.Count > 4200) all.RemoveRange(0, 1200);
                    }
                    exit = proc.Call<int>("waitFor");
                }
            }
            catch (Exception e) { Write("[native] " + tag + " failed " + e.GetType().Name + ": " + e.Message); return null; }
            return all;
        }

        // "pid: 26681, tid: 26795, name: Loading.Preload  >>> com.bob.battleofthebrooms <<<"
        static string DeadPid(string line)
        {
            int i = line.IndexOf("pid: ", StringComparison.Ordinal);
            if (i < 0) return null;
            i += 5; int j = i;
            while (j < line.Length && char.IsDigit(line[j])) j++;
            return j > i ? line.Substring(i, j - i) : null;
        }

        // logcat's default columns are "MM-DD HH:MM:SS PID TID LEVEL TAG: message".
        static string Field(string line, int index)
        {
            int i = 0;
            for (int f = 0; f < index; f++)
            {
                i = line.IndexOf(' ', i);
                if (i < 0) return null;
                i++;
            }
            int j = line.IndexOf(' ', i);
            return j < 0 ? line.Substring(i) : line.Substring(i, j - i);
        }

        static bool IsPid(string line, string pid) => Field(line, 2) == pid;

        static string Body(string line)
        {
            int i = 0;
            for (int f = 0; f < 5; f++)
            {
                i = line.IndexOf(' ', i);
                if (i < 0) return line;
                i++;
            }
            return line.Substring(i);
        }

        static bool Matches(string line, string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
                if (line.IndexOf(keys[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        void Update()
        {
            // The graphics device only exists after the first present, which is also what we need to know on a crash
            // report: whether the phone really picked GLES3.
            if (reported != 0 || Time.frameCount < 3) return;
            reported = Time.frameCount;
            Write("gfx " + SystemInfo.graphicsDeviceType + " | " + SystemInfo.graphicsDeviceName
                  + " | shaderLevel " + SystemInfo.graphicsShaderLevel + " | vram " + SystemInfo.graphicsMemorySize + "MB"
                  + " | screen " + Screen.width + "x" + Screen.height + "@" + Screen.dpi);
            if (pullOnBoot) { pullOnBoot = false; Pull(); }
        }

        void OnApplicationQuit() => Write("clean-exit");

        void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Log && condition.Length > 400) condition = condition.Substring(0, 400);
            Write(type + " " + condition.Replace("\n", " | "));
        }

        static void Write(string line)
        {
            lock (Gate)
            {
                live?.Track(line);
                if (filePath == null) return;
                try
                {
                    File.AppendAllText(filePath, line + "\n", Encoding.UTF8);
                    if (new FileInfo(filePath).Length > RotateAtBytes)
                    {
                        var all = File.ReadAllLines(filePath);
                        int keep = Math.Min(KeepLines, all.Length);
                        File.WriteAllText(filePath, string.Join("\n", all, all.Length - keep, keep) + "\n", Encoding.UTF8);
                    }
                }
                catch (Exception e)
                {
                    // The warning below re-enters OnLog -> Write, so report the failure only once.
                    if (writeFailed) return;
                    writeFailed = true;
                    Debug.LogWarning("[trail] write failed " + e.Message);
                }
            }
        }

        void Track(string line)
        {
            lines.Add(line);
            if (lines.Count > KeepLines) lines.RemoveRange(0, lines.Count - KeepLines);
        }

        bool EndsWithCleanExit()
        {
            for (int i = lines.Count - 1; i >= 0; i--)
                if (lines[i].StartsWith("clean-exit")) return true;
            return false;
        }

        void OnGUI()
        {
            var skin = GUI.skin;
            var chip = new Rect(Screen.width - 190, 8, 182, 56);
            if (GUI.Button(chip, (lastRunDied ? "LOG (ตาย)\n" : "LOG\n") + "v" + Application.version,
                           new GUIStyle(skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter }))
                open = !open;
            if (!open) return;

            var box = new Rect(8, 72, Screen.width - 16, Screen.height - 88);
            GUI.Label(new Rect(box.x + 8, box.y + 2, box.width - 420, 26),
                "trail " + filePath + "   (" + lines.Count + " lines)");
            if (GUI.Button(new Rect(box.x + box.width - 400, box.y + 2, 192, 28), capturing ? "READING..." : "PULL LOGCAT",
                           new GUIStyle(skin.button) { fontSize = 18 }))
                Pull();
            // The native lines are the whole point, but the newest Home logs push them off screen - a filter keeps them up.
            if (GUI.Button(new Rect(box.x + box.width - 200, box.y + 2, 192, 28), nativeOnly ? "ALL LINES" : "NATIVE ONLY",
                           new GUIStyle(skin.button) { fontSize = 18 }))
            { nativeOnly = !nativeOnly; scroll = Vector2.zero; }
            var inner = new Rect(box.x + 6, box.y + 30, box.width - 12, box.height - 36);
            var text = new StringBuilder();
            lock (Gate)
            {
                if (nativeOnly)
                {
                    var pick = new List<int>();
                    for (int i = lines.Count - 1; i >= 0 && pick.Count < ShowLines; i--)
                        if (lines[i].StartsWith("[native")) pick.Add(i);
                    for (int i = pick.Count - 1; i >= 0; i--) text.AppendLine(Clip(lines[pick[i]]));
                }
                else
                    for (int i = Math.Max(0, lines.Count - ShowLines); i < lines.Count; i++)
                        text.AppendLine(Clip(lines[i]));
            }
            var style = new GUIStyle(skin.label) { fontSize = 18, wordWrap = false, alignment = TextAnchor.UpperLeft };
            var measure = style.CalcHeight(new GUIContent(text.ToString()), inner.width);
            // Clipped lines keep the newest logcat frames on screen; a horizontal scroll offset was cutting their heads off.
            scroll.x = 0f;
            scroll = GUI.BeginScrollView(inner, scroll, new Rect(0, 0, inner.width - 20, measure + 20));
            GUI.Label(new Rect(2, 2, inner.width - 24, measure), text.ToString(), style);
            GUI.EndScrollView();
            if (Event.current.type == EventType.Repaint && measure > inner.height)
                scroll.y = measure - inner.height + 20;
        }

        static string Clip(string line) => line.Length <= 150 ? line : line.Substring(0, 147) + "...";
    }
}
#endif
