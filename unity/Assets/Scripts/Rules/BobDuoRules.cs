// Battle of the Brooms: the M1 duo slice (docs 32 sections 3-4). One room, six tasks, three gestures, two players.
// Pure C# so unity-check can prove the timeline and the table without Unity or a server.
using System;
using System.Collections.Generic;

namespace Bob.Duo
{
    public enum DuoPhase { Together1, Split, Together2, Done }

    /// <summary>The only three inputs the slice allows (docs 32 section 4).</summary>
    public enum DuoGesture { Hold, Rhythm, Drag }

    /// <summary>Slot 0 is the host device, slot 1 the joining device; which of them gives the first role swaps per task.</summary>
    public struct DuoTaskDef
    {
        public string Id, Name;
        public string PrimaryRole, SecondaryRole;
        public DuoGesture Primary, Secondary;
        public float PrimaryNeed, SecondaryNeed; // seconds of hold / good taps / drag units
    }

    public static class BobDuoRules
    {
        public const float Together1Seconds = 75f;
        public const float SplitSeconds = 105f;
        public const float Together2Seconds = 60f;
        public const float MatchSeconds = Together1Seconds + SplitSeconds + Together2Seconds; // 240 = 4:00
        public const int TaskCount = 6;
        public const float IdleAfter = 2.5f;
        public const float DecayPerSecond = 0.35f;

        // docs 32 section 4, row for row. Both columns of a task are shown, so a miss always reads as "the work", never
        // as the partner's fault.
        public static readonly DuoTaskDef[] Tasks =
        {
            new DuoTaskDef { Id = "duo.floor",   Name = "คราบติดพื้น", PrimaryRole = "ฉีดน้ำยาให้ทั่ว",   Primary = DuoGesture.Hold,   PrimaryNeed = 3.0f,  SecondaryRole = "ถูตามจังหวะ",   Secondary = DuoGesture.Rhythm, SecondaryNeed = 8f  },
            new DuoTaskDef { Id = "duo.trash",   Name = "เก็บขยะ",     PrimaryRole = "ลากถุงขยะ",        Primary = DuoGesture.Drag,   PrimaryNeed = 2600f, SecondaryRole = "โยนขยะลงถุง", Secondary = DuoGesture.Rhythm, SecondaryNeed = 8f  },
            new DuoTaskDef { Id = "duo.mirror",  Name = "เช็ดกระจก",   PrimaryRole = "ฉีดน้ำยาให้ทั่วแผง", Primary = DuoGesture.Hold,   PrimaryNeed = 2.5f,  SecondaryRole = "เช็ดตามแนว",   Secondary = DuoGesture.Drag,   SecondaryNeed = 2400f },
            new DuoTaskDef { Id = "duo.dishes",  Name = "ล้างจาน",     PrimaryRole = "ล้างจาน",          Primary = DuoGesture.Drag,   PrimaryNeed = 2800f, SecondaryRole = "เช็ดและวาง",   Secondary = DuoGesture.Rhythm, SecondaryNeed = 10f },
            new DuoTaskDef { Id = "duo.laundry", Name = "ตากผ้า",      PrimaryRole = "บิดผ้า",           Primary = DuoGesture.Hold,   PrimaryNeed = 3.0f,  SecondaryRole = "ตากและหนีบ",   Secondary = DuoGesture.Drag,   SecondaryNeed = 2600f },
            new DuoTaskDef { Id = "duo.ceiling", Name = "ฝุ่นเพดาน",   PrimaryRole = "ประคองบันได",      Primary = DuoGesture.Hold,   PrimaryNeed = 3.5f,  SecondaryRole = "ปัดฝุ่น",      Secondary = DuoGesture.Rhythm, SecondaryNeed = 12f },
        };

        public static DuoTaskDef Task(int index) => Tasks[Clamp(index, 0, TaskCount - 1)];

        public static DuoPhase PhaseAt(float elapsed)
        {
            if (elapsed < Together1Seconds) return DuoPhase.Together1;
            if (elapsed < Together1Seconds + SplitSeconds) return DuoPhase.Split;
            if (elapsed < MatchSeconds) return DuoPhase.Together2;
            return DuoPhase.Done;
        }

        /// <summary>First task that belongs to a phase: task 0 with-together, 1-4 split, 5 with-together again.</summary>
        public static int FirstTaskOf(DuoPhase phase)
        {
            switch (phase)
            {
                case DuoPhase.Together1: return 0;
                case DuoPhase.Split: return 1;
                case DuoPhase.Together2: return 5;
                default: return TaskCount;
            }
        }

        /// <summary>The slot that owns the primary role of a task. Alternating is what makes the two players swap hands.</summary>
        public static int PrimarySlot(int taskIndex) => (taskIndex & 1) == 0 ? 0 : 1;

        public static bool IsPrimary(int taskIndex, int slot) => slot == PrimarySlot(taskIndex);

        public static DuoGesture GestureFor(int taskIndex, int slot) =>
            IsPrimary(taskIndex, slot) ? Task(taskIndex).Primary : Task(taskIndex).Secondary;

        public static float NeedFor(int taskIndex, int slot) =>
            IsPrimary(taskIndex, slot) ? Task(taskIndex).PrimaryNeed : Task(taskIndex).SecondaryNeed;

        public static string RoleFor(int taskIndex, int slot) =>
            IsPrimary(taskIndex, slot) ? Task(taskIndex).PrimaryRole : Task(taskIndex).SecondaryRole;

