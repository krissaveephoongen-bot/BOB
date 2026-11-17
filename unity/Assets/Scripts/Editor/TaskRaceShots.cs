// Captures the HUD pieces that only exist while a round is playing: the mate tray of Duo, the ping wheel, the emergency
// stain banner with its screen-edge arrow, and the T04 cart card. Edit-mode TaskRaceVerify cannot show them (the bots,
// the stains and the ping wheel all live behind Application.isPlaying), so this driver enters Play, walks the steps and
// leaves a PNG per step.
//   -executeMethod Bob.EditorTools.TaskRaceShots.Run     (Editor closed on this project; no -quit)
// Success = the Console shows "[TaskRaceShots] SHOTS PASSED" and the PNGs under Logs/.
using System;
using System.Linq;
using System.Reflection;
using Bob.TaskRace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class TaskRaceShots
    {
        const string ArmKey = "Bob.TaskRaceShots.Arm";

        static float armedAt, stepAt, yawWas;
        static int step;
        static bool entered, ok = true, seeded, spawnedOnce, arrowTurned, cardOpened;
        static TaskRaceModeManager manager;
        static TaskRaceRound round;
        static BobTaskRaceHud hud;

        [MenuItem("Bob/Run TaskRace Shots")]
        public static void Run()
        {
            step = 0;
            entered = false;
            ok = true;
            seeded = false;
            spawnedOnce = false;
            arrowTurned = false;
            cardOpened = false;
            yawWas = 0f;
            stepAt = 0f;
            manager = null;
            round = null;
            hud = null;
            SessionState.SetBool(ArmKey, true);
            EditorSceneManager.OpenScene(TaskRaceSceneBuilder.DuoScenePath);
            armedAt = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        /// <summary>Entering Play reloads the domain and drops the update hook, so arm it again from SessionState.</summary>
        [InitializeOnLoadMethod]
        static void Rehook()
        {
            if (!SessionState.GetBool(ArmKey, false)) return;
            if (EditorApplication.isPlaying) { entered = true; stepAt = 0f; }
            else armedAt = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(ArmKey, false)) { EditorApplication.update -= Tick; return; }

            if (!entered)
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    armedAt = (float)EditorApplication.timeSinceStartup;
                if ((float)EditorApplication.timeSinceStartup - armedAt < 10f) return;
                entered = true;
                EditorApplication.EnterPlaymode();
                return;
            }
            if (!EditorApplication.isPlaying || Time.realtimeSinceStartup < 1f) return;

            if (manager == null) manager = UnityEngine.Object.FindAnyObjectByType<TaskRaceModeManager>();
            if (hud == null) hud = UnityEngine.Object.FindAnyObjectByType<BobTaskRaceHud>();
            if (round == null) round = UnityEngine.Object.FindAnyObjectByType<TaskRaceRound>();
            if (manager == null || hud == null || round == null) return;
            if (manager.IsCountingDown) manager.SkipCountdown();
            if (manager.IsCountingDown) return;

            if (stepAt == 0f) stepAt = Time.realtimeSinceStartup;

            switch (step)
            {
                case 0: // a direct scene play has no MatchRequest, so no teammates; seed one and let the tray scan
                    if (!seeded)
                    {
                        seeded = true;
                        int mates = BobTaskRaceTeammates.Spawn(new[] { "sweep" }, TaskSpot.LocalTeam);
                        Debug.Log($"[TaskRaceShots] teammate(s) spawned: {mates}");
                        stepAt = Time.realtimeSinceStartup;
                        return;
                    }
                    if (Time.realtimeSinceStartup - stepAt < 1.4f) return;
                    Shot("Logs/taskrace-duo-mate.png");
                    Debug.Log($"[TaskRaceShots] mate tray: open={hud.DebugMateTray} chips={hud.DebugMateChips}");
                    Next();
                    break;
                case 1:
                    if (Time.realtimeSinceStartup - stepAt < 0.15f) return;
                    bool open = hud.DebugOpenPing();
                    Debug.Log($"[TaskRaceShots] ping wheel: open={open}");
                    if (!open) ok = false; else Shot("Logs/taskrace-ping-wheel.png");
                    hud.DebugClosePing();
                    Next();
                    break;
                case 2:
                {
                    var stains = BobEmergencyStains.Ensure(round, manager);
                    if (!spawnedOnce)
                    {
                        spawnedOnce = true;
                        bool spawned = stains.DebugSpawnNow();
                        Debug.Log($"[TaskRaceShots] emergency: spawned={spawned} enemy=\"{stains.EnemyName}\" " +
                                  $"task=\"{stains.TaskName}\" difficulty={stains.Difficulty} hasTarget={stains.HasTarget} " +
                                  $"planned={stains.PlannedSpawns} remaining={stains.Remaining:0.0}");
                        if (!spawned) { ok = false; Finish(); return; }
                        stepAt = Time.realtimeSinceStartup;
                        return; // the HUD renders the banner on its next update, not inside this call
                    }
                    if (Time.realtimeSinceStartup - stepAt < 0.45f) return;
                    Shot("Logs/taskrace-emergency.png");
                    Next();
                    break;
                }
                case 3:
                {
                    var rig = UnityEngine.Object.FindFirstObjectByType<BobTaskRaceCamera>();
                    if (!arrowTurned)
                    {
                        if (rig == null) { Debug.LogWarning("[TaskRaceShots] no camera rig for the arrow shot."); ok = false; Next(); break; }
                        yawWas = rig.yaw;
                        rig.yaw = yawWas + 180f; // face away from the stain so the edge arrow has to appear
                        arrowTurned = true;
                        stepAt = Time.realtimeSinceStartup;
                        return;
                    }
                    if (Time.realtimeSinceStartup - stepAt < 0.9f) return; // followTime 0.22 s, settle well past it
                    Shot("Logs/taskrace-emergency-arrow.png");
                    if (rig != null) rig.yaw = yawWas;
                    Next();
                    break;
                }
                case 4:
                {
                    if (!cardOpened)
                    {
                        cardOpened = true;
                        var card = typeof(BobTaskRaceHud).GetMethod("OpenEmergencyCard", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (card != null) card.Invoke(hud, null);
                        if (!hud.CardOpen) { Debug.Log("[TaskRaceShots] emergency card: open=False"); ok = false; Finish(); return; }
                        Debug.Log($"[TaskRaceShots] emergency card: open=True step=\"{hud.CardStep}\" hint=\"{hud.CardHint}\"");
                        stepAt = Time.realtimeSinceStartup;
                        return;
                    }
                    // the spawn toast rides above the card for 2.2 s and the banner hides a frame after the card opens
                    if (Time.realtimeSinceStartup - stepAt < 2.6f) return;
                    Shot("Logs/taskrace-emergency-card.png");
                    hud.DebugCloseCard();
                    Next();
                    break;
                }
                default:
                {
                    if (Time.realtimeSinceStartup - stepAt < 0.3f) return;
                    var spot = UnityEngine.Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude)
                        .FirstOrDefault(s => s.HasTask && s.TaskId == "T04" && s.OwnerTeam == TeamId.TeamA);
                    if (spot == null) { Debug.LogWarning("[TaskRaceShots] no T04 station on Team A."); ok = false; Finish(); return; }
                    spot.ResetTask();
                    if (!spot.TryOpen("shots", out var why)) { Debug.LogWarning($"[TaskRaceShots] T04 refused ({why})."); ok = false; Finish(); return; }
                    bool live = hud.DebugOpenCard(spot);
                    hud.DebugAdvanceCard(0.9f);
                    Debug.Log($"[TaskRaceShots] cart card: {spot.SpotId} live={live} open={hud.CardOpen} step=\"{hud.CardStep}\" hint=\"{hud.CardHint}\"");
                    if (!live) ok = false; else Shot("Logs/taskrace-cart-card.png");
                    hud.DebugCloseCard();
                    Finish();
                    break;
                }
            }
        }

        static void Next()
        {
            step++;
            stepAt = Time.realtimeSinceStartup;
        }

        static void Shot(string file)
        {
            GameObject hero = null;
            try { hero = GameObject.FindGameObjectWithTag("Player"); } catch (UnityException) { }
            if (hero == null) { Debug.LogWarning($"[TaskRaceShots] no hero for {file}."); ok = false; return; }
            var rig = UnityEngine.Object.FindFirstObjectByType<BobTaskRaceCamera>();
            float up = rig != null ? rig.Height : 5.2f;
            float back = rig != null ? rig.Distance : 5.2f;
            float yaw = rig != null ? rig.yaw : 0f;
            // same orbit the rig uses, yaw included, so the HUD's edge-arrow maths sees the camera it is drawn from
            TaskRaceVerify.Shot(file, hero.transform.position + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, up, -back),
                hero.transform.position + new Vector3(0f, 0.72f, 0f), 40f, hud);
        }

        static void Finish()
        {
            SessionState.SetBool(ArmKey, false);
            EditorApplication.update -= Tick;
            Debug.Log(ok ? "[TaskRaceShots] SHOTS PASSED" : "[TaskRaceShots] SHOTS FAILED");
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
