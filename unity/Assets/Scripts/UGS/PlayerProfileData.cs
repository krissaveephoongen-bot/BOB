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
    }
}
