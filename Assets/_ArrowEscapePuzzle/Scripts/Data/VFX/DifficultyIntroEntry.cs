using System;
using ShareCore.Data;

namespace ArrowGame.Data.VFX
{
    [Serializable]
    public struct DifficultyIntroEntry
    {
        public LevelDifficulty difficulty;
        public DifficultyIntroProfileSO profile;
    }
}
