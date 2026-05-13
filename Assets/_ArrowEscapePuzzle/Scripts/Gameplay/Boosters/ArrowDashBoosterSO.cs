using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Utils;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using ShareCore.Data;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "ArrowDashBoosterSO", menuName = "ArrowGame/Boosters/ArrowDashBoosterSO")]
    public class ArrowDashBoosterSO : BoosterConfigSO
    {
        [Header("--- Arrow Dash Settings ---")]
        public int targetCount = 5;
        public float delayBetweenEscapes = 0.15f; 
        public float highlightDuration = 0.5f;

        public override void Execute(GridSystem gridLogic, int targetX, int targetY, Sequence seq, Action onComplete)
        {
            string clickedId = gridLogic.GetArrowIdAt(targetX, targetY);
            if (string.IsNullOrEmpty(clickedId)) { onComplete?.Invoke(); return; }

            Direction4? primaryDirection = gridLogic.GetPrimaryExitDirection(clickedId);
            if (!primaryDirection.HasValue) { onComplete?.Invoke(); return; }

            List<string> sameDirectionIds = gridLogic.GetArrowIdsByPrimaryDirection(primaryDirection.Value); 
            sameDirectionIds.Shuffle();
            List<string> finalTargets = sameDirectionIds.GetRange(0, Mathf.Min(targetCount, sameDirectionIds.Count));

            // BƯỚC 1: Làm tối và Highlight
            seq.AppendCallback(() => 
            {
                EventManager<VisualEventID>.Post(VisualEventID.DarkenScreen, true);
                foreach (var id in sameDirectionIds) EventManager<VisualEventID>.Post(VisualEventID.ShowFocusHighlight, id);
            });

            seq.AppendInterval(highlightDuration);

            // BƯỚC 2: Trượt ra không cùng lúc
            foreach (var id in finalTargets)
            {
                seq.AppendCallback(() => 
                {
                    // 1. Kích hoạt hiệu ứng trượt
                    EventManager<VisualEventID>.Post(VisualEventID.PlayDashEscape, id);
                    // 2. Dọn Data + Đẻ EmptyDot
                    gridLogic.ForceRemoveArrow(id); 
                });
                seq.AppendInterval(delayBetweenEscapes); // Độ trễ giữa mỗi mũi tên
            }

            // BƯỚC 3: Dọn dẹp tắt đèn
            seq.AppendCallback(() => 
            {
                EventManager<VisualEventID>.Post(VisualEventID.DarkenScreen, false);
                foreach (var id in sameDirectionIds) EventManager<VisualEventID>.Post(VisualEventID.HideFocusHighlight, id);
            });

            seq.OnComplete(() => onComplete?.Invoke());
        }
    }
}
