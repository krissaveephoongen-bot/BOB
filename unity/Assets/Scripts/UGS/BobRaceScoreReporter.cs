// Battle of the Brooms: the finished match goes to the UGS leaderboard (docs 27 section 3 / docs 16 section 2.6). One entry per decided match, sent
// once, and only when this device is signed in — an offline phone keeps playing and simply posts nothing.
using System.Threading.Tasks;
using Bob.TaskRace;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace Bob.Ugs
{
    public class BobRaceScoreReporter : MonoBehaviour
    {
        TaskRaceModeManager manager;
        bool sent;

        /// <summary>The rank the board returned for the last submit (1 = top), or -1 when unknown or offline.</summary>
        public int LastRank { get; private set; } = -1;
        /// <summary>The score the board accepted, 0 when nothing was sent.</summary>
        public int LastSubmitted { get; private set; }
        public string LastError { get; private set; }

        public static BobRaceScoreReporter Attach(TaskRaceModeManager owner)
        {
            if (owner == null) return null;
            foreach (var existing in owner.GetComponents<BobRaceScoreReporter>())
                if (existing != null) return existing;
            var reporter = owner.gameObject.AddComponent<BobRaceScoreReporter>();
            reporter.manager = owner;
            return reporter;
        }

        void Awake()
        {
            if (manager == null) manager = FindAnyObjectByType<TaskRaceModeManager>();
        }

        void OnEnable()
        {
            if (manager != null) manager.MatchEnded += OnMatchEnded;
        }

        void OnDisable()
        {
            if (manager != null) manager.MatchEnded -= OnMatchEnded;
        }

        void OnMatchEnded(MatchResult result)
        {
            if (sent) return; // one board entry per match, even if the overlay is rebuilt
            if (manager != null && !manager.LastMatchRecorded) return; // the verify shot composed a result, it saved nothing
            int score = manager != null ? manager.LastScore : 0;
            if (score <= 0) { LastError = "no score to send"; return; }
            sent = true;
            _ = SubmitAsync(score);
        }

        async Task SubmitAsync(int score)
        {
            var boot = UgsBootstrapManager.Instance;
            if (boot == null || !boot.IsReady)
            {
                LastError = "not signed in";
                Debug.Log("[TaskRace] leaderboard skipped: this device is not signed in to UGS.");
                return;
            }
            var board = UgsLeaderboardManager.Instance;
            if (board == null)
            {
                LastError = "no leaderboard manager";
                Debug.LogWarning("[TaskRace] leaderboard skipped: no UgsLeaderboardManager in the scene.");
                return;
            }
            if (!await board.SubmitScoreAsync(score))
            {
                LastError = board.LastError;
                Debug.LogWarning("[TaskRace] leaderboard submit failed: " + LastError);
                return;
            }
            LastSubmitted = score;
            LeaderboardEntry entry = board.LastEntry;
            LastRank = entry != null ? entry.Rank + 1 : -1; // the API counts from 0, the player counts from 1
            LastError = null;
            Debug.Log($"[TaskRace] leaderboard {board.LeaderboardId}: score {score}" +
                      (LastRank > 0 ? $" · rank {LastRank}" : " · rank unknown"));
        }
    }
}
