// Battle of the Brooms: the match result and the device-local Task Race rewards. A finished match pays coins and XP
// and keeps the best score; the keys are the lobby's own (bob.coins, bob.xp), so the currency chips on Home show the
// same wallet the match just fed. No server yet: this is the offline profile of the shell scene.
using UnityEngine;

namespace Bob.TaskRace
{
    /// <summary>Everything the result overlay (S27) shows about one finished match.</summary>
    public readonly struct MatchResult
    {
        public readonly bool Won, Draw;
        public readonly int TeamAPoints, TeamBPoints;
        public readonly int TasksA, TasksB, TaskCount;
        public readonly int Coins, Xp;
        public readonly int BestPoints;
        public readonly bool NewBest;

        public MatchResult(bool won, bool draw, int teamAPoints, int teamBPoints, int tasksA, int tasksB,
                           int taskCount, int coins, int xp, int bestPoints, bool newBest)
        {
            Won = won; Draw = draw;
            TeamAPoints = teamAPoints; TeamBPoints = teamBPoints;
            TasksA = tasksA; TasksB = tasksB; TaskCount = taskCount;
            Coins = coins; Xp = xp;
            BestPoints = bestPoints; NewBest = newBest;
        }

        public override string ToString() =>
            $"{(Won ? "win" : Draw ? "draw" : "loss")} {TeamAPoints}:{TeamBPoints} points · " +
            $"tasks {TasksA}/{TaskCount} vs {TasksB}/{TaskCount} · reward {Coins} coins +{Xp} xp · " +
            $"best {BestPoints}{(NewBest ? " (new)" : "")}";
    }

    public static class BobTaskRaceProfile
    {
        public const string CoinsKey = "bob.coins";
        public const string XpKey = "bob.xp";
        public const string BestPointsKey = "bob.taskRace.best";
        public const string MatchesKey = "bob.taskRace.matches";
        public const string WinsKey = "bob.taskRace.wins";

        public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);
        public static int Xp => PlayerPrefs.GetInt(XpKey, 0);
        public static int BestPoints => PlayerPrefs.GetInt(BestPointsKey, 0);
        public static int Matches => PlayerPrefs.GetInt(MatchesKey, 0);
        public static int Wins => PlayerPrefs.GetInt(WinsKey, 0);

        /// <summary>The one-off XP for closing a chapter for the first time; a replay pays the normal rate only.</summary>
        public const int FirstClearBonus = 100;

        /// <summary>
        /// Pay one match. `record` false only composes the result: the verify renders the overlay without changing
        /// the developer's wallet.
        /// </summary>
        public static MatchResult Record(bool won, bool draw, int teamAPoints, int teamBPoints,
                                         int tasksA, int tasksB, int taskCount,
                                         int localPoints, int localTasks, bool record = true, int extraXp = 0)
        {
            int coins = 25 + (won ? 50 : draw ? 20 : 10) + localPoints / 20;
            int xp = 30 + (won ? 25 : 0) + localTasks * 8 + Mathf.Max(0, extraXp);
            // A chapter that cleaned nothing must not pay: otherwise three idle rounds are a coin/XP faucet.
            if (localTasks <= 0) { coins = 0; xp = 0; }

            int best = BestPoints;
            bool newBest = localPoints > best;
            if (newBest) best = localPoints;

            if (record)
            {
                PlayerPrefs.SetInt(CoinsKey, Coins + coins);
                PlayerPrefs.SetInt(XpKey, Xp + xp);
                PlayerPrefs.SetInt(BestPointsKey, best);
                PlayerPrefs.SetInt(MatchesKey, Matches + 1);
                if (won) PlayerPrefs.SetInt(WinsKey, Wins + 1);
                PlayerPrefs.Save();
            }

            return new MatchResult(won, draw, teamAPoints, teamBPoints, tasksA, tasksB, taskCount,
                coins, xp, best, newBest);
        }
    }
}
