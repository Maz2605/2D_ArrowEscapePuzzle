using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Boosters;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Interface;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    public class BoosterManager : Singleton<BoosterManager>
    {
        private GridSystem _gridLogic;
        private Dictionary<BoosterType, IBooster> _boosterStrategies;
        private BoosterType _pendingTargetBooster;

        // protected override void Awake()
        // {
        //     base.Awake();
        //     InitStrategies();
        // }

        public void Init()
        {
            InitStrategies();
        }

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<Vector2Int>(LogicGameEventID.BoosterTargetSelected, HandleTargetSelected);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<Vector2Int>(LogicGameEventID.BoosterTargetSelected, HandleTargetSelected);
        }

        public void Initialize(GridSystem logic)
        {
            _gridLogic = logic;
        }

        private void InitStrategies()
        {
            _boosterStrategies = new Dictionary<BoosterType, IBooster>
            {
                { BoosterType.Hammer, new HammerBooster() },
                { BoosterType.Hint, new HintBooster() },
                { BoosterType.LineGuide, new LineGuideBooster() } // Booster mới (Toggle & Free)
            };
        }

        // ================= NHẬN LỆNH TỪ UI (NÚT BẤM) =================
        public void RequestUseBooster(BoosterType type)
        {
            // 1. REFACTOR: Bỏ qua kiểm tra kho đồ nếu là LineGuide (Miễn phí)
            if (type != BoosterType.LineGuide && DataManager.Instance.GetBoosterCount(type) <= 0)
            {
                Debug.Log($"[BoosterManager] Hết {type} rồi! Mở popup mua.");
                return;
            }

            // 2. Kiểm tra tính hợp lệ của Booster
            if (!_boosterStrategies.TryGetValue(type, out IBooster booster) || !booster.CanUse(_gridLogic))
            {
                Debug.LogWarning($"[BoosterManager] Không thể xài {type} lúc này!");
                return;
            }

            // 3. Phân loại luồng xử lý
            if (IsTargetedBooster(type))
            {
                _pendingTargetBooster = type;
                GameManager.Instance.RequestChangeInGameState(InGameState.WaitingBoosterTarget);
            }
            else
            {
                // Loại xài ngay (Hint, LineGuide Toggle)
                ExecuteBooster(booster, -1, -1);
            }
        }

        // ================= XỬ LÝ KHI NGƯỜI CHƠI BÍ ĐƯỜNG (IDLE HINT) =================
        /// <summary>
        /// Được gọi từ IdleHintController. Không check kho đồ, không trừ phí.
        /// </summary>
        public void TriggerFreeIdleHint()
        {
            if (_gridLogic == null || _gridLogic.IsBoardEmpty()) return;

            if (_boosterStrategies.TryGetValue(BoosterType.Hint, out IBooster hintBooster))
            {
                // Thực thi logic Hint nhưng không thông qua luồng ExecuteBooster để tránh trừ đồ
                hintBooster.Execute(_gridLogic, -1, -1, null);
            }
        }

        private void HandleTargetSelected(Vector2Int gridPos)
        {
            if (_boosterStrategies.TryGetValue(_pendingTargetBooster, out IBooster booster))
            {
                ExecuteBooster(booster, gridPos.x, gridPos.y);
            }
        }

        // ================= THỰC THI & QUẢN LÝ KHO ĐỒ =================
        private void ExecuteBooster(IBooster booster, int x, int y)
        {
            // Khóa tương tác trong lúc booster đang diễn hoạt
            GameManager.Instance.RequestChangeInGameState(InGameState.BoosterExecuting);

            booster.Execute(_gridLogic, x, y, () => 
            {
                // REFACTOR: Chỉ trừ đồ nếu booster KHÔNG phải loại miễn phí
                if (booster.Type != BoosterType.LineGuide)
                {
                    DataManager.Instance.TryConsumeBooster(booster.Type);
                    Debug.Log($"[BoosterManager] Đã trừ 1 {booster.Type} khỏi kho đồ.");
                }

                // Trả về trạng thái Playing để tiếp tục tương tác
                GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
            });
        }

        public void CancelPendingBooster()
        {
            GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
        }

        private bool IsTargetedBooster(BoosterType type)
        {
            return type == BoosterType.Hammer;
        }
    }
}