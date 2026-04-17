using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Boosters;
using ArrowGame.Gameplay.Logic;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;
using DG.Tweening;

namespace ArrowGame.Gameplay.Managers
{
    public class BoosterManager : Singleton<BoosterManager>
    {
        [Header("--- KÉO CÁC FILE SCRIPTABLE OBJECT BOOSTER VÀO ĐÂY ---")]
        [SerializeField] private List<BoosterConfigSO> availableBoosters;

        private GridSystem _gridLogic;
        private Dictionary<BoosterType, BoosterConfigSO> _boosterDict;
        
        // Lưu trữ trạng thái bật/tắt của các Booster dạng Toggle tại Runtime
        private Dictionary<BoosterType, bool> _toggleStates = new Dictionary<BoosterType, bool>();
        
        private BoosterType _pendingTargetBooster;
        private Sequence _activeSequence;

        public void Init()
        {
            _boosterDict = new Dictionary<BoosterType, BoosterConfigSO>();
            
            foreach (var booster in availableBoosters)
            {
                if (booster != null && !_boosterDict.ContainsKey(booster.type))
                {
                    _boosterDict.Add(booster.type, booster);
                }
            }
        }

        public void Initialize(GridSystem logic)
        {
            _gridLogic = logic;
            
            // Reset trạng thái Toggle khi bắt đầu một màn chơi mới
            ResetAllToggleStates(false);
        }

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<Vector2Int>(LogicGameEventID.BoosterTargetSelected, HandleTargetSelected);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<Vector2Int>(LogicGameEventID.BoosterTargetSelected, HandleTargetSelected);
        }

        public void RequestUseBooster(BoosterType type)
        {
            if (_boosterDict == null || !_boosterDict.TryGetValue(type, out BoosterConfigSO booster))
            {
                Debug.LogWarning($"[BoosterManager] Lỗi: Chưa có Data SO cho Booster {type}!");
                return;
            }

            // ĐẶC BIỆT: Xử lý LineGuide (Cơ chế Toggle & Hoàn toàn miễn phí)
            if (type == BoosterType.LineGuide)
            {
                HandleLineGuideToggle();
                return;
            }

            // Kiểm tra số lượng cho các Booster tiêu tốn (isConsumable)
            if (booster.isConsumable && DataManager.Instance.GetBoosterCount(type) <= 0)
            {
                var buyPopup = UIManager.Instance.ShowPopup<BoosterBuyPopup>(PopupID.BoosterBuyPopup);
                buyPopup.Setup(booster, onBuySuccess: () => 
                {
                    RequestUseBooster(type); 
                });
                return;
            }

            if (!booster.CanUse(_gridLogic)) return;

            if (booster.isTargeted)
            {
                _pendingTargetBooster = type;
                EventManager<VisualEventID>.Post(VisualEventID.BoosterTargetModeChanged, true);
                GameManager.Instance.RequestChangeInGameState(InGameState.WaitingBoosterTarget);
            }
            else
            {
                ExecuteBooster(booster, -1, -1);
            }
        }
        public bool IsLineGuideActive()
        {
            if (_toggleStates.TryGetValue(BoosterType.LineGuide, out bool isActive))
            {
                return isActive;
            }
            return false;
        }

        private void HandleLineGuideToggle()
        {
            if (!_toggleStates.ContainsKey(BoosterType.LineGuide))
                _toggleStates[BoosterType.LineGuide] = false;

            _toggleStates[BoosterType.LineGuide] = !_toggleStates[BoosterType.LineGuide];
            bool isActive = _toggleStates[BoosterType.LineGuide];

            // Lúc này đang In-game, GridView chắc chắn sống, bắn Event thoải mái
            EventManager<LogicGameEventID>.Post(LogicGameEventID.LineGuideToggle, isActive);
            EventManager<VisualEventID>.Post(VisualEventID.ShowDirectionLines, isActive);
            
            Debug.Log($"[BoosterManager] LineGuide Toggle: {isActive}");
        }

        private void HandleTargetSelected(Vector2Int gridPos)
        {
            if (_boosterDict.TryGetValue(_pendingTargetBooster, out BoosterConfigSO booster))
            {
                ExecuteBooster(booster, gridPos.x, gridPos.y);
            }
        }

        private void ExecuteBooster(BoosterConfigSO booster, int x, int y)
        {
            EventManager<VisualEventID>.Post(VisualEventID.BoosterTargetModeChanged, false);
            GameManager.Instance.RequestChangeInGameState(InGameState.BoosterExecuting);

            _activeSequence?.Kill(complete: false);
            
            _activeSequence = DOTween.Sequence()
                .SetId("BoosterExecution")
                .OnKill(() => 
                {
                    if (GameManager.Instance != null && GameManager.Instance.CurrentInGameState == InGameState.BoosterExecuting)
                    {
                        GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
                    }
                });

            booster.Execute(_gridLogic, x, y, _activeSequence, () => 
            {
                // Chỉ trừ lượt nếu là loại tiêu tốn (LineGuide không rơi vào đây)
                if (booster.isConsumable)
                {
                    DataManager.Instance.TryConsumeBooster(booster.type);
                }

                GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
            });
        }

        public void ClearOnRestart()
        {
            _activeSequence?.Kill();
            ResetAllToggleStates(); // Đảm bảo mọi Booster Toggle đều tắt khi Restart màn
        }

        private void ResetAllToggleStates(bool triggerEvents = true)
        {
            // Ép LineGuide về trạng thái OFF khi vào màn mới
            _toggleStates[BoosterType.LineGuide] = false;
            
            if (triggerEvents)
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.LineGuideToggle, false);
                EventManager<VisualEventID>.Post(VisualEventID.ShowDirectionLines, false);
            }
        }

        public void CancelPendingBooster()
        {
            EventManager<VisualEventID>.Post(VisualEventID.BoosterTargetModeChanged, false);
            GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
        }

        public BoosterConfigSO GetBoosterConfig(BoosterType type)
        {
            if (_boosterDict != null && _boosterDict.TryGetValue(type, out BoosterConfigSO config))
            {
                return config;
            }
            return null;
        }
        
        
    }
}