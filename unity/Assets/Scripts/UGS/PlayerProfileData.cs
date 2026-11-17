// Battle of the Brooms: the whole player save, stored as one Cloud Save JSON entry.
using System.Collections.Generic;

namespace Bob.Ugs
{
    [System.Serializable]
    public class PlayerProfileData
    {
        public int coins = 0;
        public int level = 1;
        public List<string> unlockedItems = new List<string>();
        // The two cosmetic slots (docs 31 section 2.3): ids from BobUnlocks, "" when nothing is worn. Kept here so a
        // reinstall wears the same title and frame; the device still decides what it owns before wearing anything.
        public string equippedTitle = "";
        public string equippedFrame = "";
        // The sewn look (docs 11 S31): four indices "fabric:colour:pattern:accessory", "" when nothing was cut. It is
        // the player's own work rather than a piece of the table, so it travels in its own field.
        public string sewnLook = "";
        // The daily attempt count (docs 31 section 2.2): which day the count belongs to and how many attempts were spent
        // on it. Kept per account so a reinstall cannot refill the day.
        public int dailyDay = 0;
        public int dailyPlays = 0;
    }
}
