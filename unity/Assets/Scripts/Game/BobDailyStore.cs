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
        /// <summary>How many different days the player has played the daily on (docs 31 section 2.3 asks for 3).</summary>
        public const string DaysKey = "bob.daily.days";

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

        /// <summary>Distinct days the daily was played on; the wardrobe's "ขยันทุกวัน" title counts them.</summary>
        public static int DaysPlayed
        {
            get { try { return Mathf.Clamp(PlayerPrefs.GetInt(DaysKey, 0), 0, 9999); } catch (Exception) { return 0; } }
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
                bool fresh = Day != day;
                int plays = fresh ? 0 : Mathf.Clamp(PlayerPrefs.GetInt(PlaysKey, 0), 0, 99);
                PlayerPrefs.SetInt(DayKey, day);
                PlayerPrefs.SetInt(PlaysKey, Mathf.Min(99, plays + 1));
                // A new day is one more day the daily was played on, which is what the wardrobe's title counts.
                if (fresh) PlayerPrefs.SetInt(DaysKey, Mathf.Min(9999, DaysPlayed + 1));
                PlayerPrefs.Save();
                Publish();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Take today's attempts from the account (docs 31 section 2.2): the larger count wins, so deleting the device's
        /// data cannot refill a day the account already spent and playing offline is never punished. A newer day the
        /// account knows about is adopted as-is; the day itself refills the count anyway.
        /// </summary>
        public static void AdoptCloud(int cloudDay, int cloudPlays)
        {
            if (cloudDay <= 0) return;
            try
            {
                int today = Day;
                if (cloudDay == today)
                {
                    int plays = Bob.Rules.BobDaily.PlaysToday(today, today, Plays, cloudDay, cloudPlays);
                    PlayerPrefs.SetInt(PlaysKey, Mathf.Clamp(plays, 0, 99));
                }
                else if (cloudDay > today)
                {
                    PlayerPrefs.SetInt(DayKey, cloudDay);
                    PlayerPrefs.SetInt(PlaysKey, Mathf.Clamp(cloudPlays, 0, 99));
                }
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        /// <summary>Best effort: push the day and its count to the account so another device reads the same limit.</summary>
        static void Publish()
        {
            try
            {
                var svc = Bob.Ugs.UgsCloudSaveManager.Instance;
                if (svc != null) _ = svc.SetDailyAndSaveAsync(Day, Plays);
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
