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
        /// <summary>The MatchSequence already answered for, so a rematch in the same scene is allowed to submit again.</summary>
        int answeredFor = -1;

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
            if (manager == null) return;
            // One board entry per match; a rematch bumps MatchSequence, so the next match is allowed to submit again.
            if (answeredFor == manager.MatchSequence) return;
            answeredFor = manager.MatchSequence;
            if (!manager.LastMatchRecorded) return; // the verify shot composed a result, it saved nothing
            if (manager.LastMatchCanceled) { LastError = "series abandoned"; return; } // nobody is ranked for a match a player left
            int score = manager.LastScore;
            if (score <= 0) { LastError = "no score to send"; return; }
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
            var board = UgsLeaderboardManager.Ensure();
            if (board == null)
            {
                LastError = "no leaderboard manager";
                Debug.LogWarning("[TaskRace] leaderboard skipped: no UgsLeaderboardManager could be created.");
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
