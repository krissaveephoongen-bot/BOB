// Battle of the Brooms: the Task Race list of docs 16 section 2.2, as pure C# with no Unity types.
// This is the phone-side mirror of the rules the Nakama match runs (prototype/index.html -> server/modules/index.js):
// the same seeded draw, the same weights and the same per-step minimum times, so the server can check every
// TASK_BEGIN / TASK_DONE a phone sends. Run SelfTest() (or unity-check) after touching either side.
//
// The draw is mulberry32, not System.Random: the browser, the server and this file have to agree bit for bit,
// and only an algorithm written down by hand can promise that across three runtimes.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bob.Rules
{
    /// <summary>Task family of docs 16 section 1; a station only hosts its own family (or Everyone).</summary>
    public enum TaskCat { Sweep, Wash, Scrub, Spray, Everyone }

    /// <summary>Short / Long / Common of docs 16 section 2.1. The bar weight follows the step count.</summary>
    public enum TaskSpan { Short, Long, Common }

    /// <summary>One row of the minigame table: what the round can deal and what the server insists on.</summary>
    public sealed class TaskCard
    {
        public readonly string Id, Name;
        public readonly TaskCat Cat;
        public readonly TaskSpan Span;
        public readonly int Weight;
        /// <summary>How many steps the card has; a 3-step card is worth a third of its weight per step.</summary>
        public readonly int Steps;
        /// <summary>Minimum milliseconds the server accepts per step (0 = checked by position only).</summary>
        public readonly int[] MinMs;

        public TaskCard(string id, string name, TaskCat cat, TaskSpan span, int weight, int[] minMs)
        {
            Id = id; Name = name; Cat = cat; Span = span; Weight = weight;
            MinMs = minMs; Steps = Math.Max(1, minMs?.Length ?? 1);
        }
    }

    public static class BobTaskRaceRules
    {
        /// <summary>The 15 list minigames (docs 16). T16 is the emergency stain and never enters the list.</summary>
        public static readonly TaskCard[] All =
        {
            new TaskCard("T01", "กวาดตามทาง", TaskCat.Sweep, TaskSpan.Short, 1, new[] { 1500 }),
            new TaskCard("T02", "ถูคราบวน", TaskCat.Wash, TaskSpan.Short, 1, new[] { 1800 }),
            new TaskCard("T03", "ฉีดให้ตรงจุด", TaskCat.Spray, TaskSpan.Short, 1, new[] { 2500 }),
            new TaskCard("T04", "จัดรถเข็นตามสูตร", TaskCat.Everyone, TaskSpan.Long, 2, new[] { 1500, 600 }),
            new TaskCard("T05", "แยกขยะ", TaskCat.Sweep, TaskSpan.Short, 1, new[] { 2500 }),
            new TaskCard("T06", "บิดผ้าถู", TaskCat.Scrub, TaskSpan.Short, 1, new[] { 1500 }),
            new TaskCard("T07", "ล้างจานกองสูง", TaskCat.Wash, TaskSpan.Long, 3, new[] { 2500, 1500, 0 }),
            new TaskCard("T08", "เช็ดกระจก", TaskCat.Wash, TaskSpan.Short, 1, new[] { 1800 }),
            new TaskCard("T09", "แช่คราบรอ", TaskCat.Everyone, TaskSpan.Long, 2, new[] { 2000, 0 }),
            new TaskCard("T10", "ไล่ฝุ่นใต้โต๊ะ", TaskCat.Sweep, TaskSpan.Short, 1, new[] { 2500 }),
            new TaskCard("T11", "เติมน้ำยาตามระดับ", TaskCat.Spray, TaskSpan.Short, 1, new[] { 1200 }),
            new TaskCard("T12", "จัดโต๊ะเก้าอี้", TaskCat.Everyone, TaskSpan.Common, 2, new[] { 0 }),
            new TaskCard("T13", "ยกของหนักสองคน", TaskCat.Everyone, TaskSpan.Common, 4, new[] { 2000 }),
            new TaskCard("T14", "ล้างพื้นทั้งแถว", TaskCat.Scrub, TaskSpan.Long, 3, new[] { 1500, 1500, 1500 }),
            new TaskCard("T15", "จำลำดับเครื่องซักผ้า", TaskCat.Everyone, TaskSpan.Short, 1, new[] { 3000 }),
        };

        public const string CartId = "T04", EmergencyId = "T16";
        public const float RoundTime = 120f, EmergencyGrace = 25f, ResumeSec = 10f;
        static readonly TaskCat[] AllCats = { TaskCat.Sweep, TaskCat.Wash, TaskCat.Scrub, TaskCat.Spray };

        public static TaskCard Get(string id) => All.FirstOrDefault(g => g.Id == id);

        /// <summary>Steps are 1-based, like the messages on the wire.</summary>
        public static int MinMs(string id, int step)
        {
            var g = Get(id); if (g == null) return 0;
            return g.MinMs[Math.Max(0, Math.Min(g.MinMs.Length - 1, step - 1))];
        }

        /// <summary>Composition of docs 16 section 2.2, with the D2/D3 short-card trim for 3-4 player teams.</summary>
        public static void Composition(int players, int difficulty, out int common, out int longs, out int shorts)
        {
            if (players <= 1) { common = 0; longs = 2; shorts = 4; }
            else if (players == 2) { common = 1; longs = 2; shorts = 4; }
            else if (players == 3) { common = 1; longs = 2; shorts = 6; }
            else { common = 1; longs = 3; shorts = 7; }
            if (players >= 3) shorts -= Math.Max(0, Math.Min(2, difficulty - 1));
        }

        /// <summary>
        /// The seeded task list: the cart card first, then the long, the common and the short cards, never repeating.
        /// Identical to taskPlan() in the shared rules; the server refuses any card that is not in this list.
        /// </summary>
        public static List<TaskCard> PlanRound(int seed, int players, int difficulty, IList<TaskCat> cats = null)
        {
            Composition(players, difficulty, out int common, out int longs, out int shorts);
            int total = common + longs + shorts;
            var have = cats != null && cats.Count > 0 ? cats : AllCats;
            var rng = new Rng(seed ^ unchecked((int)0x5bd1e995u));
            var pool = All.Where(g => g.Cat == TaskCat.Everyone || have.Contains(g.Cat)).ToList();
            var list = new List<TaskCard> { Get(CartId) };

            Add(longs - 1, g => g.Span == TaskSpan.Long && g.Id != CartId);
            Add(common, g => g.Span == TaskSpan.Common);
            Add(shorts, g => g.Span == TaskSpan.Short);
            Add(total - list.Count, _ => true);
            Add(total - list.Count, _ => true, All);
            return list;

            void Add(int count, Func<TaskCard, bool> keep, TaskCard[] source = null)
            {
                var outList = (source ?? pool.ToArray()).Where(g => keep(g) && !list.Any(x => x.Id == g.Id)).ToList();
                for (int i = outList.Count - 1; i > 0; i--)
                {
                    int j = (int)(rng.Next() * (i + 1));
                    (outList[i], outList[j]) = (outList[j], outList[i]);
                }
                for (int i = 0; i < outList.Count && count > 0; i++) { list.Add(outList[i]); count--; }
            }
        }

        public static int TotalWeight(IEnumerable<TaskCard> plan) => plan.Sum(g => g.Weight);

        /// <summary>Weight-based progress of docs 16 section 2.1, from how many steps of each card are finished.</summary>
        public static float WeightDone(IReadOnlyList<TaskCard> plan, IReadOnlyList<int> steps)
        {
            float sum = 0f;
            for (int i = 0; i < plan.Count; i++)
            {
                int done = i < steps.Count ? Math.Max(0, Math.Min(plan[i].Steps, steps[i])) : 0;
                sum += plan[i].Weight * done / plan[i].Steps;
            }
            return sum;
        }

        /// <summary>The second the emergency stain spawns: the same 2-3 moments for both islands.</summary>
        public static List<int> EmergencyTimes(int seed)
        {
            var rng = new Rng(seed ^ unchecked((int)0x9e3779b9u));
            int n = 2 + (rng.Next() < 0.5 ? 0 : 1);
            var at = new[] { 40, 75, 100 };
            return Enumerable.Range(0, n).Select(i => at[i]).ToList();
        }

        /// <summary>The enemy that stains the map, drawn from the seed so both teams meet the same one.</summary>
        public static string EmergencyEnemy(int seed)
        {
            var rng = new Rng(seed ^ unchecked((int)0x85ebca6bu));
            var ids = new[] { "stain", "dust", "grease", "rust", "scale" };
            return ids[(int)(rng.Next() * ids.Length) % ids.Length];
        }

        /// <summary>
        /// The browser/server pairs this file must match. Returns one line per broken case; empty = in sync.
        /// The expected lists were produced by taskPlan() in the shared rules.
        /// </summary>
        public static List<string> SelfTest()
        {
            var bad = new List<string>();
            void Case(int seed, int players, int difficulty, string ids, int weight, string emerg)
            {
                var plan = PlanRound(seed, players, difficulty);
                string got = string.Join(" ", plan.Select(g => g.Id));
                if (got != ids) bad.Add($"plan seed {seed} players {players} D{difficulty}: {got} != {ids}");
                if (TotalWeight(plan) != weight) bad.Add($"weight seed {seed} players {players} D{difficulty}: {TotalWeight(plan)} != {weight}");
                string em = string.Join(",", EmergencyTimes(seed));
                if (em != emerg) bad.Add($"emergency seed {seed}: {em} != {emerg}");
            }
            Case(1975, 1, 1, "T04 T09 T03 T05 T08 T10", 8, "40,75,100");
            Case(1975, 4, 3, "T04 T09 T07 T13 T03 T05 T08 T10 T15", 16, "40,75,100");
            Case(777, 2, 2, "T04 T14 T13 T01 T06 T15 T03", 13, "40,75");
            Case(42, 3, 1, "T04 T09 T13 T11 T05 T08 T02 T01 T15", 14, "40,75");
            Case(123456, 4, 1, "T04 T07 T09 T12 T02 T15 T08 T11 T01 T05 T06", 16, "40,75");

            if (EmergencyEnemy(1975) != "scale") bad.Add("enemy 1975: " + EmergencyEnemy(1975) + " != scale");
            if (EmergencyEnemy(777) != "stain") bad.Add("enemy 777: " + EmergencyEnemy(777) + " != stain");
            if (EmergencyEnemy(123456) != "dust") bad.Add("enemy 123456: " + EmergencyEnemy(123456) + " != dust");
            if (MinMs("T07", 3) != 0 || MinMs("T13", 1) != 2000) bad.Add("MinMs table drifted");
            return bad;
        }

        /// <summary>mulberry32, written out so the browser, the server and this file draw the same numbers.</summary>
        sealed class Rng
        {
            int a;
            public Rng(int seed) { a = seed; }
            public double Next()
            {
                unchecked
                {
                    a += 0x6D2B79F5;
                    int t = (a ^ (int)((uint)a >> 15)) * (1 | a);
                    t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
                    return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
                }
            }
        }
    }
}
