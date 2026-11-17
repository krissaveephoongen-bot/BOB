// Battle of the Brooms: the daily challenge (docs 31 section 2.2), as pure C# with no Unity types.
//
// The seed of the day belongs to the server: the phone asks GET /daily/seed, and this file is the phone-side mirror of
// that answer, so a reply can be checked before a match starts. The maths is mulberry32 over the day number, written
// out by hand for the same reason BobTaskRaceRules is: the browser, the server and this file have to agree bit for bit.
//
// Why check a reply at all: the phone keeps the clock, and the day it thinks it is may be wrong. A seed whose day does
// not match its own seed (Matches) cannot come from this rule, so the client refuses it instead of starting a list
// nobody else is playing. The limit is per device (BobDailyStore), because the endpoint has no idea who is asking.
using System;

namespace Bob.Rules
{
    public static class BobDaily
    {
        /// <summary>Must match DAILY_SALT in server/daily.js and prototype/index.html.</summary>
        public const int Salt = 0x02f6e2b1;

        /// <summary>One graded attempt per day; a second attempt in the same day is the ordinary Solo mode.</summary>
        public const int RoundsPerDay = 1;

        /// <summary>The turn of the day is UTC+7 midnight, the owner's time zone, not UTC.</summary>
        public const long DayOffsetMs = 7 * 3600 * 1000L;

        public const long DayMs = 86400000L;

        /// <summary>The seed range of the Task Race scene: 1..8388607 (Random.Range(1, 8388608)).</summary>
        public const int SeedMax = 8388607;

        /// <summary>
        /// The day number since the epoch in UTC+7: 20774 for 2026-11-17. Whole days, so a seed never depends on what
        /// time of day the asking happens. The server sends this number; a phone can also compute it to compare.
        /// </summary>
        public static int DayNumber(long utcUnixMs)
        {
            long shifted = utcUnixMs + DayOffsetMs;
            if (shifted < 0) shifted = 0;
            return (int)(shifted / DayMs);
        }

        /// <summary>Milliseconds until the next turn of the day, for the "กลับมาพรุ่งนี้" line.</summary>
        public static long UntilNextDayMs(long utcUnixMs)
        {
            long shifted = utcUnixMs + DayOffsetMs;
            long into = shifted % DayMs;
            if (into < 0) into += DayMs;
            return DayMs - into;
        }

        /// <summary>The seed of one day, 1..8388607. Identical to dailySeed() on the server.</summary>
        public static int SeedFor(int dayNumber)
        {
            double u = Mulberry(dayNumber ^ Salt);
            return 1 + (int)(u * SeedMax);
        }

        /// <summary>True when the seed is the one this day must have; a mangled or stale reply fails here.</summary>
        public static bool Matches(int seed, int dayNumber) => seed == SeedFor(dayNumber);

        /// <summary>Attempts left today: a new day refills them, a played day does not.</summary>
        public static int AttemptsLeft(int todayDayNumber, int storedDayNumber, int playsToday)
        {
            if (storedDayNumber != todayDayNumber) return RoundsPerDay;
            return Math.Max(0, RoundsPerDay - Math.Max(0, playsToday));
        }

        /// <summary>
        /// The attempts that count for a day once the account's copy arrives (docs 31 section 2.2). The larger of the
        /// two sides wins, so a reinstall cannot refill a day the account already spent, and a device that played
        /// offline is not punished for it. A day that is not today contributes nothing.
        /// </summary>
        public static int PlaysToday(int todayDayNumber, int localDayNumber, int localPlays,
                                     int cloudDayNumber, int cloudPlays)
        {
            int local = localDayNumber == todayDayNumber ? Math.Max(0, localPlays) : 0;
            int cloud = cloudDayNumber == todayDayNumber ? Math.Max(0, cloudPlays) : 0;
            return Math.Max(local, cloud);
        }

        /// <summary>
        /// The daily leaderboard's id (docs 31 section 3): one board per day, so the daily is a race against the same
        /// seed rather than against every score ever posted. Leaderboard ids are lowercase letters, digits, dashes and
        /// underscores, 4..50 characters; this shape always fits.
        /// </summary>
        public static string DailyBoardName(int dayNumber) => "daily_" + Math.Max(0, dayNumber);

        /// <summary>True when a board id is one Unity Leaderboards will accept (used by the check, not by the game).</summary>
        public static bool ValidBoardName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 4 || name.Length > 50) return false;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (!ok) return false;
            }
            return true;
        }

        /// <summary>mulberry32, the same generator BobTaskRaceRules draws the task list with.</summary>
        static double Mulberry(int seed)
        {
            unchecked
            {
                int a = seed + 0x6D2B79F5;
                int t = (a ^ (int)((uint)a >> 15)) * (1 | a);
                t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
                return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
            }
        }
    }
}
