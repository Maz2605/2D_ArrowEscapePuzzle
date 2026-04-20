using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers; // Thêm thư viện gọi GlobalVFXManager
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "UFOBooster", menuName = "ArrowGame/Boosters/UFO")]
    public class UFOBoosterSO : BoosterConfigSO
    {
        [Header("--- 1. UFO Ship Specific Settings (Global VFX) ---")]
        public GameObject ufoShipPrefab; 
        public float ufoChargeUpTime = 1.5f; // Thời gian gồng laser (Chờ trước khi đẻ hố đen)
        public float ufoShipTotalDuration = 2.5f; // Tổng thời gian đĩa bay tồn tại trên màn hình
        public Vector3 ufoSpawnPosition = Vector3.zero; // Vị trí UFO xuất hiện (Thường là 0,0,0 ở giữa map)

        // Lưu ý: Biến vfxConfig từ class cha (BoosterConfigSO) sẽ được dùng cho 3 cái HỐ ĐEN!

        public override void Execute(GridSystem gridLogic, int targetX, int targetY, Sequence seq, Action onComplete)
        {
            var targets = gridLogic.GetMultipleEscapableArrows(3);
            if (targets == null || targets.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // BƯỚC 1: ĐẺ CHIẾC ĐĨA BAY (Dùng GlobalVFXManager)
            seq.AppendCallback(() => 
            {
                if (ufoShipPrefab != null)
                {
                    // GlobalVFXManager tự động đẻ ra, tự động thu hồi sau ufoShipTotalDuration
                    GlobalVFXManager.Instance.PlayVFX(ufoShipPrefab, ufoSpawnPosition, Quaternion.identity, ufoShipTotalDuration);
                }
            });

            // BƯỚC 2: CHỜ ĐĨA BAY GỒNG LASER
            seq.AppendInterval(ufoChargeUpTime);

            // BƯỚC 3: ĐẺ 3 CÁI HỐ ĐEN TẠI VỊ TRÍ MŨI TÊN (Dùng Targeted Event)
            float holeDuration = vfxConfig.duration;
            
            seq.AppendCallback(() =>
            {
                foreach (var target in targets)
                {
                    // Chuyền vfxConfig (Cái chứa Prefab Hố Đen) đi
                    var payload = new VFXRequestPayload { TargetArrowId = target.ID, Config = this.vfxConfig };
                    EventManager<VisualEventID>.Post(VisualEventID.PlayBoosterVFX, payload);
                }
            });

            // BƯỚC 4: CHỜ HỐ ĐEN HÚT MŨI TÊN
            seq.AppendInterval(holeDuration);

            // BƯỚC 5: XÓA DATA (Logic dọn dẹp và bật Empty Dot)
            seq.AppendCallback(() =>
            {
                foreach (var target in targets)
                {
                    gridLogic.ForceRemoveArrow(target.ID);
                }
            });

            // BƯỚC 6: CHỜ ĐĨA BAY BAY ĐI MẤT TRƯỚC KHI TRẢ INPUT
            // (Đảm bảo thời gian Sequence chạy hết khớp với thời gian ufoShipTotalDuration)
            float remainTime = Mathf.Max(0, ufoShipTotalDuration - ufoChargeUpTime - holeDuration);
            if (remainTime > 0)
            {
                seq.AppendInterval(remainTime);
            }

            seq.OnComplete(() => onComplete?.Invoke());
        }
    }
}