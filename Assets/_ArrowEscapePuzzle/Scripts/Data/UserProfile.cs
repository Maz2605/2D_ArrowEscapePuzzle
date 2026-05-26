using System.Collections.Generic;
using ArrowGame.Data.Booster;

namespace ArrowGame.Data
{
    public class UserProfile
    {
        public int CurrentLevelIndex { get; set; } = 1;
        public int CurrentWinStreak { get; set; } = 0;
        public int Coin { get; set; } = 0;
        public Dictionary<BoosterType, int> BoosterInventory { get; set; } = new Dictionary<BoosterType, int>();
        public Dictionary<int, int> LevelStars { get; set; } = new Dictionary<int, int>();
    }
}
