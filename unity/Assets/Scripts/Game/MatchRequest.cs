// Battle of the Brooms: the lobby's pick for the next Task Race scene (docs 11 S11 -> S14).
// The match lives in its own scene, and loading a scene keeps statics, so the lobby hands over exactly what the player
// chose: which mode, whether the chapter is the three-round story, how large a team the round list is sized for, and
// which heroes fill the offline teammate slots until the online lobby of docs 27 can seat real friends.
//
// A direct play of a match scene (editor, verify, an old save) has Has == false, and the scene then keeps the values it
// was built with — the request never invents a mode.
using System;

namespace Bob.Game
{
    public static class MatchRequest
    {
        /// <summary>A lobby pick is waiting. False = the scene's own serialized values are the truth.</summary>
        public static bool Has { get; private set; }

        /// <summary>Mode id of the pick ("story", "solo", "duo", "team"): the log and the HUD read it.</summary>
        public static string ModeId { get; private set; } = "story";

        /// <summary>Players per side the round list is sized for: 1 for story/solo, 2 for Duo and 2v2, 3 for 3v3.</summary>
        public static int Players { get; private set; } = 1;

        /// <summary>Story chapter = three rounds, best of three. Every other pick is one timed round.</summary>
        public static bool Series { get; private set; } = true;

        /// <summary>Hero ids of the offline teammates on the player's island; empty for a solo or Duo pick.</summary>
        public static string[] Helpers { get; private set; } = Array.Empty<string>();

        public static void Set(string modeId, int players, bool series, string[] helpers)
        {
            ModeId = string.IsNullOrEmpty(modeId) ? "story" : modeId;
            Players = Math.Max(1, players);
            Series = series;
            Helpers = helpers ?? Array.Empty<string>();
            Has = true;
        }

        /// <summary>Home's play button and S13's start: the story chapter, three rounds, one hero.</summary>
        public static void Story() => Set("story", 1, true, Array.Empty<string>());
    }
}
