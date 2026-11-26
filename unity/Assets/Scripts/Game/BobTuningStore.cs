// Battle of the Brooms: the PlayerPrefs half of the practice tuning table (docs 11 S41). Bob.Rules.BobTuning holds the
// knobs and the maths; this file remembers them between launches and answers the two questions the match code asks:
// "which number does this run use?" and "does this run count?". Nothing here talks to UGS — a practice run simply
// never posts, which is BobRaceScoreReporter's own rule (LastMatchRecorded = false).
using Bob.Rules;
using UnityEngine;

namespace Bob.Game
{
    /// <summary>The device's tuning values, loaded once per launch and written through PlayerPrefs on every change.</summary>
    public static class BobTuningStore
    {
        /// <summary>PlayerPrefs key of one knob: "bob.tune.&lt;id&gt;"; only a touched knob is ever written.</summary>
        public const string Prefix = "bob.tune.";

        static BobTuningState state;

        public static BobTuningState State => state ?? (state = Load());

        static BobTuningState Load()
        {
            var s = new BobTuningState();
            for (int i = 0; i < BobTuning.Defs.Length; i++)
            {
                var def = BobTuning.Defs[i];
                s.Set(def.Id, PlayerPrefs.GetInt(Prefix + def.Id, def.Default));
            }
            return s;
        }

        /// <summary>Any knob off its shipped number: this run is a practice run and nothing about it is saved.</summary>
        public static bool Practice => !State.IsOfficial;

        /// <summary>Set one knob and remember it, so the panel and the next launch read the same value.</summary>
        public static void Set(string id, int value)
        {
            var def = BobTuning.Def(id);
            if (def == null) return;
            State.Set(id, value);
            PlayerPrefs.SetInt(Prefix + def.Id, State.Get(id));
            PlayerPrefs.Save();
        }

        /// <summary>Back to the shipped numbers; the keys go away so a fresh install and a reset read the same.</summary>
        public static void Reset()
        {
            State.Reset();
            for (int i = 0; i < BobTuning.Defs.Length; i++) PlayerPrefs.DeleteKey(Prefix + BobTuning.Defs[i].Id);
            PlayerPrefs.Save();
        }

        // ---- the four readings the match code uses. Each falls back to the caller's shipped number unless a practice
        // ---- run is on, so a normal match and an online room can never drift.

        public static float RoundSeconds(float official) => Practice ? State.Get(BobTuning.RoundTimeId) : official;

        public static int Budget(int official) => Practice ? State.Get(BobTuning.BudgetId) : official;

        /// <summary>The rival's try count as a fraction of the shipped one; 1 when nothing is tuned.</summary>
        public static float RivalScale => Practice ? State.Get(BobTuning.RivalSkillId) / 100f : 1f;

        /// <summary>
        /// The Task Race clock for a chapter: the chapter's own seconds plus the tuned delta, never under the 30 s
        /// floor ApplyChapter already keeps, and untouched unless a practice run is on.
        /// </summary>
        public static float TaskSeconds(float official) =>
            Practice ? Mathf.Max(30f, official + State.Get(BobTuning.TaskTimeId)) : official;
    }
}
