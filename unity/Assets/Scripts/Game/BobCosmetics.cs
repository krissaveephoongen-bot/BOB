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
        /// <summary>The sewn look (docs 11 S31, four indices "fabric:colour:pattern:accessory"); "" for none.</summary>
        public const string SewKey = "bob.sew.look";
        /// <summary>Shards already spent at the sewing table; the earned pool is BobSewing.ShardsEarned(matches).</summary>
        public const string SewShardsKey = "bob.sew.shards";

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

        /// <summary>The worn frame's colour at full strength; white when nothing (or a title) is worn.</summary>
        public static Color FrameColour
        {
            get
            {
                var def = Bob.Rules.BobUnlocks.Get(FrameId);
                if (def == null || def.Title || string.IsNullOrEmpty(def.Hex)) return Color.white;
                return BobUi.Hex(def.Hex);
            }
        }

        /// <summary>The stage disc colour: the worn frame, or the plain translucent white.</summary>
        public static Color FrameTint
        {
            get
            {
                // A sewn look sits over the frame (docs 11 S31): the cloth is what the player made, the frame is what
                // they earned, and the newest decoration should be the one they see.
                if (HasSewn)
                {
                    var sewn = ClothColour;
                    sewn.a = 0.55f;
                    return sewn;
                }
                if (!HasFrame) return PlainFrame;
                // The stage disc sits under the hero art, so the frame keeps the plain disc's softness instead of turning
                // into an opaque plate behind the character.
                var c = FrameColour;
                c.a = 0.55f;
                return c;
            }
        }

        /// <summary>
        /// The T04 cart body in the worn colour (docs 31 section 2.3): the card and the wardrobe preview both read this
        /// one value, so the paint a player sees in the wardrobe is the paint they get in the minigame. No rule reads it.
        /// </summary>
        public static Color CartPaint
        {
            get
            {
                if (HasSewn)
                {
                    var cloth = SewnCloth;
                    var sewnBody = Bob.Rules.BobCartPaint.Body(true, cloth);
                    return new Color(sewnBody.R, sewnBody.G, sewnBody.B, 1f);
                }
                var frame = FrameColour;
                var body = Bob.Rules.BobCartPaint.Body(HasFrame, new Bob.Rules.BobRgb(frame.r, frame.g, frame.b));
                return new Color(body.R, body.G, body.B, 1f);
            }
        }

        // ---------------------------------------------------------------- the sewn look (docs 11 S31)

        /// <summary>The four stored indices of the sewn look, or null when nothing was cut (or the value is foreign).</summary>
        public static int[] SewnPicks
        {
            get
            {
                try { return Bob.Rules.BobSewing.Decode(PlayerPrefs.GetString(SewKey, "")); }
                catch (System.Exception) { return null; }
            }
        }

        public static bool HasSewn => SewnPicks != null;

        /// <summary>The stored look as it travels in the cloud profile ("" when none).</summary>
        public static string SewnText => Bob.Rules.BobSewing.Encode(SewnPicks);

        /// <summary>The cloth colour of the sewn look; cream when nothing was cut.</summary>
        public static Bob.Rules.BobRgb SewnCloth =>
            HasSewn ? Bob.Rules.BobSewing.Cloth(SewnPicks) : Bob.Rules.BobCartPaint.Cream;

        /// <summary>The cloth as a Unity colour (opaque) for the wardrobe preview.</summary>
        public static Color ClothColour
        {
            get
            {
                var c = SewnCloth;
                return new Color(c.R, c.G, c.B, 1f);
            }
        }

        /// <summary>The look in words, or "" when nothing was cut.</summary>
        public static string SewnName
        {
            get
            {
                var picks = SewnPicks;
                return picks == null ? "" : Bob.Rules.BobSewing.Describe(picks);
            }
        }

        /// <summary>Wear a sewn look. A look that is not four valid choices is refused, never half-applied.</summary>
        public static bool WearSewn(int[] picks)
        {
            try
            {
                string text = Bob.Rules.BobSewing.Encode(picks);
                if (string.IsNullOrEmpty(text)) return false;
                PlayerPrefs.SetString(SewKey, text);
                PlayerPrefs.Save();
                SaveToCloud();
                return true;
            }
            catch (System.Exception) { return false; }
        }

        /// <summary>Shards left at the sewing table: earned from matches minus what earlier cuts spent.</summary>
        public static int SewShardsLeft
        {
            get
            {
                int earned = Bob.Rules.BobSewing.ShardsEarned(Bob.TaskRace.BobTaskRaceProfile.Matches);
                int spent;
                try { spent = PlayerPrefs.GetInt(SewShardsKey, 0); } catch (System.Exception) { spent = 0; }
                return System.Math.Max(0, earned - spent);
            }
        }

        /// <summary>Pay the shard part of a cut; false (and nothing spent) when the pile is short.</summary>
        public static bool SpendSewShards(int shards)
        {
            if (shards <= 0) return true;
            if (SewShardsLeft < shards) return false;
            try
            {
                int spent;
                try { spent = PlayerPrefs.GetInt(SewShardsKey, 0); } catch (System.Exception) { spent = 0; }
                PlayerPrefs.SetInt(SewShardsKey, spent + shards);
                PlayerPrefs.Save();
                return true;
            }
            catch (System.Exception) { return false; }
        }

        public static void ClearSewn()
        {
            try
            {
                PlayerPrefs.SetString(SewKey, "");
                PlayerPrefs.Save();
                SaveToCloud();
            }
            catch (System.Exception) { }
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
                    SaveToCloud();
                    return true;
                }
                var def = Bob.Rules.BobUnlocks.Get(id);
                if (def == null || !IsOwned(id)) return false;
                PlayerPrefs.SetString(def.Title ? TitleKey : FrameKey, def.Id);
                PlayerPrefs.Save();
                SaveToCloud();
                return true;
            }
            catch (System.Exception) { return false; }
        }

        /// <summary>
        /// Adopt what the account already owns and wears when a cloud profile arrives: the owned ids are merged (never
        /// replaced, so offline play is not lost) and each slot keeps the device's choice only when that id is still
        /// owned and belongs to the slot. Called by the cloud save manager after a successful load.
        /// </summary>
        public static void AdoptCloud(string cloudTitle, string cloudFrame, System.Collections.Generic.ICollection<string> cloudOwned,
                                      string cloudSewn = "")
        {
            try
            {
                var local = OwnedIds();
                var merged = Bob.Rules.BobUnlocks.MergeOwned(local, cloudOwned);
                PlayerPrefs.SetString(OwnedKey, string.Join(",", merged));
                string title = Bob.Rules.BobUnlocks.ResolveWorn(TitleId, cloudTitle, true, merged);
                string frame = Bob.Rules.BobUnlocks.ResolveWorn(FrameId, cloudFrame, false, merged);
                PlayerPrefs.SetString(TitleKey, title);
                PlayerPrefs.SetString(FrameKey, frame);
                // The sewn look is the player's own work, not a piece of the table: a device that has one keeps it,
                // and a fresh install takes the account's when the text still parses as four valid choices.
                if (!HasSewn && Bob.Rules.BobSewing.Decode(cloudSewn) != null)
                    PlayerPrefs.SetString(SewKey, cloudSewn);
                PlayerPrefs.Save();
            }
            catch (System.Exception) { }
        }

        /// <summary>Push both slots to the account so a reinstall wears the same thing (best effort, silent offline).</summary>
        public static void SaveToCloud()
        {
            try
            {
                var svc = Bob.Ugs.UgsCloudSaveManager.Instance;
                if (svc != null) _ = svc.SetEquippedAndSaveAsync(TitleId, FrameId, SewnText);
            }
            catch (System.Exception) { }
        }

        /// <summary>The owned list as ids, read the way the shop writes it.</summary>
        public static System.Collections.Generic.List<string> OwnedIds()
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var s in PlayerPrefs.GetString(OwnedKey, "").Split(','))
                    if (s.Length > 0) list.Add(s);
            }
            catch (System.Exception) { }
            return list;
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
