using System;
using ArrowGame.Data;
using GameCore.Data;
using GameCore.Utils.DesignPattern.Events;
using ShareCore.Data;
using ShareCore.Interface;
using UnityEngine;
using Random = UnityEngine.Random;


namespace ArrowGame.Gameplay
{
    public class  LevelManager : MonoBehaviour
    {
        [Header("Dependencies")] [SerializeField]
        private MonoBehaviour dataProviderObject;

        private ILevelDataProvider _dataProvider;

        [Header("Settings")] [SerializeField] private int maxLevelCount = 50;

        private const string SAVE_FILE_NAME = "PlayerData";
        private UserProfile _userProfile;

        private void Awake()
        {
            _dataProvider = dataProviderObject as ILevelDataProvider;

            _userProfile = SaveSystem.Load<UserProfile>(SAVE_FILE_NAME);

            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.LevelComplete, OnLevelCompleteHandle);
        }

        private void OnLevelCompleteHandle()
        {
            _userProfile.CurrentLevelIndex++;
            SaveSystem.Save<UserProfile>(SAVE_FILE_NAME, _userProfile);

            Debug.Log($"[LevelManager] Đã lưu JSON! Level tiếp theo: {{_currentUserProfile.CurrentLevelIndex}}");
        }

        public int GetCurrentLevelIndex() => _userProfile.CurrentLevelIndex;


        public LevelSaveData LoadCurrentLevel()
        {
            int currentLevel = GetCurrentLevelIndex();
            int playLevelIndex = CalculateActualLevelIndex(currentLevel);

            string LevelID = $"Level_{playLevelIndex}";
            LevelSaveData data = _dataProvider.GetLevelData(LevelID);

            if (data == null)
            {
                Debug.LogWarning($"[LevelManager] Fallback Level_1 do thiếu data!");
                data = _dataProvider.GetLevelData("Level_1");
            }

            return data;
        }


        private int CalculateActualLevelIndex(int currentLevel)
        {
            if (currentLevel <= maxLevelCount) return currentLevel;
            int loopStart = Mathf.Max(1, maxLevelCount / 2);
            Random.InitState(currentLevel);
            return Random.Range(loopStart, maxLevelCount + 1);
        }

        public void ResetLevel()
        {
            _userProfile.CurrentLevelIndex = 1;
            SaveSystem.Save<UserProfile>(SAVE_FILE_NAME, _userProfile);
            Debug.Log("[LevelManager] Reset Level");
        }

        private void OnDestroy()
        {
            EventManager<LogicGameEventID>.RemoveListener(LogicGameEventID.LevelComplete, OnLevelCompleteHandle);
        }
    }
}