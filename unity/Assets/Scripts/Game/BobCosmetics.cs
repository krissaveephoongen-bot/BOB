// Battle of the Brooms: the player's cosmetics — which pieces are worn, and the counters they are earned with
// (docs 31 section 2.3). The table itself lives in Rules/BobUnlocks.cs; this file only reads the device.
//
// Two equipped slots, a title and a frame colour, because those are exactly the two cosmetic kinds the table ships:
// a title is a line under the hero's name on Home, a frame is the colour of the stage disc behind the hero and of the
// XP bar. Neither is read by any rule, so a player with nothing equipped plays the identical game.
using UnityEngine;

namespace Bob.Game
{
    public static class BobCosmetics
    {
        /// <summary>Which title is worn ("title_*" of BobUnlocks), "" for none.</summary>
        public const string TitleKey = "bob.equip.title";
        /// <summary>Which frame colour is worn ("frame_*" of BobUnlocks), "" for the plain disc.</summary>
        public const string FrameKey = "bob.equip.frame";
        /// <summary>The owned list the shop already writes; the wardrobe adds to the same key, never a second one.</summary>
        public const string OwnedKey = "bob.owned";

        /// <summary>The plain stage disc, used when no frame is worn.</summary>
        public static readonly Color PlainFrame = new Color(1f, 1f, 1f, 0.28f);

        /// <summary>Everything the unlock table needs, read from the counters the game already keeps.</summary>
        public static Bob.Rules.BobUnlockStats Stats()
        {
            int chapters = 0;
            for (int i = 1; i <= Bob.Rules.BobChapters.Count; i++)
                if (Bob.Rules.BobChapters.Cleared(i)) chapters++;
            return new Bob.Rules.BobUnlockStats(
                Bob.TaskRace.BobTaskRaceProfile.Matches,
                Bob.TaskRace.BobTaskRaceProfile.Wins,
                Bob.TaskRace.BobTaskRaceProfile.BestPoints,
                chapters,
                BobDailyStore.DaysPlayed);
        }

        public static string TitleId
        {
            get { try { return PlayerPrefs.GetString(TitleKey, ""); } catch (System.Exception) { return ""; } }
        }

        public static string FrameId
        {
            get { try { return PlayerPrefs.GetString(FrameKey, ""); } catch (System.Exception) { return ""; } }
        }

        /// <summary>The worn title as it is printed, "" when none (Home then shows no extra line).</summary>
        public static string TitleName
        {
            get
            {
                var def = Bob.Rules.BobUnlocks.Get(TitleId);
                return def != null && def.Title ? def.Name : "";
            }
        }

        public static bool HasFrame => Bob.Rules.BobUnlocks.Get(FrameId) != null && !Bob.Rules.BobUnlocks.Get(FrameId).Title;

        /// <summary>The worn frame at full strength, only meaningful when HasFrame; "" and titles fall back to the plain disc.</summary>
        public static Color FrameColour
        {
            get
            {
                var def = Bob.Rules.BobUnlocks.Get(FrameId);
                if (def == null || def.Title || string.IsNullOrEmpty(def.Hex)) return PlainFrame;
                return BobUi.Hex(def.Hex);
            }
        }

        /// <summary>The stage disc colour: the worn frame, or the plain translucent white.</summary>
        public static Color FrameTint
        {
            get
            {
                var c = FrameColour;
                // The stage disc sits under the hero art, so the frame keeps the plain disc's softness instead of turning
                // into an opaque plate behind the character.
                if (HasFrame) c.a = 0.55f;
                return c;
            }
        }

        /// <summary>Wear an owned piece in its slot; "" takes both off. A piece that is not owned is refused.</summary>
        public static bool Equip(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    PlayerPrefs.SetString(TitleKey, "");
                    PlayerPrefs.SetString(FrameKey, "");
                    PlayerPrefs.Save();
                    return true;
                }
                var def = Bob.Rules.BobUnlocks.Get(id);
                if (def == null || !IsOwned(id)) return false;
                PlayerPrefs.SetString(def.Title ? TitleKey : FrameKey, def.Id);
                PlayerPrefs.Save();
                return true;
            }
            catch (System.Exception) { return false; }
        }

        /// <summary>The owned list as the shop writes it: ids separated by commas.</summary>
        public static bool IsOwned(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            try
            {
                foreach (var s in PlayerPrefs.GetString(OwnedKey, "").Split(','))
                    if (s.Length > 0 && s == id) return true;
                return false;
            }
            catch (System.Exception) { return false; }
        }
    }
}
