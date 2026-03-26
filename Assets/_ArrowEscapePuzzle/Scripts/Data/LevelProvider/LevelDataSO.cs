using System.Collections.Generic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Data
{
    [CreateAssetMenu(fileName =  "LevelDataSO", menuName = "ArrowGame/LevelDataSO")]
    public class LevelDataSO : ScriptableObject
    { 
        public string levelID;
        public int width;
        public int height;
        public List<CellData> cells = new List<CellData>();
        
        public LevelSaveData ToLevelSaveData()
        {
            return new LevelSaveData(levelID, width, height)
            {
                Cells = this.cells
            };
        }
    }
}