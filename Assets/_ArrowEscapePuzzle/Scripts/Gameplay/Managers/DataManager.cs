using System;
using ArrowGame.Data;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Logic;
using GameCore.Interface;
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
        private const int DefaultMaxEnergy = 5;
        private const float DefaultEnergyRecoveryMinutes = 30f;

        public UserProfile Profile { get; private set; } = new UserProfile();
        
        public int LastEarnedStars { get; set; }
        public int LastEarnedCoins { get; set; }
        public int SelectedLevelIndex { get; set; } = -1;
        public int CurrentWinStreak => _levelStreakTracker.CurrentWinStreak;
        public bool IsStreakActive => _levelStreakTracker.IsStreakActive;
        public bool CurrentLevelAttemptIsStreakEligible => _levelStreakTracker.CurrentLevelAttemptIsStreakEligible;

        private bool _isDataDirty = false;
        private bool _hasLoadedData = false;
        private readonly LevelStreakTracker _levelStreakTracker = new LevelStreakTracker();
        private EnergySystem _energySystem;
        private int _configuredMaxEnergy = DefaultMaxEnergy;
        private float _configuredEnergyRecoveryMinutes = DefaultEnergyRecoveryMinutes;
        private int _lastPublishedEnergy = -1;
        private int _lastPublishedEnergyTimerSeconds = -1;
        private float _nextEnergyTimerUpdateAt;

        protected override void Awake()
        {
            base.Awake();
            _energySystem = CreateEnergySystem(_configuredMaxEnergy, _configuredEnergyRecoveryMinutes);
            EnsureDataLoaded();
        }

        public void Init()
        {
            EnsureDataLoaded();
            InitializeEnergyState();
            RefreshEnergyState();
            RestorePersistedStreakSession();
            PublishProfileDataOnStartup();
            Debug.Log("[DataManager] Initalized.");
        }

        private void EnsureDataLoaded()
        {
            if (_hasLoadedData) return;
            _hasLoadedData = true;
            LoadData();
        }

        private void PublishProfileDataOnStartup()
        {
            if (Profile == null) return;
            EventManager<LogicGameEventID>.Post(LogicGameEventID.CoinChanged, Profile.Coin);
            EventManager<LogicGameEventID>.Post(LogicGameEventID.StreakChanged, CurrentWinStreak);
        }

        private void LoadData()
        {
            var loadedData = SaveSystem.Load<UserProfile>(SAVE_FILE_NAME);

            if (loadedData != null)
            {
                Profile = loadedData;
                EnsureProfileData();
                Debug.Log($"[DataManager] Load thành công! Level hiện tại: {Profile.CurrentLevelIndex}");
            }
            else
            {
                Debug.Log("[DataManager] Không có file save, khởi tạo Profile mặc định.");
                EnsureProfileData();
                _isDataDirty = true;
                SaveData(); 
            }
        }
        public void ForceReloadData()
        {
            LoadData();
            InitializeEnergyState();
            RefreshEnergyState();
            RestorePersistedStreakSession();
            Debug.Log("[DataManager] Đã ép đồng bộ lại dữ liệu từ ổ cứng!");
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

        private void Update()
        {
            if (Profile == null || Profile.CurrentEnergy >= GetMaxEnergy()) return;
            if (Time.unscaledTime < _nextEnergyTimerUpdateAt) return;

            RefreshEnergyState();
            _nextEnergyTimerUpdateAt = Time.unscaledTime + 1f;
        }

        #region Energy Data

        public int GetCurrentEnergy()
        {
            return Profile != null ? Profile.CurrentEnergy : DefaultMaxEnergy;
        }

        public int GetMaxEnergy()
        {
            if (Profile == null || Profile.MaxEnergy <= 0) return DefaultMaxEnergy;
            return Profile.MaxEnergy;
        }

        public bool CanStartLevel()
        {
            RefreshEnergyState();
            return GetCurrentEnergy() > 0;
        }

        public void ConfigureEnergySystem(int maxEnergy, float recoveryMinutes)
        {
            int sanitizedMaxEnergy = Mathf.Max(1, maxEnergy);
            float sanitizedRecoveryMinutes = Mathf.Max(0.1f, recoveryMinutes);

            if (_configuredMaxEnergy == sanitizedMaxEnergy &&
                Mathf.Approximately(_configuredEnergyRecoveryMinutes, sanitizedRecoveryMinutes))
            {
                return;
            }

            _configuredMaxEnergy = sanitizedMaxEnergy;
            _configuredEnergyRecoveryMinutes = sanitizedRecoveryMinutes;
            _energySystem = CreateEnergySystem(_configuredMaxEnergy, _configuredEnergyRecoveryMinutes);

            if (Profile == null)
            {
                Profile = new UserProfile();
            }

            InitializeEnergyState();
            RefreshEnergyState();

            Debug.Log($"[DataManager] Energy config updated. Max={_configuredMaxEnergy}, Recovery={_configuredEnergyRecoveryMinutes} minutes.");
        }

        public bool TryConsumeEnergyForFailedAttempt()
        {
            return TryConsumeEnergy("failed attempt");
        }

        public bool TryConsumeEnergyForAbortAttempt()
        {
            return TryConsumeEnergy("aborted attempt");
        }

        public void RefillEnergyToMax()
        {
            if (Profile == null) return;

            Profile.MaxEnergy = GetMaxEnergy();
            Profile.CurrentEnergy = Profile.MaxEnergy;
            Profile.EnergyRecoveryStartedAtUtcTicks = 0;

            _isDataDirty = true;
            SaveData(force: true);
            PublishEnergyState(DateTime.UtcNow, force: true);
        }

        public void RefreshEnergyState()
        {
            DateTime nowUtc = DateTime.UtcNow;
            bool changed = _energySystem.Refresh(Profile, nowUtc);
            if (changed)
            {
                _isDataDirty = true;
                SaveData(force: true);
            }

            PublishEnergyState(nowUtc);
        }

        public int GetRemainingRecoverySeconds()
        {
            return GetRemainingRecoverySeconds(DateTime.UtcNow);
        }

        private void InitializeEnergyState()
        {
            int previousEnergy = Profile != null ? Profile.CurrentEnergy : DefaultMaxEnergy;
            int previousMaxEnergy = Profile != null ? Profile.MaxEnergy : DefaultMaxEnergy;
            long previousRecoveryTicks = Profile != null ? Profile.EnergyRecoveryStartedAtUtcTicks : 0;

            _energySystem.EnsureInitialized(Profile, DateTime.UtcNow);
            if (Profile != null &&
                (previousEnergy != Profile.CurrentEnergy ||
                 previousMaxEnergy != Profile.MaxEnergy ||
                 previousRecoveryTicks != Profile.EnergyRecoveryStartedAtUtcTicks))
            {
                _isDataDirty = true;
                SaveData(force: true);
            }

            PublishEnergyState(DateTime.UtcNow, force: true);
        }

        private EnergySystem CreateEnergySystem(int maxEnergy, float recoveryMinutes)
        {
            return new EnergySystem(Mathf.Max(1, maxEnergy), TimeSpan.FromMinutes(Mathf.Max(0.1f, recoveryMinutes)));
        }

        private bool TryConsumeEnergy(string reason)
        {
            DateTime nowUtc = DateTime.UtcNow;
            bool consumed = _energySystem.TryConsume(Profile, nowUtc);
            PublishEnergyState(nowUtc);

            if (!consumed)
            {
                Debug.LogWarning($"[DataManager] Không đủ năng lượng để xử lý {reason}.");
                return false;
            }

            _isDataDirty = true;
            SaveData(force: true);
            Debug.Log($"[DataManager] Consumed 1 energy because of {reason}. Remaining: {Profile.CurrentEnergy}/{Profile.MaxEnergy}");
            return true;
        }

        private int GetRemainingRecoverySeconds(DateTime nowUtc)
        {
            return _energySystem.GetRemainingRecoverySeconds(Profile, nowUtc);
        }

        private void PublishEnergyState(DateTime nowUtc, bool force = false)
        {
            if (Profile == null) return;

            int currentEnergy = GetCurrentEnergy();
            if (force || currentEnergy != _lastPublishedEnergy)
            {
                _lastPublishedEnergy = currentEnergy;
                EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.EnergyChanged, currentEnergy);
            }

            int remainingSeconds = GetRemainingRecoverySeconds(nowUtc);
            if (force || remainingSeconds != _lastPublishedEnergyTimerSeconds)
            {
                _lastPublishedEnergyTimerSeconds = remainingSeconds;
                EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.EnergyTimerChanged, remainingSeconds);
            }
        }

        #endregion

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
                if (BoosterManager.Instance != null)
                {
                    BoosterManager.Instance.CheckAndAwardUnlockedBoosters();
                }
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

        public void BeginLevelAttempt(int levelIndex, int frontierLevelIndex, bool hasPlayedLevel)
        {
            int previousStreak = CurrentWinStreak;
            _levelStreakTracker.BeginLevelAttempt(levelIndex, frontierLevelIndex, hasPlayedLevel);
            SyncPersistedStreak(previousStreak);
            Debug.Log($"[DataManager] Begin attempt L{levelIndex} | Frontier={frontierLevelIndex} | Played={hasPlayedLevel} | Eligible={CurrentLevelAttemptIsStreakEligible} | Streak={CurrentWinStreak}");
        }

        public void HandleLevelWin()
        {
            int previousStreak = CurrentWinStreak;
            _levelStreakTracker.HandleLevelWin();
            SyncPersistedStreak(previousStreak);
            Debug.Log($"[DataManager] Win streak updated: {CurrentWinStreak} (active={IsStreakActive})");
        }

        public void HandleLevelFail()
        {
            int previousStreak = CurrentWinStreak;
            _levelStreakTracker.HandleLevelFail();
            SyncPersistedStreak(previousStreak);
            Debug.Log("[DataManager] Win streak reset because level failed.");
        }

        public void ResetStreakSession()
        {
            int previousStreak = CurrentWinStreak;
            _levelStreakTracker.ResetSession();
            SyncPersistedStreak(previousStreak);
            Debug.Log("[DataManager] Win streak session reset.");
        }

        public void ResetLevelData()
        {
            Profile.CurrentLevelIndex = 1;
            ResetStreakSession();
            Profile.CurrentEnergy = GetMaxEnergy();
            Profile.EnergyRecoveryStartedAtUtcTicks = 0;
            InitializeEnergyState();
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
            
            // ÉP LƯU NGAY LẬP TỨC: Bảo vệ tài sản người chơi (Mua IAP, xem Ads xong phải có liền)
            SaveData(force: true); 

            EventManager<LogicGameEventID>.Post(LogicGameEventID.CoinChanged, Profile.Coin);
        }

        public bool TrySpendCoin(int amount)
        {
            if (amount <= 0 || Profile.Coin < amount) return false;

            Profile.Coin -= amount;
            
            // ÉP LƯU NGAY LẬP TỨC: Tránh exploit tắt app xài tiền chùa
            SaveData(force: true); 

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
                // Có thể cân nhắc thêm SaveData(force: true) ở đây nếu muốn đảm bảo lưu số sao tuyệt đối
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
            
            // ÉP LƯU NGAY LẬP TỨC
            SaveData(force: true);

            EventManager<LogicGameEventID>.Post(LogicGameEventID.BoosterChanged, type);
        }

        public bool TryConsumeBooster(BoosterType type)
        {
            int current = GetBoosterCount(type);
            if (current <= 0) return false;

            Profile.BoosterInventory[type] = current - 1;
            
            // ÉP LƯU NGAY LẬP TỨC: Dùng booster xong là mất, cấm chơi ăn gian
            SaveData(force: true);

            EventManager<LogicGameEventID>.Post(LogicGameEventID.BoosterChanged, type);
            return true;
        }

        public bool IsBoosterUnlocked(BoosterConfigSO config)
        {
            return IsBoosterUnlocked(config, GetActiveLevel());
        }

        public bool IsBoosterUnlocked(BoosterConfigSO config, int activeLevel)
        {
            if (config == null) return false;
            int requiredLevel = Mathf.Max(1, config.unlockLevel);
            return activeLevel >= requiredLevel;
        }

        public bool HasSeenBoosterIntroduction(BoosterType type)
        {
            if (Profile == null) return false;
            EnsureProfileData();
            return Profile.SeenBoosterIntroductions.TryGetValue(type, out bool seen) && seen;
        }

        public void MarkBoosterIntroductionSeen(BoosterType type)
        {
            if (Profile == null || type == BoosterType.None) return;
            EnsureProfileData();

            if (Profile.SeenBoosterIntroductions.TryGetValue(type, out bool seen) && seen)
            {
                return;
            }

            Profile.SeenBoosterIntroductions[type] = true;
            _isDataDirty = true;
            SaveData(force: true);
        }

        public void InitTestBoosters()
        {
            AddBooster(BoosterType.Hint, 99);
            AddBooster(BoosterType.Gate, 99);
            AddBooster(BoosterType.Ufo, 99);
            AddBooster(BoosterType.Lightning, 99);
        }

        #endregion
        
        //Clear Data to test
        public void DeleteAllProgress()
        {
            Profile = new UserProfile();
            SelectedLevelIndex = -1;
            LastEarnedStars = 0;
            LastEarnedCoins = 0;
            ResetStreakSession();
            InitializeEnergyState();

            SaveData(force: true);

            EventManager<LogicGameEventID>.Post(LogicGameEventID.CoinChanged, Profile.Coin);
            PublishEnergyState(DateTime.UtcNow, force: true);
            
            Debug.LogWarning("[DataManager] ĐÃ XÓA TRẮNG TOÀN BỘ DỮ LIỆU GAME CỦA NGƯỜI CHƠI!");
        }

        private void EnsureProfileData()
        {
            if (Profile == null)
            {
                Profile = new UserProfile();
            }

            Profile.CurrentLevelIndex = Mathf.Max(1, Profile.CurrentLevelIndex);
            Profile.MaxEnergy = Mathf.Max(1, Profile.MaxEnergy);
            Profile.BoosterInventory ??= new System.Collections.Generic.Dictionary<BoosterType, int>();
            Profile.SeenBoosterIntroductions ??= new System.Collections.Generic.Dictionary<BoosterType, bool>();
            Profile.LevelStars ??= new System.Collections.Generic.Dictionary<int, int>();
            Profile.AwardedBoosters ??= new System.Collections.Generic.List<BoosterType>();
            Profile.CompletedTutorials ??= new System.Collections.Generic.List<string>();
        }

        public bool HasCompletedTutorial(string tutorialID)
        {
            if (Profile == null) return false;
            EnsureProfileData();
            return Profile.CompletedTutorials.Contains(tutorialID);
        }

        public void MarkTutorialCompleted(string tutorialID)
        {
            if (Profile == null || string.IsNullOrEmpty(tutorialID)) return;
            EnsureProfileData();

            if (!Profile.CompletedTutorials.Contains(tutorialID))
            {
                Profile.CompletedTutorials.Add(tutorialID);
                _isDataDirty = true;
                SaveData(force: true);
                Debug.Log($"[DataManager] Tutorial completed: {tutorialID}");
            }
        }

        private void PublishStreakChangedIfNeeded(int previousStreak)
        {
            if (previousStreak == CurrentWinStreak) return;
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.StreakChanged, CurrentWinStreak);
        }

        private void RestorePersistedStreakSession()
        {
            _levelStreakTracker.RestorePersistedStreak(Profile != null ? Profile.CurrentWinStreak : 0);
            Debug.Log($"[DataManager] Restored persisted streak: {CurrentWinStreak}");
        }

        private void SyncPersistedStreak(int previousStreak)
        {
            if (Profile == null) return;

            Profile.CurrentWinStreak = CurrentWinStreak;
            if (previousStreak == CurrentWinStreak) return;

            _isDataDirty = true;
            SaveData(force: true);
            PublishStreakChangedIfNeeded(previousStreak);
        }
    }
}
