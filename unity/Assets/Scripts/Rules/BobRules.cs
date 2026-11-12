// Battle of the Brooms: client copy of the stacking rules, used to draw live stat bars while the player builds.
// Pure C# (no UnityEngine) so it can be unit-tested outside Unity. The SERVER result is always the one that counts.
// Source of truth: prototype/index.html -> server/modules/index.js. unity-check/ verifies this file against the server.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bob.Rules
{
    public enum Family { Tool, Chem, Energy, Method }

    public sealed class Part
    {
        public string Id, Name; public Family Family; public int Cost; public int[] S; // S = clean, gentle, speed, shine
        public Part(string id, string name, Family f, int cost, params int[] s) { Id = id; Name = name; Family = f; Cost = cost; S = s; }
    }

    public sealed class Effect
    {
        public int[] D; public string Text; public int At; public string Src; public bool Good, Off;
        public int Sum => D[0] + D[1] + D[2] + D[3];
    }

    public sealed class EvalResult
    {
        public int[] Stats = new int[4]; public List<Effect> Fx = new List<Effect>(); public int Cost, Layers;
        public int Get(string key) => Stats[Array.IndexOf(BobRules.StatKeys, key)];
    }

    public sealed class ScoreResult { public bool Pass; public double Test, Budget, Time; public int Total; }

    public static class BobRules
    {
        public const int MaxLayers = 5, MaxMethods = 2, RoundBudget = 100, RoundTime = 100, HandSolo = 3, HandTeam = 2;
        public static readonly string[] StatKeys = { "clean", "gentle", "speed", "shine" };
        public static readonly string[] Methods = { "soak", "spray", "circle" };

        public static readonly Dictionary<string, Part> Parts = new[]
        {
            new Part("cloth", "ผ้า", Family.Tool, 10, 1, 3, 1, 2), new Part("sponge", "ฟองน้ำ", Family.Tool, 15, 2, 2, 1, 1),
            new Part("brush", "แปรง", Family.Tool, 20, 4, 0, 1, 0), new Part("broom", "ไม้กวาด", Family.Tool, 10, 1, 2, 3, 0),
            new Part("soap", "สบู่", Family.Chem, 15, 2, 1, 0, 1), new Part("vinegar", "น้ำส้มสายชู", Family.Chem, 20, 3, 0, 0, 1),
            new Part("soda", "เบกกิ้งโซดา", Family.Chem, 15, 2, 1, 0, 0), new Part("polish", "น้ำมันขัดเงา", Family.Chem, 25, 0, 1, 0, 4),
            new Part("hot", "น้ำร้อน", Family.Energy, 20, 2, 0, 1, 0), new Part("steam", "ไอน้ำ", Family.Energy, 30, 3, 1, 0, 1),
            new Part("scrub", "แรงขัด", Family.Energy, 15, 3, -1, 1, 0), new Part("air", "ลมเป่า", Family.Energy, 10, 0, 1, 2, 1),
            new Part("soak", "แช่ไว้", Family.Method, 5, 2, 0, -2, 0), new Part("spray", "ฉีดพ่น", Family.Method, 5, -1, 0, 2, 0),
            new Part("circle", "ถูวน", Family.Method, 5, 0, 0, 0, 2),
        }.ToDictionary(p => p.Id);

        sealed class Rule { public string[] Lo, Hi; public bool Sym; public int[] D; public string T; }
        static Rule R(string lo, string hi, int[] d, string t, bool sym = false) =>
            new Rule { Lo = lo.Split(','), Hi = hi.Split(','), D = d, T = t, Sym = sym };

        // Order-sensitive reactions between consecutive parts (methods are skipped). Lo sits below Hi.
        static readonly Rule[] Rules =
        {
            R("hot", "soap", new[] { 3, 0, 0, 0 }, "น้ำร้อนละลายสบู่ ขจัดคราบดีขึ้น"),
            R("soap", "hot", new[] { 0, 0, -2, 0 }, "ฟองล้น เสียเวลา"),
            R("vinegar", "soda", new[] { 2, -1, 0, 0 }, "ฟู่! กัดคราบแรงขึ้นแต่กัดพื้นผิวด้วย", true),
            R("sponge", "soap", new[] { 1, 1, 0, 0 }, "ฟองน้ำอุ้มสบู่ นุ่มและสะอาด"),
            R("brush", "scrub", new[] { 3, -2, 0, 0 }, "ขัดด้วยแปรงเต็มแรง สะอาดแต่เป็นรอย"),
            R("cloth", "polish", new[] { 0, 0, 0, 3 }, "ผ้ารองน้ำมันขัดเงา เงาวับ"),
            R("polish", "soap,vinegar,soda", new[] { 0, 0, 0, -2 }, "น้ำยาล้างความเงาออก"),
            R("steam", "cloth", new[] { 0, 1, 0, 2 }, "ไอน้ำแล้วเช็ดผ้า เงาและถนอม"),
            R("broom", "hot,steam", new[] { -2, 0, 0, 0 }, "ฝุ่นเปียกกลายเป็นโคลน"),
            R("hot,steam", "air", new[] { 0, 0, 1, 1 }, "เป่าแห้งหลังเปียก"),
        };

        // A method reacts with the part directly below it: (method, below, delta, text).
        static readonly (string m, string[] below, int[] d, string t)[] MethodRules =
        {
            ("soak", new[] { "vinegar" }, new[] { 2, 0, 0, 0 }, "แช่น้ำส้มสายชู คราบหลุดง่าย"),
            ("spray", new[] { "soap" }, new[] { 0, 0, 1, 0 }, "ฉีดพ่นสบู่ ทั่วถึงเร็ว"),
            ("circle", new[] { "polish" }, new[] { 0, 0, 0, 2 }, "ถูวนน้ำมันขัดเงา เงาขึ้นอีก"),
            ("circle", new[] { "cloth" }, new[] { 0, 0, 0, 1 }, "ถูวนด้วยผ้า"),
        };

        public static bool IsMethod(string id) => Parts[id].Family == Family.Method;
        public static int StackCost(IEnumerable<string> stack) => stack.Sum(id => Parts[id].Cost);
        public static int CountLayers(IEnumerable<string> stack) => stack.Count(id => !IsMethod(id));
        public static int CountMethods(IEnumerable<string> stack) => stack.Count(IsMethod);

        /// <summary>Why a part cannot be placed on top of the stack right now, or null when it can.</summary>
        public static string WhyCannotPlace(IList<string> stack, string id, int budgetLeft)
        {
            if (IsMethod(id))
            {
                if (CountMethods(stack) >= MaxMethods) return "ใช้วิธีใช้ได้ 2 อย่างต่อกอง";
                if (stack.Count == 0 || IsMethod(stack[stack.Count - 1])) return "วิธีใช้ต้องวางบนอุปกรณ์ น้ำยา หรือแรงทำงาน";
            }
            else if (CountLayers(stack) >= MaxLayers) return "รถเข็นเต็ม 5 ชั้นแล้ว";
            return Parts[id].Cost > budgetLeft ? "งบไม่พอ" : null;
        }

        public static EvalResult EvalStack(IList<string> stack, IList<string> enemies = null, bool shield = false)
        {
            var r = new EvalResult();
            foreach (var id in stack) for (int i = 0; i < 4; i++) r.Stats[i] += Parts[id].S[i];
            var p = new List<int>();
            for (int i = 0; i < stack.Count; i++) if (!IsMethod(stack[i])) p.Add(i);

            for (int k = 0; k < p.Count - 1; k++)
            {
                string lo = stack[p[k]], hi = stack[p[k + 1]];
                foreach (var rule in Rules)
                {
                    bool hit = (rule.Lo.Contains(lo) && rule.Hi.Contains(hi)) || (rule.Sym && rule.Lo.Contains(hi) && rule.Hi.Contains(lo));
                    if (hit) r.Fx.Add(new Effect { D = rule.D, Text = rule.T, At = p[k + 1], Src = "pair" });
                }
            }
            for (int i = 1; i < stack.Count; i++)
                if (IsMethod(stack[i]))
                    foreach (var mr in MethodRules)
                        if (mr.m == stack[i] && mr.below.Contains(stack[i - 1]))
                            r.Fx.Add(new Effect { D = mr.d, Text = mr.t, At = i, Src = "method" });
            if (p.Count > 0 && stack[p[p.Count - 1]] == "air")
                r.Fx.Add(new Effect { D = new[] { 0, 0, 2, 0 }, Text = "จบด้วยลมเป่า แห้งไว", At = p[p.Count - 1], Src = "top" });

            bool Has(string id) => stack.Contains(id);
            int Cnt(params string[] ids) => stack.Count(ids.Contains);
            void Enemy(int[] d, string t) => r.Fx.Add(new Effect { D = d, Text = t, At = -1, Src = "enemy" });
            foreach (var e in enemies ?? Array.Empty<string>())
            {
                if (e == "dust")
                {
                    int dry = Cnt("cloth", "broom", "air"), wet = Cnt("hot", "steam");
                    if (dry > 0) Enemy(new[] { dry, 0, 0, 0 }, "Dust แพ้ของแห้ง");
                    if (wet > 0) Enemy(new[] { -2 * wet, 0, 0, 0 }, "Dust โดนน้ำกลายเป็นโคลน");
                }
                if (e == "grease")
                {
                    if (Has("hot") || Has("steam")) Enemy(new[] { 2, 0, 0, 0 }, "Grease แพ้ความร้อน");
                    else if (stack.Count > 0) Enemy(new[] { -3, 0, 0, 0 }, "Grease ไม่ออกเพราะไม่มีความร้อน");
                }
                if (e == "rust")
                {
                    if (Has("vinegar") && (Has("scrub") || Has("brush"))) Enemy(new[] { 3, 0, 0, 0 }, "Rust แพ้น้ำส้มสายชูกับแรงขัด");
                    if (Has("hot") || Has("steam") || Has("soak")) Enemy(new[] { -2, 0, 0, 0 }, "Rust ลามเพราะโดนน้ำหรือแช่ไว้");
                }
                if (e == "scale")
                {
                    if (Has("vinegar") && Has("soak")) Enemy(new[] { 3, 0, 0, 0 }, "Scale แพ้น้ำส้มสายชูที่แช่ไว้");
                    if (Has("scrub") || Has("brush")) Enemy(new[] { 0, -2, 0, 0 }, "ขัดแรงบนคราบหินปูน พื้นผิวเป็นรอย");
                }
            }
            foreach (var f in r.Fx) f.Good = f.Sum >= 0;
            if (shield)
            {
                Effect worst = null;
                foreach (var f in r.Fx) if (f.Sum < 0 && (worst == null || f.Sum < worst.Sum)) worst = f;
                if (worst != null) worst.Off = true;
            }
            foreach (var f in r.Fx) if (!f.Off) for (int i = 0; i < 4; i++) r.Stats[i] += f.D[i];
            r.Cost = StackCost(stack); r.Layers = CountLayers(stack);
            return r;
        }

        public static ScoreResult TotalScore(EvalResult ev, IDictionary<string, int> need, int budgetLeft, int budget, double timeFrac)
        {
            double acc = 0; bool pass = true;
            foreach (var kv in need)
            {
                int v = ev.Get(kv.Key);
                acc += Math.Max(0.0, Math.Min(1.0, (double)v / kv.Value));
                if (v < kv.Value) pass = false;
            }
            double baseScore = need.Count > 0 ? acc / need.Count * 100 : 0;
            double test = pass ? baseScore : baseScore * 0.5;
            double b = (double)Math.Max(0, budgetLeft) / budget * 100, t = Math.Max(0.0, Math.Min(1.0, timeFrac)) * 100;
            // Math.Floor(x + 0.5) matches JavaScript's Math.round; C# Math.Round would round halves to even.
            return new ScoreResult { Pass = pass, Test = test, Budget = b, Time = t, Total = (int)Math.Floor(0.7 * test + 0.2 * b + 0.1 * t + 0.5) };
        }
    }
}
