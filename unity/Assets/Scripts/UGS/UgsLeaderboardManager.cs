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
                await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
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
