using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Boosters;
using ArrowGame.Gameplay.Logic;
using ArrowGame.UI.Components;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    public class BoosterManager : Singleton<BoosterManager>
    {
        [Header("--- KÉO CÁC FILE SCRIPTABLE OBJECT BOOSTER VÀO ĐÂY ---")]
        [SerializeField] private List<BoosterConfigSO> availableBoosters;

        public event Action<BoosterType, bool> BoosterToggleChanged;

        private readonly Dictionary<BoosterType, bool> _toggleStates = new Dictionary<BoosterType, bool>();

        private GridSystem _gridLogic;
        private Dictionary<BoosterType, BoosterConfigSO> _boosterDict;
        private Dictionary<BoosterType, IBoosterExecutor> _executorDict;
        private BoosterType _pendingTargetBooster;
        private Sequence _activeSequence;
        private BoosterExecutionContext _activeContext;
        private readonly Queue<BoosterConfigSO> _unlockIntroductionQueue = new Queue<BoosterConfigSO>();

        public BoosterType PendingBoosterType => _pendingTargetBooster;

        public void Init()
        {
            BuildExecutorRegistry();
            BuildBoosterRegistry();
            ValidateConfiguredBoosters();
            CheckAndAwardUnlockedBoosters();
        }

        public void Initialize(GridSystem logic)
        {
            _gridLogic = logic;
            _pendingTargetBooster = BoosterType.None;
            ResetAllToggleStates(false);
            SetTargetMode(false);
            BuildUnlockIntroductionQueue();
        }

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<Vector2Int>(LogicGameEventID.BoosterTargetSelected, HandleTargetSelected);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<Vector2Int>(LogicGameEventID.BoosterTargetSelected, HandleTargetSelected);
        }

        public void RequestUseBooster(BoosterType type, bool skipInstructionPopup = false)
        {
            if (!TryResolve(type, out BoosterConfigSO booster, out IBoosterExecutor executor)) return;

            if (GameManager.Instance == null || GameManager.Instance.CurrentInGameState != InGameState.Playing)
            {
                return;
            }

            if (!IsBoosterUnlocked(type))
            {
                UIManager.Instance.ShowToast($"UNLOCK AT LEVEL {Mathf.Max(1, booster.unlockLevel)}", 1f);
                return;
            }

            if (booster.isConsumable && DataManager.Instance.GetBoosterCount(type) <= 0)
            {
                BoosterBuyPopup buyPopup = UIManager.Instance.ShowPopup<BoosterBuyPopup>(PopupID.BoosterBuyPopup);
                if (buyPopup != null)
                {
                    buyPopup.Setup(booster, onBuySuccess: () => RequestUseBooster(type, skipInstructionPopup));
                }

                return;
            }

            if (!executor.CanUse(booster, _gridLogic))
            {
                UIManager.Instance.ShowToast("BOOSTER NOT AVAILABLE", 1f);
                return;
            }

            _pendingTargetBooster = type;
            SetTargetMode(false);

            if (booster.useBoosterInstructionPopup && !skipInstructionPopup)
            {
                GameManager.Instance.RequestChangeInGameState(InGameState.BoosterInstruction);
                return;
            }

            ProceedWithPendingBooster();
        }

        public bool IsLineGuideActive()
        {
            return _toggleStates.TryGetValue(BoosterType.LineGuide, out bool isActive) && isActive;
        }

        private void HandleTargetSelected(Vector2Int gridPos)
        {
            TryHandlePendingBoosterClick(gridPos);
        }

        public bool TryHandlePendingBoosterClick(Vector2Int gridPos)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentInGameState != InGameState.WaitingBoosterTarget)
            {
                return false;
            }

            if (!TryResolve(_pendingTargetBooster, out BoosterConfigSO booster, out IBoosterExecutor executor))
            {
                CancelPendingBooster();
                return true;
            }

            if (!executor.CanUse(booster, _gridLogic))
            {
                CancelPendingBooster();
                return true;
            }

            if (string.IsNullOrEmpty(_gridLogic.GetArrowIdAt(gridPos.x, gridPos.y)))
            {
                CancelPendingBooster();
                return true;
            }

            if (!executor.IsValidTarget(booster, _gridLogic, gridPos))
            {
                CancelPendingBooster();
                return true;
            }

            ExecuteBooster(booster, executor, gridPos);
            return true;
        }

        public BoosterConfigSO GetPendingBoosterConfig()
        {
            return _boosterDict != null && _boosterDict.TryGetValue(_pendingTargetBooster, out BoosterConfigSO config)
                ? config
                : null;
        }

        public void ConfirmPendingBoosterFromPopup()
        {
            ProceedWithPendingBooster();
        }

        public void CancelPendingBooster()
        {
            ClearPendingBooster();
            SetTargetMode(false);
            GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
        }

        public void ClearOnRestart()
        {
            _activeSequence?.Kill(complete: false);
            _activeContext?.Cleanup();
            _activeSequence = null;
            _activeContext = null;
            ClearPendingBooster();
            _unlockIntroductionQueue.Clear();
            SetTargetMode(false);
            ResetAllToggleStates();
        }

        public BoosterConfigSO GetBoosterConfig(BoosterType type)
        {
            return _boosterDict != null && _boosterDict.TryGetValue(type, out BoosterConfigSO config) ? config : null;
        }

        public bool IsBoosterUnlocked(BoosterType type)
        {
            BoosterConfigSO config = GetBoosterConfig(type);
            if (config == null || DataManager.Instance == null) return false;

            // 1. Kiểm tra điều kiện level
            bool levelUnlocked = DataManager.Instance.IsBoosterUnlocked(config);
            if (!levelUnlocked) return false;

            // 2. Nếu booster yêu cầu giới thiệu và người chơi chưa xem, thì vẫn coi như khóa
            if (config.showUnlockIntroduction && !DataManager.Instance.HasSeenBoosterIntroduction(type))
            {
                return false;
            }

            return true;
        }

        public int GetBoosterUnlockLevel(BoosterType type)
        {
            BoosterConfigSO config = GetBoosterConfig(type);
            return config != null ? Mathf.Max(1, config.unlockLevel) : 1;
        }

        public bool HasPendingUnlockIntroductions()
        {
            return _unlockIntroductionQueue.Count > 0;
        }

        public BoosterConfigSO GetCurrentUnlockIntroductionConfig()
        {
            return _unlockIntroductionQueue.Count > 0 ? _unlockIntroductionQueue.Peek() : null;
        }

        public bool ConfirmCurrentUnlockIntroduction()
        {
            if (_unlockIntroductionQueue.Count <= 0) return false;

            BoosterConfigSO config = _unlockIntroductionQueue.Dequeue();
            DataManager.Instance?.MarkBoosterIntroductionSeen(config.type);
            
            // Tặng 5 lượt sử dụng khi người chơi xác nhận giới thiệu mở khóa
            AwardBoosterUnlockReward(config.type);
            
            // Post event để BottomHUD vẽ lại slot ở trạng thái đã mở khóa
            EventManager<LogicGameEventID>.Post(LogicGameEventID.BoosterChanged, config.type);
            
            return _unlockIntroductionQueue.Count > 0;
        }

        private void ProceedWithPendingBooster()
        {
            if (!TryResolve(_pendingTargetBooster, out BoosterConfigSO booster, out IBoosterExecutor executor))
            {
                CancelPendingBooster();
                return;
            }

            if (!executor.CanUse(booster, _gridLogic))
            {
                CancelPendingBooster();
                return;
            }

            if (booster.isTargeted)
            {
                SetTargetMode(true);
                GameManager.Instance.RequestChangeInGameState(InGameState.WaitingBoosterTarget);
                return;
            }

            ExecuteBooster(booster, executor, new Vector2Int(-1, -1));
        }

        private void ExecuteBooster(BoosterConfigSO booster, IBoosterExecutor executor, Vector2Int targetGridPosition)
        {
            ClearPendingBooster();
            SetTargetMode(false);

            bool desiredToggleState = booster.type == BoosterType.LineGuide && !IsLineGuideActive();
            GameManager.Instance.RequestChangeInGameState(InGameState.BoosterExecuting);

            _activeSequence?.Kill(complete: false);
            _activeContext?.Cleanup();

            Sequence sequence = DOTween.Sequence()
                .SetId("BoosterExecution")
                .OnKill(() =>
                {
                    _activeContext?.Cleanup();
                    _activeContext = null;

                    if (GameManager.Instance != null && GameManager.Instance.CurrentInGameState == InGameState.BoosterExecuting)
                    {
                        GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
                    }
                });

            _activeSequence = sequence;
            _activeContext = new BoosterExecutionContext(booster, _gridLogic, targetGridPosition, sequence, desiredToggleState);

            executor.Execute(_activeContext, success =>
            {
                bool consumed = true;
                if (success && booster.isConsumable)
                {
                    consumed = DataManager.Instance.TryConsumeBooster(booster.type);
                    if (!consumed)
                    {
                        Debug.LogError($"[BoosterManager] Booster {booster.type} executed but inventory consume failed.");
                    }
                }

                if (success && consumed && booster.type == BoosterType.LineGuide)
                {
                    SetToggleState(BoosterType.LineGuide, desiredToggleState);
                }

                _activeContext?.Cleanup();
                _activeContext = null;
                _activeSequence = null;

                if (GameManager.Instance != null && GameManager.Instance.CurrentInGameState == InGameState.BoosterExecuting)
                {
                    GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
                }
            });
        }

        private bool TryResolve(BoosterType type, out BoosterConfigSO booster, out IBoosterExecutor executor)
        {
            booster = null;
            executor = null;

            if (type == BoosterType.None || _boosterDict == null || !_boosterDict.TryGetValue(type, out booster))
            {
                Debug.LogWarning($"[BoosterManager] Missing BoosterConfigSO for {type}.");
                return false;
            }

            if (_executorDict == null || !_executorDict.TryGetValue(type, out executor))
            {
                Debug.LogError($"[BoosterManager] Missing executor for {type}.");
                return false;
            }

            return true;
        }

        private void BuildExecutorRegistry()
        {
            _executorDict = new Dictionary<BoosterType, IBoosterExecutor>
            {
                { BoosterType.Hint, new HintBoosterExecutor() },
                { BoosterType.Gate, new GateBoosterExecutor() },
                { BoosterType.Ufo, new UfoBoosterExecutor() },
                { BoosterType.Lightning, new LightningBoosterExecutor() },
                { BoosterType.LineGuide, new LineGuideBoosterExecutor() },
                { BoosterType.ArrowDash, new ArrowDashBoosterExecutor() }
            };
        }

        private void BuildBoosterRegistry()
        {
            _boosterDict = new Dictionary<BoosterType, BoosterConfigSO>();
            if (availableBoosters == null) return;

            foreach (BoosterConfigSO booster in availableBoosters)
            {
                if (booster == null) continue;

                if (_boosterDict.ContainsKey(booster.type))
                {
                    Debug.LogError($"[BoosterManager] Duplicate BoosterConfigSO for {booster.type}.");
                    continue;
                }

                _boosterDict.Add(booster.type, booster);
            }
        }

        private void ValidateConfiguredBoosters()
        {
            if (_boosterDict == null || _executorDict == null) return;

            foreach (KeyValuePair<BoosterType, BoosterConfigSO> kvp in _boosterDict)
            {
                if (!_executorDict.TryGetValue(kvp.Key, out IBoosterExecutor executor))
                {
                    Debug.LogError($"[BoosterManager] No executor registered for {kvp.Key}.");
                    continue;
                }

                string error = executor.GetConfigurationError(kvp.Value);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"[BoosterManager] Invalid booster config: {error}", kvp.Value);
                }

                if (kvp.Value.unlockLevel < 1)
                {
                    Debug.LogError($"[BoosterManager] Invalid unlock level for {kvp.Key}. Unlock level must be >= 1.", kvp.Value);
                }
            }
        }

        private void BuildUnlockIntroductionQueue()
        {
            _unlockIntroductionQueue.Clear();
            if (availableBoosters == null || DataManager.Instance == null) return;

            int activeLevel = DataManager.Instance.GetActiveLevel();
            foreach (BoosterConfigSO booster in availableBoosters)
            {
                if (booster == null || !booster.showUnlockIntroduction) continue;
                if (!DataManager.Instance.IsBoosterUnlocked(booster, activeLevel)) continue;
                if (DataManager.Instance.HasSeenBoosterIntroduction(booster.type)) continue;

                _unlockIntroductionQueue.Enqueue(booster);
            }
        }

        private void ResetAllToggleStates(bool notify = true)
        {
            _toggleStates[BoosterType.LineGuide] = false;
            GameManager.Instance?.CurrentGridView?.ToggleDirectionLines(false);

            if (notify)
            {
                BoosterToggleChanged?.Invoke(BoosterType.LineGuide, false);
            }
        }

        private void SetToggleState(BoosterType type, bool isActive)
        {
            _toggleStates[type] = isActive;
            BoosterToggleChanged?.Invoke(type, isActive);
        }

        private void SetTargetMode(bool isSelecting)
        {
            GameManager.Instance?.CurrentGridView?.SetBoosterTargetMode(isSelecting);
            BoosterOverlayUI.Instance?.SetTargetMode(isSelecting);
        }

        private void ClearPendingBooster()
        {
            _pendingTargetBooster = BoosterType.None;
        }

        public void CheckAndAwardUnlockedBoosters()
        {
            if (availableBoosters == null || DataManager.Instance == null) return;
            
            int activeLevel = DataManager.Instance.GetActiveLevel();
            
            foreach (var booster in availableBoosters)
            {
                if (booster == null || booster.type == BoosterType.None) continue;
                
                // Nếu booster không có introduction popup, tặng quà mở khóa ngay lập tức khi đạt level
                if (!booster.showUnlockIntroduction && DataManager.Instance.IsBoosterUnlocked(booster, activeLevel))
                {
                    AwardBoosterUnlockReward(booster.type);
                    EventManager<LogicGameEventID>.Post(LogicGameEventID.BoosterChanged, booster.type);
                }
            }
        }

        private void AwardBoosterUnlockReward(BoosterType type)
        {
            if (DataManager.Instance == null) return;
            
            // Nếu chưa từng được tặng quà mở khóa
            if (DataManager.Instance.Profile.AwardedBoosters == null || 
                !DataManager.Instance.Profile.AwardedBoosters.Contains(type))
            {
                if (DataManager.Instance.Profile.AwardedBoosters == null)
                {
                    DataManager.Instance.Profile.AwardedBoosters = new List<BoosterType>();
                }
                
                // Thêm vào danh sách đã tặng
                DataManager.Instance.Profile.AwardedBoosters.Add(type);
                
                // Tặng 5 cái
                int currentCount = DataManager.Instance.GetBoosterCount(type);
                DataManager.Instance.Profile.BoosterInventory[type] = currentCount + 5;
                
                DataManager.Instance.SaveData(force: true);
                Debug.Log($"[BoosterManager] Tặng 5 booster {type} do mở khóa thành công!");
            }
        }
    }
}
