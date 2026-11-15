// Check for A2 (3-2-1-ลุย! before the clock starts). No Play mode: the timing is a pure function, so the whole
// sequence can be walked at 60 fps here, plus the manager wiring the countdown depends on.
//   -executeMethod Bob.EditorTools.RaceCountdownCheck.Run     (Editor closed on this project)
// Success = the Console shows "[CountdownCheck] COUNTDOWN CHECK PASSED".
using System.Collections.Generic;
using System.Reflection;
using Bob.TaskRace;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class RaceCountdownCheck
    {
        [MenuItem("Bob/Run Countdown Check")]
        public static void Run()
        {
            failures = 0;
            CheckTiming();
            CheckManagerWiring();
            bool ok = failures == 0;
            Debug.Log(ok ? "[CountdownCheck] COUNTDOWN CHECK PASSED"
                         : $"[CountdownCheck] COUNTDOWN CHECK FAILED ({failures} problem(s))");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        static int failures;

        static void Expect(bool condition, string what)
        {
            if (condition) Debug.Log($"[CountdownCheck] ok: {what}");
            else { failures++; Debug.LogWarning($"[CountdownCheck] FAILED: {what}"); }
        }

        static void CheckTiming()
        {
            const float step = BobRaceCountdown.StepSeconds;
            float total = BobRaceCountdown.TotalSeconds;
            Expect(Mathf.Approximately(total, 2.7f), $"the countdown lasts 2.7 s (got {total:0.###})");
            Expect(BobRaceCountdown.Steps.Length == 4, "four beats: 3, 2, 1, ลุย!");

            Expect(BobRaceCountdown.TextAt(0f) == "3", "\"3\" at 0.00 s");
            Expect(BobRaceCountdown.TextAt(step - 0.01f) == "3", $"\"3\" at {step - 0.01f:0.00} s (last frame)");
            Expect(BobRaceCountdown.TextAt(step) == "2", $"\"2\" at {step:0.00} s");
            Expect(BobRaceCountdown.TextAt(step * 2f) == "1", $"\"1\" at {step * 2f:0.00} s");
            Expect(BobRaceCountdown.TextAt(step * 3f) == "ลุย!", $"\"ลุย!\" at {step * 3f:0.00} s");
            Expect(BobRaceCountdown.TextAt(total - 0.01f) == "ลุย!", $"\"ลุย!\" at {total - 0.01f:0.00} s (last frame)");
            Expect(BobRaceCountdown.TextAt(total) == null, $"finished at {total:0.00} s");
            Expect(BobRaceCountdown.TextAt(99f) == null, "still finished at 99 s");
            Expect(BobRaceCountdown.TextAt(-1f) == "3", "a negative clock shows \"3\" instead of skipping a beat");

            // The real contract: walk the timeline at 60 fps and collect each change, as Update would.
            const float dt = 1f / 60f;
            var seen = new List<string>();
            string last = null;
            bool finishedOnce = false;
            for (float t = 0f; t < total + 0.5f; t += dt)
            {
                string now = BobRaceCountdown.TextAt(t);
                if (now != last && now != null) seen.Add(now);
                if (now == null) finishedOnce = true;
                last = now;
            }
            string order = string.Join(" → ", seen);
            Expect(order == "3 → 2 → 1 → ลุย!", $"60 fps walk shows {order}");
            Expect(finishedOnce, "the walk reaches the finished state");

            int frames = Mathf.CeilToInt(total / dt);
            Expect(frames >= 160 && frames <= 170, $"the clock is held for about {frames} frames ({total:0.0} s)");
        }

        static void CheckManagerWiring()
        {
            var type = typeof(TaskRaceModeManager);
            var field = type.GetField("countdownBeforeMatch", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Expect(field != null && field.FieldType == typeof(bool), "the manager has the countdownBeforeMatch switch");
            Expect(field != null && field.GetCustomAttribute<SerializeField>() != null, "the switch is serialized (tunable per scene)");
            Expect(type.GetProperty("IsCountingDown") != null, "the manager exposes IsCountingDown");
            Expect(type.GetProperty("CountdownSeconds") != null, "the manager exposes CountdownSeconds");
            Expect(type.GetMethod("StartCountdown") != null, "the manager can start the countdown");
            Expect(type.GetMethod("SkipCountdown") != null, "the manager can drop the countdown");
            Expect(type.GetEvent("CountdownFinished") != null, "the manager announces CountdownFinished");

            var view = typeof(BobRaceCountdown);
            Expect(view.GetProperty("Running") != null, "the view reports Running");
            Expect(view.GetMethod("Begin") != null && view.GetMethod("Cancel") != null, "the view has Begin/Cancel");
        }
    }
}
