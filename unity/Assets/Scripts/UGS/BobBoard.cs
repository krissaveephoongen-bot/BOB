// Battle of the Brooms: the data the board card draws (docs 31 section 3). All SDK types stay in this file so the UI
// only ever reads plain rows; offline or signed-out, LoadAsync says so instead of throwing.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Bob.Ugs
{
    /// <summary>One line of the board: rank from 1, the name to show, the score, and whether it is this device.</summary>
    public sealed class BobBoardRow
    {
        public int Rank;
        public string Name;
        public int Score;
        public bool Self;

        public BobBoardRow(int rank, string name, int score)
        {
            Rank = rank; Name = name; Score = score;
        }
    }

    /// <summary>The top of the board plus my own line: everything the card needs, and nothing that needs the SDK.</summary>
    public sealed class BobBoardSnapshot
    {
        public readonly List<BobBoardRow> Rows = new List<BobBoardRow>();
        public bool SignedIn;
        public int SelfRank = -1; // 1 = top; -1 = this device has no entry yet
        public int SelfScore;
        public string Error;
    }

    public static class BobBoardService
    {
        public const string NoName = "ผู้เล่น";

        /// <summary>The top `limit` scores and my own entry. Never throws: a failure comes back as Error + SignedIn.</summary>
        public static async Task<BobBoardSnapshot> LoadAsync(int limit = 10)
        {
            var snap = new BobBoardSnapshot();
            var boot = UgsBootstrapManager.Instance;
            if (boot == null || !boot.IsReady)
            {
                snap.Error = "offline";
                return snap;
            }
            snap.SignedIn = true;

            var board = UgsLeaderboardManager.Instance;
            if (board == null)
            {
                // The lobby scene only hosts the bootstrap, so the board manager is created on the first look.
                var host = new GameObject("UGS Board");
                board = host.AddComponent<UgsLeaderboardManager>();
            }

            var page = await board.GetTopScoresAsync(limit);
            if (page == null)
            {
                snap.Error = string.IsNullOrEmpty(board.LastError) ? "read failed" : board.LastError;
                return snap;
            }
            if (page.Results != null)
            {
                foreach (var e in page.Results)
                {
                    if (e == null) continue;
                    string name = string.IsNullOrEmpty(e.PlayerName) ? NoName : e.PlayerName;
                    snap.Rows.Add(new BobBoardRow(e.Rank + 1, name, (int)Math.Round(e.Score)));
                }
            }

            var self = await board.GetPlayerScoreAsync();
            if (self != null)
            {
                snap.SelfRank = self.Rank + 1;
                snap.SelfScore = (int)Math.Round(self.Score);
            }
            for (int i = 0; i < snap.Rows.Count; i++)
                if (snap.SelfRank > 0 && snap.Rows[i].Rank == snap.SelfRank) snap.Rows[i].Self = true;
            return snap;
        }

        /// <summary>
        /// The top of one day's board (docs 31 section 2.2). Same offline contract as LoadAsync, but it never marks a
        /// Self row: a daily page is one day's own board, and this device's place in it is not read here.
        /// </summary>
        public static async Task<BobBoardSnapshot> LoadDailyAsync(int day, int limit = 5)
        {
            var snap = new BobBoardSnapshot();
            var boot = UgsBootstrapManager.Instance;
            if (boot == null || !boot.IsReady)
            {
                snap.Error = "offline";
                return snap;
            }
            snap.SignedIn = true;

            var board = UgsLeaderboardManager.Instance;
            if (board == null)
            {
                // The lobby scene only hosts the bootstrap, so the board manager is created on the first look.
                var host = new GameObject("UGS Board");
                board = host.AddComponent<UgsLeaderboardManager>();
            }

            var page = await board.GetDailyScoresAsync(day, limit);
            if (page == null)
            {
                snap.Error = string.IsNullOrEmpty(board.LastError) ? "read failed" : board.LastError;
                return snap;
            }
            if (page.Results != null)
            {
                foreach (var e in page.Results)
                {
                    if (e == null) continue;
                    string name = string.IsNullOrEmpty(e.PlayerName) ? NoName : e.PlayerName;
                    snap.Rows.Add(new BobBoardRow(e.Rank + 1, name, (int)Math.Round(e.Score)));
                }
            }
            return snap;
        }
    }
}
