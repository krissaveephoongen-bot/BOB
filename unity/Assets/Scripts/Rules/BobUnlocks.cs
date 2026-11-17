// Battle of the Brooms: what play earns a cosmetic (docs 31 section 2.3), as pure C# with no Unity types.
//
// The owner's rule (answer 4 of 17 Nov): progression unlocks decoration only. Nothing here changes a stat, a card, a
// clock or a score — every entry is a title drawn under the hero's name or the colour of the profile frame, so a
// newcomer who has played nothing loses no power, only glitter.
//
// A condition is one counter the device already keeps: matches, wins, best points, chapters cleared, daily days. The
// table is deliberately small and readable from the wardrobe screen: name, what it asks, how far along the player is.
// No server mirror: the cloud only stores the ids the device decided on (UgsCloudSaveManager), so the rule never has
// to be replayed by another runtime.
using System.Collections.Generic;

namespace Bob.Rules
{
    /// <summary>The five counters a cosmetic may ask about. One row per counter keeps the table easy to read.</summary>
    public enum UnlockKind { Matches, Wins, BestPoints, Chapters, DailyDays }

    /// <summary>The player's own counters, handed in by the game layer so this file stays free of PlayerPrefs.</summary>
    public readonly struct BobUnlockStats
    {
        public readonly int Matches, Wins, BestPoints, ChaptersCleared, DailyDays;

        public BobUnlockStats(int matches, int wins, int bestPoints, int chaptersCleared, int dailyDays)
        {
            Matches = matches; Wins = wins; BestPoints = bestPoints;
            ChaptersCleared = chaptersCleared; DailyDays = dailyDays;
        }
    }

    /// <summary>One cosmetic: what it is called, what it asks for, and how it is drawn.</summary>
    public sealed class UnlockDef
    {
        public readonly string Id, Name;
        public readonly UnlockKind Kind;
        public readonly int Target;
        /// <summary>True = a title under the hero's name; false = the colour of the profile frame.</summary>
        public readonly bool Title;
        /// <summary>Frame colour as "#RRGGBB"; a title ignores it.</summary>
        public readonly string Hex;

        public UnlockDef(string id, string name, UnlockKind kind, int target, bool title, string hex)
        {
            Id = id; Name = name; Kind = kind; Target = target; Title = title; Hex = hex;
        }
    }

    public static class BobUnlocks
    {
        /// <summary>
        /// Six pieces, two per habit: playing at all, winning, doing well (twice), clearing chapters, coming back daily.
        /// Ids are what the cloud save stores, so they never change once shipped.
        /// </summary>
        public static readonly UnlockDef[] All =
        {
            new UnlockDef("title_sweep",  "ป้าย: กวาดเก่ง",       UnlockKind.Matches,  3,    true,  ""),
            new UnlockDef("frame_teal",   "กรอบเขียว",            UnlockKind.Wins,     1,    false, "#2F7F6F"),
            new UnlockDef("frame_sun",    "กรอบทอง",              UnlockKind.BestPoints, 900, false, "#E8B53A"),
            new UnlockDef("title_steady", "ป้าย: ขยันทุกวัน",      UnlockKind.DailyDays, 3,   true,  ""),
            new UnlockDef("title_clean",  "ป้าย: เก็บครบทุกห้อง",   UnlockKind.Chapters, 3,    true,  ""),
            new UnlockDef("frame_pink",   "กรอบชมพู",             UnlockKind.BestPoints, 1500, false, "#D96E8A"),
        };

        public static UnlockDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        /// <summary>The Thai name of a counter, for the "อีก N ..." line under a locked piece.</summary>
        public static string KindLabel(UnlockKind kind) =>
            kind == UnlockKind.Matches ? "แมตช์"
            : kind == UnlockKind.Wins ? "ครั้งที่ชนะ"
            : kind == UnlockKind.BestPoints ? "คะแนนดีสุด"
            : kind == UnlockKind.Chapters ? "บทที่จบ"
            : "วันเดลี่";

        /// <summary>The counter a piece asks about, read out of the player's stats.</summary>
        public static int Value(UnlockKind kind, BobUnlockStats stats) =>
            kind == UnlockKind.Matches ? stats.Matches
            : kind == UnlockKind.Wins ? stats.Wins
            : kind == UnlockKind.BestPoints ? stats.BestPoints
            : kind == UnlockKind.Chapters ? stats.ChaptersCleared
            : stats.DailyDays;

        /// <summary>How far along the piece is, capped at what it asks, so the bar never overshoots.</summary>
        public static int Progress(UnlockDef def, BobUnlockStats stats) =>
            def == null ? 0 : System.Math.Min(def.Target, System.Math.Max(0, Value(def.Kind, stats)));

        public static bool Earned(UnlockDef def, BobUnlockStats stats) =>
            def != null && def.Target > 0 && Value(def.Kind, stats) >= def.Target;

        public static int EarnedCount(BobUnlockStats stats)
        {
            int n = 0;
            for (int i = 0; i < All.Length; i++) if (Earned(All[i], stats)) n++;
            return n;
        }

        /// <summary>The ids the player has earned but does not own yet, in table order (what the wardrobe grants).</summary>
        public static List<string> Missing(BobUnlockStats stats, ICollection<string> owned)
        {
            var list = new List<string>();
            for (int i = 0; i < All.Length; i++)
                if (Earned(All[i], stats) && (owned == null || !owned.Contains(All[i].Id))) list.Add(All[i].Id);
            return list;
        }

        /// <summary>One line for the wardrobe: what the piece asks, with the player's number next to it.</summary>
        public static string Requirement(UnlockDef def, BobUnlockStats stats)
        {
            if (def == null) return "";
            int have = System.Math.Max(0, Value(def.Kind, stats));
            return KindLabel(def.Kind) + " " + System.Math.Min(have, def.Target) + "/" + def.Target;
        }
    }
}