        public static string HintFor(int taskIndex, int slot) => GestureFor(taskIndex, slot) switch
        {
            DuoGesture.Hold => "กดค้างบนการ์ดจนกว่าแถบจะเต็ม",
            DuoGesture.Rhythm => "แตะการ์ดตามจังหวะ ตรงกลางวงจะติด",
            _ => "ลากนิ้วบนการ์ดให้ทั่ว",
        };

        public static string PhaseThai(DuoPhase phase)
        {
            switch (phase)
            {
                case DuoPhase.Together1: return "ด้วยกัน";
                case DuoPhase.Split: return "แยกฝั่ง";
                case DuoPhase.Together2: return "ด้วยกันอีกครั้ง";
                default: return "จบ";
            }
        }

        /// <summary>Clock text for the HUD: 4:00 -> 0:00.</summary>
        public static string Clock(float elapsed)
        {
            float left = MatchSeconds - elapsed;
            if (left < 0f) left = 0f;
            int seconds = (int)Math.Ceiling(left);
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }

        /// <summary>Every assertion the offline gate runs. Returns one line per failure; empty means green.</summary>
        public static IEnumerable<string> SelfTest()
        {
            var bad = new List<string>();
            void Check(string name, bool ok) { if (!ok) bad.Add(name); }

            Check("match lasts 4:00", MatchSeconds == 240f);
            Check("phases add up", Together1Seconds + SplitSeconds + Together2Seconds == MatchSeconds);
            Check("first phase at 0", PhaseAt(0f) == DuoPhase.Together1);
            Check("with-together until 1:15", PhaseAt(74.99f) == DuoPhase.Together1);
            Check("split starts at 1:15", PhaseAt(75f) == DuoPhase.Split);
            Check("split until 3:00", PhaseAt(179.99f) == DuoPhase.Split);
            Check("final together at 3:00", PhaseAt(180f) == DuoPhase.Together2);
            Check("final together until 4:00", PhaseAt(239.99f) == DuoPhase.Together2);
            Check("bell at 4:00", PhaseAt(240f) == DuoPhase.Done);
            Check("done stays done", PhaseAt(999f) == DuoPhase.Done);

            Check("first split task is 1", FirstTaskOf(DuoPhase.Split) == 1);
            Check("final task is 5", FirstTaskOf(DuoPhase.Together2) == 5);
            Check("six tasks", Tasks.Length == TaskCount);

            for (int i = 0; i < TaskCount; i++)
            {
                var t = Task(i);
                Check("task " + i + " has an id", !string.IsNullOrEmpty(t.Id));
                Check("task " + i + " has a name", !string.IsNullOrEmpty(t.Name));
                Check("task " + i + " needs two roles", !string.IsNullOrEmpty(t.PrimaryRole) && !string.IsNullOrEmpty(t.SecondaryRole) && t.PrimaryRole != t.SecondaryRole);
                Check("task " + i + " halves differ", t.Primary != t.Secondary);
                Check("task " + i + " needs are positive", t.PrimaryNeed > 0f && t.SecondaryNeed > 0f);
                Check("task " + i + " swaps hands", PrimarySlot(i) == (i % 2 == 0 ? 0 : 1));
                Check("task " + i + " is two-handed", RoleFor(i, 0) != RoleFor(i, 1));
            }

            Check("only hold/rhythm/drag appear", UsesOnlyThreeGestures());
            Check("clock starts at 4:00", Clock(0f) == "4:00");
            Check("clock ends at 0:00", Clock(240f) == "0:00");

            // A task cannot be finished by one hand, and an idle hand drags the work back (spec section 4).
            var solo = new BobDuoTaskState(0);
            solo.Add(0, 1f);
            for (int i = 0; i < 60; i++) solo.Tick(0.1f, true, false);
            Check("one hand cannot finish", !solo.Done && solo.Charge(1) == 0f);

            var pair = new BobDuoTaskState(2);
            pair.Add(0, 1f); pair.Add(1, 1f);
            Check("both hands finish", pair.Done && pair.IsComplete);
            Check("finished progress is full", pair.Progress == 1f);

            var decay = new BobDuoTaskState(1);
            decay.Add(0, 0.9f); decay.Add(1, 0.9f);
            for (int i = 0; i < 30; i++) decay.Tick(0.1f, true, false); // 3 s: 0.5 s past the grace
            Check("idle hand loses progress", decay.Charge(1) < 0.9f && decay.Charge(1) > 0.7f);
            Check("idle hand stops the task", !decay.Done && decay.Progress < 0.9f);
            decay.Add(0, 1f); decay.Add(1, 1f);
            Check("a returning hand finishes it", decay.Done);

            var idle = new BobDuoTaskState(3);
            idle.Add(0, 0.5f);
            for (int i = 0; i < 100; i++) idle.Tick(0.1f, false, false);
            Check("progress never goes negative", idle.Charge(0) == 0f);

            var restored = new BobDuoTaskState(4);
            restored.Restore(0.4f, 0.6f, false);
            Check("snapshot restore keeps both halves", Math.Abs(restored.Charge(0) - 0.4f) < 0.0001f && Math.Abs(restored.Charge(1) - 0.6f) < 0.0001f);
            restored.Restore(1f, 1f, true);
            Check("snapshot restore can close a task", restored.Done);

            return bad;
        }

        static bool UsesOnlyThreeGestures()
        {
            foreach (var t in Tasks)
                foreach (var g in new[] { t.Primary, t.Secondary })
                    if (g != DuoGesture.Hold && g != DuoGesture.Rhythm && g != DuoGesture.Drag) return false;
            return true;
        }

        static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }
}
