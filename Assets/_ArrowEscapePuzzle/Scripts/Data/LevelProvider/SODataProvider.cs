using System.Collections.Generic;
using ArrowGame.Data.LevelProvider;
using ShareCore.Data;
using ShareCore.Interface;
using UnityEngine;

namespace ArrowGame.Data
{
    public class SODataProvider : MonoBehaviour, ILevelDataProvider
    {
        [Header("Level Database")]
        [Tooltip("Kéo thả các file LevelDataSO (Level_1, Level_2...) vào đây để test")]
        [SerializeField] private List<LevelDataSO> _levelDatabase;

        public LevelSaveData GetLevelData(string levelID)
        {
            LevelDataSO foundSO = _levelDatabase.Find(level => level.levelID == levelID);
            
            if (foundSO != null)
            {
                return foundSO.ToLevelSaveData(); 
            }

            Debug.LogError($"[SOLevelDataProvider] LỖI: Không tìm thấy level có ID: {levelID}");
            return null;
        }
    }
}