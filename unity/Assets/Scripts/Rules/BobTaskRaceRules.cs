// Battle of the Brooms: the Task Race list of docs 16 section 2.2, as pure C# with no Unity types.
// This is the phone-side source of truth for the seeded draw (prototype/index.html carries the same numbers):
// the same weights and per-step minimum times, so a future authoritative server can check every TASK_BEGIN /
// TASK_DONE a phone sends. Run SelfTest() after touching it.
//
// The draw is mulberry32, not System.Random: the browser and this file have to agree bit for bit,
// and only an algorithm written down by hand can promise that across runtimes.
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

        /// <summary>
        /// The seconds the emergency stains spawn: docs 16 section 5 row T16 asks for 2 / 2 / 3 enemies per round at
        /// D1 / D2 / D3, at fixed moments of the reference round so both islands meet them at the same second. The
        /// seed stays in the signature because the server replay and the browser mirror call it the same way.
        /// </summary>
        public static List<int> EmergencyTimes(int seed, int difficulty)
        {
            int d = Math.Max(1, Math.Min(3, difficulty));
            int n = d >= 3 ? 3 : 2;
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

        /// <summary>The four stat keys of BobRules.EvalStack, in the order the S28 cart bars draw them.</summary>
        public static readonly string[] CartKeys = { "clean", "gentle", "speed", "shine" };

        /// <summary>
        /// How many parts the T04 card lets the player take off the shelf (docs 16 section 4). Four is what makes the
        /// D3 worksheet (three of four conditions) reachable with the parts BobRules actually ships.
        /// </summary>
        public const int CartParts = 4;

        /// <summary>The shelf always shows six parts: a four-part solution plus two decoys.</summary>
        public const int CartShelfSize = 6;

        /// <summary>
        /// The catalogue in a fixed order, so the phone, the browser prototype and the server enumerate the same parts
        /// in the same sequence. BobRules.Parts is a dictionary; its order is not a contract we can rely on.
        /// </summary>
        public static readonly string[] PartIds =
        {
            "cloth", "sponge", "brush", "broom", "soap", "vinegar", "soda", "polish",
            "hot", "steam", "scrub", "air", "soak", "spray", "circle",
        };

        /// <summary>
        /// The most four parts can reach per stat in CartKeys order, best order, once pair/method reactions and the
        /// trailing air bonus land: 17 clean (brush→scrub, vinegar→soda), 9 gentle, 11 speed, 16 shine. A worksheet
        /// target above these can never be met, so CartNeed clamps to them; SelfTest recomputes this array from
        /// BobRules.EvalStack, so a part or reaction change fails the test instead of silently drifting.
        /// </summary>
        public static readonly int[] CartCeiling = { 17, 9, 11, 16 };

        /// <summary>The round's star condition, from the same first draw CartNeed makes.</summary>
        public static int CartStar(int seed) => (int)(new Rng(seed ^ unchecked((int)0xc2b2ae35u)).Next() * 4) % 4;

        /// <summary>
        /// The T04 worksheet of docs 16 section 4: what the cart must reach in each stat. One stat is the round's star
        /// condition (base 6), the rest sit three lower (base 3), and D2/D3 raise every number by one. Four parts and
        /// a warm-up of reactions are what make the D3 ask (three conditions, the star among them) reachable.
        /// </summary>
        public static Dictionary<string, int> CartNeed(int seed, int difficulty)
        {
            var rng = new Rng(seed ^ unchecked((int)0xc2b2ae35u));
            int d = Math.Max(1, Math.Min(3, difficulty));
            int star = (int)(rng.Next() * 4) % 4;
            var need = new Dictionary<string, int>();
            for (int i = 0; i < CartKeys.Length; i++)
            {
                int value = (i == star ? 6 : 3) + (d - 1) + (rng.Next() < 0.5 ? 0 : 1);
                need[CartKeys[i]] = Math.Max(3, Math.Min(CartCeiling[i], value));
            }
            return need;
        }

        /// <summary>
        /// Two stacks per star key (indices 2*star and 2*star+1) that meet the hardest worksheet (D3 + the jitter:
        /// star 9, the rest 6) with the star among its three conditions, so every seed has a shelf that can be passed.
        /// Verified by SelfTest and by server/task-race-test.js, both through BobRules.EvalStack.
        /// </summary>
        static readonly string[][] CartSolutions =
        {
            new[] { "sponge", "brush", "steam", "cloth" },   // clean
            new[] { "brush", "soap", "steam", "cloth" },     // clean, second shape
            new[] { "sponge", "soap", "steam", "cloth" },    // gentle
            new[] { "sponge", "steam", "cloth", "broom" },   // gentle, second shape
            new[] { "cloth", "brush", "broom", "air" },      // speed
            new[] { "broom", "sponge", "steam", "air" },     // speed, second shape
            new[] { "cloth", "polish", "sponge", "brush" },  // shine
            new[] { "cloth", "polish", "sponge", "steam" },  // shine, second shape
        };

        /// <summary>
        /// The shelf the T04 card offers for this seed: four parts that pass the hardest worksheet plus two decoys,
        /// drawn from the seed so both islands see the same shelf. Difficulty is in the signature for the server
        /// replay; the solution already covers D3, so it covers D1 and D2 as well.
        /// </summary>
        public static List<string> CartShelf(int seed, int difficulty)
        {
            int star = CartStar(seed);
            var options = new[] { CartSolutions[star * 2], CartSolutions[star * 2 + 1] };
            var rng = new Rng(seed ^ unchecked((int)0x27d4eb2du));
            var shelf = new List<string>(options[(int)(rng.Next() * options.Length) % options.Length]);

            var rest = PartIds.Where(id => !shelf.Contains(id)).ToList();
            for (int i = rest.Count - 1; i > 0; i--)
            {
                int j = (int)(rng.Next() * (i + 1));
                (rest[i], rest[j]) = (rest[j], rest[i]);
            }
            for (int i = 0; i < rest.Count && shelf.Count < CartShelfSize; i++) shelf.Add(rest[i]);
            return shelf;
        }

        /// <summary>Does some order of four parts off this shelf meet the pass rule for this worksheet?</summary>
        public static bool ShelfCanPass(IList<string> shelf, IDictionary<string, int> need, int difficulty)
        {
            int d = Math.Max(1, Math.Min(3, difficulty));
            for (int a = 0; a < shelf.Count; a++)
                for (int b = 0; b < shelf.Count; b++)
                {
                    if (b == a) continue;
                    for (int c = 0; c < shelf.Count; c++)
                    {
                        if (c == a || c == b) continue;
                        for (int e = 0; e < shelf.Count; e++)
                        {
                            if (e == a || e == b || e == c) continue;
                            var stack = new[] { shelf[a], shelf[b], shelf[c], shelf[e] };
                            if (CartPassCount(BobRules.EvalStack(stack).Stats, need) >= d) return true;
                        }
                    }
                }
            return false;
        }

        /// <summary>The best four-part stack per stat, recomputed from BobRules.EvalStack for SelfTest.</summary>
        static int[] BestCartStats()
        {
            var best = new int[4];
            for (int a = 0; a < PartIds.Length; a++)
                for (int b = 0; b < PartIds.Length; b++)
                {
                    if (b == a) continue;
                    for (int c = 0; c < PartIds.Length; c++)
                    {
                        if (c == a || c == b) continue;
                        for (int e = 0; e < PartIds.Length; e++)
                        {
                            if (e == a || e == b || e == c) continue;
                            var stats = BobRules.EvalStack(new[] { PartIds[a], PartIds[b], PartIds[c], PartIds[e] }).Stats;
                            for (int i = 0; i < 4; i++) if (stats[i] > best[i]) best[i] = stats[i];
                        }
                    }
                }
            return best;
        }

        /// <summary>How many of the four worksheet conditions the built cart meets (0-4).</summary>
        public static int CartPassCount(IList<int> stats, IDictionary<string, int> need)
        {
            int pass = 0;
            for (int i = 0; i < CartKeys.Length; i++)
            {
                int have = stats != null && i < stats.Count ? stats[i] : 0;
                if (have >= need[CartKeys[i]]) pass++;
            }
            return pass;
        }

        /// <summary>T04 pass rule: D1 needs one condition, D2 two, D3 three. A lock below that still counts as step 2.</summary>
        public static bool CartPassed(int passCount, int difficulty) =>
            passCount >= Math.Max(1, Math.Min(3, difficulty));

        /// <summary>
        /// The buff keys the cart hands the whole team for the rest of the round when it passes: the same four stats,
        /// mapped onto the systems that read them (docs 16 section 4).
        /// </summary>
        public static List<string> CartBuffs(IList<int> stats, IDictionary<string, int> need)
        {
            var keys = new List<string>();
            for (int i = 0; i < CartKeys.Length; i++)
            {
                int have = stats != null && i < stats.Count ? stats[i] : 0;
                if (have >= need[CartKeys[i]]) keys.Add(CartKeys[i]);
            }
            return keys;
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
                string em = string.Join(",", EmergencyTimes(seed, difficulty));
                if (em != emerg) bad.Add($"emergency seed {seed} D{difficulty}: {em} != {emerg}");
            }
            Case(1975, 1, 1, "T04 T09 T03 T05 T08 T10", 8, "40,75");
            Case(1975, 4, 3, "T04 T09 T07 T13 T03 T05 T08 T10 T15", 16, "40,75,100");
            Case(777, 2, 2, "T04 T14 T13 T01 T06 T15 T03", 13, "40,75");
            Case(42, 3, 1, "T04 T09 T13 T11 T05 T08 T02 T01 T15", 14, "40,75");
            Case(123456, 4, 1, "T04 T07 T09 T12 T02 T15 T08 T11 T01 T05 T06", 16, "40,75");

            // docs 16 section 5 row T16: 2 / 2 / 3 stains per round as difficulty rises.
            if (EmergencyTimes(1975, 1).Count != 2 || EmergencyTimes(1975, 2).Count != 2 || EmergencyTimes(1975, 3).Count != 3)
                bad.Add("emergency count per difficulty drifted");

            if (EmergencyEnemy(1975) != "scale") bad.Add("enemy 1975: " + EmergencyEnemy(1975) + " != scale");
            if (EmergencyEnemy(777) != "stain") bad.Add("enemy 777: " + EmergencyEnemy(777) + " != stain");
            if (EmergencyEnemy(123456) != "dust") bad.Add("enemy 123456: " + EmergencyEnemy(123456) + " != dust");
            if (MinMs("T07", 3) != 0 || MinMs("T13", 1) != 2000) bad.Add("MinMs table drifted");

            // T04 worksheet: deterministic per seed, in range, and the pass rule counts conditions.
            var need = CartNeed(1975, 1);
            var again = CartNeed(1975, 1);
            foreach (var k in CartKeys)
            {
                if (need[k] != again[k]) bad.Add("cart need not seeded for " + k);
                int ceiling = CartCeiling[System.Array.IndexOf(CartKeys, k)];
                if (need[k] < 3 || need[k] > ceiling) bad.Add($"cart need unreachable {k}={need[k]} > {ceiling}");
                if (CartNeed(1975, 3)[k] > ceiling) bad.Add($"cart need at D3 cannot be built: {k}");
                if (CartNeed(1975, 3)[k] < need[k]) bad.Add($"cart need shrinks with difficulty: {k}");
            }
            if (CartPassCount(new[] { 0, 0, 0, 0 }, need) != 0) bad.Add("cart pass count on an empty cart");
            if (CartPassCount(new[] { 99, 99, 99, 99 }, need) != 4) bad.Add("cart pass count on a perfect cart");
            if (CartPassed(1, 1) == false || CartPassed(1, 3) || CartPassed(2, 2) == false || CartPassed(2, 3))
                bad.Add("cart pass rule drifted");
            if (CartBuffs(new[] { 99, 0, 0, 0 }, need).Count != 1) bad.Add("cart buffs drifted");

            // Four parts is the hand size, and the ceiling is what four parts really reach with reactions.
            if (PartIds.Length != BobRules.Parts.Count || PartIds.Distinct().Count() != PartIds.Length)
                bad.Add("cart part order does not cover the catalogue exactly once");
            foreach (var id in PartIds) if (!BobRules.Parts.ContainsKey(id)) bad.Add("cart part order has an unknown part " + id);
            var best = BestCartStats();
            for (int i = 0; i < 4; i++)
                if (best[i] != CartCeiling[i]) bad.Add($"cart ceiling drifted: {CartKeys[i]} {best[i]} != {CartCeiling[i]}");

            // Every star key has a shelf solution that meets the D3 worksheet with the star among its conditions.
            for (int star = 0; star < 4; star++)
            {
                var hardest = new Dictionary<string, int>();
                for (int i = 0; i < CartKeys.Length; i++) hardest[CartKeys[i]] = i == star ? 9 : 6;
                for (int o = star * 2; o < star * 2 + 2; o++)
                {
                    var sol = CartSolutions[o];
                    if (sol.Length != CartParts || sol.Distinct().Count() != CartParts)
                    {
                        bad.Add($"cart solution for {CartKeys[star]} is not {CartParts} distinct parts");
                        continue;
                    }
                    var ev = BobRules.EvalStack(sol);
                    if (ev.Stats[star] < 9 || CartPassCount(ev.Stats, hardest) < 3)
                        bad.Add($"cart solution for {CartKeys[star]} no longer passes D3: {string.Join("+", sol)}");
                }
            }

            // The shelf really hands a passable four somewhere, for every seed and difficulty.
            foreach (var seed in new[] { 1975, 9, 42, 777 })
                for (int d = 1; d <= 3; d++)
                {
                    var shelf = CartShelf(seed, d);
                    if (shelf.Count != CartShelfSize || shelf.Distinct().Count() != CartShelfSize)
                        bad.Add($"cart shelf seed {seed} D{d} is not {CartShelfSize} distinct parts");
                    foreach (var id in shelf) if (!BobRules.Parts.ContainsKey(id)) bad.Add($"cart shelf seed {seed} has an unknown part {id}");
                    if (!ShelfCanPass(shelf, CartNeed(seed, d), d))
                        bad.Add($"cart shelf seed {seed} D{d} has no passing four: {string.Join("+", shelf)}");
                }

            // The same three rows server/task-race-test.js asserts (seed 1975): both sides draw these numbers.
            foreach (var row in new[] { (1, 3, 4, 7, 4), (2, 4, 5, 8, 5), (3, 5, 6, 9, 6) })
            {
                var g = CartNeed(1975, row.Item1);
                if (g["clean"] != row.Item2 || g["gentle"] != row.Item3 || g["speed"] != row.Item4 || g["shine"] != row.Item5)
                    bad.Add($"cart need 1975 D{row.Item1} drifted: {g["clean"]}/{g["gentle"]}/{g["speed"]}/{g["shine"]}");
            }
            // A golden shelf pins the C# and JS draws together (seed 1975 D3 -> the speed solution + two decoys).
            if (string.Join(" ", CartShelf(1975, 3)) != "broom sponge steam air circle soap")
                bad.Add("cart shelf 1975 D3 drifted: " + string.Join(" ", CartShelf(1975, 3)));
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
