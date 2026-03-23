using ArrowGame.Data;
using GameCore.Data;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    [DefaultExecutionOrder(-100)] 
    public class DataManager : Singleton<DataManager>
    {
        private const string SAVE_FILE_NAME = "PlayerData";
        public UserProfile Profile { get; private set; } = new UserProfile();

        protected override void Awake()
        {
            base.Awake();
            LoadData();
        }
        
        private void LoadData()
        {
            var loadedData = SaveSystem.Load<UserProfile>(SAVE_FILE_NAME);
            
            if (loadedData != null)
            {
                Profile = loadedData;
                Debug.Log($"[DataManager] Load thành công! Level hiện tại: {Profile.CurrentLevelIndex}");
            }
            else
            {
                Debug.Log("[DataManager] Không có file save, khởi tạo Profile mặc định.");
            }
        }
        
        public int GetCurrentLevel() => Profile != null ? Profile.CurrentLevelIndex : 1;

        public void IncreaseLevel()
        {
            Profile.CurrentLevelIndex++;
            SaveData(); 
        }

        public void ResetLevelData()
        {
            Profile.CurrentLevelIndex = 1;
            SaveData();
            Debug.Log("[DataManager] Đã reset tiến độ Level.");
        }

        public void SaveData()
        {
            if (Profile == null) return;
            SaveSystem.Save(SAVE_FILE_NAME, Profile);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveData();
        }
    }
}