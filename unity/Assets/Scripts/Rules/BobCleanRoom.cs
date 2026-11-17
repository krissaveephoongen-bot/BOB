// Battle of the Brooms: S25, the clean-room card of docs 11 ("ห้องสะอาด (ก่อน-หลัง) · แบ่งครึ่งจอ 4 วิ แสดง % สองทีม").
//
// The round already knows two numbers per side: how many tasks were done and how much weight they carried. This file
// turns them into the two halves the card draws — "ก่อน" is the island as the round found it, "หลัง" is what the team
// left behind — and the one line that compares the two sides without deciding anything. It reads no Unity type, so
// the percentages can be checked without an editor.
namespace Bob.Rules
{
    /// <summary>One side of the split card: the room before the round and the room as it was left.</summary>
    public readonly struct CleanShot
    {
        public readonly int Before, After;

        public CleanShot(int before, int after)
        {
            Before = Clamp(before);
            After = Clamp(after);
        }

        /// <summary>How much of the room this side actually cleaned, in points of percentage.</summary>
        public int Gain => After - Before;

        static int Clamp(int v) => v < 0 ? 0 : v > 100 ? 100 : v;
    }

    public static class BobCleanRoom
    {
        /// <summary>The share of the worksheet that is clean, as a percentage. An empty list reads as 100% clean.</summary>
        public static int Percent(int doneWeight, int totalWeight)
        {
            if (totalWeight <= 0) return 100;
            int d = doneWeight < 0 ? 0 : doneWeight;
            if (d >= totalWeight) return 100;
            return (int)((long)d * 100 / totalWeight);
        }

        /// <summary>
        /// The two halves of the card. A round starts on a dirty island (0%) and ends at whatever the sheet says, so
        /// "before" is the room the round was handed, not the previous round's leftovers.
        /// </summary>
        public static CleanShot Of(int doneWeight, int totalWeight) => new CleanShot(0, Percent(doneWeight, totalWeight));

        /// <summary>The one line under the split: both sides' before/after, ours first (docs 11 S25).</summary>
        public static string Line(CleanShot ours, CleanShot theirs)
        {
            string lead = ours.After > theirs.After ? "ห้องเราสะอาดกว่า"
                : ours.After < theirs.After ? "คู่แข่งสะอาดกว่า"
                : "สะอาดเท่ากัน";
            return $"ห้องสะอาด · เรา {ours.Before}%→{ours.After}% · คู่แข่ง {theirs.Before}%→{theirs.After}% · {lead}";
        }

        /// <summary>A shorter line for the narrow chip: who won the room, without the numbers twice.</summary>
        public static string Verdict(CleanShot ours, CleanShot theirs) =>
            ours.After > theirs.After ? "ห้องเราสะอาดกว่า"
            : ours.After < theirs.After ? "คู่แข่งสะอาดกว่า"
            : "สะอาดเท่ากัน";

        /// <summary>The bar fill of one half, 0..1, for a bar of any width.</summary>
        public static float Fill(CleanShot shot) => shot.After / 100f;
    }
}
