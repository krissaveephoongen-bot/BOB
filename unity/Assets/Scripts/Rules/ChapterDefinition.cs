// Battle of the Brooms: one chapter of the story ladder (docs 24 section 15.1, P0 of section 15.3). Data only —
// the S13 stage select reads these assets, and the TaskRace scene applies `Difficulty` and `RoundTime` to the round
// it already has. Nothing here changes how a task is drawn: the list still comes from BobTaskRaceRules.
using System.Collections.Generic;
using UnityEngine;

namespace Bob.Rules
{
    [CreateAssetMenu(menuName = "BOB/Chapter", fileName = "Chapter")]
    public class ChapterDefinition : ScriptableObject
    {
        [Header("Identity (docs 24 section 15.1)")]
        [SerializeField, Min(1)] int order = 1;
        [SerializeField] string title = "ห้องเช่า";
        [Tooltip("The place of the chapter, one short line under the title in S13.")]
        [SerializeField] string place = "ห้องเดียวขนาดเล็ก";
        [Tooltip("Player level this chapter opens at; the ladder is levels 1-50 (docs 24 section 15.1).")]
        [SerializeField, Min(1)] int unlockLevel = 1;

        [Header("Round handed to the TaskRace scene")]
        [Tooltip("D1-D3 for TaskRaceRound: D2/D3 stretch every minigame by 15% per step (docs 16).")]
        [SerializeField, Range(1, 3)] int difficulty = 1;
        [Tooltip("Match clock of this chapter, seconds. The scene's serialized 120 s is the fallback, not the rule.")]
        [SerializeField, Min(30f)] float roundTime = 120f;

        [Header("Lobby")]
        [Tooltip("Art id from the art library (art/docs/23); the lobby shows it behind Home and on the S13 card.")]
        [SerializeField] string backdrop = "lobby_rental_room";
        [TextArea(2, 4)] [SerializeField] string blurb = "";
        [Tooltip("The story in one line under the chapter title on the S13 card (docs 29 section 5).")]
        [TextArea(1, 2)] [SerializeField] string storyLine = "";
        [Tooltip("The line the chapter card closes with once the chapter is passed (docs 29 section 5).")]
        [TextArea(1, 2)] [SerializeField] string clearedLine = "";
        [Tooltip("Rule cards of the GDD, listed in S13. Enforcing them is P2 of docs 24 section 15.3, not this asset.")]
        [SerializeField] string[] ruleCards = new string[0];

        public int Order => Mathf.Max(1, order);
        public string Title => string.IsNullOrEmpty(title) ? "บท " + Order : title;
        public string Place => place ?? "";
        public int UnlockLevel => Mathf.Max(1, unlockLevel);
        public int Difficulty => Mathf.Clamp(difficulty, 1, 3);
        public float RoundTime => Mathf.Max(30f, roundTime);
        public string Backdrop => string.IsNullOrEmpty(backdrop) ? "lobby_rental_room" : backdrop;
        public string Blurb => blurb ?? "";
        /// <summary>One story line for the S13 card (docs 29); empty when the asset has not been filled yet.</summary>
        public string StoryLine => storyLine ?? "";
        /// <summary>The closing line of the chapter card; empty when the asset has not been filled yet.</summary>
        public string ClearedLine => clearedLine ?? "";
        public IReadOnlyList<string> RuleCards => ruleCards ?? System.Array.Empty<string>();

        /// <summary>Best-score band of this chapter: 1 = chapters 1-2, 2 = 3-4, 3 = 5-6 (the 4.6 bands).</summary>
        public int Band => Mathf.Clamp((Order + 1) / 2, 1, 3);

        /// <summary>One line for the S13 card: "บท 3 · D2 · LV 13".</summary>
        public string Tag => $"LV {UnlockLevel} · D{Difficulty}";
    }
}
