// Battle of the Brooms: the practice tuning table (docs 11 S41). Four numbers the owner can nudge while the game
// runs, kept in one file so the panel, the match code and unity-check read the same defaults and the same limits.
// Pure C# (no UnityEngine): the PlayerPrefs half is BobTuningStore in Game/, and a run played with any knob off its
// shipped value is a PRACTICE run — it never posts a score and never writes a best (docs 31 section 2.6).
using System;
using System.Collections.Generic;

namespace Bob.Rules
{
    /// <summary>One knob: its name, its range, the grid the + / - buttons walk, and how the value reads on screen.</summary>
    public sealed class BobTuningDef
    {
        public readonly string Id, Label, Hint, Unit;
        public readonly int Default, Min, Max, Step;

        public BobTuningDef(string id, string label, string hint, int def, int min, int max, int step, string unit)
        {
            Id = id; Label = label; Hint = hint; Unit = unit;
            Default = def; Min = min; Max = max; Step = Math.Max(1, step);
        }

        /// <summary>Inside the knob's own range (never below Min, never above Max).</summary>
        public bool InRange(int value) => value >= Min && value <= Max;

        /// <summary>On the grid the buttons walk, counted from Min, so every reachable value is one press apart.</summary>
        public bool OnGrid(int value) => InRange(value) && (value - Min) % Step == 0;
    }

    /// <summary>
    /// The four knobs of the practice panel. They are deliberately the numbers that change how a round FEELS and not
    /// the ones a server or a second device has to agree with (a seed, a worksheet, a minimum step time): those stay
    /// in BobRules / BobTaskRaceRules untouched, so an online room and a tuned phone still play the same match.
    /// </summary>
    public static class BobTuning
    {
        public const string RoundTimeId = "roundTime";
        public const string BudgetId = "budget";
        public const string RivalSkillId = "rivalSkill";
        public const string TaskTimeId = "taskTime";

        public static readonly BobTuningDef[] Defs =
        {
            // The stacking round's build clock, in seconds.
            new BobTuningDef(RoundTimeId, "เวลาสร้างรถเข็น", "นาฬิกาของรอบวางซ้อน", 100, 60, 180, 10, "วิ"),
            // The cart budget of the stacking round, in points.
            new BobTuningDef(BudgetId, "งบต่อรอบ", "แต้มที่หยิบของได้ในหนึ่งรอบ", 100, 60, 160, 10, "แต้ม"),
            // The simulated rival's try count, as a percentage of the shipped 5 + round x 4.
            new BobTuningDef(RivalSkillId, "ความเก่งคู่แข่ง", "เทียบกับค่าที่ออกแบบไว้ (100 = เท่าเดิม)", 100, 10, 150, 10, "%"),
            // The Task Race clock, as a delta in seconds on the chapter's own clock (0 = the chapter decides).
            new BobTuningDef(TaskTimeId, "เวลารอบงาน", "บวกหรือลบวินาทีจากเวลาของบท", 0, -60, 60, 15, "วิ"),
        };

        public static BobTuningDef Def(string id)
        {
            for (int i = 0; i < Defs.Length; i++) if (Defs[i].Id == id) return Defs[i];
            return null;
        }

        public static int Clamp(BobTuningDef def, int value)
        {
            if (def == null) return value;
            return value < def.Min ? def.Min : value > def.Max ? def.Max : value;
        }

        /// <summary>Clamp, then move to the nearest point of the knob's grid (a stored value can be off-grid).</summary>
        public static int Snap(BobTuningDef def, int value)
        {
            if (def == null) return value;
            int v = Clamp(def, value);
            int steps = (int)Math.Round((v - def.Min) / (double)def.Step, MidpointRounding.AwayFromZero);
            return Clamp(def, def.Min + steps * def.Step);
        }

        /// <summary>One press of + or -: the neighbour on the grid, held inside the range.</summary>
        public static int Step(BobTuningDef def, int value, int direction)
        {
            if (def == null) return value;
            return Clamp(def, Snap(def, value) + def.Step * (direction < 0 ? -1 : 1));
        }

        /// <summary>"100 วิ", "120%", and for the clock delta "ตามบท" at zero.</summary>
        public static string Format(BobTuningDef def, int value)
        {
            if (def == null) return value.ToString();
            int v = Clamp(def, value);
            if (def.Id == TaskTimeId) return v == 0 ? "ตามบท" : (v > 0 ? "+" + v : v.ToString()) + " " + def.Unit;
            return def.Unit == "%" ? v + "%" : v + " " + def.Unit;
        }

        /// <summary>True while the knob still carries the number the game shipped with.</summary>
        public static bool Official(BobTuningDef def, int value) => def != null && Clamp(def, value) == def.Default;
    }

    /// <summary>
    /// The values one device is running with. It starts at the shipped numbers, every write clamps to the knob's own
    /// range, and "touched" simply means "differs from the shipped number" — which is what makes a run a practice run.
    /// </summary>
    public sealed class BobTuningState
    {
        readonly Dictionary<string, int> values = new Dictionary<string, int>();

        public BobTuningState()
        {
            for (int i = 0; i < BobTuning.Defs.Length; i++) values[BobTuning.Defs[i].Id] = BobTuning.Defs[i].Default;
        }

        public int Get(string id)
        {
            var def = BobTuning.Def(id);
            if (def == null) return 0;
            return values.TryGetValue(id, out int v) ? v : def.Default;
        }

        public void Set(string id, int value)
        {
            var def = BobTuning.Def(id);
            if (def == null) return;
            values[id] = BobTuning.Clamp(def, value);
        }

        public void Reset()
        {
            for (int i = 0; i < BobTuning.Defs.Length; i++) values[BobTuning.Defs[i].Id] = BobTuning.Defs[i].Default;
        }

        /// <summary>The knobs that are no longer at their shipped number, in table order.</summary>
        public List<BobTuningDef> Touched()
        {
            var list = new List<BobTuningDef>(BobTuning.Defs.Length);
            for (int i = 0; i < BobTuning.Defs.Length; i++)
                if (!BobTuning.Official(BobTuning.Defs[i], Get(BobTuning.Defs[i].Id))) list.Add(BobTuning.Defs[i]);
            return list;
        }

        public int TouchedCount => Touched().Count;

        /// <summary>No knob touched: the run counts like any other.</summary>
        public bool IsOfficial => TouchedCount == 0;

        /// <summary>The one line the settings card and the result card print.</summary>
        public string Summary => IsOfficial
            ? "ค่ามาตรฐาน · รอบนี้นับเป็นทางการ"
            : "ปรับ " + TouchedCount + " ค่า · รอบนี้เป็นรอบซ้อม";
    }
}
