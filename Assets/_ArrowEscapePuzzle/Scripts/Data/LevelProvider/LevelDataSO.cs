using System.Collections.Generic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Data.LevelProvider
{
    [CreateAssetMenu(fileName =  "LevelDataSO", menuName = "ArrowGame/LevelDataSO")]
    public class LevelDataSO : ScriptableObject
    { 
        public string levelID;
        public int width;
        public int height;
        public List<CellData> cells = new List<CellData>();
        public LevelDifficulty difficulty = LevelDifficulty.Normal;
        
        public LevelSaveData ToLevelSaveData()
        {
            return new LevelSaveData(levelID, width, height, difficulty)
            {
                Cells = this.cells
            };
        }
    }
}