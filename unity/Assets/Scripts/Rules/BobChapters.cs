// Battle of the Brooms: the chapter ladder as the lobby and the TaskRace scene see it. The six ChapterDefinition
// assets live in Resources/Chapters (saved by hand; the editor menu "Bob > Chapters > Check Ladder" reports
// gaps), and the pick is saved on the device as bob.chapter. The TaskRace scene reads the same pick through
// TaskRaceModeManager, so the stage the player chose in S13 is the difficulty and clock they actually play.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bob.Rules
{
    public static class BobChapters
    {
        public const string PrefKey = "bob.chapter";
        /// <summary>PlayerPrefs key of a chapter's best star count (A3): "bob.story.stars.&lt;order&gt;".</summary>
        public const string StarsPrefix = "bob.story.stars.";
        /// <summary>XP per level: the lobby's level gate and the match's next-chapter check share this one number.</summary>
        public const int XpPerLevel = 200;
        /// <summary>Top of the ladder (docs 24 section 15.1): XP keeps counting but the level stops here.</summary>
        public const int MaxLevel = 50;
        /// <summary>Stars the previous chapter needs before the next one opens (A3: level alone is not enough).</summary>
        public const int StarsToOpen = 2;
        /// <summary>Key of "this chapter was closed at least once": the first-clear XP bonus is paid once.</summary>
        public const string ClearedPrefix = "bob.story.cleared.";
        const string ResourcePath = "Chapters";

        static ChapterDefinition[] all;
        static bool warned, fromAssets;

        public static IReadOnlyList<ChapterDefinition> All { get { Ensure(); return all; } }
        public static int Count => All.Count;

        /// <summary>The chapter with this order; an unknown order falls back to the first chapter, never null.</summary>
        public static ChapterDefinition Get(int order)
        {
            Ensure();
            return all.FirstOrDefault(c => c.Order == order) ?? all[0];
        }

        /// <summary>A chapter opens on its level AND on two stars of the chapter before it; the first one is free.</summary>
        public static bool Unlocked(ChapterDefinition chapter, int level) =>
            chapter != null && level >= chapter.UnlockLevel &&
            (chapter.Order <= 1 || Stars(chapter.Order - 1) >= StarsToOpen);

        /// <summary>Highest chapter opened at this level (chapter 1 at level 1, since it unlocks at 1).</summary>
        public static ChapterDefinition LastUnlocked(int level)
        {
            Ensure();
            ChapterDefinition best = all[0];
            foreach (var c in all) if (Unlocked(c, level)) best = c;
            return best;
        }

        /// <summary>Saved pick with no level check — the TaskRace scene has no player level; never null.</summary>
        public static ChapterDefinition Saved
        {
            get
            {
                int order;
                try { order = PlayerPrefs.GetInt(PrefKey, 1); } catch (Exception) { order = 1; }
                return Get(order);
            }
        }

        /// <summary>Saved pick; missing or still-locked falls back to the highest chapter opened at this level.</summary>
        public static ChapterDefinition Selected(int level)
        {
            var chapter = Saved;
            return Unlocked(chapter, level) ? chapter : LastUnlocked(level);
        }

        /// <summary>Saves the pick and keeps bob.band in step, so the older band-keyed best score follows the chapter.</summary>
        public static void Select(ChapterDefinition chapter, int level)
        {
            if (!Unlocked(chapter, level)) return;
            try
            {
                PlayerPrefs.SetInt(PrefKey, chapter.Order);
                PlayerPrefs.SetInt("bob.band", chapter.Band);
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        /// <summary>Best-score band of a chapter (1 = chapters 1-2, 2 = 3-4, 3 = 5-6), matching the 4.6 bands.</summary>
        public static int Band(ChapterDefinition chapter) => chapter == null ? 1 : chapter.Band;

        /// <summary>Player level of an XP total; the lobby and the TaskRace scene both gate chapters with it.</summary>
        public static int LevelFromXp(int xp) => Mathf.Clamp(1 + Mathf.Max(0, xp) / XpPerLevel, 1, MaxLevel);

        /// <summary>True once this chapter was won at least once (the first-clear bonus hangs off it).</summary>
        public static bool Cleared(int order)
        {
            try { return PlayerPrefs.GetInt(ClearedPrefix + Mathf.Max(1, order), 0) != 0; }
            catch (Exception) { return false; }
        }

        /// <summary>Remember a chapter win; returns true when this call is the first clear.</summary>
        public static bool MarkCleared(int order)
        {
            int chapter = Mathf.Max(1, order);
            if (Cleared(chapter)) return false;
            try { PlayerPrefs.SetInt(ClearedPrefix + chapter, 1); PlayerPrefs.Save(); }
            catch (Exception) { return false; }
            return true;
        }

        /// <summary>Best 0-3 star count this chapter has earned on this device.</summary>
        public static int Stars(ChapterDefinition chapter) => chapter == null ? 0 : Stars(chapter.Order);

        public static int Stars(int order)
        {
            try { return Mathf.Clamp(PlayerPrefs.GetInt(StarsPrefix + Mathf.Max(1, order), 0), 0, 3); }
            catch (Exception) { return 0; }
        }

        /// <summary>Keep the better of the stored and the earned star count; returns what is now stored.</summary>
        public static int SaveStars(int order, int stars)
        {
            int chapter = Mathf.Max(1, order);
            int best = Mathf.Max(Stars(chapter), Mathf.Clamp(stars, 0, 3));
            try
            {
                PlayerPrefs.SetInt(StarsPrefix + chapter, best);
                PlayerPrefs.Save();
            }
            catch (Exception) { }
            return best;
        }

        /// <summary>Stars collected across the ladder, for the lobby's chapter summary.</summary>
        public static int TotalStars
        {
            get
            {
                Ensure();
                int sum = 0;
                foreach (var c in all) sum += Stars(c.Order);
                return sum;
            }
        }

        /// <summary>
        /// The chapter after this one on the ladder, or null at the top. A3 reads it for "บทถัดไป", so the last
        /// chapter of the story has to be answered with null rather than chapter 1.
        /// </summary>
        public static ChapterDefinition Next(ChapterDefinition chapter)
        {
            Ensure();
            if (chapter == null) return null;
            for (int i = 0; i < all.Length; i++)
                if (all[i].Order == chapter.Order) return i + 1 < all.Length ? all[i + 1] : null;
            return null;
        }

        /// <summary>Ladder problems for the editor menu (empty = the six chapters are ordered and unlock in order).</summary>
        public static List<string> Validate()
        {
            Ensure();
            var problems = new List<string>();
            if (!fromAssets) problems.Add("no ChapterDefinition asset under Resources/" + ResourcePath);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) { problems.Add("null entry at index " + i); continue; }
                if (i > 0 && all[i].Order <= all[i - 1].Order) problems.Add($"chapter order {all[i].Order} is not after {all[i - 1].Order}");
                if (i > 0 && all[i].UnlockLevel < all[i - 1].UnlockLevel) problems.Add($"chapter {all[i].Order} unlocks before chapter {all[i - 1].Order}");
                if (all[i].UnlockLevel > MaxLevel) problems.Add($"chapter {all[i].Order} unlocks at LV {all[i].UnlockLevel}, above the LV {MaxLevel} ladder");
                if (all[i].RoundTime < 30f) problems.Add($"chapter {all[i].Order} round time {all[i].RoundTime:0} s is below the 30 s floor");
            }
            if (Count > 0 && all[0].Order != 1) problems.Add("the ladder does not start at chapter 1");
            return problems;
        }

        static void Ensure()
        {
            if (all != null) return;
            all = Resources.LoadAll<ChapterDefinition>(ResourcePath).Where(c => c != null).OrderBy(c => c.Order).ToArray();
            fromAssets = all.Length > 0;
            if (fromAssets) return;
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("[BobChapters] no assets under Resources/" + ResourcePath + ": playing the default chapter.");
            }
            all = new[] { ScriptableObject.CreateInstance<ChapterDefinition>() };
        }
    }
}
