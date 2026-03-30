using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Boosters;
using ArrowGame.Gameplay.Logic;
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

        protected override void Awake()
        {
            base.Awake();
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
                { BoosterType.Hammer, new HammerBooster() }
                // { BoosterType.Hint, new HintBooster() } -> Sau này thêm đồ thì cứ nhét vào đây
            };
        }

        // ================= NHẬN LỆNH TỪ UI (NÚT BẤM) =================
        public void RequestUseBooster(BoosterType type)
        {
            // 1. Check xem còn hàng không?
            if (DataManager.Instance.GetBoosterCount(type) <= 0)
            {
                Debug.Log($"[BoosterManager] Hết {type} rồi! Mở popup mua thôi.");
                // UIManager.Instance.ShowPopup(PopupID.BuyBoosterPopup...);
                return;
            }

            // 2. Lấy Booster từ kho
            if (!_boosterStrategies.TryGetValue(type, out IBooster booster) || !booster.CanUse(_gridLogic))
            {
                Debug.LogWarning($"[BoosterManager] Không thể xài {type} lúc này!");
                return;
            }

            // 3. Phân loại: Xài ngay (Instant) hay Cần chọn mục tiêu (Targeted)?
            if (IsTargetedBooster(type))
            {
                _pendingTargetBooster = type;
                GameStateManager.Instance.ChangeState(GameState.WaitingBoosterTarget);
                Debug.Log($"[BoosterManager] Đã bật chế độ nhắm mục tiêu cho {type}. Hãy chọn 1 ô!");
            }
            else
            {
                // Loại xài ngay (ví dụ: Hint, Shuffle)
                ExecuteBooster(booster, -1, -1);
            }
        }

        // ================= XỬ LÝ KHI USER CLICK VÀO LƯỚI =================
        private void HandleTargetSelected(Vector2Int gridPos)
        {
            if (GameStateManager.Instance.CurrentState != GameState.WaitingBoosterTarget) return;

            // User đã chọn 1 ô, tiến hành đập!
            if (_boosterStrategies.TryGetValue(_pendingTargetBooster, out IBooster booster))
            {
                // Gọi hàm thực thi
                ExecuteBooster(booster, gridPos.x, gridPos.y);
            }
        }

        // ================= THỰC THI & KHÓA LUỒNG =================
        private void ExecuteBooster(IBooster booster, int x, int y)
        {
            // 1. KHÓA MÀN HÌNH: Tránh user spam click lung tung
            GameStateManager.Instance.ChangeState(GameState.BoosterExecuting);

            // 2. Bắt đầu chạy logic + animation
            booster.Execute(_gridLogic, x, y, () => 
            {
                // 3. CALL BACK TỪ BOOSTER: Báo cáo đã xong!
                // Trừ đồ trong kho
                DataManager.Instance.TryConsumeBooster(booster.Type);
                Debug.Log($"[BoosterManager] Xài thành công {booster.Type}. Đã trừ kho.");

                // 4. MỞ KHÓA MÀN HÌNH: Chơi tiếp
                GameStateManager.Instance.ChangeState(GameState.Playing);
            });
        }

        // ================= HỦY LỆNH (CANCEL) =================
        public void CancelPendingBooster()
        {
            if (GameStateManager.Instance.CurrentState == GameState.WaitingBoosterTarget)
            {
                GameStateManager.Instance.ChangeState(GameState.Playing);
                Debug.Log("[BoosterManager] Đã hủy nhắm mục tiêu. KHÔNG trừ đồ.");
            }
        }

        // Hàm helper để phân loại
        private bool IsTargetedBooster(BoosterType type)
        {
            // Chỉ định rõ cái nào cần user tap vào màn hình
            return type == BoosterType.Hammer; // || type == BoosterType.Bomb;
        }
    }
}