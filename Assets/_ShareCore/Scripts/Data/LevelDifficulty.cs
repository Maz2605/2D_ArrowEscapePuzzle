namespace ShareCore.Data
{
    public enum LevelDifficulty
    {
        None,
        Easy,
        Normal,
        Medium,
        Hard,
        SuperHard,
        Legendary
    }
    public static class LevelDifficultyExtensions
    {
        public static float GetMultiplier(this LevelDifficulty difficulty)
        {
            return difficulty switch
            {
                LevelDifficulty.Easy => 1.0f,    
                LevelDifficulty.Normal => 1.0f,
                LevelDifficulty.Medium => 1.5f,
                LevelDifficulty.Hard => 2.0f,    
                LevelDifficulty.SuperHard =>3.0f,
                LevelDifficulty.Legendary => 5.0f,
                _ => 1.0f
            };
        }
    }
}