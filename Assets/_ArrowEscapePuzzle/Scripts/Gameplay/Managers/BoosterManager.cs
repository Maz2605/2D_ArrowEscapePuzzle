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

            if (booster.isConsumable && DataManager.Instance.GetBoosterCount(type) <= 0)
            {
                var buyPopup = UIManager.Instance.ShowPopup<BoosterBuyPopup>(PopupID.BoosterBuyPopup);
        
                buyPopup.Setup(booster, onBuySuccess: () => 
                {
                    // Tự động sử dụng luôn sau khi mua thành công (giảm số lần click cho user)
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

            _activeSequence?.Kill();
            _activeSequence = DOTween.Sequence().SetId("BoosterExecution");

            booster.Execute(_gridLogic, x, y, _activeSequence, () => 
            {
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