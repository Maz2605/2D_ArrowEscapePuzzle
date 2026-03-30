using System;
using ArrowGame.Data;
using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Logic;
using DG.Tweening;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    public class HammerBooster : IBooster
    {
        public BoosterType Type => BoosterType.Hammer;

        public bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty(); // Bảng còn mũi tên thì mới cho đập
        }

        public void Execute(GridSystem gridLogic, int targetX, int targetY, Action onComplete)
        {
            // 1. Kiểm tra mục tiêu user bấm vào có hợp lệ không
            string targetArrowId = gridLogic.GetArrowIdAt(targetX, targetY);

            if (string.IsNullOrEmpty(targetArrowId))
            {
                Debug.Log("[Hammer] Đập hụt vào ô trống! Hủy lệnh.");
                // Không gọi onComplete() với tham số thành công, hoặc trả state về thẳng Playing
                return; // Tạm thời return, Manager sẽ xử lý timeout hoặc bắt user chọn lại
            }

            Debug.Log($"[Hammer] Đang nện búa vào mũi tên: {targetArrowId}...");

            // 2. GỌI EVENT ĐỂ SPAWN VISUAL BÚA (Nếu bạn làm VFX thì uncomment dòng dưới)
            // EventManager<LogicGameEventID>.Post(LogicGameEventID.SpawnHammerVisual, targetX, targetY);

            // 3. TẠO ĐỘ TRỄ (Chờ Animation búa đập xuống)
            DOVirtual.DelayedCall(0.3f, () => 
            {
                // Xóa Data
                gridLogic.ForceRemoveArrow(targetArrowId);
                
                // Báo cho Manager là "Đập xong rồi!"
                onComplete?.Invoke();
                
            }).SetUpdate(false);
        }
    }
}