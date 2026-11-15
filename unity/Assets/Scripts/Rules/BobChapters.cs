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

        public static bool Unlocked(ChapterDefinition chapter, int level) => chapter != null && level >= chapter.UnlockLevel;

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
