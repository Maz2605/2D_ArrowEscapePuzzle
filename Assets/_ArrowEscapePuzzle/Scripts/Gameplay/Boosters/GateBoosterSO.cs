using System;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Logic;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "GateBooster", menuName = "ArrowGame/Boosters/Gate")]
    public class GateBoosterSO : BoosterConfigSO
    {
        public override void Execute(GridSystem gridLogic, int targetX, int targetY, Sequence seq, Action onComplete)
        {
            string targetId = gridLogic.GetArrowIdAt(targetX, targetY);
            if (string.IsNullOrEmpty(targetId))
            {
                onComplete?.Invoke();
                return;
            }

            var payload = new VFXRequestPayload { TargetArrowId = targetId, Config = this.vfxConfig };
            EventManager<VisualEventID>.Post(VisualEventID.PlayBoosterVFX, payload);

            float duration = vfxConfig.duration;
            seq.AppendInterval(duration) 
                .OnComplete(() => 
                {
                    gridLogic.ForceRemoveArrow(targetId);
                    onComplete?.Invoke(); 
                });
        }
    }
}