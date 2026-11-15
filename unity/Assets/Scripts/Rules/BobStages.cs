// Battle of the Brooms: the six stages as the lobby and the TaskRace scene see them. The StageConfig assets live in
// Resources/Stages (rebuilt by "Bob > Stages > Rebuild Six Stage Assets"), and the stage follows the chapter pick by
// its order — picking chapter 3 starts stage 3. Never null: a missing asset falls back to the defaults of the class.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bob.Rules
{
    public static class BobStages
    {
        public const string PrefKey = "bob.stage";
        const string ResourcePath = "Stages";

        static StageConfig[] all;
        static bool warned, fromAssets;

        public static IReadOnlyList<StageConfig> All { get { Ensure(); return all; } }
        public static int Count => All.Count;

        public static StageConfig Get(int order)
        {
            Ensure();
            return all.FirstOrDefault(s => s.Order == order) ?? all[0];
        }

        /// <summary>The stage of the chapter picked in S13; never null.</summary>
        public static StageConfig Saved => Get(BobChapters.Saved.Order);

        public static void Select(StageConfig stage)
        {
            if (stage == null) return;
            try { PlayerPrefs.SetInt(PrefKey, stage.Order); PlayerPrefs.Save(); }
            catch (Exception) { }
        }

        /// <summary>Ladder problems for the editor menu (empty = stages 1-6 exist and are ordered).</summary>
        public static List<string> Validate()
        {
            Ensure();
            var problems = new List<string>();
            if (!fromAssets) problems.Add("no StageConfig asset under Resources/" + ResourcePath);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) { problems.Add("null entry at index " + i); continue; }
                if (i > 0 && all[i].Order <= all[i - 1].Order) problems.Add($"stage order {all[i].Order} is not after {all[i - 1].Order}");
            }
            if (Count > 0 && all[0].Order != 1) problems.Add("the ladder does not start at stage 1");
            return problems;
        }

        static void Ensure()
        {
            if (all != null) return;
            all = Resources.LoadAll<StageConfig>(ResourcePath).Where(s => s != null).OrderBy(s => s.Order).ToArray();
            fromAssets = all.Length > 0;
            if (fromAssets) return;
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("[BobStages] no assets under Resources/" + ResourcePath + ": playing the default stage.");
            }
            all = new[] { ScriptableObject.CreateInstance<StageConfig>() };
        }
    }
}
