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

        /// <summary>Best of three: a story chapter, and an online room (docs 27 section 10.7). Every other pick is one timed round.</summary>
        public static bool Series { get; private set; } = true;

        /// <summary>Hero ids of the offline teammates on the player's island; empty for a solo or Duo pick.</summary>
        public static string[] Helpers { get; private set; } = Array.Empty<string>();

        /// <summary>The match is an online room (docs 27): the seed is the host's and the other players are real clients.</summary>
        public static bool Online { get; private set; }

        /// <summary>This device hosts the online match (Netcode server); false for a joining client.</summary>
        public static bool Host { get; private set; }

        /// <summary>The host's round seed, so both devices draw the same task list. 0 = the scene's own seed.</summary>
        public static int Seed { get; private set; }

        /// <summary>
        /// The daily challenge (docs 31 section 2.2): one round on the server's seed. The attempt is spent when the
        /// match ends, not when the lobby opens, so a match that never starts costs nothing.
        /// </summary>
        public static bool Daily { get; private set; }

        /// <summary>The day number the daily seed belongs to; 0 for every other mode.</summary>
        public static int DayNumber { get; private set; }

        public static void Set(string modeId, int players, bool series, string[] helpers)
        {
            ModeId = string.IsNullOrEmpty(modeId) ? "story" : modeId;
            Players = Math.Max(1, players);
            Series = series;
            Helpers = helpers ?? Array.Empty<string>();
            Online = false;
            Host = false;
            Seed = 0;
            Daily = false;
            DayNumber = 0;
            Has = true;
        }

        /// <summary>
        /// An online room's start: a best-of-three series (docs 27 section 10.7), the host's seed for round one, and no
        /// offline teammates (the clients are). Rounds two and three get their seed from the host, not from this value.
        /// </summary>
        public static void SetOnline(bool host, int players, int seed)
        {
            ModeId = "online";
            Players = Math.Max(1, players);
            Series = true;
            Helpers = Array.Empty<string>();
            Online = true;
            Host = host;
            Seed = Math.Max(0, seed);
            Daily = false;
            DayNumber = 0;
            Has = true;
        }

        /// <summary>
        /// The daily round: one timed solo round drawn on the seed the server issued for that day. The seed is required
        /// (a daily without one would fall back to the scene's seed and quietly stop being the daily).
        /// </summary>
        public static void SetDaily(int dayNumber, int seed)
        {
            ModeId = "daily";
            Players = 1;
            Series = false;
            Helpers = Array.Empty<string>();
            Online = false;
            Host = false;
            Seed = Math.Max(0, seed);
            Daily = Seed > 0 && dayNumber > 0;
            DayNumber = Daily ? dayNumber : 0;
            Has = true;
        }

        /// <summary>Home's play button and S13's start: the story chapter, three rounds, one hero.</summary>
        public static void Story() => Set("story", 1, true, Array.Empty<string>());
    }
}
