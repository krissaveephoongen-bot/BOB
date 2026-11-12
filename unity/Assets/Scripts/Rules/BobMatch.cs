// Battle of the Brooms: round generation and the simulated rival, ported from prototype/index.html (makeRound, aiBest).
// Pure C# like BobRules. The server's bob_make_round is the real source of rooms; MakeRound here is the offline fallback
// and must give the same room for the same seed (unity-check/ compares it with the server).
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bob.Rules
{
    /// <summary>mulberry32, bit for bit the same as the prototype's JavaScript version.</summary>
    public sealed class Mulberry
    {
        uint a;
        public Mulberry(long seed) { a = unchecked((uint)(int)seed); }
        public double Next()
        {
            unchecked
            {
                a += 0x6D2B79F5u;
                uint t = (a ^ (a >> 15)) * (1u | a);
                t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
        public int Below(int n) => (int)Math.Floor(Next() * n);
    }

    public sealed class EnemyInfo
    {
        public string Id, Name, Thai, Tip, Color; public (string k, int v)[] Need;
    }

    public sealed class CharInfo { public string Id, Name, Ability, Desc, Color; }

    public sealed class Round
    {
        public int Number; public string[] Enemies; public string[] Offer;
        public Dictionary<string, int> Need = new Dictionary<string, int>(); public List<string> Notes = new List<string>();
    }

    public sealed class RivalPlay { public List<string> Stack; public EvalResult Ev; public ScoreResult Score; }

    public static class BobMatch
    {
        public static readonly EnemyInfo[] Enemies =
        {
            new EnemyInfo { Id = "stain", Name = "Stain", Thai = "คราบทั่วไป", Color = "#7A5C3E", Need = new[] { ("clean", 6) }, Tip = "แพ้น้ำยาทุกชนิด" },
            new EnemyInfo { Id = "dust", Name = "Dust", Thai = "ฝุ่น", Color = "#A9A39A", Need = new[] { ("clean", 5), ("speed", 3) }, Tip = "แพ้ของแห้ง (ผ้า ไม้กวาด ลมเป่า) โดนน้ำแล้วเป็นโคลน" },
            new EnemyInfo { Id = "grease", Name = "Grease", Thai = "คราบมัน", Color = "#C9A227", Need = new[] { ("clean", 7) }, Tip = "แพ้ความร้อน (น้ำร้อน ไอน้ำ) ไม่มีความร้อนแทบไม่ออก" },
            new EnemyInfo { Id = "rust", Name = "Rust", Thai = "สนิม", Color = "#B5532A", Need = new[] { ("clean", 7), ("gentle", 1) }, Tip = "แพ้น้ำส้มสายชูคู่กับแรงขัดหรือแปรง โดนน้ำหรือแช่ไว้จะลาม" },
            new EnemyInfo { Id = "scale", Name = "Scale", Thai = "คราบหินปูน", Color = "#8FB8CC", Need = new[] { ("clean", 6), ("gentle", 3) }, Tip = "แพ้น้ำส้มสายชูที่แช่ไว้ ขัดแรงแล้วพื้นผิวเป็นรอย" },
        };
        public static EnemyInfo Enemy(string id) => Enemies.First(e => e.Id == id);

        public static readonly CharInfo[] Chars =
        {
            new CharInfo { Id = "sweep", Name = "Sweep", Color = "#F4A340", Ability = "จัดทัพ", Desc = "สลับที่ 2 ชั้นที่ติดกันโดยไม่ต้องยกออก" },
            new CharInfo { Id = "bubble", Name = "Bubble", Color = "#4EA8DE", Ability = "โล่ฟอง", Desc = "ยกเลิกปฏิกิริยาหักล้างที่แรงที่สุด 1 คู่ในรอบนี้" },
            new CharInfo { Id = "spray", Name = "Spray", Color = "#9B7EDE", Ability = "เล็งเป้า", Desc = "ดูคะแนนที่กองปัจจุบันจะได้ 1 ครั้ง" },
            new CharInfo { Id = "press", Name = "Press", Color = "#57CC99", Ability = "รีดเวลา", Desc = "หยุดนาฬิกา 8 วินาที" },
        };
        public static CharInfo Char(string id) => Chars.First(c => c.Id == id);

        static readonly (string k, int v, string t)[] Extras = { ("gentle", 3, "พื้นผิวบอบบาง"), ("shine", 4, "ลูกค้าอยากได้เงาวับ"), ("speed", 3, "ลูกค้ารีบ") };

        // Same order as Object.keys(PARTS) in the prototype; the RNG walks these lists, so the order matters.
        static readonly string[] PartOrder = { "cloth", "sponge", "brush", "broom", "soap", "vinegar", "soda", "polish", "hot", "steam", "scrub", "air", "soak", "spray", "circle" };

        static void BuildSpec(Round r, Mulberry rng)
        {
            foreach (var e in r.Enemies)
                foreach (var (k, v) in Enemy(e).Need)
                    r.Need[k] = Math.Max(r.Need.TryGetValue(k, out var o) ? o : 0, v);
            var pool = Extras.Where(x => !(r.Need.TryGetValue(x.k, out var o) && o >= x.v)).ToList();
            for (int i = 0; i < r.Number - 1 && pool.Count > 0; i++)
            {
                int j = rng.Below(pool.Count); var x = pool[j]; pool.RemoveAt(j);
                r.Need[x.k] = Math.Max(r.Need.TryGetValue(x.k, out var o) ? o : 0, x.v); r.Notes.Add(x.t);
            }
        }

        /// <summary>Offline copy of the server's bob_make_round.</summary>
        public static Round MakeRound(int round, int seed)
        {
            var rng = new Mulberry(seed + round * 7919L);
            var ids = Enemies.Select(e => e.Id).ToArray();
            string Pick() => ids[rng.Below(ids.Length)];
            for (int attempt = 0; attempt < 40; attempt++)
            {
                var enemies = new List<string> { round == 1 ? new[] { "stain", "dust", "grease" }[rng.Below(3)] : Pick() };
                if (round == 3) { var e2 = Pick(); while (e2 == enemies[0]) e2 = Pick(); enemies.Add(e2); }
                var r = new Round { Number = round, Enemies = enemies.ToArray() };
                BuildSpec(r, rng);
                List<string> Fam(Family f)
                {
                    var a = PartOrder.Where(k => BobRules.Parts[k].Family == f).ToList();
                    for (int i = a.Count - 1; i > 0; i--) { int j = rng.Below(i + 1); (a[i], a[j]) = (a[j], a[i]); }
                    return a;
                }
                r.Offer = Fam(Family.Tool).Take(3).Concat(Fam(Family.Chem).Take(3)).Concat(Fam(Family.Energy).Take(2)).ToArray();
                var chk = AiBest(r.Offer, r, BobRules.RoundBudget, 500, rng, 0.3);
                if (chk != null && chk.Score.Pass) return r;
            }
            var fb = new Round { Number = round, Enemies = new[] { "stain" }, Offer = new[] { "cloth", "sponge", "brush", "soap", "vinegar", "soda", "hot", "scrub" } };
            fb.Need["clean"] = 6;
            return fb;
        }

        static List<string> RandomStack(string[] offer, int budget, Mulberry rng)
        {
            int n = 2 + rng.Below(4), m = 0; var st = new List<string>();
            for (int i = 0; i < n; i++)
            {
                var id = offer[rng.Below(offer.Length)];
                if (BobRules.StackCost(st) + BobRules.Parts[id].Cost > budget) break;
                st.Add(id);
                if (m < BobRules.MaxMethods && rng.Next() < 0.3)
                {
                    var mm = BobRules.Methods[rng.Below(3)];
                    if (BobRules.StackCost(st) + 5 <= budget) { st.Add(mm); m++; }
                }
            }
            return st;
        }

        /// <summary>Best of `tries` random stacks: the simulated rival (few tries = weak, many = strong).</summary>
        public static RivalPlay AiBest(string[] offer, Round r, int budget, int tries, Mulberry rng, double timeFrac)
        {
            RivalPlay best = null;
            for (int i = 0; i < tries; i++)
            {
                var st = RandomStack(offer, budget, rng);
                if (BobRules.CountLayers(st) == 0) continue;
                var ev = BobRules.EvalStack(st, r.Enemies, false);
                var sc = BobRules.TotalScore(ev, r.Need, budget - ev.Cost, budget, timeFrac);
                if (best == null || sc.Total > best.Score.Total) best = new RivalPlay { Stack = st, Ev = ev, Score = sc };
            }
            return best;
        }

        /// <summary>The rival's play for a round, as the prototype's submit() does it.</summary>
        public static RivalPlay Rival(Round r, int seed, int rivalBudget)
        {
            var rng = new Mulberry(seed * 31L + r.Number);
            double tf = 0.1 + rng.Next() * 0.25; // JS evaluates this argument before aiBest runs
            return AiBest(r.Offer, r, rivalBudget, 5 + r.Number * 4, rng, tf);
        }
    }
}
