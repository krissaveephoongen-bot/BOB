// Battle of the Brooms: the daily challenge's clock and attempt count, kept on the device (docs 31 section 2.2).
//
// The server owns the seed; the device owns "already played today". Two keys, one day number, nothing else: when the
// day in the reply differs from the stored day the counter is simply stale and reads as a fresh day.
using System;
using UnityEngine;

namespace Bob.Game
{
    public static class BobDailyStore
    {
        public const string DayKey = "bob.daily.day";
        public const string PlaysKey = "bob.daily.plays";

        /// <summary>The day number the stored count belongs to; 0 before the first play.</summary>
        public static int Day
        {
            get { try { return PlayerPrefs.GetInt(DayKey, 0); } catch (Exception) { return 0; } }
        }

        /// <summary>Attempts spent on that day.</summary>
        public static int Plays
        {
            get { try { return Mathf.Clamp(PlayerPrefs.GetInt(PlaysKey, 0), 0, 99); } catch (Exception) { return 0; } }
        }

        /// <summary>Attempts left on the day the server named.</summary>
        public static int AttemptsLeft(int todayDay) => Bob.Rules.BobDaily.AttemptsLeft(todayDay, Day, Plays);

        /// <summary>
        /// One daily round was played: count it against the day it belonged to. Called when the match ends, so quitting
        /// mid-round still spends the attempt — the seed was handed out and the clock ran.
        /// </summary>
        public static void Consume(int day)
        {
            if (day <= 0) return;
            try
            {
                int plays = Day == day ? Mathf.Clamp(PlayerPrefs.GetInt(PlaysKey, 0), 0, 99) : 0;
                PlayerPrefs.SetInt(DayKey, day);
                PlayerPrefs.SetInt(PlaysKey, Mathf.Min(99, plays + 1));
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        /// <summary>For the editor dev panel: forget today so the card can be walked through again.</summary>
        public static void Forget()
        {
            try { PlayerPrefs.DeleteKey(DayKey); PlayerPrefs.DeleteKey(PlaysKey); PlayerPrefs.Save(); } catch (Exception) { }
        }
    }
}
