// Check for A3 (the chapter card): stars 0-3 off a closed series, the best-of store, and the next-chapter link.
// No Play mode needed: StorySeries is plain state and BobChapters is a thin layer over PlayerPrefs, so the rules can
// be walked here. The card itself and the scene reload of "บทถัดไป" are covered by TaskRaceVerify's shot and by the
// owner's device pass.
//   -executeMethod Bob.EditorTools.ChapterResultCheck.Run     (Editor closed on this project)
// Success = the Console shows "[ChapterCheck] CHAPTER RESULT CHECK PASSED".
using System;
using System.Reflection;
using Bob.Rules;
using Bob.TaskRace;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class ChapterResultCheck
    {
        [MenuItem("Bob/Run Chapter Result Check")]
        public static void Run()
        {
            failures = 0;
            CheckStars();
            CheckLadder();
            CheckStarStore();
            CheckWiring();
            bool ok = failures == 0;
            Debug.Log(ok ? "[ChapterCheck] CHAPTER RESULT CHECK PASSED"
                         : $"[ChapterCheck] CHAPTER RESULT CHECK FAILED ({failures} problem(s))");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        static int failures;

        static void Expect(bool condition, string what)
        {
            if (condition) Debug.Log($"[ChapterCheck] ok: {what}");
            else { failures++; Debug.LogWarning($"[ChapterCheck] FAILED: {what}"); }
        }

        /// <summary>Play a series from task pairs per round; points stay 0 so only the task counts decide.</summary>
        static StorySeries Play(params int[] tasksPerRound)
        {
            var series = new StorySeries();
            for (int i = 0; i + 1 < tasksPerRound.Length; i += 2)
                series.Register(tasksPerRound[i], tasksPerRound[i + 1], 0, 0);
            return series;
        }

        static void CheckStars()
        {
            var sweepBig = Play(6, 3, 6, 4); // 2-0 with a three-task margin
            Expect(sweepBig.Over && sweepBig.LocalTookSeries && sweepBig.CleanSweep, "2-0 with a big win is a clean sweep");
            Expect(sweepBig.ChallengeOffered, "the three-task margin is the challenge offer");
            Expect(sweepBig.Stars == 3, $"a 2-0 sweep with a big win is 3 stars (got {sweepBig.Stars})");

            var sweep = Play(6, 5, 5, 4); // 2-0, no margin
            Expect(sweep.Stars == 2, $"a 2-0 without a big win is 2 stars (got {sweep.Stars})");

            var close = Play(6, 5, 5, 6, 6, 5); // 2-1
            Expect(close.Stars == 1, $"a 2-1 chapter is 1 star (got {close.Stars})");

            var loss = Play(5, 6, 5, 6);
            Expect(loss.Over && loss.Stars == 0, $"a lost chapter is 0 stars (got {loss.Stars})");

            var draws = Play(5, 5, 5, 5, 5, 5);
            Expect(draws.Over && !draws.LocalTookSeries, "three draws still close the chapter");
            Expect(draws.Stars == 0, $"a drawn chapter is 0 stars (got {draws.Stars})");

            var bigButLost = Play(10, 2, 1, 5, 1, 5);
            Expect(bigButLost.ChallengeOffered && !bigButLost.LocalTookSeries, "a big win inside a lost series still offers the challenge");
            Expect(bigButLost.Stars == 0, $"the challenge win alone does not pay a star (got {bigButLost.Stars})");

            var open = new StorySeries();
            open.Register(6, 5, 0, 0);
            Expect(!open.Over && open.Stars == 0, "a series still in play has no stars yet");

            foreach (var series in new[] { sweepBig, sweep, close, loss, draws, bigButLost })
                Expect(series.Stars >= 0 && series.Stars <= 3, $"stars stay inside 0-3 ({series.Score} -> {series.Stars})");
        }

        static void CheckLadder()
        {
            Expect(BobChapters.Count == 6, $"six chapters in the ladder (got {BobChapters.Count})");
            for (int order = 1; order < BobChapters.Count; order++)
            {
                var next = BobChapters.Next(BobChapters.Get(order));
                Expect(next != null && next.Order == order + 1, $"next of chapter {order} is {order + 1} (got {next?.Order.ToString() ?? "null"})");
            }
            Expect(BobChapters.Next(BobChapters.Get(BobChapters.Count)) == null,
                $"the last chapter has no next (got {BobChapters.Next(BobChapters.Get(BobChapters.Count))?.Order.ToString() ?? "null"})");
            Expect(BobChapters.Next(null) == null, "an empty chapter hands back no next");

            Expect(BobChapters.LevelFromXp(0) == 1 && BobChapters.LevelFromXp(199) == 1, "0-199 XP is level 1");
            Expect(BobChapters.LevelFromXp(200) == 2, $"200 XP is level 2 (got {BobChapters.LevelFromXp(200)})");
            Expect(BobChapters.LevelFromXp(-50) == 1, "a negative XP total never drops below level 1");
            Expect(BobChapters.LevelFromXp(200 * 40) == 41, $"8000 XP reaches level 41, the last chapter (got {BobChapters.LevelFromXp(8000)})");

            var chapter2 = BobChapters.Get(2);
            Expect(!BobChapters.Unlocked(chapter2, 5) && BobChapters.Unlocked(chapter2, 6),
                $"chapter 2 opens exactly at LV 6 (unlock {chapter2.UnlockLevel})");
        }

        static void CheckStarStore()
        {
            const int order = 1, other = 6;
            string key = BobChapters.StarsPrefix + order, otherKey = BobChapters.StarsPrefix + other;
            bool had = PlayerPrefs.HasKey(key), hadOther = PlayerPrefs.HasKey(otherKey);
            int old = had ? PlayerPrefs.GetInt(key) : 0, oldOther = hadOther ? PlayerPrefs.GetInt(otherKey) : 0;
            try
            {
                PlayerPrefs.SetInt(key, 0);
                Expect(BobChapters.Stars(order) == 0, "a fresh chapter starts at 0 stars");
                Expect(BobChapters.SaveStars(order, 2) == 2 && BobChapters.Stars(order) == 2, "two stars are stored");
                Expect(BobChapters.SaveStars(order, 1) == 2, "a worse replay keeps the best (2, got 1 asked)");
                Expect(BobChapters.SaveStars(order, 3) == 3 && BobChapters.Stars(order) == 3, "a better replay raises the best to 3");
                Expect(BobChapters.SaveStars(order, 9) == 3, "an out-of-range ask is clamped to 3");
                PlayerPrefs.SetInt(otherKey, 1);
                Expect(BobChapters.Stars(other) == 1 && BobChapters.Stars(order) == 3, "each chapter keeps its own stars");
                Debug.Log($"[ChapterCheck] stars now: " + string.Join(" ", Array.ConvertAll(new[] { 1, 2, 3, 4, 5, 6 }, o => $"{o}:{BobChapters.Stars(o)}")) +
                          $", total {BobChapters.TotalStars}");
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(key, old); else PlayerPrefs.DeleteKey(key);
                if (hadOther) PlayerPrefs.SetInt(otherKey, oldOther); else PlayerPrefs.DeleteKey(otherKey);
                PlayerPrefs.Save();
                Debug.Log($"[ChapterCheck] stars restored: chapter {order} {(had ? old.ToString() : "none")}, " +
                          $"chapter {other} {(hadOther ? oldOther.ToString() : "none")}");
            }
        }

        static void CheckWiring()
        {
            var hud = typeof(BobTaskRaceHud);
            Expect(hud.GetMethod("BuildChapterCard", BindingFlags.Instance | BindingFlags.NonPublic) != null,
                "the HUD draws the chapter card");
            Expect(hud.GetMethod("GoToNextChapter", BindingFlags.Instance | BindingFlags.NonPublic) != null,
                "the HUD has the บทถัดไป action");
            Expect(hud.GetMethod("StarIcons", BindingFlags.Instance | BindingFlags.NonPublic) != null,
                "the HUD draws the stars as sprites (neither Thai font has a ★ glyph)");

            var manager = typeof(TaskRaceModeManager);
            Expect(manager.GetMethod("DebugComposeSeries") != null, "the manager can compose a closed series for the verify");
            var stars = typeof(StorySeries).GetProperty("Stars");
            Expect(stars != null && stars.PropertyType == typeof(int), "StorySeries exposes Stars");
            Expect(typeof(StorySeries).GetProperty("CleanSweep") != null, "StorySeries exposes CleanSweep");

            Expect(typeof(BobChapters).GetMethod("SaveStars") != null && typeof(BobChapters).GetMethod("Next") != null,
                "BobChapters owns the star store and the next-chapter link");
        }
    }
}
