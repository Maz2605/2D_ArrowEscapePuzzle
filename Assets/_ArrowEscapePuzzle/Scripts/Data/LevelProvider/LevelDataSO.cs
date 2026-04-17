using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Data.LevelProvider
{
    [CreateAssetMenu(fileName = "LevelDataSO", menuName = "ArrowGame/LevelDataSO")]
    public class LevelDataSO : ScriptableObject
    { 
        public string levelID;
        public int width = 10;
        public int height = 10;
        public LevelDifficulty difficulty = LevelDifficulty.Normal;

        public List<ArrowSaveData> arrows = new List<ArrowSaveData>();
        
        /// <summary>
        /// Chuyển đổi từ ScriptableObject sang đối tượng Data chuẩn để Logic Game sử dụng.
        /// </summary>
        public LevelSaveData ToLevelSaveData()
        {
            var data = new LevelSaveData(levelID, width, height, difficulty);
            data.Arrows = new List<ArrowSaveData>(this.arrows);
            
            return data;
        }
    }
}