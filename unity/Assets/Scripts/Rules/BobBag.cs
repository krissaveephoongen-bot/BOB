// Battle of the Brooms: S33, the bag (docs 11 "S33 กระเป๋า" — a tab of S31), as pure C# with no Unity types.
//
// The bag is the read-only half of the wardrobe: what the account already owns (titles and frame colours from
// Rules/BobUnlocks) plus the material the sewing table spends. It never buys, never sells and never equips — the owner
// settled that there is nothing to sell back (docs 11 S33) and nothing here changes a rule.
//
// Kept in Rules/ so the counts and the material line can be checked without an editor; the HUD only draws them.
using System.Collections.Generic;

namespace Bob.Rules
{
    /// <summary>How many of the six-piece table this account owns, split by what the pieces are.</summary>
    public readonly struct BobBagCounts
    {
        public readonly int Titles, Frames;

        public BobBagCounts(int titles, int frames)
        {
            Titles = titles < 0 ? 0 : titles;
            Frames = frames < 0 ? 0 : frames;
        }

        public int Total => Titles + Frames;
    }

    public static class BobBag
    {
        /// <summary>Count the owned ids against the table. An id that is not in the table is ignored, not counted.</summary>
        public static BobBagCounts Count(ICollection<string> owned)
        {
            int titles = 0, frames = 0;
            if (owned != null)
            {
                for (int i = 0; i < BobUnlocks.All.Length; i++)
                {
                    var def = BobUnlocks.All[i];
                    if (def == null || !owned.Contains(def.Id)) continue;
                    if (def.Title) titles++; else frames++;
                }
            }
            return new BobBagCounts(titles, frames);
        }

        /// <summary>The bag's first line: what is in it, or that it is empty.</summary>
        public static string Headline(BobBagCounts counts) =>
            counts.Total == 0
                ? "ยังไม่มีของแต่ง · เล่นแมตช์เพื่อปลด"
                : $"ของแต่ง {counts.Total} ชิ้น · ป้าย {counts.Titles} · กรอบ {counts.Frames}";

        /// <summary>
        /// The material line: earned from matches (BobSewing.ShardsEarned), how much is left, and how much the sewing
        /// table took. Left can never exceed earned, so a broken save cannot print a negative "used".
        /// </summary>
        public static string Materials(int earned, int left)
        {
            int e = earned < 0 ? 0 : earned;
            int l = left < 0 ? 0 : left > e ? e : left;
            return $"เกล็ดตัดเย็บ · เหลือ {l} · ได้มา {e} · ใช้ไป {e - l}";
        }

        /// <summary>The owner's rule, printed where a shop would put a sell button: nothing goes back.</summary>
        public static string SellNote => "ขายคืนไม่ได้ · ของแต่งไม่เพิ่มพลัง";
    }
}
