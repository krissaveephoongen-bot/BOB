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
    }
}
