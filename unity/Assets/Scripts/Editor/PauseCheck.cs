// Check for the pause / exit card of docs 24 §15.3 A1. The edit-mode verify can only show the card; that the clock
// really stands still and then runs again needs Play mode, so this is the same arm/enter pattern as StorySeriesCheck:
//   -executeMethod Bob.EditorTools.PauseCheck.Run     (Editor closed on this project, no -batchmode -quit)
// Success = "[PauseCheck] PAUSE CHECK PASSED"; shots go to unity/Logs/pausecheck-*.png.
using System.IO;
using Bob.TaskRace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class PauseCheck
    {
        const string ArmKey = "Bob.PauseCheck.Arm";

        [MenuItem("Bob/Run Pause Check")]
        public static void Run()
        {
            SessionState.SetBool(ArmKey, true);
            EditorSceneManager.OpenScene(TaskRaceSceneBuilder.ScenePath);
            quietFrom = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        /// <summary>Entering Play mode reloads the domain and drops the update hook, so arm it again from SessionState.</summary>
        [InitializeOnLoadMethod]
        static void Rehook()
        {
            if (!SessionState.GetBool(ArmKey, false)) return;
            if (EditorApplication.isPlaying) entered = true;
            else quietFrom = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static double quietFrom;
        static bool entered;
        static int step;
        static float stepAt; // unscaled: Time.timeScale is 0 while the card is up
        static float clockBefore;
        static bool frozenOk, resumedOk, refusedOk;

        static void Tick()
        {
            if (!SessionState.GetBool(ArmKey, false)) { EditorApplication.update -= Tick; return; }

            if (!entered)
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) quietFrom = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - quietFrom < 12) return;
                entered = true;
                EditorApplication.EnterPlaymode();
                return;
            }
            if (!EditorApplication.isPlaying) return;

            var manager = TaskRaceModeManager.Instance;
            var hud = Object.FindAnyObjectByType<BobTaskRaceHud>();
            if (manager == null || hud == null) return;

            if (step == 0 && Time.timeSinceLevelLoad > 2f)
            {
                clockBefore = manager.RemainingSeconds;
                Debug.Log($"[PauseCheck] start: clock {clockBefore:0.00} s · timeScale {Time.timeScale} · " +
                          $"button visible {hud.PauseButtonVisible}");
                Capture("1-button");
                hud.DebugShowPause();
                stepAt = Time.unscaledTime;
                step = 1;
            }
            else if (step == 1 && Time.unscaledTime > stepAt + 0.8f)
            {
                frozenOk = Mathf.Approximately(Time.timeScale, 0f) && manager.Paused &&
                           Mathf.Abs(manager.RemainingSeconds - clockBefore) < 0.001f;
                Debug.Log($"[PauseCheck] paused: card {hud.PauseOpen} · timeScale {Time.timeScale:0.00} · " +
                          $"clock {manager.RemainingSeconds:0.00} s (was {clockBefore:0.00}, 0.8 real s later) · " +
                          $"button hidden {!hud.PauseButtonVisible} · frozen {frozenOk}");
                Capture("2-paused");
                hud.DebugClosePause();
                stepAt = Time.unscaledTime;
                step = 2;
            }
            else if (step == 2 && Time.unscaledTime > stepAt + 0.8f)
            {
                resumedOk = Mathf.Approximately(Time.timeScale, 1f) && !manager.Paused &&
                            manager.RemainingSeconds < clockBefore - 0.2f;
                Debug.Log($"[PauseCheck] resumed: card {hud.PauseOpen} · timeScale {Time.timeScale:0.00} · " +
                          $"clock {manager.RemainingSeconds:0.00} s (was {clockBefore:0.00}) · running {resumedOk}");
                // A card owns a TASK_LOCK and ticks on unscaled time: pause must refuse until the card is closed.
                var spot = FindDirtyOwnSpot();
                bool opened = spot != null && spot.TryOpen("pause-check", out var why);
                refusedOk = opened && manager.CardOpen && !manager.SetPaused(true) && Mathf.Approximately(Time.timeScale, 1f);
                Debug.Log($"[PauseCheck] card open blocks pause: spot {(spot != null ? spot.SpotId : "none")} " +
                          $"opened {opened} · CardOpen {manager.CardOpen} · paused {manager.Paused} · " +
                          $"timeScale {Time.timeScale:0.00} · refused {refusedOk}");
                if (opened) spot.Close(false);
                stepAt = Time.unscaledTime;
                step = 3;
            }
            else if (step == 3 && Time.unscaledTime > stepAt + 0.4f)
            {
                bool ok = frozenOk && resumedOk && refusedOk && Mathf.Approximately(Time.timeScale, 1f) && !manager.Paused;
                Debug.Log(ok ? "[PauseCheck] PAUSE CHECK PASSED" : "[PauseCheck] PAUSE CHECK FAILED");
                SessionState.SetBool(ArmKey, false);
                EditorApplication.update -= Tick;
                EditorApplication.ExitPlaymode();
                EditorApplication.delayCall += () => EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        static TaskSpot FindDirtyOwnSpot()
        {
            foreach (var spot in Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude))
                if (spot.OwnerTeam == TaskSpot.LocalTeam && spot.State == SpotState.Dirty && spot.HasTask) return spot;
            return null;
        }

        static void Capture(string name) =>
            ScreenCapture.CaptureScreenshot(Path.Combine("Logs", "pausecheck-" + name + ".png"));
    }
}
