using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Logic;
using DG.Tweening;
using ShareCore.Data;
using UnityEngine;
using ArrowGame.Utils;
using GameCore.Utils.DesignPattern.Events;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "LaserBooster", menuName = "ArrowGame/Boosters/Laser")]
    public class LightningBoosterSO : BoosterConfigSO
    {
        public override void Execute(GridSystem gridLogic, int targetX, int targetY, Sequence seq, Action onComplete)
        {
            string clickedId = gridLogic.GetArrowIdAt(targetX, targetY);
            if (string.IsNullOrEmpty(clickedId)) { onComplete?.Invoke(); return; }

            var group = gridLogic.ArrowGroups[clickedId];
            CellType headType = group.Find(a => a.Type == CellType.ArrowHeadUp || a.Type == CellType.ArrowHeadDown || 
                                                a.Type == CellType.ArrowHeadLeft || a.Type == CellType.ArrowHeadRight)?.Type ?? CellType.None;

            if (headType == CellType.None) { onComplete?.Invoke(); return; }

            // Lấy thêm 4 mục tiêu ngẫu nhiên cùng loại
            List<string> sameDirectionIds = gridLogic.GetAllArrowIdsByType(headType, clickedId);
            sameDirectionIds.Shuffle();
            if (sameDirectionIds.Count > 4) sameDirectionIds = sameDirectionIds.GetRange(0, 4);

            List<string> finalTargets = new List<string> { clickedId };
            finalTargets.AddRange(sameDirectionIds);

            float duration = vfxConfig.duration;

            // BƯỚC 1: Bắn Event gọi tia sét Dây Chuyền
            seq.AppendCallback(() => 
            {
                var payload = new VFXChainRequestPayload 
                { 
                    TargetArrowIds = finalTargets, 
                    Config = this.vfxConfig 
                };
                EventManager<VisualEventID>.Post(VisualEventID.PlayChainBoosterVFX, payload);
            });

            // BƯỚC 2: Chờ tia sét giật xong (khoảng 0.4s)
            seq.AppendInterval(duration);

            // BƯỚC 3: Đồng loạt xóa Data, tung Empty Dot
            seq.AppendCallback(() => 
            {
                foreach (var id in finalTargets)
                {
                    gridLogic.ForceRemoveArrow(id);
                }
            });

            seq.OnComplete(() => onComplete?.Invoke());
        }
    }
}