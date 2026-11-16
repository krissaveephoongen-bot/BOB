// Battle of the Brooms: the solo opponent on Team B (docs 24 §15.4 Phase 4). A visible walker with the same rules the
// player has: it walks to a dirty station of its own island, spends the task time there, cleans it, buys the 30 s shield
// when the team pool can pay for it, and once the raid opens it crosses a bridge to sabotage one clean station of Team A.
//
// The map is flat and the bridges are kept clear by the clearance check, so the bot walks straight segments with
// explicit bridge waypoints instead of a NavMeshAgent: no bake dependency, no agent that can miss the bridge links.
// It replaces BobTaskRaceRival in the solo scene (the rival stays the invisible pace model of the other modes).
using UnityEngine;

namespace Bob.TaskRace
{
    public class TaskRaceBotController : MonoBehaviour
    {
        [Header("Pace")]
        [Tooltip("Walk speed in world units per second; the hero walks 3.2.")]
        public float speed = 3.0f;
        [Tooltip("Work time on a station as a multiple of the station's estimate.")]
        [Range(0.6f, 2f)] public float pace = 1.12f;
        [Tooltip("Seconds after the start before the bot takes its first task.")]
        [Range(0f, 20f)] public float headStart = 4f;
        [Tooltip("Seconds it stands still when every own station is clean and waiting for a restock.")]
        [Range(0.25f, 5f)] public float retrySeconds = 1f;

        [Header("Shield")]
        [Tooltip("Buys the timed shield on the station it just cleaned once the pool can pay for it.")]
        public bool buyShield = true;

        [Header("Raid (cross the bridge)")]
        [Tooltip("Seconds between raids once the bridges are open.")]
        [Range(5f, 60f)] public float raidCooldown = 14f;
        [Tooltip("Sabotage hold before the station goes back to Dirty (the player's card uses 2.75 s).")]
        [Range(2.5f, 3f)] public float sabotageHoldSeconds = 2.75f;

        [Header("Walk")]
        [Tooltip("Waypoint arrival radius.")]
        public float arriveRadius = 0.55f;

        enum Job { None, Walk, Work, Sabotage }
        Job job, pending;
        TaskRaceModeManager manager;
        TaskRaceRound round;
        TaskSpot target;
        readonly System.Collections.Generic.List<Vector3> route = new System.Collections.Generic.List<Vector3>();
        int waypoint;
        float workSeconds, jobUntil, nextRaidAt, nextPickAt;

        /// <summary>Work the bot finished this match (the HUD reads the round; this is for the log).</summary>
        public int CleanCount { get; private set; }
        public int SabotageCount { get; private set; }
        /// <summary>What the bot is doing, for the debug bar and the verify log.</summary>
        public string JobName => (job == Job.Walk ? pending : job) == Job.None
            ? "idle" : $"{(job == Job.Walk ? pending : job)} {(target != null ? target.SpotId : "-")}";

        void Start()
        {
            manager = TaskRaceModeManager.Instance;
            round = FindAnyObjectByType<TaskRaceRound>();
            if (manager != null) manager.MatchStarted += OnMatchStarted;
            Schedule();
        }

        void OnDestroy()
        {
            if (manager != null) manager.MatchStarted -= OnMatchStarted;
        }

        void OnMatchStarted()
        {
            round = FindAnyObjectByType<TaskRaceRound>();
            CleanCount = 0; SabotageCount = 0;
            Schedule();
        }

        void Schedule()
        {
            job = Job.None;
            target = null;
            route.Clear();
            nextPickAt = Time.time + headStart;
            nextRaidAt = Time.time + headStart + raidCooldown;
        }

        void Update()
        {
            if (manager == null || manager.IsMatchOver || manager.LastResult.HasValue) return;
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();

            switch (job)
            {
                case Job.None:
                    if (Time.time >= nextPickAt) PickJob();
                    break;
                case Job.Walk:
                    Walk();
                    break;
                case Job.Work:
                case Job.Sabotage:
                    if (Time.time >= jobUntil) Finish();
                    break;
            }
        }

        // ---------------------------------------------------------------- choosing work

        void PickJob()
        {
            if (round == null) { nextPickAt = Time.time + retrySeconds; return; }

            // A raid beats cleaning while the bridges are open: undoing a clean station is worth more than one task.
            if (manager.AreBridgesOpen && Time.time >= nextRaidAt)
            {
                var victim = CleanEnemySpot();
                if (victim != null)
                {
                    Begin(victim, Job.Sabotage, sabotageHoldSeconds);
                    Debug.Log($"[TaskRaceBot] raid: walking to sabotage {victim.SpotId} on Team A.");
                    return;
                }
            }

            var own = DirtyOwnSpot();
            if (own == null) { nextPickAt = Time.time + retrySeconds; return; }
            Begin(own, Job.Work, Mathf.Clamp(own.ExpectedSeconds * pace, 2f, 30f));
        }

        void Begin(TaskSpot spot, Job kind, float seconds)
        {
            target = spot;
            pending = kind;
            workSeconds = seconds;
            job = Job.Walk;
            waypoint = 0;
            BuildRoute(spot.transform.position);
            nextPickAt = float.MaxValue;
        }

        void Finish()
        {
            if (target == null) { NextPick(raidCooldown); return; }
            if (job == Job.Sabotage) FinishSabotage(); else FinishClean();
        }

