using ArrowGame.Data;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Interface;
using GameCore.Data;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    [DefaultExecutionOrder(-100)]
    public class DataManager : Singleton<DataManager>, IAppService
    {
        private const string SAVE_FILE_NAME = "PlayerData";
        public UserProfile Profile { get; private set; } = new UserProfile();
        
        public int LastEarnedStars { get; set; }
        public int LastEarnedCoins { get; set; }
        public int SelectedLevelIndex { get; set; } = -1;

        private bool _isDataDirty = false;

        // protected override void Awake()
        // {
        //     base.Awake();
        //     LoadData();
        // }

        public void Init()
        {
            LoadData();
            Debug.Log("[DataManager] Initalized.");
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
                _isDataDirty = true;
                SaveData(); 
            }
        }

        public void SaveData(bool force = false)
        {
            if (Profile == null) return;
            if (!force && !_isDataDirty) return;

            SaveSystem.Save(SAVE_FILE_NAME, Profile);
            _isDataDirty = false;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveData();
        }

        #region Level Data

        public int GetCurrentLevel() => Profile != null ? Profile.CurrentLevelIndex : 1;

        public int GetActiveLevel() 
        {
            return SelectedLevelIndex != -1 ? SelectedLevelIndex : GetCurrentLevel();
        }

        /// <summary>
        /// Gọi hàm này khi người chơi THẮNG màn hiện tại
        /// Xử lý luôn logic cày lại map cũ (Replay)
        /// </summary>
        public void CompleteCurrentLevel()
        {
            int playedLevel = GetActiveLevel();
            
            // Nếu map vừa thắng chính là map cao nhất -> mở khóa map mới
            if (playedLevel == Profile.CurrentLevelIndex)
            {
                Profile.CurrentLevelIndex++;
                Debug.Log($"[DataManager] Chúc mừng! Mở khóa Level mới: {Profile.CurrentLevelIndex}");
            }
            else
            {
                Debug.Log($"[DataManager] Hoàn thành Replay Level {playedLevel}. Vẫn giữ nguyên Max Level: {Profile.CurrentLevelIndex}");
            }

            // Reset selected level để lần sau load lại là load map cao nhất (nếu user ko chọn map cụ thể)
            SelectedLevelIndex = -1;
            
            // Đánh dấu bẩn và ép lưu ngay lập tức vì qua màn là cột mốc quan trọng
            _isDataDirty = true;
            SaveData(force: true); 
        }

        public void ResetLevelData()
        {
            Profile.CurrentLevelIndex = 1;
            _isDataDirty = true;
            SaveData(force: true);
            Debug.Log("[DataManager] Đã reset tiến độ Level.");
        }

        #endregion

        #region Coin Data

        public int GetCurrentCoin() => Profile != null ? Profile.Coin : 0;

        public void AddCoin(int amount)
        {
            if (amount <= 0) return;
            Profile.Coin += amount;
            _isDataDirty = true; 

            EventManager<LogicGameEventID>.Post(LogicGameEventID.CoinChanged, Profile.Coin);
        }

        public bool TrySpendCoin(int amount)
        {
            if (amount <= 0 || Profile.Coin < amount) return false;

            Profile.Coin -= amount;
            _isDataDirty = true; 

            EventManager<LogicGameEventID>.Post(LogicGameEventID.CoinChanged, Profile.Coin);
            return true;
        }

        #endregion
        
        #region Level Progress 
        
        public int GetLevelStars(int levelIndex)
        {
            if (Profile != null && Profile.LevelStars.TryGetValue(levelIndex, out int stars))
                return stars;
            return 0;
        }

        public bool HasPlayedLevel(int levelIndex)
        {
            return Profile != null && Profile.LevelStars.ContainsKey(levelIndex);
        }

        public void SaveLevelStars(int levelIndex, int stars)
        {
            if (Profile == null) return;
            
            if (!Profile.LevelStars.ContainsKey(levelIndex) || stars > Profile.LevelStars[levelIndex])
            {
                Profile.LevelStars[levelIndex] = stars;
                _isDataDirty = true;
            }
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
            _isDataDirty = true;

            EventManager<LogicGameEventID>.Post(LogicGameEventID.BoosterChanged, type);
        }

        public bool TryConsumeBooster(BoosterType type)
        {
            int current = GetBoosterCount(type);
            if (current <= 0) return false;

            Profile.BoosterInventory[type] = current - 1;
            _isDataDirty = true;

            EventManager<LogicGameEventID>.Post(LogicGameEventID.BoosterChanged, type);
            return true;
        }

        #endregion
        
        //Clear Data to test
        public void DeleteAllProgress()
        {
            Profile = new UserProfile();
            SelectedLevelIndex = -1;
            LastEarnedStars = 0;
            LastEarnedCoins = 0;

            SaveData(force: true);

            EventManager<LogicGameEventID>.Post(LogicGameEventID.CoinChanged, Profile.Coin);
            
            Debug.LogWarning("[DataManager] ĐÃ XÓA TRẮNG TOÀN BỘ DỮ LIỆU GAME CỦA NGƯỜI CHƠI!");
        }
        
        
        
    }
}