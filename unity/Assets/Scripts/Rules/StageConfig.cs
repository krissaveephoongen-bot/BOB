// Battle of the Brooms: one stage of the story ladder (owner spec 2026-11-15). Data only — the lobby's "เริ่มด่านนี้"
// starts the chapter, the stage with the same order follows it, and TaskRaceModeManager copies these numbers onto the
// match it already runs. Nothing here draws a list: the cards still come from BobTaskRaceRules.
using UnityEngine;

namespace Bob.Rules
{
    [CreateAssetMenu(menuName = "BOB/Stage", fileName = "Stage")]
    public class StageConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Min(1)] int order = 1;
        [SerializeField] string title = "ห้องเช่า";

        [Header("Clock and bridge (stage 1-6)")]
        [Tooltip("Round clock, seconds. Story rounds use the same clock, so a stage 5 round is 90 s each.")]
        [SerializeField, Min(30f)] float clockSeconds = 120f;
        [Tooltip("Seconds remaining when the gates drop. The 5 s warning starts 5 s before this.")]
        [SerializeField, Min(20f)] float bridgeOpenAtRemaining = 60f;

        [Header("Points")]
        [Tooltip("Paid on every Dirty -> Clean completion, re-cleans after a sabotage included.")]
        [SerializeField, Min(1)] int pointsPerTask = 100;
        [Tooltip("Paid to the winning team once the stage closes (stage 6 = 200).")]
        [SerializeField, Min(0)] int stageBonus = 0;

        [Header("Rules of the stage")]
        [Tooltip("Seconds the hero is stunned after closing a station card early; 0 = off (stage 3 = 1.5).")]
        [SerializeField, Min(0f)] float fragileStunSeconds = 0f;
        [Tooltip("Stain stations (T02 ถูคราบวน) need two completed passes before they count as clean (stage 4).")]
        [SerializeField] bool twoPassStains = false;
        [Tooltip("Each round deals tasks to stations at random instead of matching the station's category (stage 5).")]
        [SerializeField] bool randomizeSpots = false;

        public int Order => Mathf.Max(1, order);
        public string Title => string.IsNullOrEmpty(title) ? "ด่าน " + Order : title;
        public float ClockSeconds => Mathf.Max(30f, clockSeconds);
        public float BridgeOpenAtRemaining => Mathf.Clamp(bridgeOpenAtRemaining, 10f, ClockSeconds - 5f);
        public float WarningAtRemaining => BridgeOpenAtRemaining + 5f;
        public int PointsPerTask => Mathf.Max(1, pointsPerTask);
        public int StageBonus => Mathf.Max(0, stageBonus);
        public float FragileStunSeconds => Mathf.Max(0f, fragileStunSeconds);
        public bool TwoPassStains => twoPassStains;
        public bool RandomizeSpots => randomizeSpots;

        /// <summary>One line for S13: "120 วิ · สะพาน 60 วิ".</summary>
        public string Tag => $"{ClockSeconds:0} วิ · สะพาน {BridgeOpenAtRemaining:0} วิ";
    }
}
