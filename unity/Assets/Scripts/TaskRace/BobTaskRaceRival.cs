// Battle of the Brooms: the AI rival for Team B on the Task Race map. With no server yet, the opponent is a pace
// model: it cleans its own stations one after another on the round's own estimate per task, so a player who finishes
// a task in the estimated time stays level and a faster one pulls ahead. Each clean runs the normal Closed path, so
// points, the round count and the restock all behave exactly as a human station would.
//
// `pace` is the balance lever the economy pass will tune (1.0 = the estimate, 1.15 = a forgiving rival). The rival
// only works while the match runs and stops the moment the result is decided.
using UnityEngine;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class BobTaskRaceRival : MonoBehaviour
    {
        [Tooltip("Pace factor on the round's estimate per task: 1.0 = exactly the estimate, 1.2 = 20% slower.")]
        [Range(0.5f, 2f)] public float pace = 1.12f;
        [Tooltip("Seconds after the start before the rival's first clean, so the player always gets a head start.")]
        [Range(0f, 20f)] public float headStart = 4f;
        [Tooltip("Random spread around the pace, +/-, so the relay never looks metronomic.")]
        [Range(0f, 0.4f)] public float jitter = 0.12f;
        [Tooltip("Seconds between retries when every Team B station is clean and waiting for its restock.")]
        [Range(0.25f, 5f)] public float retrySeconds = 1f;

        TaskRaceModeManager manager;
        TaskRaceRound round;
        float nextAt;
        int cleanCount;

        /// <summary>Cleans the rival has scored this match; the HUD reads the round instead, the log reads this.</summary>
        public int CleanCount => cleanCount;

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
            cleanCount = 0;
            Schedule();
        }

        void Update()
        {
            if (manager == null) return;
            if (manager.LastResult.HasValue || manager.IsMatchOver) return;
            if (Time.time < nextAt) return;

            var spot = NextDirty();
            if (spot == null)
            {
                nextAt = Time.time + retrySeconds;
                return;
            }

            if (!spot.ForceComplete())
            {
                // The player is holding this card: come back for another station instead of wiping their progress.
                nextAt = Time.time + retrySeconds;
                return;
            }
            cleanCount++;
            if (Application.isPlaying)
                Debug.Log($"[TaskRaceRival] clean {cleanCount} on {spot.SpotId} ({spot.TaskName}); " +
                          $"Team B {round?.DoneCountOf(TeamId.TeamB)}/{round?.TaskCount} tasks.");

            if (round != null && round.TaskCount > 0 && round.DoneCountOf(TeamId.TeamB) >= round.TaskCount)
            {
                manager.EndMatch();
                return;
            }
            Schedule();
        }

        void Schedule()
        {
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            float perTask = round != null && round.TaskCount > 0 ? round.EstimatedSeconds / round.TaskCount : 12f;
            float spread = 1f + Random.Range(-jitter, jitter);
            nextAt = Time.time + headStart + perTask * pace * Mathf.Max(0.2f, spread);
        }

        /// <summary>First dirty Team B station in spot-id order; null while the whole island waits for a restock.</summary>
        TaskSpot NextDirty()
        {
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            if (round == null) return null;

            TaskSpot best = null;
            foreach (var spot in round.StationsOf(TeamId.TeamB))
            {
                if (spot == null || !spot.HasTask || spot.State != SpotState.Dirty) continue;
                if (best == null || string.CompareOrdinal(spot.SpotId, best.SpotId) < 0) best = spot;
            }
            return best;
        }
    }
}
