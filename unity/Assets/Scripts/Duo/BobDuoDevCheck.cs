// Battle of the Brooms: the evidence run for the M1 slice. Plays the solo smoke match through one full task, asserts the
// HUD moved, writes a screenshot under Logs/ and quits Play mode. Armed only by the editor menu.
using System.IO;
using UnityEngine;

namespace Bob.Duo
{
    public sealed class BobDuoDevCheck : MonoBehaviour
    {
        BobDuoMatch match;
        BobDuoHud hud;
        float startedAt = -1f;
        bool shot;
        bool finished;
        float driveAt;

        public void Arm(BobDuoMatch owner, BobDuoHud ui)
        {
            if (owner == null || ui == null) { Debug.LogWarning("[DuoCheck] not armed — match or HUD missing"); return; }
            match = owner; hud = ui; startedAt = Time.time;
        }

        /// <summary>The check plays slot 0 the way a finger would, so the run actually advances without a human.</summary>
        void Drive()
        {
            if (Time.time < driveAt) return;
            driveAt = Time.time + 0.3f;
            int index = Mathf.Clamp(match.TaskIndex, 0, BobDuoRules.TaskCount - 1);
            switch (BobDuoRules.GestureFor(index, 0))
            {
                case DuoGesture.Hold: hud.HoldForCheck(true); break;
                case DuoGesture.Rhythm: hud.HoldForCheck(false); match.Tap(0); break;
                default: hud.HoldForCheck(false); match.Drag(0, BobDuoRules.NeedFor(index, 0) / 12f); break;
            }
        }

        void Update()
        {
            if (match == null || startedAt < 0f) return;
            if (finished) return;
            if (Time.time - startedAt < 1.5f) return;
            Drive();

            if (!shot && Time.time - startedAt > 3.4f)
            {
                shot = true;
                var logs = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
                Directory.CreateDirectory(logs);
                ScreenCapture.CaptureScreenshot(Path.Combine(logs, "duo-slice.png"));
            }
            if (Time.time - startedAt > 9f)
            {
                // Task 0 is done only if both hands moved, so this fails when the local input never reached the sim.
                bool ok = (match.Running || match.Over) && match.Tasks[0].Done;
                finished = true;
                Debug.Log((ok ? "[DuoCheck] PASSED" : "[DuoCheck] FAILED") + " · task " + match.TaskIndex +
                          " · " + Mathf.RoundToInt(match.Percent) + "% · over=" + match.Over + " · screenshot unity/Logs/duo-slice.png");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Play");
#endif
            }
        }
    }
}
