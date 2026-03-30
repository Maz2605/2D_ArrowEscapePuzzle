using ArrowGame.Data;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using GameCore.Data;
using GameCore.Utils.DesignPattern.Events;
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

        public void SaveData()
        {
            if (Profile == null) return;
            SaveSystem.Save(SAVE_FILE_NAME, Profile);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveData();
        }

        #region Level Data

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

        #endregion

        #region Coin Data

        public int GetCurrentCoin() => Profile != null ? Profile.Coin : 0;

        public void AddCoin(int amount)
        {
            if (amount <= 0) return;
            Profile.Coin += amount;
            SaveData();

            EventManager<LogicGameEventID>.Post(LogicGameEventID.CoinChanged, Profile.Coin);
        }

        public bool TrySpendCoin(int amount)
        {
            if (amount <= 0 || Profile.Coin < amount) return false;

            Profile.Coin -= amount;
            SaveData();

            EventManager<LogicGameEventID>.Post(LogicGameEventID.CoinChanged, Profile.Coin);
            return true;
        }

        #endregion

        #region Booster Data

        public int GetBoosterCount(BoosterType type)
        {
            if (Profile.BoosterInventory.TryGetValue(type, out int count))
            {
                return count;
            }

            return 0; 
        }

        public void AddBooster(BoosterType type, int amount)
        {
            if (amount <= 0) return;

            int current = GetBoosterCount(type);
            Profile.BoosterInventory[type] = current + amount;
            SaveData();

            EventManager<LogicGameEventID>.Post(LogicGameEventID.BoosterChanged, type);
        }

        public bool TryConsumeBooster(BoosterType type)
        {
            int current = GetBoosterCount(type);
            if (current <= 0) return false; // Không đủ đồ

            Profile.BoosterInventory[type] = current - 1;
            SaveData();

            EventManager<LogicGameEventID>.Post(LogicGameEventID.BoosterChanged, type);
            return true;
        }
    }

    #endregion
}

