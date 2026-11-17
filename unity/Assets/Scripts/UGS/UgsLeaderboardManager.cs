// Battle of the Brooms: one global high-score board through Unity Leaderboards.
// The board itself is created in the Unity Cloud dashboard with the same id as leaderboardId.
using System;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace Bob.Ugs
{
    public class UgsLeaderboardManager : MonoBehaviour
    {
        [SerializeField] private string leaderboardId = "global_high_score";

        public static UgsLeaderboardManager Instance { get; private set; }

        public string LeaderboardId => leaderboardId;
        public LeaderboardScoresPage LastScores { get; private set; }
        /// <summary>The page read from the last daily board (docs 31 section 2.2), null when it could not be read.</summary>
        public LeaderboardScoresPage LastDailyScores { get; private set; }
        /// <summary>The entry the board returned from the last submit: its Rank counts from 0 (1st place = 0).</summary>
        public LeaderboardEntry LastEntry { get; private set; }
        public string LastError { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public async Task<bool> SubmitScoreAsync(double score)
        {
            try
            {
                LastEntry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
                LastError = null;
                Debug.Log($"[UGS] score {score} submitted to {leaderboardId}");
                return true;
            }
            catch (LeaderboardsException e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] score submit failed: " + e);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] score submit failed: " + e);
            }

            return false;
        }

        /// <summary>My own entry on the board (rank counts from 0), or null when this device has no score yet.</summary>        public async Task<LeaderboardEntry> GetPlayerScoreAsync()
        {
            try
            {
                var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
                LastError = null;
                return entry;
            }
            catch (LeaderboardsException e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] player score read failed: " + e);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] player score read failed: " + e);
            }

            return null;
        }

        /// <summary>
        /// Submit the daily round's score to that day's own board (docs 31 section 2.2). The board id is derived
        /// (BobDaily.DailyBoardName), so each day starts empty and yesterday's runs never mix in. A board that has not
        /// been created in the dashboard simply fails here and never blocks the global submit.
        /// </summary>
        public async Task<bool> SubmitDailyAsync(double score, int dayNumber)
        {
            string id = Bob.Rules.BobDaily.DailyBoardName(dayNumber);
            if (!Bob.Rules.BobDaily.ValidBoardName(id))
            {
                LastError = "bad daily board name: " + id;
                return false;
            }
            try
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(id, score);
                Debug.Log($"[UGS] daily score {score} submitted to {id}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UGS] daily score not submitted to {id}: {e.Message}");
                return false;
            }
        }

        /// <summary>The top of one day's board, or null when the board cannot be read (missing, offline).</summary>
        public async Task<LeaderboardScoresPage> GetDailyScoresAsync(int dayNumber, int limit = 10)
        {
            string id = Bob.Rules.BobDaily.DailyBoardName(dayNumber);
            if (!Bob.Rules.BobDaily.ValidBoardName(id)) return null;
            try
            {
                LastDailyScores = await LeaderboardsService.Instance.GetScoresAsync(id, new GetScoresOptions { Limit = limit });
                LastError = null;
                return LastDailyScores;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UGS] daily board {id} not read: {e.Message}");
                return null;
            }
        }

        public async Task<LeaderboardScoresPage> GetTopScoresAsync(int limit = 10)
        {
            try
            {
                LastScores = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions { Limit = limit });
                LastError = null;
                Debug.Log($"[UGS] {LastScores.Results.Count} score(s) read from {leaderboardId}");
            }
            catch (LeaderboardsException e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] leaderboard read failed: " + e);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError("[UGS] leaderboard read failed: " + e);
            }

            return LastScores;
        }
    }
}
