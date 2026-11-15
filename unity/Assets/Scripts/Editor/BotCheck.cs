// Check for the solo Team B bot (docs 24 §15.7 Phase 4). Opens the solo scene, enters Play, has the bot clean one own
// station, then opens the bridges and has it cross to sabotage a clean Team A station.
//   -executeMethod Bob.EditorTools.BotCheck.Run     (Editor closed on this project)
// Success = the Console shows "[BotCheck] BOT CHECK PASSED".
using System.Linq;
using Bob.TaskRace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BotCheck
    {
        const string ArmKey = "Bob.BotCheck.Arm";

        [MenuItem("Bob/Run Bot Check")]
        public static void Run()
        {
            cleaned = crossed = sabotaged = false;
            step = 0; nextLog = 0f; deadline = 0f;
            SessionState.SetBool(ArmKey, true);
            EditorSceneManager.OpenScene(TaskRaceSceneBuilder.ScenePath);
            quietFrom = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static double quietFrom;
        static bool entered;
        static int step;
        static float nextLog, deadline, startedAt, stepAt;
        static TaskRaceBotController bot;
        static TaskRaceModeManager manager;
        static Vector3 botStart;
        static bool cleaned, crossed, sabotaged;

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

            manager = TaskRaceModeManager.Instance;
            if (manager == null) return;

            // Step 0: pick the bot and send it to a dirty station of its own island.
            if (step == 0 && Time.timeSinceLevelLoad > 1.5f)
            {
                bot = Object.FindAnyObjectByType<TaskRaceBotController>();
                if (bot == null) { Fail("the solo scene has no TaskRaceBotController."); return; }
                botStart = bot.transform.position;
                bool working = bot.DebugWorkNow();
                Debug.Log($"[BotCheck] start: {bot.JobName} at ({botStart.x:0.0},{botStart.z:0.0}) · " +
                          $"work job started {working} · bridges open {manager.AreBridgesOpen}");
                startedAt = Time.time;
                deadline = Time.time + 20f;
                nextLog = 0f;
                step = 1;
            }
            // Step 1: wait for the first clean (the walk plus the station time), then open the bridges for the raid.
            else if (step == 1)
            {
                if (Time.time > nextLog)
                {
                    Debug.Log($"[BotCheck] cleaning: {bot.JobName} at ({bot.transform.position.x:0.0},{bot.transform.position.z:0.0}) · " +
                              $"clean {bot.CleanCount}, {Time.time - startedAt:0.0}s");
                    nextLog = Time.time + 1.5f;
                }
                if (bot.CleanCount > 0)
                {
                    cleaned = true;
                    // A raid needs a clean enemy station: both sides start dirty, so finish one on Team A first.
                    var victim = Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude)
                        .Where(s => s.OwnerTeam == TeamId.TeamA && s.HasTask && s.State == SpotState.Dirty)
                        .OrderBy(s => s.SpotId).FirstOrDefault();
                    if (victim == null) { Fail("no dirty Team A station to mark clean for the raid."); return; }
                    victim.ForceComplete();
                    manager.DebugOpenBridges();
                    bool raiding = bot.DebugRaidNow();
                    Debug.Log($"[BotCheck] cleaned {bot.CleanCount}; Team A {victim.SpotId} set clean; " +
                              $"bridges open {manager.AreBridgesOpen} · raid job started {raiding}");
                    if (!raiding) { Fail("the bot did not accept the raid job."); return; }
                    deadline = Time.time + 25f;
                    nextLog = 0f;
                    step = 2;
                }
                else if (Time.time > deadline) Fail("the bot did not finish a clean within 20 s.");
            }
            // Step 2: watch it cross the bridge (x flips to Team A) and finish the sabotage.
            else if (step == 2)
            {
                if (bot.transform.position.x < 0f) crossed = true;
                if (bot.SabotageCount > 0) sabotaged = true;
                if (Time.time > nextLog)
                {
                    Debug.Log($"[BotCheck] raiding: {bot.JobName} at ({bot.transform.position.x:0.0},{bot.transform.position.z:0.0}) · " +
                              $"crossed {crossed} · sabotage {bot.SabotageCount}");
                    nextLog = Time.time + 1f;
                }
                if (sabotaged || Time.time > deadline) Done();
            }
            else if (step == 3 && Time.unscaledTime > stepAt + 1f)
            {
                bool ok = cleaned && crossed && sabotaged;
                Debug.Log(ok ? "[BotCheck] BOT CHECK PASSED"
                             : $"[BotCheck] BOT CHECK FAILED: cleaned {cleaned}, crossed {crossed}, sabotaged {sabotaged}");
                SessionState.SetBool(ArmKey, false);
                EditorApplication.update -= Tick;
                EditorApplication.ExitPlaymode();
                EditorApplication.delayCall += () => EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        static void Done()
        {
            bool ok = cleaned && crossed && sabotaged;
            Debug.Log($"[BotCheck] bot after {Time.timeSinceLevelLoad:0.0}s: moved {(bot.transform.position - botStart).magnitude:0.00} u · " +
                      $"cleaned {cleaned}, crossed {crossed}, sabotaged {sabotaged} ({bot.SabotageCount})");
            if (!ok) Debug.LogWarning($"[BotCheck] unfinished: cleaned {cleaned}, crossed {crossed}, sabotaged {sabotaged}");
            step = 3;
            stepAt = Time.unscaledTime;
        }

        static void Fail(string why)
        {
            Debug.Log($"[BotCheck] BOT CHECK FAILED: {why}");
            SessionState.SetBool(ArmKey, false);
            EditorApplication.update -= Tick;
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(1);
        }
    }
}
