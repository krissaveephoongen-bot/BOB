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
        /// <summary>The entry the board returned from the last submit: its Rank counts from 0 (1st place = 0).</summary>
        public LeaderboardEntry LastEntry { get; private set; }
        public string LastError { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            // The board has to outlive the screen that created it: Home makes it for the board card, the Task Race
            // scene submits from there, and a scene load would otherwise take the singleton with it.
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// The one board manager, on its own persistent object when no scene carries one (docs 27 section 3). Outside
        /// Play mode nothing is created and null comes back, so the edit-mode verify never grows objects into a scene.
        /// </summary>
        public static UgsLeaderboardManager Ensure()
        {
            if (Instance != null) return Instance;
            var existing = FindAnyObjectByType<UgsLeaderboardManager>();
            if (existing != null) { Instance = existing; return existing; }
            if (!Application.isPlaying) return null;
            return new GameObject("UGS Board").AddComponent<UgsLeaderboardManager>();
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

        /// <summary>My own entry on the board (rank counts from 0), or null when this device has no score yet.</summary>
        public async Task<LeaderboardEntry> GetPlayerScoreAsync()
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
