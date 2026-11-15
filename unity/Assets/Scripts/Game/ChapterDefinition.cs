// The chapter layer of docs 24 section 15.3 P0: each asset is one of the six chapters of the GDD, and the stage select
// (S13) hands the picked chapter's difficulty band to the Task Race round the game already plays.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bob.Game
{
    /// <summary>One story chapter: the level it unlocks at, the band its round plays and how its card is drawn.</summary>
    [CreateAssetMenu(menuName = "BOB/Chapter Definition", fileName = "ch01_rental_room")]
    public sealed class ChapterDefinition : ScriptableObject
    {
        [SerializeField] string id = "ch01";
        [SerializeField] string title = "ห้องเช่า";
        [TextArea(2, 4), SerializeField] string blurb = "";
        [SerializeField, Min(1)] int unlockLevel = 1;
        [Tooltip("D1 = บท 1-2, D2 = 3-4, D3 = 5-6; each band stretches every minigame by 15% (docs 16).")]
        [SerializeField, Range(1, 3)] int difficulty = 1;
        [Tooltip("Lobby picture on the chapter card until the islands take a chapter skin (docs 24 section 15.3 P1).")]
        [SerializeField] string backdrop = "lobby_rental_room";

        public string Id => id;
        public string Title => title;
        public string Blurb => blurb;
        public int UnlockLevel => unlockLevel;
        public int Difficulty => difficulty;
        public string Backdrop => backdrop;
        public string Band => "D" + difficulty;
    }

    /// <summary>The six chapter assets in unlock order and the one the player last picked (device-local, like the profile).</summary>
    public static class Chapters
    {
        const string Key = "bob.chapter";
        static ChapterDefinition[] all;

        public static IReadOnlyList<ChapterDefinition> All
        {
            get
            {
                if (all == null || all.Length == 0)
                    all = Resources.LoadAll<ChapterDefinition>("Chapters").OrderBy(c => c.UnlockLevel).ThenBy(c => c.Id).ToArray();
                return all;
            }
        }

        /// <summary>The picked chapter; the first chapter until the player picks another, null before the assets exist.</summary>
        public static ChapterDefinition Selected
        {
            get
            {
                string id = PlayerPrefs.GetString(Key, "");
                return All.FirstOrDefault(c => c.Id == id) ?? All.FirstOrDefault();
            }
        }

        public static int NumberOf(ChapterDefinition chapter)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] == chapter) return i + 1;
            return 0;
        }

        public static bool Unlocked(ChapterDefinition chapter, int level) => chapter != null && level >= chapter.UnlockLevel;

        public static void Select(ChapterDefinition chapter)
        {
            if (chapter == null) return;
            PlayerPrefs.SetString(Key, chapter.Id); PlayerPrefs.Save();
        }
    }
}