        void FinishClean()
        {
            if (!target.ForceComplete()) { NextPick(1.2f); return; } // the card is held by the player right now
            CleanCount++;
            Debug.Log($"[TaskRaceBot] clean {CleanCount} on {target.SpotId}; Team B {round?.DoneCountOf(TeamId.TeamB)}/{round?.TaskCount}.");

            if (buyShield && manager.PointsOf(TeamId.TeamB) >= manager.shieldItemCost && !target.IsProtected &&
                manager.TryPurchaseShield(TeamId.TeamB, target))
                Debug.Log($"[TaskRaceBot] shield on {target.SpotId}; pool {manager.PointsOf(TeamId.TeamB)}.");

            NextPick(1.2f);
        }

        void FinishSabotage()
        {
            if (target.TrySabotageAs(TeamId.TeamB, "คู่แข่ง", out var message))
            {
                target.CompleteSabotage();
                SabotageCount++;
                Debug.Log($"[TaskRaceBot] sabotage {SabotageCount} on {target.SpotId}; Team A {round?.DoneCountOf(TeamId.TeamA)}/{round?.TaskCount}.");
            }
            else Debug.Log($"[TaskRaceBot] sabotage on {target.SpotId} refused: {message}");
            NextPick(raidCooldown);
        }

        void NextPick(float pause)
        {
            job = Job.None;
            target = null;
            route.Clear();
            nextPickAt = Time.time + pause;
        }

        // ---------------------------------------------------------------- walking

        void BuildRoute(Vector3 to)
        {
            route.Clear();
            var from = transform.position;
            if (Mathf.Sign(to.x) == Mathf.Sign(from.x) || Mathf.Abs(to.x) < 1f)
            {
                route.Add(to);
                return;
            }
            // Cross on the one deck: its z and the x where it lands are read off the scene's Bridges_Group, so the bot
            // walks the same line the NavMesh baked (the old code hard-coded the solo map's z = +-2.7).
            float z = 0f, mouth = 5f;
            var deck = manager != null && manager.bridgesGroup != null && manager.bridgesGroup.transform.childCount > 0
                ? manager.bridgesGroup.transform.GetChild(0) : null;
            if (deck != null)
            {
                z = deck.position.z;
                var walk = deck.Find("Walkable");
                var col = walk != null ? walk.GetComponent<Collider>() : null;
                if (col != null) mouth = Mathf.Abs(col.bounds.max.x);
            }
            route.Add(new Vector3(Mathf.Sign(from.x) * mouth, 0f, z));
            route.Add(new Vector3(0f, 0f, z));
            route.Add(new Vector3(-Mathf.Sign(from.x) * mouth, 0f, z));
            route.Add(to);
        }

        void Walk()
        {
            if (waypoint >= route.Count) { Arrive(); return; }
            var p = route[waypoint];
            p.y = transform.position.y;
            var step = speed * Time.deltaTime;
            var to = p - transform.position;
            if (to.magnitude <= Mathf.Max(arriveRadius, step))
            {
                transform.position = p;
                waypoint++;
                if (waypoint >= route.Count) Arrive();
                return;
            }
            transform.position += to.normalized * step;
            var face = Quaternion.LookRotation(new Vector3(to.x, 0f, to.z), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, 12f * Time.deltaTime);
        }

        void Arrive()
        {
            if (target == null) { Schedule(); return; }
            job = pending;
            jobUntil = Time.time + workSeconds;
            if (job == Job.Sabotage && !manager.AreBridgesOpen) NextPick(retrySeconds);
        }

        // ---------------------------------------------------------------- queries

        /// <summary>Check hook: start a clean job now, ignoring the head start (used by the editor bot check).</summary>
        public bool DebugWorkNow()
        {
            var own = DirtyOwnSpot();
            if (own == null) return false;
            Begin(own, Job.Work, Mathf.Clamp(own.ExpectedSeconds * pace, 2f, 30f));
            return true;
        }

        /// <summary>Check hook: cross the bridge now to sabotage the nearest clean Team A station, ignoring the cooldown.</summary>
        public bool DebugRaidNow()
        {
            if (manager == null || !manager.AreBridgesOpen) return false;
            var victim = CleanEnemySpot();
            if (victim == null) return false;
            Begin(victim, Job.Sabotage, sabotageHoldSeconds);
            nextRaidAt = Time.time + raidCooldown;
            Debug.Log($"[TaskRaceBot] debug raid: walking to sabotage {victim.SpotId} on Team A.");
            return true;
        }

        TaskSpot DirtyOwnSpot()
        {
            TaskSpot best = null;
            float bestD = float.MaxValue;
            foreach (var spot in round.StationsOf(TeamId.TeamB))
            {
                if (spot == null || !spot.HasTask || spot.State != SpotState.Dirty || spot.IsOpen) continue;
                float d = Vector3.Distance(transform.position, spot.transform.position);
                if (d < bestD) { bestD = d; best = spot; }
            }
            return best;
        }

        TaskSpot CleanEnemySpot()
        {
            TaskSpot best = null;
            float bestD = float.MaxValue;
            foreach (var spot in round.StationsOf(TeamId.TeamA))
            {
                if (spot == null || !spot.HasTask || spot.State != SpotState.Clean || spot.SabotageUsed || spot.IsProtected) continue;
                float d = Vector3.Distance(transform.position, spot.transform.position);
                if (d < bestD) { bestD = d; best = spot; }
            }
            return best;
        }
    }
}
